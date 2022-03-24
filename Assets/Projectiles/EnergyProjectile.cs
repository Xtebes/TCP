using UnityEngine;
using FMODUnity;
public class EnergyProjectile : Projectile
{
    protected override void CallHits(Vector3 hitPosition, GameObject hitCollider, Collider[] colliders)
    {
        bool spawnEnemy = true;
        PlayerStats player = FindObjectOfType<PlayerStats>();
        bool doTerrainImpactSound = false;
        ChunkManager chunkManager = FindObjectOfType<ChunkManager>();
        chunkManager.SphereDeform(hitPosition, radius, -strength, new Color(0.04f, 0.04f, 0.04f, 0.04f));
        if (hitCollider != null)
        {
            if (hitCollider.CompareTag("core"))
            {
                spawnEnemy = false;
            }
        }
        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag("crystal"))
            {
                Crystal crystal = collider.GetComponent<Crystal>();
                player.AddCrystals(crystal.data.crystalType, crystal.data.crystalAmount);
                chunkManager.DestroyCrystal(crystal);
            }
            else if (collider.CompareTag("enemy"))
            {
                collider.gameObject.GetComponent<Enemy>().DamageEnemy(-strength);
                spawnEnemy = false;
            }
            else if (collider.CompareTag("Terrain")) doTerrainImpactSound = true;
        }
        if (spawnEnemy) EnemyManager.onSpawnEnemy.Invoke(hitPosition);
        if (doTerrainImpactSound) RuntimeManager.PlayOneShot("event:/Gun/CaveImpact", hitPosition);
        Destroy(gameObject);
    }
}
