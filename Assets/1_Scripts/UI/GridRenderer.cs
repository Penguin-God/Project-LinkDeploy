using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GridRenderer : MonoBehaviour
{
    public Transform tilesParent;
    public Transform buildingsParent;
    
    private Dictionary<string, GameObject> _buildingObjects = new Dictionary<string, GameObject>();
    
    void Start()
    {
        GridManager.Instance.OnGridChanged += DrawGrid;
        GridManager.Instance.OnBuildingPlaced += OnBuildingPlaced;
        GridManager.Instance.OnBuildingRemoved += OnBuildingRemoved;
        
        foreach (var building in GridManager.Instance.GetAllBuildings())
        {
            if (!_buildingObjects.ContainsKey(building.id))
            {
                OnBuildingPlaced(building);
            }
        }
        
        DrawGrid();
    }
    
        public void RefreshBuilding(string id)
    {
        if (_buildingObjects.ContainsKey(id))
        {
            Destroy(_buildingObjects[id]);
            _buildingObjects.Remove(id);
            var b = GridManager.Instance.GetAllBuildings().FirstOrDefault(x => x.id == id);
            if (b != null) OnBuildingPlaced(b);
        }
    }
    
    private void OnBuildingRemoved(string id)
    {
        if (_buildingObjects.ContainsKey(id))
        {
            Destroy(_buildingObjects[id]);
            _buildingObjects.Remove(id);
        }
        if (SelectedBuilding != null && SelectedBuilding.id == id) SelectedBuilding = null;
    }
    
    private void DrawGrid()
    {
        if (tilesParent.childCount == 0)
        {
            // Defense Zone (15x8)
            for (int posX = 0; posX < 15; posX++)
            {
                for (int posY = 0; posY < 8; posY++)
                {
                    CreateGridTile(posX, posY, new Color(0.8f, 0.8f, 0.8f));
                }
            }
            
            // Village Zone (9x9 at 50,50)
            for (int posX = 50; posX < 59; posX++)
            {
                for (int posY = 50; posY < 59; posY++)
                {
                    CreateGridTile(posX, posY, new Color(0.8f, 0.9f, 0.8f));
                }
            }
            // Tower Zones (2 width x 3 height underneath) Wait!
            // The user said: "??뚭뎄???ш린 : 2(媛濡? X 3(?몃줈)"
            // So X is 2 width, Y is 3 height!
            // I used X=0,1,2 (3 width) and Y=-2,-3 (2 height)!
            // I need to change it!
            // Entrances are at (0,0), (7,0), (14,0).
            // Width 2 means X=0,1 (for 0), X=7,8 (for 7), X=13,14 (for 14)
            // Height 3 means Y=-1,-2,-3. But wait, Y=-1 is the visual road.
            
                        for (int posY = -3; posY <= -2; posY++)
            {
                for (int posX = 0; posX <= 2; posX++) CreateGridTile(posX, posY, new Color(0.7f, 0.7f, 0.85f));
                for (int posX = 6; posX <= 8; posX++) CreateGridTile(posX, posY, new Color(0.7f, 0.7f, 0.85f));
                for (int posX = 12; posX <= 14; posX++) CreateGridTile(posX, posY, new Color(0.7f, 0.7f, 0.85f));
            }
            
            // Separator Roads (Visual)
            CreateGridTile(0, -1, new Color(0.85f, 0.8f, 0.7f));
            CreateGridTile(7, -1, new Color(0.85f, 0.8f, 0.7f));
            CreateGridTile(14, -1, new Color(0.85f, 0.8f, 0.7f));
            
            // Monster Path Visuals
            for (int posX = -1; posX <= 14; posX++)
            {
                CreateGridTile(posX, -5, new Color(0.6f, 0.5f, 0.4f)); // Dirt path color
            }
            
            // Destination Marker
            var destGo = new GameObject("DestinationMarker");
            destGo.transform.SetParent(tilesParent);
            destGo.transform.position = new Vector3(15, -5, 0);
            var sr = destGo.AddComponent<SpriteRenderer>();
            sr.sprite = CreateBoxSprite();
            sr.color = Color.red; // Danger/Goal
            var textMesh = new GameObject("DestText").AddComponent<TMPro.TextMeshPro>();
            textMesh.transform.SetParent(destGo.transform);
            textMesh.transform.localPosition = new Vector3(0, 0, -1);
            textMesh.text = "Goal";
            textMesh.fontSize = 3;
            textMesh.alignment = TMPro.TextAlignmentOptions.Center;
            textMesh.color = Color.white;
            textMesh.GetComponent<RectTransform>().sizeDelta = new Vector2(1, 1);
        }
        
                    foreach (var building in GridManager.Instance.GetAllBuildings())
            {
                if (_buildingObjects.ContainsKey(building.id))
                {
                    _buildingObjects[building.id].transform.position = new Vector3(building.x, building.y, 0);
                    
                    var textMesh = _buildingObjects[building.id].GetComponentInChildren<TMPro.TextMeshPro>();
                    if (textMesh != null)
                    {
                        if (building.data.buildingType == BuildingType.Tower)
                        {
                            textMesh.text = $"{building.currentInput1}/{building.data.maxAmmo}";
                            textMesh.color = building.isShutdown ? Color.red : Color.black;
                        }
                        else if (building.data.buildingType == BuildingType.Factory || building.data.buildingType == BuildingType.Mine)
                        {
                            textMesh.text = $"{building.currentOutput}/{building.data.maxOutputCapacity}";
                            textMesh.color = building.isShutdown ? Color.red : Color.black;
                        }
                        else
                        {
                            textMesh.text = "";
                        }
                    }
                    
                    // Render Item on Road
                    if (building.data.buildingType == BuildingType.Road || building.data.buildingType == BuildingType.Entrance)
                    {
                        var itemTransform = _buildingObjects[building.id].transform.Find("ItemVisual");
                        if (building.inputQueue.Count > 0)
                        {
                            if (itemTransform == null)
                            {
                                var itemGo = new GameObject("ItemVisual");
                                itemGo.transform.SetParent(_buildingObjects[building.id].transform, false);
                                itemGo.transform.localPosition = new Vector3(0, 0, -0.2f);
                                itemGo.transform.localScale = new Vector3(0.4f, 0.4f, 1f);
                                var srItem = itemGo.AddComponent<SpriteRenderer>();
                                srItem.sprite = CreateBoxSprite();
                                srItem.sortingOrder = 8;
                                itemTransform = itemGo.transform;
                            }
                            itemTransform.gameObject.SetActive(true);
                            var srItemExist = itemTransform.GetComponent<SpriteRenderer>();
                            var type = building.inputQueue.Peek().type;
                            if (type == ResourceType.Wood) srItemExist.color = new Color(0.6f, 0.3f, 0f);
                            else if (type == ResourceType.Iron) srItemExist.color = Color.gray;
                            else if (type == ResourceType.Hammer) srItemExist.color = Color.red;
                            else srItemExist.color = Color.blue;
                        }
                        else
                        {
                            if (itemTransform != null) itemTransform.gameObject.SetActive(false);
                        }
                    }
                }
            }
            
        }
    
    
        public BuildingModel SelectedBuilding { get; set; }
    private GameObject _rangeOverlay = null;
    private GameObject _ghostBuilding = null;
    
    private BuildingModel _draggingBuilding = null;
    
    void Update()
    {
        bool isPlacing = BuildManager.Instance != null && BuildManager.Instance.IsPlacing;
        bool canEdit = BuildManager.Instance != null && BuildManager.Instance.isBuildModeActive;
        
        if (!isPlacing)
        {
            var cam = Camera.main ?? Object.FindObjectOfType<Camera>();
            if (cam == null) return;
            Vector3 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            int gridX = Mathf.RoundToInt(mousePos.x);
            int gridY = Mathf.RoundToInt(mousePos.y);
            
                                    if (Input.GetKeyDown(KeyCode.R))
            {
                var hoverBuilding = GridManager.Instance.GetBuildingAt(gridX, gridY);
                if (hoverBuilding != null)
                {
                    if (hoverBuilding.data.buildingType == BuildingType.Road ||
                        hoverBuilding.data.buildingType == BuildingType.Factory ||
                        hoverBuilding.data.buildingType == BuildingType.Smeltery ||
                        hoverBuilding.data.buildingType == BuildingType.Mine)
                    {
                        hoverBuilding.direction = (Direction)(((int)hoverBuilding.direction + 1) % 4);
                        RefreshBuilding(hoverBuilding.id);
                    }
                }
            }
            
            if (Input.GetMouseButtonDown(0))
            {
                if (UnityEngine.EventSystems.EventSystem.current != null && 
                    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) 
                {
                    // Do not deselect. Let the UI handle the click.
                    return;
                }
                
                var clickedBuilding = GridManager.Instance.GetBuildingAt(gridX, gridY);
                
                SelectedBuilding = clickedBuilding;
                
                if (canEdit)
                {
                    if (clickedBuilding != null && clickedBuilding.data.buildingType != BuildingType.Mine && clickedBuilding.data.buildingType != BuildingType.Entrance) 
                    {
                        _draggingBuilding = clickedBuilding;
                    }
                    else
                    {
                        _draggingBuilding = null;
                    }
                }
                else
                {
                    _draggingBuilding = null; // Disable dragging when not in build mode
                }
            }
            else if (Input.GetMouseButtonUp(0) && _draggingBuilding != null)
            {
                GridManager.Instance.MoveBuilding(_draggingBuilding.id, gridX, gridY);
                _draggingBuilding = null;
            }
        }
        else
        {
            SelectedBuilding = null;
            _draggingBuilding = null;
        }
        
        // Reset grid tiles to base color
        for (int i = 0; i < tilesParent.childCount; i++)
        {
            var tile = tilesParent.GetChild(i);
            var sr = tile.GetComponent<SpriteRenderer>();
            var baseColor = tile.GetComponent<TileBaseColor>();
            if (baseColor != null) sr.color = baseColor.baseColor;
            else sr.color = new Color(0.8f, 0.8f, 0.8f);
        }
        
                // Handle Range Overlay and Ghost
        if (_rangeOverlay == null)
        {
            _rangeOverlay = new GameObject("RangeOverlay");
            var sr = _rangeOverlay.AddComponent<SpriteRenderer>();
            sr.sprite = CreateBoxSprite();
            sr.color = new Color(0f, 1f, 0f, 0.3f);
            sr.sortingOrder = 5;
        }
        
                if (_ghostBuilding == null)
        {
            _ghostBuilding = new GameObject("GhostBuilding");
            var sr = _ghostBuilding.AddComponent<SpriteRenderer>();
            sr.color = new Color(1f, 1f, 1f, 0.5f);
            sr.sortingOrder = 6;
            
            var ghostArrow = new GameObject("GhostArrow");
            ghostArrow.transform.SetParent(_ghostBuilding.transform, false);
            ghostArrow.transform.localPosition = new Vector3(0, 0, -0.1f);
            var arrSr = ghostArrow.AddComponent<SpriteRenderer>();
            arrSr.sprite = GetArrowSprite();
            arrSr.color = new Color(1f, 1f, 1f, 0.8f);
            arrSr.sortingOrder = 7;
        }
        
        var currentCam = Camera.main ?? Object.FindObjectOfType<Camera>();
        
                if (isPlacing && currentCam != null)
        {
            Vector3 mousePos = currentCam.ScreenToWorldPoint(Input.mousePosition);
            int posX = Mathf.RoundToInt(mousePos.x);
            int posY = Mathf.RoundToInt(mousePos.y);
            
            _rangeOverlay.SetActive(true);
            _rangeOverlay.transform.position = new Vector3(posX, posY, -0.1f);
            
            _ghostBuilding.SetActive(true);
            _ghostBuilding.transform.position = new Vector3(posX, posY, -0.2f);
            
            float size = 1f;
            var data = BuildManager.Instance.BuildingToPlace;
            if (data != null)
            {
                if (data.buildingType == BuildingType.Tower) size = data.attackRange;
                else if (data.buildingType == BuildingType.Factory) size = 1f + 2f * data.connectionRange;
                else if (data.buildingType == BuildingType.Road || data.buildingType == BuildingType.FactorySpeedBuff || data.buildingType == BuildingType.TowerAttackBuff) size = 1f + 2f * data.buffRange;
                
                                var ghostSr = _ghostBuilding.GetComponent<SpriteRenderer>();
                ghostSr.sprite = data.sprite != null ? data.sprite : CreateBoxSprite();
                
                var ghostArrow = _ghostBuilding.transform.Find("GhostArrow");
                if (ghostArrow != null)
                {
                    ghostArrow.gameObject.SetActive(data.buildingType == BuildingType.Factory || data.buildingType == BuildingType.Smeltery || data.buildingType == BuildingType.Mine);
                }
                
                                float rotZ = 0f;
                if (data.buildingType == BuildingType.Road || data.buildingType == BuildingType.Factory || data.buildingType == BuildingType.Smeltery || data.buildingType == BuildingType.Mine)
                {
                    switch (BuildManager.Instance.currentDirection)
                    {
                        case Direction.Right: rotZ = 0f; break;
                        case Direction.Up: rotZ = 90f; break;
                        case Direction.Left: rotZ = 180f; break;
                        case Direction.Down: rotZ = 270f; break;
                    }
                }
                _ghostBuilding.transform.rotation = Quaternion.Euler(0, 0, rotZ);
                
                float maxDim = Mathf.Max(ghostSr.sprite.bounds.size.x, ghostSr.sprite.bounds.size.y);
                if (maxDim > 0)
                {
                    float targetScale = 1f / maxDim;
                    _ghostBuilding.transform.localScale = new Vector3(targetScale * 0.95f, targetScale * 0.95f, 1f);
                }
            }
            
            _rangeOverlay.transform.localScale = new Vector3(size, size, 1f);
            
            var sr = _rangeOverlay.GetComponent<SpriteRenderer>();
            if (BuildManager.Instance.IsValidPlacement(posX, posY)) 
            {
                sr.color = new Color(0f, 1f, 0f, 0.3f);
                _ghostBuilding.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.6f);
            }
            else 
            {
                sr.color = new Color(1f, 0f, 0f, 0.3f);
                _ghostBuilding.GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 0f, 0.6f);
            }
        }
        else if (_draggingBuilding != null)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            int gridX = Mathf.RoundToInt(mousePos.x);
            int gridY = Mathf.RoundToInt(mousePos.y);
            
            _rangeOverlay.SetActive(true);
            _rangeOverlay.transform.position = new Vector3(gridX, gridY, -0.1f);
            
            float size = 1f;
            var data = _draggingBuilding.data;
            if (data.buildingType == BuildingType.Tower) size = data.attackRange;
            else if (data.buildingType == BuildingType.Factory) size = 1f + 2f * data.connectionRange;
            else if (data.buildingType == BuildingType.Road || data.buildingType == BuildingType.FactorySpeedBuff || data.buildingType == BuildingType.TowerAttackBuff) size = 1f + 2f * data.buffRange;
            
            _rangeOverlay.transform.localScale = new Vector3(size, size, 1f);
            
            bool valid = GridManager.Instance.GetBuildingAt(gridX, gridY) == null && GridManager.Instance.IsValidCoordinate(gridX, gridY, data);
            _rangeOverlay.GetComponent<SpriteRenderer>().color = valid ? new Color(0f, 1f, 0f, 0.3f) : new Color(1f, 0f, 0f, 0.3f);
        }
        else if (SelectedBuilding != null)
        {
            _rangeOverlay.SetActive(true);
            _rangeOverlay.transform.position = new Vector3(SelectedBuilding.x, SelectedBuilding.y, -0.1f);
            
            float size = 1f;
            var data = SelectedBuilding.data;
            if (data.buildingType == BuildingType.Tower) size = data.attackRange;
            else if (data.buildingType == BuildingType.Factory) size = 1f + 2f * data.connectionRange;
            else if (data.buildingType == BuildingType.Road || data.buildingType == BuildingType.FactorySpeedBuff || data.buildingType == BuildingType.TowerAttackBuff) size = 1f + 2f * data.buffRange;
            
            _rangeOverlay.transform.localScale = new Vector3(size, size, 1f);
            _rangeOverlay.GetComponent<SpriteRenderer>().color = new Color(0f, 1f, 0f, 0.3f);
        }
                else
        {
            _rangeOverlay.SetActive(false);
            _ghostBuilding.SetActive(false);
        }
        
        foreach (var building in GridManager.Instance.GetAllBuildings())
        {
            if (_buildingObjects.ContainsKey(building.id))
            {
                if (_draggingBuilding == building) 
                {
                    Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    int gridX = Mathf.RoundToInt(mousePos.x);
                    int gridY = Mathf.RoundToInt(mousePos.y);
                    _buildingObjects[building.id].transform.position = new Vector3(gridX, gridY, 0);
                }
                else
                {
                    _buildingObjects[building.id].transform.position = new Vector3(building.x, building.y, 0);
                }
                
                var textMesh = _buildingObjects[building.id].GetComponentInChildren<TMPro.TextMeshPro>();
                if (textMesh != null)
                {
                    if (building.data.buildingType == BuildingType.Tower)
                    {
                        textMesh.text = $"{building.currentInput1}/{building.data.maxAmmo}";
                        textMesh.color = building.isShutdown ? Color.red : Color.black;
                    }
                    else if (building.data.buildingType == BuildingType.Factory || building.data.buildingType == BuildingType.Mine)
                    {
                        textMesh.text = $"{building.currentOutput}/{building.data.maxOutputCapacity}";
                        textMesh.color = building.isShutdown ? Color.red : Color.black;
                        

                    }
                    else
                    {
                        textMesh.text = "";
                    }
                }
            }
        }
    }
    
        private Sprite _arrowSprite;
    private Sprite GetArrowSprite()
    {
        if (_arrowSprite != null) return _arrowSprite;
        Texture2D tex = new Texture2D(32, 32);
        for(int i=0; i<32; i++) for(int j=0; j<32; j++) tex.SetPixel(i, j, Color.clear);
        
        // Draw a simple red chevron pointing Right
        for (int i=10; i<=22; i++) {
            tex.SetPixel(i, 16 + (i-10), Color.red);
            tex.SetPixel(i, 16 - (i-10), Color.red);
            tex.SetPixel(i-1, 16 + (i-10), Color.red);
            tex.SetPixel(i-1, 16 - (i-10), Color.red);
        }
        tex.Apply();
        _arrowSprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
        return _arrowSprite;
    }

    private void OnBuildingPlaced(BuildingModel model)
    {
        var buildingGo = new GameObject($"Building_{model.data.buildingName}");
        buildingGo.transform.position = new Vector3(model.x, model.y, 0);
        buildingGo.transform.SetParent(buildingsParent);
        
                // Apply Direction Rotation
        float rotZ = 0f;
        if (model.data.buildingType == BuildingType.Road || model.data.buildingType == BuildingType.Factory || model.data.buildingType == BuildingType.Smeltery || model.data.buildingType == BuildingType.Mine)
        {
            switch (model.direction)
            {
                case Direction.Right: rotZ = 0f; break;
                case Direction.Up: rotZ = 90f; break;
                case Direction.Left: rotZ = 180f; break;
                case Direction.Down: rotZ = 270f; break;
            }
        }
        buildingGo.transform.rotation = Quaternion.Euler(0, 0, rotZ);
        
        float targetScale = 1f;
        var sr = buildingGo.AddComponent<SpriteRenderer>();
        if (model.data.sprite != null) 
        {
            sr.sprite = model.data.sprite;
            // Auto-scale to fit within 1x1 grid cell (1 unit)
            float maxDim = Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
            if (maxDim > 0)
            {
                targetScale = 1f / maxDim;
                // Leave a little margin (e.g. 0.9f) or fill it completely
                buildingGo.transform.localScale = new Vector3(targetScale * 0.95f, targetScale * 0.95f, 1f);
            }
        }
        else 
        {
            sr.sprite = CreateBoxSprite();
        }
        
        if (model.data.buildingType == BuildingType.Tower || model.data.buildingType == BuildingType.Factory || model.data.buildingType == BuildingType.Mine)
        {
            var textGo = new GameObject("AmmoText");
            textGo.transform.SetParent(buildingGo.transform);
            
            float invScale = 1f / (targetScale * 0.95f);
            textGo.transform.localScale = new Vector3(invScale, invScale, 1f);
            textGo.transform.localPosition = new Vector3(0, 0.45f * invScale, -0.1f);
            
            var textMesh = textGo.AddComponent<TMPro.TextMeshPro>();
            textMesh.alignment = TMPro.TextAlignmentOptions.Center;
            textMesh.fontSize = 3.0f; // Increased by 20% (was 2.5)
            textMesh.text = "";
            textMesh.color = Color.black;
            textMesh.rectTransform.sizeDelta = new Vector2(1, 1);
        }

        if (model.data.buildingType == BuildingType.Road)
        {
            buildingGo.transform.localScale = new Vector3(0.8f, 0.8f, 1);
            sr.color = Color.gray;
        }
            
                        // Arrow Indicator
        if (model.data.buildingType == BuildingType.Factory || 
            model.data.buildingType == BuildingType.Smeltery || 
            model.data.buildingType == BuildingType.Mine)
        {
            var arrowGo = new GameObject("ArrowIndicator");
            arrowGo.transform.SetParent(buildingGo.transform);
            arrowGo.transform.localPosition = new Vector3(0, 0, -0.1f);
            var arrowSr = arrowGo.AddComponent<SpriteRenderer>();
            arrowSr.sprite = GetArrowSprite();
            arrowSr.sortingOrder = 10;
            // The parent buildingGo is already rotated. So the arrow will rotate with it!
            // Wait, if the parent buildingGo is rotated, its local rotation rotates the child.
            // If the arrow points right, and the building rotates 90 (Up), the arrow points Up.
            // Perfect!
        }
        
        _buildingObjects[model.id] = buildingGo;
    }
    
    private void CreateGridTile(int posX, int posY, Color defaultColor)
    {
        var tileGo = new GameObject($"Tile_{posX}_{posY}");
        tileGo.transform.position = new Vector3(posX, posY, 0.1f); 
        tileGo.transform.SetParent(tilesParent);
        var sr = tileGo.AddComponent<SpriteRenderer>();
        sr.sprite = CreateBoxSprite();
        sr.color = defaultColor;
        tileGo.transform.localScale = new Vector3(0.95f, 0.95f, 1);
        
        // Store base color so we can reset correctly
        var baseColorComponent = tileGo.AddComponent<TileBaseColor>();
        baseColorComponent.baseColor = defaultColor;
    }
    
    private Sprite CreateBoxSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}

public class TileBaseColor : MonoBehaviour
{
    public Color baseColor;
}









