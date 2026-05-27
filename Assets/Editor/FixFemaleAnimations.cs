using UnityEngine;
using UnityEditor;

namespace CharacterTools
{
    public class FixFemaleAnimations : EditorWindow
    {
        [MenuItem("Tools/Character/Fix A03 Animations")]
        public static void FixAnimations()
        {
            string fbxPath = "Assets/AllStarCharacterLibrary/Models/Characters/A03.FBX";
            string baseFemalePath = "Assets/AllStarCharacterLibrary/Models/RootAnimsFemale/BaseFemale.fbx";
            
            // Load the generated Avatar
            Avatar avatar = LoadAvatarFromPath(fbxPath);
            if (avatar == null)
            {
                // Fallback to BaseFemale which A03 copies its rig avatar from
                avatar = LoadAvatarFromPath(baseFemalePath);
            }

            if (avatar == null)
            {
                Debug.LogError("Failed to find Avatar for A03.");
                EditorUtility.DisplayDialog("Error", "Could not locate the Avatar sub-asset in A03.FBX or BaseFemale.fbx.", "OK");
                return;
            }

            // Load the FemalePlayer Animator Controller
            string controllerPath = "Assets/AllStarCharacterLibrary/Models/RootAnimsFemale/FemalePlayer.controller";
            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            if (controller == null)
            {
                Debug.LogError($"Could not find Animator Controller at: {controllerPath}");
                EditorUtility.DisplayDialog("Error", $"Could not find Animator Controller at:\n{controllerPath}", "OK");
                return;
            }

            // Find A03 in the active scene
            GameObject sceneObj = GameObject.Find("A03");
            if (sceneObj == null)
            {
                // Fallback: try finding any object containing A03 in its name
                GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var go in allObjects)
                {
                    if (go.name == "A03" && go.transform.parent == null)
                    {
                        sceneObj = go;
                        break;
                    }
                }
            }

            if (sceneObj == null)
            {
                Debug.LogWarning("Could not find root GameObject 'A03' in the active scene.");
            }
            else
            {
                Animator animator = sceneObj.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = sceneObj.AddComponent<Animator>();
                }

                Undo.RegisterCompleteObjectUndo(animator, "Configure A03 Animator");
                animator.avatar = avatar;
                animator.runtimeAnimatorController = controller;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                EditorUtility.SetDirty(animator);
                Debug.Log("Configured Animator on active A03 scene object.");
            }

            // Also search all prefabs or other scene instances of A03
            Animator[] sceneAnimators = FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int sceneCount = 0;
            foreach (var anim in sceneAnimators)
            {
                if (anim.gameObject.name == "A03" || anim.gameObject.name.Contains("A03"))
                {
                    // Check if it's the root or has children matching A03 structure
                    if (anim.transform.Find("A03_LOD") != null || anim.transform.Find("A03") != null)
                    {
                        Undo.RegisterCompleteObjectUndo(anim, "Configure A03 Animator");
                        anim.avatar = avatar;
                        anim.runtimeAnimatorController = controller;
                        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                        EditorUtility.SetDirty(anim);
                        sceneCount++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Success", $"A03 Animations Applied!\n\n- Assigned Avatar: {avatar.name}\n- Assigned Controller: {controller.name}\n- Updated {sceneCount} instances in the active scene.", "OK");
        }

        private static Avatar LoadAvatarFromPath(string path)
        {
            Avatar avatar = AssetDatabase.LoadAssetAtPath<Avatar>(path);
            if (avatar == null)
            {
                object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var asset in subAssets)
                {
                    if (asset is Avatar)
                    {
                        return (Avatar)asset;
                    }
                }
            }
            return avatar;
        }
    }
}
