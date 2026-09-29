using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class GridDomainLogic
{
    public static bool IsInRange(int startX, int startY, int targetX, int targetY, int range) =>
        Mathf.Abs(startX - targetX) <= range && Mathf.Abs(startY - targetY) <= range;
        
    public static float GetTowerAttackDamage(BuildingModel tower, IEnumerable<BuildingModel> allBuildings)
    {
        var attackBuffs = allBuildings
            .Where(building => building.data.buildingType == BuildingType.TowerAttackBuff)
            .Where(building => IsInRange(tower.x, tower.y, building.x, building.y, building.data.buffRange))
            .Sum(building => building.data.buffAmount + GetUpgradedValue(building));
            
        float myUpgradeBuff = GetUpgradedValue(tower);
            
        return tower.data.attackDamage + myUpgradeBuff + attackBuffs;
    }

    public static MonsterModel GetClosestMonster(BuildingModel tower, IEnumerable<MonsterModel> monsters)
    {
        MonsterModel closestMonster = null;
        float minDistance = float.MaxValue;
        
        Vector2 towerPosition = new Vector2(tower.x, tower.y);
        
        foreach (var monster in monsters)
        {
            Vector2 monsterPosition = new Vector2(monster.currentPosition.x, monster.currentPosition.y);
            
            float dx = Mathf.Abs(towerPosition.x - monsterPosition.x);
            float dy = Mathf.Abs(towerPosition.y - monsterPosition.y);
            float halfRange = tower.data.attackRange / 2f;
            
            if (dx <= halfRange && dy <= halfRange)
            {
                float distance = Vector2.Distance(towerPosition, monsterPosition);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestMonster = monster;
                }
            }
        }
        
        return closestMonster;
    }
    
    public static float GetUpgradedValue(BuildingModel building)
    {
        float totalIncrease = 0f;
        if (building.data.upgrades != null)
        {
            foreach (var upg in building.data.upgrades)
            {
                if (building.level >= upg.level)
                {
                    totalIncrease += upg.effectAmount;
                }
            }
        }
        return totalIncrease;
    }
    
    public static bool IsMatchingAmmo(ResourceType required, ResourceType provided)
    {
        if (required == provided) return true;
        if (required == ResourceType.Arrow && (provided == ResourceType.IceArrow || provided == ResourceType.FireArrow)) return true;
        if (required == ResourceType.RoundStone && (provided == ResourceType.IceStone || provided == ResourceType.FireStone)) return true;
        if (required == ResourceType.GunAmmo && (provided == ResourceType.IceGunAmmo || provided == ResourceType.FireGunAmmo)) return true;
        return false;
    }
}
