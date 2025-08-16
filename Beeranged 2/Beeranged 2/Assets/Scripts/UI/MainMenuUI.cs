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
    public TextMeshProUGUI bestTimeText;         // "Best: 06:42"
    public TextMeshProUGUI leaderboardListText;  // multi-line top 10
    [Header("Title")]
    public TextMeshProUGUI titleText;            // game title (version appended here)

    [Header("Version")]
    public bool showVersionOnTitle = true;
    public string versionString = "v0.1";

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
            titleText.text = $"{baseTitle}  {versionString}";
        }

        RefreshLeaderboard();
    }

    // ---------- Buttons ----------
    public void OnPlayButton()
    {
        if (!string.IsNullOrEmpty(gameSceneName))
            SceneManager.LoadScene(gameSceneName);
        else
            Debug.LogWarning("MainMenuUI: gameSceneName is empty.");
    }

    public void OnOpenLeaderboard()
    {
        if (mainPanel) mainPanel.SetActive(false);
        if (leaderboardPanel) leaderboardPanel.SetActive(true);

        // Hide the title while viewing the leaderboard
        if (titleText) titleText.gameObject.SetActive(false);

        RefreshLeaderboard();
    }

    public void OnBackToMenu()
    {
        if (leaderboardPanel) leaderboardPanel.SetActive(false);
        if (mainPanel) mainPanel.SetActive(true);

        // Re-show the title
        if (titleText) titleText.gameObject.SetActive(true);
    }

    public void OnResetLeaderboard()
    {
        SaveService.ResetLeaderboard();
        RefreshLeaderboard();
    }

    // ---------- Helpers ----------
    private void RefreshLeaderboard()
    {
        // Best
        if (bestTimeText)
        {
            float best = SaveService.Data.bestTimeSeconds;
            bestTimeText.text = $"Best: {FormatTime(best)}";
        }

        // Top 10 list
        if (leaderboardListText)
        {
            List<SaveService.LeaderboardEntry> top = SaveService.GetTopEntries();
            if (top.Count == 0)
            {
                leaderboardListText.text = "No runs yet.";
            }
            else
            {
                var sb = new StringBuilder();
                for (int i = 0; i < top.Count; i++)
                {
                    var e = top[i];
                    sb.Append(i + 1).Append(". ")
                      .Append(FormatTime(e.seconds))
                      .Append(" — ")
                      .Append(string.IsNullOrEmpty(e.initials) ? "BRAZA" : e.initials)
                      .Append(" — ")
                      .Append(string.IsNullOrEmpty(e.date) ? "-" : e.date)
                      .AppendLine();
                }
                leaderboardListText.text = sb.ToString();
            }
        }
    }

    private string FormatTime(float seconds)
    {
        int t = Mathf.FloorToInt(seconds);
        int m = t / 60;
        int s = t % 60;
        return $"{m:00}:{s:00}";
    }
}
