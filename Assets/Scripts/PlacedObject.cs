using System.Collections.Generic;
using UnityEngine;
public class PlacedObject : MonoBehaviour
{
    private BuildingType placedObject;
    private Vector2Int origin;
    private Direction dir;
    public static PlacedObject Create(Vector3 worldPosiontion, Vector2Int origin, Direction direction, BuildingType placed)
    {
        Transform placedTransform = Instantiate(placed.prefab, worldPosiontion, Quaternion.Euler(0, placed.GetRotationAngle(direction), 0));
        PlacedObject po = placedTransform.GetComponent<PlacedObject>();
        po.placedObject = placed;
        po.origin = origin;
        po.dir = direction;
        return po;
    }
    public List<Vector2Int> GetGridPostionList()
    {
        return placedObject.GetGridPositionList(origin, dir);
    }
    public void Destroy() 
    {
        Destroy(gameObject);
    }
}