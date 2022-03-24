using UnityEngine;
public class CryoProjectile : Projectile
{
    protected override void CallHits(Vector3 hitPosition, GameObject hitCollider, Collider[] colliders)
    {
        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag("core"))
            {
                collider.GetComponent<CoreTemperature>().CoolCore((int)(-strength * 200));
                break;
            }
            else if (collider.CompareTag("enemy"))
            {
                collider.gameObject.GetComponent<Enemy>().DamageEnemy(-strength);
            }
        }
        Destroy(gameObject);
    }
}
