using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Audio
{
    public string name;       // Audio name
    public AudioClip clip;    // Audio clip
    public float pitch = 1f;  // Audio pitch
    public bool loop = false; // Should the audio loop?
    public bool music = false; // Is this audio track music?
    public bool isMultiplayer = false; // Is this audio multiplayer?

    [HideInInspector]
    public AudioSource source; // The AudioSource component for this audio
}

