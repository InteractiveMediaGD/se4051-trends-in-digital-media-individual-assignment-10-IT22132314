using UnityEngine;
using UnityEditor;

namespace CharacterTools
{
    public class FixDogAvatar : EditorWindow
    {
        [MenuItem("Tools/Character/Fix Dog Avatar")]
        public static void FixAvatar()
        {
            string fbxPath = "Assets/RSG_DogsPack/HDRP/Models/SK_GermanShepherd_01.fbx";
            ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"Could not find Dog FBX at path: {fbxPath}");
                EditorUtility.DisplayDialog("Error", $"Could not find Dog FBX at path:\n{fbxPath}", "OK");
                return;
            }

            // Set Avatar Setup to Create From This Model
            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.SaveAndReimport();
                Debug.Log("Dog FBX Rig import settings updated to CreateFromThisModel.");
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
                Debug.LogError("Failed to find or generate Avatar from the Dog FBX.");
                EditorUtility.DisplayDialog("Error", "Rig settings updated, but could not retrieve the Avatar sub-asset.", "OK");
                return;
            }

            Debug.Log($"Found Dog Avatar: {avatar.name}");

            // Load the Animator Controller
            string controllerPath = "Assets/RSG_DogsPack/HDRP/Animations/AC_Dogs_Type_01.controller";
            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            if (controller == null)
            {
                Debug.LogWarning($"Could not find Animator Controller at: {controllerPath}");
            }

            // Assign to the Prefabs
            string[] prefabPaths = {
                "Assets/RSG_DogsPack/HDRP/Prefabs/P_GermanShepherd.prefab"
            };

            foreach (var path in prefabPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    Animator[] animators = prefab.GetComponentsInChildren<Animator>(true);
                    foreach (var animator in animators)
                    {
                        Undo.RegisterCompleteObjectUndo(animator, "Assign Dog Avatar & Controller");
                        animator.avatar = avatar;
                        if (controller != null)
                        {
                            animator.runtimeAnimatorController = controller;
                        }
                        
                        // Auto-add DogWander component if not present
                        if (animator.gameObject.GetComponent<DogWander>() == null)
                        {
                            animator.gameObject.AddComponent<DogWander>();
                        }

                        EditorUtility.SetDirty(animator);
                        EditorUtility.SetDirty(animator.gameObject);
                    }
                    Debug.Log($"Assigned Avatar, Controller and Wander script to Prefab: {path}");
                }
            }

            // Assign to active instances in the current scene
            Animator[] sceneAnimators = FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int sceneCount = 0;
            foreach (var animator in sceneAnimators)
            {
                if (animator.gameObject.name.Contains("GermanShepherd") || animator.gameObject.name.Contains("Dog") || animator.gameObject.name.Contains("P_GermanShepherd"))
                {
                    Undo.RegisterCompleteObjectUndo(animator, "Assign Dog Avatar & Controller");
                    animator.avatar = avatar;
                    if (controller != null)
                    {
                        animator.runtimeAnimatorController = controller;
                    }
                    
                    // Auto-add DogWander component if not present
                    if (animator.gameObject.GetComponent<DogWander>() == null)
                    {
                        animator.gameObject.AddComponent<DogWander>();
                    }

                    EditorUtility.SetDirty(animator);
                    EditorUtility.SetDirty(animator.gameObject);
                    sceneCount++;
                }
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Success", $"Dog Avatar and Controller fixed successfully!\n\n- Updated Rig Settings on Dog FBX\n- Assigned Avatar & Controller to Prefab\n- Assigned Avatar & Controller to {sceneCount} instances in the scene.", "OK");
        }
    }
}
