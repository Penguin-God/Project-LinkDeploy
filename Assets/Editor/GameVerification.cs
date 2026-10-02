using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class GameVerification
{
    static readonly List<string> passed=new List<string>();
    static LinkDeployCatalog catalog;
    static void Check(bool condition,string name){if(!condition)throw new Exception("GAME TEST FAILED: "+name);passed.Add(name);}
    static GameState Fresh(){var state=LinkDeploySimulation.Create(catalog);state.gold=100000;state.unlockedBuildings=catalog.buildings.Select(definition=>definition.id).ToList();return state;}
    static BuildingState Add(GameState state,string id,int column,int row,int zone=-1,FlowDirection direction=FlowDirection.Down)
    {
        var building=new BuildingState{id=state.nextId++,definitionId=id,column=column,row=row,zone=zone,direction=direction};state.buildings.Add(building);return building;
    }
    static GameState Advance(GameState state,float seconds){for(int step=0;step<Mathf.CeilToInt(seconds/.025f);step++)state=LinkDeploySimulation.Step(state,catalog,.025f);return state;}
    public static void Run()
    {
        passed.Clear();catalog=Resources.Load<LinkDeployCatalog>("LinkDeployCatalog");
        Check(catalog!=null&&catalog.stages.Length==7,"Seven stages loaded from the valid CSV");
        Check(catalog.startingGold==2000&&catalog.productionWidth==15&&catalog.productionHeight==8,"Document economy and map dimensions");
        Placement();Production();Transport();Enhancement();Combat();
        Directory.CreateDirectory("Artifacts");File.WriteAllLines("Artifacts/domain-verification.txt",passed.Select(name=>"PASS  "+name));
        Debug.Log("GAME_VERIFICATION_PASSED "+passed.Count);
    }
    static void Placement()
    {
        var state=Fresh();var mine=catalog.Building("StoneMine");
        Check(LinkDeploySimulation.PlacementError(state,catalog,mine,-1,0,0)!=null,"Supply entrances cannot be occupied");
        Check(LinkDeploySimulation.PlacementError(state,catalog,mine,0,0,0)!=null,"Production buildings rejected in tower zones");
        Check(LinkDeploySimulation.PlacementError(state,catalog,catalog.Building("Archer"),-1,4,4)!=null,"Towers rejected in production area");
        var placed=LinkDeploySimulation.Place(state,catalog,mine.id,-1,1,1,FlowDirection.Down);
        Check(state.buildings.Count==0&&placed.buildings.Count==1&&placed.gold==state.gold-mine.price,"Placement is immutable and spends exact cost");
        Check(ReferenceEquals(placed,LinkDeploySimulation.Place(placed,catalog,mine.id,-1,1,1,FlowDirection.Down)),"Occupied placement is rejected without spending");
        var moved=LinkDeploySimulation.Move(placed,catalog,placed.buildings[0].id,-1,4,4);
        Check(moved.buildings[0].column==4&&placed.buildings[0].column==1,"Drag move preserves prior state");
        var sold=LinkDeploySimulation.Sell(moved,moved.buildings[0].id);
        Check(sold.gold==state.gold&&sold.buildings.Count==0,"Recovery returns investment without duplicating a building");
        var locked=LinkDeploySimulation.Create(catalog);
        Check(ReferenceEquals(locked,LinkDeploySimulation.Place(locked,catalog,"Gun",0,1,1,FlowDirection.Down)),"Locked buildings cannot be purchased");
        var tower=Add(state,"Slingshot",0,0,0);
        var upgraded=LinkDeploySimulation.Upgrade(state,catalog,tower.id);
        Check(upgraded.gold==state.gold-100&&LinkDeploySimulation.TowerDamage(upgraded.buildings[0],catalog.Building("Slingshot"))==70,"Spreadsheet upgrade price and damage");
    }
    static void Production()
    {
        var state=Fresh();Add(state,"StoneMine",5,5);
        var full=Advance(state,5);
        Check(full.buildings[0].output.Count==10,"Mine stops at spreadsheet output capacity");
        Check(state.buildings[0].output.Count==0,"Simulation does not mutate prior inventory");
        state=Fresh();var factory=Add(state,"AmmoFactory",4,4);
        for(int index=0;index<4;index++)factory.input.Add(new Cargo{material=MaterialKind.Arrow});
        Check(!LinkDeploySimulation.Accepts(factory,catalog.Building(factory.definitionId),new Cargo{material=MaterialKind.Arrow})&&LinkDeploySimulation.Accepts(factory,catalog.Building(factory.definitionId),new Cargo{material=MaterialKind.RoundStone}),"Mixed recipe reserves space for its missing ingredient");
        factory.input.Add(new Cargo{material=MaterialKind.RoundStone,damage=50,slow=.5f});
        var result=Advance(state,1.05f);
        Check(result.buildings[0].output.Count==1&&result.buildings[0].output[0].material==MaterialKind.Bullet&&result.buildings[0].output[0].damage==0&&result.buildings[0].output[0].slow==0,"Recipe consumes both ingredients and removes inherited enhancements");
    }
    static void Transport()
    {
        var state=Fresh();var first=Add(state,"Road",4,4,-1,FlowDirection.Right);Add(state,"Road",5,4,-1,FlowDirection.Right);Add(state,"Road",6,4);
        first.output.Add(new Cargo{material=MaterialKind.Iron});var once=LinkDeploySimulation.Step(state,catalog,.01f);
        Check(once.buildings[0].output.Count==0&&once.buildings[1].output.Count==1&&once.buildings[2].output.Count==0,"Road moves cargo only one cell per simulation frame");
        var twice=LinkDeploySimulation.Step(once,catalog,.01f);
        Check(twice.buildings[2].output.Count==1,"Turned road accepts arbitrary input direction");
        state=Fresh();var road=Add(state,"Road",7,1);road.output.Add(new Cargo{material=MaterialKind.Arrow});var full=Add(state,"Archer",0,0,1);full.input.Add(new Cargo{material=MaterialKind.Arrow});var empty=Add(state,"Archer",1,0,1);var other=Add(state,"Slingshot",2,0,1);
        var distributed=LinkDeploySimulation.Step(state,catalog,.01f);
        Check(distributed.buildings.First(building=>building.id==empty.id).input.Count==1&&distributed.buildings.First(building=>building.id==other.id).input.Count==0,"Entrance prioritizes least stocked compatible tower");
        state=Fresh();road=Add(state,"Road",4,4,-1,FlowDirection.Right);road.output.Add(new Cargo{material=MaterialKind.Iron});Add(state,"StoneFactory",5,4);
        Check(LinkDeploySimulation.Step(state,catalog,.01f).buildings[0].output.Count==1,"Incompatible input causes backpressure without material loss");
        state=Fresh();road=Add(state,"Road",4,4,-1,FlowDirection.Right);road.output.Add(new Cargo{material=MaterialKind.Stone});var blocked=Add(state,"Road",5,4);blocked.output.Add(new Cargo{material=MaterialKind.Stone});
        Check(LinkDeploySimulation.Step(state,catalog,.01f).buildings.Sum(building=>building.output.Count)==2,"Full destination preserves both materials");
    }
    static void Enhancement()
    {
        var state=Fresh();var frost=Add(state,"SlowEffect",4,4);frost.input.Add(new Cargo{material=MaterialKind.Arrow,slow=.8f,damage=50});
        var result=Advance(state,1.05f);var cargo=result.buildings[0].output[0];
        Check(cargo.material==MaterialKind.Arrow&&cargo.slow==.9f&&cargo.damage==50,"Enhancement preserves projectile type and clamps stacked slow at 90 percent");
        frost.forgeFilter=MaterialKind.Bullet;
        Check(!LinkDeploySimulation.Accepts(frost,catalog.Building("SlowEffect"),new Cargo{material=MaterialKind.Arrow}),"Forge filter blocks unselected projectile types");
        state=Fresh();var fire=Add(state,"DamageEffect",4,4);fire.input.Add(new Cargo{material=MaterialKind.RoundStone,damage=50});
        Check(Advance(state,1.05f).buildings[0].output[0].damage==100,"Damage enhancements stack");
    }
    static void Combat()
    {
        var empty=LinkDeploySimulation.StartStage(LinkDeploySimulation.Create(catalog),catalog,0);
        var lost=Advance(empty,25);
        Check(lost.mode==SessionMode.Defeat,"A single escaping monster immediately causes defeat");
        Check(ReferenceEquals(empty,LinkDeploySimulation.Place(empty,catalog,"StoneMine",-1,5,5,FlowDirection.Down)),"Construction locked during defense");
        var state=LinkDeploySimulation.StarterLayout(LinkDeploySimulation.Create(catalog),catalog);
        Check(state.gold==500&&state.buildings.Count==8,"Starter layout uses 1500 gold and eight real buildings");
        var poor=LinkDeploySimulation.Create(catalog);poor.gold=LinkDeploySimulation.StarterCost(catalog)-1;
        Check(ReferenceEquals(poor,LinkDeploySimulation.StarterLayout(poor,catalog)),"Unaffordable starter never partially spends gold");
        state=Advance(state,15);Check(state.buildings.Where(building=>catalog.Building(building.definitionId).kind==BuildingKind.Tower).All(building=>building.input.Count>0),"Both starter supply chains deliver real ammunition");
        state=LinkDeploySimulation.StartStage(state,catalog,0);state=Advance(state,30);
        Check(state.mode==SessionMode.Victory&&state.defeated==3&&state.gold==700&&state.unlockedStage==1,"Playable starter wins first stage and receives exact reward");
        Check(state.unlockedBuildings.Contains("SlowEffect")&&state.unlockedBuildings.Contains("DamageEffect"),"Stage clear unlocks configured buildings");
        Check(LinkDeploySimulation.Step(state,catalog,1).gold==state.gold,"Victory reward cannot be granted twice");
        var noAmmo=Fresh();Add(noAmmo,"Gun",0,0,0);noAmmo=LinkDeploySimulation.StartStage(noAmmo,catalog,0);noAmmo=Advance(noAmmo,1);
        Check(noAmmo.totalShots==0,"Tower without ammunition does not fire");
        var targeted=Fresh();var tower=Add(targeted,"Archer",1,0,1);tower.targetId=100;tower.input.Add(new Cargo{material=MaterialKind.Arrow});
        targeted.mode=SessionMode.Defense;targeted.spawned=catalog.stages[0].monsterCount;
        targeted.monsters.Add(new MonsterState{id=100,position=6,health=200,maxHealth=200});targeted.monsters.Add(new MonsterState{id=101,position=7,health=200,maxHealth=200});
        var locked=LinkDeploySimulation.Step(targeted,catalog,.01f);
        Check(locked.buildings[0].targetId==100&&locked.projectiles[0].targetId==100,"Tower retains existing in-range target over a nearer enemy");
        var serialized=JsonUtility.FromJson<GameState>(JsonUtility.ToJson(state));
        Check(serialized.gold==state.gold&&serialized.buildings.Count==state.buildings.Count&&serialized.cleared[0],"Save data round trip preserves progression and layout");
    }
}
