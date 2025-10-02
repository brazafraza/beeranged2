using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelUpPanel : MonoBehaviour
{
    [Header("Panel Root")]
    public GameObject root;

    [Header("Option 1")]
    public Button optionButton1;
    public Image optionIcon1;
    public TextMeshProUGUI optionName1;
    public TextMeshProUGUI optionDesc1;

    [Header("Option 2")]
    public Button optionButton2;
    public Image optionIcon2;
    public TextMeshProUGUI optionName2;
    public TextMeshProUGUI optionDesc2;

    [Header("Option 3")]
    public Button optionButton3;
    public Image optionIcon3;
    public TextMeshProUGUI optionName3;
    public TextMeshProUGUI optionDesc3;

    [Header("Behavior")]
    [Tooltip("If true, panel hides as soon as an option is clicked.")]
    public bool autoHideOnPick = true;

    private ItemSO[] _options;
    private Action<ItemSO> _onChoose;

    void Awake()
    {
        Hide();

        if (optionButton1) optionButton1.onClick.AddListener(() => Choose(0));
        if (optionButton2) optionButton2.onClick.AddListener(() => Choose(1));
        if (optionButton3) optionButton3.onClick.AddListener(() => Choose(2));
    }

    public void SetAutoHide(bool value) => autoHideOnPick = value;

    public void Show(ItemSO[] options, Action<ItemSO> onChoose)
    {
        _options = options;
        _onChoose = onChoose;

        SetupOption(0, (options != null && options.Length > 0) ? options[0] : null);
        SetupOption(1, (options != null && options.Length > 1) ? options[1] : null);
        SetupOption(2, (options != null && options.Length > 2) ? options[2] : null);

        if (root) root.SetActive(true);
        else gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (root) root.SetActive(false);
        else gameObject.SetActive(false);
    }

    private void Choose(int i)
    {
        ItemSO pick = null;
        if (_options != null && i >= 0 && i < _options.Length)
            pick = _options[i];

        _onChoose?.Invoke(pick);

        if (autoHideOnPick)
            Hide();
    }

    private void SetupOption(int index, ItemSO item)
    {
        Button btn = index == 0 ? optionButton1 : index == 1 ? optionButton2 : optionButton3;
        Image icon = index == 0 ? optionIcon1 : index == 1 ? optionIcon2 : optionIcon3;
        TextMeshProUGUI name = index == 0 ? optionName1 : index == 1 ? optionName2 : optionName3;
        TextMeshProUGUI desc = index == 0 ? optionDesc1 : index == 1 ? optionDesc2 : optionDesc3;

        bool hasItem = item != null;

        if (btn)
        {
            btn.gameObject.SetActive(hasItem);
            btn.interactable = hasItem;
        }

        if (icon)
        {
            icon.sprite = hasItem ? item.icon : null;
            icon.enabled = hasItem && item.icon != null;
        }

        if (name) name.text = hasItem ? item.itemName : "--";
        if (desc) desc.text = hasItem ? item.description : "";
    }
}
