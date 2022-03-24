using UnityEngine;
using System.Collections;
public class Kamikaze : Enemy
{
    [SerializeField]
    int explosionRadius;
    [SerializeField]
    float timeToStartLockedMovement, maxTimeInLockedState, lockedStateBaseSpeed;
    WaitForFixedUpdate wait = new WaitForFixedUpdate();
    public static System.Action<Vector3, float, int> onExplode;
    private IEnumerator LockedMovement()
    {
        Vector3 lockDirection = (target.transform.position - transform.position).normalized;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        yield return new WaitForSeconds(timeToStartLockedMovement);
        yield return wait;
        for (float t = 0; t < maxTimeInLockedState; t+= Time.fixedDeltaTime)
        {
            rb.MovePosition(rb.position + lockDirection * Time.fixedDeltaTime * t * t * lockedStateBaseSpeed);
            yield return wait;
        }
        Explode();
    }
    private void Update()
    {
        if (isAvoiding && Vector3.Distance(target.transform.position, transform.position) < avoidanceRange * 2)
        {
            isAvoiding = false;
            StartCoroutine(LockedMovement());
        }
    }
    private void FixedUpdate()
    {
        if (isAvoiding)
        {
            Avoid();
            LookAtPlayer();
            PursuePlayer();
        }
    }
    void Explode()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].CompareTag("Player"))
            {
                colliders[i].gameObject.GetComponent<PlayerStats>().onPlayerHit.Invoke(damageDealt);
            }
        }
        onExplode?.Invoke(transform.position, Random.Range(-0.8f, -0.3f), explosionRadius);
        StopAllCoroutines();
        onDestroy.Invoke();
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (!isAvoiding)
        {
            Explode();
        }
    }
}