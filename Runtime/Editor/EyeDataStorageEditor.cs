using System;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace Cineon.ELE.Editor
{
    /// <summary>
    /// Single custom editor that automatically applies the ELE banner to any script
    /// inheriting from ELEMonoBehaviour. No per-script editor classes needed.
    /// </summary>
    [CustomEditor(typeof(ELEMonoBehaviour), editorForChildClasses: true)]
    public class ELEMonoBehaviourEditor : UnityEditor.Editor
    {
        private Texture2D bannerTexture;
        private string versionText;

        protected virtual void OnEnable()
        {
            bannerTexture = Resources.Load<Texture2D>("ELELogo");
            try
            {
                var packageJsonPath = Path.Combine(Application.dataPath, "ThirdParty/ele-unity-client/package.json");
                if (File.Exists(packageJsonPath))
                {
                    var json = File.ReadAllText(packageJsonPath);
                    const string versionKey = "\"version\"";
                    var index = json.IndexOf(versionKey);
                    if (index >= 0)
                    {
                        var afterKey = json.IndexOf(':', index);
                        if (afterKey > 0)
                        {
                            var quoteStart = json.IndexOf('"', afterKey + 1);
                            var quoteEnd = json.IndexOf('"', quoteStart + 1);
                            if (quoteStart > 0 && quoteEnd > quoteStart)
                            {
                                var version = json.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
                                versionText = $"v{version}";
                            }
                        }
                    }
                }
            }
            catch
            {
                versionText = null;
            }
        }

        protected void DrawBanner()
        {
            GUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(6);

            if (bannerTexture != null)
            {
                float aspect = (float)bannerTexture.width / bannerTexture.height;
                float height = 42f;
                float width = height * aspect;
                Rect rect = GUILayoutUtility.GetRect(width, height, GUILayout.ExpandWidth(false));
                GUI.DrawTexture(rect, bannerTexture, ScaleMode.ScaleToFit);
                GUILayout.Space(8);
                EditorGUILayout.BeginVertical(GUILayout.Height(height));
                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Cineon ELE Package", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (!string.IsNullOrEmpty(versionText))
                {
                    EditorGUILayout.LabelField(versionText, GUILayout.Width(70));
                }
                EditorGUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
            }
            GUILayout.FlexibleSpace();
            GUILayout.Space(6);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(6);
        }

        public override void OnInspectorGUI()
        {
            DrawBanner();
            DrawDefaultInspector();
        }
    }
}
#endif