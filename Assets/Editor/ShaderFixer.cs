using UnityEngine;
using UnityEditor;

namespace ShaderTools
{
    public class ShaderFixer : EditorWindow
    {
        [MenuItem("Tools/Materials/Fix Character Shaders")]
        public static void FixShaders()
        {
            // Find the standard URP Lit shader
            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLitShader == null)
            {
                // Fallback to Simple Lit if Lit is not active
                urpLitShader = Shader.Find("Universal Render Pipeline/Simple Lit");
            }
            if (urpLitShader == null)
            {
                urpLitShader = Shader.Find("Standard");
            }

            if (urpLitShader == null)
            {
                Debug.LogError("Could not find any suitable Lit shader!");
                EditorUtility.DisplayDialog("Error", "Could not find URP Lit, Simple Lit, or Standard shader in the project.", "OK");
                return;
            }

            Debug.Log($"Using shader: {urpLitShader.name}");

            // Find all materials in the AllStarCharacterLibrary folder
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/AllStarCharacterLibrary" });
            int fixedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null)
                {
                    Shader currentShader = mat.shader;
                    
                    // If the shader is missing, broken (pink), or legacy built-in standard
                    bool isBroken = currentShader == null || currentShader.name.Contains("InternalErrorShader") || currentShader.name == "";
                    bool isLegacy = currentShader != null && (
                        currentShader.name == "Standard" || 
                        currentShader.name.StartsWith("Legacy Shaders/") || 
                        currentShader.name.StartsWith("Mobile/") ||
                        currentShader.name.Contains("Self-Illumin") ||
                        currentShader.name.Contains("Bumped Diffuse")
                    );

                    if (isBroken || isLegacy)
                    {
                        Undo.RegisterCompleteObjectUndo(mat, "Fix Shader");
                        
                        // Capture existing textures
                        Texture mainTex = mat.mainTexture;
                        Texture bumpMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
                        Color mainColor = mat.HasProperty("_Color") ? mat.color : Color.white;

                        // Assign URP Lit
                        mat.shader = urpLitShader;

                        // Re-assign captured properties to URP naming conventions
                        if (mainTex != null)
                        {
                            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", mainTex);
                            else if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", mainTex);
                        }
                        if (bumpMap != null)
                        {
                            if (mat.HasProperty("_BumpMap")) mat.SetTexture("_BumpMap", bumpMap);
                            mat.EnableKeyword("_NORMALMAP");
                        }
                        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", mainColor);
                        else if (mat.HasProperty("_Color")) mat.SetColor("_Color", mainColor);

                        EditorUtility.SetDirty(mat);
                        fixedCount++;
                        Debug.Log($"Fixed material: {mat.name} (Changed shader from '{currentShader?.name}' to '{urpLitShader.name}')");
                    }
                }
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Success", $"Fixed {fixedCount} materials in AllStarCharacterLibrary!", "OK");
        }
    }
}
