using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class Cargo
{
    public MaterialKind material;
    public float damage;
    public float slow;
    public Cargo Copy() => (Cargo)MemberwiseClone();
}

[Serializable]
public class BuildingState
{
    public int id;
    public string definitionId;
    public int zone = -1;
    public int column;
    public int row;
    public FlowDirection direction = FlowDirection.Down;
    public int level;
    public int investment;
    public float progress;
    public float cooldown;
    public float lastTransferTime = -100;
    public int targetId = -1;
    public MaterialKind forgeFilter;
    public List<Cargo> input = new List<Cargo>();
    public List<Cargo> output = new List<Cargo>();
    public BuildingState Copy()
    {
        var copy = (BuildingState)MemberwiseClone();
        copy.input = input.Select(cargo => cargo.Copy()).ToList();
        copy.output = output.Select(cargo => cargo.Copy()).ToList();
        return copy;
    }
}

[Serializable]
public class MonsterState
{
    public int id;
    public float position;
    public float health;
    public float maxHealth;
    public float slow;
    public float slowRemaining;
    public MonsterState Copy() => (MonsterState)MemberwiseClone();
}

[Serializable]
public class ProjectileState
{
    public int id;
    public int targetId;
    public Vector2 position;
    public float damage;
    public float slow;
    public MaterialKind material;
    public ProjectileState Copy() => (ProjectileState)MemberwiseClone();
}

[Serializable]
public class GameState
{
    public int gold;
    public int nextId = 1;
    public int selectedStage;
    public int unlockedStage;
    public int spawned;
    public int defeated;
    public int totalShots;
    public int delivered;
    public float spawnTimer;
    public float elapsed;
    public SessionMode mode;
    public bool[] cleared;
    public List<string> unlockedBuildings = new List<string>();
    public List<BuildingState> buildings = new List<BuildingState>();
    public List<MonsterState> monsters = new List<MonsterState>();
    public List<ProjectileState> projectiles = new List<ProjectileState>();
    public GameState Copy()
    {
        var copy = (GameState)MemberwiseClone();
        copy.cleared = (bool[])cleared.Clone();
        copy.unlockedBuildings = new List<string>(unlockedBuildings);
        copy.buildings = buildings.Select(building => building.Copy()).ToList();
        copy.monsters = monsters.Select(monster => monster.Copy()).ToList();
        copy.projectiles = projectiles.Select(projectile => projectile.Copy()).ToList();
        return copy;
    }
}

// Pure state transitions. No scene objects, audio, file access, random numbers or clock reads.
// Every public mutation returns a copy; callers retain the prior state for retry/save/inspection.
public static class LinkDeploySimulation
{
    public const float MaximumSlow = .9f;
    public const int MaximumLevel = 3;
    public static GameState Create(LinkDeployCatalog catalog) => new GameState
    {
        gold = catalog.startingGold,
        cleared = new bool[catalog.stages.Length],
        unlockedBuildings = catalog.buildings.Where(definition => definition.unlockAfterStage == 0).Select(definition => definition.id).ToList()
    };

    public static bool CanEdit(GameState state) => state.mode == SessionMode.Planning;
    public static Vector2Int Offset(FlowDirection direction) => direction switch
    {
        FlowDirection.Right => Vector2Int.right,
        FlowDirection.Down => Vector2Int.down,
        FlowDirection.Left => Vector2Int.left,
        _ => Vector2Int.up
    };
    public static bool IsEntrance(LinkDeployCatalog catalog, int column, int row) => row == 0 && catalog.entranceColumns.Contains(column);
    public static BuildingState At(GameState state, int zone, int column, int row) => state.buildings.FirstOrDefault(building => building.zone == zone && building.column == column && building.row == row);
    public static int Remaining(GameState state, BuildingDefinition definition) => definition.limit < 0 ? int.MaxValue : definition.limit - state.buildings.Count(building => building.definitionId == definition.id);
    public static bool IsUnlocked(GameState state, BuildingDefinition definition) => state.unlockedBuildings.Contains(definition.id);

