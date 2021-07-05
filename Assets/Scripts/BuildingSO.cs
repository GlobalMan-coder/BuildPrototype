using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu()]
public class BuildingSO : ScriptableObject
{
    public string nameString;
    public Transform prefab;
    public Transform visual;
    public Sprite texture;
    public int width;
    public int height;
    public float buildTime;
    public BuildingType type;

    public static Direction GetNextDirection(Direction dir) { return (dir == Direction.Right) ? Direction.Down : ++dir ; }
    public int GetRotationAngle(Direction dir) { return (int)dir * 90; }
    public Vector2Int GetRotationOffset(Direction dir)
    {
        switch (dir)
        {
            default:
            case Direction.Down: return new Vector2Int(0, 0);
            case Direction.Left: return new Vector2Int(0, width);
            case Direction.Up: return new Vector2Int(width, height);
            case Direction.Right: return new Vector2Int(height, 0);
        }
    }
    public List<Vector2Int> GetGridPositionList(Vector2Int offset, Direction dir)
    {
        List<Vector2Int> gridPositionList = new List<Vector2Int>();
        switch (dir)
        {
            default:
            case Direction.Down:
            case Direction.Up:
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        gridPositionList.Add(offset + new Vector2Int(x, y));
                    }
                }
                break;
            case Direction.Left:
            case Direction.Right:
                for (int x = 0; x < height; x++)
                {
                    for (int y = 0; y < width; y++)
                    {
                        gridPositionList.Add(offset + new Vector2Int(x, y));
                    }
                }
                break;
        }
        return gridPositionList;
    }
}
public enum Direction
{
    Down,
    Left,
    Up,
    Right
}
public enum BuildingType
{
    Building,
    Road,
    TownCenter
}