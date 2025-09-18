using System;
using UnityEngine;

[DefaultExecutionOrder(-100)] // initialize early so others can query states safely
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    // -------- Public Query --------
    public static bool IsPaused => Instance && (Instance._hardCount > 0 || Instance._softCount > 0);
    public static bool IsHardPaused => Instance && Instance._hardCount > 0;
    public static bool IsSoftPaused => Instance && Instance._softCount > 0;

    // -------- UI (optional) --------
    [Header("UI (optional)")]
    [Tooltip("Shown while HARD paused.")]
    public GameObject hardPauseUI;
    [Tooltip("Shown while SOFT paused (hidden if hard is active).")]
    public GameObject softPauseUI;

    // -------- Input --------
    [Header("Input")]
    [Tooltip("Press to toggle HARD pause (menu).")]
    public KeyCode toggleHardKey = KeyCode.Escape;

    // -------- Time scales --------
    [Header("Time Scales")]
    [Tooltip("Time.timeScale while HARD paused (usually 0).")]
    public float hardTimeScale = 0f;
    [Tooltip("Time.timeScale while SOFT paused (default 1 so gameplay still runs; set to 0 if you want soft to also freeze).")]
    public float softTimeScale = 1f;

    // -------- Audio --------
    [Header("Audio")]
    [Tooltip("Pause all audio while HARD paused.")]
    public bool pauseAudioOnHard = true;
    [Tooltip("Pause all audio while SOFT paused.")]
    public bool pauseAudioOnSoft = false;

    // -------- Cursor --------
    [Header("Cursor")]
    [Tooltip("Cursor state while HARD paused.")]
    public bool showCursorOnHard = true;
    public CursorLockMode hardLockState = CursorLockMode.None;

    [Tooltip("Cursor state while SOFT paused.")]
    public bool showCursorOnSoft = true;
    public CursorLockMode softLockState = CursorLockMode.None;

    [Tooltip("Cursor state when fully resumed.")]
    public CursorLockMode resumedLockState = CursorLockMode.Locked;
    [Tooltip("Show cursor when fully resumed?")]
    public bool showCursorOnResume = false;

    // -------- Events --------
    public static event Action OnPaused;       // fired when transitioning from unpaused -> paused (any kind)
    public static event Action OnResumed;      // fired when transitioning from paused (any) -> unpaused
    public static event Action OnHardPaused;
    public static event Action OnHardResumed;
    public static event Action OnSoftPaused;
    public static event Action OnSoftResumed;

    // -------- Internal --------
    int _hardCount = 0;
    int _softCount = 0;
    float _prevTimeScale = 1f;
    bool _wasAnyPaused = false;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _prevTimeScale = Mathf.Approximately(Time.timeScale, 0f) ? 1f : Time.timeScale;
        if (hardPauseUI) hardPauseUI.SetActive(false);
        if (softPauseUI) softPauseUI.SetActive(false);
        AudioListener.pause = false;
        ApplyState(force: true);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleHardKey))
        {
            if (IsHardPaused) PopHardPause();
            else PushHardPause();
        }
    }

    // -------- Public API: HARD --------
    public static void PushHardPause() => Instance?._PushHardPause();
    public static void PopHardPause() => Instance?._PopHardPause();
    public static void SetHardPaused(bool value)
    {
        if (!Instance) return;
        if (value && Instance._hardCount == 0) Instance._PushHardPause();
        else if (!value && Instance._hardCount > 0) Instance._PopHardPause();
    }

    // -------- Public API: SOFT --------
    public static void PushSoftPause() => Instance?._PushSoftPause();
    public static void PopSoftPause() => Instance?._PopSoftPause();
    public static void SetSoftPaused(bool value)
    {
        if (!Instance) return;
        if (value && Instance._softCount == 0) Instance._PushSoftPause();
        else if (!value && Instance._softCount > 0) Instance._PopSoftPause();
    }

    // -------- Internal Mutators --------
    void _PushHardPause()
    {
        bool wasPaused = IsPaused;
        _hardCount++;
        OnHardPaused?.Invoke();
        if (!wasPaused) OnPaused?.Invoke();
        ApplyState();
    }

    void _PopHardPause()
    {
        if (_hardCount <= 0) return;
        _hardCount--;
        OnHardResumed?.Invoke();
        if (!IsPaused) OnResumed?.Invoke();
        ApplyState();
    }

    void _PushSoftPause()
    {
        bool wasPaused = IsPaused;
        _softCount++;
        OnSoftPaused?.Invoke();
        if (!wasPaused) OnPaused?.Invoke();
        ApplyState();
    }

    void _PopSoftPause()
    {
        if (_softCount <= 0) return;
        _softCount--;
        OnSoftResumed?.Invoke();
        if (!IsPaused) OnResumed?.Invoke();
        ApplyState();
    }

    // -------- Apply State --------
    void ApplyState(bool force = false)
    {
        bool anyPaused = IsPaused;
        bool hard = IsHardPaused;
        bool soft = IsSoftPaused;

        // Time: store previous scale on transition into paused; restore on full resume
        if (anyPaused && (!_wasAnyPaused || force))
        {
            _prevTimeScale = (Mathf.Approximately(Time.timeScale, 0f) ? 1f : Time.timeScale);
        }

        float targetScale;
        if (!anyPaused)
        {
            targetScale = (_prevTimeScale <= 0f) ? 1f : _prevTimeScale;
        }
        else
        {
            targetScale = hard ? hardTimeScale : softTimeScale;
        }

        if (!Mathf.Approximately(Time.timeScale, targetScale))
            Time.timeScale = targetScale;

        // Audio: hard wins over soft
        bool wantAudioPause = (hard && pauseAudioOnHard) || (!hard && soft && pauseAudioOnSoft);
        AudioListener.pause = wantAudioPause;

        // UI
        if (hardPauseUI) hardPauseUI.SetActive(hard);
        if (softPauseUI) softPauseUI.SetActive(soft && !hard);

        // Cursor: hard policy overrides soft
        if (anyPaused)
        {
            if (hard)
            {
                Cursor.lockState = hardLockState;
                Cursor.visible = showCursorOnHard;
            }
            else
            {
                Cursor.lockState = softLockState;
                Cursor.visible = showCursorOnSoft;
            }
        }
        else
        {
            Cursor.lockState = resumedLockState;
            Cursor.visible = showCursorOnResume;
        }

        _wasAnyPaused = anyPaused;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (_hardCount < 0) _hardCount = 0;
        if (_softCount < 0) _softCount = 0;
    }
#endif
}
