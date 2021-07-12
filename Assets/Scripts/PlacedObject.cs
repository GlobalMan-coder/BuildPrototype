using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class PlacedObject : MonoBehaviour
{
    private bool isLinked = false;
    public BuildingSO SO { get; private set; }
    public Vector2Int origin { get; private set; }
    public Direction Dir { get; private set; }
    public bool IsBuilt { get; private set; }
    public bool IsLinked
    {
        get { return isLinked; }
        set
        {
            isLinked = value;
            IsConstructing = isLinked && !IsBuilt;
        }
    }
    public bool IsConstructing { get; private set; }
    public static PlacedObject Create(Vector3 worldPosiontion, Vector2Int origin, Direction direction, BuildingSO placed)
    {
        Transform placedTransform = Instantiate(placed.prefab, worldPosiontion, Quaternion.Euler(0, placed.GetRotationAngle(direction), 0));
        PlacedObject po = placedTransform.GetComponent<PlacedObject>();
        po.SO = placed;
        po.origin = origin;
        po.Dir = direction;
        if (po.SO.type == BuildingType.Building)
            po.CheckLinkLoad();
        if (po.SO.type == BuildingType.TownCenter)
            po.IsLinked = true;
        return po;
    }
    private void Update()
    {
        if (IsConstructing)
        {
            transform.localScale += new Vector3(0, Time.deltaTime / SO.buildTime, 0);
            if (transform.localScale.y >= 1)
            {
                IsConstructing = false;
                transform.Find("Bottom").gameObject.SetActive(false);
            }
        }
    }
    public List<Vector2Int> GetGridPostionList()
    {
        return SO.GetGridPositionList(origin, Dir);
    }
    public void Destroy() 
    {
        Destroy(gameObject);
    }
    public void CheckLinkLoad()
    {
        var gpl = GetGridPostionList();
        IsLinked = false;
        foreach (var gp in gpl)
        {
            if (GridManager.Instance.CheckPointLink(gp.x, gp.y))
            {
                IsLinked = true;
                return;
            }
        }
    }
}