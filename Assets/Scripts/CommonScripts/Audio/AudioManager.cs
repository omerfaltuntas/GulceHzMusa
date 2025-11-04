using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)] // AudioManager'ı erken çalıştır
public class AudioManager : MonoSingleton<AudioManager>
{
    [Header("Sounds")]
    public Sound[] Sounds;

    // Hızlı erişim
    Dictionary<string, Sound> _map = new Dictionary<string, Sound>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Inspector üzerindeki değişikliklerde çağrılır.
    /// İsimlerin benzersizliğini kontrol eder ve name→sound eşlemesini yeniden kurar.
    /// </summary>
    void OnValidate()
    {
        EnsureUniqueNames();
        BuildMap();
    }

    /// <summary>
    /// MonoSingleton tabanının Awake'ini çalıştırır ve AudioSource'ları erken hazırlar.
    /// </summary>
    protected override void Awake()
    {
        base.Awake(); // MonoSingleton<T>.Awake() çalışsın, instance ayarlansın

        // Audio kaynaklarını kur
        if (Sounds != null)
        {
            foreach (Sound s in Sounds)
            {
                if (s == null) continue;
                if (s.Source == null) s.Source = gameObject.AddComponent<AudioSource>();
                s.Source.clip = s.AudioClip;
                s.Source.volume = s.Volume;
                s.Source.pitch  = s.Pitch;
                s.Source.mute   = s.Mute;
                s.Source.loop   = s.Loop;
                s.Source.playOnAwake = s.playOnAwake;
            }
        }

        BuildMap(); // name→sound eşleşmesini kur
    }

    /// <summary>
    /// Dahili name→sound sözlüğünü baştan kurar.
    /// </summary>
    private void BuildMap()
    {
        _map.Clear();
        if (Sounds == null) return;
        foreach (var s in Sounds)
        {
            if (s == null || string.IsNullOrWhiteSpace(s.Name)) continue;
            _map[s.Name] = s;
        }
    }

