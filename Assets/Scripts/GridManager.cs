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
            GridObject go = grid.GetGridObject(x, z);
            if (go.CanBuild())
            {
                Transform builtTransform = Instantiate(testType.prefab, grid.GetWorldPosition(x, z), Quaternion.identity);
                go.SetTransform(builtTransform);
            } 
            else
            {
                Utils.CreateWorldTextPopup("Cannot build here!", Utils.GetMouseWorldPosition()); 
            }
        }
    }
}
