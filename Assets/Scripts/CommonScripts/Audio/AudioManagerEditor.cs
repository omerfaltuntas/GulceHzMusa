#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using System.Reflection;

// ---------------- PREVIEW HELPER ----------------
static class EditorAudioPreview
{
    static readonly System.Type T = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");

    // Play
    static readonly MethodInfo mPlayPreview3 = Find("PlayPreviewClip", typeof(AudioClip), typeof(int), typeof(bool));             // (clip, start, loop)
    static readonly MethodInfo mPlayPreview4 = Find("PlayPreviewClip", typeof(AudioClip), typeof(int), typeof(bool), typeof(bool)); // (clip, start, loop, startPaused)
    static readonly MethodInfo mPlayClip1 = Find("PlayClip", typeof(AudioClip));                                          // (clip) – bazı sürümler

    // Stop
    static readonly MethodInfo mStopAllPrev = Find("StopAllPreviewClips"); // ()
    static readonly MethodInfo mStopAllClips = Find("StopAllClips");        // () – bazı sürümler
    static readonly MethodInfo mStopPrev1 = Find("StopPreviewClip", typeof(AudioClip)); // (clip) – bazı sürümler
    static readonly MethodInfo mStopClip1 = Find("StopClip", typeof(AudioClip));   // (clip) – bazı sürümler

    // IsPlaying
    static readonly MethodInfo mIsPrev0 = Find("IsPreviewClipPlaying");              // ()
    static readonly MethodInfo mIsPrev1 = Find("IsPreviewClipPlaying", typeof(AudioClip)); // (clip)

    static MethodInfo Find(string name, params System.Type[] args)
    {
        if (T == null) return null;
        return T.GetMethod(
            name,
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            args ?? System.Type.EmptyTypes,
            null
        );
    }

    public static bool IsPlaying(AudioClip clip)
    {
        try
        {
            if (mIsPrev1 != null) return (bool)mIsPrev1.Invoke(null, new object[] { clip });
            if (mIsPrev0 != null) return (bool)mIsPrev0.Invoke(null, null);
        }
        catch { }
        return false;
    }

    public static void Play(AudioClip clip, bool loop = false)
    {
        if (clip == null) return;
        try
        {
            if (mPlayPreview3 != null) { mPlayPreview3.Invoke(null, new object[] { clip, 0, loop }); return; }
            if (mPlayPreview4 != null) { mPlayPreview4.Invoke(null, new object[] { clip, 0, loop, false }); return; }
            if (mPlayClip1 != null) { mPlayClip1.Invoke(null, new object[] { clip }); return; }
        }
        catch { }
    }

    public static void Stop(AudioClip clip)
    {
        try
        {
            if (clip != null && mStopPrev1 != null) { mStopPrev1.Invoke(null, new object[] { clip }); return; }
            if (clip != null && mStopClip1 != null) { mStopClip1.Invoke(null, new object[] { clip }); return; }
        }
        catch { }
        StopAll();
    }

    public static void StopAll()
    {
        try
        {
            if (mStopAllPrev != null) { mStopAllPrev.Invoke(null, null); return; }
            if (mStopAllClips != null) { mStopAllClips.Invoke(null, null); return; }
        }
        catch { }
    }

    // Çalıyorsa durdur, çalmıyorsa önce hepsini kes sonra çal (stack’lenmeyi önler)
    public static void Toggle(AudioClip clip, bool loop = false)
    {
        if (clip == null) return;
        if (IsPlaying(clip)) Stop(clip);
        else { StopAll(); Play(clip, loop); }
    }
}

// ---------------- CUSTOM INSPECTOR ----------------
[CustomEditor(typeof(AudioManager))]
public class AudioManagerEditor : Editor
{
    SerializedProperty soundsProp;
    SearchField _searchField;
    string _query = "";

    // Quick add
    string _newName = "";
    AudioClip _newClip = null;
    float _newVol = 1f;
    float _newPitch = 1f;
    bool _newLoop = false;
    bool _newAwake = false;

    bool _showList = true;

