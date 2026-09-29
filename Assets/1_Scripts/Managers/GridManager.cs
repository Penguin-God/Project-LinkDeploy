using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }
    
    private Dictionary<string, BuildingModel> _buildings = new Dictionary<string, BuildingModel>();
    
    public event Action OnGridChanged;
    public event Action<BuildingModel> OnBuildingPlaced;
    public event Action<string> OnBuildingRemoved;
    
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    
    void Start()
    {
        // Adjust camera to center the map and shift it up to make room for UI
        if (Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(7f, 0f, -10f);
            Camera.main.orthographicSize = 9f;
        }
        
        // Load from GameState
        foreach (var b in GameState.savedBuildings)
        {
            PlaceFixedBuilding(b.buildingName, b.x, b.y, b.id, b);
        }
        
                // Ensure Defense fixed buildings exist
        if (GetBuildingAt(0, 0) == null) PlaceFixedBuilding("Entrance", 0, 0);
        if (GetBuildingAt(7, 0) == null) PlaceFixedBuilding("Entrance", 7, 0);
        if (GetBuildingAt(14, 0) == null) PlaceFixedBuilding("Entrance", 14, 0);
        
        // Ensure Village fixed buildings exist
        if (GetBuildingAt(54, 54) == null) PlaceFixedBuilding("TownHall", 54, 54);
    }
    
    private void PlaceFixedBuilding(string buildingName, int x, int y, string existingId = null, BuildingSaveData saveData = null)
    {
        var data = Resources.Load<BuildingDataSO>($"Buildings/{buildingName}");
        if (data != null)
        {
            string newId = string.IsNullOrEmpty(existingId) ? System.Guid.NewGuid().ToString() : existingId;
            var buildingModel = new BuildingModel(newId, x, y, data, saveData != null ? saveData.direction : Direction.Right);
            
            if (saveData != null)
            {
                buildingModel.currentInput1 = saveData.currentInput1;
                buildingModel.currentInput2 = saveData.currentInput2;
                buildingModel.currentOutput = saveData.currentOutput;
                buildingModel.level = saveData.level > 0 ? saveData.level : 1;
                buildingModel.selectedAmmoType = saveData.selectedAmmoType;
                
                if (saveData.savedInputQueue != null && saveData.savedInputQueue.Count > 0)
                {
                    foreach (var item in saveData.savedInputQueue) buildingModel.inputQueue.Enqueue(item);
                }
                else if (buildingModel.data.buildingType == BuildingType.Tower)
                {
                    for (int i=0; i<buildingModel.currentInput1; i++) buildingModel.inputQueue.Enqueue(new AmmoItem(buildingModel.data.requiredAmmoType));
                }
                else if (buildingModel.data.buildingType == BuildingType.Smeltery)
                {
                    for (int i=0; i<buildingModel.currentInput1; i++) buildingModel.inputQueue.Enqueue(new AmmoItem(buildingModel.selectedAmmoType));
                }
                
                if (saveData.savedOutputQueue != null && saveData.savedOutputQueue.Count > 0)
                {
                    foreach (var item in saveData.savedOutputQueue) buildingModel.outputQueue.Enqueue(item);
                }
                else if (buildingModel.data.buildingType == BuildingType.Smeltery)
                {
                    for (int i=0; i<buildingModel.currentOutput; i++) 
                    {
                        var item = new AmmoItem(buildingModel.selectedAmmoType);
                        if (buildingModel.data.buildingName == "SlowEffect") item.slowAmount = 0.5f + GridDomainLogic.GetUpgradedValue(buildingModel);
                        if (buildingModel.data.buildingName == "DamageEffect") item.bonusDamage = 50f + GridDomainLogic.GetUpgradedValue(buildingModel);
                        buildingModel.outputQueue.Enqueue(item);
                    }
                }
            }
            
            _buildings[newId] = buildingModel;
            OnBuildingPlaced?.Invoke(buildingModel);
            
            if (string.IsNullOrEmpty(existingId))
                GameState.SaveGrid(_buildings.Values);
        }
    }
    
    public IEnumerable<BuildingModel> GetAllBuildings() => _buildings.Values;
    
    public BuildingModel GetBuildingAt(int gridX, int gridY) => _buildings.Values.FirstOrDefault(building => building.x == gridX && building.y == gridY);
    
    public bool IsValidCoordinate(int gridX, int gridY, BuildingDataSO data)
    {
        bool inVillage = gridX >= 50 && gridX <= 58 && gridY >= 50 && gridY <= 58;
        bool inDefense = gridX >= 0 && gridX <= 14 && gridY >= 0 && gridY <= 7;
        bool inTower = false;
        
        // Tower Zones are X: 0,1, X: 7,8, X: 13,14 and Y: -3..-1. Wait! The user said 2x3.
        // Entrance is at (0,0), (7,0), (14,0).
        // Y=-1 is the visual road. So tower zone is Y=-4..-2?
        // Let's make it Y=-4 to -2, X=0,1 | 7,8 | 13,14
                if (gridY >= -3 && gridY <= -2)
        {
            if (gridX >= 0 && gridX <= 2) inTower = true;
            else if (gridX >= 6 && gridX <= 8) inTower = true;
            else if (gridX >= 12 && gridX <= 14) inTower = true;
        }

        bool valid = false;
        if (data.allowedZones != null)
        {
            if (inVillage && data.allowedZones.Contains(ZoneType.Village)) valid = true;
            if (inDefense && data.allowedZones.Contains(ZoneType.Defense)) valid = true;
            if (inTower && data.allowedZones.Contains(ZoneType.Tower)) valid = true;
        }
        
        if (!valid && data.buildingType == BuildingType.Road)
        {
            if (gridY >= -4 && gridY <= -1 && (gridX == 0 || gridX == 7 || gridX == 14)) valid = true;
            if (gridY >= -4 && gridY <= -1 && (gridX == 1 || gridX == 8 || gridX == 13)) valid = true;
        }
        
        return valid;
    }
    
    public bool PlaceBuilding(BuildingDataSO buildingData, int gridX, int gridY, Direction dir = Direction.Right)
    {
        if (GetBuildingAt(gridX, gridY) != null) return false; 
        if (!IsValidCoordinate(gridX, gridY, buildingData)) return false; 
        
        string newId = System.Guid.NewGuid().ToString();
        var buildingModel = new BuildingModel(newId, gridX, gridY, buildingData, dir);
        _buildings[newId] = buildingModel;
        
        OnBuildingPlaced?.Invoke(buildingModel);
        OnGridChanged?.Invoke();
        GameState.SaveGrid(_buildings.Values);
        return true;
    }
    
    public void RemoveBuilding(string buildingId)
    {
        if (_buildings.ContainsKey(buildingId))
        {
            _buildings.Remove(buildingId);
            OnBuildingRemoved?.Invoke(buildingId);
            OnGridChanged?.Invoke();
            GameState.SaveGrid(_buildings.Values);
        }
    }
    
    public bool MoveBuilding(string buildingId, int targetX, int targetY)
    {
        if (!_buildings.ContainsKey(buildingId)) return false;
        
        var building = _buildings[buildingId];
        // Fixed buildings cannot be moved
        if (building.data.buildingType == BuildingType.Entrance) return false;
        
        if (GetBuildingAt(targetX, targetY) != null) return false;
        if (!IsValidCoordinate(targetX, targetY, building.data)) return false;
        
        building.x = targetX;
        building.y = targetY;
        OnGridChanged?.Invoke();
        GameState.SaveGrid(_buildings.Values);
        return true;
    }
}



