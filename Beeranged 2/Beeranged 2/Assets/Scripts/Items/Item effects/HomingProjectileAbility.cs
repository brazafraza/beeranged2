using UnityEngine;

public class HomingProjectileAbility : MonoBehaviour
{
    private AutoShooter shooter;

    void Start()
    {
        shooter = GetComponent<AutoShooter>();
        if (shooter != null)
        {
            shooter.enableHoming = true;
        }
    }
}
