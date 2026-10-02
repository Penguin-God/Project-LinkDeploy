using System.Collections;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static GameGraphics;

public partial class LinkDeployGame
{
    void ClearModal() => ClearChildren(modalLayer);
    RectTransform Dialog(string title,string eyebrow,int height=490)
    {
        ClearModal();
        var shade=Image("Dim",modalLayer,0,0,1920,1080,new Color(.015f,.035f,.045f,.8f));shade.raycastTarget=true;
        var panel=Box("Dialog",modalLayer,610,(1080-height)/2,700,height,Panel,Border);
        Image("Accent",panel,1,1,698,4,Gold);
        Text("Eyebrow",panel,36,30,628,26,eyebrow,14,Gold);
        Text("Title",panel,36,66,628,58,title,37);
        return panel;
    }
    void ShowWelcome()
    {
        onboarding=true;
        var panel=Dialog("당신의 첫 번째 공급망","LINK DEPLOY   /   숲의 요새",550);
        Text("Description",panel,36,139,628,73,"채굴하고, 가공하고, 연결하세요.\n타워에 탄약이 도착해야 방어가 시작됩니다.",23,Paper);
        for(int index=0;index<4;index++) Image("Supply chain",panel,61+index*157,234,104,104,Color.white,sprites[new[]{0,2,13,8}[index]]);
        Text("Chain",panel,36,347,628,34,"돌 채굴장       돌 공장       보급 입구       새총",17,Muted,TextAlignmentOptions.Center);
        Button("Starter",panel,36,420,306,54,$"추천 배치 / {LinkDeploySimulation.StarterCost(catalog):N0}G",PrepareStarter,new Color32(130,79,29,255),18).interactable=state.gold>=LinkDeploySimulation.StarterCost(catalog);
        Button("Manual",panel,356,420,308,54,"직접 설계하기",()=>{onboarding=false;ClearModal();},null,19);
        Text("Note",panel,36,489,628,28,$"시작 골드 {catalog.startingGold:N0}G / 건물 회수 시 사용 골드 전액 반환",14,Muted,TextAlignmentOptions.Center);
    }
    void ShowHelp()
    {
        helpOpen=true;
        var panel=Dialog("공급망이 곧 방어력입니다","FIELD MANUAL",650);
        Text("Instructions",panel,36,141,628,365,"01  생산 구역에 채굴장과 공장을 배치합니다.\n      돌 > 동그란 돌 / 쇠 > 화살 / 둘을 조합 > 총알\n\n02  R 키로 출력 방향을 맞추고 도로로 연결합니다.\n      재료가 맞지 않거나 저장고가 가득 차면 멈춥니다.\n\n03  맨 아래 세 입구로 탄약을 보내세요.\n      각 입구는 바로 아래의 3x2 타워 구역에 공급합니다.\n\n04  탄약이 채워지면 스테이지 버튼으로 전투를 시작합니다.\n      적을 모두 처치하면 골드와 새로운 건물이 해금됩니다.\n\n05  냉기와 화력 제련소로 탄약을 강화할 수 있습니다.\n      강화 효과는 중첩되며 둔화는 최대 90%입니다.",19,Paper);
        Button("Close",panel,36,555,628,51,"준비됐습니다",()=>{helpOpen=false;ClearModal();},new Color32(45,84,78,255),20);
    }
    void ShowMenu()
    {
        helpOpen=true;
        var panel=Dialog("작전 메뉴","LINK DEPLOY",470);
        Button("Resume",panel,36,147,628,48,"계속하기",()=>{helpOpen=false;ClearModal();},null,20);
        Button("Save",panel,36,207,628,48,"저장하고 계속하기",()=>{Save();helpOpen=false;ClearModal();Toast("배치와 진행 상황을 저장했습니다.");},null,20);
        Button("Reset",panel,36,267,628,48,"새 작전 시작",ShowResetConfirmation,null,20);
        Button("Sound",panel,36,327,628,48,sound.mute?"효과음 켜기":"효과음 끄기",()=>{sound.mute=!sound.mute;ShowMenu();},null,20);
        Text("Note",panel,36,396,628,26,"전투 중 저장한 게임은 건설 준비 상태에서 이어집니다.",14,Muted,TextAlignmentOptions.Center);
    }
    void ShowResetConfirmation()
    {
        var panel=Dialog("새로운 요새를 세울까요?","NEW OPERATION",380);
        Text("Warning",panel,36,140,628,74,$"현재 배치와 스테이지 진행이 초기화됩니다.\n시작 골드 {catalog.startingGold:N0}G로 다시 시작합니다.",21,Paper);
        Button("Reset",panel,36,267,306,50,"초기화하고 시작",()=>{state=LinkDeploySimulation.Create(catalog);selectedId=-1;buildId=null;paused=false;helpOpen=false;Save();RefreshCatalog();RefreshInspector();ShowWelcome();},new Color32(119,49,39,255),19);
        Button("Cancel",panel,356,267,308,50,"돌아가기",ShowMenu,null,19);
    }
    void ShowResult()
    {
        bool victory=state.mode==SessionMode.Victory;
        var stage=catalog.stages[state.selectedStage];
        var panel=Dialog(victory?"요새를 지켜냈습니다":"방어선이 돌파되었습니다",$"STAGE {state.selectedStage+1:00}   /   {(victory?"VICTORY":"DEFEAT")}",470);
        Text("Details",panel,36,145,628,111,victory?$"{stage.monsterCount}마리 처치 완료    /    보상 {stage.clearGold:N0}G\n\n{(stage.unlockBuildings.Length>0?"해금: "+string.Join(", ",stage.unlockBuildings.Select(id=>catalog.Building(id).title)):state.selectedStage==catalog.stages.Length-1?"모든 스테이지를 클리어했습니다!":"다음 침공을 준비하세요.")}":"적 한 마리가 목적지에 도착했습니다.\n\n탄약 공급과 타워의 사거리를 확인하고 다시 도전하세요.",21,Paper);
        Button("Return",panel,36,328,628,56,victory?"건설 준비로 돌아가기":"공급망 정비하기",()=>{state=LinkDeploySimulation.ReturnToPlanning(state);ClearModal();paused=false;RefreshCatalog();RefreshInspector();Save();},new Color32(81,88,49,255),21);
        Text("Note",panel,36,405,628,26,"건물과 남은 재료는 유지됩니다.",14,Muted,TextAlignmentOptions.Center);
    }

