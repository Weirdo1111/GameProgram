using System.Collections.Generic;
using UnityEngine;

// Audio responsibilities for ZombieStormGameController.
public sealed partial class ZombieStormGameController
{
    private const float GameplayMusicVolumeMultiplier = 0.6f;

    private readonly Dictionary<string, AudioClip> sfx = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, float> sfxLastPlayed = new Dictionary<string, float>();

    private AudioSource audioSource;
    private AudioSource musicSource;
    private bool useGameplayMusicVolume;

    // Plays a one-shot sound effect through the shared audio source after applying
    // the current master volume and the caller's per-effect volume.
    public void PlaySfx(string key, float volume = 1f, float minInterval = 0.02f)
    {
        if (audioSource == null)
        {
            return;
        }

        AudioClip clip;
        if (!sfx.TryGetValue(key, out clip) || clip == null)
        {
            return;
        }

        float now = Time.unscaledTime;
        float last;
        if (sfxLastPlayed.TryGetValue(key, out last) && now - last < minInterval)
        {
            return;
        }

        sfxLastPlayed[key] = now;
        audioSource.pitch = UnityEngine.Random.Range(0.96f, 1.04f);
        audioSource.PlayOneShot(clip, sfxMuted ? 0f : Mathf.Clamp01(volume * masterVolume * sfxVolume));
    }

    // Generates sound clips for attacks, pickups, upgrades, hits, and feedback.
    private void CreateAudioClips()
    {
        sfx.Clear();
        sfx["shoot"] = CreateSynthClip("zs_shoot", 0.075f, 820f, 1180f, 0.45f, 0.08f, ZombieStormWave.Square);
        sfx["normal_attack"] = sfx["shoot"];
        sfx["fire_bomb"] = CreateSynthClip("zs_fire_bomb", 0.18f, 420f, 110f, 0.62f, 0.28f, ZombieStormWave.Saw);
        sfx["enemy_death"] = CreateSynthClip("zs_enemy_death", 0.16f, 180f, 52f, 0.66f, 0.38f, ZombieStormWave.Noise);
        sfx["enemy_death_alt"] = sfx["enemy_death"];
        sfx["story_transition"] = CreateSynthClip("zs_story_transition", 0.12f, 460f, 1260f, 0.44f, 0.12f, ZombieStormWave.Saw);
        sfx["hit"] = CreateSynthClip("zs_hit", 0.07f, 190f, 82f, 0.55f, 0.42f, ZombieStormWave.Noise);
        sfx["pickup"] = CreateSynthClip("zs_pickup", 0.105f, 620f, 1240f, 0.35f, 0.02f, ZombieStormWave.Triangle);
        sfx["hurt"] = CreateSynthClip("zs_hurt", 0.16f, 190f, 74f, 0.62f, 0.24f, ZombieStormWave.Saw);
        sfx["level_up"] = CreateArpeggioClip("zs_level_up", new[] { 520f, 780f, 1040f, 1560f }, 0.34f, 0.48f);
        sfx["upgrade"] = CreateArpeggioClip("zs_upgrade", new[] { 440f, 660f, 990f }, 0.24f, 0.42f);
        sfx["boom"] = CreateSynthClip("zs_boom", 0.28f, 110f, 38f, 0.8f, 0.58f, ZombieStormWave.Noise);
        sfx["lightning"] = CreateSynthClip("zs_lightning", 0.16f, 1380f, 420f, 0.46f, 0.22f, ZombieStormWave.Saw);
        sfx["ultimate"] = CreateSynthClip("zs_ultimate", 0.46f, 180f, 58f, 0.74f, 0.32f, ZombieStormWave.Saw);
        sfx["fire_tornado"] = sfx["ultimate"];
        sfx["elite_down"] = CreateArpeggioClip("zs_elite_down", new[] { 760f, 570f, 380f }, 0.2f, 0.48f);
        sfx["boss_down"] = CreateArpeggioClip("zs_boss_down", new[] { 360f, 540f, 720f, 1080f }, 0.42f, 0.56f);
        sfx["victory"] = CreateArpeggioClip("zs_victory", new[] { 520f, 660f, 780f, 1040f, 1320f }, 0.62f, 0.58f);
        sfx["fail"] = CreateArpeggioClip("zs_fail", new[] { 330f, 247f, 196f }, 0.42f, 0.62f);
        sfx["start"] = CreateArpeggioClip("zs_start", new[] { 330f, 495f, 660f }, 0.26f, 0.34f);

        OverrideSfxFromResources("normal_attack", "Audio/normal_attack");
        OverrideSfxFromResources("fire_tornado", "Audio/fire_tornado");
        OverrideSfxFromResources("fire_bomb", "Audio/fire_bomb");
        OverrideSfxFromResources("enemy_death", "Audio/enemy_death");
        OverrideSfxFromResources("enemy_death_alt", "Audio/enemy_death_alt");
        OverrideSfxFromResources("hurt", "Audio/player_hurt");
        OverrideSfxFromResources("level_up", "Audio/level_up");
        OverrideSfxFromResources("upgrade", "Audio/level_up");
        OverrideSfxFromResources("story_transition", "Audio/story_transition");
        OverrideSfxFromResources("victory", "Audio/victory");
        OverrideSfxFromResources("fail", "Audio/defeat");
        StartBackgroundMusic();
    }

