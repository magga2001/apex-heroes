using UnityEngine;

public abstract class StatusEffect
{
    protected Enemy enemy;
    protected Player Player;
    protected float duration;

    public StatusEffect(Enemy enemy, float duration)
    {
        this.enemy = enemy;
        this.duration = duration;
    }
    public StatusEffect(Player Player, float duration)
    {
        this.Player = Player;
        this.duration = duration;
    }
    // Method to apply the effect
    public abstract void ApplyEffect();

    // Method to remove the effect after the duration
    public abstract void RemoveEffect();
}

