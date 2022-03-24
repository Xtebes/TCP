using UnityEngine;
public class GravityPoint : MonoBehaviour
{
    private void FixedUpdate()
    {
        foreach (GravityAffected gravityAffectedBody in GravityAffected.gravityAffectedBodies)
        {
            gravityAffectedBody.Attract(transform.position);
        }
    }
}
