#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class ControllerSetupEditorWindow : EditorWindow
{
    private static string GenerateKey => Application.productName + "_setup_shown";

    private static readonly Dictionary<int, string> LayerDict = new()
    {
        { 7, "Player" },
        { 8, "Climbable" },
        { 9, "Ladders" }
    };

    [InitializeOnLoadMethod]
    private static void OnInitialize()
    {
     //   if (HasOpenInstances<ControllerSetupEditorWindow>()) return;
        if (EditorPrefs.GetBool(GenerateKey, false) || HasOpenInstances<ControllerSetupEditorWindow>()) return;
        ShowWindow();
    }

    public void CreateGUI()
    {
        var label = new Label(
            "Hello,\\n\\nThank you for your support. I hope you find the controller both useful and enjoyable.\\n\\nUpon initial import, you may encounter errors, most likely due to missing dependencies. To maximize the controller's capabilities, I strongly recommend installing the new <b>Input System</b>. While the legacy input system is supported, for future-proofing your game, the new input system is highly advisable.\\n\\nIf you're interested in exploring the demo scene, you'll also need to install the <b>2D Tilemap Editor</b> package.\\n\\nTo install these packages, navigate to Window -> Package Manager and install both '<b>Input System</b>' and '<b>2D Tilemap Editor</b>'.\\n\\nIf the demo scene is not of interest to you, feel free to delete the _Demo folder.\\n\\n<b>Setup:</b> \\nA few layers need to be setup before use. I can automatically define the layers for you by clicking the button below:");
        label.style.whiteSpace = new StyleEnum<WhiteSpace>(WhiteSpace.Normal);
        label.style.marginBottom = 10;
        rootVisualElement.Add(label);

        var layerButton = new Button(CheckLayerConflicts);
        layerButton.text = "Setup Layers";
        layerButton.style.marginBottom = 10;
        rootVisualElement.Add(layerButton);


        var label2 = new Label(
            "Once layers have been added, be sure to set them correctly in the 'Player Stats' scriptable object. Generally something like:\n'Player Layer: Player' & 'Collision Layer: Default, Ground, Climbable, Ladders'\n\n<b>Usage:</b> \\nTo get started, simply drag and drop the prefab into your scene and hit the 'Play' button. The controller is designed to work right out of the box. If you wish to modify player stats, you can do so via the 'Player Stats' scriptable object. You also have the option to create alternate versions of the scriptable object for quick stat swapping.\\n\\nFor direct support, the best way to reach me is through the Patreon-only Discord channel: https://discord.gg/tarodev\\n\\nhttps://www.patreon.com/tarodev\\n\\nBest regards,\\nTarodev");
        label2.style.whiteSpace = new StyleEnum<WhiteSpace>(WhiteSpace.Normal);
        rootVisualElement.Add(label2);

        rootVisualElement.style.paddingLeft = 10;
        rootVisualElement.style.paddingRight = 10;
        rootVisualElement.style.paddingTop = 10;
        rootVisualElement.style.paddingBottom = 10;
    }

    [MenuItem("Tarodev Controller/Setup Window")]
    public static void ShowMyEditor() => ShowWindow();

    private static void ShowWindow()
    {
        EditorWindow wnd = GetWindow<ControllerSetupEditorWindow>();
        wnd.titleContent = new GUIContent("Tarodev Controller Setup");

        var size = new Vector2(500, 625);
        wnd.maxSize = size;
        wnd.minSize = size;
        
        EditorPrefs.SetBool(GenerateKey, true);
    }

    private static void CheckLayerConflicts()
    {
        var (_, layersProp) = GetLayerConfiguration();

        var conflicts = LayerDict.Select(l => layersProp.GetArrayElementAtIndex(l.Key)).Any(layerProp => !string.IsNullOrEmpty(layerProp.stringValue));

        if (conflicts)
        {
            if (EditorUtility.DisplayDialogComplex("Layer Conflict", "One or more layers are already defined. Would you like to overwrite them?", "Yes", "No", "Cancel") == 0) AddLayers();
        }
        else
        {
            AddLayers();
        }
    }

    private static void AddLayers()
    {
        var (tagManager, layersProp) = GetLayerConfiguration();

        foreach (var l in LayerDict)
        {
            var layerProp = layersProp.GetArrayElementAtIndex(l.Key);
            layerProp.stringValue = l.Value;
            tagManager.ApplyModifiedProperties();
        }
        
        Debug.Log("Layers have been set");
    }

    private static (SerializedObject, SerializedProperty) GetLayerConfiguration()
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layersProp = tagManager.FindProperty("layers");
        return (tagManager, layersProp);
    }
}

#endif