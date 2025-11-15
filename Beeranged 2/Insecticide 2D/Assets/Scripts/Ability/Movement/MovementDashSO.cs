using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Movement/Dash")]
public class MovementDashSO : MovementAbilitySO
{
    public float dashSpeed = 16f;
    public float dashDuration = 0.15f;
   

    public override void UseMovement(PlayerController ctx)
    {
        if (ctx == null) return;

        // You can expose a helper on PlayerController to start a dash.
        // For now, simplest is to call a public method you add, e.g.:
        ctx.StartDash(dashSpeed, dashDuration);
    }
}
