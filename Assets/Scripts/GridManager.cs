using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GridManager : MonoBehaviour
{
    private BuildingSO curBuilding;
    [SerializeField] private Transform Plane;
    [SerializeField] private BuildingSO Road;
    public static GridManager Instance { get; private set; }
    private GridType<GridObject> grid;
    private Direction direction = Direction.Down;
    private Transform visual;
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
            if (visual != null)
            {
                Destroy(visual.gameObject);
                visual = null;
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
        if (!CurrentBuilding) return;
        Vector2Int rotationOffset = curBuilding.GetRotationOffset(direction);
        direction = BuildingSO.GetNextDirection(direction);
        targetPosition = GetMouseWorldSnappedPosition(visual.position + new Vector3(1, 0, 1) - new Vector3(rotationOffset.x, 0, rotationOffset.y) * grid.CellSize);
    }
    public void Place()
    {
        if (visual == null) return;
        bool canBuild = true;
        grid.GetXZ(visual.transform.position + Vector3.one, out int x, out int z);
        List<Vector2Int> gridPositionList = curBuilding.GetGridPositionList(new Vector2Int(x, z) - curBuilding.GetRotationOffset(direction), direction);
        foreach (Vector2Int pos in gridPositionList)
        {
            var o = grid.GetGridObject(pos.x, pos.y);
            if (o == default || !o.CanBuild())
            {
                canBuild = false;
                break;
            }
        }
        if (canBuild)
        {
            var placedObject = PlacedObject.Create(
                new Vector3(visual.position.x, 0, visual.position.z)
                , new Vector2Int(x, z)
                , direction
                , curBuilding
                );
            foreach (Vector2Int pos in gridPositionList)
            {
                grid.GetGridObject(pos.x, pos.y).SetObject(placedObject);
            }
            if (CurrentBuilding.type == BuildingType.TownCenter)
            {
                builtTown = true;
            }
            else
            {
                FindNearRoad(placedObject);
            }
            Destroy(visual.gameObject);
            CurrentBuilding = null;
            visual = null;
            direction = Direction.Down;
        }
        else
        {
            MessageManager.Instance.AddMessage("Cannot build there.", MessageManager.Type.Alert);
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
            Vector2Int offset = po.SO.GetRotationOffset(po.Dir);
            BuildingRoad(x - offset.x, z - offset.y, targetX, targetY, width, height);
        }
    }
    private void BuildingRoad(int originX, int originY, int targetX, int targetY, int width, int height)
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
            if (visual == null)  // Camera move
            {
                float x = Input.GetAxis("Mouse X");
                float y = Input.GetAxis("Mouse Y");
                Camera.main.transform.position += new Vector3(-x - y, 0, x - y);
            }
            // Visual move
            else
            {
                targetPosition = GetMouseWorldSnappedPosition();
                grid.GetXZ(targetPosition + Vector3.one, out int x, out int z);
                if (x != _prevx || z != _prevz)
                {
                    _prevx = x;
                    _prevz = z;
                    List<Vector2Int> gridPositionList = curBuilding.GetGridPositionList(new Vector2Int(x, z) -curBuilding.GetRotationOffset(direction), direction);
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
                    visual.Find("Bottom").GetComponent<MeshRenderer>().material.color = (canBuild) ? Color.green : Color.red;
                }
            }
        }
        if (visual != null && curBuilding.type != BuildingType.Road)
        {
            visual.position = Vector3.Lerp(visual.position, targetPosition, Time.deltaTime * 15f);
            visual.rotation = Quaternion.Lerp(visual.rotation, GetPlacedObjectRotation(), Time.deltaTime * 15f);
        }
    }
    private void RefreshVisual()
    {
        if (CurrentBuilding != null)
        {
            if (visual != null)
            {
                Destroy(visual.gameObject);
                visual = null;
            }
            visual = Instantiate(curBuilding.visual, GetMouseWorldSnappedPosition(), Quaternion.identity);
            visual.parent = transform;
        }
    }
}
