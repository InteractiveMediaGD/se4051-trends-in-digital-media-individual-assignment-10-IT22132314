using UnityEngine;
using System.Collections;

public class RandomDragonBehavior : MonoBehaviour
{
    Animator anim;

    [Header("Animation Settings")]
    public string idleAnimationName = "Idle";
    public string[] randomActions = { "Roar", "Attack", "LookAround" };

    void Start()
    {
        anim = GetComponent<Animator>();
        StartCoroutine(RandomRoutine());
    }

    IEnumerator RandomRoutine()
    {
        while (true)
        {
            // 1. Wait a random amount of time (between 3 to 8 seconds)
            yield return new WaitForSeconds(Random.Range(3f, 8f));

            // 2. Pick a random action from the list and play it
            if (randomActions.Length > 0)
            {
                int randomIndex = Random.Range(0, randomActions.Length);
                anim.CrossFade(randomActions[randomIndex], 0.2f);
            }

            // 3. Wait 2.5 seconds for the action to finish playing
            yield return new WaitForSeconds(2.5f);

            // 4. Return to the normal Idle animation
            anim.CrossFade(idleAnimationName, 0.2f);
        }
    }
}
