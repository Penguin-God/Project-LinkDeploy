#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SceneBuilderWindow : EditorWindow
{
    [MenuItem("Adventure/Build Scenes")]
    public static void BuildScenes()
    {
        GenerateScriptableObjects();
        
        // Build Lobby Scene
        var lobbyScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        lobbyScene.name = "Lobby";
        BuildLobbyScene();
        EditorSceneManager.SaveScene(lobbyScene, "Assets/0_Adventure/Deploy/Lobby.unity");
        
        // Build Defense Scene
        var defenseScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        defenseScene.name = "Defense";
        BuildDefenseScene();
        EditorSceneManager.SaveScene(defenseScene, "Assets/0_Adventure/Deploy/Defense.unity");
        
        // Update Build Settings
        var scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene("Assets/0_Adventure/Deploy/Lobby.unity", true),
            new EditorBuildSettingsScene("Assets/0_Adventure/Deploy/Defense.unity", true)
        };
        EditorBuildSettings.scenes = scenes.ToArray();
        
        // Load Lobby as start
        EditorSceneManager.OpenScene("Assets/0_Adventure/Deploy/Lobby.unity");
        
        Debug.Log("Lobby and Defense scenes created successfully!");
    }
    
    private static void BuildLobbyScene()
    {
        var mainCam = new GameObject("Main Camera");
        mainCam.tag = "MainCamera";
        var cam = mainCam.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 9f;
        mainCam.transform.position = new Vector3(7f, 0f, -10f);
        
        var gridManagerGo = new GameObject("GridManager");
        var gridManager = gridManagerGo.AddComponent<GridManager>();
        
        var gridRendererGo = new GameObject("GridRenderer");
        var gridRenderer = gridRendererGo.AddComponent<GridRenderer>();
        var tilesGo = new GameObject("Tiles");
        var buildingsGo = new GameObject("Buildings");
        tilesGo.transform.SetParent(gridRendererGo.transform);
        buildingsGo.transform.SetParent(gridRendererGo.transform);
        gridRenderer.tilesParent = tilesGo.transform;
        gridRenderer.buildingsParent = buildingsGo.transform;
        
        var buildManagerGo = new GameObject("BuildManager");
        var buildManager = buildManagerGo.AddComponent<BuildManager>();
        
        var supplyChainGo = new GameObject("SupplyChainManager");
        var supplyChain = supplyChainGo.AddComponent<SupplyChainManager>();
        
        var uiManagerGo = new GameObject("UIManager");
        var uiManager = uiManagerGo.AddComponent<UIManager>();
        
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();
        uiManagerGo.transform.SetParent(canvasGo.transform);
        
        // Top Bar
        var topBarGo = new GameObject("TopBarText");
        topBarGo.transform.SetParent(canvasGo.transform, false);
        var topBarText = topBarGo.AddComponent<TextMeshProUGUI>();
        topBarText.alignment = TextAlignmentOptions.Center;
        topBarText.fontSize = 24;
        topBarText.color = Color.black;
        var topBarRt = topBarGo.GetComponent<RectTransform>();
        topBarRt.anchorMin = new Vector2(0, 1);
        topBarRt.anchorMax = new Vector2(1, 1);
        topBarRt.pivot = new Vector2(0.5f, 1);
        topBarRt.anchoredPosition = new Vector2(0, -20);
        topBarRt.sizeDelta = new Vector2(-40, 50);
        uiManager.topBarText = topBarText;
        
        // Build Mode Buttons Container
        var buttonsContainerGo = new GameObject("ButtonsContainer");
        buttonsContainerGo.transform.SetParent(canvasGo.transform, false);
        var containerRt = buttonsContainerGo.AddComponent<RectTransform>();
        containerRt.anchorMin = new Vector2(0, 0);
        containerRt.anchorMax = new Vector2(1, 0);
        containerRt.pivot = new Vector2(0.5f, 0);
        containerRt.anchoredPosition = new Vector2(0, 20);
        containerRt.sizeDelta = new Vector2(-100, 80);
        var layoutGroup = buttonsContainerGo.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.spacing = 10;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = false;
        uiManager.buttonsContainer = buttonsContainerGo.transform;
        
        // Stage Container
        var stageContainerGo = new GameObject("StageContainer");
        stageContainerGo.transform.SetParent(canvasGo.transform, false);
        var stageContainerRt = stageContainerGo.AddComponent<RectTransform>();
        stageContainerRt.anchorMin = new Vector2(0.5f, 0.5f);
        stageContainerRt.anchorMax = new Vector2(0.5f, 0.5f);
        stageContainerRt.pivot = new Vector2(0.5f, 0.5f);
        stageContainerRt.anchoredPosition = new Vector2(0, 250);
        stageContainerRt.sizeDelta = new Vector2(600, 100);
        var stageLayout = stageContainerGo.AddComponent<HorizontalLayoutGroup>();
        stageLayout.childAlignment = TextAnchor.MiddleCenter;
        stageLayout.spacing = 20;
        
        // Cancel Button
        var cancelBtnGo = new GameObject("CancelButton");
        cancelBtnGo.transform.SetParent(canvasGo.transform, false);
        cancelBtnGo.AddComponent<Image>().color = new Color(1f, 0.4f, 0.4f);
        var cancelBtn = cancelBtnGo.AddComponent<Button>();
        var cancelRt = cancelBtnGo.GetComponent<RectTransform>();
        cancelRt.anchorMin = new Vector2(0.95f, 1);
        cancelRt.anchorMax = new Vector2(0.95f, 1);
        cancelRt.pivot = new Vector2(1f, 1);
        cancelRt.anchoredPosition = new Vector2(0, -20);
        cancelRt.sizeDelta = new Vector2(150, 50);
        var cancelTextGo = new GameObject("Text");
        cancelTextGo.transform.SetParent(cancelBtnGo.transform, false);
        var cancelText = cancelTextGo.AddComponent<TextMeshProUGUI>();
        cancelText.text = "Cancel";
        cancelText.color = Color.white;
        cancelText.alignment = TextAlignmentOptions.Center;
        cancelText.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 50);
        uiManager.cancelButton = cancelBtn;
        
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }
    
    private static void BuildDefenseScene()
    {
        var mainCam = new GameObject("Main Camera");
        mainCam.tag = "MainCamera";
        var cam = mainCam.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 9f;
        mainCam.transform.position = new Vector3(7f, 0f, -10f);
        
        var gridManagerGo = new GameObject("GridManager");
        var gridManager = gridManagerGo.AddComponent<GridManager>();
        
        var gridRendererGo = new GameObject("GridRenderer");
        var gridRenderer = gridRendererGo.AddComponent<GridRenderer>();
        var tilesGo = new GameObject("Tiles");
        var buildingsGo = new GameObject("Buildings");
        tilesGo.transform.SetParent(gridRendererGo.transform);
        buildingsGo.transform.SetParent(gridRendererGo.transform);
        gridRenderer.tilesParent = tilesGo.transform;
        gridRenderer.buildingsParent = buildingsGo.transform;
        
        var supplyChainGo = new GameObject("SupplyChainManager");
        var supplyChain = supplyChainGo.AddComponent<SupplyChainManager>();
        
        var defenseManagerGo = new GameObject("DefenseManager");
        var defenseManager = defenseManagerGo.AddComponent<DefenseManager>();
        
        var monsterManagerGo = new GameObject("MonsterManager");
        var monsterManager = monsterManagerGo.AddComponent<MonsterManager>();
        
        var projectileManagerGo = new GameObject("ProjectileManager");
        var projectileManager = projectileManagerGo.AddComponent<ProjectileManager>();
        
        var uiManagerGo = new GameObject("UIManager");
        var uiManager = uiManagerGo.AddComponent<UIManager>();
        
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();
        uiManagerGo.transform.SetParent(canvasGo.transform);
        
        var topBarGo = new GameObject("TopBarText");
        topBarGo.transform.SetParent(canvasGo.transform, false);
        var topBarText = topBarGo.AddComponent<TextMeshProUGUI>();
        topBarText.alignment = TextAlignmentOptions.Center;
        topBarText.fontSize = 24;
        topBarText.color = Color.black;
        var topBarRt = topBarGo.GetComponent<RectTransform>();
        topBarRt.anchorMin = new Vector2(0, 1);
        topBarRt.anchorMax = new Vector2(1, 1);
        topBarRt.pivot = new Vector2(0.5f, 1);
        topBarRt.anchoredPosition = new Vector2(0, -20);
        topBarRt.sizeDelta = new Vector2(-40, 50);
        uiManager.topBarText = topBarText;
        
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }
    
    private static void GenerateScriptableObjects()
    {
        if (!AssetDatabase.IsValidFolder("Assets/0_Adventure/Deploy/Resources"))
            AssetDatabase.CreateFolder("Assets/0_Adventure/Deploy", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/0_Adventure/Deploy/Resources/Buildings"))
            AssetDatabase.CreateFolder("Assets/0_Adventure/Deploy/Resources", "Buildings");
        if (!AssetDatabase.IsValidFolder("Assets/0_Adventure/Deploy/Resources/Monsters"))
            AssetDatabase.CreateFolder("Assets/0_Adventure/Deploy/Resources", "Monsters");
        if (!AssetDatabase.IsValidFolder("Assets/0_Adventure/Deploy/Resources/Stages"))
            AssetDatabase.CreateFolder("Assets/0_Adventure/Deploy/Resources", "Stages");
            
        // Mines
        CreateBuildingData("StoneMine", BuildingType.Mine, 0, ResourceType.Gold, 1, 0.1f, ResourceType.None, ResourceType.None, 0, ResourceType.Stone, 50, desc: "돌을 캐는 광산");
        CreateBuildingData("IronMine", BuildingType.Mine, 0, ResourceType.Gold, 1, 0.1f, ResourceType.None, ResourceType.None, 0, ResourceType.Iron, 50, desc: "철을 캐는 광산");
        CreateBuildingData("WoodMine", BuildingType.Mine, 0, ResourceType.Gold, 1, 0.1f, ResourceType.None, ResourceType.None, 0, ResourceType.Wood, 50, desc: "나무를 캐는 광산");
        
        // Village Core
        CreateBuildingData("TownHall", BuildingType.Hall, 0, ResourceType.Gold, 1, 0, ResourceType.None, ResourceType.None, 0, ResourceType.None, 0, desc: "마을 회관. 채굴 자원이 여기로 모입니다.", allowedZones: new[] { ZoneType.Village }, dName: "마을 회관");
        
        // Entrance & Road
        CreateBuildingData("Entrance", BuildingType.Entrance, 0, ResourceType.Gold, 1, 0, ResourceType.None, ResourceType.None, 0, ResourceType.None, 0, desc: "타워 구역 입구", dName: "입구");
        CreateBuildingData("Road", BuildingType.Road, 200, ResourceType.Gold, 1, 0, ResourceType.None, ResourceType.None, 0, ResourceType.None, 0, desc: "건물 간 재료들의 연결 범위를 늘려줌", allowedZones: new[] { ZoneType.Village, ZoneType.Defense }, dName: "도로");
        
        // Factories
        CreateBuildingData("HammerFactory", BuildingType.Factory, 150, ResourceType.Gold, 1, 1f, ResourceType.Wood, ResourceType.Iron, 5, ResourceType.Hammer, 5, desc: "나무와 쇠로 망치를 만드는 공장", allowedZones: new[] { ZoneType.Village }, dName: "망치 공장");
        CreateBuildingData("StoneFactory", BuildingType.Factory, 100, ResourceType.Wood, 1, 1f, ResourceType.Stone, ResourceType.None, 5, ResourceType.RoundStone, 5, desc: "돌을 동그란돌로 만드는 공장", allowedZones: new[] { ZoneType.Defense }, dName: "돌 공장");
        CreateBuildingData("ArrowFactory", BuildingType.Factory, 150, ResourceType.Iron, 1, 1f, ResourceType.Iron, ResourceType.None, 5, ResourceType.Arrow, 5, desc: "철을 화살로 만드는 공장", allowedZones: new[] { ZoneType.Defense }, dName: "화살 공장");
        CreateBuildingData("AmmoFactory", BuildingType.Factory, 400, ResourceType.Hammer, 1, 1f, ResourceType.Arrow, ResourceType.RoundStone, 5, ResourceType.GunAmmo, 5, desc: "화살, 동그란 돌을 총알로 만드는 공장", allowedZones: new[] { ZoneType.Defense }, dName: "총알 공장");
        CreateBuildingData("SlowEffect", BuildingType.Smeltery, 300, ResourceType.Gold, 1, 1f, ResourceType.None, ResourceType.None, 5, ResourceType.None, 5, desc: "투사체에 슬로우 효과 50%를 부여", allowedZones: new[] { ZoneType.Defense }, dName: "냉기 제련소");
        CreateBuildingData("DamageEffect", BuildingType.Smeltery, 400, ResourceType.Gold, 1, 1f, ResourceType.None, ResourceType.None, 5, ResourceType.None, 5, desc: "투사체에 공격력 50 증가", allowedZones: new[] { ZoneType.Defense }, dName: "화력 제련소");
        
        // Buffs
        CreateBuildingData("FactoryBuff", BuildingType.FactorySpeedBuff, 300, ResourceType.Hammer, 1, 0, ResourceType.None, ResourceType.None, 0, ResourceType.None, 0, buffAmt: 1f, buffRng: 1, desc: "주변 공장의 생산 속도를 올려줌", allowedZones: new[] { ZoneType.Defense }, dName: "공장 가속기");
        CreateBuildingData("TowerBuff", BuildingType.TowerAttackBuff, 400, ResourceType.Gold, 1, 0, ResourceType.None, ResourceType.None, 0, ResourceType.None, 0, buffAmt: 0.5f, buffRng: 1, desc: "주변 타워의 공격력을 올려줌", allowedZones: new[] { ZoneType.Tower }, dName: "타워 강화소");

        // Towers
        CreateBuildingData("Slingshot", BuildingType.Tower, 100, ResourceType.Wood, 1, 0, ResourceType.None, ResourceType.None, 0, ResourceType.None, 0, atk: 50f, atkSpd: 0.7f, atkRange: 6, maxAmmo: 3, reqAmmo: ResourceType.RoundStone, desc: "동그란 돌을 사용하는 타워", allowedZones: new[] { ZoneType.Tower }, dName: "새총");
        CreateBuildingData("Archer", BuildingType.Tower, 200, ResourceType.Iron, 1, 0, ResourceType.None, ResourceType.None, 0, ResourceType.None, 0, atk: 35f, atkSpd: 1.2f, atkRange: 10, maxAmmo: 5, reqAmmo: ResourceType.Arrow, desc: "화살 사용하는 타워", allowedZones: new[] { ZoneType.Tower }, dName: "궁수");
        CreateBuildingData("Gun", BuildingType.Tower, 600, ResourceType.Hammer, 1, 0, ResourceType.None, ResourceType.None, 0, ResourceType.None, 0, atk: 50f, atkSpd: 2f, atkRange: 15, maxAmmo: 10, reqAmmo: ResourceType.GunAmmo, desc: "총알 사용하는 타워", allowedZones: new[] { ZoneType.Tower }, dName: "총");

        
        // Upgrades
        AddUpgrades("HammerFactory", new[] { new UpgradeInfo { level = 2, cost = 250, costType = ResourceType.Wood, effectAmount = 0.1f }, new UpgradeInfo { level = 3, cost = 600, costType = ResourceType.Wood, effectAmount = 0.1f } });
        AddUpgrades("StoneFactory", new[] { new UpgradeInfo { level = 2, cost = 200, costType = ResourceType.Wood, effectAmount = 0.1f }, new UpgradeInfo { level = 3, cost = 500, costType = ResourceType.Wood, effectAmount = 0.1f } });
        AddUpgrades("ArrowFactory", new[] { new UpgradeInfo { level = 2, cost = 300, costType = ResourceType.Iron, effectAmount = 0.1f }, new UpgradeInfo { level = 3, cost = 700, costType = ResourceType.Iron, effectAmount = 0.1f } });
        AddUpgrades("AmmoFactory", new[] { new UpgradeInfo { level = 2, cost = 500, costType = ResourceType.Hammer, effectAmount = 0.1f }, new UpgradeInfo { level = 3, cost = 1000, costType = ResourceType.Hammer, effectAmount = 0.1f } });
        AddUpgrades("SlowEffect", new[] { new UpgradeInfo { level = 2, cost = 500, costType = ResourceType.Gold, effectAmount = 0.1f }, new UpgradeInfo { level = 3, cost = 700, costType = ResourceType.Gold, effectAmount = 0.1f } });
        AddUpgrades("DamageEffect", new[] { new UpgradeInfo { level = 2, cost = 700, costType = ResourceType.Gold, effectAmount = 10f }, new UpgradeInfo { level = 3, cost = 1000, costType = ResourceType.Gold, effectAmount = 10f } });
        
        AddUpgrades("FactoryBuff", new[] { new UpgradeInfo { level = 2, cost = 500, costType = ResourceType.Hammer, effectAmount = 0.1f }, new UpgradeInfo { level = 3, cost = 700, costType = ResourceType.Hammer, effectAmount = 0.1f } });
        AddUpgrades("TowerBuff", new[] { new UpgradeInfo { level = 2, cost = 700, costType = ResourceType.Gold, effectAmount = 0.1f }, new UpgradeInfo { level = 3, cost = 1000, costType = ResourceType.Gold, effectAmount = 0.1f } });
        
        AddUpgrades("Slingshot", new[] { new UpgradeInfo { level = 2, cost = 150, costType = ResourceType.Wood, effectAmount = 20f }, new UpgradeInfo { level = 3, cost = 250, costType = ResourceType.Wood, effectAmount = 30f } });
        AddUpgrades("Archer", new[] { new UpgradeInfo { level = 2, cost = 300, costType = ResourceType.Iron, effectAmount = 20f }, new UpgradeInfo { level = 3, cost = 500, costType = ResourceType.Iron, effectAmount = 30f } });
        AddUpgrades("Gun", new[] { new UpgradeInfo { level = 2, cost = 900, costType = ResourceType.Hammer, effectAmount = 20f }, new UpgradeInfo { level = 3, cost = 1500, costType = ResourceType.Hammer, effectAmount = 30f } });
        
        AddUpgrades("TownHall", new[] { new UpgradeInfo { level = 2, cost = 1000, costType = ResourceType.Gold, effectAmount = 0f }, new UpgradeInfo { level = 3, cost = 3000, costType = ResourceType.Gold, effectAmount = 0f }, new UpgradeInfo { level = 4, cost = 5000, costType = ResourceType.Gold, effectAmount = 0f } });
        
        var monsterPath = "Assets/0_Adventure/Deploy/Resources/Monsters/BasicMonster.asset";
        var monsterData = AssetDatabase.LoadAssetAtPath<MonsterDataSO>(monsterPath);
        if (monsterData == null)
        {
            monsterData = ScriptableObject.CreateInstance<MonsterDataSO>();
            AssetDatabase.CreateAsset(monsterData, monsterPath);
        }
        monsterData.maxHp = 150f;
        monsterData.speed = 1f;
        monsterData.rewardGold = 20;
        EditorUtility.SetDirty(monsterData);
        
        var settingsPath = "Assets/0_Adventure/Deploy/Resources/GameSettings.asset";
        var settings = AssetDatabase.LoadAssetAtPath<GameSettingsSO>(settingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<GameSettingsSO>();
            AssetDatabase.CreateAsset(settings, settingsPath);
        }
        settings.startingGold = 10000;
        settings.monsterSpawnDelay = 1f;
        settings.monsterBaseHp = 150f;
        settings.monsterHpIncreaseStep = 10;
        settings.monsterHpIncreasePercent = 0.1f;
        EditorUtility.SetDirty(settings);
        
        // Generate StageDataSO
        CreateStageData(1, 120f, 2.0f, 1f, 3, 1000, new List<BuildingCount>());
        CreateStageData(2, 150f, 1.0f, 2f, 5, 1000, new List<BuildingCount>());
        CreateStageData(3, 200f, 1.0f, 4f, 7, 1000, new List<BuildingCount>());
        CreateStageData(4, 330f, 0.7f, 5f, 7, 1000, new List<BuildingCount>());
        CreateStageData(5, 500f, 0.7f, 5f, 8, 1000, new List<BuildingCount>());
        CreateStageData(6, 800f, 0.7f, 5f, 9, 1000, new List<BuildingCount>());
        CreateStageData(7, 1000f, 0.7f, 5f, 10, 1000, new List<BuildingCount>());
        
        AssetDatabase.SaveAssets();
    }
    
    private static void CreateStageData(int stageNum, float hp, float delay, float speed, int count, int clearRewardGold, List<BuildingCount> rewards)
    {
        string path = $"Assets/0_Adventure/Deploy/Resources/Stages/Stage_{stageNum}.asset";
        var s = AssetDatabase.LoadAssetAtPath<StageDataSO>(path);
        if (s == null)
        {
            s = ScriptableObject.CreateInstance<StageDataSO>();
            AssetDatabase.CreateAsset(s, path);
        }
        s.stageNumber = stageNum;
        s.monsterHp = hp;
        s.monsterSpeed = speed;
        s.spawnDelay = delay;
        s.totalMonsters = count;
        s.clearRewardGold = clearRewardGold;
        s.buildingRewards = rewards;
        EditorUtility.SetDirty(s);
    }
    
    private static void CreateBuildingData(
        string name, BuildingType type, int cost, ResourceType costType, int connRange, 
        float prodTime, ResourceType in1, ResourceType in2, int maxIn, 
        ResourceType outType, int maxOut, 
        float buffAmt = 0, int buffRng = 0, 
        float atk = 0, float atkSpd = 0, float atkRange = 0, int maxAmmo = 0, ResourceType reqAmmo = ResourceType.None, string desc = "",
        ZoneType[] allowedZones = null, string dName = null)
    {
        string path = $"Assets/0_Adventure/Deploy/Resources/Buildings/{name}.asset";
        var b = AssetDatabase.LoadAssetAtPath<BuildingDataSO>(path);
        if (b == null)
        {
            b = ScriptableObject.CreateInstance<BuildingDataSO>();
            AssetDatabase.CreateAsset(b, path);
        }
        b.buildingName = name;
        b.displayName = string.IsNullOrEmpty(dName) ? name : dName;
        b.buildingType = type;
        b.cost = cost;
        b.costType = costType;
        b.connectionRange = connRange;
        
        b.productionTime = prodTime;
        b.inputType1 = in1;
        b.inputType2 = in2;
        b.maxInputCapacity = maxIn;
        b.outputType = outType;
        b.maxOutputCapacity = maxOut;
        
        b.buffAmount = buffAmt;
        b.buffRange = buffRng;
        
        b.attackDamage = atk;
        b.attackSpeed = atkSpd;
        b.attackRange = atkRange;
        b.maxAmmo = maxAmmo;
        b.requiredAmmoType = reqAmmo;
        
        if (allowedZones != null)
        {
            b.allowedZones = new System.Collections.Generic.List<ZoneType>(allowedZones);
        }
        else
        {
            b.allowedZones.Clear();
        }
        
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/0_Adventure/Deploy/Resources/Sprites/{name}.png");
        if (sprite != null) b.sprite = sprite;
        
        b.description = desc;
        
        EditorUtility.SetDirty(b);
    }
    
    private static void AddUpgrades(string name, UpgradeInfo[] upgrades)
    {
        var path = $"Assets/0_Adventure/Deploy/Resources/Buildings/{name}.asset";
        var b = AssetDatabase.LoadAssetAtPath<BuildingDataSO>(path);
        if (b != null)
        {
            b.upgrades = new List<UpgradeInfo>(upgrades);
            EditorUtility.SetDirty(b);
        }
    }
}
#endif
