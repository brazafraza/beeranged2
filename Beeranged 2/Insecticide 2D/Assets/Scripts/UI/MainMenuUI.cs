using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Text;
using System.Collections.Generic;

public class MainMenuUI : MonoBehaviour
{
    [Header("Scenes")]
    public string gameSceneName = "Game";

    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject leaderboardPanel;

    [Header("Leaderboard UI")]
    public TextMeshProUGUI bestTimeText;         
    public TextMeshProUGUI leaderboardListText;  // multi-line top 10
    [Header("Title")]
    public TextMeshProUGUI titleText;           

    [Header("Version")]
    public bool showVersionOnTitle = true;
    public string versionString = "v2.0";

    private void Awake()
    {
        if (mainPanel) mainPanel.SetActive(true);
        if (leaderboardPanel) leaderboardPanel.SetActive(false);
    }

    private void Start()
    {
        // Put version on the TITLE only
        if (showVersionOnTitle && titleText != null)
        {
            string baseTitle = string.IsNullOrEmpty(titleText.text) ? "Beeranged" : titleText.text;
            titleText.text = $"{baseTitle} {versionString}";
        }

        
    }

    // ---------- Buttons ----------
    public void OnPlayButton()
    {
        if (!string.IsNullOrEmpty(gameSceneName))
            SceneManager.LoadScene(gameSceneName);
        else
            Debug.LogWarning("MainMenuUI: gameSceneName is empty.");
    }

  

    public void OnBackToMenu()
    {
        if (leaderboardPanel) leaderboardPanel.SetActive(false);
        if (mainPanel) mainPanel.SetActive(true);

        // Re-show the title
        if (titleText) titleText.gameObject.SetActive(true);
    }

  

    private string FormatTime(float seconds)
    {
        int t = Mathf.FloorToInt(seconds);
        int m = t / 60;
        int s = t % 60;
        return $"{m:00}:{s:00}";
    }
}
