using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class MergeStation : MonoBehaviour
{
    // ======== Class tags for compatibility ========
    public enum InsectClass { Winged = 1, Predator = 2, Metamorph = 3, Swarm = 4, Dancer = 5 }

    [System.Serializable]
    public class ItemMeta
    {
        public ItemSO item;
        public InsectClass classTag = InsectClass.Winged;
        [Range(0, 100)] public float stability = 100f; // per-item intrinsic stability
    }

    [Header("Metadata Overrides (Base Items)")]
    [Tooltip("Assign class + stability for each base ItemSO here. Merged items derive their data automatically.")]
    public List<ItemMeta> itemMeta = new List<ItemMeta>();

    [Header("Inventory & View")]
    public InventorySystem inventory;     // auto-found if null
    public InventoryView inventoryView;   // auto-found if null (to RebuildUI after inventory changes)
    public PlayerStats playerStats;       // auto-found if null (to RecalculateStats after changes)

    [Header("Slots (always exist)")]
    public RectTransform slot1;           // ingredient A
    public RectTransform slot2;           // ingredient B
    public RectTransform slot3;           // output (draggable result)

    [Header("UI Text")]
    public TextMeshProUGUI chanceText;    // shows success chance

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

    [Header("Merge Behavior")]
    [Tooltip("When a merge fails, ingredients are still consumed.")]
    public bool consumeOnFail = true;

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

    // Treat all three merge slots as drag sources via InventorySlotUI
    private InventorySlotUI _slot1AsSlotUI;
    private InventorySlotUI _slot2AsSlotUI;
    private InventorySlotUI _slot3AsSlotUI;

    // Compatibility matrix (1..5). Diagonals 100.
    private static readonly float[,] COMP = {
        {0,    0,    0,    0,    0,    0   },
        {0,  100f, 85f,  70f,  70f,  85f }, // 1 Winged
        {0,   85f,100f,  85f,  70f,  70f }, // 2 Predator
        {0,   70f, 85f, 100f,  85f,  70f }, // 3 Metamorph
        {0,   70f, 70f,  85f, 100f,  85f }, // 4 Swarm
        {0,   85f, 70f,  70f,  85f, 100f }  // 5 Dancer
    };

    // Decay curve by merge depth (index: depth+1)
    // 1:0% 2:10% 3:20% 4:35% 5:50% 6:75% 7:90% 8:95% 9:99% 10:100%
    private static readonly int[] DECAY = { 0, 0, 10, 20, 35, 50, 75, 90, 95, 99, 100 };

    void Awake()
    {
        if (!inventory) inventory = FindAnyObjectByType<InventorySystem>();
        if (!inventoryView) inventoryView = FindAnyObjectByType<InventoryView>();
        if (!playerStats) playerStats = FindAnyObjectByType<PlayerStats>();

        EnsureSlotDropCatcher(slot1, 0);
        EnsureSlotDropCatcher(slot2, 1);
        EnsureSlotDropCatcher(slot3, 2);

        if (mergeButton) mergeButton.onClick.AddListener(Merge);
        if (clearButton) clearButton.onClick.AddListener(ClearAll);

        EnsureSlotsActLikeDraggable();
        RefreshUI();
    }

    // ======== Public hook: call this when inventory UI closes ========
    public void OnInventoryClosed()
    {
        // Return any staged ingredients back to inventory and clear
        ReturnIngredientToInventory(ref _ingA);
        ReturnIngredientToInventory(ref _ingB);

        // (Optional) also clear any result in Slot3:
        // _lastOutput = null;
        // if (_slot3AsSlotUI) _slot3AsSlotUI.Bind(inventoryView, SlotGroup.Inactive, -1, null, 0);

        RefreshUI();
        if (inventoryView) inventoryView.RebuildUI();
        if (playerStats) playerStats.RecalculateStats();
    }

    // ===================== Merge (chance-gated) =====================
    public void Merge()
    {
        if (_ingA == null || _ingB == null || _ingA.item == null || _ingB.item == null)
        {
            Debug.Log("Merge failed: need two ingredients.");
            return;
        }

        float chance = ComputeSuccessChance(_ingA.item, _ingB.item);
        bool success = Random.Range(0f, 100f) <= chance;

        if (!success)
        {
            if (consumeOnFail)
            {
                _ingA = null;
                _ingB = null;
            }
            else
            {
                ReturnIngredientToInventory(ref _ingA);
                ReturnIngredientToInventory(ref _ingB);
            }

            _lastOutput = null;
            if (_slot3AsSlotUI) _slot3AsSlotUI.Bind(inventoryView, SlotGroup.Inactive, -1, null, 0);

            if (chanceText) chanceText.text = $"Success: {chance:0.#}% (Failed)";
            RefreshUI();
            if (inventoryView) inventoryView.RebuildUI();
            if (playerStats) playerStats.RecalculateStats();
            if (debugLog) Debug.Log($"[MergeStation] Merge FAILED at {chance:0.#}%");
            return;
        }

        // Success → create merged item (fallback icon)
        Sprite mergedIcon = placeholderIcon ? placeholderIcon : GeneratePlaceholderSprite(64, 64);

        var merged = ScriptableObject.CreateInstance<MergedItemSO>();
        merged.hideFlags = HideFlags.DontSave;

        string nameA = string.IsNullOrEmpty(_ingA.item.itemName) ? _ingA.item.name : _ingA.item.itemName;
        string nameB = string.IsNullOrEmpty(_ingB.item.itemName) ? _ingB.item.name : _ingB.item.itemName;

        merged.itemName = $"Merged: {nameA} + {nameB}";
        merged.description = $"Contains combined effects of {nameA} (x{_ingA.count}) and {nameB} (x{_ingB.count}).";
        merged.icon = mergedIcon;

        merged.sources = new List<MergedItemSO.SourceStack>
        {
            new MergedItemSO.SourceStack(_ingA.item, _ingA.count),
            new MergedItemSO.SourceStack(_ingB.item, _ingB.count)
        };

        _ingA = null;
        _ingB = null;

        // Show result in Slot3
        _lastOutput = merged;
        BindSlotAsItem(_slot3AsSlotUI, _lastOutput);

        if (chanceText) chanceText.text = $"Success: {chance:0.#}% (Success)";
        RefreshUI();
        if (inventoryView) inventoryView.RebuildUI();
        if (playerStats) playerStats.RecalculateStats();
        if (debugLog) Debug.Log($"[MergeStation] Merge SUCCESS at {chance:0.#}%");
    }

    public void ClearAll()
    {
        ReturnIngredientToInventory(ref _ingA);
        ReturnIngredientToInventory(ref _ingB);

        _lastOutput = null;
        if (_slot3AsSlotUI) _slot3AsSlotUI.Bind(inventoryView, SlotGroup.Inactive, -1, null, 0);

        RefreshUI();
        if (inventoryView) inventoryView.RebuildUI();
        if (playerStats) playerStats.RecalculateStats();
    }

    // ===================== Drops (consume on placement + support dragging between) =====================
    private void OnDropIntoSlot(int slotIndex, PointerEventData e)
    {
        var dragged = e.pointerDrag ? e.pointerDrag.GetComponentInParent<InventorySlotUI>() : null;
        if (dragged == null || dragged.item == null) return;

        // 1) If dragging the RESULT (slot3) into slot1/slot2 → move result to ingredient and clear slot3
        bool fromOutput = (dragged == _slot3AsSlotUI);
        if (fromOutput)
        {
            if (_lastOutput == null) return;

            ItemSO resultItem = _lastOutput;

            if (slotIndex == 0)
            {
                if (_ingA != null && _ingA.item != null && _ingA.item != resultItem)
                    ReturnIngredientToInventory(ref _ingA);
                _ingA = new IngredientStack(resultItem, 1);
            }
            else if (slotIndex == 1)
            {
                if (_ingB != null && _ingB.item != null && _ingB.item != resultItem)
                    ReturnIngredientToInventory(ref _ingB);
                _ingB = new IngredientStack(resultItem, 1);
            }

            _lastOutput = null;
            _slot3AsSlotUI.Bind(inventoryView, SlotGroup.Inactive, -1, null, 0);

            RefreshUI();
            if (inventoryView) inventoryView.RebuildUI();
            if (playerStats) playerStats.RecalculateStats();
            return;
        }

        // 2) If dragging FROM an ingredient slot TO the other ingredient slot → move it across
        bool fromIng1 = (dragged == _slot1AsSlotUI);
        bool fromIng2 = (dragged == _slot2AsSlotUI);
        if (fromIng1 || fromIng2)
        {
            var origin = fromIng1 ? ref _ingA : ref _ingB;
            var target = slotIndex == 0 ? ref _ingA : ref _ingB;

            // If dropping on same slot, do nothing
            if ((fromIng1 && slotIndex == 0) || (fromIng2 && slotIndex == 1))
                return;

            // If target occupied with different item, return it first
            if (target != null && target.item != null && target.item != origin.item)
                ReturnIngredientToInventory(ref target);

            // Move origin → target
            target = origin;
            origin = null;

            RefreshUI();
            if (inventoryView) inventoryView.RebuildUI();
            if (playerStats) playerStats.RecalculateStats();
            return;
        }

        // 3) Dragging from Inventory/Active into ingredient (consume)
        ItemSO item = dragged.item;
        int stackCount = Mathf.Max(1, dragged.count);

        if (slotIndex == 0 && _ingA != null && _ingA.item != null && _ingA.item != item)
            ReturnIngredientToInventory(ref _ingA);
        if (slotIndex == 1 && _ingB != null && _ingB.item != null && _ingB.item != item)
            ReturnIngredientToInventory(ref _ingB);

        // CONSUME from inventory immediately on placement
        ConsumeFromInventory(item, stackCount, dragged.group == SlotGroup.Active);

        if (slotIndex == 0)
        {
            if (_ingA != null && _ingA.item == item) _ingA.count += stackCount;
            else _ingA = new IngredientStack(item, stackCount);
        }
        else if (slotIndex == 1 || slotIndex == 2) // allow drop on Slot3 area to fill B
        {
            if (_ingB != null && _ingB.item == item) _ingB.count += stackCount;
            else _ingB = new IngredientStack(item, stackCount);
        }

        RefreshUI();
        if (inventoryView) inventoryView.RebuildUI();
        if (playerStats) playerStats.RecalculateStats();
    }

    // When end-dragging from any merge slot, if dropped outside merge panel → return to inventory and clear.
    private void HandleEndDragFromMergeSlot(InventorySlotUI originSlotUI, System.Func<IngredientStack> getter, System.Action clearAction, PointerEventData e)
    {
        var go = e.pointerCurrentRaycast.gameObject;
        bool droppedOnMerge =
            (go != null) &&
            (go.transform.IsChildOf(slot1) || go.transform.IsChildOf(slot2) || go.transform.IsChildOf(slot3) ||
             go.transform == slot1 || go.transform == slot2 || go.transform == slot3);

        if (!droppedOnMerge)
        {
            var stk = getter();
            if (stk != null && stk.item != null)
            {
                for (int i = 0; i < Mathf.Max(1, stk.count); i++)
                    inventory.AddItem(stk.item);
            }

            clearAction?.Invoke();

            RefreshUI();
            if (inventoryView) inventoryView.RebuildUI();
            if (playerStats) playerStats.RecalculateStats();
        }
    }

    // Called when dragging from Slot3 ends. If dropped OUTSIDE merge panel, add to inventory and clear.
    private void HandleEndDragFromResult(PointerEventData e)
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
                inventory.AddItem(_lastOutput);

            _lastOutput = null;
            if (_slot3AsSlotUI)
                _slot3AsSlotUI.Bind(inventoryView, SlotGroup.Inactive, -1, null, 0);

            RefreshUI();
            if (inventoryView) inventoryView.RebuildUI();
            if (playerStats) playerStats.RecalculateStats();
        }
        // else: if dropped on merge panel, OnDropIntoSlot handles moving into Slot1/Slot2.
    }

    // ===================== Inventory ops =====================
    private void ConsumeFromInventory(ItemSO item, int count, bool cameFromActive)
    {
        if (inventory == null || count <= 0 || item == null) return;

        int remaining = count;

        if (cameFromActive)
            remaining -= RemoveFromList(inventory.activeItems, item, remaining);

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

        // Output
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

        // Chance label
        if (chanceText)
        {
            if (_ingA != null && _ingB != null && _ingA.item != null && _ingB.item != null)
            {
                float chance = ComputeSuccessChance(_ingA.item, _ingB.item);
                chanceText.text = $"Success: {chance:0.#}%";
            }
            else chanceText.text = "Success: --";
        }

        // Re-bind the UI slot wrappers to reflect current contents (so dragging works)
        BindSlotAsItem(_slot1AsSlotUI, _ingA?.item);
        BindSlotAsItem(_slot2AsSlotUI, _ingB?.item);
        BindSlotAsItem(_slot3AsSlotUI, _lastOutput);

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

    private void EnsureSlotsActLikeDraggable()
    {
        // Slot1 as drag source (ingredient A)
        _slot1AsSlotUI = slot1.GetComponent<InventorySlotUI>();
        if (!_slot1AsSlotUI) _slot1AsSlotUI = slot1.gameObject.AddComponent<InventorySlotUI>();
        if (!_slot1AsSlotUI.canvasGroup)
            _slot1AsSlotUI.canvasGroup = slot1.GetComponent<CanvasGroup>() ?? slot1.gameObject.AddComponent<CanvasGroup>();
        if (slot1Icon) _slot1AsSlotUI.icon = slot1Icon;
        _slot1AsSlotUI.acceptDrops = false; // DO NOT let inv UI consume drops here
        _slot1AsSlotUI.Bind(inventoryView, SlotGroup.Inactive, -1, null, 0);

        // Right-click to return to inventory
        var click1 = slot1.GetComponent<RightClickReturn>();
        if (!click1) click1 = slot1.gameObject.AddComponent<RightClickReturn>();
        click1.onRightClick = () => { ReturnIngredientToInventory(ref _ingA); RefreshUI(); if (inventoryView) inventoryView.RebuildUI(); if (playerStats) playerStats.RecalculateStats(); };

        var endDrag1 = slot1.GetComponent<EndDragTracker>();
        if (!endDrag1) endDrag1 = slot1.gameObject.AddComponent<EndDragTracker>();
        endDrag1.init = () => HandleEndDragFromMergeSlot(_slot1AsSlotUI, () => _ingA, () => _ingA = null, endDrag1.lastEvent);
        endDrag1.ownerSlot = _slot1AsSlotUI;

        // Slot2 as drag source (ingredient B)
        _slot2AsSlotUI = slot2.GetComponent<InventorySlotUI>();
        if (!_slot2AsSlotUI) _slot2AsSlotUI = slot2.gameObject.AddComponent<InventorySlotUI>();
        if (!_slot2AsSlotUI.canvasGroup)
            _slot2AsSlotUI.canvasGroup = slot2.GetComponent<CanvasGroup>() ?? slot2.gameObject.AddComponent<CanvasGroup>();
        if (slot2Icon) _slot2AsSlotUI.icon = slot2Icon;
        _slot2AsSlotUI.acceptDrops = false; // same
        _slot2AsSlotUI.Bind(inventoryView, SlotGroup.Inactive, -1, null, 0);

        var click2 = slot2.GetComponent<RightClickReturn>();
        if (!click2) click2 = slot2.gameObject.AddComponent<RightClickReturn>();
        click2.onRightClick = () => { ReturnIngredientToInventory(ref _ingB); RefreshUI(); if (inventoryView) inventoryView.RebuildUI(); if (playerStats) playerStats.RecalculateStats(); };

        var endDrag2 = slot2.GetComponent<EndDragTracker>();
        if (!endDrag2) endDrag2 = slot2.gameObject.AddComponent<EndDragTracker>();
        endDrag2.init = () => HandleEndDragFromMergeSlot(_slot2AsSlotUI, () => _ingB, () => _ingB = null, endDrag2.lastEvent);
        endDrag2.ownerSlot = _slot2AsSlotUI;

        // Slot3 as drag source (result)
        _slot3AsSlotUI = slot3.GetComponent<InventorySlotUI>();
        if (!_slot3AsSlotUI) _slot3AsSlotUI = slot3.gameObject.AddComponent<InventorySlotUI>();
        if (!_slot3AsSlotUI.canvasGroup)
            _slot3AsSlotUI.canvasGroup = slot3.GetComponent<CanvasGroup>() ?? slot3.gameObject.AddComponent<CanvasGroup>();
        if (outputIcon) _slot3AsSlotUI.icon = outputIcon;
        _slot3AsSlotUI.acceptDrops = false; // result slot also shouldn't eat drops
        _slot3AsSlotUI.Bind(inventoryView, SlotGroup.Inactive, -1, null, 0);

        var endDrag3 = slot3.GetComponent<Slot3EndDragTracker>();
        if (!endDrag3) endDrag3 = slot3.gameObject.AddComponent<Slot3EndDragTracker>();
        endDrag3.owner = this;
    }

    private void BindSlotAsItem(InventorySlotUI slotUI, ItemSO item)
    {
        if (!slotUI || !inventoryView) return;
        // Count shows as 1 here (visual only)
        slotUI.Bind(inventoryView, SlotGroup.Inactive, -1, item, item ? 1 : 0);
    }

    // ===================== Chance math =====================
    private float ComputeSuccessChance(ItemSO a, ItemSO b)
    {
        float stabA = GetStability(a);
        float stabB = GetStability(b);

        float comp = ComputeCompatibilityPercent(a, b);
        float decay = 0.5f * (GetDecayPercentForDepth(GetMergeDepth(a)) + GetDecayPercentForDepth(GetMergeDepth(b)));

        float result = ((stabA + stabB) * 0.5f) - decay - (100f - comp);
        result = Mathf.Clamp(result, 0f, 100f);

        if (debugLog)
            Debug.Log($"[MergeStation] Chance calc: stabA={stabA}, stabB={stabB}, comp={comp}, decay={decay} => {result}%");
        return result;
    }

    private float GetStability(ItemSO item)
    {
        if (item is MergedItemSO m && m.sources != null && m.sources.Count > 0)
        {
            float total = 0f; int totalCount = 0;
            foreach (var s in m.sources)
            {
                if (s?.item == null || s.count <= 0) continue;
                float stab = GetStability(s.item);
                total += stab * s.count;
                totalCount += s.count;
            }
            return (totalCount > 0) ? total / totalCount : 100f;
        }

        var meta = FindMeta(item);
        return meta != null ? Mathf.Clamp(meta.stability, 0f, 100f) : 100f;
    }

    private int GetMergeDepth(ItemSO item)
    {
        if (item is MergedItemSO m && m.sources != null && m.sources.Count > 0)
        {
            int best = 0;
            foreach (var s in m.sources)
            {
                if (s?.item == null) continue;
                int d = GetMergeDepth(s.item);
                if (d > best) best = d;
            }
            return best + 1;
        }
        return 0;
    }

    private float GetDecayPercentForDepth(int depth)
    {
        int idx = Mathf.Clamp(depth + 1, 0, DECAY.Length - 1);
        return DECAY[idx];
    }

    private float ComputeCompatibilityPercent(ItemSO a, ItemSO b)
    {
        var setA = GetClassSet(a);
        var setB = GetClassSet(b);

        if (setA.Count == 0 || setB.Count == 0) return 100f;

        float sum = 0f; int n = 0;
        foreach (var ca in setA)
            foreach (var cb in setB)
            {
                sum += COMP[(int)ca, (int)cb];
                n++;
            }
        return (n > 0) ? (sum / n) : 100f;
    }

    private HashSet<InsectClass> GetClassSet(ItemSO item)
    {
        var result = new HashSet<InsectClass>();
        if (item is MergedItemSO m && m.sources != null && m.sources.Count > 0)
        {
            foreach (var s in m.sources)
            {
                if (s?.item == null) continue;
                foreach (var c in GetClassSet(s.item)) result.Add(c);
            }
        }
        else
        {
            var meta = FindMeta(item);
            result.Add(meta != null ? meta.classTag : InsectClass.Dancer);
        }
        return result;
    }

    private ItemMeta FindMeta(ItemSO item)
    {
        for (int i = 0; i < itemMeta.Count; i++)
            if (itemMeta[i].item == item) return itemMeta[i];
        return null;
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

    // Right-click utility to send ingredient back to inventory
    private class RightClickReturn : MonoBehaviour, IPointerClickHandler
    {
        public System.Action onRightClick;
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
                onRightClick?.Invoke();
        }
    }

    // For Slot1/Slot2 end-drag handling (return to inventory if dropped outside)
    private class EndDragTracker : MonoBehaviour, IEndDragHandler
    {
        public InventorySlotUI ownerSlot;
        public PointerEventData lastEvent;
        public System.Action init; // set by MergeStation to call with latest event
        public void OnEndDrag(PointerEventData eventData) { lastEvent = eventData; init?.Invoke(); }
    }

    private class Slot3EndDragTracker : MonoBehaviour, IEndDragHandler
    {
        public MergeStation owner;
        public void OnEndDrag(PointerEventData eventData) => owner?.HandleEndDragFromResult(eventData);
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
