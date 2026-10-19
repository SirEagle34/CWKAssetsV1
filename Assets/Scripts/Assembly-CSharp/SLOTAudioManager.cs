using System;
using System.Collections.Generic;
using DarkTonic.MasterAudio;
using UnityEngine;

public class SLOTAudioManager : Singleton<SLOTAudioManager>
{
    public enum AudioType
    {
        SFX,
        VO,
        Music
    }

    private class MusicFadeOutInfo
    {
        public AudioSource audioSourceToFade;
        public AudioSource audioSourceToPlay;
        public AudioClip audioClip;
        public string clipName;
        public float fadeOutTime;
        public float musicVolume;
        public bool loop;

        public MusicFadeOutInfo(AudioSource a1, AudioSource a2, AudioClip c, float fadeouttime, float musicvolume, bool lp)
        {
            audioSourceToFade = a1;
            audioSourceToPlay = a2;
            audioClip = c;
            clipName = null;
            fadeOutTime = fadeouttime;
            musicVolume = musicvolume;
            loop = lp;
        }

        public MusicFadeOutInfo(AudioSource a1, AudioSource a2, string c, float fadeouttime, float musicvolume, bool lp)
        {
            audioSourceToFade = a1;
            audioSourceToPlay = a2;
            audioClip = null;
            clipName = c;
            fadeOutTime = fadeouttime;
            musicVolume = musicvolume;
            loop = lp;
        }
    }

    [Serializable]
    public struct NamedCooldown
    {
        public VOEvent Event;
        public float Cooldown;
    }

    public AudioSource gui_audiosource;
    private bool useMasterAudio = false;
    private AudioListener listener;

    public float soundVolume = 1.0f;
    public float voVolume = 1f;
    public float musicVolume = 1f;
    private float prevSoundVolume = -1234f;
    private float prevVOVolume = -1234f;
    private float prevMusicVolume = -1234f;

    private List<MusicFadeOutInfo> musicFadeOutList = new List<MusicFadeOutInfo>();
    public NamedCooldown[] VoEventCooldowns;
    private float[] mVoEventCooldowns = new float[13];
    private float[] mCurrentVoEventCooldowns = new float[13];
    private bool mInitialized;
    private static bool s_isOnPhoneCall;
    private static float s_lastPhoneCallCheckTime;

    // Cache to prevent scanning Resources every time a leader speaks.
    // Key: lowercase folder path (e.g. "vo/finn"), Value: all AudioClips in that folder.
    private Dictionary<string, AudioClip[]> voCache = new Dictionary<string, AudioClip[]>();

    // Faction token suffixes used in clip names (must all be lowercase, prefixed with "_").
    // Keep this in sync with GetFactionSuffix().
    private static readonly string[] FactionTokens = new string[]
    {
        "_corn", "_sand", "_plains", "_swamp", "_nicelands", "_floop", "_ice"
    };

    // ==========================================
    // VOLUME PROPERTIES
    // ==========================================

    public float SoundVolume
    {
        get { return soundVolume; }
        set { soundVolume = value; Serialize(); }
    }

    public float VOVolume
    {
        get { return voVolume; }
        set { voVolume = value; Serialize(); }
    }

    public float MusicVolume
    {
        get { return musicVolume; }
        set { musicVolume = value; Serialize(); }
    }

    public float GetSoundVolume() { return SoundVolume; }
    public float GetVOVolume()    { return VOVolume; }
    public float GetMusicVolume() { return MusicVolume; }

    public void SetSoundVolume(float v) { SoundVolume = v; UpdateAudioVolumes(); }
    public void SetVOVolume(float v)    { VOVolume = v;    UpdateAudioVolumes(); }
    public void SetMusicVolume(float v) { MusicVolume = v; UpdateAudioVolumes(); }

    // ==========================================
    // LIFECYCLE
    // ==========================================

