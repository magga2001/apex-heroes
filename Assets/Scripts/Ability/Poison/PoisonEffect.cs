using System.Collections;
using UnityEngine;

public class PoisonEffect : StatusEffect
{
    private float damageMultiplier;

    public PoisonEffect(Enemy enemy, float duration, float damageMultiplier) : base(enemy, duration)
    {
        this.damageMultiplier = damageMultiplier; 
    }

    public override void ApplyEffect()
    {
        enemy.StartCoroutine(EffectCoroutine());
    }

    private IEnumerator EffectCoroutine()
    {
        // Apply the poison effect
        enemy.damageMultiplier *= damageMultiplier;

        // Wait for the duration
        yield return new WaitForSeconds(duration);

        // Remove the poison effect after duration
        RemoveEffect();
    }

    public override void RemoveEffect()
    {
        // Reset the damage multiplier to normal
        enemy.damageMultiplier /= damageMultiplier;
    }
}
