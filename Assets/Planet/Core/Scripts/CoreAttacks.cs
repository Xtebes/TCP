using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
public class CoreAttacks : MonoBehaviour
{
    public float degreesPerSecond, beamDamagePerSecond, coreSurfaceDamagePerSecond;
    public float startOffset;
    public float beamMaxRange;
    float beamCurrentRange, beamMinRange;
    public float laserThickness;
    [Range(0,1)]
    public float laserSkinPerUnit;
    public GameObject LaserPrefab;
    List<LineRenderer> lineRenderers = new List<LineRenderer>();
    public Ease beamEase;
    private void OnCollisionStay(Collision collision)
    {
        PlayerStats stats;
        if (collision.collider.TryGetComponent(out stats))
        {
            stats.onPlayerHit.Invoke(coreSurfaceDamagePerSecond * Time.fixedDeltaTime);
        }
    }
    private System.Collections.IEnumerator EnemySpawnCycle(Transform playerTransform)
    {
        while (true)
        {
            if (Vector3.Distance(transform.position, playerTransform.position) < 90)
            {
                EnemyManager.onSpawnEnemy.Invoke((playerTransform.position - transform.position).normalized * 35);
            }
            yield return new WaitForSeconds(Random.Range(9, 15));
        }
    }
    private System.Collections.IEnumerator BeamCycle()
    {
        while (true)
        {
            TweenBeams(beamMinRange, beamMaxRange, 5).SetEase(beamEase);
            yield return new WaitForSeconds(Random.Range(7,20));
            TweenBeams(beamMaxRange, beamMinRange, 5).SetEase(beamEase);
            yield return new WaitForSeconds(Random.Range(7,20));
        }
    }
    Tween TweenBeams(float beamRangeStartValue, float beamRangeEndValue, float tweenDuration)
    {
        beamCurrentRange = beamRangeStartValue;
        return DOTween.To(() => beamCurrentRange, x => beamCurrentRange = x, beamRangeEndValue, tweenDuration);
    }
    void Start()
    {
        beamMinRange = transform.localScale.x / 2;
        Vector3[] pts = PointsOnSphere(transform.position, transform.localScale.x / 2, 128);
        for (int i = 0; i < pts.Length; i++)
        {
            GameObject laser = Instantiate(LaserPrefab, transform);
            LineRenderer lineRenderer = laser.GetComponent<LineRenderer>();
            lineRenderer.startWidth = laserThickness;
            lineRenderer.endWidth = laserThickness;
            lineRenderers.Add(lineRenderer);
            laser.transform.position = pts[i] + pts[i].normalized * startOffset;
        }
        StartCoroutine(BeamCycle());
        StartCoroutine(EnemySpawnCycle(FindObjectOfType<Player>().transform));
    }
    private void Update()
    {
        transform.Rotate(degreesPerSecond * Time.deltaTime, degreesPerSecond * Time.deltaTime, degreesPerSecond * Time.deltaTime);
        for (int i = 0; i < lineRenderers.Count; i++)
        {
            Ray ray = new Ray(transform.position, (lineRenderers[i].transform.position - transform.position).normalized);
            RaycastHit hit;
            Vector3 endPos = transform.position + ray.direction * beamCurrentRange;
            if (Physics.SphereCast(ray, laserThickness/2 - laserSkinPerUnit/2*laserThickness, out hit, beamCurrentRange))
            {
                PlayerStats playerStats;
                endPos = transform.position + ray.direction * hit.distance;
                if (hit.collider.TryGetComponent(out playerStats))
                {
                    playerStats.onPlayerHit.Invoke(beamDamagePerSecond * Time.deltaTime);
                }
            }
            lineRenderers[i].SetPosition(0, lineRenderers[i].transform.position);
            lineRenderers[i].SetPosition(1, endPos);
        }
    }
    //Completely stolen from some web page
    public static Vector3[] PointsOnSphere(Vector3 spherePosition, float scale, int sphereNumber)
    {
        Vector3[] pointsOnSphere = new Vector3[sphereNumber];
        float inc = Mathf.PI * (3 - Mathf.Sqrt(5));
        float off = 2.0f / sphereNumber;
        for (int i = 0; i < sphereNumber; i++)
        {
            float y = i * off - 1 + (off / 2);
            float r = Mathf.Sqrt(1 - y * y);
            float phi = i * inc;
            float x = Mathf.Cos(phi) * r;
            float z = Mathf.Sin(phi) * r;
            pointsOnSphere[i]  = (spherePosition + (new Vector3(x, y, z).normalized * scale));
        }
        return pointsOnSphere;
    }
}
