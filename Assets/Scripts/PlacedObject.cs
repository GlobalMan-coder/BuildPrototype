using System.Collections.Generic;
using UnityEngine;
public class PlacedObject : MonoBehaviour
{
    private BuildingPrefab placedObject;
    private Vector2Int origin;
    private Direction dir;
    private bool isLinked = false;
    public bool IsConstructing { get; private set; }
    public bool IsBuilt { get; private set; }
    public bool IsLinked { 
        get { return IsLinked; }
        private set {
            isLinked = value;
            if (isLinked && !IsBuilt) IsConstructing = true;
        }
    }
    public static PlacedObject Create(Vector3 worldPosiontion, Vector2Int origin, Direction direction, BuildingPrefab placed)
    {
        Transform placedTransform = Instantiate(placed.prefab, worldPosiontion, Quaternion.Euler(0, placed.GetRotationAngle(direction), 0));
        PlacedObject po = placedTransform.GetComponent<PlacedObject>();
        po.placedObject = placed;
        po.origin = origin;
        po.dir = direction;
        po.CheckLinkLoad();
        return po;
    }
    private void Update()
    {
        if (IsConstructing)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, Time.deltaTime / placedObject.buildTime);
        }
    }
    public List<Vector2Int> GetGridPostionList()
    {
        return placedObject.GetGridPositionList(origin, dir);
    }
    public void Destroy() 
    {
        Destroy(gameObject);
    }
    public void CheckLinkLoad()
    {
        IsLinked = true;
    }
}