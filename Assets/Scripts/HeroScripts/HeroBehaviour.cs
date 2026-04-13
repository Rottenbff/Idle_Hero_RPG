using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeroBehaviour : MonoBehaviour
{
    [SerializeField] private HeroData heroData;
    [SerializeField] private TextMeshProUGUI heroText;
    [SerializeField] private GameObject textPrefab;
    [SerializeField] private Slider _heroHealthBar;
    [SerializeField] private Animator heroAnimator;

    private int currentHealth;
    private bool isHeroDead = false;
    private CombatSystem combatSystem;

    // These two flags are SET by Animation Events on the attack clip.
    // The coroutine simply waits on them — no timers, no state name checks.
    private bool attackHitFrame = false;   // set at the "hit" frame of the swing
    private bool attackFinished = false;   // set at the very last frame of the clip

    public bool IsDead => isHeroDead;
    public int CurrentHealth => currentHealth;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    void Start()
    {
        combatSystem = FindObjectOfType<CombatSystem>();
        if (combatSystem == null)
        {
            Debug.LogError("[HeroBehaviour] CombatSystem not found!");
            return;
        }

        if (heroAnimator != null)
        {
            heroAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        currentHealth = heroData.GetStartingHealth();
        _heroHealthBar.maxValue = currentHealth;
        _heroHealthBar.value = currentHealth;
        heroText.text = $"{heroData.Name} HP: {currentHealth}";

        combatSystem.RegisterHero(this);
    }

    // -------------------------------------------------------------------------
    // Animation Events — add these two events to your attack animation clip:
    //
    //   1. At the "hit" frame (when weapon connects):
    //      Function: OnAttackHit
    //
    //   2. At the very last frame of the clip:
//        Function: OnAttackEnd
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
    // Called by CombatSystem during Hero Phase
    // -------------------------------------------------------------------------

    public void ExecuteAttack(EnemyBehaviour target, Action onFinished)
    {
        if (isHeroDead) { onFinished?.Invoke(); return; }
        StartCoroutine(AttackCoroutine(target, onFinished));
    }

    private IEnumerator AttackCoroutine(EnemyBehaviour target, Action onFinished)
    {
        // Reset flags before starting
        attackHitFrame = false;
        attackFinished = false;

        // Play the attack animation
        heroAnimator.SetBool("isAttacking", true);

        // Wait until the hit-frame event fires
        yield return new WaitUntil(() => attackHitFrame);

        // Deal damage exactly at the hit frame
        if (target != null && !target.IsDead)
        {
            int damage = heroData.GetAttackDamage();
            target.TakeDamage(damage);
            Debug.Log($"[Hero] {heroData.Name} hit {target.name} for {damage}.");
        }

        // Wait until the end-frame event fires (clip fully played)
        float timeout = 2.0f;
        while (!attackFinished && timeout > 0)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        // Return to idle
        heroAnimator.SetBool("isAttacking", false);
        heroText.text = $"{heroData.Name} HP: {currentHealth}";

        // Tell CombatSystem this hero's turn is done
        onFinished?.Invoke();
    }

    // -------------------------------------------------------------------------
    // Receiving Damage
    // -------------------------------------------------------------------------

    public void TakeDamage(int damage)
    {
        if (isHeroDead) return;

        currentHealth -= damage;
        _heroHealthBar.value = currentHealth;

        var go = Instantiate(textPrefab, transform.position, Quaternion.identity, transform);
        go.GetComponent<TextMesh>().text = $"-{damage}";
        if (FloatingCombatText.Instance != null)
        {
            FloatingCombatText.Instance.Show(go);
        }

        heroText.text = $"{heroData.Name} HP: {currentHealth}";
        Debug.Log($"[Hero] {heroData.Name} took {damage}. HP left: {currentHealth}");

        if (currentHealth <= 0) Die();
    }

    // -------------------------------------------------------------------------
    // Death
    // -------------------------------------------------------------------------

    private void Die()
    {
        if (isHeroDead) return;
        isHeroDead = true;
        heroAnimator.SetBool("isAttacking", false);
        Debug.Log($"[Hero] {heroData.Name} died.");
        combatSystem?.OnHeroDied(this);
        Destroy(gameObject);
    }
}