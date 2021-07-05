using System;
using UnityEngine;

internal class GridType<TGridObject>
{
    public event EventHandler<OnGridObjectChangedEventArgs> OngridObjectChanged;
    public class OnGridObjectChangedEventArgs
    {
        public int x;
        public int z;
    }

    public int Width { get; private set; }
    public int Height { get; private set; }
    private float cellSize;
    private Vector3 originPosition;
    private TGridObject[,] gridArray;

    public float CellSize { get {return cellSize; } }

    public GridType(int width, int height, float cellSize, Vector3 originPosition, Func<GridType<TGridObject>, int, int, TGridObject> createGridObject)
    {
        this.Width = width;
        this.Height = height;
        this.cellSize = cellSize;
        this.originPosition = originPosition;

        gridArray = new TGridObject[width, height];
        for(int x = 0; x< gridArray.GetLength(0); x++)
            for(int z = 0; z < gridArray.GetLength(1); z++)
            {
                gridArray[x, z] = createGridObject(this, x, z);
            }

        bool showDebug = true;
        if (showDebug)
        {
            TextMesh[,] debugTextArray = new TextMesh[width, height];

            for(int x = 0; x < gridArray.GetLength(0); x ++)
                for(int z = 0; z < gridArray.GetLength(1); z++)
                {
                    debugTextArray[x, z] = Utils.CreateWorldText(gridArray[x, z]?.ToString(), GetWorldPosition(x, z) + new Vector3(cellSize, 0, cellSize) * .5f);
                    Debug.DrawLine(GetWorldPosition(x, z), GetWorldPosition(x, z + 1), Color.white, 100f);
                    Debug.DrawLine(GetWorldPosition(x, z), GetWorldPosition(x + 1, z), Color.white, 100f);
                }
            Debug.DrawLine(GetWorldPosition(0, height), GetWorldPosition(width, height), Color.white, 100f);
            Debug.DrawLine(GetWorldPosition(width, 0), GetWorldPosition(width, height), Color.white, 100f);

            OngridObjectChanged += (object sender, OnGridObjectChangedEventArgs eventArgs) =>
            {
                debugTextArray[eventArgs.x, eventArgs.z].text = gridArray[eventArgs.x, eventArgs.z]?.ToString();
            };
        }
    }

    internal TGridObject GetGridObject(int x, int z)
    {
        if(x >= 0 && z >= 0 && x < Width && z < Height)
        {
            return gridArray[x, z];
        }else
        {
            return default;
        }
    }
    
    internal TGridObject GetGridObject(Vector3 worldPosition)
    {
        int x, z;
        GetXZ(worldPosition, out x, out z);
        return GetGridObject(x, z);
    }
    
    internal void TriggerGridObjectChanged(int x, int z)
    {
        OngridObjectChanged?.Invoke(this, new OnGridObjectChangedEventArgs { x = x, z = z });
    }

    internal void GetXZ(Vector3 worldPosition, out int x, out int z)
    {
        x = Mathf.FloorToInt((worldPosition - originPosition).x / cellSize);
        z = Mathf.FloorToInt((worldPosition - originPosition).z / cellSize);
    }

    public Vector3 GetWorldPosition(int x, int z)
    {
        return new Vector3(x, 0, z) * cellSize + originPosition;
    }

}