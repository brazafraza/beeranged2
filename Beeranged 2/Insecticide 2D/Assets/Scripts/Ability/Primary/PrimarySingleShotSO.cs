using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Primary/Single Shot")]
public class PrimarySingleShotSO : PrimaryAbilitySO
{
    public GameObject projectilePrefab;
    public float speed = 16f;
    public int damage = 5;
    public float rotationOffsetDeg = -90f;

    public override void UsePrimary(PlayerAttack ctx)
    {
        if (!projectilePrefab || ctx == null) return;

        // Use ctx helpers but keep it simple:
        Transform muzzle = ctx.muzzle ? ctx.muzzle : ctx.transform;
        Vector3 spawnPos = muzzle.position + (Vector3)ctx.spawnOffset;

        int dirSign = 1;
        if (ctx.useFacingFromSprite && ctx.spriteToRead != null)
            dirSign = ctx.spriteToRead.flipX ? -1 : 1;

        Vector2 dir = (dirSign > 0) ? Vector2.right : Vector2.left;
        float baseAngle = (dirSign > 0) ? 0f : 180f;
        Quaternion rot = Quaternion.Euler(0f, 0f, baseAngle + rotationOffsetDeg);

        GameObject go = GameObject.Instantiate(projectilePrefab, spawnPos, rot);

        var rb = go.GetComponent<Rigidbody2D>();
        if (rb) rb.velocity = dir * speed;

        // Optional: set damage on a Bullet component if you have one
        var bullet = go.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.damage = damage; // or call a setter
        }
    }
}
