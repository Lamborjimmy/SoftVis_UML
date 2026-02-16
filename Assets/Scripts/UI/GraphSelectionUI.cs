using System.Collections.Generic;
using Assets.Scripts.Data;
using Assets.Scripts.Graphs;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GraphSelectionUI : MonoBehaviour
{
    [SerializeField] private GameObject graphListElementPrefab;
    [SerializeField] private Transform graphListElementParent;
    [SerializeField] private GraphDataManager graphDataManager;
    private GraphVisualizer graphVisualizer;
    private List<string> visualizedGraphIds = new List<string>();
    private void Start()
    {
        graphDataManager.OnGraphsListed += HandleGraphsListed;
        graphDataManager.ListGraphs(false);
        graphVisualizer = GetComponent<GraphVisualizer>();
    }
    private void OnDestroy()
    {
        graphDataManager.OnGraphsListed -= HandleGraphsListed;
    }

    private void HandleGraphsListed(List<GraphMetadata> graphs)
    {
        foreach (var g in graphs)
        {
            GameObject obj = Instantiate(graphListElementPrefab, graphListElementParent);
            var buttonComponent = obj.GetComponentInChildren<Button>();
            var textComponent = buttonComponent.GetComponentInChildren<TextMeshProUGUI>();
            var buttonImage = buttonComponent.GetComponent<Image>();

            buttonComponent.onClick.AddListener(() =>
                HandleButtonClick(g.Key, textComponent, buttonImage));
        }
    }
    private void HandleButtonClick(string key, TextMeshProUGUI btnText, Image btnImage)
    {
        if (!visualizedGraphIds.Contains(key))
        {
            visualizedGraphIds.Add(key);
            graphDataManager.FetchFullGraph(key);

            btnText.text = "Remove";
            btnImage.color = Color.red;

            Debug.Log($"Visualizing graph: {key}");
        }
        else
        {
            visualizedGraphIds.Remove(key);

            graphVisualizer.RemoveGraph(key);

            btnText.text = "Visualize";
            btnImage.color = Color.white;

            Debug.Log($"Removed graph: {key}");
        }
    }
}