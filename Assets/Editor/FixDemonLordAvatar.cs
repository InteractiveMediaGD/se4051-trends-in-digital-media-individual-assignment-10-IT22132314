using UnityEngine;
using UnityEditor;

namespace CharacterTools
{
    public class FixDemonLordAvatar : EditorWindow
    {
        [MenuItem("Tools/Character/Fix Demon Lord Avatar")]
        public static void FixAvatar()
        {
            string fbxPath = "Assets/DemonLord2/Base_Mesh/BM_DemonLord2.fbx";
            ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"Could not find FBX at path: {fbxPath}");
                EditorUtility.DisplayDialog("Error", $"Could not find FBX at path:\n{fbxPath}", "OK");
                return;
            }

            // Set Avatar Setup to Create From This Model
            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.SaveAndReimport();
                Debug.Log("Demon Lord FBX Rig import settings updated to CreateFromThisModel.");
            }

            // Load the generated Avatar
            Avatar avatar = AssetDatabase.LoadAssetAtPath<Avatar>(fbxPath);
            if (avatar == null)
            {
                // Try finding it among sub-assets
                object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
                foreach (var asset in subAssets)
                {
                    if (asset is Avatar)
                    {
                        avatar = (Avatar)asset;
                        break;
                    }
                }
            }

            if (avatar == null)
            {
                Debug.LogError("Failed to find or generate Avatar from the FBX.");
                EditorUtility.DisplayDialog("Error", "Rig settings updated, but could not retrieve the Avatar sub-asset.", "OK");
                return;
            }

            Debug.Log($"Found Avatar: {avatar.name}");

            // Assign Avatar to the Prefabs
            string[] prefabPaths = {
                "Assets/DemonLord2/Prefab/BM_DemonLord2.prefab",
                "Assets/DemonLord2/Prefab/BM_DemonLord2 (1).prefab"
            };

            foreach (var path in prefabPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    Animator[] animators = prefab.GetComponentsInChildren<Animator>(true);
                    foreach (var animator in animators)
                    {
                        Undo.RegisterCompleteObjectUndo(animator, "Assign Avatar");
                        animator.avatar = avatar;
                        EditorUtility.SetDirty(animator);
                    }
                    Debug.Log($"Assigned Avatar to Prefab: {path}");
                }
            }

            // Assign Avatar to active instances in the current scene
            Animator[] sceneAnimators = FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int sceneCount = 0;
            foreach (var animator in sceneAnimators)
            {
                if (animator.gameObject.name.Contains("DemonLord") || animator.gameObject.name.Contains("BM_DemonLord2"))
                {
                    Undo.RegisterCompleteObjectUndo(animator, "Assign Avatar");
                    animator.avatar = avatar;
                    EditorUtility.SetDirty(animator);
                    sceneCount++;
                }
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Success", $"Demon Lord Avatar fixed successfully!\n\n- Updated Rig Settings on FBX\n- Assigned Avatar to Prefabs\n- Assigned Avatar to {sceneCount} instances in the scene.", "OK");
        }
    }
}