    IEnumerator SmokeRun()
    {
        state=LinkDeploySimulation.StarterLayout(LinkDeploySimulation.Create(catalog),catalog);
        for(int step=0;step<800;step++) state=LinkDeploySimulation.Step(state,catalog,.025f);
        selectedId=state.buildings.First(building=>building.definitionId=="ArrowFactory").id;
        RefreshInspector();UpdateHud();RenderWorld();
        Directory.CreateDirectory("Artifacts");
        yield return new WaitForEndOfFrame();CaptureDiagnosticFrame("01-planning.png");
        yield return new WaitForSecondsRealtime(.3f);
        catalogButtons["StoneFactory"].onClick.Invoke();RenderWorld();
        yield return new WaitForEndOfFrame();CaptureDiagnosticFrame("02-construction.png");
        yield return new WaitForSecondsRealtime(.3f);
        CancelBuild(); stageButtons[0].onClick.Invoke();fast=false;
        selectedId=state.buildings.First(building=>building.definitionId=="Archer").id;RefreshInspector();
        yield return new WaitForSecondsRealtime(4.5f);
        yield return new WaitForEndOfFrame();CaptureDiagnosticFrame("03-defense.png");
        fast=true;
        float timeout=Time.realtimeSinceStartup+40;
        while(state.mode==SessionMode.Defense&&Time.realtimeSinceStartup<timeout)yield return null;
        yield return new WaitForEndOfFrame();CaptureDiagnosticFrame("04-result.png");
        yield return new WaitForSecondsRealtime(.5f);
        File.WriteAllText("Artifacts/runtime-smoke.json",JsonUtility.ToJson(state,true));
        Debug.Log($"RUNTIME_SMOKE {state.mode} shots={state.totalShots} delivered={state.delivered} gold={state.gold}");
        Application.Quit(state.mode==SessionMode.Victory?0:2);
    }

    // Render the actual canvas into a target texture so visual checks also work in hidden windows.
    void CaptureDiagnosticFrame(string filename)
    {
        var canvas=root.GetComponentInParent<Canvas>();
        var captureObject=new GameObject("Screenshot camera",typeof(Camera),typeof(UniversalAdditionalCameraData));
        var camera=captureObject.GetComponent<Camera>();camera.enabled=false;
        camera.orthographic=true;camera.orthographicSize=5;camera.nearClipPlane=.1f;camera.farClipPlane=100;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Ink;camera.allowHDR=false;
        var target=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32);target.Create();camera.targetTexture=target;
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;
        Canvas.ForceUpdateCanvases();
        RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
        var previous=RenderTexture.active;RenderTexture.active=target;
        var pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();
        var colors=new HashSet<Color32>();var samples=pixels.GetPixels32();
        for(int index=0;index<samples.Length;index+=997)colors.Add(samples[index]);
        File.WriteAllBytes(Path.GetFullPath("Artifacts/"+filename),pixels.EncodeToPNG());
        Debug.Log($"RENDER_SMOKE {filename} sampledColors={colors.Count}");
        RenderTexture.active=previous;canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;
        camera.targetTexture=null;target.Release();Destroy(target);Destroy(pixels);Destroy(captureObject);
        Canvas.ForceUpdateCanvases();
        if(colors.Count<20){Debug.LogError("Visual smoke test produced an empty frame.");Application.Quit(3);}
    }
}
