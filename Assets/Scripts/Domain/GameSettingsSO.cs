using UnityEngine;

// Preserves the project's pre-existing serialized settings without losing their values.
// The implemented game reads LinkDeployCatalog instead.
public class GameSettingsSO : ScriptableObject
{
    [Header("Legacy settings - edit LinkDeployCatalog for the current game")]
    public int startingGold = 10000;
    public float monsterSpawnDelay = 1;
    public float monsterBaseHp = 150;
    public int monsterHpIncreaseStep = 10;
    public float monsterHpIncreasePercent = .1f;
}
