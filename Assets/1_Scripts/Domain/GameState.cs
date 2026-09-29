using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BuildingSaveData
{
    public string id;
    public string buildingName; // To look up the SO
    public int x;
    public int y;
    
    public int currentInput1;
    public int currentInput2;
    public int currentOutput;
    public int level;
    public ResourceType selectedAmmoType = ResourceType.None;
    public Direction direction = Direction.Right;
    
    public List<AmmoItem> savedInputQueue = new List<AmmoItem>();
    public List<AmmoItem> savedOutputQueue = new List<AmmoItem>();
}

public static class GameState
{
    public static int unlockedStage = 1;
    public static int currentPlayingStage = 1;
    public static int currentGold = 10000;
    public static int currentWood = 10000;
    public static int currentIron = 10000;
    public static int currentHammer = 10000;
    
    public static bool HasResource(ResourceType type, int amount)
    {
        switch (type)
        {
            case ResourceType.Gold: return currentGold >= amount;
            case ResourceType.Wood: return currentWood >= amount;
            case ResourceType.Iron: return currentIron >= amount;
            case ResourceType.Hammer: return currentHammer >= amount;
        }
        return false;
    }
    
    public static void ConsumeResource(ResourceType type, int amount)
    {
        switch (type)
        {
            case ResourceType.Gold: currentGold -= amount; break;
            case ResourceType.Wood: currentWood -= amount; break;
            case ResourceType.Iron: currentIron -= amount; break;
            case ResourceType.Hammer: currentHammer -= amount; break;
        }
    }
    
    public static void RefundResource(ResourceType type, int amount)
    {
        switch (type)
        {
            case ResourceType.Gold: currentGold += amount; break;
            case ResourceType.Wood: currentWood += amount; break;
            case ResourceType.Iron: currentIron += amount; break;
            case ResourceType.Hammer: currentHammer += amount; break;
        }
    }
    
    public static List<BuildingSaveData> savedBuildings = new List<BuildingSaveData>();
    
    public static void SaveGrid(IEnumerable<BuildingModel> buildings)
    {
        savedBuildings.Clear();
        foreach (var b in buildings)
        {
            var data = new BuildingSaveData
            {
                id = b.id,
                buildingName = b.data.buildingName,
                x = b.x,
                y = b.y,
                currentInput1 = b.currentInput1,
                currentInput2 = b.currentInput2,
                currentOutput = b.currentOutput,
                level = b.level,
                selectedAmmoType = b.selectedAmmoType,
                direction = b.direction
            };
            if (b.inputQueue != null) data.savedInputQueue = new List<AmmoItem>(b.inputQueue);
            if (b.outputQueue != null) data.savedOutputQueue = new List<AmmoItem>(b.outputQueue);
            
            savedBuildings.Add(data);
        }
    }
}
