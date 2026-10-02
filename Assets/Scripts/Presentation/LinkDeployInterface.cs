using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static GameGraphics;

public partial class LinkDeployGame
{
    void CreateInterface()
    {
        var canvasObject = new GameObject("Interface",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth,ReferenceHeight); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var canvasRect = canvasObject.GetComponent<RectTransform>();
        var backdrop = Image("Backdrop",canvasRect,0,0,ReferenceWidth,ReferenceHeight,Ink);
        backdrop.rectTransform.anchorMin = Vector2.zero; backdrop.rectTransform.anchorMax = Vector2.one;
        backdrop.rectTransform.offsetMin = backdrop.rectTransform.offsetMax = Vector2.zero;
        root = Rect("Game",canvasRect,0,0,ReferenceWidth,ReferenceHeight);
        root.anchorMin = root.anchorMax = new Vector2(.5f,.5f); root.anchoredPosition = new Vector2(-ReferenceWidth * .5f,ReferenceHeight * .5f);
        if (FindFirstObjectByType<EventSystem>() == null) new GameObject("Input",typeof(EventSystem),typeof(InputSystemUIInputModule));

        Box("Header",root,24,22,1872,80);
        Text("Brand",root,45,31,280,34,"LINK <color=#F6B74B>DEPLOY</color>",30);
        Text("Subtitle",root,47,69,280,20,"공급망을 설계하고, 요새를 지켜라",14,Muted);
        Text("ModeLabel",root,376,36,100,20,"OPERATION",12,Muted);
        statusText = Text("Mode",root,376,58,160,28,"건설 준비",23,Green);
        waveText = Text("Wave",root,555,36,440,28,"보급을 연결한 뒤 전투를 시작하세요",20);
        Image("ProgressTrack",root,557,75,398,5,new Color32(35,55,65,255));
        waveFill = Image("Progress",root,557,75,0,5,Cyan);
        Text("Breach",root,1010,36,160,45,"돌파 허용 <color=#EB6757>0</color>",22);
        goldText = Text("Gold",root,1210,36,220,44,"2,000 G",29,Gold,TextAlignmentOptions.MidlineRight);
        var pauseButton = Button("Pause",root,1475,38,82,46,"일시정지",TogglePause,null,16); pauseText = pauseButton.GetComponentInChildren<TextMeshProUGUI>();
        var speedButton = Button("Speed",root,1567,38,72,46,"x1",ToggleSpeed,null,23); speedText = speedButton.GetComponentInChildren<TextMeshProUGUI>();
        Button("Help",root,1649,38,100,46,"플레이 방법",ShowHelp,null,15);
        Button("Settings",root,1759,38,112,46,"메뉴",ShowMenu,null,17);

        var map = Box("Map",root,346,120,1218,906,new Color32(28,43,36,255));
        var mapClip = Rect("Map clip",map,1,1,1216,904); mapClip.gameObject.AddComponent<RectMask2D>();
        if(landscape!=null)
        {
            TerrainStrip(mapClip,70,450,0,626);
            TerrainStrip(mapClip,520,80,626,66);
            TerrainStrip(mapClip,600,160,692,140);
            TerrainStrip(mapClip,760,100,832,72);
        }
        Image("Map shade",mapClip,0,0,1216,904,new Color(0, .02f, .04f,.08f));
        boardLayer = Rect("Board",root,0,0,ReferenceWidth,ReferenceHeight);
        Text("ProductionLabel",root,374,134,350,25,"01   생산 구역",19,Green);
        Text("ProductionSize",root,1270,136,258,22,"15 x 8  /  SUPPLY NETWORK",13,Paper,TextAlignmentOptions.MidlineRight);
        // Slight tint visually separates the buildable field without drawing a permanent grid.
        Image("Field tint",boardLayer,GridLeft,GridTop,catalog.productionWidth * CellSize,catalog.productionHeight * CellSize,new Color(.1f,.22f,.12f,.10f));
        cellLayer = Rect("Placement cells",root,0,0,ReferenceWidth,ReferenceHeight);
        for (int row = 0; row < catalog.productionHeight; row++)
            for (int column = 0; column < catalog.productionWidth; column++)
            {
                var point = BuildingPoint(-1,column,row);
                cells.Add(Image("Production cell",cellLayer,point.x - CellSize/2 + 1,point.y - CellSize/2 + 1,CellSize - 2,CellSize - 2,Color.clear));
            }
        for (int zone = 0; zone < catalog.entranceColumns.Length; zone++)
        {
            var entrance = BuildingPoint(-1,catalog.entranceColumns[zone],0);
            Image("Supply gate",boardLayer,entrance.x-38,entrance.y-38,76,76,Color.white,sprites[13]);
            Text("Gate label",boardLayer,entrance.x-36,entrance.y+24,72,22,$"보급 {zone+1}",12,Cyan,TextAlignmentOptions.Center);
            var upperLeft = BuildingPoint(zone,0,1) - Vector2.one * CellSize/2;
            Frame("Courtyard",boardLayer,upperLeft.x-3,upperLeft.y-3,CellSize*3+6,CellSize*2+6,new Color(.04f,.07f,.07f,.2f),new Color(.88f,.62f,.25f,.55f));
            Text("Zone name",boardLayer,upperLeft.x,upperLeft.y-32,216,24,$"0{zone+2}   타워 구역 {zone+1}",16,Gold,TextAlignmentOptions.Center);
            for (int row = 0; row < catalog.towerHeight; row++)
                for (int column = 0; column < catalog.towerWidth; column++)
                {
                    var point = BuildingPoint(zone,column,row);
                    cells.Add(Image("Tower cell",cellLayer,point.x-CellSize/2+1,point.y-CellSize/2+1,CellSize-2,CellSize-2,Color.clear));
                }
        }
        pipes = Rect("Connections",root,0,0,ReferenceWidth,ReferenceHeight).gameObject.AddComponent<PipelineGraphic>(); pipes.raycastTarget = false;
        var rangeClip = Rect("Range clip",root,347,121,1216,904); rangeClip.gameObject.AddComponent<RectMask2D>();
        range = Rect("Attack range",rangeClip,0,0,100,100).gameObject.AddComponent<RangeGraphic>(); range.raycastTarget = false; range.color = new Color(.45f,.87f,.82f,.55f);
        unitLayer = Rect("Buildings and enemies",root,0,0,ReferenceWidth,ReferenceHeight);
        flowMarkers = Rect("Cargo flow",root,0,0,ReferenceWidth,ReferenceHeight).gameObject.AddComponent<PipelineGraphic>();
        flowMarkers.drawTracks = false; flowMarkers.raycastTarget = false;
        effectsLayer = Rect("Projectiles",root,0,0,ReferenceWidth,ReferenceHeight);
        preview = Image("Build preview",effectsLayer,0,0,CellSize,CellSize,new Color(1,1,1,.65f));
        hintText = Text("Map hint",root,585,134,670,27,"생산 건물의 출력 방향을 보급 입구까지 연결하세요.",14,Paper,TextAlignmentOptions.Center);
        Text("Enemy route",root,378,971,340,20,"진입  >",14,Gold);
        Text("Destination",root,1438,971,104,20,"방어선  >",14,Red,TextAlignmentOptions.MidlineRight);

        var stages = Box("Stages",root,24,120,300,160);
        Text("Title",stages,16,9,268,30,"스테이지 선택",21);
        Text("Caption",stages,16,40,268,24,"차례대로 클리어하여 건물을 해금하세요",12,Muted);
        stageButtons = new Button[catalog.stages.Length];
        for (int index = 0; index < stageButtons.Length; index++)
        {
            int stageIndex = index;
            stageButtons[index] = Button("Stage "+(index+1),stages,16+index*39,77,34,47,(index+1).ToString(),()=>StartStage(stageIndex),null,19);
        }
        Text("Stage note",stages,16,128,268,22,"버튼을 누르면 바로 전투가 시작됩니다",12,Muted);

        var construction = Box("Construction",root,24,296,300,438);
        Text("Title",construction,16,10,178,32,"건축 모드",23);
        constructionToggle=Button("BuildMode",construction,203,12,83,30,"닫기 B",ToggleConstruction,null,14).GetComponentInChildren<TextMeshProUGUI>();
        string[] categoryNames = { "생산", "강화", "타워" };
        for (int index=0;index<categoryNames.Length;index++)
        {
            int tabIndex=index;
            Button("Category",construction,14+index*92,54,88,35,categoryNames[index],()=>{category=tabIndex;if(!constructionOpen)ToggleConstruction();RefreshCatalog();},null,17);
        }
        catalogLayer = Rect("Catalog",construction,14,103,272,320);
        leftDetailLayer = Box("Selected building",root,24,750,300,276);

        var overview = Box("Overview",root,1584,120,312,231);
        Text("Title",overview,16,10,280,28,"요새 현황",21);
        Image("Mini map",overview,16,48,280,158,new Color(.75f,.8f,.8f,1),landscape);
        Frame("Production region",overview,32,58,248,86,new Color(.08f,.3f,.13f,.35f),Green);
        Text("Production",overview,40,84,232,24,"생산  /  운송",17,Paper,TextAlignmentOptions.Center);
        Frame("Tower region",overview,32,150,248,42,new Color(.3f,.16f,.03f,.5f),Gold);
        Text("Towers",overview,40,158,232,26,"보급  /  방어",17,Paper,TextAlignmentOptions.Center);
        inspectorLayer = Box("Inspector",root,1584,367,312,389);
        var monsterPanel = Box("Monster info",root,1584,772,312,134);
        Image("Monster portrait",monsterPanel,10,33,86,86,Color.white,sprites[14]);
        Text("Title",monsterPanel,16,8,280,28,"다음 침공",20);
        monsterText = Text("Stats",monsterPanel,103,43,198,84,"",15,Paper);
        var supplyPanel = Box("Supply info",root,1584,922,312,104);
        Text("Title",supplyPanel,16,9,280,25,"보급 상태",18,Cyan);
        supplyText = Text("Supply",supplyPanel,16,39,280,53,"",14,Muted);

        Image("Footer",root,24,1041,1872,1,Border);
        Text("Keys",root,28,1048,1864,24,"R  회전     우클릭 / ESC  설치 취소     드래그  건물 이동     DEL  회수     SPACE  일시정지",14,Muted,TextAlignmentOptions.Center);
        toastText = Text("Toast",root,585,134,670,27,"",14,Gold,TextAlignmentOptions.Center);
        toastText.gameObject.AddComponent<Shadow>().effectColor = Color.black;
        modalLayer = Rect("Modal",root,0,0,ReferenceWidth,ReferenceHeight);
    }

