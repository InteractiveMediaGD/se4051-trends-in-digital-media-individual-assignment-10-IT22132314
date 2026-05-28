using UnityEngine;

public class CharacterHealth : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Animations")]
    public string hitAnimation = "GetHit";
    public string deathAnimation = "Death";
    public float deathDestroyDelay = 5f;

    public bool isPlayer = false;

    private Animator animator;
    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponentInChildren<Animator>();

        // Auto-detect if this is the player
        if (gameObject.CompareTag("Player") || gameObject.name == "Main_Player")
        {
            isPlayer = true;
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log(gameObject.name + " took " + damage + " damage. Current Health: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Play hit animation
            if (animator != null && !string.IsNullOrEmpty(hitAnimation))
            {
                animator.CrossFade(hitAnimation, 0.1f);
            }
        }
    }

    private void Die()
    {
        isDead = true;
        Debug.Log(gameObject.name + " has died.");

        if (animator != null && !string.IsNullOrEmpty(deathAnimation))
        {
            animator.CrossFade(deathAnimation, 0.1f);
        }

        // Disable movement scripts
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            if (script != this)
            {
                script.enabled = false;
            }
        }

        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (deathDestroyDelay > 0 && !isPlayer)
        {
            Destroy(gameObject, deathDestroyDelay);
        }
    }

    public bool IsDead()
    {
        return isDead;
    }

    // Visual health bar HUD for the Player
    void OnGUI()
    {
        if (isPlayer)
        {
            // Custom styling for a beautiful modern HUD bar
            float width = 250;
            float height = 30;
            float x = 20;
            float y = 20;

            // Draw shadow/background container
            GUI.Box(new Rect(x, y, width, height), "");

            // Calculate fill width
            float pct = Mathf.Clamp01((float)currentHealth / maxHealth);
            
            // Draw health fill color
            GUI.color = Color.red;
            GUI.Box(new Rect(x + 3, y + 3, (width - 6) * pct, height - 6), "");

            // Reset GUI color for text styling
            GUI.color = Color.white;
            GUIStyle style = new GUIStyle();
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = Color.white;
            style.fontSize = 14;
            style.fontStyle = FontStyle.Bold;

            GUI.Label(new Rect(x, y, width, height), "PLAYER HP: " + currentHealth + " / " + maxHealth, style);
        }
    }
}
