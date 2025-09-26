using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class InventorySlotUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("UI (auto-wired if left empty)")]
    public Image icon;                       // child named "Icon"
    public TextMeshProUGUI countLabel;       // child named "Count"
    public Image background;                 // raycast target on the slot root

    [Header("Options")]
    [Tooltip("If true, ensures the slot root has a raycastable Image so empty slots can receive drops.")]
    public bool ensureRaycastBackground = true;

    [Tooltip("Color for the background raycast Image (alpha can be 0).")]
    public Color backgroundColor = new Color(0, 0, 0, 0);

    [Tooltip("Force icon to preserve aspect.")]
    public bool preserveIconAspect = true;

    [Tooltip("Force icon tint on bind (keeps it visible).")]
    public Color iconTint = Color.white;

    [HideInInspector] public CanvasGroup canvasGroup;

    // Bound data
    [HideInInspector] public SlotGroup group;
    [HideInInspector] public int index;
    [HideInInspector] public ItemSO item;
    [HideInInspector] public int count;

    private InventoryView _view;

    void Reset() { AutoWire(); }
    void Awake() { AutoWire(); }
#if UNITY_EDITOR
    void OnValidate() { if (!Application.isPlaying) AutoWire(); }
#endif

    private void AutoWire()
    {
        // Ensure CanvasGroup
        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        // Find child "Icon" first, otherwise any Image in children (including inactive)
        if (!icon)
        {
            var t = transform.Find("Icon");
            if (t) icon = t.GetComponent<Image>();
            if (!icon) icon = GetComponentInChildren<Image>(true);
        }

        // Find child "Count" first, otherwise any TMP in children
        if (!countLabel)
        {
            var t = transform.Find("Count");
            if (t) countLabel = t.GetComponent<TextMeshProUGUI>();
            if (!countLabel) countLabel = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        // Ensure a raycastable background so empty slots still receive drops
        if (ensureRaycastBackground)
        {
            if (!background) background = GetComponent<Image>();
            if (!background) background = gameObject.AddComponent<Image>();
            background.color = backgroundColor;     // can be fully transparent
            background.raycastTarget = true;        // important for OnDrop on empty slots
        }

        if (icon)
        {
            icon.preserveAspect = preserveIconAspect;
            icon.color = iconTint;
        }
    }

    /// <summary>Bind this slot to a view index / item / count.</summary>
    public void Bind(InventoryView view, SlotGroup g, int idx, ItemSO it, int ct)
    {
        _view = view;
        group = g;
        index = idx;
        item = it;
        count = Mathf.Max(0, ct);

        // ICON
        if (icon)
        {
            icon.sprite = (item != null) ? item.icon : null;
            icon.enabled = (icon.sprite != null);
            icon.color = iconTint;                 // ensure visible (not transparent)
            icon.preserveAspect = preserveIconAspect;
        }

        // COUNT
        if (countLabel)
            countLabel.text = (item != null && count > 1) ? $"x{count}" : string.Empty;

        // Keep background raycastable even when slot is empty
        if (background)
            background.raycastTarget = true;
    }

    /// <summary>Clear visuals (does not change underlying inventory data).</summary>
    public void Clear()
    {
        item = null;
        count = 0;

        if (icon)
        {
            icon.sprite = null;
            icon.enabled = false;
        }

        if (countLabel)
            countLabel.text = string.Empty;
    }

    // ===== Drag & Drop =====
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_view == null || item == null) return;   // only drag when the slot has an item
        _view.BeginDrag(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_view == null) return;
        _view.Drag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_view == null) return;
        _view.EndDrag(this);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (_view == null) return;
        // This is called even if the slot is empty, because background is a raycast target
        _view.HandleDropOn(this);
    }
}
