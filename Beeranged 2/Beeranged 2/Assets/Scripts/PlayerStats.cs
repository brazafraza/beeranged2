using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public float health = 100;
    public float maxHealth = 100;


    private TextMeshProUGUI healthText;



    private void Start()
    {
        GameObject go;
        go = GameObject.Find("Health");
        healthText = go.GetComponent<TextMeshProUGUI>();

        health = maxHealth;
        UpdateUI();
    }

    private void Update()
    {
        
    }

    public void UpdateUI()
    {
        healthText.text = $"{health} / {maxHealth}";
        //Debug.Log("PlayerStats: Health Updated");
    }

    
}
