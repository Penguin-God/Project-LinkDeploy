using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static GameGraphics;

public partial class LinkDeployGame
{
    class BuildingView
    {
        public RectTransform root;
        public Image body, selection, progress;
        public TextMeshProUGUI storage, level;
    }
    class MonsterView
    {
        public RectTransform root;
        public Image body, health;
    }

    Vector2 WorldPoint(Vector2 position) => new Vector2(GridLeft+(position.x+.5f)*CellSize,GridTop+(catalog.productionHeight-.5f-position.y)*CellSize);
    Vector2 BuildingPoint(int zone,int column,int row) => zone<0?WorldPoint(new Vector2(column,row)):WorldPoint(LinkDeploySimulation.TowerPosition(catalog,new BuildingState{zone=zone,column=column,row=row}));

    void RenderWorld()
    {
        foreach(var id in buildingViews.Keys.Where(id=>state.buildings.All(building=>building.id!=id)).ToList())
        { Destroy(buildingViews[id].root.gameObject); buildingViews.Remove(id); }
        foreach(var building in state.buildings)
        {
            var definition=catalog.Building(building.definitionId);
            if(!buildingViews.TryGetValue(building.id,out var view))
            {
                var container=Rect(definition.title,unitLayer,0,0,CellSize,CellSize);
                view=new BuildingView{root=container};
                view.selection=Image("Selection",container,0,0,CellSize,CellSize,new Color(.9f,.7f,.2f,.23f));
                view.body=Image("Sprite",container,0,-5,CellSize,CellSize,Color.white,sprites[definition.spriteIndex]);
                view.body.rectTransform.pivot=Vector2.one*.5f;view.body.rectTransform.anchoredPosition=new Vector2(CellSize/2,-CellSize/2+5);
                Box("Storage badge",container,-2,-17,76,20,new Color32(9,22,26,235),new Color32(79,106,102,220));
                view.storage=Text("Storage",container,-1,-18,74,22,"",13,Paper,TextAlignmentOptions.Center);
                view.level=Text("Level",container,47,49,28,21,"",12,Gold,TextAlignmentOptions.Center);
                view.progress=Image("Production",container,7,67,0,2,Cyan);
                buildingViews[building.id]=view;
            }
            var point=BuildingPoint(building.zone,building.column,building.row);
            view.root.anchoredPosition=new Vector2(point.x-CellSize/2,-point.y+CellSize/2);
            view.selection.gameObject.SetActive(building.id==selectedId);
            view.body.color=building.id==dragId?new Color(1,1,1,.35f):Color.white;
            view.body.rectTransform.localEulerAngles=new Vector3(0,0,SpriteRotation(definition,building.direction));
            view.level.text=building.level>0?$"+{building.level}":"";
            view.storage.text=definition.kind==BuildingKind.Tower?$"{building.input.Count}/{definition.inputCapacity}":definition.kind==BuildingKind.Factory||definition.kind==BuildingKind.Forge?$"{building.input.Count}  >  {building.output.Count}/{definition.outputCapacity}":definition.kind==BuildingKind.FactoryBuff||definition.kind==BuildingKind.TowerBuff?"가속 중":$"{building.output.Count}/{definition.outputCapacity}";
            view.storage.color=definition.kind==BuildingKind.Tower&&building.input.Count==0?Gold:Paper;
            float cycle=Mathf.Max(.1f,1/Mathf.Max(.01f,definition.productionPerSecond)-(definition.kind==BuildingKind.Factory?.1f*building.level:0));
            view.progress.rectTransform.sizeDelta=new Vector2(Mathf.Clamp01(building.progress/cycle)*58,2);
        }
        RenderConnections(); RenderPlacement(); RenderEnemies();
    }