    private void Start()
    {
        Deserialize();
        KFFSoundPlayer.SetPlayOneShotFunction(PlayOneShot);
        base.transform.position = new Vector3(0f, 0f, 0f);

        // Load inspector-configured cooldowns into the runtime array.
        if (VoEventCooldowns != null)
        {
            for (int i = 0; i < VoEventCooldowns.Length; i++)
            {
                NamedCooldown nc = VoEventCooldowns[i];
                mVoEventCooldowns[(int)nc.Event] = nc.Cooldown;
            }
        }

        SetSoundVolumeMasterAudio(soundVolume);
        SetMusicVolumeMasterAudio(musicVolume);
        mInitialized = true;
    }

    private void Update()
    {
        UpdateVolumes();
        for (int i = 0; i < mCurrentVoEventCooldowns.Length; i++)
        {
            if (mCurrentVoEventCooldowns[i] > 0f)
                mCurrentVoEventCooldowns[i] -= Time.deltaTime;
        }
    }

    // ==========================================
    // CORE AUDIO PLAYBACK
    // ==========================================

    public AudioSource PlaySound(AudioSource audiosource)
    {
        return (audiosource != null) ? PlaySound(audiosource, audiosource.clip) : null;
    }

    public AudioSource PlaySound(AudioSource audiosource, AudioClip audioclip)
    {
        return (audiosource != null) ? PlaySound(audiosource, audioclip, true, false, AudioType.SFX) : null;
    }

    public AudioSource PlaySound(GameObject obj, AudioClip audioclip)
    {
        return PlaySound(obj, audioclip, true);
    }

    public AudioSource PlaySound(GameObject obj, AudioClip audioclip, bool oneshot)
    {
        return PlaySound(obj, audioclip, oneshot, false);
    }

    public AudioSource PlaySound(GameObject obj, AudioClip audioclip, bool oneshot, bool createnewaudiosource)
    {
        return PlaySound(obj, audioclip, oneshot, createnewaudiosource, false);
    }

    public AudioSource PlaySound(GameObject obj, AudioClip audioclip, bool oneshot, bool createnewaudiosource, bool NoStacking, AudioType audioType = AudioType.SFX)
    {
        AudioSource audioSource = obj.GetComponent(typeof(AudioSource)) as AudioSource;
        if (audioSource == null || createnewaudiosource)
            audioSource = obj.AddComponent(typeof(AudioSource)) as AudioSource;
        return PlaySound(audioSource, audioclip, oneshot, NoStacking, audioType);
    }

    public AudioSource PlaySound(AudioSource audiosource, AudioClip audioclip, bool oneshot, bool NoStacking, AudioType audioType = AudioType.SFX)
    {
        if (audiosource == null) return null;

        switch (audioType)
        {
            case AudioType.SFX:   audiosource.volume = soundVolume; break;
            case AudioType.VO:    audiosource.volume = voVolume;    break;
            case AudioType.Music: audiosource.volume = musicVolume; break;
        }

        audiosource.enabled = true;
        if (oneshot)
        {
            audiosource.PlayOneShot(audioclip);
        }
        else if (!NoStacking || !audiosource.isPlaying)
        {
            audiosource.clip = audioclip;
            audiosource.Play();
        }
        return audiosource;
    }

    public AudioSource PlaySound(string audioClipName, float volume, bool useaudiofolder = true, AudioType audioType = AudioType.SFX)
    {
        string clipName = audioClipName;
        if (useaudiofolder) clipName = "audio/" + clipName;

        AudioClip soundResource = Resources.Load(clipName, typeof(AudioClip)) as AudioClip;
        if (soundResource == null)
        {
            Debug.LogError("SLOTAudioManager: " + clipName + " not found.");
            return null;
        }

        AudioSource src = PlayGUISound(soundResource, true, audioType);

        if (src != null)
        {
            // 🔥 istediğin volume override
            src.volume *= volume;
        }

        return src;
    }
    
    // Plays a randomly numbered variant of a clip (e.g. "VO_Finn_P1Intro_" + random 1..clips).
    public void PlayRandomSound(string audioClipName, int clips, bool useaudiofolder = true, AudioType audioType = AudioType.SFX)
    {
        int random = UnityEngine.Random.Range(1, clips + 1);
        PlaySound(audioClipName + random.ToString(), useaudiofolder, audioType);
    }

