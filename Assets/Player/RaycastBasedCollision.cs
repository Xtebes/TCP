using UnityEngine;
public class RaycastBasedCollision : MonoBehaviour
{
	public float skinWidth = 0.1f;
	private float minimumExtent;
	private Vector3 previousPosition;
	private Rigidbody myRigidbody;
	private Collider myCollider;
	public LayerMask mask; 
	void Start()
	{
		myRigidbody = GetComponent<Rigidbody>();
		myCollider = GetComponent<Collider>();
		previousPosition = myRigidbody.position;
		minimumExtent = Mathf.Max(Mathf.Max(myCollider.bounds.extents.x, myCollider.bounds.extents.y), myCollider.bounds.extents.z);
	}
	void Update()
	{
		Vector3 movementThisStep = myRigidbody.position - previousPosition;
		float movementMagnitude = movementThisStep.magnitude;
		RaycastHit hitInfo;
		if (Physics.Raycast(previousPosition, movementThisStep, out hitInfo, movementMagnitude, mask))
		{
			if (!hitInfo.collider.isTrigger)
			{
				myRigidbody.position = hitInfo.point - (movementThisStep * movementMagnitude) * (minimumExtent + skinWidth);
				myRigidbody.velocity = Vector3.zero;
			}
		}
		previousPosition = myRigidbody.position;
	}
}
