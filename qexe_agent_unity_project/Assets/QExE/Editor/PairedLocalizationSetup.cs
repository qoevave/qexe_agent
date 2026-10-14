using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using QExE.Localization;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor menu items for paired (A/B) localization:
///   QExE > Paired Localization > 1. Create Method Interface Prefab
///   QExE > Paired Localization > 2. Set Up Open Scene
///   QExE > Paired Localization > Validate Open Scene
/// The single-stimulus localization menu (QExE > Localization) is unchanged.
/// </summary>
public static class PairedLocalizationSetup
{
    private const string SourceInterfacePrefab = "Assets/QExE/UI/Prefabs/Interfaces/ACR.prefab";
    private const string MethodsResourcesFolder = "Assets/QExE/Resources/QExEMethods";
    private const string InterfacePrefab = MethodsResourcesFolder + "/PairedLocalization.prefab";

    // ---------------------------------------------------------------------------------------------
    // 1. Method interface prefab (methodType "PairedLocalization" -> Resources/QExEMethods/PairedLocalization)
    // ---------------------------------------------------------------------------------------------

    [MenuItem("QExE/Paired Localization/1. Create Method Interface Prefab", priority = 101)]
    public static void CreateMethodInterfacePrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(SourceInterfacePrefab) == null)
        {
            EditorUtility.DisplayDialog("QExE", "Source prefab not found:\n" + SourceInterfacePrefab, "OK");
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(InterfacePrefab) != null &&
            !EditorUtility.DisplayDialog("QExE", "PairedLocalization.prefab already exists. Recreate it?", "Recreate", "Cancel"))
            return;

        EnsureFolder(MethodsResourcesFolder);
        AssetDatabase.DeleteAsset(InterfacePrefab);
        if (!AssetDatabase.CopyAsset(SourceInterfacePrefab, InterfacePrefab))
        {
            EditorUtility.DisplayDialog("QExE", "Could not copy " + SourceInterfacePrefab, "OK");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(InterfacePrefab);
        try
        {
            root.name = "PairedLocalization";

            var acr = root.GetComponent<ACR>();
            if (acr != null) Object.DestroyImmediate(acr);

            foreach (var name in new[] { "sliders", "sliderLabels" })
            {
                var t = FindChild(root.transform, name);
                if (t != null) t.gameObject.SetActive(false);
            }

            var ui = root.GetComponent<PairedLocalizationMethodUI>();
            if (ui == null) ui = root.AddComponent<PairedLocalizationMethodUI>();
            ui.startButton = FindComponent<Button>(root.transform, "btn/StartPlayback");
            ui.nextButton = FindComponent<Button>(root.transform, "btn/Next");
            ui.titleText = FindComponent<TMP_Text>(root.transform, "Title");
            ui.statusText = FindComponent<TMP_Text>(root.transform, "Question");
            if (ui.titleText != null) ui.titleText.text = ui.title;
            if (ui.statusText != null) ui.statusText.text = "Press Start to begin.";

            PrefabUtility.SaveAsPrefabAsset(root, InterfacePrefab);

            var missing = new List<string>();
            if (ui.startButton == null) missing.Add("btn/StartPlayback");
            if (ui.nextButton == null) missing.Add("btn/Next");
            if (ui.statusText == null) missing.Add("Question (status text)");
            Debug.Log("QExE: created " + InterfacePrefab + (missing.Count > 0 ? " - could not find: " + string.Join(", ", missing) : ""));
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(InterfacePrefab);
        EditorGUIUtility.PingObject(Selection.activeObject);
    }

    // ---------------------------------------------------------------------------------------------
    // 2. Scene setup: turns a localization scene (e.g. a copy of _localization) into a paired one
    // ---------------------------------------------------------------------------------------------

    [MenuItem("QExE/Paired Localization/2. Set Up Open Scene", priority = 102)]
    public static void SetUpOpenScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name == "_localization" &&
            !EditorUtility.DisplayDialog("QExE", "The open scene is _localization (the single-stimulus test). " +
                                                 "Converting it removes its LocalizationTrialController.\n\nContinue anyway?", "Convert", "Cancel"))
            return;

        var pointer = Object.FindFirstObjectByType<LocalizationPointer>(FindObjectsInactive.Include);
        if (pointer == null)
        {
            EditorUtility.DisplayDialog("QExE",
                "No localization components in this scene.\n\nRun QExE > Localization > 2. Add Localization To Open Scene first " +
                "(it adds the pointer, dial, ring, alignment and listening position), then run this again.", "OK");
            return;
        }
        GameObject go = pointer.gameObject;

        // The single-stimulus controller would fight over the dial and pointer: remove it.
        foreach (var single in go.GetComponents<LocalizationTrialController>())
            Undo.DestroyObjectImmediate(single);

        var dial = go.GetComponent<LocalizationDial>();
        if (dial == null) dial = Undo.AddComponent<LocalizationDial>(go);
        var ring = go.GetComponent<LocalizationHeadRing>();
        if (ring == null) ring = Undo.AddComponent<LocalizationHeadRing>(go);
        var alignment = go.GetComponent<LocalizationAlignment>();
        if (alignment == null) alignment = Undo.AddComponent<LocalizationAlignment>(go);
        var buttons = go.GetComponent<LocalizationABButtons>();
        if (buttons == null) buttons = Undo.AddComponent<LocalizationABButtons>(go);
        var controller = go.GetComponent<PairedLocalizationController>();
        if (controller == null) controller = Undo.AddComponent<PairedLocalizationController>(go);

        Undo.RecordObjects(new Object[] { pointer, dial, ring, alignment, buttons, controller }, "Wire QExE Paired Localization");
        controller.pointer = pointer;
        controller.dial = dial;
        controller.headRing = ring;
        controller.alignment = alignment;
        controller.buttons = buttons;
        buttons.dial = dial;
        pointer.dial = dial;
        if (alignment.head == null) alignment.head = pointer.head;

        var oscManager = GameObject.Find("OSCManager");
        if (oscManager != null)
        {
            controller.receiver = oscManager.GetComponent<extOSC.OSCReceiver>();
            controller.transmitter = oscManager.GetComponent<extOSC.OSCTransmitter>();
        }

        // Room for the A/B buttons between the dial and its counter / status text.
        if (dial.textExtraOffset < buttons.RequiredTextOffset)
            dial.textExtraOffset = buttons.RequiredTextOffset;

        foreach (var c in new Object[] { pointer, dial, ring, alignment, buttons, controller })
            EditorUtility.SetDirty(c);

        dial.Rebuild();
        ring.Rebuild();
        alignment.Rebuild();
        buttons.Rebuild();
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = go;

        AddSceneToBuildSettings(scene.path);
        Validate(true);
    }

    [MenuItem("QExE/Paired Localization/Validate Open Scene", priority = 120)]
    public static void ValidateMenu()
    {
        Validate(true);
    }

    private static bool Validate(bool showDialog)
    {
        var report = new StringBuilder();
        bool ok = true;

        void Check(bool condition, string good, string bad)
        {
            report.AppendLine((condition ? "OK    " : "FIX   ") + (condition ? good : bad));
            ok &= condition;
        }

        var scene = EditorSceneManager.GetActiveScene();
        var controller = Object.FindFirstObjectByType<PairedLocalizationController>(FindObjectsInactive.Include);
        var single = Object.FindFirstObjectByType<LocalizationTrialController>(FindObjectsInactive.Include);
        var pointer = controller != null ? controller.pointer : null;
        var osc = GameObject.Find("OSCManager");

        Check(!string.IsNullOrEmpty(scene.path), "Scene is saved: " + scene.path,
              "Save the scene first (its name must match unityScene_ID in the config, e.g. _pairedLocalization).");
        Check(osc != null && osc.GetComponent<extOSC.OSCReceiver>() != null && osc.GetComponent<extOSC.OSCTransmitter>() != null,
              "OSCManager with receiver and transmitter", "Add the OSCManager prefab (Assets/QExE/Prefabs/OSCManager.prefab).");
        Check(GameObject.Find("QExETestInterface") != null, "QExETestInterface found (menu anchor)",
              "No 'QExETestInterface' object. Use the QExE XR Set Up rig prefab.");
        Check(controller != null, "PairedLocalizationController present", "Run QExE > Paired Localization > 2. Set Up Open Scene.");
        Check(single == null, "No single-stimulus LocalizationTrialController in this scene",
              "Remove the LocalizationTrialController (run QExE > Paired Localization > 2. Set Up Open Scene).");
        Check(controller != null && controller.dial != null && controller.headRing != null && controller.alignment != null && controller.buttons != null,
              "Dial, ring, alignment and A/B buttons connected", "Run QExE > Paired Localization > 2. Set Up Open Scene again.");
        Check(pointer != null && pointer.head != null && pointer.pointerOrigin != null,
              "Pointer head and origin assigned", "Assign LocalizationPointer.head (camera) and pointerOrigin (controller).");
        Check(controller == null || controller.dial == null || controller.buttons == null ||
              controller.dial.textExtraOffset >= controller.buttons.RequiredTextOffset - 1e-4f,
              "Dial text sits above the A/B buttons",
              "Set LocalizationDial.textExtraOffset to at least " + (controller != null && controller.buttons != null ? controller.buttons.RequiredTextOffset.ToString("F3") : "the buttons' size") + ".");
        Check(AssetDatabase.LoadAssetAtPath<GameObject>(InterfacePrefab) != null, "Method interface prefab exists",
              "Run QExE > Paired Localization > 1. Create Method Interface Prefab.");
        Check(EditorBuildSettings.scenes.Any(s => s.path == scene.path && s.enabled), "Scene is in the build settings",
              "Add the scene to File > Build Profiles > Scene List.");
        Check(Object.FindFirstObjectByType<LocalizationSpace>(FindObjectsInactive.Include) != null,
              "Listening position (LocalizationSpace) present", "Run QExE > Localization > 2. Add Localization To Open Scene to add it.");

        Debug.Log("QExE paired localization scene check:\n" + report);
        if (showDialog)
            EditorUtility.DisplayDialog(ok ? "QExE: scene ready" : "QExE: scene needs attention", report.ToString(), "OK");
        return ok;
    }

    // ---------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------

    private static void AddSceneToBuildSettings(string scenePath)
    {
        if (string.IsNullOrEmpty(scenePath)) return;
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == scenePath)) return;
        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("QExE: added " + scenePath + " to the build settings.");
    }

    private static Transform FindChild(Transform root, string name)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    private static T FindComponent<T>(Transform root, string name) where T : Component
    {
        var t = FindChild(root, name);
        return t != null ? t.GetComponent<T>() : null;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        var parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }
}
