using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private BuildingType testType;
    [SerializeField] private Transform Plane;
    private GridType<GridObject> grid;
    private void Awake()
    {
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
        private Transform transform;
        public GridObject(object grid, int x, int z)
        {
            this.grid = grid as GridType<GridObject>;
            this.x = x;
            this.z = z;
        }

        public void SetTransform(Transform transform)
        {
            this.transform = transform;
            grid.TriggerGridObjectChanged(x, z);
        }
        public bool CanBuild()
        {
            return transform == null;
        }

        public void ClearTransform()
        {
            transform = null;
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
            List<Vector2Int> gridPositionList = testType.GetGridPositionList(new Vector2Int(x, z), BuildingType.Direction.Down);

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
                Transform builtTransform = Instantiate(testType.prefab, grid.GetWorldPosition(x, z), Quaternion.identity);
                foreach(Vector2Int pos in gridPositionList)
                {
                    grid.GetGridObject(pos.x, pos.y).SetTransform(builtTransform);
                }
            } 
            else
            {
                Utils.CreateWorldTextPopup("Cannot build here!", Utils.GetMouseWorldPosition()); 
            }
        }
    }
}
