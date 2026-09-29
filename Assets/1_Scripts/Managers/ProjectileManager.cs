using System.Collections.Generic;
using UnityEngine;
using System;

public class ProjectileManager : MonoBehaviour
{
    public static ProjectileManager Instance { get; private set; }
    
    private List<ProjectileModel> _activeProjectiles = new List<ProjectileModel>();
    private Dictionary<string, GameObject> _projectileObjects = new Dictionary<string, GameObject>();
    
    public Transform projectilesParent;
    
    private AudioClip _shotClip;
    private AudioSource _audioSource;
    private Sprite _projectileSprite;
    
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        _audioSource = gameObject.GetComponent<AudioSource>();
        _shotClip = Resources.Load<AudioClip>("Sounds/Shot");
        _projectileSprite = CreateCircleSprite();
    }
    
    public void FireProjectile(Vector3 startPosition, string targetMonsterId, float damage, float speed, bool isIce = false, float slowAmount = 0f)
    {
        if (_shotClip != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(_shotClip);
        }
        
        var model = new ProjectileModel
        {
            id = System.Guid.NewGuid().ToString(),
            currentPosition = startPosition,
            targetMonsterId = targetMonsterId,
            damage = damage,
            speed = speed,
            isIce = isIce,
            slowAmount = slowAmount
        };
        
        _activeProjectiles.Add(model);
        
        var go = new GameObject($"Projectile_{model.id}");
        if (projectilesParent != null) go.transform.SetParent(projectilesParent);
        go.transform.position = startPosition;
        
        var renderer = go.AddComponent<SpriteRenderer>();
        if (_projectileSprite == null) _projectileSprite = CreateCircleSprite();
        renderer.sprite = _projectileSprite;
        renderer.sortingOrder = 15;
        renderer.color = isIce ? Color.cyan : Color.white; // Visual cue!
        go.transform.localScale = new Vector3(0.05f, 0.05f, 1f);
        
        _projectileObjects[model.id] = go;
    }
    
    void Update()
    {
        // Must iterate backwards since we might remove
        for (int i = _activeProjectiles.Count - 1; i >= 0; i--)
        {
            var projectileModel = _activeProjectiles[i];
            var targetMonster = MonsterManager.Instance.GetMonster(projectileModel.targetMonsterId);
            
            if (targetMonster == null)
            {
                RemoveProjectile(projectileModel);
                continue;
            }
            
            float distanceToTarget = Vector3.Distance(projectileModel.currentPosition, targetMonster.currentPosition);
            float moveStep = projectileModel.speed * Time.deltaTime;
            
            if (moveStep >= distanceToTarget)
            {
                MonsterManager.Instance.TakeDamage(targetMonster.id, projectileModel.damage);
                
                if (projectileModel.isIce)
                {
                    MonsterManager.Instance.ApplySlow(targetMonster.id, projectileModel.slowAmount);
                }
                
                RemoveProjectile(projectileModel);
                continue;
            }
            else
            {
                projectileModel.currentPosition = Vector3.MoveTowards(projectileModel.currentPosition, targetMonster.currentPosition, moveStep);
            }
            
            if (_projectileObjects.ContainsKey(projectileModel.id))
            {
                _projectileObjects[projectileModel.id].transform.position = projectileModel.currentPosition;
            }
        }
    }
    
    private void RemoveProjectile(ProjectileModel projectileModel)
    {
        _activeProjectiles.Remove(projectileModel);
        if (_projectileObjects.ContainsKey(projectileModel.id))
        {
            Destroy(_projectileObjects[projectileModel.id]);
            _projectileObjects.Remove(projectileModel.id);
        }
    }
    
    private Sprite CreateCircleSprite()
    {
        // Simple 16x16 circle texture
        int resolution = 16;
        Texture2D texture = new Texture2D(resolution, resolution);
        Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
        float radius = resolution / 2f;
        
        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                float distance = Vector2.Distance(center, new Vector2(x, y));
                if (distance <= radius) texture.SetPixel(x, y, Color.white);
                else texture.SetPixel(x, y, Color.clear);
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), 1f);
    }
}
