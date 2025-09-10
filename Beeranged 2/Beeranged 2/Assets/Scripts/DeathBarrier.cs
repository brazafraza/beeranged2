using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathBarrier : MonoBehaviour
{
    public int dotValue;
    public Transform tpDest;

    public enum BarrierType
    {
        Death,
        DOT,
        Teleport
    }

    public BarrierType barrierType;

    private void Awake()
    {
        // Auto-fetch tpDest from GameManager
        if (GameManager.Instance != null)
        {
            tpDest = GameManager.Instance.tpDest;
        }

        if (tpDest == null && barrierType == BarrierType.Teleport)
        {
           // Debug.LogWarning("tpDest was not set from GameManager.");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        Debug.Log("Hit barrier: " + barrierType);

        PlayerController player = collision.GetComponent<PlayerController>();
        if (player == null)
        {
            Debug.LogWarning("PlayerController component not found on Player.");
            return;
        }

        switch (barrierType)
        {
            case BarrierType.Death:
                TeleportToLastJumpPoint(player);
                break;

            case BarrierType.DOT:
                Debug.Log("DOT hit: Value = " + dotValue);
                // TODO: Add DOT system here
                break;

            case BarrierType.Teleport:
                TeleportToTarget(player);
                break;
        }
    }

    void TeleportToLastJumpPoint(PlayerController player)
    {
        Transform target = player.lastJumpPoint != null
            ? player.lastJumpPoint
            : player.fallbackPoint;

        if (target != null)
        {
            player.transform.position = target.position;
            player.rb.velocity = Vector2.zero;
            Debug.Log("Teleported to last jump point.");
        }
        else
        {
            Debug.LogWarning("No jump or fallback point set on player.");
        }
    }

    void TeleportToTarget(PlayerController player)
    {
        Transform globalDest = GameManager.Instance != null ? GameManager.Instance.tpDest : null;

        if (globalDest != null)
        {
            player.transform.position = globalDest.position;
            player.rb.velocity = Vector2.zero;
            Debug.Log("Teleported to GameManager.tpDest.");
        }
        else
        {
            Debug.LogWarning("Teleport destination (tpDest) is not assigned.");
        }
    }

}
