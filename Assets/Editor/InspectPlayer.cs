using UnityEngine;
using UnityEditor;

namespace DebugTools
{
    public class InspectPlayer : EditorWindow
    {
        [MenuItem("Tools/Debug/Inspect Player")]
        public static void Inspect()
        {
            GameObject player = GameObject.Find("Main_Player");
            if (player == null)
            {
                player = GameObject.FindWithTag("Player");
            }

            if (player == null)
            {
                Debug.LogError("Could not find Main_Player in the scene.");
                EditorUtility.DisplayDialog("Error", "Could not find Main_Player in the active scene.", "OK");
                return;
            }

            string report = $"Player GameObject: {player.name}\n";
            report += $"Position: {player.transform.position}\n";

            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                report += $"\nCharacterController:\n";
                report += $"- Center: {cc.center}\n";
                report += $"- Height: {cc.height}\n";
                report += $"- Radius: {cc.radius}\n";
                report += $"- Skin Width: {cc.skinWidth}\n";
                report += $"- Min Move Distance: {cc.minMoveDistance}\n";
                report += $"- Step Offset: {cc.stepOffset}\n";
            }
            else
            {
                report += "\nNo CharacterController component found on the root Player GameObject!\n";
            }

            // Check children positions relative to parent
            report += "\nChild Transforms and Offsets:\n";
            foreach (Transform child in player.transform)
            {
                report += $"- {child.name}: Local Position: {child.localPosition}, Local Rotation: {child.localRotation.eulerAngles}, Local Scale: {child.localScale}\n";
            }

            Debug.Log(report);
            EditorUtility.DisplayDialog("Player Inspection Report", report, "OK");
        }
    }
}