    // Loads a clip by name from Resources and plays it through the GUI audio source.
    public void PlaySound(string audioClipName, bool useaudiofolder = true, AudioType audioType = AudioType.SFX)
    {
        string clipName = audioClipName;
        if (useaudiofolder) clipName = "audio/" + clipName;
        AudioClip soundResource = Resources.Load(clipName, typeof(AudioClip)) as AudioClip;
        if (soundResource == null)
        {
            Debug.LogError("SLOTAudioManager: " + clipName + " not found.");
            return;
        }
        PlayGUISound(soundResource, true, audioType);
    }

    public AudioSource PlayLoopSound(string audioClipName, float volumeMultiplier = 1f, bool useaudiofolder = true, AudioType audioType = AudioType.SFX)
    {
        string clipName = audioClipName;
        if (useaudiofolder) clipName = "audio/" + clipName;

        AudioClip clip = Resources.Load(clipName, typeof(AudioClip)) as AudioClip;
        if (clip == null)
        {
            Debug.LogError("SLOTAudioManager: " + clipName + " not found.");
            return null;
        }

        GameObject obj = new GameObject("LoopSound_" + audioClipName);
        obj.transform.SetParent(this.transform);

        AudioSource source = obj.AddComponent<AudioSource>();

        switch (audioType)
        {
            case AudioType.SFX:   source.volume = soundVolume; break;
            case AudioType.VO:    source.volume = voVolume; break;
            case AudioType.Music: source.volume = musicVolume; break;
        }

        source.volume *= volumeMultiplier; // 🔥 burada kısıyoruz

        source.clip = clip;
        source.loop = true;
        source.Play();

        return source;
    }

    public void StopLoopSound(AudioSource source)
    {
        if (source != null)
        {
            source.Stop();
            Destroy(source.gameObject); // 🔥 memory leak önler
        }
    }

    public void StopSound(string audioClipName)
    {
        if (gui_audiosource != null) gui_audiosource.Stop();
        if (!useMasterAudio) return;

        MasterAudio.AudioGroupInfo groupInfo = MasterAudio.GetGroupInfo(audioClipName);
        if (groupInfo == null || groupInfo.Group == null || groupInfo.Sources == null) return;

        foreach (MasterAudio.AudioInfo source in groupInfo.Sources)
        {
            if (source != null && source.Source != null)
                source.Source.Stop();
        }
    }

    public void StopMusic(string musicName) { MasterAudio.FadeOutAllOfSound(musicName, 1f); }

    public AudioSource PlayVO(AudioSource audiosource, AudioClip audioclip)
    {
        return PlaySound(audiosource, audioclip, false, false, AudioType.VO);
    }

    public AudioSource PlayOneShot(AudioClip audioclip)              { return PlayOneShot(audioclip, 1f); }
    public AudioSource PlayOneShot(AudioClip audioclip, float volume) { return PlayOneShot(audioclip, volume, 1f); }

    public AudioSource PlayOneShot(AudioClip audioclip, float volume, float pitch)
    {
        AudioSource audioSource = PlayGUISound(audioclip, true);
        if (audioSource != null)
        {
            audioSource.volume = soundVolume * volume;
            audioSource.pitch = pitch;
        }
        return audioSource;
    }

    public AudioSource PlayOneShot(AudioSource audiosource, AudioClip audioclip)
    {
        return PlaySound(audiosource, audioclip, true, false, AudioType.SFX);
    }

    public AudioSource PlayGUISound(AudioClip audioclip) { return PlayGUISound(audioclip, true); }

    public AudioSource PlayGUISound(AudioClip audioclip, bool oneshot, AudioType audioType = AudioType.SFX)
    {
        CreateListener();
        if (gui_audiosource != null)
            return PlaySound(gui_audiosource, audioclip, oneshot, true, audioType);
        gui_audiosource = PlaySound(base.gameObject, audioclip, oneshot, true, false, audioType);
        return gui_audiosource;
    }

    // ==========================================
    // DYNAMIC VO SYSTEM
    // ==========================================

