using System;
using System.Linq;
using UnityEngine;

public enum BuildingKind { Road, Mine, Factory, Forge, Tower, FactoryBuff, TowerBuff }
public enum MaterialKind { None, Stone, Iron, RoundStone, Arrow, Bullet }
public enum FlowDirection { Right, Down, Left, Up }
public enum SessionMode { Planning, Defense, Victory, Defeat }

[Serializable]
public class Ingredient
{
    public MaterialKind material;
    public int quantity = 1;
    public Ingredient(MaterialKind material, int quantity = 1) { this.material = material; this.quantity = quantity; }
}

[Serializable]
public class BuildingDefinition
{
    public string id;
    public string title;
    [TextArea] public string description;
    public BuildingKind kind;
    public int spriteIndex;
    public int price;
    public int limit = 4;
    public int unlockAfterStage;
    public MaterialKind output;
    public MaterialKind ammunition;
    public Ingredient[] recipe = Array.Empty<Ingredient>();
    public int inputCapacity = 5;
    public int outputCapacity = 5;
    public float productionPerSecond = 1;
    public float range;
    public float attacksPerSecond;
    public float damage;
    public float slow;
    public float bonusDamage;
    public int[] upgradePrices = Array.Empty<int>();
    public float[] damageIncreases = Array.Empty<float>();
    public bool IsTowerArea => kind == BuildingKind.Tower || kind == BuildingKind.TowerBuff;
    public bool HasOutput => !IsTowerArea && kind != BuildingKind.FactoryBuff;
}

[Serializable]
public class StageDefinition
{
    public string title;
    public float monsterHealth;
    public float spawnDelay;
    public float speed;
    public int monsterCount;
    public int clearGold;
    public string[] unlockBuildings = Array.Empty<string>();
}

[CreateAssetMenu(menuName = "Link Deploy/Game catalog", fileName = "LinkDeployCatalog")]
public class LinkDeployCatalog : ScriptableObject
{
    [Header("Economy and map")]
    public int startingGold = 2000;
    public int productionWidth = 15;
    public int productionHeight = 8;
    public int towerWidth = 3;
    public int towerHeight = 2;
    public int[] entranceColumns = { 0, 7, 14 };
    public float pathStart = -1;
    public float pathEnd = 15;
    public float pathHeight = -4;
    public float projectileSpeed = 22;
    public float slowDuration = 2;
    public float factoryBuffMultiplier = 1.25f;
    public float towerBuffMultiplier = 1.2f;
    [Header("Editable gameplay data")]
    public BuildingDefinition[] buildings;
    public StageDefinition[] stages;

    public BuildingDefinition Building(string id) => buildings.First(building => building.id == id);

