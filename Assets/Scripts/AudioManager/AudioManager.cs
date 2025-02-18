using System.Collections;
using UnityEngine;
using Fusion;
using System;

public class AudioManager : NetworkBehaviour
{
    [Header("Sound Effects")]
    public Audio[] sounds; // Array for sound effects (SFX)

    [Header("Music Tracks")]
    public Audio[] musicTracks; // Array for music tracks

    public static AudioManager Instance;

    private Audio currentMusic; // The currently playing music

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeAudios(sounds);
        InitializeAudios(musicTracks);
    }

    private void InitializeAudios(Audio[] audioArray)
    {
        foreach (Audio audio in audioArray)
        {
            audio.source = gameObject.AddComponent<AudioSource>();
            audio.source.clip = audio.clip;
            audio.source.pitch = audio.pitch;
            audio.source.loop = audio.loop;
            audio.source.playOnAwake = false;

            if (audio.music)
            {
                audio.source.volume = PlayerPrefs.GetFloat("music", 1f);
            }
            else
            {
                audio.source.volume = PlayerPrefs.GetFloat("sfx", 1f);
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void PlayAudioRpc(string audioName, bool isMusic)
    {
        if (isMusic)
        {
            PlayMusic(audioName, isRpc: true);
        }
        else
        {
            PlaySound(audioName, isRpc: true);
        }
    }

    public void PlaySound(string name, bool isRpc = false)
    {
        Audio audio = Array.Find(sounds, s => s.name == name);

        if (audio == null)
        {
            Debug.LogWarning($"Sound {name} not found in SFX array");
            return;
        }

        if (audio.isMultiplayer && !isRpc)
        {
            // Play the sound for all players
            if (Object.HasStateAuthority)
            {
                PlayAudioRpc(name, isMusic: false);
                Debug.Log($"Multiplayer Sound being played: {name}");
            }
        }
        else
        {
            // Play the sound locally for this player
            Play(audio);
            Debug.Log($"Local Sound being played: {name}");
        }
    }

    public void PlayMusic(string name, bool isRpc = false)
    {
        Audio music = Array.Find(musicTracks, m => m.name == name);

        if (music == null)
        {
            Debug.LogWarning($"Music {name} not found in Music array");
            return;
        }

        if (music.isMultiplayer && !isRpc)
        {
            // Play the music for all players
            if (Object.HasStateAuthority)
            {
                PlayAudioRpc(name, isMusic: true);
                Debug.Log($"Multiplayer Music being played: {name}");
            }
        }
        else
        {
            if (currentMusic != null && currentMusic.source.isPlaying)
            {
                currentMusic.source.Stop(); // Stop the currently playing music
            }

            currentMusic = music; // Update the current music reference
            Play(music);
            Debug.Log($"Local Music being played: {name}");
        }
    }

    public void StopMusic()
    {
        if (currentMusic != null && currentMusic.source.isPlaying)
        {
            currentMusic.source.Stop();
            Debug.Log($"Stopped music: {currentMusic.name}");
        }
    }

    private void Play(Audio audio)
    {
        if (audio.source != null)
        {
            audio.source.Play();
        }
        else
        {
            Debug.LogWarning($"AudioSource for {audio.name} is null");
        }
    }
}
