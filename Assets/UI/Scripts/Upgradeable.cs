using UnityEngine.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
public class Upgradeable : Selectable
{
    public int level = 0;
    public int maxLevel = 0;
    [SerializeField]
    Image[] upgradeIcons;
    public Color[] upgradeableColors;
    [SerializeField]
    private GameObject[] ShowOnSelect;
    [SerializeField]
    public TextMeshProUGUI statValueText, redCrystalCost, yellowCrystalCost, orangeCrystalCost;
    public System.Action<int, Upgradeable> onLevelChange; 
    public System.Func<int, bool> onLevelPurchase;
    [SerializeField]
    bool canDowngrade;
    protected override void Start()
    {
        for (int i = 0; i < ShowOnSelect.Length; i++)
        {
            ShowOnSelect[i].SetActive(false);
        }
        for (int i = 0; i < upgradeIcons.Length; i++)
        {
            upgradeIcons[i].gameObject.SetActive(false);
        }
        redCrystalCost.transform.parent.parent.gameObject.SetActive(false);
        onLevelChange?.Invoke(level, this);
    }
    public override Selectable FindSelectableOnLeft()
    {
        if (canDowngrade)
        {

            if (!checkForLevelChange(level - 1)) return null;
            level--;
            upgradeIcons[level].color = upgradeableColors[1];
            onLevelChange?.Invoke(level, this);
            redCrystalCost.transform.parent.gameObject.SetActive(false);
            yellowCrystalCost.transform.parent.gameObject.SetActive(false);
            orangeCrystalCost.transform.parent.gameObject.SetActive(false);
            if (level == 0)
                ShowOnSelect[0].SetActive(false);
        }
        return null;
    }
    bool checkForLevelChange(int level)
    {
        if (Mathf.Clamp(level,0,upgradeIcons.Length) == this.level)
        {
            return false;
        }
        return true;
    }
    public override Selectable FindSelectableOnRight()
    {
        if (!checkForLevelChange(level + 1)) return null;
        if (level + 1 > maxLevel)
        {
            if (onLevelPurchase.Invoke(level))
            {
                maxLevel++;
            }
            else
            {
                return null;
            }
        }
        level++;
        if (canDowngrade)
            ShowOnSelect[0].SetActive(true);
        upgradeIcons[level - 1].color = upgradeableColors[2];
        onLevelChange?.Invoke(level, this);
        upgradeIcons[level - 1].gameObject.SetActive(true);
        return null;
    }
    public override void OnDeselect(BaseEventData eventData)
    {
        base.OnDeselect(eventData);
        for (int i = 0; i < ShowOnSelect.Length; i++)
        {
            ShowOnSelect[i].SetActive(false);
        }
        redCrystalCost.transform.parent.parent.gameObject.SetActive(false);
    }
    public override void OnSelect(BaseEventData eventData)
    {
        base.OnSelect(eventData);
        for (int i = 0; i < ShowOnSelect.Length; i++)
        {
            ShowOnSelect[i].SetActive(true);
        }
        if (!canDowngrade || level == 0)
        {
            ShowOnSelect[0].SetActive(false);
        }
        redCrystalCost.transform.parent.parent.gameObject.SetActive(true);
    }
}
