using UnityEngine;

public class EnemyProjectile : Projectile
{
    protected override void CallHits(Vector3 hitPosition, GameObject hitCollider, Collider[] colliders)
    {
        foreach(Collider collider in colliders)
        {
            if (collider.CompareTag("Player"))
            {
                collider.GetComponent<PlayerStats>().onPlayerHit.Invoke(strength);
            }
        }
        onDestroy.Invoke();
    }
}
