using System.Collections;
using TMPro;
using UnityEngine;
using System;
public class CountdownTimer : MonoBehaviour
{
    public float TimeLeft = 300f;
    public TextMeshProUGUI TimeText;
    private WaitForSeconds waitOneSecond = new WaitForSeconds(1);
    public Action onRunOutOfTime;
    private IEnumerator Timer()
    {
        while (TimeLeft > 0)
        {
            TimeLeft -= 1;
            TimeText.text = TimeSpan.FromSeconds(TimeLeft).ToString(@"mm\:ss");
            yield return waitOneSecond;
        }
        onRunOutOfTime?.Invoke();
    }
    private void Start()
    {
        StartCoroutine(Timer());
        StartCoroutine(Blinker());
    }
    private IEnumerator Blinker()
    {
        float timeToWait = 1;
        while (true)
        {
            yield return new WaitForSeconds(timeToWait);
            if (TimeLeft <= 30 && TimeLeft > 10)
            {
                timeToWait = 0.4f;
            }
            else if (TimeLeft <= 10 && TimeLeft > 0)
            {
                    timeToWait = 0.2f;
            }
            if (TimeLeft < 30)
            {
                TimeText.gameObject.SetActive(false);
                yield return new WaitForSeconds(timeToWait);
                TimeText.gameObject.SetActive(true);
            }
        }
    }
}