    void OnEnable()
    {
        soundsProp = serializedObject.FindProperty("Sounds");
        _searchField = new SearchField();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Audio Manager", EditorStyles.boldLabel);

        // ---- Add New Sound ----
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Add New Sound", EditorStyles.boldLabel);
            _newName = EditorGUILayout.TextField("Name", _newName);
            _newClip = (AudioClip)EditorGUILayout.ObjectField("Audio Clip", _newClip, typeof(AudioClip), false);
            _newVol = EditorGUILayout.Slider("Volume", _newVol, 0f, 1f);
            _newPitch = EditorGUILayout.Slider("Pitch", _newPitch, 0.1f, 3f);
            _newLoop = EditorGUILayout.Toggle("Loop", _newLoop);
            _newAwake = EditorGUILayout.Toggle("Play On Awake", _newAwake);

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_newName) || _newClip == null))
            {
                if (GUILayout.Button("Add"))
                {
                    int i = soundsProp.arraySize;
                    soundsProp.arraySize++;
                    var sProp = soundsProp.GetArrayElementAtIndex(i);

                    sProp.FindPropertyRelative("Name").stringValue = _newName;
                    sProp.FindPropertyRelative("AudioClip").objectReferenceValue = _newClip;
                    sProp.FindPropertyRelative("Volume").floatValue = _newVol;
                    sProp.FindPropertyRelative("Pitch").floatValue = _newPitch;
                    sProp.FindPropertyRelative("Mute").boolValue = false;
                    sProp.FindPropertyRelative("Loop").boolValue = _newLoop;
                    sProp.FindPropertyRelative("playOnAwake").boolValue = _newAwake;

                    _newName = "";
                    _newClip = null;
                    _newVol = 1f; _newPitch = 1f; _newLoop = false; _newAwake = false;

                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(target);
                }
            }
        }

        // ---- Search ----
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Search", GUILayout.Width(50));
        _query = _searchField.OnGUI(_query);
        if (GUILayout.Button("Clear", GUILayout.Width(60))) _query = "";
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(6);

        _showList = EditorGUILayout.Foldout(_showList, $"Sounds ({soundsProp.arraySize})");
        if (_showList) DrawFilteredSounds();

        EditorGUILayout.Space(8);
        if (GUILayout.Button("Ping AudioManager"))
            EditorGUIUtility.PingObject(((AudioManager)target).gameObject);

        serializedObject.ApplyModifiedProperties();
    }

    void DrawFilteredSounds()
    {
        var indices = new List<int>();
        for (int i = 0; i < soundsProp.arraySize; i++)
        {
            var sProp = soundsProp.GetArrayElementAtIndex(i);
            var nameProp = sProp.FindPropertyRelative("Name");
            string name = nameProp != null ? nameProp.stringValue : "";
            if (string.IsNullOrEmpty(_query) ||
                (!string.IsNullOrEmpty(name) && name.ToLower().Contains(_query.ToLower())))
            {
                indices.Add(i);
            }
        }

        if (indices.Count == 0)
        {
            EditorGUILayout.HelpBox("Arama sonucunda eşleşme yok.", MessageType.Info);
            return;
        }

        using (new EditorGUILayout.VerticalScope("box"))
        {
            foreach (var idx in indices)
            {
                var sProp = soundsProp.GetArrayElementAtIndex(idx);
                var nameProp = sProp.FindPropertyRelative("Name");
                var clipProp = sProp.FindPropertyRelative("AudioClip");
                var volProp = sProp.FindPropertyRelative("Volume");
                var pitchProp = sProp.FindPropertyRelative("Pitch");
                var muteProp = sProp.FindPropertyRelative("Mute");
                var loopProp = sProp.FindPropertyRelative("Loop");
                var awakeProp = sProp.FindPropertyRelative("playOnAwake");

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();

                GUILayout.Label(string.IsNullOrEmpty(nameProp.stringValue) ? "(No Name)" : nameProp.stringValue, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                var clip = (AudioClip)clipProp.objectReferenceValue;

                // Play = Toggle (ikinci basışta durdurur)
                if (GUILayout.Button("Play", GUILayout.Width(50)))
                    EditorAudioPreview.Toggle(clip);

                // Sadece ilgili klibi durdur
                if (GUILayout.Button("Stop", GUILayout.Width(50)))
                    EditorAudioPreview.Stop(clip);

                // OneShot = önce hepsini kes, sonra bir kez çal
                if (GUILayout.Button("OneShot", GUILayout.Width(70)))
                {
                    EditorAudioPreview.StopAll();
                    EditorAudioPreview.Play(clip, false);
                }

                // Çöp kutusu ikonlu Delete
                GUIContent trashIcon = EditorGUIUtility.IconContent("TreeEditor.Trash");
                if (GUILayout.Button(trashIcon, GUILayout.Width(24), GUILayout.Height(18)))
                {
                    var nameStr = string.IsNullOrEmpty(nameProp.stringValue) ? "(No Name)" : nameProp.stringValue;
                    if (EditorUtility.DisplayDialog(
                            "Delete Sound",
                            $"Silinsin mi?\n\nName: {nameStr}",
                            "Delete", "Cancel"))
                    {
                        EditorAudioPreview.Stop(clip);

                        // SerializedProperty ile güvenli silme
                        soundsProp.DeleteArrayElementAtIndex(idx);
                        serializedObject.ApplyModifiedProperties();

                        // Tekrar Apply’dan sonra gerekirse bir daha sil
                        if (idx < soundsProp.arraySize &&
                            soundsProp.GetArrayElementAtIndex(idx).objectReferenceValue == null)
                        {
                            soundsProp.DeleteArrayElementAtIndex(idx);
                            serializedObject.ApplyModifiedProperties();
                        }

                        EditorUtility.SetDirty(target);
                        GUIUtility.ExitGUI(); // << ÖNEMLİ: Unity’nin inspector redraw bug’ını engeller
                    }
                }
                EditorGUILayout.EndHorizontal();

                // Özellikler
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(nameProp);
                EditorGUILayout.PropertyField(clipProp);
                EditorGUILayout.Slider(volProp, 0f, 1f);
                EditorGUILayout.Slider(pitchProp, 0.1f, 3f);
                EditorGUILayout.PropertyField(muteProp);
                EditorGUILayout.PropertyField(loopProp);
                EditorGUILayout.PropertyField(awakeProp);
                EditorGUI.indentLevel--;

                EditorGUILayout.EndVertical();
            }
        }
    }
}
#endif