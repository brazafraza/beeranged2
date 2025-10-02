using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameClock : MonoBehaviour
{
    [Header("UI (optional)")]
    public TextMeshProUGUI timeText;     // assign a UI Text, or leave null
    public bool autoStart = true;

    public float ElapsedSeconds { get; private set; }
    public bool IsRunning { get; private set; }

    public event Action<int> OnMinuteChanged; // passes whole minutes elapsed

    private int _lastMinute = -1;

    void Start()
    {
        IsRunning = autoStart;
        ElapsedSeconds = 0f;
        _lastMinute = -1;
        UpdateUI();
    }

    void Update()
    {
        if (!IsRunning) return;

        // Use scaled time so clock pauses with timeScale == 0 (game paused)
        float dt = Time.deltaTime;
        if (dt <= 0f) return; // paused or no frame progress

        ElapsedSeconds += dt;

        int minute = Mathf.FloorToInt(ElapsedSeconds / 60f);
        if (minute != _lastMinute)
        {
            _lastMinute = minute;
            OnMinuteChanged?.Invoke(minute);
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (timeText == null) return;
        int total = Mathf.FloorToInt(ElapsedSeconds);
        int m = total / 60;
        int s = total % 60;
        timeText.text = $"{m:00}:{s:00}";
    }

    public void Pause() { IsRunning = false; }
    public void Resume() { IsRunning = true; }

    public void ResetClock()
    {
        ElapsedSeconds = 0f;
        _lastMinute = -1;
        UpdateUI();
    }
}