    /// <summary>
    /// Dizideki Sound kayıtlarının Name alanlarının boş/tekrar olup olmadığını kontrol eder ve uyarı verir.
    /// </summary>
    private void EnsureUniqueNames()
    {
        if (Sounds == null) return;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < Sounds.Length; i++)
        {
            var s = Sounds[i];
            if (s == null) continue;
            if (string.IsNullOrWhiteSpace(s.Name))
            {
                Debug.LogWarning($"[AudioManager] Sounds[{i}] isimsiz. Bir Name ver.");
                continue;
            }
            if (!seen.Add(s.Name))
            {
                Debug.LogWarning($"[AudioManager] Aynı isim tekrarı: \"{s.Name}\".");
            }
        }
    }

    /// <summary>
    /// Verilen ada karşılık gelen <see cref="Sound"/> kaydını döndürür.
    /// </summary>
    /// <param name="name">Aranan sesin adı (case-insensitive).</param>
    /// <returns>Eşleşen <see cref="Sound"/> ya da yoksa null.</returns>
    public Sound Get(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        _map.TryGetValue(name, out var s);
        return s;
    }

    /// <summary>
    /// Ad içinde geçen metne göre (contains) ses araması yapar.
    /// </summary>
    /// <param name="query">Aranacak metin (case-insensitive).</param>
    /// <param name="max">Maksimum sonuç sayısı.</param>
    /// <returns>Eşleşen <see cref="Sound"/> listesi.</returns>
    public List<Sound> Search(string query, int max = 20)
    {
        var list = new List<Sound>();
        if (string.IsNullOrWhiteSpace(query) || Sounds == null) return list;
        string q = query.Trim();
        for (int i = 0; i < Sounds.Length; i++)
        {
            var s = Sounds[i];
            if (s == null || string.IsNullOrEmpty(s.Name)) continue;
            if (s.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                list.Add(s);
                if (list.Count >= max) break;
            }
        }
        return list;
    }

    /// <summary>
    /// İlgili Sound kaydı için AudioSource yoksa oluşturur, varsa aynen geri döndürür.
    /// </summary>
    /// <param name="s">Kaynağı kontrol edilecek <see cref="Sound"/>.</param>
    /// <returns>Hazır bir <see cref="AudioSource"/>; yoksa null.</returns>
    private AudioSource EnsureSource(Sound s)
    {
        if (s == null) return null;
        if (s.Source == null)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.clip = s.AudioClip;
            src.volume = s.Volume;
            src.pitch  = s.Pitch;
            src.mute   = s.Mute;
            src.loop   = s.Loop;
            src.playOnAwake = s.playOnAwake;
            s.Source = src;
        }
        return s.Source;
    }

    /// <summary>
    /// Adı verilen sesi (clip) çalar.
    /// </summary>
    /// <param name="Name">Ses adı.</param>
    public void Play(string Name)
    {
        var s = Get(Name);
        if (s == null || s.AudioClip == null) return;
        var src = EnsureSource(s);
        if (src == null) return;
        src.Play();
    }

    /// <summary>
    /// Adı verilen sesin çalımını durdurur.
    /// </summary>
    /// <param name="Name">Ses adı.</param>
    public void Stop(string Name)
    {
        var s = Get(Name);
        if (s == null) return;
        if (s.Source != null) s.Source.Stop();
    }

    /// <summary>
    /// Adı verilen sesin mute durumunu kapatır (ses aktif).
    /// </summary>
    /// <param name="Name">Ses adı.</param>
    public void SoundEffectsActive(string Name)
    {
        var s = Get(Name);
        if (s == null) return;
        var src = EnsureSource(s);
        if (src != null) src.mute = false;
    }

    /// <summary>
    /// Adı verilen sesi mute eder (ses pasif).
    /// </summary>
    /// <param name="Name">Ses adı.</param>
    public void SoundEffectsPassive(string Name)
    {
        var s = Get(Name);
        if (s == null) return;
        var src = EnsureSource(s);
        if (src != null) src.mute = true;
    }

    /// <summary>
    /// Adı verilen sesin pitch değerini ayarlar.
    /// </summary>
    /// <param name="Name">Ses adı.</param>
    /// <param name="value">Yeni pitch değeri.</param>
    public void SetPitch(string Name, float value)
    {
        var s = Get(Name);
        if (s == null) return;
        var src = EnsureSource(s);
        if (src != null) src.pitch = value;
    }

    /// <summary>
    /// Adı verilen sesi <see cref="AudioSource.PlayOneShot(AudioClip,float)"/> ile üst üste çalar.
    /// </summary>
    /// <param name="name">Ses adı.</param>
    /// <param name="volume">Çalma ses yüksekliği (0–1).</param>
    public void PlayOneShot(string name, float volume = 1f)
    {
        var s = Get(name);
        if (s == null || s.AudioClip == null) return;
        var src = EnsureSource(s);
        if (src == null) return;
        src.PlayOneShot(s.AudioClip, volume);
    }

    /// <summary>
    /// Geçici bir AudioSource üzerinde kısa “pluck” (tıng) efekti çalar; basit attack/decay zarfı uygular.
    /// </summary>
    /// <param name="name">Ses adı.</param>
    /// <param name="volume">Hedef ses yüksekliği.</param>
    /// <param name="attack">Yükseliş süresi (sn).</param>
    /// <param name="decay">Sönüm süresi (sn).</param>
    /// <param name="pitchJitter">Rastgele perde sapması oranı.</param>
    /// <param name="lowpass">Düşük geçiren filtre uygula mı?</param>
    public void PlayPluck(string name, float volume = 1f, float attack = 0.01f, float decay = 0.7f, float pitchJitter = 0.015f, bool lowpass = true)
    {
        var s = Get(name);
        if (s == null || s.AudioClip == null) return;

        var go = new GameObject("Pluck_" + name);
        go.transform.SetParent(this.transform);
        var src = go.AddComponent<AudioSource>();
        src.clip = s.AudioClip;
        src.outputAudioMixerGroup = s.Source != null ? s.Source.outputAudioMixerGroup : null;
        src.spatialBlend = 0f;
        src.loop = false;
        src.playOnAwake = false;

        // Hafif rastgele perde
        src.pitch = s.Pitch * (1f + UnityEngine.Random.Range(-pitchJitter, pitchJitter));
        src.volume = 0f;

        AudioLowPassFilter lp = null;
        if (lowpass) { lp = go.AddComponent<AudioLowPassFilter>(); lp.cutoffFrequency = 18000f; }

        src.Play();
        StartCoroutine(PluckEnv(src, volume, attack, decay, lp));
    }

    /// <summary>
    /// Pluck için attack/decay zarfını uygular; bittiğinde geçici kaynağı yok eder.
    /// </summary>
    /// <param name="src">Geçici AudioSource.</param>
    /// <param name="targetVol">Hedef ses yüksekliği.</param>
    /// <param name="attack">Yükseliş süresi (sn).</param>
    /// <param name="decay">Sönüm süresi (sn).</param>
    /// <param name="lp">Opsiyonel LowPass filtresi.</param>
    /// <returns>Coroutine IEnumerator.</returns>
    private IEnumerator PluckEnv(AudioSource src, float targetVol, float attack, float decay, AudioLowPassFilter lp)
    {
        // Attack
        float t = 0f;
        while (t < attack)
        {
            t += Time.deltaTime;
            float k = t / Mathf.Max(attack, 0.0001f);
            if (src) src.volume = Mathf.Lerp(0f, targetVol, k);
            if (lp) lp.cutoffFrequency = Mathf.Lerp(18000f, 16000f, k);
            yield return null;
        }

        // Decay
        t = 0f;
        while (t < decay && src != null)
        {
            t += Time.deltaTime;
            float k = t / Mathf.Max(decay, 0.0001f);
            src.volume = Mathf.Lerp(targetVol, 0f, k);
            if (lp) lp.cutoffFrequency = Mathf.Lerp(16000f, 3500f, k);
            yield return null;
        }

        if (src != null)
        {
            src.Stop();
            Destroy(src.gameObject);
        }
    }
}