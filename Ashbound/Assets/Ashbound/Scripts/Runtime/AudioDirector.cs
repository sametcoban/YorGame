using System;
using UnityEngine;

namespace Ashbound
{
    // Small generated cues keep the prototype self-contained and asset-license free.
    internal sealed class AudioDirector : IDisposable
    {
        private readonly AudioSource source;
        private readonly AudioClip swing;
        private readonly AudioClip impact;
        private readonly AudioClip perfect;
        private readonly AudioClip hurt;
        private readonly AudioClip end;
        private readonly AudioClip ultimate;
        private readonly AudioClip drone;
        private readonly AudioSource ambience;
        internal bool Muted { get; private set; }

        internal AudioDirector(GameObject host)
        {
            source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            ambience = host.AddComponent<AudioSource>();
            ambience.playOnAwake = false;
            ambience.loop = true;
            ambience.volume = 0.12f;
            swing = Tone("Sword", 0.14f, 210f, 0.7f, 0.1f);
            impact = Tone("Bone", 0.18f, 85f, 0.55f, 0.55f);
            perfect = Tone("Soul claimed", 0.35f, 660f, 0.06f, 0.6f);
            hurt = Tone("Wound", 0.32f, 55f, 0.2f, 0.7f);
            end = Tone("Oath broken", 0.9f, 42f, 0.05f, 0.6f);
            ultimate = Tone("Oathbreaker wave", 0.6f, 140f, 0.45f, 0.8f);
            drone = Drone();
            ambience.clip = drone;
            SetMuted(PlayerPrefs.GetInt("Ashbound.Muted", 0) != 0);
            ambience.Play();
        }

        internal void SetMuted(bool muted)
        {
            Muted = muted;
            source.mute = ambience.mute = muted;
            PlayerPrefs.SetInt("Ashbound.Muted", muted ? 1 : 0);
        }

        internal void Swing() { source.PlayOneShot(swing, 0.23f); }
        internal void Impact(bool precise)
        {
            source.PlayOneShot(impact, 0.5f);
            if (precise) source.PlayOneShot(perfect, 0.3f);
        }
        internal void Hurt() { source.PlayOneShot(hurt, 0.5f); }
        internal void End() { source.PlayOneShot(end, 0.5f); }
        internal void Ultimate()
        {
            source.PlayOneShot(ultimate, 0.7f);
            source.PlayOneShot(perfect, 0.35f);
        }

        private static AudioClip Tone(string name, float length, float frequency, float noise, float volume)
        {
            const int rate = 22050;
            var samples = new float[(int)(rate * length)];
            var random = new System.Random(47);
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                float envelope = Mathf.Min(t * 200f, 1f) * Mathf.Pow(1f - t / length, 2f);
                samples[i] = ((float)Math.Sin(t * frequency * Math.PI * 2) * (1f - noise)
                    + ((float)random.NextDouble() * 2f - 1f) * noise) * envelope * volume;
            }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip Drone()
        {
            const int rate = 22050;
            const int seconds = 4;
            var samples = new float[rate * seconds];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                samples[i] = Mathf.Sin(t * Mathf.PI * 2f * 55f) * 0.18f
                    + Mathf.Sin(t * Mathf.PI * 2f * 82.5f) * 0.08f
                    + Mathf.Sin(t * Mathf.PI * 2f * 110f) * 0.035f;
            }
            var clip = AudioClip.Create("The ruined chapel", samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public void Dispose()
        {
            source.Stop();
            ambience.Stop();
            foreach (var clip in new[] { swing, impact, perfect, hurt, end, ultimate, drone })
                UnityEngine.Object.Destroy(clip);
        }
    }
}
