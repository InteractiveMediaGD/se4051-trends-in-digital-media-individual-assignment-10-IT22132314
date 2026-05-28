using UnityEngine;

namespace CharacterTools
{
    public static class FixHumansRuntime
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int count = 0;
            
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

                    // 1. Ensure CharacterController is added, enabled, and correctly sized
                    CharacterController cc = go.GetComponent<CharacterController>();
                    if (cc == null)
                    {
                        cc = go.AddComponent<CharacterController>();
                    }
                    cc.height = 1.8f;
                    cc.radius = 0.3f;
                    cc.center = new Vector3(0f, 0.9f, 0f);
                    cc.skinWidth = 0.02f;
                    cc.minMoveDistance = 0f;
                    cc.enabled = true;

                    // 2. Ensure Animator is configured
                    Animator anim = go.GetComponent<Animator>();
                    if (anim == null) anim = go.GetComponentInChildren<Animator>();
                    if (anim != null)
                    {
                        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                        anim.enabled = true;
                    }

                    // 3. Ensure AnimalWander script is attached and set up
                    AnimalWander wander = go.GetComponent<AnimalWander>();
                    if (wander == null)
                    {
                        wander = go.AddComponent<AnimalWander>();
                    }
                    wander.enabled = true;
                    wander.isHostile = false;
                    wander.idleAnimation = "Idle";
                    wander.walkAnimation = "WalkForward";
                    wander.moveSpeed = 1.2f;
                    wander.wanderRadius = 12f;

                    // 4. Disable any conflicting asset store demo controllers that might pin them in place
                    MonoBehaviour[] scripts = go.GetComponents<MonoBehaviour>();
                    foreach (var script in scripts)
                    {
                        if (script == null) continue;
                        string scriptName = script.GetType().Name.ToLower();
                        
                        // Disable if it's a demo/custom movement script, but keep AnimalWander
                        if (scriptName != "animalwander" && 
                            (scriptName.Contains("controller") || 
                             scriptName.Contains("demo") || 
                             scriptName.Contains("move") || 
                             scriptName.Contains("test")))
                        {
                            script.enabled = false;
                            Debug.Log($"[RuntimeFix] Disabled conflicting script: {script.GetType().Name} on {go.name}");
                        }
                    }

                    count++;
                    Debug.Log($"[RuntimeFix] Successfully initialized and started wander behavior for: {go.name}");
                }
            }
            Debug.Log($"[RuntimeFix] Completed configuration for {count} human characters.");
        }
    }
}
