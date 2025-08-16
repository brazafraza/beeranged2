using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [Header("Refs")]
    public GameClock clock;                 // drag your run timer
    public TMP_InputField initialsInput;    // user enters initials (max 5 letters)
    public TextMeshProUGUI timeText;        // shows final time (MM:SS)

    [Header("Flow")]
    public string menuSceneName = "MainMenu"; // scene to load after submit
    public bool autoFocusInitials = true;

    private float _finalSeconds;

    private void OnEnable()
    {
        if (!clock) clock = FindObjectOfType<GameClock>();
        _finalSeconds = clock ? clock.ElapsedSeconds : 0f;

        if (timeText) timeText.text = FormatTime(_finalSeconds);

        // Prefill initials from last used (or BRAZA)
        string prefill = SaveService.Data.lastInitials;
        if (string.IsNullOrEmpty(prefill)) prefill = "BRAZA";
        if (initialsInput)
        {
            initialsInput.text = prefill;
            if (autoFocusInitials) initialsInput.Select();
        }
    }

    public void OnSubmitScore()
    {
        string raw = initialsInput ? initialsInput.text : "BRAZA";
        string clean = SaveService.SanitizeInitials(raw);
        SaveService.TryUpdateBestTime(_finalSeconds, clean);

        // Go back to menu
        if (!string.IsNullOrEmpty(menuSceneName))
            SceneManager.LoadScene(menuSceneName);
    }

    // Optional: live sanitize as user types (hook to InputField OnValueChanged)
    public void OnInitialsChanged(string _)
    {
        if (!initialsInput) return;
        string clean = SaveService.SanitizeInitials(initialsInput.text);
        if (clean != initialsInput.text)
        {
            int caret = Mathf.Min(clean.Length, 5);
            initialsInput.text = clean;
            initialsInput.caretPosition = caret;
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
