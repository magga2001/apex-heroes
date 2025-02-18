using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class BuffBox : NetworkBehaviour
{
    [Header("Floating Settings")]
    public float floatAmplitude = 0.5f;  // How much it floats up and down
    public float floatSpeed = 1f;        // Speed of the floating motion

    private Vector3 startPos;

    public override void Spawned()
    {
        MatchObjectPoolingEvents.OnObjectInitialized?.Invoke(Object);
    }

    // Virtual methods allow derived classes to override if necessary
    protected virtual void Start()
    {
        startPos = transform.position;
    }

    public override void FixedUpdateNetwork()
    {
        // Use Runner.SimulationTime for synchronized time
        float newY = startPos.y + Mathf.Sin((float)Runner.SimulationTime * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }
}


//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class BuffBox : MonoBehaviour
//{
//    [Header("Floating Settings")]
//    public float floatAmplitude = 0.5f;  // How much it floats up and down
//    public float floatSpeed = 1f;  // Speed of the floating motion

//    private Vector3 startPos;

//    // Virtual methods allow derived classes to override if necessary
//    protected virtual void Start()
//    {
//        startPos = transform.position;
//    }

//    protected virtual void Update()
//    {
//        // Apply floating motion
//        float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
//        transform.position = new Vector3(startPos.x, newY, startPos.z);
//    }
//}
