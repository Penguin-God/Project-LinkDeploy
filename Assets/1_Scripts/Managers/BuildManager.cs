using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BuildManager : MonoBehaviour
{
    public static BuildManager Instance { get; private set; }
    
    public List<BuildingDataSO> availableBuildings;
    
    public bool isBuildModeActive = false;
    public Direction currentDirection = Direction.Right;
    
    private BuildingDataSO _buildingToPlace;
    private bool _isPlacing;
    
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    
        void Start()
    {
        availableBuildings = Resources.LoadAll<BuildingDataSO>("Buildings")
            .Where(b => b.buildingType != BuildingType.Entrance && b.buildingType != BuildingType.Hall)
            .ToList();
    }
    
    public int GetRemainingCount(string buildingName)
    {
        int hallLevel = 1;
        if (GridManager.Instance != null)
        {
            var hall = GridManager.Instance.GetAllBuildings().FirstOrDefault(b => b.data.buildingType == BuildingType.Hall);
            if (hall != null) hallLevel = hall.level;
        }
        
                int allowed = 0;
        
        // Mines
        if (buildingName.EndsWith("Mine")) allowed += 15;

        // Lv 1 base limits
        if (buildingName == "StoneFactory") allowed += 2;
        if (buildingName == "Road") allowed += 10;
        if (buildingName == "Slingshot") allowed += 2;
        if (buildingName == "HammerFactory") allowed += 5; // Give them 5 to build in village
        
        // Lv 2: ?占쎄린 ?占쎈젴??X 3, ?占쎈젰 ?占쎈젴??X 3
        if (hallLevel >= 2)
        {
            if (buildingName == "SlowEffect") allowed += 3; 
            if (buildingName == "DamageEffect") allowed += 3;
        }
        
        // Lv 3: 沅곸닔 X2, ?占쎌궡怨듭옣 X 2, ?占쎈줈 X 10
        if (hallLevel >= 3)
        {
            if (buildingName == "Archer") allowed += 2;
            if (buildingName == "ArrowFactory") allowed += 2;
            if (buildingName == "Road") allowed += 10;
        }
        
        // Lv 4: 珥앹븣 怨듭옣 X 3, ?占쎈줈 X 15, 占?X1
        if (hallLevel >= 4)
        {
            if (buildingName == "AmmoFactory") allowed += 3;
            if (buildingName == "Road") allowed += 15;
            if (buildingName == "Gun") allowed += 1;
        }
        
        // Lv 5: 怨듭옣 踰꾪봽 X3, ?占?占쎈쾭??X2, 占?X3
        if (hallLevel >= 5)
        {
            if (buildingName == "FactoryBuff") allowed += 3;
            if (buildingName == "TowerBuff") allowed += 2;
            if (buildingName == "Gun") allowed += 3;
        }
        
        int built = 0;
        if (GridManager.Instance != null)
            built = GridManager.Instance.GetAllBuildings().Count(b => b.data.buildingName == buildingName);
            
        return allowed - built;
    }
    
    public bool IsPlacing => _isPlacing;
    public BuildingDataSO BuildingToPlace => _buildingToPlace;
    
    
    public void ToggleBuildMode()
    {
        isBuildModeActive = !isBuildModeActive;
        if (!isBuildModeActive) CancelPlacement();
        else
        {
            var gridRenderer = Object.FindObjectOfType<GridRenderer>();
            if (gridRenderer != null) gridRenderer.SelectedBuilding = null;
        }
    }
    
    void Update()
    {
        if (!isBuildModeActive) return;
        
        if (_isPlacing && _buildingToPlace != null)
        {
                        if (Input.GetKeyDown(KeyCode.R))
            {
                if (_buildingToPlace.buildingType == BuildingType.Road ||
                    _buildingToPlace.buildingType == BuildingType.Factory ||
                    _buildingToPlace.buildingType == BuildingType.Smeltery ||
                    _buildingToPlace.buildingType == BuildingType.Mine)
                {
                    currentDirection = (Direction)(((int)currentDirection + 1) % 4);
                }
            }
            
            if (Input.GetMouseButtonDown(0))
            {
                if (UnityEngine.EventSystems.EventSystem.current != null && 
                    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) 
                    return;
                    
                var cam = Camera.main ?? Object.FindObjectOfType<Camera>();
                if (cam == null) return;
                Vector3 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
                int posX = Mathf.RoundToInt(mousePos.x);
                int posY = Mathf.RoundToInt(mousePos.y);
                
                if (IsValidPlacement(posX, posY))
                {
                    int cost = _buildingToPlace.cost;
                    
                    if (GameState.HasResource(_buildingToPlace.costType, cost) && GetRemainingCount(_buildingToPlace.buildingName) > 0)
                    {
                        GameState.ConsumeResource(_buildingToPlace.costType, cost);
                        bool placed = GridManager.Instance.PlaceBuilding(_buildingToPlace, posX, posY, currentDirection);
                        if (!placed)
                        {
                            GameState.RefundResource(_buildingToPlace.costType, cost);
                            CancelPlacement();
                        }
                    }
                    else
                    {
                        CancelPlacement();
                    }
                }
            }
        }
        
        if (Input.GetMouseButtonDown(1))
        {
            CancelPlacement();
        }
    }
    
    public void StartPlacement(BuildingDataSO data)
    {
        if (GetRemainingCount(data.buildingName) <= 0) return;
        
        _buildingToPlace = data;
        _isPlacing = true;
    }
    
    public void CancelPlacement()
    {
        _buildingToPlace = null;
        _isPlacing = false;
    }
    
    public bool IsValidPlacement(int posX, int posY)
    {
        if (GridManager.Instance.GetBuildingAt(posX, posY) != null) return false;
        if (_buildingToPlace == null) return false;
        
        return GridManager.Instance.IsValidCoordinate(posX, posY, _buildingToPlace);
    }
}





