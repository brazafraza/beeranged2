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
    [Tooltip("If true, skip fancy merging and always use a placeholder icon (auto tinted by quarters).")]
    public bool forcePlaceholderIcon = true;  // default ON to avoid blanks
    public Sprite placeholderIcon;            // optional; if null, a generated checker is used (then tinted by quarters)

    [Header("Behavior")]
    [Tooltip("Pick which icon to use if icon slicing fails or sprites are missing (when not forcing placeholder).")]
    public IconFallback mergedIconFallback = IconFallback.UseFirstNonNull;
    public enum IconFallback { UseFirstNonNull, UseSecondNonNull, None }

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
    static Material _blitMat;

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

        // Decide icon
        Sprite mergedIcon = null;

        if (forcePlaceholderIcon)
        {
            var basePh = placeholderIcon ? placeholderIcon : GeneratePlaceholderSprite(64, 64);
            mergedIcon = CreateQuarterTintedIconGPU(basePh);
        }
        else
        {
            mergedIcon = CreateMergedIcon(_ingA.item.icon, _ingB.item.icon);
            if (!mergedIcon)
            {
                switch (mergedIconFallback)
                {
                    case IconFallback.UseSecondNonNull: mergedIcon = _ingB.item?.icon; break;
                    case IconFallback.UseFirstNonNull:
                    default: mergedIcon = _ingA.item?.icon ? _ingA.item.icon : _ingB.item?.icon; break;
                    case IconFallback.None: mergedIcon = null; break;
                }
                if (!mergedIcon)
                {
                    var basePh = placeholderIcon ? placeholderIcon : GeneratePlaceholderSprite(64, 64);
                    mergedIcon = CreateQuarterTintedIconGPU(basePh);
                }
            }
        }

        // Create merged runtime item
        var merged = ScriptableObject.CreateInstance<MergedItemSO>();
        merged.hideFlags = HideFlags.DontSave;

        string nameA = string.IsNullOrEmpty(_ingA.item.itemName) ? _ingA.item.name : _ingA.item.itemName;
        string nameB = string.IsNullOrEmpty(_ingB.item.itemName) ? _ingB.item.name : _ingB.item.itemName;

        merged.itemName = $"Merged: {nameA} + {nameB}";
        merged.description = $"Contains combined effects of {nameA} (x{_ingA.count}) and {nameB} (x{_ingB.count}).";
        merged.icon = mergedIcon ?? (placeholderIcon ? placeholderIcon : GeneratePlaceholderSprite(64, 64)); // <- GUARANTEE not null
        if (debugLog) Debug.Log($"[MergeStation] Merged icon set? {(merged.icon != null)}  size: {(merged.icon != null ? $"{merged.icon.texture.width}x{merged.icon.texture.height}" : "null")}");

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
                inventory.AddItem(_lastOutput); // Inventory UI will pick up item.icon

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
            slot1Icon.color = Color.white; // ensure visible
            slot1Icon.enabled = _ingA != null && _ingA.item != null && _ingA.item.icon != null;
            slot1Icon.sprite = (_ingA != null && _ingA.item != null) ? _ingA.item.icon : null;
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
            slot2Icon.color = Color.white; // ensure visible
            slot2Icon.enabled = _ingB != null && _ingB.item != null && _ingB.item.icon != null;
            slot2Icon.sprite = (_ingB != null && _ingB.item != null) ? _ingB.item.icon : null;
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
            outputIcon.color = Color.white; // ensure visible
            outputIcon.enabled = _lastOutput != null && _lastOutput.icon != null;
            outputIcon.sprite = _lastOutput ? (_lastOutput.icon ?? (placeholderIcon ? placeholderIcon : GeneratePlaceholderSprite(64, 64))) : null;
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

    // ===================== Icon merge =====================
    // Standard split (A left, B right) when not forcing placeholder
    private Sprite CreateMergedIcon(Sprite a, Sprite b)
    {
        if (!a && !b) return null;

        int outW, outH;
        GetTargetSize(a, b, out outW, out outH);
        outW = Mathf.Max(4, outW);
        outH = Mathf.Max(4, outH);

        bool aReadable = IsSpriteReadable(a);
        bool bReadable = IsSpriteReadable(b);

        if (aReadable || bReadable)
        {
            var tex = new Texture2D(outW, outH, TextureFormat.RGBA32, false, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            for (int y = 0; y < outH; y++)
            {
                float v = outH > 1 ? (y / (float)(outH - 1)) : 0f;
                for (int x = 0; x < outW; x++)
                {
                    bool leftSide = (x < outW / 2);
                    float uLocal = leftSide
                        ? (outW > 2 ? (x / (float)((outW / 2) - 1)).Clamp01() : 0f)
                        : (outW > 2 ? ((x - outW / 2) / (float)(outW - (outW / 2) - 1)).Clamp01() : 0f);

                    Color c = Color.clear;
                    if (leftSide)
                    {
                        if (a && aReadable) c = SampleSpriteBilinear(a, uLocal, v);
                        else if (b && bReadable) c = SampleSpriteBilinear(b, uLocal, v);
                    }
                    else
                    {
                        if (b && bReadable) c = SampleSpriteBilinear(b, uLocal, v);
                        else if (a && aReadable) c = SampleSpriteBilinear(a, uLocal, v);
                    }
                    tex.SetPixel(x, outH - 1 - y, c);
                }
            }
            tex.Apply(false, false);

            var sprite = Sprite.Create(tex, new Rect(0, 0, outW, outH), new Vector2(0.5f, 0.5f), GuessPPU(a, b));
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        return CreateMergedIconGPU(a, b, outW, outH);
    }

    private Sprite CreateMergedIconGPU(Sprite a, Sprite b, int w, int h)
    {
        if (!a && !b) return null;

        var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        var prevRT = RenderTexture.active;
        RenderTexture.active = rt;

        GL.PushMatrix();
        GL.LoadPixelMatrix(0, w, 0, h);
        GL.Clear(true, true, new Color(0, 0, 0, 0));

        if (a) DrawSpriteToRectTinted(a, new Rect(0, 0, w / 2f, h), Color.white);
        if (b) DrawSpriteToRectTinted(b, new Rect(w / 2f, 0, w - w / 2f, h), Color.white);

        GL.PopMatrix();

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false) { hideFlags = HideFlags.DontSave };
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
        tex.Apply(false, false);

        RenderTexture.active = prevRT;
        RenderTexture.ReleaseTemporary(rt);

        var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), GuessPPU(a, b));
        sprite.hideFlags = HideFlags.DontSave;
        return sprite;
    }

    // Placeholder ? quarter tinted (TL, TR, BL, BR each random color), GPU path (no R/W needed)
    private Sprite CreateQuarterTintedIconGPU(Sprite src)
    {
        int w = src ? Mathf.RoundToInt(src.rect.width) : 64;
        int h = src ? Mathf.RoundToInt(src.rect.height) : 64;
        float ppu = src ? src.pixelsPerUnit : 100f;

        var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        var prevRT = RenderTexture.active;
        RenderTexture.active = rt;

        GL.PushMatrix();
        GL.LoadPixelMatrix(0, w, 0, h);
        GL.Clear(true, true, new Color(0, 0, 0, 0));

        Color TL = RandomColor();
        Color TR = RandomColor();
        Color BL = RandomColor();
        Color BR = RandomColor();

        DrawSpriteToRectTinted(src, new Rect(0, h / 2f, w / 2f, h / 2f), TL);
        DrawSpriteToRectTinted(src, new Rect(w / 2f, h / 2f, w / 2f, h / 2f), TR);
        DrawSpriteToRectTinted(src, new Rect(0, 0, w / 2f, h / 2f), BL);
        DrawSpriteToRectTinted(src, new Rect(w / 2f, 0, w / 2f, h / 2f), BR);

        GL.PopMatrix();

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false) { hideFlags = HideFlags.DontSave };
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
        tex.Apply(false, false);

        RenderTexture.active = prevRT;
        RenderTexture.ReleaseTemporary(rt);

        var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), ppu);
        sprite.hideFlags = HideFlags.DontSave;
        return sprite;
    }

    private static void DrawSpriteToRectTinted(Sprite s, Rect dest, Color tint)
    {
        if (!s || !s.texture) return;
        Rect tr = s.textureRect;
        Vector2 uv0 = new Vector2(tr.xMin / s.texture.width, tr.yMin / s.texture.height);
        Vector2 uv1 = new Vector2(tr.xMax / s.texture.width, tr.yMax / s.texture.height);

        var mat = GetBlitMat();
        mat.mainTexture = s.texture;
        mat.color = tint; // multiply color
        mat.SetPass(0);

        GL.Begin(GL.QUADS);
        GL.TexCoord2(uv0.x, uv0.y); GL.Vertex3(dest.xMin, dest.yMin, 0);
        GL.TexCoord2(uv1.x, uv0.y); GL.Vertex3(dest.xMax, dest.yMin, 0);
        GL.TexCoord2(uv1.x, uv1.y); GL.Vertex3(dest.xMax, dest.yMax, 0);
        GL.TexCoord2(uv0.x, uv1.y); GL.Vertex3(dest.xMin, dest.yMax, 0);
        GL.End();
    }

    private static Color RandomColor()
    {
        Color c = Color.HSVToRGB(Random.value, Random.Range(0.65f, 1f), Random.Range(0.8f, 1f));
        c.a = 1f;
        return c;
    }

    private static void GetTargetSize(Sprite a, Sprite b, out int w, out int h)
    {
        int wa = a ? Mathf.RoundToInt(a.rect.width) : 0;
        int ha = a ? Mathf.RoundToInt(a.rect.height) : 0;
        int wb = b ? Mathf.RoundToInt(b.rect.width) : 0;
        int hb = b ? Mathf.RoundToInt(b.rect.height) : 0;
        w = Mathf.Max(wa, wb);
        h = Mathf.Max(ha, hb);
        if (w == 0) w = 64;
        if (h == 0) h = 64;
    }

    private static float GuessPPU(Sprite a, Sprite b)
    {
        if (a && a.pixelsPerUnit > 0f) return a.pixelsPerUnit;
        if (b && b.pixelsPerUnit > 0f) return b.pixelsPerUnit;
        return 100f;
    }

    private static bool IsSpriteReadable(Sprite s)
    {
        if (!s || !s.texture) return false;
        try { s.texture.GetPixelBilinear(0f, 0f); return true; }
        catch { return false; }
    }

    private static Color SampleSpriteBilinear(Sprite s, float u01, float v01)
    {
        if (!s) return Color.clear;
        var tex = s.texture;
        var r = s.textureRect;
        float u = (r.x + u01 * r.width) / tex.width;
        float v = (r.y + v01 * r.height) / tex.height;
        return tex.GetPixelBilinear(u, v);
    }

    private Sprite GeneratePlaceholderSprite(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
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

    private static Material GetBlitMat()
    {
        if (_blitMat != null) return _blitMat;
        var sh = Shader.Find("Unlit/Texture");
        _blitMat = new Material(sh) { hideFlags = HideFlags.DontSave };
        _blitMat.color = Color.white;
        return _blitMat;
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
