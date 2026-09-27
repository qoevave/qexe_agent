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
/// Editor menu items that set up the localization method:
///   QExE > Localization > 1. Create Method Interface Prefab
///   QExE > Localization > 2. Add Localization To Open Scene
///   QExE > Localization > 3. Rebuild Visuals
///   QExE > Localization > Apply Neutral Environment
///   QExE > Localization > Validate Open Scene
/// </summary>
public static class LocalizationSetup
{
    private const string SourceInterfacePrefab = "Assets/QExE/UI/Prefabs/Interfaces/ACR.prefab";
    private const string MethodsResourcesFolder = "Assets/QExE/Resources/QExEMethods";
    private const string LocalizationInterfacePrefab = MethodsResourcesFolder + "/Localization.prefab";
    private const string ControllerObjectName = "QExELocalization";

    private static readonly string[] RightControllerNames =
    {
        "Right Controller", "RightHand Controller", "Right Hand Controller", "RightHand", "Right Hand", "RightController"
    };

    private static readonly string[] RigRootNames = { "XR Origin", "XR Set Up", "XRRig", "XR Rig" };

    // ---------------------------------------------------------------------------------------------
    // 1. Method interface prefab
    // ---------------------------------------------------------------------------------------------

    [MenuItem("QExE/Localization/1. Create Method Interface Prefab", priority = 1)]
    public static void CreateMethodInterfacePrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(SourceInterfacePrefab) == null)
        {
            EditorUtility.DisplayDialog("QExE", "Source prefab not found:\n" + SourceInterfacePrefab, "OK");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(LocalizationInterfacePrefab) != null &&
            !EditorUtility.DisplayDialog("QExE", "Localization.prefab already exists. Recreate it?", "Recreate", "Cancel"))
            return;

        EnsureFolder(MethodsResourcesFolder);
        AssetDatabase.DeleteAsset(LocalizationInterfacePrefab);
        if (!AssetDatabase.CopyAsset(SourceInterfacePrefab, LocalizationInterfacePrefab))
        {
            EditorUtility.DisplayDialog("QExE", "Could not copy " + SourceInterfacePrefab, "OK");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(LocalizationInterfacePrefab);
        try
        {
            root.name = "Localization";

            // Remove the ACR method logic, keep OSCPacket / TestHooks / colour management.
            var acr = root.GetComponent<ACR>();
            if (acr != null) Object.DestroyImmediate(acr);

            // Rating sliders are not needed for localization.
            foreach (var name in new[] { "sliders", "sliderLabels" })
            {
                var t = FindChild(root.transform, name);
                if (t != null) t.gameObject.SetActive(false);
            }

            var ui = root.GetComponent<LocalizationMethodUI>();
            if (ui == null) ui = root.AddComponent<LocalizationMethodUI>();
            ui.startButton = FindComponent<Button>(root.transform, "btn/StartPlayback");
            ui.nextButton = FindComponent<Button>(root.transform, "btn/Next");
            ui.titleText = FindComponent<TMP_Text>(root.transform, "Title");
            ui.statusText = FindComponent<TMP_Text>(root.transform, "Question");

            if (ui.titleText != null) ui.titleText.text = ui.title;
            if (ui.statusText != null) ui.statusText.text = "Press Start to begin.";

            PrefabUtility.SaveAsPrefabAsset(root, LocalizationInterfacePrefab);

            var missing = new List<string>();
            if (ui.startButton == null) missing.Add("btn/StartPlayback");
            if (ui.nextButton == null) missing.Add("btn/Next");
            if (ui.statusText == null) missing.Add("Question (status text)");
            Debug.Log("QExE: created " + LocalizationInterfacePrefab +
                      (missing.Count > 0 ? " - could not find: " + string.Join(", ", missing) : ""));
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(LocalizationInterfacePrefab);
        EditorGUIUtility.PingObject(Selection.activeObject);
    }

    // ---------------------------------------------------------------------------------------------
    // 2. Scene setup
    // ---------------------------------------------------------------------------------------------

    [MenuItem("QExE/Localization/2. Add Localization To Open Scene", priority = 2)]
    public static void AddToOpenScene()
    {
        var scene = EditorSceneManager.GetActiveScene();

        var existing = Object.FindFirstObjectByType<LocalizationTrialController>(FindObjectsInactive.Include);
        GameObject go;
        if (existing != null)
        {
            go = existing.gameObject;
            Debug.Log("QExE: scene already has a LocalizationTrialController, re-wiring it.");
        }
        else
        {
            go = new GameObject(ControllerObjectName);
            Undo.RegisterCreatedObjectUndo(go, "Add QExE Localization");
        }

        // Note: no '??' here, Unity's fake-null objects would slip through it.
        var pointer = go.GetComponent<LocalizationPointer>();
        if (pointer == null) pointer = Undo.AddComponent<LocalizationPointer>(go);
        var controller = go.GetComponent<LocalizationTrialController>();
        if (controller == null) controller = Undo.AddComponent<LocalizationTrialController>(go);
        var dial = go.GetComponent<LocalizationDial>();
        if (dial == null) dial = Undo.AddComponent<LocalizationDial>(go);
        var ring = go.GetComponent<LocalizationHeadRing>();
        if (ring == null) ring = Undo.AddComponent<LocalizationHeadRing>(go);
        var alignment = go.GetComponent<LocalizationAlignment>();
        if (alignment == null) alignment = Undo.AddComponent<LocalizationAlignment>(go);
        var space = go.GetComponent<LocalizationSpace>();
        bool newSpace = space == null;
        if (newSpace)
        {
            space = Undo.AddComponent<LocalizationSpace>(go);
            // Start at the XR rig's floor origin, facing the rig's front. Move / rotate QExELocalization
            // afterwards to put the listening position where the participant sits.
            var rigOrigin = FindByNames(new[] { "XR Origin (XR Rig)", "XR Origin" });
            if (rigOrigin == null) rigOrigin = FindRigRoot();
            if (rigOrigin != null)
            {
                Undo.RecordObject(go.transform, "Place listening position");
                go.transform.position = rigOrigin.position;
                go.transform.rotation = LocalizationVisuals.ReferenceYaw(rigOrigin);
            }
        }
        Undo.RecordObjects(new Object[] { pointer, controller, dial, ring, alignment, space }, "Wire QExE Localization");

        controller.pointer = pointer;
        controller.dial = dial;
        controller.headRing = ring;
        controller.alignment = alignment;
        pointer.dial = dial;

        var oscManager = GameObject.Find("OSCManager");
        if (oscManager != null)
        {
            controller.receiver = oscManager.GetComponent<extOSC.OSCReceiver>();
            controller.transmitter = oscManager.GetComponent<extOSC.OSCTransmitter>();
        }

        if (pointer.head == null)
        {
            var cam = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
            if (cam != null) pointer.head = cam.transform;
        }
        if (pointer.pointerOrigin == null) pointer.pointerOrigin = FindByNames(RightControllerNames);
        if (pointer.referenceFrame == null) pointer.referenceFrame = FindRigRoot();
        if (alignment.head == null) alignment.head = pointer.head;
        if (space.head == null) space.head = pointer.head;

        EditorUtility.SetDirty(pointer);
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(dial);
        EditorUtility.SetDirty(ring);
        EditorUtility.SetDirty(alignment);
        EditorUtility.SetDirty(space);

        // Generate the visuals now so they can be seen and tuned in the Scene view.
        dial.Rebuild();
        ring.Rebuild();
        alignment.Rebuild();
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = go;

        AddSceneToBuildSettings(scene.path);
        Validate(true);
    }

    [MenuItem("QExE/Localization/3. Rebuild Visuals", priority = 3)]
    public static void RebuildVisuals()
    {
        var builders = Object.FindObjectsByType<LocalizationVisualBuilder>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var b in builders)
            b.Rebuild();
        Debug.Log("QExE: rebuilt " + builders.Length + " localization visual(s).");
    }

    [MenuItem("QExE/Localization/Apply Neutral Environment", priority = 10)]
    public static void ApplyNeutralEnvironment()
    {
        if (!EditorUtility.DisplayDialog("QExE",
                "Replace the skybox of the open scene with a plain grey background and flat grey ambient light, and turn fog off?\n\n" +
                "You can change the colours afterwards in the Lighting window and on the camera.", "Apply", "Cancel"))
            return;

        var grey = new Color(0.32f, 0.32f, 0.32f, 1f);

        RenderSettings.skybox = null;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f, 1f);
        RenderSettings.fog = false;

        foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RecordObject(cam, "Neutral background");
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = grey;
            EditorUtility.SetDirty(cam);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("QExE: neutral environment applied (grey background, flat ambient, no fog, no skybox).");
    }

    [MenuItem("QExE/Localization/Validate Open Scene", priority = 20)]
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
        var controller = Object.FindFirstObjectByType<LocalizationTrialController>(FindObjectsInactive.Include);
        var pointer = controller != null ? controller.pointer : null;
        var osc = GameObject.Find("OSCManager");

        Check(!string.IsNullOrEmpty(scene.path), "Scene is saved: " + scene.path,
              "Save the scene first (its name must match unityScene_ID in the config, e.g. _localization).");
        Check(scene.name != "_config" && scene.name != "_questionnaire", "Scene is an evaluation scene",
              "Do not add localization to _config or _questionnaire. Use a separate scene.");
        Check(osc != null && osc.GetComponent<extOSC.OSCReceiver>() != null && osc.GetComponent<extOSC.OSCTransmitter>() != null,
              "OSCManager with receiver and transmitter", "Add the OSCManager prefab (Assets/QExE/Prefabs/OSCManager.prefab).");
        Check(GameObject.Find("QExETestInterface") != null, "QExETestInterface found (menu anchor)",
              "No 'QExETestInterface' object. Use the QExE XR Set Up rig prefab, or add an empty object with that name.");
        Check(controller != null, "LocalizationTrialController present", "Run QExE > Localization > 2. Add Localization To Open Scene.");
        Check(pointer != null && pointer.head != null, "Pointer head assigned: " + (pointer != null && pointer.head ? pointer.head.name : ""),
              "Assign LocalizationPointer.head (the main camera).");
        Check(controller != null && controller.dial != null && controller.headRing != null && controller.alignment != null,
              "Dial, head ring and alignment indicator connected",
              "Run QExE > Localization > 2. Add Localization To Open Scene again to add the dial, ring and alignment indicator.");
        Check(pointer != null && pointer.pointerOrigin != null, "Pointer origin assigned: " + (pointer != null && pointer.pointerOrigin ? pointer.pointerOrigin.name : ""),
              "Assign LocalizationPointer.pointerOrigin (the controller the participant points with).");
        Check(AssetDatabase.LoadAssetAtPath<GameObject>(LocalizationInterfacePrefab) != null, "Method interface prefab exists",
              "Run QExE > Localization > 1. Create Method Interface Prefab.");
        Check(EditorBuildSettings.scenes.Any(s => s.path == scene.path && s.enabled), "Scene is in the build settings",
              "Add the scene to File > Build Profiles > Scene List.");

        var space = Object.FindFirstObjectByType<LocalizationSpace>(FindObjectsInactive.Include);
        Check(space != null, "Listening position (LocalizationSpace) present: " + (space != null ? space.name : ""),
              "Run QExE > Localization > 2. Add Localization To Open Scene again to add the listening position.");
        if (space != null)
            report.AppendLine($"NOTE  Listening position: eye point {space.EyePosition}, front yaw {space.transform.eulerAngles.y:F0}°. Move / rotate '{space.name}' to change it.");

        Debug.Log("QExE localization scene check:\n" + report);
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

    private static Transform FindByNames(IEnumerable<string> names)
    {
        var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var name in names)
        {
            var match = all.FirstOrDefault(t => t.name == name);
            if (match != null) return match;
        }
        return null;
    }

    private static Transform FindRigRoot()
    {
        var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        return all.Where(t => RigRootNames.Any(n => t.name.Contains(n)))
                  .OrderBy(Depth)
                  .FirstOrDefault();
    }

    private static int Depth(Transform t)
    {
        int d = 0;
        while (t.parent != null) { t = t.parent; d++; }
        return d;
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
