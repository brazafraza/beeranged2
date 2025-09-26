using UnityEngine;

public class XPGem : MonoBehaviour
{
    public int xpValue = 1;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        //add logic here
  
        else
            Destroy(gameObject);
    }
}
