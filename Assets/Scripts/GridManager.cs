using System;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] List<BuildingType> buildingTypes;
    private BuildingType curBuilding;
    [SerializeField] private Transform Plane;

    public static GridManager Instance { get; private set; }
    private GridType<GridObject> grid;
    private Direction direction = Direction.Down;

    public event EventHandler OnSelectedChanged;
    public event EventHandler OnObjectPlaced;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        int gridWidth = 10;
        int gridHeight = 10;
        float cellSize = 10f;
        Plane.localScale = new Vector3(gridWidth + 2, 10, gridHeight + 2);
        Plane.localPosition = new Vector3(gridWidth * cellSize / 2f, 0, gridHeight * cellSize / 2f);
        grid = new GridType<GridObject>(gridWidth, gridHeight, cellSize, Vector3.zero, (GridType<GridObject> g, int x, int z) => new GridObject(g, x, z));
        curBuilding = buildingTypes[0];
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
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            grid.GetXZ(Utils.GetMouseWorldPosition(), out int x, out int z);
            List<Vector2Int> gridPositionList = curBuilding.GetGridPositionList(new Vector2Int(x, z), direction);

            bool canBuild = true;
            foreach(Vector2Int pos in gridPositionList)
            {
                if(!grid.GetGridObject(pos.x, pos.y).CanBuild())
                {
                    canBuild = false;
                    break;
                }
            }

            
            if (canBuild)
            {
                Vector2Int ro = curBuilding.GetRotationOffset(direction);

                var placedObject = PlacedObject.Create(
                    grid.GetWorldPosition(x, z) + new Vector3(ro.x, 0, ro.y) * grid.CellSize
                    , new Vector2Int(x, z)
                    , direction
                    , curBuilding
                    );
                
                foreach(Vector2Int pos in gridPositionList)
                {
                    grid.GetGridObject(pos.x, pos.y).SetObject(placedObject);
                }
            } 
            else
            {
                Utils.CreateWorldTextPopup("Cannot build here!", Utils.GetMouseWorldPosition());
            }
        }

        if (Input.GetMouseButton(1))
        {
            GridObject go = grid.GetGridObject(Utils.GetMouseWorldPosition());
            PlacedObject obj = go.GetObject();
            if(obj != null)
            {
                List<Vector2Int> gridPosList = obj.GetGridPostionList();
                obj.Destroy();
                foreach (Vector2Int pos in gridPosList)
                {
                    grid.GetGridObject(pos.x, pos.y).ClearObject();
                }
            }
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            direction = BuildingType.GetNextDirection(direction);
            Utils.CreateWorldTextPopup("" + direction, Utils.GetMouseWorldPosition());
        }
        if(Input.GetKeyDown(KeyCode.Alpha1)) { curBuilding = buildingTypes[0]; RefreshSelectedBuilding(); }
        if(Input.GetKeyDown(KeyCode.Alpha2)) { curBuilding = buildingTypes[1]; RefreshSelectedBuilding(); }
        if(Input.GetKeyDown(KeyCode.Alpha3)) { curBuilding = buildingTypes[2]; RefreshSelectedBuilding(); }
    }
    public Vector3 GetMouseWorldSnappedPosition()
    {
        Vector3 mousePosition = Utils.GetMouseWorldPosition();
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

    public BuildingType GetCurObject()
    {
        return curBuilding;
    }

    private void RefreshSelectedBuilding()
    {
        OnSelectedChanged?.Invoke(this, EventArgs.Empty);
    }
}
