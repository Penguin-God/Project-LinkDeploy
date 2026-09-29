using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI topBarText;
    public Transform buttonsContainer;
    public Button cancelButton;
    
    private bool _buttonsCreated = false;
    private Button _sellButton;
    private Button _buildModeBtn;
    private GameObject _stageContainer;
    private GameObject _endGameContainer;
    
    private TMP_FontAsset _koreanFont;
    private GameObject _infoPopup;
    private TextMeshProUGUI _infoNameText;
    private TextMeshProUGUI _infoDescText;
    private Button _upgradeBtn;
    private TextMeshProUGUI _upgradeBtnText;
    private GameObject _smelterySelectionContainer;
    private bool _isVillageView = false;
    
    void Start()
    {
        _koreanFont = Resources.Load<TMP_FontAsset>("font/MaplestoryBold");
        if (topBarText != null && _koreanFont != null) topBarText.font = _koreanFont;
        
        if (cancelButton != null) cancelButton.onClick.AddListener(() => {
            if (BuildManager.Instance != null) BuildManager.Instance.CancelPlacement();
        });
        
        bool isLobby = SceneManager.GetActiveScene().name == "Lobby";
        CreateDynamicUI(isLobby);
        
        if (!isLobby)
        {
            CreateDefenseEndGameUI();
        }
    }
    
    private void CreateDynamicUI(bool isLobby)
    {
        var canvas = transform.parent;
        
        // Sell Button
        var sellBtnGo = new GameObject("SellButton");
        sellBtnGo.transform.SetParent(canvas, false);
        sellBtnGo.AddComponent<Image>().color = new Color(1f, 0.8f, 0.2f);
        _sellButton = sellBtnGo.AddComponent<Button>();
        var sellRt = sellBtnGo.GetComponent<RectTransform>();
        sellRt.anchorMin = new Vector2(0.5f, 0f);
        sellRt.anchorMax = new Vector2(0.5f, 0f);
        sellRt.pivot = new Vector2(0.5f, 0f);
        sellRt.anchoredPosition = new Vector2(0, 20);
        sellRt.sizeDelta = new Vector2(120, 60);
        var sellTextGo = new GameObject("Text");
        sellTextGo.transform.SetParent(sellBtnGo.transform, false);
        var sellText = sellTextGo.AddComponent<TextMeshProUGUI>();
        if (_koreanFont != null) sellText.font = _koreanFont;
        sellText.text = "Sell";
        sellText.color = Color.black;
        sellText.alignment = TextAlignmentOptions.Center;
        sellText.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 60);
        
        _sellButton.onClick.AddListener(OnSellClicked);
        _sellButton.gameObject.SetActive(false);
        
        // Info Popup
        _infoPopup = new GameObject("InfoPopup");
        _infoPopup.transform.SetParent(canvas, false);
        _infoPopup.AddComponent<Image>().color = new Color(0, 0, 0, 0.8f);
        var infoRt = _infoPopup.GetComponent<RectTransform>();
        infoRt.anchorMin = new Vector2(0, 0.5f);
        infoRt.anchorMax = new Vector2(0, 0.5f);
        infoRt.pivot = new Vector2(0, 0.5f);
        infoRt.anchoredPosition = new Vector2(20, 0);
        infoRt.sizeDelta = new Vector2(300, 260);
        
        var nameGo = new GameObject("NameText");
        nameGo.transform.SetParent(_infoPopup.transform, false);
        _infoNameText = nameGo.AddComponent<TextMeshProUGUI>();
        if (_koreanFont != null) _infoNameText.font = _koreanFont;
        _infoNameText.alignment = TextAlignmentOptions.Top;
        _infoNameText.fontSize = 28;
        _infoNameText.color = Color.white;
        var nameRt = nameGo.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0, 1);
        nameRt.anchorMax = new Vector2(1, 1);
        nameRt.pivot = new Vector2(0.5f, 1);
        nameRt.sizeDelta = new Vector2(0, 50);
        nameRt.anchoredPosition = new Vector2(0, -10);
        
        var descGo = new GameObject("DescText");
        descGo.transform.SetParent(_infoPopup.transform, false);
        _infoDescText = descGo.AddComponent<TextMeshProUGUI>();
        if (_koreanFont != null) _infoDescText.font = _koreanFont;
        _infoDescText.alignment = TextAlignmentOptions.TopLeft;
        _infoDescText.fontSize = 18;
        _infoDescText.color = Color.white;
        _infoDescText.enableWordWrapping = true;
        var descRt = descGo.GetComponent<RectTransform>();
        descRt.anchorMin = new Vector2(0, 0);
        descRt.anchorMax = new Vector2(1, 0);
        descRt.pivot = new Vector2(0.5f, 0);
        descRt.sizeDelta = new Vector2(-20, 130);
        descRt.anchoredPosition = new Vector2(0, 50); // Moved up to make room for Upgrade button
        
        var upgBtnGo = new GameObject("UpgradeButton");
        upgBtnGo.transform.SetParent(_infoPopup.transform, false);
        upgBtnGo.AddComponent<Image>().color = new Color(0.1f, 0.5f, 0.1f);
        _upgradeBtn = upgBtnGo.AddComponent<Button>();
        var upgRt = upgBtnGo.GetComponent<RectTransform>();
        upgRt.anchorMin = new Vector2(0.5f, 0);
        upgRt.anchorMax = new Vector2(0.5f, 0);
        upgRt.pivot = new Vector2(0.5f, 0);
        upgRt.anchoredPosition = new Vector2(0, 10);
        upgRt.sizeDelta = new Vector2(200, 40);
        
        var upgTextGo = new GameObject("Text");
        upgTextGo.transform.SetParent(upgBtnGo.transform, false);
        _upgradeBtnText = upgTextGo.AddComponent<TextMeshProUGUI>();
        if (_koreanFont != null) _upgradeBtnText.font = _koreanFont;
        _upgradeBtnText.alignment = TextAlignmentOptions.Center;
        _upgradeBtnText.color = Color.white;
        _upgradeBtnText.fontSize = 20;
        var upgTextRt = upgTextGo.GetComponent<RectTransform>();
        upgTextRt.anchorMin = new Vector2(0, 0);
        upgTextRt.anchorMax = new Vector2(1, 1);
        upgTextRt.sizeDelta = Vector2.zero;
        
        _upgradeBtn.onClick.AddListener(OnUpgradeClicked);
        
        _smelterySelectionContainer = new GameObject("SmelterySelection");
        _smelterySelectionContainer.transform.SetParent(_infoPopup.transform, false);
        var sscRt = _smelterySelectionContainer.AddComponent<RectTransform>();
        sscRt.anchorMin = new Vector2(0.5f, 0);
        sscRt.anchorMax = new Vector2(0.5f, 0);
        sscRt.pivot = new Vector2(0.5f, 0);
        sscRt.anchoredPosition = new Vector2(0, 50); // Just above upgrade button
        sscRt.sizeDelta = new Vector2(250, 40);
        var sscLayout = _smelterySelectionContainer.AddComponent<HorizontalLayoutGroup>();
        sscLayout.spacing = 10;
        sscLayout.childAlignment = TextAnchor.MiddleCenter;
        
        string[] ammoNames = { "RoundStone", "Arrow", "GunAmmo" };
        string[] ammoKorNames = { "둥근 돌", "화살", "총알" };
        ResourceType[] ammoTypes = { ResourceType.RoundStone, ResourceType.Arrow, ResourceType.GunAmmo };
        
        for (int i = 0; i < 3; i++)
        {
            var rType = ammoTypes[i];
            var abBtnGo = new GameObject(ammoNames[i]);
            abBtnGo.transform.SetParent(_smelterySelectionContainer.transform, false);
            var abImg = abBtnGo.AddComponent<Image>();
            abImg.color = Color.gray;
            var abBtn = abBtnGo.AddComponent<Button>();
            var abRt = abBtnGo.GetComponent<RectTransform>();
            abRt.sizeDelta = new Vector2(70, 35);
            
            var abTextGo = new GameObject("Text");
            abTextGo.transform.SetParent(abBtnGo.transform, false);
            var abText = abTextGo.AddComponent<TextMeshProUGUI>();
            if (_koreanFont != null) abText.font = _koreanFont;
            abText.text = ammoKorNames[i];
            abText.fontSize = 14;
            abText.alignment = TextAlignmentOptions.Center;
            abText.color = Color.black;
            abText.GetComponent<RectTransform>().sizeDelta = new Vector2(70, 35);
            
            abBtn.onClick.AddListener(() => {
                var gridRenderer = Object.FindObjectOfType<GridRenderer>();
                if (gridRenderer != null && gridRenderer.SelectedBuilding != null)
                {
                    gridRenderer.SelectedBuilding.selectedAmmoType = rType;
                    UpdateSelectionUI();
                }
            });
        }
        
        _infoPopup.SetActive(false);
        
        if (isLobby)
        {
            // Build Mode Button
            var buildModeBtnGo = new GameObject("BuildModeButton");
            buildModeBtnGo.transform.SetParent(canvas, false);
            buildModeBtnGo.AddComponent<Image>().color = new Color(0.2f, 0.8f, 0.2f);
            _buildModeBtn = buildModeBtnGo.AddComponent<Button>();
            var bmRt = buildModeBtnGo.GetComponent<RectTransform>();
            bmRt.anchorMin = new Vector2(1, 0);
            bmRt.anchorMax = new Vector2(1, 0);
            bmRt.pivot = new Vector2(1f, 0f);
            bmRt.anchoredPosition = new Vector2(-20, 20);
            bmRt.sizeDelta = new Vector2(150, 80);
            var bmTextGo = new GameObject("Text");
            bmTextGo.transform.SetParent(buildModeBtnGo.transform, false);
            var bmText = bmTextGo.AddComponent<TextMeshProUGUI>();
            bmText.text = "Build Mode";
            bmText.color = Color.white;
            bmText.alignment = TextAlignmentOptions.Center;
            bmText.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 80);
            _buildModeBtn.onClick.AddListener(() => {
                BuildManager.Instance.ToggleBuildMode();
            });
            
            // Stage Buttons
            _stageContainer = new GameObject("Stages");
            _stageContainer.transform.SetParent(canvas, false);
            var scRt = _stageContainer.AddComponent<RectTransform>();
            // Anchor to Bottom Right, above the Build Mode button
            scRt.anchorMin = new Vector2(1, 0);
            scRt.anchorMax = new Vector2(1, 0);
            scRt.pivot = new Vector2(1f, 0f);
            scRt.anchoredPosition = new Vector2(-20, 120);
            var loadedStages = Resources.LoadAll<StageDataSO>("Stages");
            int maxStage = 1;
            if (loadedStages != null && loadedStages.Length > 0)
            {
                maxStage = loadedStages.Max(s => s.stageNumber);
            }
            
            scRt.sizeDelta = new Vector2(150, maxStage * 70 + 50); // Dynamic height
            var layout = _stageContainer.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 15;
            layout.childAlignment = TextAnchor.LowerCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            
            for (int i = 1; i <= maxStage; i++)
            {
                int stageNum = i;
                var sBtnGo = new GameObject($"Stage_{i}");
                sBtnGo.transform.SetParent(_stageContainer.transform, false);
                var img = sBtnGo.AddComponent<Image>();
                img.color = GameState.unlockedStage >= i ? Color.white : Color.gray;
                var sBtn = sBtnGo.AddComponent<Button>();
                sBtn.interactable = GameState.unlockedStage >= i;
                var sRt = sBtnGo.GetComponent<RectTransform>();
                sRt.sizeDelta = new Vector2(120, 50);
                
                var stTextGo = new GameObject("Text");
                stTextGo.transform.SetParent(sBtnGo.transform, false);
                var stText = stTextGo.AddComponent<TextMeshProUGUI>();
                stText.text = $"Stage {i}";
                stText.color = Color.black;
                stText.alignment = TextAlignmentOptions.Center;
                stText.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 50);
                
                sBtn.onClick.AddListener(() => {
                    GameState.currentPlayingStage = stageNum;
                    if (GridManager.Instance != null)
                        GameState.SaveGrid(GridManager.Instance.GetAllBuildings());
                    SceneManager.LoadScene("Defense");
                });
            }
            
            // Camera Toggle Button
            var camToggleBtnGo = new GameObject("CameraToggleBtn");
            camToggleBtnGo.transform.SetParent(canvas, false);
            camToggleBtnGo.AddComponent<Image>().color = new Color(0.2f, 0.6f, 1f);
            var camToggleBtn = camToggleBtnGo.AddComponent<Button>();
            var camToggleRt = camToggleBtnGo.GetComponent<RectTransform>();
            camToggleRt.anchorMin = new Vector2(1, 0);
            camToggleRt.anchorMax = new Vector2(1, 0);
            camToggleRt.pivot = new Vector2(1f, 0f);
            camToggleRt.anchoredPosition = new Vector2(-180, 20); // To the left of Build Mode button
            camToggleRt.sizeDelta = new Vector2(150, 80);
            
            var camToggleTextGo = new GameObject("Text");
            camToggleTextGo.transform.SetParent(camToggleBtnGo.transform, false);
            var camToggleText = camToggleTextGo.AddComponent<TextMeshProUGUI>();
            if (_koreanFont != null) camToggleText.font = _koreanFont;
            camToggleText.text = "Go to Village";
            camToggleText.color = Color.white;
            camToggleText.alignment = TextAlignmentOptions.Center;
            camToggleText.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 80);
            
            camToggleBtn.onClick.AddListener(() => {
                _isVillageView = !_isVillageView;
                camToggleText.text = _isVillageView ? "Go to Defense" : "Go to Village";
                if (Camera.main != null)
                {
                    Camera.main.transform.position = _isVillageView ? new Vector3(54f, 54f, -10f) : new Vector3(7f, 0f, -10f);
                }
                if (_buttonsCreated)
                {
                    CreateBuildButtons();
                    UpdateBuildButtons();
                }
            });
        }
    }
    
    private void CreateDefenseEndGameUI()
    {
        var canvas = transform.parent;
        
        _endGameContainer = new GameObject("EndGameContainer");
        _endGameContainer.transform.SetParent(canvas, false);
        var img = _endGameContainer.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.8f);
        var egRt = _endGameContainer.GetComponent<RectTransform>();
        egRt.anchorMin = Vector2.zero;
        egRt.anchorMax = Vector2.one;
        egRt.offsetMin = Vector2.zero;
        egRt.offsetMax = Vector2.zero;
        
        var resultTextGo = new GameObject("ResultText");
        resultTextGo.transform.SetParent(_endGameContainer.transform, false);
        var resultText = resultTextGo.AddComponent<TextMeshProUGUI>();
        resultText.text = "Result";
        resultText.fontSize = 60;
        resultText.alignment = TextAlignmentOptions.Center;
        resultText.color = Color.white;
        var rRt = resultTextGo.GetComponent<RectTransform>();
        rRt.anchoredPosition = new Vector2(0, 50);
        rRt.sizeDelta = new Vector2(600, 100);
        
        var retBtnGo = new GameObject("ReturnBtn");
        retBtnGo.transform.SetParent(_endGameContainer.transform, false);
        retBtnGo.AddComponent<Image>().color = Color.white;
        var retBtn = retBtnGo.AddComponent<Button>();
        var rbRt = retBtnGo.GetComponent<RectTransform>();
        rbRt.anchoredPosition = new Vector2(0, -50);
        rbRt.sizeDelta = new Vector2(200, 60);
        
        var retTextGo = new GameObject("Text");
        retTextGo.transform.SetParent(retBtnGo.transform, false);
        var retText = retTextGo.AddComponent<TextMeshProUGUI>();
        retText.text = "Return to Lobby";
        retText.color = Color.black;
        retText.alignment = TextAlignmentOptions.Center;
        retText.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 60);
        
        retBtn.onClick.AddListener(() => {
            if (GridManager.Instance != null)
                GameState.SaveGrid(GridManager.Instance.GetAllBuildings());
            SceneManager.LoadScene("Lobby");
        });
        
        _endGameContainer.SetActive(false);
    }
    
    public void ShowEndGame(bool isWin, int rewardGold = 0)
    {
        if (_endGameContainer != null)
        {
            _endGameContainer.SetActive(true);
            var tmp = _endGameContainer.transform.Find("ResultText").GetComponent<TextMeshProUGUI>();
            if (isWin)
            {
                tmp.text = $"STAGE CLEAR\n<size=30>Reward: {rewardGold} G</size>";
                tmp.color = Color.green;
            }
            else
            {
                tmp.text = "STAGE FAILED";
                tmp.color = Color.red;
            }
        }
    }
    
    void Update()
    {
        bool isLobby = SceneManager.GetActiveScene().name == "Lobby";
        

        
        if (topBarText != null)
        {
            if (isLobby)
                topBarText.text = $"Gold: {GameState.currentGold}  Wood: {GameState.currentWood}  Iron: {GameState.currentIron}  Hammer: {GameState.currentHammer}";
            else
                topBarText.text = "";
        }
            
        if (isLobby && BuildManager.Instance != null)
        {
            if (cancelButton != null) cancelButton.gameObject.SetActive(BuildManager.Instance.IsPlacing);
            
            if (BuildManager.Instance.isBuildModeActive)
            {
                if (!_buttonsCreated && BuildManager.Instance.availableBuildings != null && BuildManager.Instance.availableBuildings.Count > 0)
                {
                    CreateBuildButtons();
                    _buttonsCreated = true;
                }
                if (buttonsContainer != null) buttonsContainer.gameObject.SetActive(true);
                UpdateBuildButtons();
                if (_stageContainer != null) _stageContainer.SetActive(false);
            }
            else
            {
                if (buttonsContainer != null) buttonsContainer.gameObject.SetActive(false);
                if (_stageContainer != null) _stageContainer.SetActive(true);
            }
        }
        else
        {
            if (cancelButton != null) cancelButton.gameObject.SetActive(false);
            if (buttonsContainer != null) buttonsContainer.gameObject.SetActive(false);
        }
        
        UpdateSelectionUI();
    }
    
    private void UpdateSelectionUI()
    {
        var gridRenderer = Object.FindObjectOfType<GridRenderer>();
        BuildingDataSO targetData = null;

        if (BuildManager.Instance != null && BuildManager.Instance.IsPlacing)
        {
            targetData = BuildManager.Instance.BuildingToPlace;
        }
        else if (gridRenderer != null && gridRenderer.SelectedBuilding != null)
        {
            targetData = gridRenderer.SelectedBuilding.data;
        }

        if (targetData != null && _infoPopup != null)
        {
            _infoPopup.SetActive(true);
            if (_infoNameText != null) _infoNameText.text = string.IsNullOrEmpty(targetData.displayName) ? targetData.buildingName : targetData.displayName;
            if (_infoDescText != null) _infoDescText.text = string.IsNullOrEmpty(targetData.description) ? "" : targetData.description;
            
            if (_upgradeBtn != null)
            {
                // Can only upgrade built things, not when placing
                if (gridRenderer != null && gridRenderer.SelectedBuilding != null && !BuildManager.Instance.IsPlacing)
                {
                    var selected = gridRenderer.SelectedBuilding;
                    if (selected.data.buildingType == BuildingType.Road || selected.data.buildingType == BuildingType.Entrance || selected.data.buildingType == BuildingType.Mine)
                    {
                        _upgradeBtn.gameObject.SetActive(false);
                    }
                    else
                    {
                        _upgradeBtn.gameObject.SetActive(true);
                        var nextUpg = targetData.upgrades?.FirstOrDefault(u => u.level == selected.level + 1);
                        if (nextUpg != null)
                        {
                            string resName = nextUpg.costType == ResourceType.Gold ? "G" : nextUpg.costType == ResourceType.Wood ? "나무" : nextUpg.costType == ResourceType.Iron ? "철" : "망치";
                            _upgradeBtnText.text = $"Lv.{selected.level}({nextUpg.cost}{resName})";
                            _upgradeBtn.interactable = GameState.HasResource(nextUpg.costType, nextUpg.cost);
                        }
                        else
                        {
                            _upgradeBtnText.text = $"Lv.{selected.level}(MAX)";
                            _upgradeBtn.interactable = false;
                        }
                    }
                }
                else
                {
                    _upgradeBtn.gameObject.SetActive(false);
                }
            }
            
            if (_smelterySelectionContainer != null)
            {
                if (gridRenderer != null && gridRenderer.SelectedBuilding != null && !BuildManager.Instance.IsPlacing && targetData.buildingType == BuildingType.Smeltery)
                {
                    _smelterySelectionContainer.SetActive(true);
                    ResourceType currentSelection = gridRenderer.SelectedBuilding.selectedAmmoType;
                    for (int i = 0; i < _smelterySelectionContainer.transform.childCount; i++)
                    {
                        var child = _smelterySelectionContainer.transform.GetChild(i);
                        var img = child.GetComponent<Image>();
                        if (img != null)
                        {
                            ResourceType rt = ResourceType.None;
                            if (child.name == "RoundStone") rt = ResourceType.RoundStone;
                            else if (child.name == "Arrow") rt = ResourceType.Arrow;
                            else if (child.name == "GunAmmo") rt = ResourceType.GunAmmo;
                            
                            img.color = rt == currentSelection ? Color.green : Color.gray;
                        }
                    }
                }
                else
                {
                    _smelterySelectionContainer.SetActive(false);
                }
            }
        }
        else if (_infoPopup != null)
        {
            _infoPopup.SetActive(false);
        }

        if (gridRenderer != null && gridRenderer.SelectedBuilding != null)
        {
            var selected = gridRenderer.SelectedBuilding;
            bool isBuildMode = BuildManager.Instance != null && BuildManager.Instance.isBuildModeActive;
            
            if (!isBuildMode && selected.data.buildingType != BuildingType.Mine && selected.data.buildingType != BuildingType.Entrance)
            {
                if (_sellButton != null) _sellButton.gameObject.SetActive(true);
            }
            else
            {
                if (_sellButton != null) _sellButton.gameObject.SetActive(false);
            }
        }
        else
        {
            if (_sellButton != null) _sellButton.gameObject.SetActive(false);
        }
    }
    
    private void CreateBuildButtons()
    {
        if (buttonsContainer == null) return;
        
        var hLayout = buttonsContainer.GetComponent<HorizontalLayoutGroup>();
        if (hLayout != null) DestroyImmediate(hLayout);
        
                var vLayout = buttonsContainer.GetComponent<VerticalLayoutGroup>();
        if (vLayout != null) DestroyImmediate(vLayout);
        if (hLayout != null) DestroyImmediate(hLayout);
        
        var grid = buttonsContainer.GetComponent<GridLayoutGroup>();
        if (grid == null) grid = buttonsContainer.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(160, 60);
        grid.spacing = new Vector2(10, 10);
        grid.childAlignment = TextAnchor.MiddleCenter;
        
        var containerRt = buttonsContainer.GetComponent<RectTransform>();
        containerRt.sizeDelta = new Vector2(1100, 240); // taller to fit rows
        
        // Destroy existing buttons if any
        for (int i = buttonsContainer.childCount - 1; i >= 0; i--)
        {
            var child = buttonsContainer.GetChild(i);
            child.SetParent(null);
            Destroy(child.gameObject);
        }
        
        foreach (var data in BuildManager.Instance.availableBuildings)
        {
            bool allowedInVillage = false;
            bool allowedInDefenseOrTower = true;
            
            if (data.allowedZones != null && data.allowedZones.Count > 0)
            {
                allowedInVillage = data.allowedZones.Contains(ZoneType.Village);
                allowedInDefenseOrTower = data.allowedZones.Contains(ZoneType.Defense) || data.allowedZones.Contains(ZoneType.Tower);
            }
            
            if (_isVillageView && !allowedInVillage) continue;
            if (!_isVillageView && !allowedInDefenseOrTower) continue;
            
            var btnGo = new GameObject($"Btn_{data.buildingName}");
            btnGo.transform.SetParent(buttonsContainer, false);
            
            btnGo.AddComponent<Image>().color = Color.white;
            var btn = btnGo.AddComponent<Button>();
            
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(btnGo.transform, false);
            var textMesh = textGo.AddComponent<TextMeshProUGUI>();
            if (_koreanFont != null) textMesh.font = _koreanFont;
            string resName = data.costType == ResourceType.Gold ? "G" : data.costType == ResourceType.Wood ? "나무" : data.costType == ResourceType.Iron ? "철" : "망치";
            string dName = string.IsNullOrEmpty(data.displayName) ? data.buildingName : data.displayName;
            textMesh.text = $"{dName}\n{data.cost}{resName} / X0";
            textMesh.color = Color.black;
            textMesh.fontSize = 20; // slightly smaller text
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 60);
            
            var capturedData = data; btn.onClick.AddListener(() => {
                if (BuildManager.Instance.GetRemainingCount(capturedData.buildingName) > 0)
                    BuildManager.Instance.StartPlacement(capturedData);
            });
        }
    }
    
    private void UpdateBuildButtons()
    {
        if (buttonsContainer == null) return;
        var buttons = buttonsContainer.GetComponentsInChildren<Button>();
        foreach (var btn in buttons)
        {
            var child = btn.transform;
            if (!child.name.StartsWith("Btn_")) continue;
            
            string bName = child.name.Replace("Btn_", "");
            int remain = BuildManager.Instance.GetRemainingCount(bName);
            var txt = child.GetComponentInChildren<TextMeshProUGUI>();
            
            if (txt != null)
            {
                var bData = BuildManager.Instance.availableBuildings.FirstOrDefault(b => b.buildingName == bName);
                if (bData != null)
                {
                    string resName = bData.costType == ResourceType.Gold ? "G" : bData.costType == ResourceType.Wood ? "나무" : bData.costType == ResourceType.Iron ? "철" : "망치";
                    string bdName = string.IsNullOrEmpty(bData.displayName) ? bData.buildingName : bData.displayName;
                    txt.text = $"{bdName}\n{bData.cost}{resName} / X{remain}";
                }
            }
            
            btn.interactable = remain > 0;
            child.GetComponent<Image>().color = remain > 0 ? Color.white : new Color(0.8f, 0.8f, 0.8f);
        }
    }
    
    private void OnSellClicked()
    {
        var gridRenderer = Object.FindObjectOfType<GridRenderer>();
        if (gridRenderer != null && gridRenderer.SelectedBuilding != null)
        {
            var selected = gridRenderer.SelectedBuilding;
            if (selected.data.buildingType == BuildingType.Mine || selected.data.buildingType == BuildingType.Entrance) return;
            
            int refund = Mathf.FloorToInt(selected.data.cost * 0.9f);
            GameState.RefundResource(selected.data.costType, refund);
            GridManager.Instance.RemoveBuilding(selected.id);
            gridRenderer.SelectedBuilding = null;
        }
    }

    private void OnUpgradeClicked()
    {
        var gridRenderer = Object.FindObjectOfType<GridRenderer>();
        if (gridRenderer != null && gridRenderer.SelectedBuilding != null)
        {
            var selected = gridRenderer.SelectedBuilding;
            var nextUpg = selected.data.upgrades?.FirstOrDefault(u => u.level == selected.level + 1);
            
            if (nextUpg != null && GameState.HasResource(nextUpg.costType, nextUpg.cost))
            {
                GameState.ConsumeResource(nextUpg.costType, nextUpg.cost);
                selected.level++;
                UpdateSelectionUI();
                if (_buttonsCreated) UpdateBuildButtons();
            }
        }
    }
}






