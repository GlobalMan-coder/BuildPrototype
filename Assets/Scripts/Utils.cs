using UnityEngine;

class Utils
{
    private static int mouseColliderLayMask = LayerMask.NameToLayer("MouseLaycast");
    static Transform textParent;
    internal static TextMesh CreateWorldText(string text, Vector3 vector3)
    {
        if (!textParent)
        {
            textParent = new GameObject().transform;
            textParent.position = Vector3.zero;
        }
        TextMesh tm = new GameObject().AddComponent<TextMesh>();
        tm.transform.position = vector3;
        tm.anchor = TextAnchor.LowerCenter;
        tm.text = text;
        tm.transform.parent = textParent;
        return tm;
    }

    internal static Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f, mouseColliderLayMask))
        {
            return raycastHit.point;
        }
        else
        {
            return Vector3.zero;
        }
    }
}
