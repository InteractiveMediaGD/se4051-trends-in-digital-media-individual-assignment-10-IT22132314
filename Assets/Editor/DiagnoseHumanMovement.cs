using UnityEngine;
using UnityEditor;

namespace DebugTools
{
    public class DiagnoseHumanMovement : EditorWindow
    {
        [MenuItem("Tools/Debug/Diagnose Human Movement")]
        public static void Diagnose()
        {
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            string report = "Human Diagnostics Report:\n\n";
            int count = 0;

            foreach (var go in allObjects)
            {
                string cleanName = go.name.Replace(" ", "").ToLower();
                if (cleanName.Contains("humanmale_character_free"))
                {
                    // Verify if it's the root object
                    if (go.transform.parent != null && go.transform.parent.name.Replace(" ", "").ToLower().Contains("humanmale_character_free"))
                    {
                        continue;
                    }

                    count++;
                    report += $"GameObject Name: \"{go.name}\"\n";
                    report += $"- Path: {GetGameObjectPath(go)}\n";
                    report += $"- Active Self: {go.activeSelf}\n";

                    // Character Controller
                    CharacterController cc = go.GetComponent<CharacterController>();
                    if (cc != null)
                    {
                        report += $"- CharacterController: FOUND (Enabled: {cc.enabled}, Height: {cc.height}, Center: {cc.center}, SkinWidth: {cc.skinWidth})\n";
                    }
                    else
                    {
                        report += $"- CharacterController: MISSING\n";
                    }

                    // Animator
                    Animator animRoot = go.GetComponent<Animator>();
                    Animator animChild = go.GetComponentInChildren<Animator>();
                    report += $"- Animator on Root: {(animRoot != null ? "FOUND" : "MISSING")}\n";
                    report += $"- Animator in Children: {(animChild != null ? $"FOUND on child \"{animChild.gameObject.name}\"" : "MISSING")}\n";

                    // AnimalWander Script
                    AnimalWander wander = go.GetComponent<AnimalWander>();
                    if (wander != null)
                    {
                        report += $"- AnimalWander: FOUND (isHostile: {wander.isHostile}, idleAnim: \"{wander.idleAnimation}\", walkAnim: \"{wander.walkAnimation}\", speed: {wander.moveSpeed}, radius: {wander.wanderRadius})\n";
                    }
                    else
                    {
                        report += $"- AnimalWander: MISSING\n";
                    }
                    report += "\n";
                }
            }

            if (count == 0)
            {
                report = "No GameObjects containing 'HumanMale_Character_Free' found in the active scene.";
            }

            Debug.Log(report);
            EditorUtility.DisplayDialog("Human Diagnostics Report", report, "OK");
        }

        private static string GetGameObjectPath(GameObject obj)
        {
            string path = "/" + obj.name;
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                path = "/" + obj.name + path;
            }
            return path;
        }
    }
}
