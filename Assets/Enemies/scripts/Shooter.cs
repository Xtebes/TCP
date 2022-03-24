using UnityEngine;
using System.Collections;
using FMODUnity;
public class Shooter : Enemy
{
    [SerializeField]
    float shotInterval, projectileSpeed, timeToStartShooting;
    [SerializeField]
    int projectileRadius;
    [SerializeField]
    GameObject projectilePrefab;
    [SerializeField]
    Transform activatedProjPool, deactivatedProjPool, projectileSource;
    [SerializeField]
    StudioEventEmitter shots;
    void PoolProjectile(Projectile projectile)
    {
        projectile.gameObject.SetActive(false);
        projectile.transform.parent = deactivatedProjPool;
    }
    void ShootProjectile(Vector3 direction)
    {
        shots.Play();
        Transform projTransform;
        Projectile enemyProj;
        if (deactivatedProjPool.childCount > 0)
        {
            projTransform = deactivatedProjPool.GetChild(0);
            enemyProj = projTransform.GetComponent<Projectile>();
        }
        else
        {
            projTransform = Instantiate(projectilePrefab).transform;
            enemyProj = projTransform.GetComponent<Projectile>();
            enemyProj.onDestroy = ()=> PoolProjectile(enemyProj);
        }
        enemyProj.SetProjectileSettings(damageDealt, direction, projectileSpeed, projectileRadius);
        projTransform.position = projectileSource.position;
        projTransform.gameObject.SetActive(true);
    }
    IEnumerator ShootCycle()
    {
        yield return new WaitForSeconds(timeToStartShooting);
        while (true)
        {
            Vector3 direction = (target.transform.position - transform.position).normalized;
            ShootProjectile(direction);
            yield return new WaitForSeconds(shotInterval);
        }
    }
    protected override void OnEnable()
    {
        base.OnEnable();
        StartCoroutine(ShootCycle());
    }
    private void FixedUpdate()
    {
        Avoid();
        LookAtPlayer();
        PursuePlayer();
    }
    private void OnDisable()
    {
        StopAllCoroutines();
    }
}
