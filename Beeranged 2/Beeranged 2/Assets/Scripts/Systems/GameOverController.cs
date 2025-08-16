using UnityEngine;

public class GameOverController : MonoBehaviour
{
    [Header("References")]
    public PlayerStats player;             // drag your PlayerStats (auto-finds if left empty)
    public GameObject gameOverPanelRoot;   // the whole Game Over panel root (starts INACTIVE)

    [Header("On Death Options")]
    public bool pauseOnDeath = true;
    [Range(0f, 1f)] public float pauseTimescale = 0f;  // usually 0
    public MonoBehaviour[] disableOnDeath;             // optional: scripts to disable (e.g., PlayerController, AutoShooter)
    public GameObject[] hideOnDeath;                   // optional: HUD/joystick roots to hide

    private bool _shown;

    private void Awake()
    {
        if (!player) player = FindObjectOfType<PlayerStats>();
        if (gameOverPanelRoot) gameOverPanelRoot.SetActive(false);
    }

    private void OnEnable()
    {
        if (player != null) player.OnDied += HandlePlayerDied;
    }

    private void OnDisable()
    {
        if (player != null) player.OnDied -= HandlePlayerDied;
    }

    private void HandlePlayerDied()
    {
        if (_shown) return;
        _shown = true;

        // Optional: disable gameplay scripts
        if (disableOnDeath != null)
        {
            for (int i = 0; i < disableOnDeath.Length; i++)
            {
                if (disableOnDeath[i] != null) disableOnDeath[i].enabled = false;
            }
        }

        // Optional: hide HUD/joystick/etc
        if (hideOnDeath != null)
        {
            for (int i = 0; i < hideOnDeath.Length; i++)
            {
                if (hideOnDeath[i] != null) hideOnDeath[i].SetActive(false);
            }
        }

        // Pause game
        if (pauseOnDeath) Time.timeScale = pauseTimescale;

        // Show game over panel (GameOverUI.OnEnable will populate time & initials)
        if (gameOverPanelRoot) gameOverPanelRoot.SetActive(true);
    }

    // Handy for testing in the editor (right-click the component header ? "Test Game Over")
    [ContextMenu("Test Game Over")]
    private void TestGameOver()
    {
        HandlePlayerDied();
    }
}