    /// <summary>
    /// Triggers a VO event for a leader, dynamically loading clips from Resources.
    /// 
    /// FIX 1 — Faction token matching: was using Contains() which could false-match
    ///          substrings (e.g. "sand" inside "second"). Now uses StartsWith("_token")
    ///          on the suffix so only exact faction prefix tokens are excluded.
    ///
    /// FIX 2 — leader.VO null check: old code used == null; now also handles empty string.
    ///
    /// CALLER NOTE (BattleIntroController): TriggerVOEvent expects a LeaderData (which has
    /// a .VO string). Make sure GetCharacter() returns LeaderData, or cast appropriately.
    /// </summary>
    public void TriggerVOEvent(LeaderData leader, VOEvent voEvent, CreatureFaction faction = CreatureFaction.Count)
    {
        // 1. Guard: cooldown active, tutorial text showing, or leader has no VO defined.
        if (mCurrentVoEventCooldowns[(int)voEvent] > 0f
            || Singleton<TutorialController>.Instance.IsShowingText()
            || string.IsNullOrEmpty(leader.VO))
        {
            return;
        }

        // 2. Load and cache the folder.
        //    Folder path is lowercased to match Unity Resources folder names on
        //    case-sensitive platforms (Android / Linux build servers).
        //    Example: "vo/finn"
        string folderPath = "vo/" + leader.VO.ToLower();
        if (!voCache.ContainsKey(folderPath))
        {
            AudioClip[] loaded = Resources.LoadAll<AudioClip>(folderPath);
            voCache[folderPath] = (loaded != null) ? loaded : new AudioClip[0];
        }

        AudioClip[] allClips = voCache[folderPath];
        if (allClips.Length == 0)
        {
            Debug.LogWarning($"[VO] No files found in folder: {folderPath}");
            return;
        }

        // 3. Build the clip name prefix.
        //    Uses leader.VO in its original casing to match actual file names.
        //    Example: "VO_Finn_P1Intro"
        string prefix       = "VO_" + leader.VO + "_" + voEvent.ToString();
        string factionSuffix = GetFactionSuffix(faction); // e.g. "_Corn", or null

        List<AudioClip> factionPool = new List<AudioClip>();
        List<AudioClip> genericPool = new List<AudioClip>();

        // 4. Sort every clip in the folder into the right pool.
        foreach (AudioClip clip in allClips)
        {
            string name = clip.name;

            // Faction pool: clip starts with prefix + faction suffix (e.g. "VO_Finn_PlayCreature_Corn").
            if (!string.IsNullOrEmpty(factionSuffix)
                && name.StartsWith(prefix + factionSuffix, StringComparison.OrdinalIgnoreCase))
            {
                factionPool.Add(clip);
            }
            // Generic pool: clip matches the prefix and is NOT a faction line.
            else if (IsGenericLine(name, prefix))
            {
                genericPool.Add(clip);
            }
        }

        // 5. Faction clips take priority; fall back to generic pool.
        List<AudioClip> finalPool = (factionPool.Count > 0) ? factionPool : genericPool;

        if (finalPool.Count > 0)
        {
            AudioClip selected = finalPool[UnityEngine.Random.Range(0, finalPool.Count)];
            PlayGUISound(selected, true, AudioType.VO);
            // Debug.Log($"[VO] {voEvent} | {selected.name} | pool={finalPool.Count}");
        }
        else
        {
            Debug.LogWarning($"[VO] No clips matched prefix '{prefix}' (faction: {faction}) in {folderPath}");
        }

        // 6. Start the cooldown.
        mCurrentVoEventCooldowns[(int)voEvent] = mVoEventCooldowns[(int)voEvent];
    }