    public static string PlacementError(GameState state, LinkDeployCatalog catalog, BuildingDefinition definition, int zone, int column, int row, int movingId = -1)
    {
        if (!CanEdit(state)) return "전투 중에는 건설하거나 이동할 수 없습니다.";
        if (definition.IsTowerArea != (zone >= 0)) return definition.IsTowerArea ? "타워 구역에 설치하세요." : "생산 구역에 설치하세요.";
        if (zone < -1 || zone >= catalog.entranceColumns.Length || column < 0 || row < 0 || column >= (zone < 0 ? catalog.productionWidth : catalog.towerWidth) || row >= (zone < 0 ? catalog.productionHeight : catalog.towerHeight)) return "설치 구역 밖입니다.";
        if (zone < 0 && IsEntrance(catalog, column, row)) return "이 칸은 타워 구역의 보급 입구입니다.";
        var occupant = At(state, zone, column, row);
        if (occupant != null && occupant.id != movingId) return "이미 건물이 있는 칸입니다.";
        if (movingId < 0)
        {
            if (!IsUnlocked(state, definition)) return "앞선 스테이지를 클리어해 해금하세요.";
            if (Remaining(state, definition) <= 0) return "이 건물의 설치 한도에 도달했습니다.";
            if (state.gold < definition.price) return "골드가 부족합니다.";
        }
        return null;
    }

    public static GameState Place(GameState source, LinkDeployCatalog catalog, string definitionId, int zone, int column, int row, FlowDirection direction)
    {
        var definition = catalog.Building(definitionId);
        if (PlacementError(source, catalog, definition, zone, column, row) != null) return source;
        var state = source.Copy();
        state.gold -= definition.price;
        state.buildings.Add(new BuildingState { id = state.nextId++, definitionId = definitionId, zone = zone, column = column, row = row, direction = direction, investment = definition.price });
        return state;
    }

    public static GameState Move(GameState source, LinkDeployCatalog catalog, int id, int zone, int column, int row)
    {
        var original = source.buildings.FirstOrDefault(building => building.id == id);
        if (original == null || PlacementError(source, catalog, catalog.Building(original.definitionId), zone, column, row, id) != null) return source;
        var state = source.Copy();
        var moved = state.buildings.First(building => building.id == id);
        moved.zone = zone; moved.column = column; moved.row = row;
        return state;
    }

    public static GameState Rotate(GameState source, LinkDeployCatalog catalog, int id)
    {
        if (!CanEdit(source)) return source;
        var state = source.Copy();
        var building = state.buildings.FirstOrDefault(candidate => candidate.id == id);
        if (building != null && catalog.Building(building.definitionId).HasOutput) building.direction = (FlowDirection)(((int)building.direction + 1) % 4);
        return state;
    }

    public static GameState Filter(GameState source, int id, MaterialKind filter)
    {
        if (!CanEdit(source)) return source;
        var state = source.Copy();
        var building = state.buildings.FirstOrDefault(candidate => candidate.id == id);
        if (building != null) building.forgeFilter = filter;
        return state;
    }

    public static int UpgradePrice(BuildingState building, BuildingDefinition definition) => building.level < definition.upgradePrices.Length ? definition.upgradePrices[building.level] : -1;
    public static GameState Upgrade(GameState source, LinkDeployCatalog catalog, int id)
    {
        var original = source.buildings.FirstOrDefault(building => building.id == id);
        if (!CanEdit(source) || original == null) return source;
        var price = UpgradePrice(original, catalog.Building(original.definitionId));
        if (price < 0 || source.gold < price) return source;
        var state = source.Copy();
        var building = state.buildings.First(candidate => candidate.id == id);
        state.gold -= price; building.investment += price; building.level++;
        return state;
    }

    public static GameState Sell(GameState source, int id)
    {
        if (!CanEdit(source)) return source;
        var state = source.Copy();
        var building = state.buildings.FirstOrDefault(candidate => candidate.id == id);
        if (building != null) { state.gold += building.investment; state.buildings.Remove(building); }
        return state;
    }

