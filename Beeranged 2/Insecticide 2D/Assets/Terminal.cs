using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider2D))]
public class Terminal : MonoBehaviour
{
    [Header("Refs")]
    public UpgradeManager upgradeManager;   // assign or auto-find
    public TextMeshProUGUI promptText;      // optional "Press R to use"

    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.R;
    public string playerTag = "Player";
    public bool autoCloseWhenPlayerLeaves = true;

    [Header("Collider Settings")]
    [Tooltip("Ensure this Collider2D is set to Trigger.")]
    public bool requireTrigger = true;

    [Header("Debug")]
    public bool logDebug = false;

    private bool _playerInRange;
    private Collider2D _playerCol;

    void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (requireTrigger && col && !col.isTrigger)
        {
            col.isTrigger = true;
#if UNITY_EDITOR
            if (logDebug) Debug.LogWarning($"{name}: set Collider2D.isTrigger = true");
#endif
        }

        TryResolveRefs();
        SetPromptVisible(false);
        UpdatePromptText();
    }

    void Update()
    {
        TryResolveRefs();
        if (!upgradeManager) return;

        // Toggle with R while in range
        if (_playerInRange && Input.GetKeyDown(interactKey))
        {
            if (upgradeManager.IsShopOpen)
            {
                if (logDebug) Debug.Log("[Terminal] Closing shop");
                upgradeManager.CloseShop();
            }
            else
            {
                if (logDebug) Debug.Log("[Terminal] Opening shop");
                upgradeManager.OpenShop();
            }
        }

        // Keep prompt in sync every frame
        SetPromptVisible(_playerInRange && !upgradeManager.IsShopOpen);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        _playerInRange = true;
        _playerCol = other;
        if (logDebug) Debug.Log("[Terminal] OnTriggerEnter2D: in range");
        SetPromptVisible(!upgradeManager || !upgradeManager.IsShopOpen);
    }

    // IMPORTANT: handles the “first time doesn’t work” case on some setups
    void OnTriggerStay2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;

        // If somehow we missed Enter, ensure we’re marked in-range now
        if (!_playerInRange)
        {
            _playerInRange = true;
            _playerCol = other;
            if (logDebug) Debug.Log("[Terminal] OnTriggerStay2D: recovering in-range state");
        }

        // Keep prompt alive while staying in trigger and shop is closed
        if (!upgradeManager || !upgradeManager.IsShopOpen)
            SetPromptVisible(true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!IsSamePlayer(other)) return;
        _playerInRange = false;
        _playerCol = null;
        if (logDebug) Debug.Log("[Terminal] OnTriggerExit2D: out of range");

        SetPromptVisible(false);

        if (autoCloseWhenPlayerLeaves && upgradeManager && upgradeManager.IsShopOpen)
            upgradeManager.CloseShop();
    }

    // --- helpers ---

    void TryResolveRefs()
    {
        if (!upgradeManager)
            upgradeManager = FindAnyObjectByType<UpgradeManager>();
    }

    bool IsPlayer(Collider2D c) => c != null && c.CompareTag(playerTag);
    bool IsSamePlayer(Collider2D c) => c != null && c == _playerCol;

    void SetPromptVisible(bool v)
    {
        if (promptText) promptText.gameObject.SetActive(v);
    }

    void UpdatePromptText()
    {
        if (promptText) promptText.text = $"Press {interactKey} to use";
    }
}
