using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Arka plan ses kodunu sahneler arası korur ve MainMenu'ye dönüldüğünde siler.
/// </summary>
public class BackgroundMusic : MonoBehaviour
{
    public static BackgroundMusic Instance;

    [Header("Audio Source")]
    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Sahne geçişinde objeyi silme
        }
        else
        {
            Destroy(gameObject); // Aynı müzik objesi varsa yok et
            return;
        }

        audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // MainMenu sahnesine dönüldüğünde kendini yok et
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// AudioSource öğesine referans verir.
    /// </summary>
    public AudioSource GetSource()
    {
        return audioSource;
    }
}