    public static GameState StartStage(GameState source, LinkDeployCatalog catalog, int index)
    {
        if (!CanEdit(source) || index < 0 || index >= catalog.stages.Length || index > source.unlockedStage) return source;
        var state = source.Copy();
        state.selectedStage = index; state.mode = SessionMode.Defense;
        state.spawned = 0; state.defeated = 0; state.spawnTimer = 0;
        state.monsters.Clear(); state.projectiles.Clear();
        foreach (var building in state.buildings) { building.targetId = -1; building.cooldown = 0; }
        return state;
    }

    public static GameState ReturnToPlanning(GameState source)
    {
        var state = source.Copy(); state.mode = SessionMode.Planning;
        state.monsters.Clear(); state.projectiles.Clear();
        return state;
    }

    public static GameState Step(GameState source, LinkDeployCatalog catalog, float deltaTime)
    {
        if (deltaTime <= 0 || source.mode == SessionMode.Victory || source.mode == SessionMode.Defeat) return source;
        var state = source.Copy(); state.elapsed += deltaTime;
        foreach (var building in state.buildings) Produce(state, catalog, building, deltaTime);
        Transport(state, catalog);
        if (state.mode == SessionMode.Defense) Fight(state, catalog, deltaTime);
        return state;
    }

    public static bool IsProjectile(MaterialKind material) => material == MaterialKind.RoundStone || material == MaterialKind.Arrow || material == MaterialKind.Bullet;
    public static bool Accepts(BuildingState receiver, BuildingDefinition definition, Cargo cargo)
    {
        if (definition.kind == BuildingKind.Road) return receiver.output.Count < definition.outputCapacity;
        if (receiver.input.Count >= definition.inputCapacity) return false;
        if (definition.kind == BuildingKind.Tower) return cargo.material == definition.ammunition;
        if (definition.kind == BuildingKind.Forge) return IsProjectile(cargo.material) && (receiver.forgeFilter == MaterialKind.None || receiver.forgeFilter == cargo.material);
        if (definition.kind != BuildingKind.Factory || !definition.recipe.Any(ingredient => ingredient.material == cargo.material)) return false;
        // Reserve space for the other recipe ingredients, preventing one input from starving a multi-input factory.
        var reserve = definition.recipe.Where(ingredient => ingredient.material != cargo.material).Sum(ingredient => Math.Max(0, ingredient.quantity - receiver.input.Count(item => item.material == ingredient.material)));
        return receiver.input.Count < definition.inputCapacity - reserve;
    }

    static void Produce(GameState state, LinkDeployCatalog catalog, BuildingState building, float deltaTime)
    {
        var definition = catalog.Building(building.definitionId);
        if (definition.kind != BuildingKind.Mine && definition.kind != BuildingKind.Factory && definition.kind != BuildingKind.Forge) return;
        bool ready = definition.kind == BuildingKind.Mine || (definition.kind == BuildingKind.Forge ? building.input.Count > 0 : definition.recipe.All(ingredient => building.input.Count(cargo => cargo.material == ingredient.material) >= ingredient.quantity));
        if (!ready || building.output.Count >= definition.outputCapacity) { building.progress = 0; return; }
        var boosted = state.buildings.Any(neighbor => neighbor.zone == building.zone && catalog.Building(neighbor.definitionId).kind == BuildingKind.FactoryBuff && Math.Abs(neighbor.column - building.column) + Math.Abs(neighbor.row - building.row) == 1);
        float cycle = Math.Max(.1f, 1 / Math.Max(.01f, definition.productionPerSecond) - (definition.kind == BuildingKind.Factory ? .1f * building.level : 0));
        building.progress += deltaTime * (boosted ? catalog.factoryBuffMultiplier : 1);
        while (building.progress >= cycle && building.output.Count < definition.outputCapacity)
        {
            if (definition.kind == BuildingKind.Factory && !definition.recipe.All(ingredient => building.input.Count(cargo => cargo.material == ingredient.material) >= ingredient.quantity)) break;
            if (definition.kind == BuildingKind.Forge && building.input.Count == 0) break;
            building.progress -= cycle;
            var result = new Cargo { material = definition.output };
            if (definition.kind == BuildingKind.Factory)
                foreach (var ingredient in definition.recipe)
                    for (int used = 0; used < ingredient.quantity; used++) building.input.Remove(building.input.First(cargo => cargo.material == ingredient.material));
            if (definition.kind == BuildingKind.Forge)
            {
                result = building.input[0].Copy(); building.input.RemoveAt(0);
                result.damage += definition.bonusDamage > 0 ? definition.bonusDamage + building.level * 10 : 0;
                result.slow = Math.Min(MaximumSlow, result.slow + (definition.slow > 0 ? definition.slow + building.level * .1f : 0));
            }
            building.output.Add(result);
        }
        building.progress = Math.Min(building.progress, cycle);
    }

