using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
public class EnergyWeapon : MonoBehaviour
{
    public float projectileSpeed, strength;
    public float fireRate;
    public GameObject electricProjectilePrefab, cryoProjectilePrefab;
    GameObject projectileToShoot;
    public Transform projectileSource;
    Coroutine fireCycle;
    public Animator animator;
    [SerializeField]
    PlayerStats stats;
    InputAction fireAction;
    float isFiring;
    bool canFire = true;
    float time;
    void Awake() 
    {
        projectileToShoot = electricProjectilePrefab;
    }
    private void Update()
    {
        isFiring = fireAction.ReadValue<float>();
        animator.SetFloat("IsFiring", fireAction.ReadValue<float>());
        AnimatorStateInfo animState = animator.GetCurrentAnimatorStateInfo(0); 
        if (animState.IsName("SwitchModes") || animState.IsName("SwitchModes -1") || animState.IsName("GunEnergyExhaust") || animState.IsName("GunCryoExhaust"))
        {
            time = 0;
        }
        else if (canFire && isFiring == 1)
        {
            time += Time.deltaTime;
            if (time >= fireRate) { FireProjectile(); time = 0; }
        }
    }
    void FireProjectile()
    {
        Vector3 direction;
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.gameObject.transform.forward);
        RaycastHit hit;
        GameObject projectileInstance;
        projectileInstance = Instantiate(projectileToShoot, projectileSource.position, Quaternion.identity);
        Projectile projectile = projectileInstance.GetComponent<Projectile>();
        direction = ray.direction;
        if (Physics.Raycast(ray, out hit)) direction = hit.point - projectileSource.position;
        projectile.SetProjectileSettings(stats.weaponStrength, direction, projectileSpeed, stats.weaponRadius);
    }
    private IEnumerator ChangeProjectile(Input input, float changeSpeed)
    {
        input.switchMode.Disable();
        animator.SetBool("EnergyMode", !animator.GetBool("EnergyMode"));
        animator.SetTrigger("Switch");
        canFire = false;
        if (fireCycle != null)
            StopCoroutine(fireCycle);
        if (projectileToShoot == electricProjectilePrefab)
        {
            projectileToShoot = cryoProjectilePrefab;
        }
        else
        {
            projectileToShoot = electricProjectilePrefab;
        }
        yield return new WaitForSeconds(changeSpeed);
        canFire = true;
        input.switchMode.Enable();
    }
    public void LoadInput(Input input)
    {
        fireAction = input.fire;
        input.switchMode.performed += callback => StartCoroutine(ChangeProjectile(input, 0.8f));
    }
}
