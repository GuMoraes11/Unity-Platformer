using UnityEngine;
using TMPro;

public static class PlayerLabelFactory
{
    public static void CreateLabel(Transform target, string text)
    {
        GameObject go = new GameObject(text + "_Label");
        var tmp = go.AddComponent<TextMeshPro>();
        var label = go.AddComponent<PlayerLabelBillboard>();

        tmp.text = text;
        tmp.fontSize = 4f;
        tmp.sortingOrder = 100;
        tmp.alignment = TextAlignmentOptions.Center;

        label.Initialize(target, text);
    }
}