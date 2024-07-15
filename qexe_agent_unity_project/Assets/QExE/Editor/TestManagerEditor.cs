using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TestManager))]
public class TestManagerEditor : UnityEditor.Editor
{
    Texture2D image;

    void Awake()
	{
        image = Resources.Load("QExE_client", typeof(Texture2D)) as Texture2D;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        TestManager manager = (TestManager)target;

        var rect = GUILayoutUtility.GetRect(0, 0);
        var width = image.width * 0.7f;
        var height = image.height * 0.7f;

        rect.x = rect.width * 0.5f - width * 0.5f;
        rect.y = rect.y + rect.height * height * 0.5f;
        rect.width = width;
        rect.height = height;

        GUI.DrawTexture(rect, image, ScaleMode.ScaleToFit, true, 0.0f);

        EditorGUILayout.Space(100);

        Color _default = GUI.color;


        using (new GUILayout.VerticalScope(new GUIStyle("box")))
        {
            EditorGUILayout.LabelField("Scene Objects for OSC Communication", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox("Objects will be automatically found, if they are placed within the scene and labelled as follows:", MessageType.None);

            using (new GUILayout.VerticalScope(new GUIStyle("box")))
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("'OSCManager'");
                if (Application.isPlaying)
                {
                    GUI.color = (manager.SceneOSCManagerFound) ? Color.green : Color.yellow;
                }
                GUILayout.Button(Application.isPlaying ? (manager.SceneOSCManagerFound) ? "Found" : "Not Found!" : "Player inactive.");
                GUI.color = _default;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("'btn/Next'");
                if (Application.isPlaying) {
                    GUI.color = (manager.InterfaceNextButtonFound) ? Color.green : Color.yellow;
                } 
                GUILayout.Button(Application.isPlaying ? (manager.InterfaceNextButtonFound) ? "Found" : "Not Found!" : "Player inactive.");
                GUI.color = _default;               
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("'btn/StartPlayback'");
                if (Application.isPlaying)
                {
                    GUI.color = (manager.InterfacePlayButtonFound) ? Color.green : Color.yellow;
                }
                GUILayout.Button(Application.isPlaying ? (manager.InterfacePlayButtonFound) ? "Found" : "Not Found!" : "Player inactive.");
                GUI.color = _default;
                GUILayout.EndHorizontal();
            }


            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Current Item", EditorStyles.boldLabel);
            using (new GUILayout.VerticalScope(new GUIStyle("box")))
            {
                EditorGUILayout.LabelField("Scene ID", manager.ThisSceneID);
                EditorGUILayout.LabelField("Path/To/Video", manager.ThisSceneVideoPath);
                EditorGUILayout.LabelField("Video File ID", manager.ThisSceneVideoID.ToString());
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Next Item", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox("Which Unity scene to load next, once the user presses the 'btn/Next' button. This only considers 'evaluation' items, and therefore does not consider the questionnaire scene.", MessageType.None);

            using (new GUILayout.VerticalScope(new GUIStyle("box")))
            {
                EditorGUILayout.LabelField("Scene ID", manager.NextSceneID);
                EditorGUILayout.LabelField("Path/To/Video", manager.NextVideoPath);
                EditorGUILayout.LabelField("Video File ID", manager.NextVideoID.ToString());
            }


            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Questionnaire", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox("Shows which questionnaire shall be used (if any), and its itegration into the test. Ensure the correct questionnaire interface prefab has been included in the available <QuestionnaireInterfaces> list. ", MessageType.None);

            using (new GUILayout.VerticalScope(new GUIStyle("box")))
            {
                EditorGUILayout.LabelField("Set Questionnaire", manager.ChosenQuestionnaire);
                EditorGUILayout.LabelField("Test Integration", manager.QuestionnaireIntegration);


                SerializedProperty questionnaires = serializedObject.FindProperty("QuestionnaireInterfaces");
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(questionnaires, true);
                if (EditorGUI.EndChangeCheck())
                    serializedObject.ApplyModifiedProperties();
            }


            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Methodology", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox("Shows which evaluation method shall be used, and the number of parallel audio conditions. Ensure the correct methodology interface prefab has been included in the available <MethodologyInterfaces> list. ", MessageType.None);

            using (new GUILayout.VerticalScope(new GUIStyle("box")))
            {
                EditorGUILayout.LabelField("Set Methodology", manager.ChosenMethodology);
                EditorGUILayout.LabelField("# Audio Conditions", manager.NumberOfConditions.ToString());

                SerializedProperty methodologies = serializedObject.FindProperty("MethodologyInterfaces");
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(methodologies, true);
                if (EditorGUI.EndChangeCheck())
                    serializedObject.ApplyModifiedProperties();
            }
        }
    }

    protected void InGameControls()
    {

    }
}