    void RenderConnections()
    {
        pipes.segments.Clear();
        flowMarkers.segments.Clear();
        foreach(var building in state.buildings.Where(building=>building.zone<0&&catalog.Building(building.definitionId).HasOutput))
        {
            var definition=catalog.Building(building.definitionId);
            var offset=LinkDeploySimulation.Offset(building.direction);
            int column=building.column+offset.x,row=building.row+offset.y;
            bool gate=LinkDeploySimulation.IsEntrance(catalog,column,row);
            var receiver=LinkDeploySimulation.At(state,-1,column,row);
            var start=BuildingPoint(-1,building.column,building.row);
            var finish=BuildingPoint(-1,column,row);
            bool linked=gate||receiver!=null;
            if(!linked) finish=Vector2.Lerp(start,finish,.54f);
            bool flowing=linked&&state.elapsed-building.lastTransferTime<.45f;
            Color color=!linked?new Color32(71,79,72,255):flowing?Gold:new Color32(137,111,57,255);
            pipes.segments.Add(new PipelineGraphic.Segment{start=new Vector2(start.x,-start.y),end=new Vector2(finish.x,-finish.y),color=color,width=16,arrows=flowing});
            if(flowing)
            {
                var travel=(finish-start).normalized;
                var arrowStart=start+travel*(definition.kind==BuildingKind.Road?0:CellSize*.32f);
                var arrowEnd=finish-travel*(receiver!=null&&catalog.Building(receiver.definitionId).kind==BuildingKind.Road?0:CellSize*.32f);
                flowMarkers.segments.Add(new PipelineGraphic.Segment{start=new Vector2(arrowStart.x,-arrowStart.y),end=new Vector2(arrowEnd.x,-arrowEnd.y),color=Gold,arrows=true});
            }
        }
        for(int zone=0;zone<catalog.entranceColumns.Length;zone++)
        {
            var entrance=BuildingPoint(-1,catalog.entranceColumns[zone],0);
            var towerTop=BuildingPoint(zone,1,1)+new Vector2(0,-CellSize/2);
            var corner=new Vector2(towerTop.x,entrance.y+68);
            pipes.segments.Add(new PipelineGraphic.Segment{start=new Vector2(entrance.x,-entrance.y-23),end=new Vector2(corner.x,-corner.y),color=new Color32(66,137,130,190),width=6});
            pipes.segments.Add(new PipelineGraphic.Segment{start=new Vector2(corner.x,-corner.y),end=new Vector2(towerTop.x,-towerTop.y),color=new Color32(66,137,130,190),width=6});
        }
        pipes.phase=state.elapsed*1.8f; pipes.SetVerticesDirty();
        flowMarkers.phase=pipes.phase; flowMarkers.SetVerticesDirty();
    }

    void RenderPlacement()
    {
        string activeId=dragId>=0?state.buildings.First(building=>building.id==dragId).definitionId:buildId;
        bool placing=activeId!=null&&LinkDeploySimulation.CanEdit(state)&&!helpOpen&&!onboarding;
        cellLayer.gameObject.SetActive(placing);
        var pointer=Pointer(); bool hit=HitCell(pointer,out int zone,out int column,out int row);
        var definition=placing?catalog.Building(activeId):null;
        if(placing)
        {
            int index=0;
            for(int cellRow=0;cellRow<catalog.productionHeight;cellRow++)
                for(int cellColumn=0;cellColumn<catalog.productionWidth;cellColumn++) SetCell(cells[index++],-1,cellColumn,cellRow);
            for(int cellZone=0;cellZone<catalog.entranceColumns.Length;cellZone++)
                for(int cellRow=0;cellRow<catalog.towerHeight;cellRow++)
                    for(int cellColumn=0;cellColumn<catalog.towerWidth;cellColumn++) SetCell(cells[index++],cellZone,cellColumn,cellRow);
        }
        void SetCell(Image cell,int cellZone,int cellColumn,int cellRow)
        {
            bool valid=LinkDeploySimulation.PlacementError(state,catalog,definition,cellZone,cellColumn,cellRow,dragId)==null;
            bool hovered=hit&&zone==cellZone&&column==cellColumn&&row==cellRow;
            cell.color=valid?new Color(.45f,.88f,.34f,hovered?.5f:.17f):new Color(.85f,.19f,.12f,hovered?.5f:.11f);
        }
        preview.gameObject.SetActive(placing&&hit);
        if(placing&&hit)
        {
            var center=BuildingPoint(zone,column,row);
            preview.sprite=sprites[definition.spriteIndex];
            preview.rectTransform.pivot=Vector2.one*.5f;
            preview.rectTransform.anchoredPosition=new Vector2(center.x,-center.y);
            var previewDirection=dragId>=0?state.buildings.First(building=>building.id==dragId).direction:direction;
            preview.rectTransform.localEulerAngles=new Vector3(0,0,SpriteRotation(definition,previewDirection));
            bool valid=LinkDeploySimulation.PlacementError(state,catalog,definition,zone,column,row,dragId)==null;
            preview.color=valid?new Color(.65f,1,.65f,.8f):new Color(1,.4f,.4f,.65f);
        }
        var selected=Selected;
        var rangeDefinition=placing?definition:selected!=null?catalog.Building(selected.definitionId):null;
        bool showRange=rangeDefinition!=null&&rangeDefinition.kind==BuildingKind.Tower&&(placing?hit&&zone>=0:true);
        range.gameObject.SetActive(showRange);
        if(showRange)
        {
            var center=placing?BuildingPoint(zone,column,row):BuildingPoint(selected.zone,selected.column,selected.row);
            float radius=rangeDefinition.range*CellSize;
            range.rectTransform.anchoredPosition=new Vector2(center.x-radius-347,-center.y+radius+121);
            range.rectTransform.sizeDelta=Vector2.one*radius*2;
        }
    }

