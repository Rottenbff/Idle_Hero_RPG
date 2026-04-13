using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Slider = UnityEngine.UI.Slider;

public class EnemyBehaviour : MonoBehaviour
{
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private TextMeshProUGUI textMeshPro;
    [SerializeField] private GameObject textPrefab;
    [SerializeField] private Slider _enemyHealthBar;
    [SerializeField] private Animator enemyAnimator;

    private int currentHealth;
    private bool isDead = false;
    private CombatSystem combatSystem;

    // These two flags are SET by Animation Events on the attack clip.
    private bool attackHitFrame = false;
    private bool attackFinished = false;

    public bool IsDead => isDead;
    public int CurrentHealth => currentHealth;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    void Start()
    {
        combatSystem = FindObjectOfType<CombatSystem>();
        if (combatSystem == null)
        {
            Debug.LogError("[EnemyBehaviour] CombatSystem not found!");
            return;
        }

        if (enemyAnimator != null)
        {
            enemyAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        currentHealth = enemyData.GetStartingHealth();
        _enemyHealthBar.maxValue = currentHealth;
        _enemyHealthBar.value = currentHealth;
        textMeshPro.text = $"{enemyData.Name} HP: {currentHealth}";

        combatSystem.RegisterEnemy(this);
    }

    // -------------------------------------------------------------------------
    // Animation Events — add these two events to your attack animation clip:
    //
    //   1. At the "hit" frame (when weapon connects):
    //      Function: OnAttackHit
    //
    //   2. At the very last frame of the clip:
    //      Function: OnAttackEnd
    //
    // To add them: select the animation clip → open Animation window →
    // drag the playhead to the right frame → click "Add Event" → type the function name.
    // -------------------------------------------------------------------------

    // Called by Animation Event at the hit frame
    [UnityEngine.Scripting.Preserve]
    public void OnAttackHit()
    {
        attackHitFrame = true;
    }

    // Called by Animation Event at the last frame
    [UnityEngine.Scripting.Preserve]
    public void OnAttackEnd()
    {
        attackFinished = true;
    }

    // -------------------------------------------------------------------------
    // Called by CombatSystem during Enemy Phase
    // -------------------------------------------------------------------------

    public void ExecuteAttack(HeroBehaviour target, Action onFinished)
    {
        if (isDead) { onFinished?.Invoke(); return; }
        StartCoroutine(AttackCoroutine(target, onFinished));
    }

    private IEnumerator AttackCoroutine(HeroBehaviour target, Action onFinished)
    {
        // Reset flags before starting
        attackHitFrame = false;
        attackFinished = false;

        // Play the attack animation
        enemyAnimator.SetBool("isAttacking", true);

        // Wait until the hit-frame event fires
        float hitTimeout = 1.5f;
        while (!attackHitFrame && hitTimeout > 0)
        {
            hitTimeout -= Time.deltaTime;
            yield return null;
        }

        // Deal damage exactly at the hit frame
        if (target != null && !target.IsDead)
        {
            int damage = enemyData.GetAttackDamage();
            target.TakeDamage(damage);
            Debug.Log($"[Enemy] {enemyData.Name} hit {target.name} for {damage}.");
        }

        // Wait until the end-frame event fires (clip fully played)
        float timeout = 2.0f;
        while (!attackFinished && timeout > 0)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        // Return to idle
        enemyAnimator.SetBool("isAttacking", false);
        textMeshPro.text = $"{enemyData.Name} HP: {currentHealth}";

        // Tell CombatSystem this enemy's turn is done
        onFinished?.Invoke();
    }

    // -------------------------------------------------------------------------
    // Receiving Damage
    // -------------------------------------------------------------------------

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        _enemyHealthBar.value = currentHealth;

        var go = Instantiate(textPrefab, transform.position, Quaternion.identity, transform);
        go.GetComponent<TextMesh>().text = $"-{damage}";
        if (FloatingCombatText.Instance != null)
        {
            FloatingCombatText.Instance.Show(go);
        }

        textMeshPro.text = $"{enemyData.Name} HP: {currentHealth}";
        Debug.Log($"[Enemy] {enemyData.Name} took {damage}. HP left: {currentHealth}");

        if (currentHealth <= 0) Die();
    }

    // -------------------------------------------------------------------------
    // Death
    // -------------------------------------------------------------------------

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        enemyAnimator.SetBool("isAttacking", false);
        Debug.Log($"[Enemy] {enemyData.Name} died.");
        combatSystem?.OnEnemyDied(this);
        Destroy(gameObject);
    }
}