using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public enum SlotGroup { Active, Inactive }

public class InventoryView : MonoBehaviour
{
    [Header("Refs (auto-found if null)")]
    public InventorySystem inventory;
    public Canvas rootCanvas;
    public ScrollRect bottomScrollRect;      // Your Scroll View (for the inventory grid)

    [Header("Containers (auto-found if null)")]
    public Transform activeContainer;        // Top row (active bar)
    public Transform inventoryGridContainer; // ScrollRect.content (inventory grid)

    [Header("Prefabs (required)")]
    public GameObject slotPrefabActive;      // Must have InventorySlotUI + Icon/Count
    public GameObject slotPrefabGrid;        // Must have InventorySlotUI + Icon/Count

    [Header("Drag Ghost (optional)")]
    public GameObject dragGhostPrefab;       // If None, a simple one is created

    [Header("Active Bar Settings")]
    [Tooltip("Number of persistent slots in the active bar.")]
    public int activeSlotsCount = 5;
    [Tooltip("Clamp activeSlotsCount to InventorySystem.MAX_ACTIVE_ITEMS on Awake.")]
    public bool clampActiveToInventoryMax = true;

    [Header("Panel Auto-Refresh")]
    [Tooltip("Assign the panel GameObject you toggle on/off (e.g., FullInventoryPanel). When it becomes active, InventoryView will auto-RebuildUI().")]
    public GameObject refreshWhenShown;
    public bool autoRefreshOnPanelShow = true;

    // runtime
    private GameObject _dragGhost;
    private Image _dragGhostIcon;
    private TextMeshProUGUI _dragGhostCount;
    private bool _dragging;
    private InventorySlotUI _dragSource;
    private bool _wasPanelActive;

    // derived view of inactive (all - active)
    private readonly List<StackedItem> _inactiveView = new();

    // persistent ACTIVE slots (never destroyed)
    private readonly List<InventorySlotUI> _activeSlots = new();

    void Awake()
    {
        // First, find references (including InventorySystem)
        AutoFindRefs();

        if (clampActiveToInventoryMax)
        {
            if (inventory != null)
            {
                // Use the gameplay mode’s max (5 or 3)
                activeSlotsCount = inventory.CurrentMaxActiveSlots;
            }
            else
            {
                // Fallback if inventory not found yet
                activeSlotsCount = Mathf.Min(activeSlotsCount, InventorySystem.MAX_ACTIVE_ITEMS);
            }
        }

        EnsureInventoryPanelDropTarget(); // drop anywhere onto the inventory panel
        BuildDragGhost();
    }

    void Start()
    {
        _wasPanelActive = refreshWhenShown ? refreshWhenShown.activeInHierarchy : true;
        RebuildUI(); // initial
    }

    void OnEnable()
    {
        if (inventory != null) inventory.OnChanged += RebuildUI;
    }