    /// <summary>
    /// Returns true if the clip name belongs to the generic (non-faction) pool for this prefix.
    ///
    /// FIX vs attempt3: the old Contains() check (e.g. Contains("sand")) could accidentally
    /// exclude generic clips whose names happen to contain those letters (e.g. "_second",
    /// "_sandstorm"). Now we check that the suffix STARTS WITH one of the known faction
    /// tokens (e.g. "_corn", "_sand"), which matches only true faction clips.
    ///
    /// Examples that return TRUE (generic):
    ///   "VO_Finn_PlayCreature"         -> exact match, suffix empty
    ///   "VO_Finn_PlayCreature_1"       -> suffix "_1", no faction token
    ///   "VO_Finn_PlayCreature_11"      -> suffix "_11"
    ///   "VO_Finn_PlayCreature_1_kpr"   -> suffix "_1_kpr"
    ///
    /// Examples that return FALSE (faction, excluded from generic pool):
    ///   "VO_Finn_PlayCreature_Corn"    -> suffix "_Corn" starts with "_corn"
    ///   "VO_Finn_PlayCreature_Corn_1"  -> suffix "_Corn_1" starts with "_corn"
    ///   "VO_Jake_GameStart"            -> prefix mismatch, different event
    /// </summary>
    private bool IsGenericLine(string clipName, string prefix)
    {
        // Must begin with the prefix (case-insensitive).
        if (!clipName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        string suffix = clipName.Substring(prefix.Length);

        // Exact match with no suffix at all — valid generic clip.
        if (string.IsNullOrEmpty(suffix))
            return true;

        // Reject any clip whose suffix begins with a known faction token.
        // Using StartsWith on the lowercased suffix prevents false matches like
        // "_sandstorm", "_secondary", etc.
        string lowerSuffix = suffix.ToLower();
        foreach (string token in FactionTokens)
        {
            if (lowerSuffix.StartsWith(token))
                return false;
        }

        // Everything else (numbered variants, _kpr tags, etc.) is generic.
        return true;
    }

    /// <summary>
    /// Maps a CreatureFaction enum to its VO clip name suffix.
    /// Keep FactionTokens[] in sync with these return values (lowercase).
    /// </summary>
    private string GetFactionSuffix(CreatureFaction faction)
    {
        switch (faction)
        {
            case CreatureFaction.Red:        return "_Corn";
            case CreatureFaction.Green:      return "_Sand";
            case CreatureFaction.Blue:       return "_Plains";
            case CreatureFaction.Dark:       return "_Swamp";
            case CreatureFaction.Light:      return "_Nicelands";
            case CreatureFaction.Colorless:  return "_Floop";
            case CreatureFaction.White:      return "_Ice";
            default:                         return null;
        }
    }

    public void SetVOEventCooldown(VOEvent voEvent)
    {
        mCurrentVoEventCooldowns[(int)voEvent] = mVoEventCooldowns[(int)voEvent];
    }

    // ==========================================
    // VOLUME, SERIALIZATION, UTILITY
    // ==========================================

    public void Serialize()
    {
        PlayerPrefs.SetFloat("Options_Sound_Volume", soundVolume);
        PlayerPrefs.SetFloat("Options_VO_Volume",    voVolume);
        PlayerPrefs.SetFloat("Options_Music_Volume", musicVolume);
    }

    public void Deserialize()
    {
        soundVolume  = PlayerPrefs.GetFloat("Options_Sound_Volume", 1f);
        voVolume     = PlayerPrefs.GetFloat("Options_VO_Volume",    1f);
        musicVolume  = PlayerPrefs.GetFloat("Options_Music_Volume", 1f);
    }

    private void UpdateVolumes()
    {
        if (s_isOnPhoneCall && Time.time - s_lastPhoneCallCheckTime > 1f)
        {
            s_isOnPhoneCall = KFFCSUtils.IsOnPhoneCall();
            s_lastPhoneCallCheckTime = Time.time;
        }

        float effectiveSFX   = s_isOnPhoneCall ? 0f : soundVolume;
        float effectiveVO    = s_isOnPhoneCall ? 0f : voVolume;
        float effectiveMusic = s_isOnPhoneCall ? 0f : musicVolume;

        if (effectiveSFX   != prevSoundVolume
            || effectiveVO   != prevVOVolume
            || effectiveMusic != prevMusicVolume)
        {
            UpdateAudioVolumes(s_isOnPhoneCall ? 1 : 0);
            prevSoundVolume  = effectiveSFX;
            prevVOVolume     = effectiveVO;
            prevMusicVolume  = effectiveMusic;
        }
    }

    private void OnApplicationPause(bool pause)
    {
        s_isOnPhoneCall = KFFCSUtils.IsOnPhoneCall();
        s_lastPhoneCallCheckTime = Time.time;
        UpdateVolumes();
    }

    public void UpdateAudioVolumes(int isonphonecall = -1)
    {
        UpdateAudioVolumes(null, isonphonecall);
    }

    public void UpdateAudioVolumes(GameObject root, int isonphonecall = -1)
    {
        bool onPhone   = (isonphonecall < 0) ? KFFCSUtils.IsOnPhoneCall() : (isonphonecall != 0);
        float sfxVol   = onPhone ? 0f : soundVolume;
        float voVol    = onPhone ? 0f : voVolume;
        float musicVol = onPhone ? 0f : musicVolume;

        CreateListener();

        UnityEngine.Object[] sources = (root != null)
            ? root.GetComponentsInChildren(typeof(AudioSource))
            : UnityEngine.Object.FindObjectsOfType(typeof(AudioSource));

        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource audioSource = sources[i] as AudioSource;
            if (audioSource == null) continue;

            SLOTMusic sLOTMusic = audioSource.gameObject.GetComponent(typeof(SLOTMusic)) as SLOTMusic;
            if (sLOTMusic != null && sLOTMusic.FindAudioSource(audioSource))
            {
                audioSource.volume = musicVol * sLOTMusic.volume;
                continue;
            }

            // NOTE: flag2 is preserved from the original — it was always false,
            // meaning all non-music sources get sfxVol. If VO sources ever need
            // separate volume tracking here, tag them with a component and check for it.
            bool isVO = false;
            audioSource.volume = isVO ? voVol : sfxVol;
        }
    }

