using UnityEngine;
using FMODUnity;
public class SpawnPlayer : MonoBehaviour
{
    [SerializeField]
    ChunkManager chunkManager;
    [SerializeField]
    GameObject player, playerPosOnDescent;
    bool isDescending = true;
    [SerializeField]
    float descentSpeed;
    Rigidbody rb;
    StudioEventEmitter crashEmitter;
    void Start()
    {
        transform.position = Random.onUnitSphere * (FindObjectOfType<ChunkManager>().maxPlanetRadius + 200);
        player.transform.position = transform.position;
        rb = GetComponent<Rigidbody>();
        player.GetComponent<Input>().inputActionAsset.Disable();
        crashEmitter = GetComponent<StudioEventEmitter>();
    }
    private void Update()
    {
        if (isDescending)
        {
            player.transform.position = playerPosOnDescent.transform.position;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isDescending && collision.collider.CompareTag("Terrain"))
        {
            crashEmitter.Play();
            isDescending = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
            player.GetComponent<Input>().inputActionAsset.Enable();
        }
    }
}
