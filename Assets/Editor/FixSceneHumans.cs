using UnityEngine;
using UnityEditor;

namespace CharacterTools
{
    public class FixSceneHumans : EditorWindow
    {
        [MenuItem("Tools/Character/Fix Humans Wander")]
        public static void FixHumans()
        {
            int fixedCount = 0;

            // Find all GameObjects in the active scene matching variations of the name
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in allObjects)
            {
                string cleanName = go.name.Replace(" ", "").ToLower();
                if (cleanName.Contains("humanmale_character_free"))
                {
                    // Ensure we target the root character GameObject, not nested bones/meshes
                    if (go.transform.parent != null && go.transform.parent.name.Replace(" ", "").ToLower().Contains("humanmale_character_free"))
                    {
                        continue;
                    }

                // 1. Ensure CharacterController is added and correctly sized
                CharacterController cc = go.GetComponent<CharacterController>();
                if (cc == null)
                {
                    cc = go.AddComponent<CharacterController>();
                }
                
                Undo.RegisterCompleteObjectUndo(cc, "Configure Human Controller");
                cc.height = 1.8f;
                cc.radius = 0.3f;
                cc.center = new Vector3(0f, 0.9f, 0f);
                cc.skinWidth = 0.02f;
                cc.minMoveDistance = 0f;

                // 2. Ensure Animator is configured
                Animator anim = go.GetComponent<Animator>();
                if (anim == null) anim = go.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    Undo.RegisterCompleteObjectUndo(anim, "Configure Human Animator");
                    anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    EditorUtility.SetDirty(anim);
                }

                // 3. Ensure AnimalWander script is attached and set up
                AnimalWander wander = go.GetComponent<AnimalWander>();
                if (wander == null)
                {
                    wander = go.AddComponent<AnimalWander>();
                }

                Undo.RegisterCompleteObjectUndo(wander, "Configure Human Wander");
                wander.isHostile = false;
                wander.idleAnimation = "Idle";
                wander.walkAnimation = "WalkForward";
                wander.moveSpeed = 1.2f;
                wander.wanderRadius = 12f;

                EditorUtility.SetDirty(go);
                fixedCount++;
                Debug.Log($"Successfully configured {go.name} to wander using AnimalWander script.");
                }
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Success", $"Configured {fixedCount} human characters to wander around their areas!", "OK");
        }
    }
}