    static void Transport(GameState state, LinkDeployCatalog catalog)
    {
        // Snapshot the candidates first: a road can advance cargo only one cell per simulation frame.
        var candidates = state.buildings.Where(building => building.zone < 0 && building.output.Count > 0).Select(building => (building, cargo: building.output[0])).ToList();
        foreach (var candidate in candidates)
        {
            var offset = Offset(candidate.building.direction);
            int column = candidate.building.column + offset.x, row = candidate.building.row + offset.y;
            BuildingState receiver;
            if (IsEntrance(catalog, column, row))
            {
                int zone = Array.IndexOf(catalog.entranceColumns, column);
                receiver = state.buildings.Where(building => building.zone == zone && catalog.Building(building.definitionId).kind == BuildingKind.Tower && Accepts(building, catalog.Building(building.definitionId), candidate.cargo)).OrderBy(building => building.input.Count).ThenBy(building => building.id).FirstOrDefault();
            }
            else receiver = At(state, -1, column, row);
            if (receiver == null || !Accepts(receiver, catalog.Building(receiver.definitionId), candidate.cargo)) continue;
            candidate.building.output.Remove(candidate.cargo);
            candidate.building.lastTransferTime = state.elapsed;
            if (catalog.Building(receiver.definitionId).kind == BuildingKind.Road) receiver.output.Add(candidate.cargo);
            else receiver.input.Add(candidate.cargo);
            state.delivered++;
        }
    }

    public static Vector2 TowerPosition(LinkDeployCatalog catalog, BuildingState building)
    {
        float center = catalog.entranceColumns[building.zone];
        // Edge courtyards remain completely within the map.
        center = Mathf.Clamp(center, 1, catalog.productionWidth - 2);
        return new Vector2(center + building.column - 1, -2.7f + building.row);
    }
    public static float TowerDamage(BuildingState building, BuildingDefinition definition) => definition.damage + definition.damageIncreases.Take(building.level).Sum();

