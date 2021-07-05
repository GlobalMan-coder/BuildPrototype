using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GridManager : MonoBehaviour
{
    private BuildingSO curBuilding;
    [SerializeField] private Transform Plane;
    public static GridManager Instance { get; private set; }
    private GridType<GridObject> grid;
    private Direction direction = Direction.Down;
    private List<Transform> visual = new List<Transform>();
    private bool isDragging = false;
    private Vector3 targetPosition;
    private bool hasTownCenter = false;

    private int _prevx, _prevz, _oldx, _oldz;
    public BuildingSO CurrentBuilding
    {
        get { return curBuilding; }
        set 
        {
            if (value != null)
            {
                if (hasTownCenter && value.type == BuildingType.TownCenter)
                {
                    MessageManager.Instance.AddMessage("You have already towncenter.", MessageManager.Type.Alert);
                    return;
                }

                if (!hasTownCenter && value.type != BuildingType.TownCenter)
                {
                    MessageManager.Instance.AddMessage("You have to towncenter first.", MessageManager.Type.Notify);
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
        if (!CurrentBuilding) return;
        Vector2Int rotationOffset = curBuilding.GetRotationOffset(direction);
        direction = BuildingSO.GetNextDirection(direction);
        targetPosition = GetMouseWorldSnappedPosition(visual[0].position + new Vector3(1, 0, 1) - new Vector3(rotationOffset.x, 0, rotationOffset.y) * grid.CellSize);
    }
    public void Place()
    {
        if (visual.Count == 0) return;
        bool canBuild = true;
        foreach (var t in visual)
        {
            grid.GetXZ(t.transform.position + Vector3.one, out int x, out int z);
            List<Vector2Int> gridPositionList = curBuilding.GetGridPositionList(new Vector2Int(x, z), direction);
            foreach (Vector2Int pos in gridPositionList)
            {
                var o = grid.GetGridObject(pos.x, pos.y);
                if (o == default || !o.CanBuild())
                {
                    canBuild = false;
                    break;
                }
            }

        }
        if (canBuild)
        {
            foreach (var t in visual)
            {
                grid.GetXZ(t.transform.position + Vector3.one, out int x, out int z);
                List<Vector2Int> gridPositionList = curBuilding.GetGridPositionList(new Vector2Int(x, z), direction);
                var placedObject = PlacedObject.Create(
                    new Vector3(t.transform.position.x, 0, t.transform.position.z)
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
                    hasTownCenter = true;
                }
                Destroy(t.gameObject);
            }
            if (curBuilding.type == BuildingType.Road) CheckRoad();
            CurrentBuilding = null;
            visual.Clear();
            direction = Direction.Down;
        }
        else
        {
            MessageManager.Instance.AddMessage("Cannot build there.", MessageManager.Type.Alert);
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
                grid.GetXZ(targetPosition + Vector3.one, out int x, out int z);
                if (x != _prevx || z != _prevz)
                {
                    if (curBuilding.type == BuildingType.Road)
                    {
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
                        if (Mathf.Abs(x - _oldx) > Mathf.Abs(z - _oldz))
                        {
                            int delta = (x > _oldx) ? 1 : -1;
                            for(int i = 1; i < Mathf.Abs(_oldx - x); i ++)
                            {
                                visual.Add(Instantiate(curBuilding.visual, grid.GetWorldPosition(_oldx + i * delta, _oldz), Quaternion.identity));
                                var o = grid.GetGridObject(visual[i].position);
                                visual[i].Find("Bottom").GetComponent<MeshRenderer>().material.color = (o == default || !o.CanBuild()) ? Color.red : Color.green;
                            }
                        }
                        else
                        {
                            int delta = (z > _oldz) ? 1 : -1;
                            for (int i = 1; i < Mathf.Abs(_oldz - z); i ++)
                            {
                                visual.Add(Instantiate(curBuilding.visual, grid.GetWorldPosition(_oldx, _oldz + i * delta), Quaternion.identity));
                                var o = grid.GetGridObject(visual[i].position);
                                visual[i].Find("Bottom").GetComponent<MeshRenderer>().material.color = (o == default || !o.CanBuild()) ? Color.red : Color.green;
                            }
                        }
                    }
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
        // linked initialize
        GridObject go;
        for(int i = 0; i < grid.Width; i++)
            for(int j = 0; j < grid.Height; j++)
            {
                go = grid.GetGridObject(i, j);
                if (go != default && go.Object != null && go.Object.IsLinked) go.Object.IsLinked = false;
            }

        while (changed)
        {
            changed = false;
            for(int i = 0; i < grid.Width; i ++)
                for(int j = 0; j < grid.Height; j++)
                {
                    go = grid.GetGridObject(i, j);
                    if (go == default || go.Object == null || go.Object.SO.type != BuildingType.Road) continue;
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
}