    void OnDisable()
    {
        if (inventory != null) inventory.OnChanged -= RebuildUI;
        ForceEndDrag();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) ForceEndDrag();
    }
    void OnApplicationPause(bool pause)
    {
        if (pause) ForceEndDrag();
    }

    void Update()
    {
        if (_dragging && _dragGhost != null)
        {
            _dragGhost.transform.position = Input.mousePosition;

            if (Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.Escape))
                ForceEndDrag();
        }

        // Auto refresh when panel is shown
        if (autoRefreshOnPanelShow && refreshWhenShown)
        {
            bool now = refreshWhenShown.activeInHierarchy;
            if (now && !_wasPanelActive)
            {
                RebuildUI();
                Canvas.ForceUpdateCanvases();
            }
            _wasPanelActive = now;
        }
    }

    void AutoFindRefs()
    {
        if (!inventory) inventory = FindAnyObjectByType<InventorySystem>();
        if (!rootCanvas)
        {
            rootCanvas = GetComponentInParent<Canvas>();
            if (!rootCanvas) rootCanvas = FindAnyObjectByType<Canvas>();
        }

        if (!bottomScrollRect)
        {
            bottomScrollRect = GetComponentInChildren<ScrollRect>(true);
            if (!bottomScrollRect)
            {
                var go = GameObject.Find("Scroll View");
                if (go) bottomScrollRect = go.GetComponent<ScrollRect>();
            }
        }

        if (!activeContainer)
        {
            var found = transform.Find("ActiveBarRoot");
            if (!found) found = GameObject.Find("ActiveBarRoot")?.transform;
            activeContainer = found;
        }

        if (!inventoryGridContainer && bottomScrollRect != null)
            inventoryGridContainer = bottomScrollRect.content;

        if (!slotPrefabGrid) slotPrefabGrid = slotPrefabActive; // fallback if using one prefab
    }

    bool ValidateForBuild(out string reason)
    {
        if (!inventory) { reason = "InventorySystem is missing."; return false; }
        if (!slotPrefabActive) { reason = "slotPrefabActive is not assigned."; return false; }
        if (!slotPrefabGrid) { reason = "slotPrefabGrid is not assigned."; return false; }
        if (!activeContainer) { reason = "activeContainer is not assigned and could not be found."; return false; }
        if (!inventoryGridContainer) { reason = "inventoryGridContainer is not assigned and could not be found."; return false; }
        reason = null; return true;
    }

    public void RebuildUI()
    {
        if (!ValidateForBuild(out var why))
        {
            Debug.LogWarning($"[InventoryView] RebuildUI aborted: {why}", this);
            return;
        }

        Vector2 savedScroll = Vector2.one;
        if (bottomScrollRect) savedScroll = bottomScrollRect.normalizedPosition;

        BuildInactiveView();
        BuildActiveBarGrowOnly();     // persistent active slots
        BuildInventoryGridRecreate(); // <<< RECREATE (so empty ones are removed)

        if (bottomScrollRect)
        {
            Canvas.ForceUpdateCanvases();
            bottomScrollRect.normalizedPosition = savedScroll;
        }
    }

    // --- Active Bar (grow-only placeholders, then bind) ---
    void BuildActiveBarGrowOnly()
    {
        if (_activeSlots.Count < activeSlotsCount)
        {
            int toAdd = activeSlotsCount - _activeSlots.Count;
            for (int i = 0; i < toAdd; i++)
            {
                var go = Instantiate(slotPrefabActive, activeContainer);
                var slot = go.GetComponent<InventorySlotUI>() ?? go.AddComponent<InventorySlotUI>();
                _activeSlots.Add(slot);
            }
        }

        int iItem = 0;
        for (; iItem < inventory.activeItems.Count && iItem < _activeSlots.Count; iItem++)
        {
            var s = inventory.activeItems[iItem];
            _activeSlots[iItem].Bind(this, SlotGroup.Active, iItem, s.item, s.count);
        }
        for (int i = iItem; i < _activeSlots.Count; i++)
        {
            _activeSlots[i].Bind(this, SlotGroup.Active, i, null, 0); // empty active slot (drop target)
        }
    }

    // --- Inactive view (all - active) ---
    void BuildInactiveView()
    {
        var dict = new Dictionary<ItemSO, int>();

        // 1) Count all copies you own (from allItems)
        foreach (var s in inventory.allItems)
        {
            if (s.item == null) continue;
            int current = 0;
            dict.TryGetValue(s.item, out current);
            dict[s.item] = current + s.count;    // count is usually 1 now, but this is safe
        }

        // 2) Subtract any copies that are currently active
        foreach (var s in inventory.activeItems)
        {
            if (s == null || s.item == null) continue;

            if (dict.TryGetValue(s.item, out var c))
            {
                c -= s.count;
                if (c <= 0) dict.Remove(s.item);
                else dict[s.item] = c;
            }
        }

        // 3) Build a *non-stacked* view: one entry per inactive copy, count always = 1
        _inactiveView.Clear();
        foreach (var kv in dict)
        {
            var item = kv.Key;
            int inactiveCount = kv.Value;

            for (int i = 0; i < inactiveCount; i++)
            {
                _inactiveView.Add(new StackedItem
                {
                    item = item,
                    count = 1   // <<< always 1, so no stacking in UI
                });
            }
        }
    }


    // --- Inventory grid (RECREATE: destroy and rebuild exactly to match inactive items) ---
    void BuildInventoryGridRecreate()
    {
        // clear all old children (so empty slots disappear)
        for (int i = inventoryGridContainer.childCount - 1; i >= 0; i--)
            Destroy(inventoryGridContainer.GetChild(i).gameObject);

        // rebuild to match current inactive stacks
        for (int i = 0; i < _inactiveView.Count; i++)
        {
            var stack = _inactiveView[i];
            var go = Instantiate(slotPrefabGrid, inventoryGridContainer);
            var slot = go.GetComponent<InventorySlotUI>() ?? go.AddComponent<InventorySlotUI>();
            slot.Bind(this, SlotGroup.Inactive, i, stack.item, stack.count);
        }
    }

    // ===== Drag & Drop API (called by InventorySlotUI) =====

    public void BeginDrag(InventorySlotUI source)
    {
        if (source == null || source.item == null) return;

        _dragSource = source;
        _dragging = true;

        if (_dragGhostIcon) _dragGhostIcon.sprite = source.icon ? source.icon.sprite : null;
        if (_dragGhostCount) _dragGhostCount.text = source.count > 1 ? $"x{source.count}" : "";

        if (_dragGhost)
        {
            _dragGhost.SetActive(true);
            _dragGhost.transform.SetAsLastSibling();
            _dragGhost.transform.position = Input.mousePosition;
        }

        if (source.canvasGroup) source.canvasGroup.blocksRaycasts = false;
    }

    public void Drag(PointerEventData e) { }

    public void EndDrag(InventorySlotUI source)
    {
        DoEndDrag(source); // normal path
    }

    private void ForceEndDrag()
    {
        DoEndDrag(_dragSource); // safety path
    }

    private void DoEndDrag(InventorySlotUI source)
    {
        _dragging = false;
        if (_dragGhost) _dragGhost.SetActive(false);

        if (source && source.canvasGroup)
            source.canvasGroup.blocksRaycasts = true;

        _dragSource = null;
    }

    public void HandleDropOn(InventorySlotUI target)
    {
        if (_dragSource == null || target == null) { ForceEndDrag(); return; }
        if (_dragSource == target) { ForceEndDrag(); return; }

        // Active -> Active
        if (_dragSource.group == SlotGroup.Active && target.group == SlotGroup.Active)
        {
            if (target.index < inventory.activeItems.Count)
            {
                inventory.SwapActive(_dragSource.index, target.index);
            }
            else
            {
                // Move to "end": deactivate then re-activate at end
                var movedItem = _dragSource.item;
                int movedCount = Mathf.Max(1, _dragSource.count);
                inventory.DeactivateSlot(_dragSource.index);
                inventory.SetActiveAt(inventory.activeItems.Count, movedItem, movedCount, replace: false);
            }
        }
        // Inactive -> Active  (MOVE ENTIRE STACK)
        else if (_dragSource.group == SlotGroup.Inactive && target.group == SlotGroup.Active)
        {
            // Carry whole stack from inventory to active
            int moveCount = Mathf.Max(1, _dragSource.count);
            bool ok = inventory.SetActiveAt(target.index, _dragSource.item, moveCount, replace: true);
            if (!ok) Debug.Log("No available remaining stack to activate.");
        }
        // Active -> Inactive (drop onto any inventory slot)
        else if (_dragSource.group == SlotGroup.Active && target.group == SlotGroup.Inactive)
        {
            inventory.DeactivateSlot(_dragSource.index);
        }
        // Inactive -> Inactive: ignore

        ForceEndDrag();
    }

    // ===== Drop anywhere on inventory panel =====
    private void HandleDropOnInventoryPanel()
    {
        if (_dragSource == null) { ForceEndDrag(); return; }

        // If dragging from Active and dropped on the panel (not a slot), deactivate.
        if (_dragSource.group == SlotGroup.Active)
        {
            inventory.DeactivateSlot(_dragSource.index);
        }

        // Inactive -> panel: ignore (already in inventory)
        ForceEndDrag();
    }

    // Attach a raycastable drop target to the ScrollRect's viewport (so you can drop "anywhere" on the panel)
    void EnsureInventoryPanelDropTarget()
    {
        if (bottomScrollRect == null) return;

        var viewport = bottomScrollRect.viewport ? bottomScrollRect.viewport.gameObject
                                                 : bottomScrollRect.transform.GetChild(0).gameObject;

        var img = viewport.GetComponent<Image>();
        if (!img) img = viewport.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0); // invisible
        img.raycastTarget = true;

        var dt = viewport.GetComponent<InventoryPanelDropTarget>();
        if (!dt) dt = viewport.AddComponent<InventoryPanelDropTarget>();
        dt.view = this;
    }

    // ===== Drag ghost creation (hardened) =====
    void BuildDragGhost()
    {
        if (!rootCanvas) return;

        if (dragGhostPrefab == null)
        {
            _dragGhost = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            _dragGhost.transform.SetParent(rootCanvas.transform, false);

            _dragGhostIcon = _dragGhost.GetComponent<Image>();
            var cg = _dragGhost.GetComponent<CanvasGroup>();
            cg.alpha = 0.85f;
            cg.blocksRaycasts = false;

            if (_dragGhostIcon) _dragGhostIcon.raycastTarget = false;
        }
        else
        {
            _dragGhost = Instantiate(dragGhostPrefab, rootCanvas.transform);
            _dragGhostIcon = _dragGhost.GetComponentInChildren<Image>();
            _dragGhostCount = _dragGhost.GetComponentInChildren<TextMeshProUGUI>();
            var cg = _dragGhost.GetComponent<CanvasGroup>() ?? _dragGhost.AddComponent<CanvasGroup>();
            cg.alpha = 0.85f;
            cg.blocksRaycasts = false;
            if (_dragGhostIcon) _dragGhostIcon.raycastTarget = false;

            var slotUi = _dragGhost.GetComponent<InventorySlotUI>();
            if (slotUi) slotUi.enabled = false;
        }

        _dragGhost.SetActive(false);
    }

    [ContextMenu("Rebuild UI Now")]
    public void ContextRebuild() => RebuildUI();

    // -------- inner helper (kept in the SAME script file) --------
    // Accept drops anywhere on the inventory panel (Viewport) to deactivate active items.
    private class InventoryPanelDropTarget : MonoBehaviour, IDropHandler
    {
        public InventoryView view;
        public void OnDrop(PointerEventData eventData)
        {
            view?.HandleDropOnInventoryPanel();
        }
    }
}
