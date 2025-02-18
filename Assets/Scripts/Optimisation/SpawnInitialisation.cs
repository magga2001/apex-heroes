using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class SpawnInitialisation : NetworkBehaviour
{
    // Enum for object types (this will show as a dropdown in the Unity Inspector)
    public enum ObjectType
    {
        None,
        Object,
        MatchObject,
        EffectObject
    }

    [Header("Object Type Settings")]
    [Tooltip("Select the type of object to initialize.")]
    [SerializeField] private ObjectType objectType; // This will appear as a dropdown in the Inspector

    public override void Spawned()
    {
        // Perform logic based on the objectType
        switch (objectType)
        {
            case ObjectType.Object:
                ObjectPoolingEvents.OnObjectInitialized?.Invoke(Object);
                break;

            case ObjectType.MatchObject:
                MatchObjectPoolingEvents.OnObjectInitialized?.Invoke(Object);
                break;

            case ObjectType.EffectObject:
                EffectPoolingEvents.OnObjectInitialized?.Invoke(Object);
                break;

            default:
                Debug.LogWarning("Unknown object type during initialization.");
                break;
        }
    }
}
