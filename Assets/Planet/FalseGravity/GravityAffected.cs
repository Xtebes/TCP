using UnityEngine;
using System.Collections.Generic;
[RequireComponent(typeof(Rigidbody))]
public class GravityAffected : MonoBehaviour
{
    public static List<GravityAffected> gravityAffectedBodies = new List<GravityAffected>(); 
    private const float gravity = 9.8f;
    private Rigidbody rigidBody;
    private void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        rigidBody.useGravity = false;
        rigidBody.freezeRotation = true;
    }
    private void OnEnable()
    {
        gravityAffectedBodies.Add(this);
    }
    private void OnDisable()
    {
        gravityAffectedBodies.Remove(this);
    }
    public void Attract(Vector3 attractionPoint)
    {
        Vector3 attractionDirection = (attractionPoint - transform.position).normalized;
        rigidBody.MoveRotation(Quaternion.FromToRotation(transform.up, -attractionDirection) * transform.rotation);
        rigidBody.AddForce(attractionDirection * gravity);
    }
}
