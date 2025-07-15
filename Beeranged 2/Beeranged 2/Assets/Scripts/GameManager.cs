using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public int money;
    public float xpCurrent = 5;
    public float xpToLevelUp = 10;

    public UIManager uiManager;


    private void Start()
    {
        uiManager = FindAnyObjectByType<UIManager>(); 
    }

    public void UpdateUI()
    {
        uiManager.UpdateUI();
    }
}
