using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Secondary/Split Spread Shot")]
public class SecondarySplitSpreadSO : SecondaryAbilitySO
{
    [Header("Projectile")]
    [Tooltip("Projectile prefab that will travel forward and then split.\n" +
             "Should have Bullet + SplitAfterDistance components.")]
    public GameObject splitProjectilePrefab;

    [Header("Shot Stats")]
    public float speed = 14f;
    public int damage = 5;

    [Header("Split Behaviour")]
    [Tooltip("Distance travelled before splitting into 3 bullets.")]
    public float splitDistance = 4f;

    [Tooltip("Angle (in degrees) for the side bullets relative to the middle one.")]
    public float spreadAngleDeg = 15f;

    [Tooltip("How fast the child bullets move compared to the original.")]
    public float childSpeedMultiplier = 1f;

    [Tooltip("Prefab used for the 3 child bullets. If null, uses the same prefab as the main.")]
    public GameObject childProjectilePrefab;

    public override void UseSecondary(PlayerAttack ctx)
    {
        if (!splitProjectilePrefab || ctx == null) return;

        // Spawn position: muzzle or player
        Transform muzzle = ctx.muzzle ? ctx.muzzle : ctx.transform;
        Vector3 spawnPos = muzzle.position + (Vector3)ctx.spawnOffset;

        // Determine facing (left/right)
        int dirSign = 1;
        if (ctx.useFacingFromSprite && ctx.spriteToRead != null)
            dirSign = ctx.spriteToRead.flipX ? -1 : 1;

        Vector2 dir = (dirSign > 0) ? Vector2.right : Vector2.left;

        // Spawn the projectile
        GameObject go = GameObject.Instantiate(splitProjectilePrefab, spawnPos, Quaternion.identity);

        // Launch it using your Bullet script if present
        var bullet = go.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.Launch(dir, speed, damage);
        }
        else
        {
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.velocity = dir.normalized * speed;
        }

        // Configure the split behaviour if present
        var splitter = go.GetComponent<SplitAfterDistance>();
        if (splitter != null)
        {
            splitter.splitDistance = splitDistance;
            splitter.spreadAngleDeg = spreadAngleDeg;
            splitter.childSpeedMultiplier = childSpeedMultiplier;
            splitter.damage = damage;

            if (childProjectilePrefab != null)
                splitter.childProjectilePrefab = childProjectilePrefab;
            else
                splitter.childProjectilePrefab = splitProjectilePrefab; // fallback
        }
    }
}
