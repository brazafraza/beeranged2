using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Image fillbar;
    public GameManager gm;

    private void Start()
    {
        gm = FindAnyObjectByType<GameManager>();
        
    }

    private void FixedUpdate()
    {
        UpdateUI();
    }


    public void UpdateUI()
    {
        fillbar.fillAmount = gm.xpCurrent / gm.xpToLevelUp;
    }
}
