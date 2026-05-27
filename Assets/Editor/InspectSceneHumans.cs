using UnityEngine;
using UnityEditor;

namespace DebugTools
{
    public class InspectSceneHumans : EditorWindow
    {
        [MenuItem("Tools/Debug/Inspect Scene Humans")]
        public static void Inspect()
        {
            string[] names = { "HumanMale_Character_Free", "HumanMale_Character_Free (1)" };
            string report = "Human Characters in Scene:\n\n";

            foreach (var name in names)
            {
                GameObject go = GameObject.Find(name);
                if (go == null)
                {
                    report += $"{name}: NOT FOUND\n\n";
                    continue;
                }

                report += $"{name}:\n";
                report += $"- Position: {go.transform.position}\n";

                Animator anim = go.GetComponent<Animator>();
                if (anim == null) anim = go.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    report += $"- Animator: Found\n";
                    report += $"- Controller: {(anim.runtimeAnimatorController != null ? anim.runtimeAnimatorController.name : "None")}\n";
                    report += $"- Avatar: {(anim.avatar != null ? anim.avatar.name : "None")}\n";
                }
                else
                {
                    report += $"- Animator: Not Found\n";
                }

                CharacterController cc = go.GetComponent<CharacterController>();
                report += $"- CharacterController: {(cc != null ? "Found" : "Missing")}\n";

                MonoBehaviour[] scripts = go.GetComponents<MonoBehaviour>();
                report += $"- Scripts attached:\n";
                foreach (var s in scripts)
                {
                    if (s != null)
                    {
                        report += $"  * {s.GetType().Name}\n";
                    }
                }
                report += "\n";
            }

            Debug.Log(report);
            EditorUtility.DisplayDialog("Humans Inspection Report", report, "OK");
        }
    }
}
