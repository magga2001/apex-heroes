using System.Collections;
using UnityEngine;

public class FreezeEffect : StatusEffect
{
    private float slowMultiplier;

    public FreezeEffect(Enemy enemy, float duration, float slowMultiplier) : base(enemy, duration)
    {
        this.slowMultiplier = slowMultiplier;
    }
    public FreezeEffect(Player player, float duration, float slowMultiplier) : base(player, duration)
    {
        this.slowMultiplier = slowMultiplier;
    }
    public override void ApplyEffect()
    {
        enemy.StartCoroutine(EffectCoroutine());
    }

    private IEnumerator EffectCoroutine()
    {
        // Reduce enemy movement speed by the slowMultiplier
        enemy.MoveSpeed *= slowMultiplier;

        yield return new WaitForSeconds(duration);

        // Reset movement speed after the duration
        RemoveEffect();
    }

    public override void RemoveEffect()
    {
        // Restore enemy's original movement speed
        enemy.MoveSpeed /= slowMultiplier;
    }
}
