using UnityEngine;
using TMPro;
[RequireComponent(typeof(TextMeshProUGUI))]
public class FpsDisplay : MonoBehaviour
{
    int frames = 0;
    [SerializeField]
    float gapPerUpdate;
    float cumulativeTime = 0;
    private TextMeshProUGUI fps;
    private void Start()
    {
        fps = GetComponent<TextMeshProUGUI>();
    }
    void Update()
    {
        frames++;
        cumulativeTime += Time.deltaTime;
        if (cumulativeTime >= gapPerUpdate)
        {
            fps.text = (frames / gapPerUpdate).ToString();
            frames = 0;
            cumulativeTime -= gapPerUpdate;
        }

    }
}
