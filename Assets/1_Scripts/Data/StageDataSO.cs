using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BuildingCount
{
    public string buildingName;
    public int count;
}

[CreateAssetMenu(fileName = "StageData", menuName = "Adventure/StageData")]
public class StageDataSO : ScriptableObject
{
    public int stageNumber;
    public float monsterHp;
    public float monsterSpeed;
    public float spawnDelay;
    public int totalMonsters;
    public int clearRewardGold;
    public List<BuildingCount> buildingRewards = new List<BuildingCount>();
}
