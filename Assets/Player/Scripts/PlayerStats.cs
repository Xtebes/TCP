using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
public class PlayerStats : MonoBehaviour
{
    public Action<float> onPlayerHit;
    public Action onPlayerKilled;
    [SerializeField]
    Upgradeable armorNullUp, maxShieldUp, shieldRegenUp, jetpackEffUp, jetpackStrengthUp, jetpackRegenUp, weaponStrengthUp, weaponRadiusUp;
    [SerializeField]
    StatisticsPanel statsPanel;
    Tween armorFade, shieldFade, jetpackEnergyFade;
    float timeSinceLastHit;
    public float timeSinceLastJetpackUse;
    private Dictionary<string, int> crystals = new Dictionary<string, int>()
    {
        {"red", 30},
        {"yellow", 10},
        {"orange", 5},
    }; 
    private readonly Dictionary<string, int>[] CapacityUpgradesCost = new[]
    {
        new Dictionary<string, int>() { {"red", 2} },
        new Dictionary<string, int>() { {"red", 6} },
        new Dictionary<string, int>() { {"red", 2}, {"yellow", 2} },
        new Dictionary<string, int>() { {"red", 4}, {"yellow", 3} },
        new Dictionary<string, int>() { {"red", 9}, {"yellow", 6} },
    };
    private readonly Dictionary<string, int>[] NullificationUpgradesCost = new[]
    {
        new Dictionary<string, int>() { {"red", 1} },
        new Dictionary<string, int>() { {"red", 3} },
        new Dictionary<string, int>() { {"red", 5}, {"yellow", 1} },
        new Dictionary<string, int>() { {"red", 10}, {"yellow", 4} },
        new Dictionary<string, int>() { {"red", 15}, {"yellow", 8}, {"orange", 4}},
    };
    private readonly Dictionary<string, int>[] ShieldRegenerationUpgradesCost = new[]
    {
        new Dictionary<string, int>() { {"red", 1} },
        new Dictionary<string, int>() { {"red", 3} },
        new Dictionary<string, int>() { {"red", 5}, {"yellow", 1} },
        new Dictionary<string, int>() { {"red", 10}, {"yellow", 4} },
        new Dictionary<string, int>() { {"red", 15}, {"yellow", 8}, {"orange", 4}},
    };
    private readonly Dictionary<string, int>[] JetpackEfficiencyUpgradesCost = new[]
    {
        new Dictionary<string, int>() { {"red", 1} },
        new Dictionary<string, int>() { {"red", 3} },
        new Dictionary<string, int>() { {"red", 5}, {"yellow", 1} },
        new Dictionary<string, int>() { {"red", 10}, {"yellow", 4} },
        new Dictionary<string, int>() { {"red", 15}, {"yellow", 8}, {"orange", 4}},
    };
    private readonly Dictionary<string, int>[] JetpackStrengthUpgradesCost = new[]
    {
        new Dictionary<string, int>() { {"red", 1} },
        new Dictionary<string, int>() { {"red", 3} },
        new Dictionary<string, int>() { {"red", 5}, {"yellow", 1} },
        new Dictionary<string, int>() { {"red", 10}, {"yellow", 4} },
        new Dictionary<string, int>() { {"red", 15}, {"yellow", 8}, {"orange", 4}},
    };
    private readonly Dictionary<string, int>[] JetpackRegenerationUpgradesCost = new[]
    {
        new Dictionary<string, int>() { {"red", 1} },
        new Dictionary<string, int>() { {"red", 3} },
        new Dictionary<string, int>() { {"red", 5}, {"yellow", 1} },
        new Dictionary<string, int>() { {"red", 10}, {"yellow", 4} },
        new Dictionary<string, int>() { {"red", 15}, {"yellow", 8}, {"orange", 4}},
    };
    private readonly Dictionary<string, int>[] weaponStrengthUpgradesCost = new[]
    {
        new Dictionary<string, int>() { {"red", 6} },
        new Dictionary<string, int>() { {"red", 8} },
        new Dictionary<string, int>() { {"red", 10}, {"yellow", 2} },
        new Dictionary<string, int>() { {"red", 6}, {"yellow", 4} },
        new Dictionary<string, int>() { {"red", 4}, {"yellow", 8}, {"orange", 4}},
    };
    private readonly Dictionary<string, int>[] weaponRadiusUpgradesCost = new[]
    {
        new Dictionary<string, int>() { {"red", 2} },
        new Dictionary<string, int>() { {"red", 5} },
        new Dictionary<string, int>() { {"red", 7}, {"yellow", 3} },
        new Dictionary<string, int>() { {"red", 4}, {"yellow", 4}, { "orange", 2 }},
        new Dictionary<string, int>() { {"red", 3}, {"yellow", 8}, {"orange", 4}},
    };
    public void AddCrystals(string crystalType, int amount)
    {
        crystals[crystalType] += amount;
        statsPanel.UpdateCrystalsDisplay(crystals);
    }
    private readonly float[] CapacityUpgradeValues = new[]
    {
        50f,
        70f,
        100f,
        140f,
        200f,
        250f,
    };
    private readonly float[] NullificationUpgradeValues = new[]
    {
        0,
        10f,
        20f,
        30f,
        50f,
        70f,
    };
    private readonly float[] ShieldRegenerationUpgradeValues = new[]
    {
        20f,
        30f,
        50f,
        80f,
        110f,
        150f,
    };
    private readonly float[] JetpackEfficiencyUpgradeValues = new[]
    {
        0f,
        25f,
        37f,
        45f,
        57f,
        70f,
    };
    private readonly float[] jetpackStrengthUpgradeValues = new[]
    {
        3f,
        5f,
        8f,
        10f,
        13f,
        16f,
    };
    private readonly float[] jetpackRegenerationUpgradeValues = new[]
    {
        9f,
        12f,
        15f,
        19f,
        24f,
        30f,
    };
    private readonly float[] weaponStrengthUpgradeValues = new[]
{
        0.3f,
        0.35f,
        0.4f,
        0.5f,
        0.6f,
        0.7f,
    };
    private readonly int[] weaponRadiusUpgradeValues = new[]
    {
        2,
        3,
        4,
        6,
        8,
        10,
    };

