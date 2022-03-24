using System.Collections;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
public class CoreTemperature : MonoBehaviour
{
    public int currentTemperature = 7430, GoalTemperature = 7000, minTemperatureBuildUp = 30, maxTemperatureBuildUp = 60;
    public GameObject canvas;
    public TextMeshProUGUI TemperatureText, TemperatureTextGoal, TemperatureTooHigh;
    public CountdownTimer timer;
    public Slider temperatureSlider;
    public float minDistanceToCoreToDisplay;
    public float secondsBeforeNextBuildUp;
    [SerializeField]
    float minSX, minSY, shakeSpeed;
    public static System.Action onCoreCooled;
    void Start()
    {
        temperatureSlider.minValue = GoalTemperature;
        temperatureSlider.maxValue = 15000;
        temperatureSlider.value = currentTemperature;
        TemperatureTextGoal.text = GoalTemperature.ToString();
        StartCoroutine(buildup());
    }
    public void CoolCore(int strength)
    {
        currentTemperature += strength;
        UpdateDisplay();
        if (currentTemperature < GoalTemperature)
        {
            onCoreCooled?.Invoke();
        }

    }
    private void UpdateDisplay()
    {
        temperatureSlider.value = currentTemperature;
        TemperatureText.text = currentTemperature.ToString();
    }

    private IEnumerator buildup()
    {
        while (true)
        {
            currentTemperature += Random.Range(minTemperatureBuildUp, maxTemperatureBuildUp);
            UpdateDisplay();    
            yield return new WaitForSeconds(secondsBeforeNextBuildUp);
        }
    }
}
