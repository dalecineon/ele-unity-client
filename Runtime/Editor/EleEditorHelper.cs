using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class EleEditorHelper
{
    private const string IconSetKey = "EleEditorHelper_IconsSet";

    /// <summary>
    /// Static constructor to set custom icons for prefabs when the editor loads. This ensures that the icons are set only once per session, improving performance.
    /// </summary>
    static EleEditorHelper()
    {
        if (!SessionState.GetBool(IconSetKey, false))
        {
            EditorApplication.delayCall += SetPrefabIcons;
        }
    }

    /// <summary> 
    /// Sets a custom icon for all prefabs in the specified directory.
    /// </summary>
    static void SetPrefabIcons()
    {
        Texture2D icon = Resources.Load<Texture2D>("ELELogo");
        if (icon == null)
        {
            Debug.LogWarning("[EleEditorHelper] Could not load ELELogo from Resources.");
            return;
        }
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[]
        {
            "Assets/ThirdParty/ele-unity-client/ele-unity-client/Runtime/Prefabs"
        });
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                EditorGUIUtility.SetIconForObject(prefab, icon);
                EditorUtility.SetDirty(prefab);
            }
        }
        AssetDatabase.SaveAssets();
        SessionState.SetBool(IconSetKey, true);
    }
}
