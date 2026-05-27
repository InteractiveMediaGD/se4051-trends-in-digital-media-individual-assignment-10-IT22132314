using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace CharacterTools
{
    public class SkinnedMeshBinder : EditorWindow
    {
        private GameObject character;
        private GameObject accessory;

        [MenuItem("Tools/Character/Bind Skinned Mesh")]
        public static void ShowWindow()
        {
            GetWindow<SkinnedMeshBinder>("Bind Skinned Mesh");
        }

        private void OnGUI()
        {
            GUILayout.Label("Bind Skinned Mesh to Character Armature", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            character = (GameObject)EditorGUILayout.ObjectField("Character GameObject", character, typeof(GameObject), true);
            accessory = (GameObject)EditorGUILayout.ObjectField("Accessory (TopKnot, Hair, etc.)", accessory, typeof(GameObject), true);

            EditorGUILayout.Space();

            if (GUILayout.Button("Bind Accessory"))
            {
                if (character == null || accessory == null)
                {
                    EditorUtility.DisplayDialog("Error", "Please assign both the character and the accessory.", "OK");
                    return;
                }

                Bind(character, accessory);
            }
        }

        private void Bind(GameObject targetChar, GameObject sourceAccessory)
        {
            // Find all transforms in the character's hierarchy (armature bones)
            Transform[] targetBones = targetChar.GetComponentsInChildren<Transform>(true);
            Dictionary<string, Transform> boneMap = new Dictionary<string, Transform>();
            foreach (var bone in targetBones)
            {
                if (!boneMap.ContainsKey(bone.name))
                {
                    boneMap.Add(bone.name, bone);
                }
            }

            // Get the SkinnedMeshRenderer on the accessory
            SkinnedMeshRenderer accessoryRenderer = sourceAccessory.GetComponentInChildren<SkinnedMeshRenderer>();
            if (accessoryRenderer == null)
            {
                EditorUtility.DisplayDialog("Error", "The accessory does not have a SkinnedMeshRenderer component.", "OK");
                return;
            }

            Undo.RegisterCompleteObjectUndo(sourceAccessory, "Bind Skinned Mesh");
            if (accessoryRenderer.gameObject != sourceAccessory)
            {
                Undo.RegisterCompleteObjectUndo(accessoryRenderer.gameObject, "Bind Skinned Mesh");
            }

            // Parent the accessory to the character root
            sourceAccessory.transform.SetParent(targetChar.transform);
            sourceAccessory.transform.localPosition = Vector3.zero;
            sourceAccessory.transform.localRotation = Quaternion.identity;
            sourceAccessory.transform.localScale = Vector3.one;

            // Map the bones
            Transform[] sourceBones = accessoryRenderer.bones;
            Transform[] newBones = new Transform[sourceBones.Length];
            bool missingBones = false;

            for (int i = 0; i < sourceBones.Length; i++)
            {
                if (sourceBones[i] == null) continue;

                string boneName = sourceBones[i].name;
                if (boneMap.TryGetValue(boneName, out Transform targetBone))
                {
                    newBones[i] = targetBone;
                }
                else
                {
                    Debug.LogWarning($"Could not find bone '{boneName}' in character hierarchy.");
                    missingBones = true;
                }
            }

            accessoryRenderer.bones = newBones;

            // Set Root Bone
            if (accessoryRenderer.rootBone != null)
            {
                string rootBoneName = accessoryRenderer.rootBone.name;
                if (boneMap.TryGetValue(rootBoneName, out Transform targetRootBone))
                {
                    accessoryRenderer.rootBone = targetRootBone;
                }
            }

            EditorUtility.SetDirty(accessoryRenderer);
            
            if (missingBones)
            {
                EditorUtility.DisplayDialog("Binding Complete with Warnings", "Accessory was bound, but some bones were not found in the character armature. Check the console for details.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Success", "Accessory successfully bound to character armature!", "OK");
            }
        }
    }
}
