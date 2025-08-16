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

    private ItemSO[] _options;
    private Action<ItemSO> _onChoose;

    void Awake()
    {
        Hide();

        if (optionButton1) optionButton1.onClick.AddListener(() => Choose(0));
        if (optionButton2) optionButton2.onClick.AddListener(() => Choose(1));
        if (optionButton3) optionButton3.onClick.AddListener(() => Choose(2));
    }

    public void Show(ItemSO[] options, Action<ItemSO> onChoose)
    {
        _options = options;
        _onChoose = onChoose;

        SetupOption(0, options);
        SetupOption(1, options);
        SetupOption(2, options);

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
    }

    private void SetupOption(int i, ItemSO[] options)
    {
        ItemSO item = (options != null && i < options.Length) ? options[i] : null;

        var btn = i == 0 ? optionButton1 : i == 1 ? optionButton2 : optionButton3;
        var icon = i == 0 ? optionIcon1 : i == 1 ? optionIcon2 : optionIcon3;
        var name = i == 0 ? optionName1 : i == 1 ? optionName2 : optionName3;
        var desc = i == 0 ? optionDesc1 : i == 1 ? optionDesc2 : optionDesc3;

        if (btn) btn.interactable = item != null;
        if (icon) icon.sprite = item ? item.icon : null;
        if (icon) icon.enabled = item != null;
        if (name) name.text = item ? item.displayName : "--";
        if (desc) desc.text = item ? item.description : "";
    }
}
