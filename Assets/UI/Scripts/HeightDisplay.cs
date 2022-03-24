using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HeightDisplay : MonoBehaviour
{
    Slider slider;
    [SerializeField]
    TextMeshProUGUI text;
    public IEnumerator UpdateHeight(Transform transform)
    {
        while (true)
        {
            slider.value = Vector3.Distance(Vector3.zero, transform.position);
            text.text = ((int)slider.value).ToString();
            yield return null;
        }
    }
    void Awake()
    {
        var terrainGeneration = FindObjectOfType<ChunkManager>();
        slider = GetComponent<Slider>();
        slider.maxValue = terrainGeneration.maxPlanetRadius + 200;
    }
}
