using UnityEngine;
public abstract class Projectile : MonoBehaviour
{
    [SerializeField]
    private float duration;
    private float lifeTime;
    protected int radius;
    protected float strength, speed;
    protected Vector3 direction;
    private Vector3 oldPosition;
    public System.Action onDestroy;
    [SerializeField]
    LayerMask layerMask;
    public void SetProjectileSettings(float strength, Vector3 direction, float speed, int radius)
    {
        this.strength = strength;
        this.speed = speed;
        this.direction = direction;
        this.radius = radius;
    }
    protected abstract void CallHits(Vector3 hitPosition, GameObject hitCollider, Collider[] colliders); 
    private void OnEnable() 
    {
        oldPosition = transform.position;
        lifeTime = 0;
    }
    void FixedUpdate()
    {
        lifeTime += Time.deltaTime;
        transform.position = transform.position + direction.normalized * speed * Time.fixedDeltaTime;
        RaycastHit hit;  
        if (Physics.Raycast(new Ray(oldPosition,transform.position - oldPosition), out hit, Vector3.Distance(transform.position, oldPosition), layerMask))
        {
            Collider[] colliders = Physics.OverlapSphere(hit.point, radius);
            CallHits(hit.point, hit.collider.gameObject, colliders);
        }
        else if (duration < lifeTime)
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, radius);
            CallHits(transform.position, null, colliders);
        }
        oldPosition = transform.position;     
    }
}