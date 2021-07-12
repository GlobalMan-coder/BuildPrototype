using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class BuildingButton : MonoBehaviour
{
    [SerializeField] List<BuildingSO> buildingTypes;
    [SerializeField] private Transform BuildingButtonPanel;
    [SerializeField] private Transform BuildingButtonPrefab;
    [SerializeField] private Button AutoModeButton;
    private void Start()
    {
        foreach(BuildingSO b in buildingTypes)
        {
            Transform button = Instantiate(BuildingButtonPrefab, BuildingButtonPanel);
            button.GetComponent<Image>().sprite = b.texture;
            button.GetChild(0).GetComponent<Text>().text = b.name;
            button.GetChild(1).GetComponent<Text>().text = b.width + "×" + b.height;
            button.GetComponent<Button>().onClick.AddListener(delegate { GridManager.Instance.CurrentBuilding = b; });
        }
        AutoModeButton.onClick.AddListener(delegate { ChangeMode(); });
    }
    private void ChangeMode()
    {
        GridManager.Instance.autoMode = !GridManager.Instance.autoMode;
        AutoModeButton.GetComponent<Image>().color = (GridManager.Instance.autoMode) ? Color.green : Color.red;
    }
}
