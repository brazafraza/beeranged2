using UnityEngine;

public abstract class PrimaryAbilitySO : ScriptableObject
{
    public abstract void UsePrimary(PlayerAttack ctx);
}

public abstract class SecondaryAbilitySO : ScriptableObject
{
    public abstract void UseSecondary(PlayerAttack ctx);
}

public abstract class MovementAbilitySO : ScriptableObject
{
    public abstract void UseMovement(PlayerController ctx);
}