    void TerrainStrip(Transform parent,float sourceTop,float sourceHeight,float targetTop,float targetHeight)
    {
        var texture=landscape.texture;
        float scale=texture.height/1024f;
        var sprite=Sprite.Create(texture,new Rect(0,texture.height-(sourceTop+sourceHeight)*scale,texture.width,sourceHeight*scale),Vector2.one*.5f);
        Image("Terrain",parent,0,targetTop,1216,targetHeight,Color.white,sprite);
    }

    void ClearChildren(Transform parent)
    {
        foreach (Transform child in parent) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
    }
    void RefreshCatalog()
    {
        ClearChildren(catalogLayer); catalogButtons.Clear(); stockTexts.Clear();
        var definitions = catalog.buildings.Where(definition => category == 2 ? definition.IsTowerArea : category == 1 ? definition.kind == BuildingKind.Forge || definition.kind == BuildingKind.FactoryBuff : definition.kind == BuildingKind.Road || definition.kind == BuildingKind.Mine || definition.kind == BuildingKind.Factory).ToList();
        for(int index=0;index<definitions.Count;index++)
        {
            var definition = definitions[index];
            var button=Button(definition.id,catalogLayer,(index%3)*92,(index/3)*104,88,98,"",()=>ChooseBuilding(definition.id));
            button.gameObject.AddComponent<CanvasGroup>();
            Image("Icon",button.transform,14,2,60,60,Color.white,sprites[definition.spriteIndex]);
            Text("Name",button.transform,2,62,84,20,definition.title,13,null,TextAlignmentOptions.Center);
            Text("Price",button.transform,4,81,52,15,$"{definition.price}G",11,Gold);
            stockTexts[definition.id]=Text("Stock",button.transform,54,81,30,15,"",11,Cyan,TextAlignmentOptions.MidlineRight);
            catalogButtons[definition.id]=button;
        }
        RefreshCatalogAvailability();
    }
    void RefreshCatalogAvailability()
    {
        foreach(var entry in catalogButtons)
        {
            var definition=catalog.Building(entry.Key);
            bool unlocked=LinkDeploySimulation.IsUnlocked(state,definition);
            entry.Value.interactable=LinkDeploySimulation.CanEdit(state)&&unlocked&&state.gold>=definition.price&&LinkDeploySimulation.Remaining(state,definition)>0;
            entry.Value.GetComponent<CanvasGroup>().alpha=entry.Value.interactable?1:unlocked?.5f:.35f;
            entry.Value.GetComponent<Image>().color = buildId == entry.Key ? Gold : Border;
            stockTexts[entry.Key].text=!unlocked?$"{definition.unlockAfterStage}단계":definition.limit<0?"무한":$"x{LinkDeploySimulation.Remaining(state,definition)}";
            entry.Value.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text="";
            entry.Value.transform.Find("Icon").GetComponent<Image>().color=unlocked?Color.white:new Color(.35f,.4f,.42f);
        }
    }

