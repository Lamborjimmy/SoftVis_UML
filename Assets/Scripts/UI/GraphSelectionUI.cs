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

    private void Start()
    {
        graphDataManager.OnGraphsListed += HandleGraphsListed;
        graphDataManager.ListGraphs(false);
    }
    private void ODestroy()
    {
        graphDataManager.OnGraphsListed -= HandleGraphsListed;
    }

    private void HandleGraphsListed(List<GraphMetadata> graphs)
    {
        foreach (var g in graphs)
        {
            GameObject obj = Instantiate(graphListElementPrefab, graphListElementParent);

            var textComponent = obj.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null) textComponent.text = $"ID: {g.Key}";

            var buttonComponent = obj.GetComponentInChildren<Button>();
            buttonComponent.onClick.AddListener(() => graphDataManager.FetchFullGraph(g.Key));
        }
    }
}