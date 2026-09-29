using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SupplyChainManager : MonoBehaviour
{
    public static SupplyChainManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    
    void Update()
    {
        var allBuildings = GridManager.Instance.GetAllBuildings().ToList();
        
                // 0. Process Roads
        foreach (var road in allBuildings.Where(b => b.data.buildingType == BuildingType.Road || b.data.buildingType == BuildingType.Entrance))
        {
            road.productionTimer += Time.deltaTime;
            if (road.productionTimer >= 0.2f)
            {
                if (road.inputQueue.Count > 0)
                {
                    var item = road.inputQueue.Peek();
                    if (DistributeOutput(road, item.type, item, allBuildings))
                    {
                        road.inputQueue.Dequeue();
                    }
                }
                // Only reset timer when an item actually moves, so the next item moves immediately if queued?
                // No, a belt moves continuously. If we dequeue, we must wait 0.2f.
                road.productionTimer = 0f;
            }
        }
        
        
        // 1. Process Mines
        foreach (var mine in allBuildings.Where(b => b.data.buildingType == BuildingType.Mine))
        {
            mine.mineTimer += Time.deltaTime;
            float productionTime = mine.data.productionTime > 0 ? mine.data.productionTime : 0.1f;
            
            if (mine.mineTimer >= productionTime)
            {
                mine.mineTimer = 0;
                if (mine.currentOutput < mine.data.maxOutputCapacity)
                {
                    mine.currentOutput++;
                }
            }
            
            if (mine.currentOutput <= 0) mine.isShutdown = true;
            else mine.isShutdown = false;
            
            if (!mine.isShutdown && mine.currentOutput > 0)
            {
                var item = new AmmoItem(mine.data.outputType);
                if (DistributeOutput(mine, mine.data.outputType, item, allBuildings)) 
                {
                    mine.currentOutput--;
                }
                else
                {
                    // Blocked!
                    mine.isShutdown = true;
                }
            }
        }
        
        // 2. Process Factories
        foreach (var factory in allBuildings.Where(b => b.data.buildingType == BuildingType.Factory))
        {
            bool hasInput1 = factory.data.inputType1 == ResourceType.None || factory.currentInput1 > 0;
            bool hasInput2 = factory.data.inputType2 == ResourceType.None || factory.currentInput2 > 0;
            
            if (!hasInput1 || !hasInput2) factory.isShutdown = true;
            else factory.isShutdown = false;
            
            if (!factory.isShutdown && factory.currentOutput < factory.data.maxOutputCapacity)
            {
                factory.productionTimer += Time.deltaTime;
                float prodTime = factory.data.productionTime > 0 ? factory.data.productionTime : 1f;
                float buff = GetSpeedBuff(factory, allBuildings);
                prodTime = Mathf.Max(0.1f, prodTime - buff - GridDomainLogic.GetUpgradedValue(factory));
                
                if (factory.productionTimer >= prodTime)
                {
                    factory.productionTimer = 0;
                    if (factory.data.inputType1 != ResourceType.None) factory.currentInput1--;
                    if (factory.data.inputType2 != ResourceType.None) factory.currentInput2--;
                    factory.currentOutput++;
                }
            }
            
            if (factory.currentOutput > 0)
            {
                var item = new AmmoItem(factory.data.outputType);
                if (DistributeOutput(factory, factory.data.outputType, item, allBuildings)) 
                {
                    factory.currentOutput--;
                }
                else
                {
                    // If full, we should probably stop producing too, which is natural because currentOutput won't be < maxOutputCapacity
                }
            }
        }
        
        // 3. Process Smelteries
        foreach (var smeltery in allBuildings.Where(b => b.data.buildingType == BuildingType.Smeltery))
        {
            if (smeltery.selectedAmmoType == ResourceType.None)
            {
                smeltery.isShutdown = true;
                continue;
            }
            
            bool hasInput = smeltery.inputQueue.Count > 0;
            if (!hasInput) smeltery.isShutdown = true;
            else smeltery.isShutdown = false;
            
            if (!smeltery.isShutdown && smeltery.outputQueue.Count < smeltery.data.maxOutputCapacity)
            {
                smeltery.productionTimer += Time.deltaTime;
                float prodTime = smeltery.data.productionTime > 0 ? smeltery.data.productionTime : 1f;
                
                if (smeltery.productionTimer >= prodTime)
                {
                    smeltery.productionTimer = 0;
                    var item = smeltery.inputQueue.Dequeue();
                    smeltery.currentInput1--;
                    if (smeltery.data.buildingName == "SlowEffect") 
                    {
                        item.slowAmount += 0.5f + GridDomainLogic.GetUpgradedValue(smeltery);
                        item.slowAmount = Mathf.Min(0.9f, item.slowAmount);
                    }
                    if (smeltery.data.buildingName == "DamageEffect") 
                    {
                        item.bonusDamage += 50f + GridDomainLogic.GetUpgradedValue(smeltery);
                    }
                    smeltery.outputQueue.Enqueue(item);
                    smeltery.currentOutput++;
                }
            }
            
            if (smeltery.outputQueue.Count > 0)
            {
                var item = smeltery.outputQueue.Peek();
                if (DistributeOutput(smeltery, item.type, item, allBuildings))
                {
                    smeltery.outputQueue.Dequeue();
                    smeltery.currentOutput--;
                }
            }
        }
    }
    
    private bool DistributeOutput(BuildingModel source, ResourceType baseType, AmmoItem itemToDistribute, List<BuildingModel> allBuildings)
    {
        int targetX = source.x;
        int targetY = source.y;
        
        switch (source.direction)
        {
            case Direction.Up: targetY += 1; break;
            case Direction.Down: targetY -= 1; break;
            case Direction.Left: targetX -= 1; break;
            case Direction.Right: targetX += 1; break;
        }
        
        var target = GridManager.Instance.GetBuildingAt(targetX, targetY);
        if (target != null && GiveResource(target, baseType, itemToDistribute))
        {
            return true;
        }
        return false;
    }
    
    private bool GiveResource(BuildingModel target, ResourceType type, AmmoItem itemToDistribute)
    {
        if (target.data.buildingType == BuildingType.Tower)
        {
            if (target.data.requiredAmmoType == type && target.currentInput1 < target.data.maxAmmo)
            {
                target.currentInput1++;
                target.inputQueue.Enqueue(itemToDistribute ?? new AmmoItem(type));
                return true;
            }
        }
        else if (target.data.buildingType == BuildingType.Smeltery)
        {
            if (target.selectedAmmoType == type && target.currentInput1 < target.data.maxInputCapacity)
            {
                target.currentInput1++;
                target.inputQueue.Enqueue(itemToDistribute ?? new AmmoItem(type));
                return true;
            }
        }
        else if (target.data.buildingType == BuildingType.Factory)
        {
            if (target.data.inputType1 == type && target.currentInput1 < target.data.maxInputCapacity)
            {
                target.currentInput1++;
                return true;
            }
            if (target.data.inputType2 == type && target.currentInput2 < target.data.maxInputCapacity)
            {
                target.currentInput2++;
                return true;
            }
        }
        else if (target.data.buildingType == BuildingType.Hall)
        {
            if (type == ResourceType.Wood) { GameState.currentWood++; return true; }
            if (type == ResourceType.Iron) { GameState.currentIron++; return true; }
            if (type == ResourceType.Hammer) { GameState.currentHammer++; return true; }
        }
        else if (target.data.buildingType == BuildingType.Road || target.data.buildingType == BuildingType.Entrance)
        {
            // Pipeline queue capacity: 1 per tile
            if (target.inputQueue.Count < 1)
            {
                target.inputQueue.Enqueue(itemToDistribute ?? new AmmoItem(type));
                return true;
            }
        }
        return false;
    }
    
    private float GetSpeedBuff(BuildingModel factory, List<BuildingModel> allBuildings)
    {
        float buff = 0f;
        foreach (var b in allBuildings.Where(x => x.data.buildingType == BuildingType.FactorySpeedBuff))
        {
            if (GridDomainLogic.IsInRange(factory.x, factory.y, b.x, b.y, b.data.buffRange))
            {
                buff += 0.2f + GridDomainLogic.GetUpgradedValue(b);
            }
        }
        return buff;
    }
}