    static void Fight(GameState state, LinkDeployCatalog catalog, float deltaTime)
    {
        var stage = catalog.stages[state.selectedStage];
        state.spawnTimer -= deltaTime;
        while (state.spawnTimer <= 0 && state.spawned < stage.monsterCount)
        {
            state.monsters.Add(new MonsterState { id = state.nextId++, position = catalog.pathStart, health = stage.monsterHealth, maxHealth = stage.monsterHealth });
            state.spawned++; state.spawnTimer += Math.Max(.05f, stage.spawnDelay);
        }
        foreach (var tower in state.buildings.Where(building => catalog.Building(building.definitionId).kind == BuildingKind.Tower))
        {
            var definition = catalog.Building(tower.definitionId);
            var position = TowerPosition(catalog, tower);
            float Distance(MonsterState monster) => Vector2.Distance(position, new Vector2(monster.position, catalog.pathHeight));
            var target = state.monsters.FirstOrDefault(monster => monster.id == tower.targetId && monster.health > 0 && Distance(monster) <= definition.range);
            if (target == null) target = state.monsters.Where(monster => monster.health > 0 && Distance(monster) <= definition.range).OrderBy(Distance).ThenBy(monster => monster.id).FirstOrDefault();
            tower.targetId = target?.id ?? -1;
            tower.cooldown = Math.Max(0, tower.cooldown - deltaTime);
            if (target == null || tower.input.Count == 0 || tower.cooldown > 0) continue;
            var ammunition = tower.input[0]; tower.input.RemoveAt(0);
            bool boosted = state.buildings.Any(building => building.zone == tower.zone && catalog.Building(building.definitionId).kind == BuildingKind.TowerBuff);
            tower.cooldown = 1 / (definition.attacksPerSecond * (boosted ? catalog.towerBuffMultiplier : 1));
            state.projectiles.Add(new ProjectileState { id = state.nextId++, targetId = target.id, position = position, material = ammunition.material, damage = TowerDamage(tower, definition) + ammunition.damage, slow = ammunition.slow });
            state.totalShots++;
        }
        foreach (var projectile in state.projectiles.ToList())
        {
            var target = state.monsters.FirstOrDefault(monster => monster.id == projectile.targetId && monster.health > 0);
            if (target == null) { state.projectiles.Remove(projectile); continue; }
            var destination = new Vector2(target.position, catalog.pathHeight);
            projectile.position = Vector2.MoveTowards(projectile.position, destination, catalog.projectileSpeed * deltaTime);
            if (Vector2.Distance(projectile.position, destination) > .12f) continue;
            target.health -= projectile.damage;
            if (projectile.slow > 0) { target.slow = Math.Max(target.slow, Math.Min(MaximumSlow, projectile.slow)); target.slowRemaining = catalog.slowDuration; }
            state.projectiles.Remove(projectile);
            if (target.health <= 0) { state.monsters.Remove(target); state.defeated++; }
        }
        foreach (var monster in state.monsters)
        {
            monster.slowRemaining = Math.Max(0, monster.slowRemaining - deltaTime);
            if (monster.slowRemaining <= 0) monster.slow = 0;
            monster.position += stage.speed * (1 - monster.slow) * deltaTime;
            if (monster.position >= catalog.pathEnd) { state.mode = SessionMode.Defeat; return; }
        }
        if (state.spawned == stage.monsterCount && state.monsters.Count == 0)
        {
            state.mode = SessionMode.Victory; state.gold += stage.clearGold;
            state.cleared[state.selectedStage] = true;
            state.unlockedStage = Math.Max(state.unlockedStage, Math.Min(catalog.stages.Length - 1, state.selectedStage + 1));
            foreach (var unlock in stage.unlockBuildings) if (!state.unlockedBuildings.Contains(unlock)) state.unlockedBuildings.Add(unlock);
            foreach (var definition in catalog.buildings.Where(definition => definition.unlockAfterStage <= state.selectedStage + 1))
                if (!state.unlockedBuildings.Contains(definition.id)) state.unlockedBuildings.Add(definition.id);
        }
    }

    public static string MaterialName(MaterialKind material) => material switch
    {
        MaterialKind.Stone => "돌", MaterialKind.Iron => "쇠", MaterialKind.RoundStone => "동그란 돌", MaterialKind.Arrow => "화살", MaterialKind.Bullet => "총알", _ => "모든 투사체"
    };

    public static GameState StarterLayout(GameState source, LinkDeployCatalog catalog)
    {
        if (!CanEdit(source) || source.buildings.Count > 0 || source.gold < StarterCost(catalog)) return source;
        var state = source;
        state = Place(state, catalog, "StoneMine", -1, 0, 3, FlowDirection.Down);
        state = Place(state, catalog, "StoneFactory", -1, 0, 2, FlowDirection.Down);
        state = Place(state, catalog, "Road", -1, 0, 1, FlowDirection.Down);
        state = Place(state, catalog, "Slingshot", 0, 0, 0, FlowDirection.Down);
        state = Place(state, catalog, "IronMine", -1, 7, 3, FlowDirection.Down);
        state = Place(state, catalog, "ArrowFactory", -1, 7, 2, FlowDirection.Down);
        state = Place(state, catalog, "Road", -1, 7, 1, FlowDirection.Down);
        state = Place(state, catalog, "Archer", 1, 1, 0, FlowDirection.Down);
        return state.buildings.Count == 8 ? state : source;
    }

    public static int StarterCost(LinkDeployCatalog catalog) => new[] { "StoneMine", "StoneFactory", "Road", "Slingshot", "IronMine", "ArrowFactory", "Road", "Archer" }.Sum(id => catalog.Building(id).price);
}