    BuildingState Selected => state.buildings.FirstOrDefault(building=>building.id==selectedId);
    BuildingDefinition SelectedDefinition => buildId != null ? catalog.Building(buildId) : Selected != null ? catalog.Building(Selected.definitionId) : null;

    void RefreshInspector()
    {
        ClearChildren(leftDetailLayer); ClearChildren(inspectorLayer);
        Image("Surface",leftDetailLayer,1,1,298,274,Panel); Image("Surface",inspectorLayer,1,1,310,387,Panel);
        var definition=SelectedDefinition; var building=buildId==null?Selected:null;
        if(definition==null)
        {
            Text("Title",leftDetailLayer,16,12,270,34,"생산에서 방어까지",21,Cyan);
            Text("Guide",leftDetailLayer,16,56,268,135,"1. 채굴장 > 가공 공장\n2. 도로 > 보급 입구\n3. 타워에 탄약을 채우기\n4. 스테이지를 눌러 방어 시작",17,Paper);
            Button("Starter",leftDetailLayer,16,215,268,42,$"추천 배치 / {LinkDeploySimulation.StarterCost(catalog):N0}G",PrepareStarter,null,17).interactable=state.buildings.Count==0&&state.gold>=LinkDeploySimulation.StarterCost(catalog)&&LinkDeploySimulation.CanEdit(state);
            Text("Title",inspectorLayer,16,14,280,30,"작전 안내",22);
            Text("Guide",inspectorLayer,18,65,276,200,"건물을 선택하면\n생산량과 연결 정보를 확인합니다.\n\n생산 구역과 타워 구역은\n세 개의 보급 입구로 연결됩니다.\n\n같은 구역의 타워 중 탄약이\n가장 적은 타워에 먼저 보급합니다.",17,Muted);
            Text("Rule",inspectorLayer,18,298,276,64,"단 한 마리도 놓치지 마세요.\n적이 방어선을 넘으면 패배합니다.",17,Red);
            return;
        }
        Image("Portrait",leftDetailLayer,9,10,70,70,Color.white,sprites[definition.spriteIndex]);
        Text("Name",leftDetailLayer,88,15,196,31,definition.title,22);
        Text("Level",leftDetailLayer,89,48,194,24,building==null?$"건설 비용  {definition.price}G":$"LEVEL {building.level}   /   {DirectionName(building.direction)}",14,Gold);
        Text("Description",leftDetailLayer,17,91,266,113,definition.description,16);
        if(building!=null)
        {
            int price=LinkDeploySimulation.UpgradePrice(building,definition);
            var upgrade=Button("Upgrade",leftDetailLayer,16,215,268,42,price<0?$"Lv.{building.level}   /   강화 완료":$"Lv.{building.level} > {building.level+1}  ({price}G)",UpgradeSelected,new Color32(123,73,24,255),17);
            upgrade.interactable=price>=0&&state.gold>=price&&LinkDeploySimulation.CanEdit(state);
        }
        else Button("Cancel",leftDetailLayer,16,215,268,42,"설치 취소   /   우클릭",CancelBuild,null,17);

        Text("Title",inspectorLayer,16,14,280,32,"건물 정보",22);
        Image("Portrait",inspectorLayer,13,51,86,86,Color.white,sprites[definition.spriteIndex]);
        Text("Name",inspectorLayer,111,60,184,30,definition.title,22);
        Text("Area",inspectorLayer,111,92,182,29,definition.IsTowerArea?"타워 구역 전용":"생산 구역 전용",14,Muted);
        Image("Divider",inspectorLayer,16,147,280,1,Border);
        string details;
        if(definition.kind==BuildingKind.Tower)
            details=$"사용 탄약    {LinkDeploySimulation.MaterialName(definition.ammunition)}\n공격력        {(building==null?definition.damage:LinkDeploySimulation.TowerDamage(building,definition)):0}\n공격 속도    초당 {definition.attacksPerSecond:0.0}회\n사거리        {definition.range:0}칸\n탄약 저장    {definition.inputCapacity}개";
        else if(definition.kind==BuildingKind.Factory)
            details=$"입력    {string.Join(" + ",definition.recipe.Select(ingredient=>$"{LinkDeploySimulation.MaterialName(ingredient.material)} {ingredient.quantity}"))}\n출력    {LinkDeploySimulation.MaterialName(definition.output)} 1개\n생산    {Mathf.Max(.1f,1/definition.productionPerSecond-(building?.level??0)*.1f):0.0}초 / 개\n입력 저장    {definition.inputCapacity}개\n출력 저장    {definition.outputCapacity}개";
        else if(definition.kind==BuildingKind.Mine)
            details=$"생산 자원    {LinkDeploySimulation.MaterialName(definition.output)}\n생산 속도    초당 {definition.productionPerSecond:0}개\n최대 저장    {definition.outputCapacity}개\n\n출력 포트 방향으로만 전달합니다.";
        else if(definition.kind==BuildingKind.Forge)
            details=$"입력    {LinkDeploySimulation.MaterialName(building?.forgeFilter??MaterialKind.None)}\n강화    {(definition.slow>0?$"둔화 {(definition.slow+(building?.level??0)*.1f)*100:0}%":$"공격력 +{definition.bonusDamage+(building?.level??0)*10:0}")}\n생산    초당 {definition.productionPerSecond:0}개\n최대 저장    {definition.inputCapacity} / {definition.outputCapacity}개";
        else details=definition.description;
        Text("Details",inspectorLayer,18,161,278,158,details,17);
        if(building!=null)
        {
            if(definition.kind==BuildingKind.Forge)
                Button("Filter",inspectorLayer,16,314,136,29,"필터 변경",()=>{var next=building.forgeFilter==MaterialKind.None?MaterialKind.RoundStone:building.forgeFilter==MaterialKind.Bullet?MaterialKind.None:building.forgeFilter+1;state=LinkDeploySimulation.Filter(state,building.id,next);RefreshInspector();},null,14).interactable=LinkDeploySimulation.CanEdit(state);
            else if(definition.HasOutput)
                Button("Rotate",inspectorLayer,16,314,136,29,"회전  R",()=>{state=LinkDeploySimulation.Rotate(state,catalog,building.id);RefreshInspector();},null,14).interactable=LinkDeploySimulation.CanEdit(state);
            Button("Sell",inspectorLayer,161,314,135,29,$"회수 {building.investment}G",SellSelected,null,14).interactable=LinkDeploySimulation.CanEdit(state);
            Text("Status",inspectorLayer,16,350,280,27,"드래그로 빈 칸에 이동할 수 있습니다",12,Muted);
        }
        else Text("Place",inspectorLayer,18,333,278,39,"R 회전   /   초록색 칸에 설치",16,Green);
    }

