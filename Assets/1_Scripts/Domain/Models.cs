using UnityEngine;

// Enums are moved to Enums.cs

public class AmmoItem
{
    public ResourceType type;
    public float slowAmount;
    public float bonusDamage;
    
    public AmmoItem(ResourceType t) { type = t; slowAmount = 0f; bonusDamage = 0f; }
}

public class BuildingModel
{
    public string id;
    public int x;
    public int y;
    public BuildingDataSO data;
    
    // Inventory
    public int currentInput1;
    public int currentInput2;
    public int currentOutput;
    
    public System.Collections.Generic.Queue<AmmoItem> inputQueue = new System.Collections.Generic.Queue<AmmoItem>();
    public System.Collections.Generic.Queue<AmmoItem> outputQueue = new System.Collections.Generic.Queue<AmmoItem>();
    
    // Logic Timers
    public float attackTimer;
    public float productionTimer;
    public float mineTimer;
    
    // Shutdown state
    public bool isShutdown;
    public int level = 1;
    public ResourceType selectedAmmoType = ResourceType.None;
    public Direction direction = Direction.Right;
    
    public BuildingModel(string id, int x, int y, BuildingDataSO data, Direction dir = Direction.Right)
    {
        this.id = id;
        this.x = x;
        this.y = y;
        this.data = data;
        this.direction = dir;
        
        this.currentInput1 = 0;
        this.currentInput2 = 0;
        this.currentOutput = 0;
        
        if (data.buildingType == BuildingType.TowerAttackBuff || 
            data.buildingType == BuildingType.FactorySpeedBuff ||
            (data.buildingType == BuildingType.Factory && data.inputType1 == ResourceType.None && data.inputType2 == ResourceType.None))
        {
            this.isShutdown = false;
        }
        else
        {
            this.isShutdown = true;
        }
        
        this.attackTimer = 999f; // Start ready to attack
    }
}

public class MonsterModel
{
    public string id;
    public MonsterDataSO data;
    public float maxHp;
    public float currentHp;
    public Vector3 currentPosition;
    public int currentPathIndex;
    public float speed;
    public float baseSpeed;
    
    public MonsterModel(string id, MonsterDataSO data, Vector3 startPosition, float hpMultiplier = 1f, float speedOverride = -1f)
    {
        this.id = id;
        this.data = data;
        this.maxHp = data.maxHp * hpMultiplier;
        this.currentHp = this.maxHp;
        this.baseSpeed = speedOverride > 0 ? speedOverride : data.speed;
        this.speed = this.baseSpeed;
        this.currentPosition = startPosition;
        this.currentPathIndex = 0;
    }
}

public class ProjectileModel
{
    public string id;
    public Vector3 currentPosition;
    public string targetMonsterId;
    public float damage;
    public float speed;
    public bool isIce;
    public float slowAmount;
}
