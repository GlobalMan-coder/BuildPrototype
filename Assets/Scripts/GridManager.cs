using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GridManager : MonoBehaviour
{
    private BuildingPrefab curBuilding;
    [SerializeField] private Transform Plane;
    public static GridManager Instance { get; private set; }
    private GridType<GridObject> grid;
    private Direction direction = Direction.Down;
    private Transform visual;
    private bool isDragging = false;
    private Vector3 targetPosition;

    private int _oldx, _oldz;
    public BuildingPrefab CurrentBuilding
    {
        get { return curBuilding; }
        set 
        {
            curBuilding = value;
            if (visual) Destroy(visual.gameObject);
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
        private PlacedObject placedObject;
        public GridObject(object grid, int x, int z)
        {
            this.grid = grid as GridType<GridObject>;
            this.x = x;
            this.z = z;
        }
        public void SetObject(PlacedObject placedObject)
        {
            this.placedObject = placedObject;
            grid.TriggerGridObjectChanged(x, z);
        }
        public PlacedObject GetObject() { return placedObject; }
        public bool CanBuild()
        {
            return placedObject == null;
        }

        public void ClearObject()
        {
            placedObject = null;
        }

        public override string ToString()
        { 
            return x + ", " + z;
        }
    }
    public void Rotate()
    {
        if (!CurrentBuilding) return;
        Vector2Int rotationOffset = curBuilding.GetRotationOffset(direction);
        direction = BuildingPrefab.GetNextDirection(direction);
        targetPosition = GetMouseWorldSnappedPosition(visual.position + new Vector3(1, 0, 1) - new Vector3(rotationOffset.x, 0, rotationOffset.y) * grid.CellSize);
    }
    public void Place()
    {
        if (visual == null) return;
        grid.GetXZ(visual.transform.position, out int x, out int z);
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

        if (canBuild)
        {
            Vector2Int ro = curBuilding.GetRotationOffset(direction);
            var placedObject = PlacedObject.Create(
                new Vector3(visual.transform.position.x, 0, visual.transform.position.z)
                , new Vector2Int(x, z)
                , direction
                , curBuilding
                );

            foreach (Vector2Int pos in gridPositionList)
            {
                grid.GetGridObject(pos.x, pos.y).SetObject(placedObject);
            }
            CurrentBuilding = null;
            Destroy(visual.gameObject);
            visual = null;
            direction = Direction.Down;
        }
        else
        {
            MessageManager.Instance.AddMessage("Cannot build there.", Color.red);
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
            placedObjectWorldPosition.y = 1;
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
            if (GridManager.Instance.CurrentBuilding != null)
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
            PlacedObject obj = go.GetObject();
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
            if (visual == null)
            {
                float x = Input.GetAxis("Mouse X");
                float y = Input.GetAxis("Mouse Y");
                Camera.main.transform.position += new Vector3(-x - y, 0, x - y);
            }
            else
            {
                targetPosition = GetMouseWorldSnappedPosition();
                grid.GetXZ(visual.transform.position, out int x, out int z);
                if (x != _oldx || z != _oldz)
                {
                    List<Vector2Int> gridPositionList = curBuilding.GetGridPositionList(new Vector2Int(x, z), direction);
                    bool canBuild = true;
                    foreach (Vector2Int pos in gridPositionList)
                    {
                        var o = grid.GetGridObject(pos.x, pos.y);
                        if (o == default) return;
                        if (!o.CanBuild())
                        {
                            canBuild = false;
                            break;
                        }
                    }
                    visual.Find("Bottom").GetComponent<MeshRenderer>().material.color = (canBuild) ? Color.green : Color.red;
                }
            }
        }
        if (visual != null)
        {
            visual.position = Vector3.Lerp(visual.position, targetPosition, Time.deltaTime * 15f);
            visual.rotation = Quaternion.Lerp(visual.rotation, GetPlacedObjectRotation(), Time.deltaTime * 15f);
        }
    }
    private void RefreshVisual()
    {
        BuildingPrefab curBuilding = GridManager.Instance.CurrentBuilding;

        if (curBuilding != null)
        {
            if (visual != null)
                Destroy(visual.gameObject);
            visual = Instantiate(curBuilding.visual, Vector3.zero, Quaternion.identity);
            visual.parent = transform;
        }
    }
}
