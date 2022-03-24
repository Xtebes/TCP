using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using FMODUnity;
public class Player : MonoBehaviour
{
    Transform cameraTransform;
    Vector3 movementDirection;
    [SerializeField]
    float mouseSensitivity, 
        cameraMaxHeight, cameraMinHeight, 
        feetDistance;
    float verticalCameraRotation;
    float isJumping;
    [SerializeField]
    LayerMask jumpDetectionLayerMask;
    Rigidbody rigidBody;
    PlayerStats stats;
    [SerializeField]
    Vector3 playerFeetArea;
    InputAction bodyMovement, mousePosition, jump;
    public StudioEventEmitter jetpackThrustEmitter;
    [SerializeField]
    Animator movementAnimator;
    void Awake() 
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        cameraTransform = Camera.main.transform;
    }
    void Start()
    {
        stats = GetComponent<PlayerStats>();
        rigidBody = GetComponent<Rigidbody>();
        StartCoroutine(JumpCycle());
    }
    public void LoadInput(Input input) 
    {
        mousePosition = input.mousePosition;
        bodyMovement = input.bodyMovement;
        jump = input.jump;
        jump.performed += ctx => jetpackThrustEmitter.Stop();
    }
    private void FixedUpdate()
    {
        Vector3 posToBe = rigidBody.position + movementDirection * Time.fixedDeltaTime * stats.movementSpeed;
        rigidBody.MovePosition(posToBe);
    }
    bool IsOnFloor()
    {
        return Physics.BoxCast(transform.position, playerFeetArea / 2, -transform.up, transform.rotation, feetDistance, jumpDetectionLayerMask);
    }
    private IEnumerator JumpCycle()
    {
        IEnumerator JumpOnCooldown(float cooldown)
        {
            rigidBody.AddForce(transform.up.normalized * stats.jumpForce, ForceMode.Impulse);
            yield return new WaitForSeconds(cooldown);
        }
        while (true)
        {
            if (isJumping > 0.5f)
            {
                if(IsOnFloor())
                {
                    yield return StartCoroutine(JumpOnCooldown(0.4f));
                }
                else
                {
                    jetpackThrustEmitter.Play();
                    while (isJumping > 0.5f)
                    {
                        float energyToConsume = stats.jetpackConsumptionPerSecond * Time.fixedDeltaTime * (100 - stats.jetpackEfficiency) / 100;
                        if (stats.jetpackEnergy >= energyToConsume)
                        {
                            rigidBody.MovePosition(transform.position + transform.up.normalized * stats.jetpackForce * Time.fixedDeltaTime);
                            rigidBody.velocity = Vector3.zero;
                            stats.IncreaseJetpackEnergyBy(-energyToConsume);
                            stats.timeSinceLastJetpackUse = 0;
                        }
                        yield return new WaitForFixedUpdate();
                    }
                    jetpackThrustEmitter.Stop();             
                }
            }
            yield return new WaitForFixedUpdate();
        }
    }
    void Update()
    {
        isJumping = jump.ReadValue<float>();
        Vector2 bodyMovementInput = bodyMovement.ReadValue<Vector2>();
        Vector2 mousePositionDelta = mousePosition.ReadValue<Vector2>();
        movementAnimator.SetBool("Grounded", IsOnFloor());
        movementAnimator.SetFloat("IsMoving", bodyMovementInput.magnitude);
        transform.Rotate(Vector3.up * mousePositionDelta.x * mouseSensitivity);
        movementDirection = transform.TransformDirection(new Vector3(bodyMovementInput.x, 0, bodyMovementInput.y).normalized);
        verticalCameraRotation += -mousePositionDelta.y * mouseSensitivity;
        verticalCameraRotation = Mathf.Clamp(verticalCameraRotation, cameraMinHeight, cameraMaxHeight);
        cameraTransform.localEulerAngles = new Vector3(verticalCameraRotation, 0, 0);
    }
}
