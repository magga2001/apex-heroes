using UnityEngine;
using Fusion;

public abstract class Ability : NetworkBehaviour
{
    public string abilityName;
    public Sprite abilityIcon;
    public float cooldownTime;

    // Abstract method to force each derived class to implement its own behavior
    public abstract void Activate(PlayerRef playerref, Player player);
}

