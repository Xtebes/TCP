using UnityEngine;
using System;
using FMODUnity;
[RequireComponent(typeof(Rigidbody))]
public abstract class Enemy : MonoBehaviour
{
    [SerializeField]
    protected StudioEventEmitter damageTakenSound;
    public Action onDestroy;
    Vector3[] rayPoints;
    protected Rigidbody rb;
    public Player target;
    public int id;
    protected bool isAvoiding = true;
    [SerializeField]
    protected float 
        avoidanceStrength, avoidanceRange, avoidanceRangeGain, 
        pursueStrength, pursueGain, maxPursueStrength,
        health, damageDealt, baseHealth;
    public void DamageEnemy(float value)
    {
        damageTakenSound.Play();
        health += value;
        if (health < 0)
        {
            onDestroy.Invoke();
        }
    }
    protected virtual void OnEnable()
    {
        health = baseHealth;
        isAvoiding = true;
    }
    void Start()
    {
        rayPoints = CoreAttacks.PointsOnSphere(Vector3.zero, 1, 16);
        rb = GetComponent<Rigidbody>();
    }
    protected void Avoid()
    {
        for (int i = 0; i < rayPoints.Length; i++)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, rayPoints[i], out hit, avoidanceRange))
            {
                rb.AddForce(Mathf.Clamp((avoidanceRange - hit.distance * avoidanceRangeGain) / avoidanceRange, 0, 1) * avoidanceStrength * (transform.position - hit.point).normalized * Time.fixedDeltaTime);
            }
        }
    }
    protected void LookAtPlayer()
    {
        transform.LookAt(target.transform, transform.position.normalized);
    }
    protected void PursuePlayer()
    {
        Vector3 chaseForce = (target.transform.position - transform.position).normalized * pursueStrength * Time.fixedDeltaTime * (Vector3.Distance(target.transform.position, transform.position) * pursueGain);
        chaseForce = chaseForce.magnitude > maxPursueStrength ? chaseForce : chaseForce.normalized * maxPursueStrength;
        rb.AddForce(chaseForce);
    }
}
