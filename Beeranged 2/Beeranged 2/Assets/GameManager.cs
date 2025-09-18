using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public Transform tpDest;

    public GameObject inventoryRoot;
    public bool inventoryOpen = false;
    public GameObject blockint;

    void Awake()
    {
        // Singleton pattern (optional)
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (!inventoryOpen)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                blockint.SetActive(false);
                inventoryOpen = true;
                inventoryRoot.SetActive(true);
                PauseManager.SetSoftPaused(true);
            }
        }
        else if (inventoryOpen)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                blockint.SetActive(true);
                inventoryOpen = false;
                inventoryRoot.SetActive(false);
                PauseManager.SetSoftPaused(false);
            }
        }

        }
}
