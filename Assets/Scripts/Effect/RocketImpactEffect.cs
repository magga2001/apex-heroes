using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketImpactEffect : NetworkBehaviour
{
    public float duration = 2f;  // How long the explosion stays active

    public override void Spawned()
    {
        EffectPoolingEvents.OnObjectInitialized?.Invoke(Object);
    }

    private void OnEnable()
    {
        // Automatically start the coroutine when the explosion is enabled
        StartCoroutine(DeactivateAfterDelay());
    }

    private IEnumerator DeactivateAfterDelay()
    {
        // Wait for the duration
        yield return new WaitForSeconds(duration);

        // Deactivate the explosion (or destroy it if preferred)
        gameObject.SetActive(false);  // Use Destroy(gameObject) if you prefer to remove it completely
    }
}
