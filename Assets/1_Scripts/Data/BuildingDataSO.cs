using UnityEngine;

[CreateAssetMenu(fileName = "BuildingData", menuName = "Deploy/BuildingData")]
public class BuildingDataSO : ScriptableObject
{
    public BuildingType buildingType;
    public string buildingName;
    public string displayName;
    public Sprite sprite;
    public Color color = Color.white;
    public string description;
    
    public System.Collections.Generic.List<ZoneType> allowedZones = new System.Collections.Generic.List<ZoneType>();
    
    public float attackDamage;
    public float attackSpeed;
    public float attackRange;
    public int maxAmmo; // used for Tower Input capacity
    public ResourceType requiredAmmoType; // Tower Input Type
    
    public float productionTime; // Factory/Mine production time
    public ResourceType inputType1; // Factory Input 1
    public ResourceType inputType2; // Factory Input 2 (optional)
    public int maxInputCapacity;    // Factory Input Capacity
    
    public ResourceType outputType; // Factory/Mine Output
    public int maxOutputCapacity;   // Mine/Factory Output Capacity
    
    public int connectionRange; 
    public float buffAmount;
    public int buffRange;
    public ResourceType costType = ResourceType.Gold;
    public int cost = 100;
    
    public System.Collections.Generic.List<UpgradeInfo> upgrades = new System.Collections.Generic.List<UpgradeInfo>();
}

[System.Serializable]
public class UpgradeInfo
{
    public int level;
    public ResourceType costType = ResourceType.Gold;
    public int cost;
    public float effectAmount;
}
