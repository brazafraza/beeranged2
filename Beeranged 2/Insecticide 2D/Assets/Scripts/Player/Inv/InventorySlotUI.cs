using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("UI")]
    public Image icon;                  // child named "Icon"
    public TextMeshProUGUI countLabel;  // child named "Count"
    public Image background;            // raycast area on the slot root

    [Header("Options")]
    [Tooltip("Ensures the slot root has a raycastable Image so empty slots can receive drops.")]
    public bool ensureRaycastBackground = true;
    [Tooltip("Color for the background image (alpha can be 0).")]
    public Color backgroundColor = new Color(0, 0, 0, 0);

    [Tooltip("If false, this slot will NOT forward OnDrop to the InventoryView. Useful for special UIs (e.g., MergeStation slots).")]
    public bool acceptDrops = true;

    [HideInInspector] public CanvasGroup canvasGroup;

    // bound data
    [HideInInspector] public SlotGroup group;
    [HideInInspector] public int index;
    [HideInInspector] public ItemSO item;
    [HideInInspector] public int count;

    private InventoryView _view;

    void Awake()
    {
        canvasGroup = gameObject.GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        if (!icon)
            icon = transform.Find("Icon") ? transform.Find("Icon").GetComponent<Image>() : null;
        if (!countLabel)
            countLabel = transform.Find("Count") ? transform.Find("Count").GetComponent<TextMeshProUGUI>() : null;

        if (ensureRaycastBackground)
        {
            if (!background) background = GetComponent<Image>();
            if (!background) background = gameObject.AddComponent<Image>();
            background.color = backgroundColor;
            background.raycastTarget = true;
        }
    }

    public void Bind(InventoryView view, SlotGroup g, int idx, ItemSO it, int ct)
    {
        _view = view;
        group = g;
        index = idx;
        item = it;
        count = ct;

        if (icon)
        {
            if (it != null)
            {
                icon.enabled = true;
                icon.sprite = it.icon;
            }
            else
            {
                icon.enabled = false;
                icon.sprite = null;
            }
        }

        if (countLabel)
            countLabel.text = (ct > 1) ? $"x{ct}" : "";

        if (background)
            background.raycastTarget = true;
    }

    // ===== Drag & Drop =====

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_view == null || item == null) return;
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
        if (!acceptDrops) return;              // <-- key change
        if (_view == null) return;
        _view.HandleDropOn(this);
    }
}
