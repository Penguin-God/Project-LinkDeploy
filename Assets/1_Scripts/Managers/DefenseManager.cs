using UnityEngine;
using System.Linq;

public class DefenseManager : MonoBehaviour
{
    public static DefenseManager Instance { get; private set; }
    
    public float elapsedTime = 0f;
    private bool _isGameOver = false;

    private StageDataSO _currentStageData;
    private int _monstersSpawned = 0;
    private int _monstersKilled = 0;
    
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    
    void Start()
    {
        _currentStageData = Resources.Load<StageDataSO>($"Stages/Stage_{GameState.currentPlayingStage}");
        
        MonsterManager.Instance.OnMonsterReachedEnd += HandleGameOver;
        MonsterManager.Instance.OnMonsterKilled += HandleMonsterKilled;
        
        StartCoroutine(SpawnMonstersRoutine());
    }
    
    private void HandleMonsterKilled(int reward)
    {
        GameState.currentGold += reward;
        _monstersKilled++;
        
        if (_monstersKilled >= _currentStageData.totalMonsters && !_isGameOver)
        {
            HandleGameWin();
        }
    }
    
    private System.Collections.IEnumerator SpawnMonstersRoutine()
    {
        while (!_isGameOver && _monstersSpawned < _currentStageData.totalMonsters)
        {
            var monsterData = Resources.Load<MonsterDataSO>("Monsters/BasicMonster");
            if (monsterData != null)
            {
                // We use StageDataSO monsterHp
                float hp = _currentStageData.monsterHp;
                float speed = _currentStageData.monsterSpeed;
                
                MonsterManager.Instance.SpawnMonster(monsterData, hp / monsterData.maxHp, speed); 
                _monstersSpawned++;
            }
            float delay = _currentStageData.spawnDelay;
            yield return new WaitForSeconds(delay);
        }
    }
    
    void Update()
    {
        if (_isGameOver) return;
        
        elapsedTime += Time.deltaTime;
        
        var allBuildings = GridManager.Instance.GetAllBuildings();
        var towers = allBuildings.Where(b => b.data.buildingType == BuildingType.Tower).ToList();
        var monsters = MonsterManager.Instance.GetActiveMonsters().ToList();
        
        // 타워 공격 로직
        foreach (var tower in towers)
        {
            if (tower.currentInput1 > 0 && tower.isShutdown)
            {
                tower.isShutdown = false;
            }
            
            if (tower.currentInput1 <= 0)
            {
                tower.isShutdown = true;
            }
            
            if (tower.isShutdown) continue;
            
            tower.attackTimer += Time.deltaTime;
            float cooldown = tower.data.attackSpeed > 0f ? 1f / tower.data.attackSpeed : float.MaxValue;
            if (tower.attackTimer >= cooldown)
            {
                var targetMonster = GridDomainLogic.GetClosestMonster(tower, monsters);
                if (targetMonster != null && tower.currentInput1 > 0)
                {
                    tower.attackTimer = 0f;
                    tower.currentInput1--;
                    AmmoItem ammoUsed = null;
                    if (tower.inputQueue != null && tower.inputQueue.Count > 0)
                    {
                        ammoUsed = tower.inputQueue.Dequeue();
                    }
                    
                    float damage = GridDomainLogic.GetTowerAttackDamage(tower, allBuildings);
                    float slowAmt = 0f;
                    
                    if (ammoUsed != null)
                    {
                        damage += ammoUsed.bonusDamage;
                        slowAmt = ammoUsed.slowAmount;
                    }
                    
                    bool isIce = slowAmt > 0f;
                    
                    ProjectileManager.Instance.FireProjectile(new Vector3(tower.x, tower.y, 0), targetMonster.id, damage, 10f, isIce, slowAmt);
                }
            }
        }
    }
    
    private void HandleGameOver()
    {
        if (_isGameOver) return;
        _isGameOver = true;
        Debug.Log("Game Over! A monster reached the end.");
        
        var uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null) uiManager.ShowEndGame(false);
    }
    
    private void HandleGameWin()
    {
        if (_isGameOver) return;
        _isGameOver = true;
        Debug.Log("Stage Clear!");
        
        GameState.currentGold += _currentStageData.clearRewardGold;
        
        if (GameState.unlockedStage == GameState.currentPlayingStage && GameState.unlockedStage < 4)
        {
            GameState.unlockedStage++;
        }
        
        var uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null) uiManager.ShowEndGame(true, _currentStageData.clearRewardGold);
    }
}