    public void ApplyDocumentDefaults()
    {
        buildings = new[]
        {
            new BuildingDefinition { id = "Road", title = "도로", description = "재료를 출력 방향의 다음 칸으로 전달합니다. 어떤 재료든 통과하며, 막히면 앞 칸에서 기다립니다.", kind = BuildingKind.Road, spriteIndex = 12, price = 200, limit = -1, inputCapacity = 1, outputCapacity = 1 },
            new BuildingDefinition { id = "StoneMine", title = "돌 채굴장", description = "돌을 초당 5개 채굴합니다. 돌 공장으로 연결해 동그란 돌을 만드세요.", kind = BuildingKind.Mine, spriteIndex = 0, price = 100, output = MaterialKind.Stone, outputCapacity = 10, productionPerSecond = 5, limit = 3 },
            new BuildingDefinition { id = "IronMine", title = "쇠 채굴장", description = "쇠를 초당 5개 채굴합니다. 화살 공장에 원료를 공급합니다.", kind = BuildingKind.Mine, spriteIndex = 1, price = 200, output = MaterialKind.Iron, outputCapacity = 10, productionPerSecond = 5, limit = 3 },
            new BuildingDefinition { id = "StoneFactory", title = "돌 공장", description = "돌 1개를 동그란 돌 1개로 가공합니다. 새총의 탄약이자 총알의 재료입니다.", kind = BuildingKind.Factory, spriteIndex = 2, price = 200, output = MaterialKind.RoundStone, recipe = new[] { new Ingredient(MaterialKind.Stone) }, upgradePrices = new[] { 200, 300, 500 } },
            new BuildingDefinition { id = "ArrowFactory", title = "화살 공장", description = "쇠 1개를 화살 1개로 가공합니다. 궁수에 보급하거나 총알 공장으로 보냅니다.", kind = BuildingKind.Factory, spriteIndex = 3, price = 300, output = MaterialKind.Arrow, recipe = new[] { new Ingredient(MaterialKind.Iron) }, upgradePrices = new[] { 300, 400, 600 } },
            new BuildingDefinition { id = "AmmoFactory", title = "총알 공장", description = "화살 1개와 동그란 돌 1개를 총알 1개로 조합합니다. 재료의 강화 효과는 조합 시 사라집니다.", kind = BuildingKind.Factory, spriteIndex = 4, price = 400, output = MaterialKind.Bullet, recipe = new[] { new Ingredient(MaterialKind.Arrow), new Ingredient(MaterialKind.RoundStone) }, unlockAfterStage = 2, upgradePrices = new[] { 400, 500, 1000 } },
            new BuildingDefinition { id = "SlowEffect", title = "냉기 제련소", description = "선택한 투사체에 둔화 50%를 추가합니다. 여러 번 강화할 수 있으며 최대 둔화는 90%입니다.", kind = BuildingKind.Forge, spriteIndex = 5, price = 300, slow = .5f, unlockAfterStage = 1, upgradePrices = new[] { 300, 500, 700 } },
            new BuildingDefinition { id = "DamageEffect", title = "화력 제련소", description = "선택한 투사체에 공격력 50을 추가합니다. 다른 제련소의 효과와 중첩됩니다.", kind = BuildingKind.Forge, spriteIndex = 6, price = 400, bonusDamage = 50, unlockAfterStage = 1, upgradePrices = new[] { 400, 700, 1000 } },
            new BuildingDefinition { id = "FactoryBuff", title = "공장 가속소", description = "상하좌우에 인접한 채굴장 / 공장 / 제련소의 생산 속도를 25% 높입니다.", kind = BuildingKind.FactoryBuff, spriteIndex = 7, price = 400, limit = 2, unlockAfterStage = 3 },
            new BuildingDefinition { id = "Slingshot", title = "새총", description = "동그란 돌을 사용하는 타워. 가장 가까운 적을 조준하고 범위를 벗어나기 전까지 추적합니다.", kind = BuildingKind.Tower, spriteIndex = 8, price = 100, ammunition = MaterialKind.RoundStone, inputCapacity = 3, range = 6, attacksPerSecond = .7f, damage = 50, limit = 6, upgradePrices = new[] { 100, 150, 250 }, damageIncreases = new[] { 20f, 30f, 30f } },
            new BuildingDefinition { id = "Archer", title = "궁수", description = "화살을 사용하는 타워. 긴 사거리와 빠른 연사로 적을 방어합니다.", kind = BuildingKind.Tower, spriteIndex = 9, price = 200, ammunition = MaterialKind.Arrow, inputCapacity = 5, range = 10, attacksPerSecond = 1.2f, damage = 35, limit = 6, upgradePrices = new[] { 200, 300, 500 }, damageIncreases = new[] { 20f, 30f, 30f } },
            new BuildingDefinition { id = "Gun", title = "총", description = "총알을 사용하는 타워. 강력한 화력을 유지하려면 두 종류의 원료 공급이 필요합니다.", kind = BuildingKind.Tower, spriteIndex = 10, price = 600, ammunition = MaterialKind.Bullet, inputCapacity = 10, range = 15, attacksPerSecond = 2, damage = 50, limit = 6, unlockAfterStage = 2, upgradePrices = new[] { 600, 900, 1500 }, damageIncreases = new[] { 20f, 30f, 30f } },
            new BuildingDefinition { id = "TowerBuff", title = "타워 증폭기", description = "같은 타워 구역의 공격 속도를 20% 높입니다. 증폭기는 한 구역에서 한 번만 적용됩니다.", kind = BuildingKind.TowerBuff, spriteIndex = 11, price = 350, limit = 3, unlockAfterStage = 3 }
        };
        stages = new[]
        {
            Stage("숲의 입구",120,2,1,3,200,"SlowEffect","DamageEffect"),
            Stage("정찰대",150,1,2,5,300,"AmmoFactory","Gun"),
            Stage("돌격",200,1,4,7,500,"FactoryBuff","TowerBuff"),
            Stage("붉은 행렬",330,.7f,5,7,700),
            Stage("철갑 부대",500,.7f,5,8,1000),
            Stage("불타는 숲",800,.7f,5,9,1500),
            Stage("마지막 요새",1000,.7f,5,10,0)
        };
    }

    static StageDefinition Stage(string title, float health, float delay, float speed, int count, int gold, params string[] unlocks)
        => new StageDefinition { title = title, monsterHealth = health, spawnDelay = delay, speed = speed, monsterCount = count, clearGold = gold, unlockBuildings = unlocks };
}