    static float SpriteRotation(BuildingDefinition definition,FlowDirection direction)=>definition.HasOutput?(definition.kind==BuildingKind.Road?0:90)-(int)direction*90:0;

    void RenderEnemies()
    {
        foreach(var id in monsterViews.Keys.Where(id=>state.monsters.All(monster=>monster.id!=id)).ToList())
        { Destroy(monsterViews[id].root.gameObject); monsterViews.Remove(id); }
        foreach(var monster in state.monsters)
        {
            if(!monsterViews.TryGetValue(monster.id,out var view))
            {
                var container=Rect("Invader",unitLayer,0,0,68,68);
                view=new MonsterView{root=container,body=Image("Sprite",container,0,0,68,68,Color.white,sprites[state.selectedStage>=4?15:14])};
                Image("Health background",container,10,-7,48,6,new Color32(25,22,23,255));
                view.health=Image("Health",container,11,-6,46,4,Red); monsterViews[monster.id]=view;
            }
            var point=WorldPoint(new Vector2(monster.position,catalog.pathHeight));
            float bounce=Mathf.Abs(Mathf.Sin(state.elapsed*9+monster.id))*3;
            view.root.anchoredPosition=new Vector2(point.x-34,-point.y+34+bounce);
            view.health.rectTransform.sizeDelta=new Vector2(46*Mathf.Clamp01(monster.health/monster.maxHealth),4);
            view.body.color=monster.slow>0?new Color(.5f,.85f,1):Color.white;
        }
        foreach(var id in projectileViews.Keys.Where(id=>state.projectiles.All(projectile=>projectile.id!=id)).ToList())
        { Destroy(projectileViews[id].gameObject); projectileViews.Remove(id); }
        foreach(var projectile in state.projectiles)
        {
            if(!projectileViews.TryGetValue(projectile.id,out var view))
            {
                view=Image("Projectile",effectsLayer,0,0,projectile.material==MaterialKind.Arrow?17:10,projectile.material==MaterialKind.Arrow?4:10,projectile.slow>0?Cyan:projectile.damage>75?new Color(1,.35f,.12f):Gold);
                view.rectTransform.pivot=Vector2.one*.5f;
                var shadow=view.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(1,.5f,.1f,.5f);shadow.effectDistance=new Vector2(3,3);
                projectileViews[projectile.id]=view;
            }
            var point=WorldPoint(projectile.position);view.rectTransform.anchoredPosition=new Vector2(point.x,-point.y);
            var target=state.monsters.FirstOrDefault(monster=>monster.id==projectile.targetId);
            if(target!=null){var travel=new Vector2(target.position,catalog.pathHeight)-projectile.position;view.rectTransform.localEulerAngles=new Vector3(0,0,Mathf.Atan2(travel.y,travel.x)*Mathf.Rad2Deg);}
        }
    }
}