    static string DirectionName(FlowDirection direction)=>direction switch { FlowDirection.Right=>"오른쪽",FlowDirection.Down=>"아래",FlowDirection.Left=>"왼쪽",_=>"위" };
    void UpdateHud()
    {
        goldText.text=$"{state.gold:N0} <size=20>G</size>";
        statusText.text=paused?"일시정지":state.mode switch { SessionMode.Defense=>"방어 중",SessionMode.Victory=>"방어 성공",SessionMode.Defeat=>"방어 실패",_=>"건설 준비" };
        statusText.color=state.mode==SessionMode.Defeat?Red:state.mode==SessionMode.Defense?Gold:Green;
        var stage=catalog.stages[state.selectedStage];
        waveText.text=state.mode==SessionMode.Planning?"보급을 연결한 뒤 전투를 시작하세요":$"STAGE {state.selectedStage+1:00}   {stage.title}    {state.defeated} / {stage.monsterCount}";
        waveFill.rectTransform.sizeDelta=new Vector2(state.mode==SessionMode.Planning?0:398f*state.defeated/stage.monsterCount,5);
        pauseText.text=paused?"계속":"일시정지"; speedText.text=fast?"x2":"x1";
        for(int index=0;index<stageButtons.Length;index++)
        {
            stageButtons[index].interactable=index<=state.unlockedStage&&LinkDeploySimulation.CanEdit(state);
            stageButtons[index].GetComponentInChildren<TextMeshProUGUI>().text=(index+1).ToString();
            stageButtons[index].GetComponent<Image>().color=state.cleared[index]?Cyan:index==state.unlockedStage?Gold:Border;
        }
        var upcoming=catalog.stages[state.mode==SessionMode.Planning?state.unlockedStage:state.selectedStage];
        monsterText.text=$"HP {upcoming.monsterHealth:0}   속도 {upcoming.speed:0.#}\n총 {upcoming.monsterCount}마리  /  {upcoming.spawnDelay:0.#}초 간격\n클리어  {upcoming.clearGold:N0}G";
        var towers=state.buildings.Where(building=>catalog.Building(building.definitionId).kind==BuildingKind.Tower).ToList();
        supplyText.text=$"타워 {towers.Count}기   /   보유 탄약 {towers.Sum(tower=>tower.input.Count)}개\n{(towers.Any(tower=>tower.input.Count==0)?"탄약이 없는 타워의 보급선을 확인하세요":"생산과 보급은 준비 중에도 작동합니다")}";
        hintText.text=buildId!=null?$"{catalog.Building(buildId).title} 설치 중   /   출력: {DirectionName(direction)}   /   R 회전  /  우클릭 취소":dragId>=0?"빈 칸에 놓아 건물을 이동하세요":state.mode==SessionMode.Defense?"방어 중에는 건설 / 이동 / 강화가 잠깁니다. 건물 정보는 확인할 수 있습니다.":"생산 건물의 출력 방향을 보급 입구까지 연결하세요.";
        RefreshCatalogAvailability();
    }
}
