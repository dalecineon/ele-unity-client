using UnityEngine;
using UnityEditor;
using Cineon.ELE.Networking;
using Cineon.ELE.Editor;

[CustomEditor(typeof(EyeDataDistributor))]
public class EyeDataDistributorEditor : ELEMonoBehaviourEditor
{
    public override void OnInspectorGUI()
    {
        DrawBanner();

        serializedObject.Update();
        SerializedProperty serverTypeProp = serializedObject.FindProperty("serverType");
        SerializedProperty customURLProp = serializedObject.FindProperty("customURL");
        
        DrawPropertiesExcluding(serializedObject,"customURL");

        if ((EyeDataDistributor.ServerType)serverTypeProp.intValue == EyeDataDistributor.ServerType.customURL)
        {
            EditorGUILayout.PropertyField(customURLProp);
            EditorGUILayout.HelpBox("This is only to be used if you have been given a specific URL.", MessageType.Info);
        }
        serializedObject.ApplyModifiedProperties();
    }
}