    public void CreateListener()
    {
        if (listener != null) return;
        listener = UnityEngine.Object.FindObjectOfType(typeof(AudioListener)) as AudioListener;
        if (listener == null)
        {
            Camera camera = Camera.main ?? UnityEngine.Object.FindObjectOfType(typeof(Camera)) as Camera;
            if (camera != null)
                listener = camera.gameObject.AddComponent(typeof(AudioListener)) as AudioListener;
        }
    }

    public void SetMusicVolumeMasterAudio()
    {
        musicVolume = 1f - musicVolume;
        prevMusicVolume = musicVolume;
        Serialize();
    }

    public void SetSoundVolumeMasterAudio()
    {
        soundVolume = 1f - soundVolume;
        prevSoundVolume = soundVolume;
        Serialize();
    }

    public void SetMusicVolumeMasterAudio(float volume)
    {
        musicVolume = volume;
        Singleton<SLOTMusic>.Instance.musicAudioSources[0].volume = volume;
        prevMusicVolume = musicVolume;
        Serialize();
    }

    public void SetSoundVolumeMasterAudio(float volume)
    {
        soundVolume = volume;
        prevSoundVolume = soundVolume;
        Serialize();
    }

    public void OnInstantiate(UnityEngine.Object clone)
    {
        if (clone == null) return;

        GameObject gameObject = clone as GameObject;
        if (gameObject == null)
        {
            Transform t = clone as Transform;
            if (t != null)
            {
                gameObject = t.gameObject;
            }
            else
            {
                MonoBehaviour mb = clone as MonoBehaviour;
                if (mb != null) gameObject = mb.gameObject;
            }
        }
        if (gameObject == null) return;

        if (useMasterAudio)
        {
            Component[] components = gameObject.GetComponentsInChildren(typeof(AudioSource));
            foreach (Component component in components)
            {
                AudioSource audioSource = component as AudioSource;
                if (audioSource != null && audioSource.clip != null && audioSource.playOnAwake)
                {
                    audioSource.Stop();
                    PlaySound(audioSource.clip.name);
                    UnityEngine.Object.Destroy(audioSource);
                }
            }
        }
        else
        {
            UpdateAudioVolumes(gameObject);
        }
    }

    public bool IsInitialized() { return mInitialized; }

    public void PlayErrorSound() { PlaySound("ui/UI_ErrorSound"); }
}
