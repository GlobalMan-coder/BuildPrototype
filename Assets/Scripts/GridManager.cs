using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GridManager : MonoBehaviour
{
    private BuildingSO curBuilding;
    [SerializeField] private Transform Plane;
    [SerializeField] private BuildingSO Road;
    public bool autoMode = true;
    public static GridManager Instance { get; private set; }
    private GridType<GridObject> grid;
    private Direction direction = Direction.Down;
    //private Transform visual;
    private List<Transform> visual = new List<Transform>();
    private bool isDragging = false;
    private Vector3 targetPosition;
    private int _prevx, _prevz, _oldx, _oldz;
    bool builtTown = false;
    public BuildingSO CurrentBuilding
    {
        get { return curBuilding; }
        set
        {
            if (value != null)
            {
                if (builtTown && value.type == BuildingType.TownCenter)
                {
                    MessageManager.Instance.AddMessage("You have already towncenter.", MessageManager.Type.Alert);
                    return;
                }

                if (!builtTown && value.type != BuildingType.TownCenter)
                {
                    MessageManager.Instance.AddMessage("You have to towncenter first.", MessageManager.Type.Alert);
                    return;
                }
            }
            curBuilding = value;
            if (visual.Count > 0)
            {
                GameObject go;
                for (int i = visual.Count - 1; i >= 0; i--)
                {
                    go = visual[i].gameObject;
                    visual.RemoveAt(i);
                    Destroy(go);
                }
            }
        }
    }
    private void Awake()
    {
        if (Instance == null) Instance = this;
        int gridWidth = 10;
        int gridHeight = 10;
        float cellSize = 10f;
        Plane.localScale = new Vector3(gridWidth + 2, 10, gridHeight + 2);
        Plane.localPosition = new Vector3(gridWidth * cellSize / 2f, 0, gridHeight * cellSize / 2f);
        grid = new GridType<GridObject>(gridWidth, gridHeight, cellSize, Vector3.zero, (GridType<GridObject> g, int x, int z) => new GridObject(g, x, z));
    }
    public class GridObject
    {
        private GridType<GridObject> grid;
        private int x;
        private int z;
        public PlacedObject Object { get; private set; }
        public GridObject(object grid, int x, int z)
        {
            this.grid = grid as GridType<GridObject>;
            this.x = x;
            this.z = z;
        }
        public void SetObject(PlacedObject placedObject)
        {
            this.Object = placedObject;
            grid.TriggerGridObjectChanged(x, z);
        }
        public bool CanBuild() { return Object == null; }
        public void ClearObject() { Object = null; }
        public override string ToString() { return $"{x}, {z}"; }
    }
    public void Rotate()
    {
        if (visual.Count != 1) return;
        Vector2Int rotationOffset = curBuilding.GetRotationOffset(direction);
        direction = BuildingSO.GetNextDirection(direction);
        targetPosition = GetMouseWorldSnappedPosition(targetPosition + new Vector3(1, 0, 1) - new Vector3(rotationOffset.x, 0, rotationOffset.y) * grid.CellSize);
    }
    public void Place()
    {
        if (visual.Count == 0) return;
        bool canBuild = true;
        int x, z;
        List<Vector2Int> gridPositionList;
        foreach(var t in visual)
        {
            Vector2Int rotationOffset = curBuilding.GetRotationOffset(direction);
            grid.GetXZ(t.position + Vector3.one - new Vector3(rotationOffset.x, 0, rotationOffset.y) * grid.CellSize, out x, out z);
            gridPositionList = curBuilding.GetGridPositionList(new Vector2Int(x, z), direction);
            foreach(Vector2Int pos in gridPositionList)
            {
                var o = grid.GetGridObject(pos.x, pos.y);
                if(o == default || o.Object != null)
                {
                    canBuild = false;
                    break;
                }
            }
        }
        if (canBuild)
        {
            PlacedObject placedObject = null;
            foreach (var t in visual)
            {
                Vector2Int rotationOffset = curBuilding.GetRotationOffset(direction);
                grid.GetXZ(t.position + Vector3.one - new Vector3(rotationOffset.x, 0, rotationOffset.y) * grid.CellSize, out x, out z);
                gridPositionList = curBuilding.GetGridPositionList(new Vector2Int(x, z), direction);
                placedObject = PlacedObject.Create(
                    new Vector3(t.position.x, 0, t.position.z)
                    , new Vector2Int(x, z)
                    , direction
                    , curBuilding
                    );
                foreach (Vector2Int pos in gridPositionList)
                {
                    grid.GetGridObject(pos.x, pos.y).SetObject(placedObject);
                }
            }
            if (CurrentBuilding.type == BuildingType.TownCenter)
            {
                builtTown = true;
            }
            else
            {
                if (autoMode && visual.Count == 1)
                {
                    placedObject.IsLinked = true;
                    FindNearRoad(placedObject);
                }
                CheckRoad();
            }
            CurrentBuilding = null;
            visual.Clear();
            direction = Direction.Down;
        }
    }
    private void FindNearRoad(PlacedObject po)
    {
        int x = po.origin.x;
        int z = po.origin.y;
        int distance = Mathf.Max(grid.Width, grid.Height);
        int width = (po.Dir == Direction.Down || po.Dir == Direction.Up) ? po.SO.width : po.SO.height;
        int height = (po.Dir == Direction.Down || po.Dir == Direction.Up) ? po.SO.height: po.SO.width;
        int targetX = -1;
        int targetY = -1;
        GridObject go;
        bool isFound = false;
        int _distance = 0;
        for(int i = 0; i < grid.Width; i++)
            for(int j = 0; j < grid.Height; j++)
            {
                go = grid.GetGridObject(i, j);
                if(go != default && go.Object != null && (go.Object.SO.type == BuildingType.TownCenter || go.Object.SO.type == BuildingType.Road))
                {
                    _distance = Mathf.Max(x, x + width - 1, i) - Mathf.Min(x, x + width - 1, i)
                        + Mathf.Max(z, z + height - 1, j) - Mathf.Min(z, z + height - 1, j);
                    if(_distance < distance)
                    {
                        distance = _distance;
                        targetX = i;
                        targetY = j;
                        isFound = true;
                    }
                }
            }
        if(isFound && distance > 1)
        {
            BuildRoad(x, z, targetX, targetY, width, height);
        }
    }
    private void BuildRoad(int originX, int originY, int targetX, int targetY, int width, int height)
    {
        if (width > 1 && targetX > originX)
        {
            originX = (targetX >= originX + width) ? originX + width - 1 : targetX;
        }
        if (height > 1 && targetY > originY)
        {
            originY = (targetY >= originY + height) ? originY + height - 1 : targetY;
        }
        int deltax = targetX - originX;
        int step = (deltax > 0)? 1: -1;
        GridObject go;
        for(int i = 0; i < Mathf.Abs(deltax); i++)
        {
            go = grid.GetGridObject(originX + i * step, originY);
            if(go != default && go.Object == null)
            {
                var placedObject = PlacedObject.Create(
                grid.GetWorldPosition(originX + i * step, originY)
                , new Vector2Int(originX + i * step, originY)
                , Direction.Down
                , Road
                );
                placedObject.IsLinked = true;
                grid.GetGridObject(originX + i * step, originY).SetObject(placedObject);
            }
        }
        int deltay = targetY - originY;
        step = (deltay > 0) ? 1 : -1;
        for(int j = 0; j < Mathf.Abs(deltay); j++)
        {
            go = grid.GetGridObject(targetX, originY + j * step);
            if (go != default && go.Object == null)
            {
                var placedObject = PlacedObject.Create(
                grid.GetWorldPosition(targetX, originY + j * step)
                , new Vector2Int(targetX, originY + j * step)
                , Direction.Down
                , Road
                );
                placedObject.IsLinked = true;
                grid.GetGridObject(targetX, originY + j * step).SetObject(placedObject);
            }
        }
    }
    public void Cancel()
    {
        CurrentBuilding = null;
    }
    public Vector3 GetMouseWorldSnappedPosition(Vector3? pos = null)
    {
        Vector3 mousePosition = pos ?? Utils.GetMouseWorldPosition();
        grid.GetXZ(mousePosition, out int x, out int z);

        if (curBuilding != null)
        {
            Vector2Int rotationOffset = curBuilding.GetRotationOffset(direction);
            Vector3 placedObjectWorldPosition = grid.GetWorldPosition(x, z) + new Vector3(rotationOffset.x, 0, rotationOffset.y) * grid.CellSize;
            return placedObjectWorldPosition;
        }
        else
        {
            return mousePosition;
        }
    }
    public Quaternion GetPlacedObjectRotation()
    {
        if (curBuilding != null)
        {
            return Quaternion.Euler(0, curBuilding.GetRotationAngle(direction), 0);
        }
        else
        {
            return Quaternion.identity;
        }
    }
    private void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            if (CurrentBuilding != null)
            {
                RefreshVisual();
            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
        if (Input.GetMouseButton(1))
        {
            GridObject go = grid.GetGridObject(Utils.GetMouseWorldPosition());
            PlacedObject obj = go.Object;
            if (obj != null)
            {
                List<Vector2Int> gridPosList = obj.GetGridPostionList();
                obj.Destroy();
                foreach (Vector2Int pos in gridPosList)
                {
                    grid.GetGridObject(pos.x, pos.y).ClearObject();
                }
            }
        }
    }
    private void LateUpdate()
    {
        if (isDragging)
        {
            if (visual.Count == 0)  // Camera move
            {
                float x = Input.GetAxis("Mouse X");
                float y = Input.GetAxis("Mouse Y");
                Camera.main.transform.position += new Vector3(-x - y, 0, x - y);
            }
            // Visual move
            else
            {
                targetPosition = GetMouseWorldSnappedPosition();
                Vector2Int rotationOffset = curBuilding.GetRotationOffset(direction);
                grid.GetXZ(targetPosition + Vector3.one - new Vector3(rotationOffset.x, 0, rotationOffset.y) * grid.CellSize, out int x, out int z);
                if (x != _prevx || z != _prevz)
                {
                    // linked road visual generate
                    if (curBuilding.type == BuildingType.Road)
                    {
                        VisualClear();
                        if (Mathf.Abs(x - _oldx) > Mathf.Abs(z - _oldz))
                        {
                            int delta = (x > _oldx) ? 1 : -1;
                            for (int i = 1; i < Mathf.Abs(_oldx - x); i++)
                            {
                                visual.Add(Instantiate(curBuilding.visual, grid.GetWorldPosition(_oldx + i * delta, _oldz), Quaternion.identity));
                                var o = grid.GetGridObject(visual[i].position);
                                visual[i].Find("Bottom").GetComponent<MeshRenderer>().material.color = (o == default || !o.CanBuild()) ? Color.red : Color.green;
                            }
                        }
                        else
                        {
                            int delta = (z > _oldz) ? 1 : -1;
                            for (int i = 1; i < Mathf.Abs(_oldz - z); i++)
                            {
                                visual.Add(Instantiate(curBuilding.visual, grid.GetWorldPosition(_oldx, _oldz + i * delta), Quaternion.identity));
                                var o = grid.GetGridObject(visual[i].position);
                                visual[i].Find("Bottom").GetComponent<MeshRenderer>().material.color = (o == default || !o.CanBuild()) ? Color.red : Color.green;
                            }
                        }
                    }
                    // building check if can build
                    else
                    {
                        _prevx = x;
                        _prevz = z;
                        List<Vector2Int> gridPositionList = curBuilding.GetGridPositionList(new Vector2Int(x, z), direction);
                        bool canBuild = true;
                        foreach (Vector2Int pos in gridPositionList)
                        {
                            var o = grid.GetGridObject(pos.x, pos.y);
                            if (o == default || !o.CanBuild())
                            {
                                canBuild = false;
                                break;
                            }
                        }
                        visual[0].Find("Bottom").GetComponent<MeshRenderer>().material.color = (canBuild) ? Color.green : Color.red;
                    }
                }
            }
        }
        if (visual.Count > 0 && curBuilding.type != BuildingType.Road)
        {
            visual[0].position = Vector3.Lerp(visual[0].position, targetPosition, Time.deltaTime * 15f);
            visual[0].rotation = Quaternion.Lerp(visual[0].rotation, GetPlacedObjectRotation(), Time.deltaTime * 15f);
        }
    }
    private void RefreshVisual()
    {
        if (CurrentBuilding != null)
        {
            if (visual.Count > 0)
            {
                GameObject go;
                for (int i = visual.Count - 1; i >= 0; i--)
                {
                    go = visual[i].gameObject;
                    visual.RemoveAt(i);
                    Destroy(go);
                }
            }
            visual.Add(Instantiate(curBuilding.visual, GetMouseWorldSnappedPosition(), Quaternion.identity));
            visual[0].parent = transform;
            if(curBuilding.type == BuildingType.Road)
            {
                var o = grid.GetGridObject(visual[0].position);
                visual[0].Find("Bottom").GetComponent<MeshRenderer>().material.color = (o == default || !o.CanBuild()) ? Color.red : Color.green;
                grid.GetXZ(GetMouseWorldSnappedPosition() + Vector3.one, out _oldx, out _oldz);
            }
        }
    }
    private void CheckRoad()
    {
        bool changed = true;
        GridObject go;

        while (changed)
        {
            changed = false;
            for (int i = 0; i < grid.Width; i++)
                for (int j = 0; j < grid.Height; j++)
                {
                    go = grid.GetGridObject(i, j);
                    if (go == default || go.Object == null) continue;
                    if (!go.Object.IsLinked && CheckPointLink(i, j))
                    {
                        go.Object.IsLinked = true;
                        changed = true;
                    }
                }
        }
    }
    public bool CheckPointLink(int x, int z)
    {
        var go = grid.GetGridObject(x - 1, z);
        if (CheckIsRoad(go)) return true;
        go = grid.GetGridObject(x + 1, z);
        if (CheckIsRoad(go)) return true;
        go = grid.GetGridObject(x, z - 1);
        if (CheckIsRoad(go)) return true;
        go = grid.GetGridObject(x, z + 1);
        if (CheckIsRoad(go)) return true;
        return false;
    }
    private bool CheckIsRoad(GridObject go)
    {
        return go != default && go.Object != null && (go.Object.SO.type == BuildingType.Road && go.Object.IsLinked || go.Object.SO.type == BuildingType.TownCenter);
    }
    private void VisualClear() {
        if (visual.Count > 0)
        {
            GameObject go;
            for (int i = visual.Count - 1; i > 0; i--)
            {
                go = visual[i].gameObject;
                visual.RemoveAt(i);
                Destroy(go);
            }
        }
    }
}