    public float
        //armor stats
        armor, maxArmor, nullification,
        //shield stats
        timeToStartShieldRegen, shieldRegenPerSecond, shield, maxShield,
        //jetpack stats
        timeToStartJetpackRegen, jetpackRegenPerSecond, jetpackEnergy, jetpackConsumptionPerSecond, maxJetpackEnergy, jetpackForce, jetpackEfficiency,
        //movement stats
        movementSpeed, jumpForce,
        //weapon stats
        weaponStrength; public int  weaponRadius;
    //displayed
    [SerializeField]
    private Slider 
        armorOnHUD, armorOnHUDFade, 
        shieldOnHUD, shieldOnHUDFade, 
        jetpackEnergyOnHUD, jetpackEnergyOnHUDFade;
    private void Start()
    {
        armorNullUp.onLevelPurchase = TryToPurchaseArmorNullLevel;
        armorNullUp.onLevelChange = ChangeNullificationLevel;
        maxShieldUp.onLevelPurchase = TryToPurchaseShieldCapacityLevel;
        maxShieldUp.onLevelChange = ChangeShieldCapacityLevel;
        shieldRegenUp.onLevelPurchase = TryToPurchaseShieldRegenLevel;
        shieldRegenUp.onLevelChange = ChangeShieldRegenLevel;
        jetpackEffUp.onLevelPurchase = TryToPurchaseJetpackEfficiencyLevel;
        jetpackEffUp.onLevelChange = ChangeJetpackEfficiencyLevel;
        jetpackRegenUp.onLevelPurchase = TryToPurchaseJetpackRegenLevel;
        jetpackRegenUp.onLevelChange = ChangeJetpackRegenerationLevel;
        jetpackStrengthUp.onLevelPurchase = TryToPurchaseJetpackStrengthLevel;
        jetpackStrengthUp.onLevelChange = ChangeJetpackStrengthLevel;
        weaponStrengthUp.onLevelPurchase = TryToPurchaseWeaponStrengthLevel;
        weaponStrengthUp.onLevelChange = ChangeWeaponStrengthLevel;
        weaponRadiusUp.onLevelPurchase = TryToPurchaseWeaponRadiusLevel;
        weaponRadiusUp.onLevelChange = ChangeWeaponRadiusLevel;
        statsPanel.UpdateCrystalsDisplay(crystals);
    }
    private bool canPurchaseLevel(Dictionary<string, int> crystalCost)
    {
        for (int i = 0; i < crystalCost.Count; i++)
        {
            string key = crystalCost.ElementAt(i).Key;
            if (crystals[key] < crystalCost[key])
            {
                return false;
            }
        }
        return true;
    }
    private void PurchaseLevel(Dictionary<string,int> crystalCost)
    {
        for (int i = 0; i < crystalCost.Count; i++)
        {
            string key = crystalCost.ElementAt(i).Key;
            crystals[key] -= crystalCost[key];
        }
        statsPanel.UpdateCrystalsDisplay(crystals);
    }
    private bool TryToPurchaseArmorNullLevel(int level)
    {
        if (!canPurchaseLevel(NullificationUpgradesCost[level])) return false;
        PurchaseLevel(NullificationUpgradesCost[level]); return true;
    }
    private void ChangeNullificationLevel(int level, Upgradeable up)
    {
        nullification = NullificationUpgradeValues[level];
        Dictionary<string, int> costToUpdate = null;
        if (level < 5)
            costToUpdate = NullificationUpgradesCost[level]; 
        UpdateStatDisplay(NullificationUpgradeValues[level], up, costToUpdate);
        up.statValueText.text = up.statValueText.text + "%";
    }
    private void ChangeShieldCapacityLevel(int level, Upgradeable up)
    {
       SetMaxShield(CapacityUpgradeValues[level]);
        Dictionary<string, int> costToUpdate = null;
        if (level < 5)
            costToUpdate = CapacityUpgradesCost[level];
        UpdateStatDisplay(CapacityUpgradeValues[level], up, costToUpdate);
    }
    private void ChangeShieldRegenLevel(int level, Upgradeable up)
    {
       shieldRegenPerSecond = ShieldRegenerationUpgradeValues[level];
        Dictionary<string, int> costToUpdate = null;
        if (level < 5)
            costToUpdate = ShieldRegenerationUpgradesCost[level];
        UpdateStatDisplay(ShieldRegenerationUpgradeValues[level], up, costToUpdate);
    }
    private void ChangeJetpackEfficiencyLevel(int level, Upgradeable up)
    {
        jetpackEfficiency = JetpackEfficiencyUpgradeValues[level];
        Dictionary<string, int> costToUpdate = null;
        if (level < 5)
            costToUpdate = JetpackEfficiencyUpgradesCost[level];
        UpdateStatDisplay(JetpackEfficiencyUpgradeValues[level], up, costToUpdate);
        up.statValueText.text = up.statValueText.text + "%";
    }
    private void ChangeJetpackRegenerationLevel(int level, Upgradeable up)
    {
        jetpackRegenPerSecond = jetpackRegenerationUpgradeValues[level];
        Dictionary<string, int> costToUpdate = null;
        if (level < 5)
            costToUpdate = JetpackRegenerationUpgradesCost[level];
        UpdateStatDisplay(jetpackRegenerationUpgradeValues[level], up, costToUpdate);

    }
    private void ChangeJetpackStrengthLevel(int level, Upgradeable up)
    {
        jetpackForce = jetpackStrengthUpgradeValues[level];
        Dictionary<string, int> costToUpdate = null;
        if (level < 5)
            costToUpdate = JetpackStrengthUpgradesCost[level];
        UpdateStatDisplay(jetpackStrengthUpgradeValues[level], up, costToUpdate);
    }
    private void ChangeWeaponStrengthLevel(int level, Upgradeable up)
    {
        weaponStrength = weaponStrengthUpgradeValues[level];
        Dictionary<string, int> costToUpdate = null;
        if (level < 5)
            costToUpdate = weaponStrengthUpgradesCost[level];
        UpdateStatDisplay(weaponStrengthUpgradeValues[level], up, costToUpdate);
        up.statValueText.text = (float.Parse(up.statValueText.text) * 100).ToString();
    }
    private void ChangeWeaponRadiusLevel(int level, Upgradeable up)
    {
        weaponRadius = weaponRadiusUpgradeValues[level];
        Dictionary<string, int> costToUpdate = null;
        if (level < 5)
            costToUpdate = weaponRadiusUpgradesCost[level];
        UpdateStatDisplay(weaponRadiusUpgradeValues[level], up, costToUpdate);
    }
    private bool TryToPurchaseShieldCapacityLevel(int level)
    {
        if (!canPurchaseLevel(CapacityUpgradesCost[level])) return false;
        PurchaseLevel(CapacityUpgradesCost[level]); return true;
    }
    private bool TryToPurchaseShieldRegenLevel(int level)
    {
        if (!canPurchaseLevel(ShieldRegenerationUpgradesCost[level])) return false;
        PurchaseLevel(ShieldRegenerationUpgradesCost[level]); return true;
    }
    private bool TryToPurchaseJetpackEfficiencyLevel(int level)
    {
        if (!canPurchaseLevel(JetpackEfficiencyUpgradesCost[level])) return false;
        PurchaseLevel(JetpackEfficiencyUpgradesCost[level]); return true;
    }
    private bool TryToPurchaseJetpackRegenLevel(int level)
    {
        if (!canPurchaseLevel(JetpackRegenerationUpgradesCost[level])) return false;
        PurchaseLevel(JetpackRegenerationUpgradesCost[level]); return true;
    }
    private bool TryToPurchaseJetpackStrengthLevel(int level)
    {
        if (!canPurchaseLevel(JetpackStrengthUpgradesCost[level])) return false;
        PurchaseLevel(JetpackStrengthUpgradesCost[level]); return true;
    }
    private bool TryToPurchaseWeaponStrengthLevel(int level)
    {
        if (!canPurchaseLevel(weaponStrengthUpgradesCost[level])) return false;
        PurchaseLevel(weaponRadiusUpgradesCost[level]); return true;
    }
    private bool TryToPurchaseWeaponRadiusLevel(int level)
    {
        if (!canPurchaseLevel(weaponRadiusUpgradesCost[level])) return false;
        PurchaseLevel(weaponRadiusUpgradesCost[level]); return true;
    }
    private void KillPlayer()
    {
        onPlayerKilled?.Invoke();
    }
    private void BreakShield()
    {
    }
    void UpdateStatDisplay(float value, Upgradeable up, Dictionary<string,int> nextLevelCosts)
    {
        string textValue = value.ToString();
        int subLength = Mathf.Min(3, textValue.Length);
        up.statValueText.text = textValue.Substring(0, subLength);
        if (up.level == up.maxLevel && up.level < 5)
        {
            up.yellowCrystalCost.transform.parent.gameObject.SetActive(true);
            up.orangeCrystalCost.transform.parent.gameObject.SetActive(true);
            if (nextLevelCosts.ContainsKey("red"))
            {
                up.redCrystalCost.text = nextLevelCosts["red"].ToString();
                up.redCrystalCost.transform.parent.gameObject.SetActive(true);
            }
            else
            {
                up.redCrystalCost.transform.parent.gameObject.SetActive(false);
            }
            if (nextLevelCosts.ContainsKey("yellow"))
            {
                up.yellowCrystalCost.text = nextLevelCosts["yellow"].ToString();
                up.yellowCrystalCost.transform.parent.gameObject.SetActive(true);
            }
            else
            {
                up.yellowCrystalCost.transform.parent.gameObject.SetActive(false);
            }
            if (nextLevelCosts.ContainsKey("orange"))
            {
                up.orangeCrystalCost.text = nextLevelCosts["orange"].ToString();
                up.orangeCrystalCost.transform.parent.gameObject.SetActive(true);
            }
            else
            {
                up.orangeCrystalCost.transform.parent.gameObject.SetActive(false);
            }
        }
        else
        {
            up.redCrystalCost.transform.parent.gameObject.SetActive(false);
            up.yellowCrystalCost.transform.parent.gameObject.SetActive(false);
            up.orangeCrystalCost.transform.parent.gameObject.SetActive(false);
        }

    }
    private void PlayerHit(float value)
    {
        timeSinceLastHit = 0;
        float leftOverDamage = IncreaseShieldBy(-value);
        if (leftOverDamage > 0) { leftOverDamage = leftOverDamage * ((100f - nullification) / 100); IncreaseArmorBy(-leftOverDamage);}
    }
    public void SetMaxJetpackEnergy(float value)
    {
        maxJetpackEnergy = value;
        jetpackEnergyOnHUD.maxValue = value;
        jetpackEnergyOnHUDFade.maxValue = value;
    }
    public void SetMaxArmor(float value)
    {
        maxArmor = value;
        armorOnHUD.maxValue = value;
        armorOnHUDFade.maxValue = value;
    }
    public void SetMaxShield(float value)
    {
        maxShield = value;
        shieldOnHUD.maxValue = value;
        shieldOnHUDFade.maxValue = value;
    }
    public void IncreaseArmorBy(float value)
    {
        float lastArmorValue = armor;
        armor = Mathf.Clamp(armor + value, 0, maxArmor);
        if (Mathf.Approximately(armor, 0) && lastArmorValue > 0) KillPlayer();
        armorOnHUD.value = armor;
        if (armorFade != null) armorFade.Kill();
        armorFade = armorOnHUDFade.DOValue(armor, 0.4f);
    }
    public void IncreaseJetpackEnergyBy(float value)
    {
        jetpackEnergy = Mathf.Clamp(jetpackEnergy + value, 0, maxJetpackEnergy);
        jetpackEnergyOnHUD.value = jetpackEnergy;
        if (jetpackEnergyFade != null) jetpackEnergyFade.Kill();
        jetpackEnergyFade = jetpackEnergyOnHUDFade.DOValue(jetpackEnergy, 0.4f);
    }
    public float IncreaseShieldBy(float value)
    {;
        float lastShieldValue = shield;
        shield = Mathf.Clamp(shield + value, 0, maxShield);
        if (Mathf.Approximately(shield,0) && lastShieldValue > 0) BreakShield();
        shieldOnHUD.value = shield;
        if (shieldFade != null) shieldFade.Kill();
        shieldFade = shieldOnHUDFade.DOValue(shield, 0.4f);
        if (Mathf.Approximately(shield, 0)) return Mathf.Abs(shield - value);
        return 0;
    }
    private void Update()
    {
        timeSinceLastHit += Time.deltaTime;
        if (timeSinceLastHit > timeToStartShieldRegen && shield < maxShield)
        {
            IncreaseShieldBy(shieldRegenPerSecond * Time.deltaTime);
        }
        timeSinceLastJetpackUse += Time.deltaTime;
        if (timeSinceLastJetpackUse > timeToStartJetpackRegen && jetpackEnergy < maxJetpackEnergy)
        {
            IncreaseJetpackEnergyBy(jetpackRegenPerSecond * Time.deltaTime);
        }
    }
    void Awake()
    {
        SetMaxArmor(200);
        SetMaxShield(CapacityUpgradeValues[0]);
        SetMaxJetpackEnergy(100);
        IncreaseArmorBy(maxArmor);
        IncreaseShieldBy(maxShield);
        IncreaseJetpackEnergyBy(maxJetpackEnergy);
        onPlayerHit += PlayerHit;
    }
}