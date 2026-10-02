using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static GameGraphics;

public partial class LinkDeployGame : MonoBehaviour
{
    const float ReferenceWidth = 1920, ReferenceHeight = 1080;
    public const float GridLeft = 424, GridTop = 170, CellSize = 72;
    const string SaveKey = "LinkDeploy.Save.v1";
    LinkDeployCatalog catalog;
    GameState state;
    RectTransform root, boardLayer, cellLayer, unitLayer, effectsLayer, modalLayer, catalogLayer, inspectorLayer, leftDetailLayer;
    Sprite[] sprites;
    Sprite landscape;
    PipelineGraphic pipes;
    PipelineGraphic flowMarkers;
    RangeGraphic range;
    Image preview;
    TextMeshProUGUI goldText, waveText, statusText, hintText, monsterText, supplyText, speedText, pauseText, toastText, constructionToggle;
    Image waveFill;
    Button[] stageButtons;
    readonly List<Image> cells = new List<Image>();
    readonly Dictionary<int, BuildingView> buildingViews = new Dictionary<int, BuildingView>();
    readonly Dictionary<int, MonsterView> monsterViews = new Dictionary<int, MonsterView>();
    readonly Dictionary<int, Image> projectileViews = new Dictionary<int, Image>();
    readonly Dictionary<string, Button> catalogButtons = new Dictionary<string, Button>();
    readonly Dictionary<string, TextMeshProUGUI> stockTexts = new Dictionary<string, TextMeshProUGUI>();
    string buildId;
    int selectedId = -1, dragId = -1, pressedId = -1;
    int category;
    FlowDirection direction = FlowDirection.Down;
    Vector2 pressPosition;
    bool paused, fast, helpOpen, onboarding;
    bool constructionOpen = true;
    float toastUntil, hudTimer, autosaveTimer;
    SessionMode displayedMode;
    AudioSource sound;
    AudioClip shot;
    int lastShots;
    bool smokeTest;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindFirstObjectByType<LinkDeployGame>() == null) new GameObject("Link Deploy").AddComponent<LinkDeployGame>();
    }

    void Awake()
    {
        Application.targetFrameRate = 60;
        Application.runInBackground = true;
        catalog = Resources.Load<LinkDeployCatalog>("LinkDeployCatalog");
        if (catalog == null) { catalog = ScriptableObject.CreateInstance<LinkDeployCatalog>(); catalog.ApplyDocumentDefaults(); }
        smokeTest = Environment.GetCommandLineArgs().Contains("--smoke-test");
        state = LoadState() ?? LinkDeploySimulation.Create(catalog);
        displayedMode = state.mode;
        LoadArt(); CreateInterface();
        sound = gameObject.AddComponent<AudioSource>(); sound.volume = .18f; shot = Resources.Load<AudioClip>("Sounds/Shot");
        RefreshCatalog(); RefreshInspector(); RenderWorld(); UpdateHud();
        if (smokeTest) StartCoroutine(SmokeRun());
        else if (state.buildings.Count == 0) ShowWelcome();
    }

    void LoadArt()
    {
        GameGraphics.Font = Resources.Load<TMP_FontAsset>("Font/MaplestoryBold");
        if (GameGraphics.Font != null)
        {
            var shader = Shader.Find("TextMeshPro/Distance Field");
            if (shader != null) GameGraphics.Font.material.shader = shader;
        }
        var atlas = Resources.Load<Texture2D>("Art/FoundryAtlas");
        sprites = new Sprite[16];
        if (atlas != null)
        {
            atlas.filterMode = FilterMode.Point;
            float width = atlas.width / 4f, height = atlas.height / 4f;
            for (int index = 0; index < sprites.Length; index++)
                sprites[index] = Sprite.Create(atlas, new Rect(index % 4 * width, (3 - index / 4) * height, width, height), Vector2.one * .5f, width);
        }
        var background = Resources.Load<Texture2D>("Art/ForestFortress");
        if (background != null) landscape = Sprite.Create(background,new Rect(0,0,background.width,background.height),Vector2.one * .5f);
    }

    GameState LoadState()
    {
        if (smokeTest || !PlayerPrefs.HasKey(SaveKey)) return null;
        try
        {
            var loaded = JsonUtility.FromJson<GameState>(PlayerPrefs.GetString(SaveKey));
            if (loaded?.cleared == null || loaded.cleared.Length != catalog.stages.Length || loaded.buildings.Any(building => !catalog.buildings.Any(definition => definition.id == building.definitionId))) return null;
            return LinkDeploySimulation.ReturnToPlanning(loaded);
        }
        catch (Exception) { return null; }
    }

    void Save()
    {
        if (smokeTest) return;
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(LinkDeploySimulation.ReturnToPlanning(state))); PlayerPrefs.Save();
    }
    void OnApplicationQuit() => Save();
    void OnApplicationPause(bool suspended) { if (suspended) Save(); }

    void Update()
    {
        HandleInput();
        if (!paused && !helpOpen && !onboarding)
        {
            float remaining = Mathf.Min(Time.unscaledDeltaTime, .1f) * (fast ? 2 : 1);
            while (remaining > 0) { float step = Mathf.Min(remaining, .025f); state = LinkDeploySimulation.Step(state, catalog, step); remaining -= step; }
        }
        if (lastShots < state.totalShots && shot != null && !smokeTest) sound.PlayOneShot(shot);
        lastShots = state.totalShots;
        RenderWorld();
        hudTimer += Time.unscaledDeltaTime;
        if (hudTimer >= .15f) { UpdateHud(); hudTimer = 0; }
        if (state.mode != displayedMode)
        {
            displayedMode = state.mode; RefreshCatalog(); RefreshInspector();
            if (state.mode == SessionMode.Victory || state.mode == SessionMode.Defeat) { ShowResult(); Save(); }
        }
        autosaveTimer += Time.unscaledDeltaTime;
        if (autosaveTimer > 20 && state.mode == SessionMode.Planning) { Save(); autosaveTimer = 0; }
        toastText.gameObject.SetActive(Time.unscaledTime < toastUntil);
        hintText.gameObject.SetActive(Time.unscaledTime >= toastUntil);
    }

    Vector2 Pointer()
    {
        if (Mouse.current == null) return new Vector2(-1000,-1000);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, Mouse.current.position.ReadValue(), null, out var local);
        return new Vector2(local.x, -local.y);
    }

    bool HitCell(Vector2 point, out int zone, out int column, out int row)
    {
        zone = -1; column = Mathf.FloorToInt((point.x - GridLeft) / CellSize); row = catalog.productionHeight - 1 - Mathf.FloorToInt((point.y - GridTop) / CellSize);
        if (column >= 0 && column < catalog.productionWidth && row >= 0 && row < catalog.productionHeight) return true;
        for (int index = 0; index < catalog.entranceColumns.Length; index++)
            for (int towerRow = 0; towerRow < catalog.towerHeight; towerRow++)
                for (int towerColumn = 0; towerColumn < catalog.towerWidth; towerColumn++)
                {
                    var center = BuildingPoint(index,towerColumn,towerRow);
                    if (Mathf.Abs(point.x - center.x) < CellSize * .5f && Mathf.Abs(point.y - center.y) < CellSize * .5f)
                    { zone = index; column = towerColumn; row = towerRow; return true; }
                }
        return false;
    }

    void HandleInput()
    {
        var keyboard = Keyboard.current; var mouse = Mouse.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            if (helpOpen || onboarding) { helpOpen = onboarding = false; ClearModal(); }
            else CancelBuild();
        }
        if (helpOpen || onboarding || state.mode == SessionMode.Victory || state.mode == SessionMode.Defeat) return;
        if (keyboard != null)
        {
            if (keyboard.spaceKey.wasPressedThisFrame) TogglePause();
            if (keyboard.bKey.wasPressedThisFrame) ToggleConstruction();
            if (keyboard.rKey.wasPressedThisFrame)
            {
                if (buildId != null) direction = (FlowDirection)(((int)direction + 1) % 4);
                else if (selectedId >= 0) { state = LinkDeploySimulation.Rotate(state,catalog,selectedId); RefreshInspector(); }
            }
            if (keyboard.deleteKey.wasPressedThisFrame && selectedId >= 0) SellSelected();
        }
        if (mouse == null) return;
        if (mouse.rightButton.wasPressedThisFrame) CancelBuild();
        Vector2 point = Pointer();
        bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        bool hit = HitCell(point,out int zone,out int column,out int row);
        if (mouse.leftButton.wasPressedThisFrame && !overUi)
        {
            if (hit && buildId != null)
            {
                var error = LinkDeploySimulation.PlacementError(state,catalog,catalog.Building(buildId),zone,column,row);
                if (error != null) Toast(error);
                else { state = LinkDeploySimulation.Place(state,catalog,buildId,zone,column,row,direction); selectedId = state.buildings.Last().id; RefreshInspector(); RefreshCatalogAvailability(); Save(); }
            }
            else if (hit)
            {
                var building = LinkDeploySimulation.At(state,zone,column,row);
                selectedId = building?.id ?? -1; pressedId = selectedId; pressPosition = point; RefreshInspector();
            }
            else { selectedId = -1; RefreshInspector(); }
        }
        if (mouse.leftButton.isPressed && pressedId >= 0 && LinkDeploySimulation.CanEdit(state) && Vector2.Distance(point,pressPosition) > 12) dragId = pressedId;
        if (mouse.leftButton.wasReleasedThisFrame)
        {
            if (dragId >= 0)
            {
                var building = state.buildings.First(candidate => candidate.id == dragId);
                if (hit && !overUi)
                {
                    var error = LinkDeploySimulation.PlacementError(state,catalog,catalog.Building(building.definitionId),zone,column,row,dragId);
                    if (error != null) Toast(error); else { state = LinkDeploySimulation.Move(state,catalog,dragId,zone,column,row); Save(); }
                }
                else Toast("빈 칸 위에 놓으면 이동합니다.");
            }
            dragId = pressedId = -1;
        }
    }

    void ChooseBuilding(string id)
    {
        if (!LinkDeploySimulation.CanEdit(state)) return;
        buildId = id; selectedId = -1; RefreshInspector(); RefreshCatalogAvailability();
        Toast("설치할 칸을 클릭하세요. R 회전  /  우클릭 취소");
    }
    void CancelBuild() { buildId = null; dragId = pressedId = -1; RefreshInspector(); RefreshCatalogAvailability(); }
    void ToggleConstruction()
    {
        constructionOpen=!constructionOpen;
        if(!constructionOpen)CancelBuild();
        catalogLayer.gameObject.SetActive(constructionOpen);
        constructionToggle.text=constructionOpen?"닫기 B":"열기 B";
    }
    void TogglePause() { paused = !paused; UpdateHud(); }
    void ToggleSpeed() { fast = !fast; UpdateHud(); }
    void Toast(string message) { toastText.text = message; toastUntil = Time.unscaledTime + 3.5f; toastText.gameObject.SetActive(true); }
    void SellSelected()
    {
        if (!LinkDeploySimulation.CanEdit(state)) { Toast("전투가 끝난 후 회수할 수 있습니다."); return; }
        state = LinkDeploySimulation.Sell(state,selectedId); selectedId = -1; RefreshInspector(); RefreshCatalogAvailability(); Save();
    }
    void UpgradeSelected()
    {
        var next = LinkDeploySimulation.Upgrade(state,catalog,selectedId);
        if (ReferenceEquals(next,state)) { Toast("골드 또는 강화 조건을 확인하세요."); return; }
        state = next; RefreshInspector(); RefreshCatalogAvailability(); Save(); Toast("건물을 강화했습니다.");
    }
    void StartStage(int index)
    {
        if (!LinkDeploySimulation.CanEdit(state)) return;
        state = LinkDeploySimulation.StartStage(state,catalog,index); paused = false; CancelBuild(); UpdateHud();
    }
    void PrepareStarter()
    {
        ClearModal(); onboarding = false;
        if (state.buildings.Count > 0) { Toast("추천 배치는 빈 지도에서 사용할 수 있습니다."); return; }
        var prepared = LinkDeploySimulation.StarterLayout(state,catalog);
        if(ReferenceEquals(prepared,state)) { Toast("추천 배치에 필요한 골드와 설치 한도를 확인하세요."); return; }
        state = prepared; RefreshCatalogAvailability(); Save();
        Toast("보급 준비 완료 / 탄약이 차면 스테이지 1을 시작하세요.");
    }
}
