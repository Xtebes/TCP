using UnityEngine;
using DG.Tweening;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;
public class StatisticsPanel : MonoBehaviour
{
    [SerializeField]
    float toggleAnimationDuration;
    int toggled = 0;
    public RectTransform[] upgradersTransform;
    public GameObject[] toggledObjects;
    public EventSystem eventSystem;
    private ContentSizeFitterRefresh csfr;
    InputAction toggleAction;
    [SerializeField]
    TextMeshProUGUI redCrystalDisplay,orangeCrystalDisplay,yellowCrystalDisplay;
    public void UpdateCrystalsDisplay(Dictionary<string,int> crystals)
    {
        redCrystalDisplay.text = crystals["red"].ToString();
        yellowCrystalDisplay.text = crystals["yellow"].ToString();
        orangeCrystalDisplay.text = crystals["orange"].ToString();
    }
    private void Start()
    {
        csfr = GetComponent<ContentSizeFitterRefresh>();
        for (int i =  0; i < upgradersTransform.Length; i++)
        {
            upgradersTransform[i].localScale = new Vector3(1, 0, 1);
        }
    }
    public void LoadPlayerInput(Input input)
    {
        toggleAction = input.toggleUpgraders;
        toggleAction.performed += delegate 
        {
            toggleAction.Disable();
            toggled = (toggled + 1) % 2;
            Tween tween = null;
            if (toggled == 1) eventSystem.SetSelectedGameObject(upgradersTransform[0].gameObject);
            else eventSystem.SetSelectedGameObject(null);
            for (int i = 0; i < toggledObjects.Length; i++)
            {
                toggledObjects[i].SetActive(!toggledObjects[i].activeSelf);
            }
            for (int i = 0; i < upgradersTransform.Length; i++)
            {
                csfr.RefreshContentFitters();
                tween = upgradersTransform[i].DOScale(new Vector3(1, toggled, 1), toggleAnimationDuration);
            }
            tween.onUpdate += ()=> csfr.RefreshContentFitters();
            tween.onComplete += () => toggleAction.Enable();
        };
    }
}
