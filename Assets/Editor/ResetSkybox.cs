using UnityEngine;
using UnityEditor;

namespace EnvironmentTools
{
    public class ResetSkybox : EditorWindow
    {
        [MenuItem("Tools/Environment/Reset Skybox to Default")]
        public static void ResetSky()
        {
            // Load the default Unity skybox material
            Material defaultSkybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
            
            if (defaultSkybox != null)
            {
                Undo.RecordObject(RenderSettings.skybox, "Reset Skybox");
                RenderSettings.skybox = defaultSkybox;
                
                // Update lightmaps and ambient lighting to match the default sky
                DynamicGI.UpdateEnvironment();
                
                Debug.Log("Skybox reset to default.");
                EditorUtility.DisplayDialog("Success", "Skybox has been reset to the default Unity sky!", "OK");
            }
            else
            {
                Debug.LogError("Could not locate default Unity skybox material.");
                EditorUtility.DisplayDialog("Error", "Could not locate default Unity skybox material.", "OK");
            }
        }
    }
}
