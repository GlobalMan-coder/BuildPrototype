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

    internal static void CreateWorldTextPopup(string text, Vector3 vector3)
    {
        TextMesh tm = new GameObject().AddComponent<TextMesh>();
        tm.transform.position = vector3;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.text = text;
        iTween.MoveBy(tm.gameObject, iTween.Hash("y", 5, "easeType", "easeOutExpo"));
        GameObject.Destroy(tm.gameObject, 1);
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
