using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class MergeStation : MonoBehaviour
{
    [Header("Inventory & View")]
    public InventorySystem inventory;     // auto-found if null
    public InventoryView inventoryView;   // auto-found if null (to RebuildUI after inventory changes)
    public PlayerStats playerStats;       // auto-found if null (to RecalculateStats after changes)

    [Header("Slots (always exist)")]
    public RectTransform slot1;           // ingredient A
    public RectTransform slot2;           // ingredient B
    public RectTransform slot3;           // output (draggable result)

    [Header("UI Text")]
    public TextMeshProUGUI chanceText;    // placeholder text (success chance later)

    [Header("Slot Visuals (optional)")]
    public Image slot1Icon;
    public Image slot2Icon;
    public Image outputIcon;
    public TextMeshProUGUI slot1Label;
    public TextMeshProUGUI slot2Label;
    public TextMeshProUGUI outputLabel;

    [Header("Buttons (optional)")]
    public Button mergeButton;
    public Button clearButton;

    [Header("Icons")]
    [Tooltip("The fallback icon used for every merged item. If null, a simple checker is generated.")]
    public Sprite placeholderIcon;

    [Header("Debug")]
    public bool debugLog = false;

    // ====== runtime state ======
    [System.Serializable]
    private class IngredientStack
    {
        public ItemSO item;
        public int count;
        public IngredientStack(ItemSO i, int c) { item = i; count = c; }
    }

    private IngredientStack _ingA;
    private IngredientStack _ingB;
    private ItemSO _lastOutput;

    private InventorySlotUI _slot3AsSlotUI;

    void Awake()
    {
        if (!inventory) inventory = FindAnyObjectByType<InventorySystem>();
        if (!inventoryView) inventoryView = FindAnyObjectByType<InventoryView>();
        if (!playerStats) playerStats = FindAnyObjectByType<PlayerStats>();

        EnsureSlotDropCatcher(slot1, 0);
        EnsureSlotDropCatcher(slot2, 1);
        EnsureSlotDropCatcher(slot3, 2); // drop on output acts like setting ingredient B (optional)

        if (mergeButton) mergeButton.onClick.AddListener(Merge);
        if (clearButton) clearButton.onClick.AddListener(ClearAll);

        EnsureSlot3ActsLikeInventorySlot();
        RefreshUI();
    }

    // ===================== Merge =====================
    public void Merge()
    {
        if (_ingA == null || _ingB == null || _ingA.item == null || _ingB.item == null)
        {
            Debug.Log("Merge failed: need two ingredients.");
            return;
        }

        // Always use the fallback icon for the merged item
        Sprite mergedIcon = placeholderIcon ? placeholderIcon : GeneratePlaceholderSprite(64, 64);

        // Create merged runtime item
        var merged = ScriptableObject.CreateInstance<MergedItemSO>();
        merged.hideFlags = HideFlags.DontSave;

        string nameA = string.IsNullOrEmpty(_ingA.item.itemName) ? _ingA.item.name : _ingA.item.itemName;
        string nameB = string.IsNullOrEmpty(_ingB.item.itemName) ? _ingB.item.name : _ingB.item.itemName;

        merged.itemName = $"Merged: {nameA} + {nameB}";
        merged.description = $"Contains combined effects of {nameA} (x{_ingA.count}) and {nameB} (x{_ingB.count}).";
        merged.icon = mergedIcon;  // <- ALWAYS fallback
        if (debugLog) Debug.Log($"[MergeStation] Merged icon set: {merged.icon != null}");

        merged.sources = new List<MergedItemSO.SourceStack>
        {
            new MergedItemSO.SourceStack(_ingA.item, _ingA.count),
            new MergedItemSO.SourceStack(_ingB.item, _ingB.count)
        };

        // Ingredients are USED UP on merge
        _ingA = null;
        _ingB = null;

        // Show merged in Slot3 (user drags out or presses Clear)
        _lastOutput = merged;
        BindSlot3AsItem(_lastOutput);

        RefreshUI();
        if (inventoryView) inventoryView.RebuildUI();
        if (playerStats) playerStats.RecalculateStats();
    }

    public void ClearAll()
    {
        // Return ingredients back to inventory on manual clear (not on merge)
        ReturnIngredientToInventory(ref _ingA);
        ReturnIngredientToInventory(ref _ingB);

        // Clear result (do NOT auto-add)
        _lastOutput = null;
        if (_slot3AsSlotUI)
            _slot3AsSlotUI.Bind(inventoryView, SlotGroup.Inactive, -1, null, 0);

        RefreshUI();
        if (inventoryView) inventoryView.RebuildUI();
        if (playerStats) playerStats.RecalculateStats();
    }

    // ===================== Drops (consume on placement) =====================
    private void OnDropIntoSlot(int slotIndex, PointerEventData e)
    {
        var dragged = e.pointerDrag ? e.pointerDrag.GetComponentInParent<InventorySlotUI>() : null;
        if (dragged == null || dragged.item == null) return;

        ItemSO item = dragged.item;
        int stackCount = Mathf.Max(1, dragged.count);

        // If dropping onto an occupied slot with a different item, return the old one first
        if (slotIndex == 0 && _ingA != null && _ingA.item != null && _ingA.item != item)
            ReturnIngredientToInventory(ref _ingA);
        if (slotIndex == 1 && _ingB != null && _ingB.item != null && _ingB.item != item)
            ReturnIngredientToInventory(ref _ingB);

        // CONSUME from inventory immediately on placement
        ConsumeFromInventory(item, stackCount, dragged.group == SlotGroup.Active);

        // Place/stack into ingredient
        if (slotIndex == 0)
        {
            if (_ingA != null && _ingA.item == item) _ingA.count += stackCount;
            else _ingA = new IngredientStack(item, stackCount);
        }
        else if (slotIndex == 1 || slotIndex == 2) // allow drop on Slot3 to fill B
        {
            if (_ingB != null && _ingB.item == item) _ingB.count += stackCount;
            else _ingB = new IngredientStack(item, stackCount);
        }

        RefreshUI();
        if (inventoryView) inventoryView.RebuildUI();
        if (playerStats) playerStats.RecalculateStats();
    }

    // Called when dragging from Slot3 ends. If dropped OUTSIDE merge panel, add to inventory and clear.
    private void HandleSlot3EndDrag(PointerEventData e)
    {
        if (_lastOutput == null) return;

        var go = e.pointerCurrentRaycast.gameObject;
        bool droppedOnMerge =
            (go != null) &&
            (go.transform.IsChildOf(slot1) || go.transform.IsChildOf(slot2) || go.transform.IsChildOf(slot3) ||
             go.transform == slot1 || go.transform == slot2 || go.transform == slot3);

        if (!droppedOnMerge)
        {
            if (inventory != null)
                inventory.AddItem(_lastOutput); // Inventory UI shows fallback icon

            _lastOutput = null;
            if (_slot3AsSlotUI)
                _slot3AsSlotUI.Bind(inventoryView, SlotGroup.Inactive, -1, null, 0);

            RefreshUI();
            if (inventoryView) inventoryView.RebuildUI();
            if (playerStats) playerStats.RecalculateStats();
        }
        // else: dropped back on merge panel – keep showing the result
    }

    // ===================== Inventory ops (consume/return) =====================
    private void ConsumeFromInventory(ItemSO item, int count, bool cameFromActive)
    {
        if (inventory == null || count <= 0 || item == null) return;

        int remaining = count;

        // If dragged from Active, reduce active first
        if (cameFromActive)
            remaining -= RemoveFromList(inventory.activeItems, item, remaining);

        // Always reduce the total inventory
        remaining = count;
        remaining -= RemoveFromList(inventory.allItems, item, remaining);

        if (remaining > 0)
            Debug.LogWarning($"[MergeStation] Tried to consume more '{item.itemName}' than available. Missing {remaining}.");
    }

    private void ReturnIngredientToInventory(ref IngredientStack stk)
    {
        if (inventory == null || stk == null || stk.item == null || stk.count <= 0)
        {
            stk = null;
            return;
        }

        for (int i = 0; i < stk.count; i++)
            inventory.AddItem(stk.item);

        stk = null;
    }

    private int RemoveFromList(List<StackedItem> list, ItemSO item, int need)
    {
        if (list == null || item == null || need <= 0) return 0;
        int removed = 0;

        for (int i = list.Count - 1; i >= 0 && removed < need; i--)
        {
            var s = list[i];
            if (s.item != item) continue;

            int take = Mathf.Min(need - removed, s.count);
            s.count -= take;
            removed += take;

            if (s.count <= 0)
                list.RemoveAt(i);
        }
        return removed;
    }

    // ===================== UI helpers =====================
    private void RefreshUI()
    {
        // Ingredient A
        if (slot1Icon)
        {
            slot1Icon.color = Color.white;
            slot1Icon.enabled = _ingA != null && _ingA.item != null;
            slot1Icon.sprite = slot1Icon.enabled
                ? (_ingA.item.icon ?? placeholderIcon ?? GeneratePlaceholderSprite(32, 32))
                : null;
        }
        if (slot1Label)
        {
            if (_ingA != null && _ingA.item != null)
            {
                string nameA = string.IsNullOrEmpty(_ingA.item.itemName) ? _ingA.item.name : _ingA.item.itemName;
                slot1Label.text = _ingA.count > 1 ? $"{nameA} x{_ingA.count}" : nameA;
            }
            else slot1Label.text = "—";
        }

        // Ingredient B
        if (slot2Icon)
        {
            slot2Icon.color = Color.white;
            slot2Icon.enabled = _ingB != null && _ingB.item != null;
            slot2Icon.sprite = slot2Icon.enabled
                ? (_ingB.item.icon ?? placeholderIcon ?? GeneratePlaceholderSprite(32, 32))
                : null;
        }
        if (slot2Label)
        {
            if (_ingB != null && _ingB.item != null)
            {
                string nameB = string.IsNullOrEmpty(_ingB.item.itemName) ? _ingB.item.name : _ingB.item.itemName;
                slot2Label.text = _ingB.count > 1 ? $"{nameB} x{_ingB.count}" : nameB;
            }
            else slot2Label.text = "—";
        }

        // Output (always show something if there is a result)
        if (outputIcon)
        {
            outputIcon.color = Color.white;
            outputIcon.enabled = _lastOutput != null;
            outputIcon.sprite = _lastOutput
                ? (_lastOutput.icon ?? placeholderIcon ?? GeneratePlaceholderSprite(64, 64))
                : null;
            outputIcon.preserveAspect = true;
        }
        if (outputLabel)
            outputLabel.text = _lastOutput ? _lastOutput.itemName : "Merged Item";

        if (chanceText) chanceText.text = "Success: 100%"; // placeholder

        if (mergeButton) mergeButton.interactable = (_ingA != null && _ingA.item != null && _ingB != null && _ingB.item != null);
    }

    private void EnsureSlotDropCatcher(RectTransform slot, int index)
    {
        if (!slot) return;

        var img = slot.GetComponent<Image>();
        if (!img) img = slot.gameObject.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0);
        img.raycastTarget = true;

        var catcher = slot.GetComponent<DropCatcher>();
        if (!catcher) catcher = slot.gameObject.AddComponent<DropCatcher>();
        catcher.Init(this, index);
    }

    // Make Slot3 act like a draggable slot AND notify us when the drag ends to decide whether to add to inventory and clear.
    private void EnsureSlot3ActsLikeInventorySlot()
    {
        if (!slot3) return;

        var bg = slot3.GetComponent<Image>();
        if (!bg) bg = slot3.gameObject.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0);
        bg.raycastTarget = true;

        _slot3AsSlotUI = slot3.GetComponent<InventorySlotUI>();
        if (!_slot3AsSlotUI) _slot3AsSlotUI = slot3.gameObject.AddComponent<InventorySlotUI>();

        if (outputIcon) _slot3AsSlotUI.icon = outputIcon;
        if (!_slot3AsSlotUI.canvasGroup)
            _slot3AsSlotUI.canvasGroup = slot3.GetComponent<CanvasGroup>() ?? slot3.gameObject.AddComponent<CanvasGroup>();

        // Bind empty initially
        _slot3AsSlotUI.Bind(inventoryView, SlotGroup.Inactive, -1, null, 0);

        // End-drag tracker: when user drops outside merge panel, add to inventory + clear
        var endDragTracker = slot3.GetComponent<Slot3EndDragTracker>();
        if (!endDragTracker) endDragTracker = slot3.gameObject.AddComponent<Slot3EndDragTracker>();
        endDragTracker.owner = this;
    }

    private void BindSlot3AsItem(ItemSO item)
    {
        if (!_slot3AsSlotUI || !inventoryView) return;
        _slot3AsSlotUI.Bind(inventoryView, SlotGroup.Inactive, -1, item, 1);
    }

    // ===================== Icon helper =====================
    private Sprite GeneratePlaceholderSprite(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        var c1 = new Color(0.16f, 0.16f, 0.16f, 1f);
        var c2 = new Color(0.22f, 0.22f, 0.22f, 1f);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, ((x / 8 + y / 8) % 2 == 0) ? c1 : c2);
        tex.Apply();
        var sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        sp.hideFlags = HideFlags.DontSave;
        return sp;
    }

    // ===================== inner components =====================
    private class DropCatcher : MonoBehaviour, IDropHandler
    {
        private MergeStation _station;
        private int _index;
        public void Init(MergeStation station, int index) { _station = station; _index = index; }
        public void OnDrop(PointerEventData eventData) { _station?.OnDropIntoSlot(_index, eventData); }
    }

    private class Slot3EndDragTracker : MonoBehaviour, IEndDragHandler
    {
        public MergeStation owner;
        public void OnEndDrag(PointerEventData eventData) => owner?.HandleSlot3EndDrag(eventData);
    }

    [CreateAssetMenu(fileName = "MergedItem", menuName = "Inventory/Merged Item (Runtime Only)")]
    public class MergedItemSO : ItemSO
    {
        [System.Serializable]
        public class SourceStack
        {
            public ItemSO item;
            public int count;
            public SourceStack(ItemSO i, int c) { item = i; count = c; }
        }

        public List<SourceStack> sources = new List<SourceStack>();

        public override void ApplyStatModifier(PlayerStats stats)
        {
            if (sources == null) return;
            foreach (var s in sources)
            {
                if (s?.item == null || s.count <= 0) continue;
                for (int i = 0; i < s.count; i++)
                    s.item.ApplyStatModifier(stats);
            }
        }
    }
}

// -------- Small extension --------
static class _MergeMathExt
{
    public static float Clamp01(this float x)
    {
        if (x < 0f) return 0f;
        if (x > 1f) return 1f;
        return x;
    }
}