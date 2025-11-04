using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Sound
{
    [HideInInspector] public AudioSource Source;
    public string Name;
    public AudioClip AudioClip;

    [Range(0f, 1f)] public float Volume = 1f;
    [Range(.1f, 3f)] public float Pitch = 1f;
    public bool Mute = false;
    public bool Loop = false;
    public bool playOnAwake = false;
}