    // Replaces a synthesized fallback with an imported Resources audio clip when available.
    private void OverrideSfxFromResources(string key, string resourcePath)
    {
        AudioClip clip = Resources.Load<AudioClip>(resourcePath);
        if (clip != null)
        {
            sfx[key] = clip;
        }
    }

    // Starts the imported background track once and keeps it looping from the main menu onward.
    private void StartBackgroundMusic()
    {
        if (musicSource == null)
        {
            return;
        }

        AudioClip clip = Resources.Load<AudioClip>("Audio/background_music");
        if (clip == null)
        {
            return;
        }

        musicSource.clip = clip;
        UpdateMusicVolume();
        musicSource.Play();
    }

    // Applies the saved sliders and lowers the music during a run so combat sounds stay clear.
    private void UpdateMusicVolume()
    {
        if (musicSource != null)
        {
            float stateMultiplier = useGameplayMusicVolume ? GameplayMusicVolumeMultiplier : 1f;
            musicSource.volume = Mathf.Clamp01(masterVolume * musicVolume * stateMultiplier);
        }
    }

    // Generates a short synthetic sound clip from frequency and wave settings.
    private AudioClip CreateSynthClip(string clipName, float duration, float startFrequency, float endFrequency, float volume, float noiseAmount, ZombieStormWave wave)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * sampleRate));
        float[] samples = new float[sampleCount];
        float phase = 0f;
        uint noiseState = 22222u;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)(sampleCount - 1);
            float frequency = Mathf.Lerp(startFrequency, endFrequency, t);
            phase += frequency / sampleRate;
            phase -= Mathf.Floor(phase);
            float envelope = Mathf.Pow(1f - t, 1.7f) * Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, t * 24f));
            float tone = EvaluateWave(wave, phase);
            float noise = NextNoise(ref noiseState);
            samples[i] = Mathf.Clamp((tone * (1f - noiseAmount) + noise * noiseAmount) * envelope * volume, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // Generates a short arpeggio sound from a list of notes.
    private AudioClip CreateArpeggioClip(string clipName, float[] notes, float duration, float volume)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * sampleRate));
        float[] samples = new float[sampleCount];
        float phase = 0f;
        int noteCount = Mathf.Max(1, notes.Length);

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)(sampleCount - 1);
            int noteIndex = Mathf.Clamp(Mathf.FloorToInt(t * noteCount), 0, noteCount - 1);
            float noteT = (t * noteCount) - noteIndex;
            float frequency = notes[noteIndex];
            phase += frequency / sampleRate;
            phase -= Mathf.Floor(phase);
            float envelope = Mathf.Pow(1f - noteT, 1.25f) * Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, noteT * 18f)) * Mathf.Pow(1f - t * 0.2f, 1.1f);
            samples[i] = EvaluateWave(ZombieStormWave.Triangle, phase) * envelope * volume;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // Evaluates one audio sample for the selected oscillator wave shape.
    private static float EvaluateWave(ZombieStormWave wave, float phase)
    {
        if (wave == ZombieStormWave.Square)
        {
            return phase < 0.5f ? 1f : -1f;
        }

        if (wave == ZombieStormWave.Triangle)
        {
            return 1f - Mathf.Abs(phase * 4f - 2f);
        }

        if (wave == ZombieStormWave.Saw)
        {
            return phase * 2f - 1f;
        }

        if (wave == ZombieStormWave.Noise)
        {
            return 0f;
        }

        return Mathf.Sin(phase * Mathf.PI * 2f);
    }

    // Generates pseudo-random noise used to make synth sounds punchier.
    private static float NextNoise(ref uint state)
    {
        state = state * 1664525u + 1013904223u;
        return ((state >> 8) / 16777215f) * 2f - 1f;
    }
}
