using UnityEngine;

namespace NeonZigzag
{
    /// <summary>Procedurally generated blips — keeps the project asset-free.</summary>
    public class Sfx : MonoBehaviour
    {
        const int Rate = 44100;

        AudioSource[] _sources;
        int _next;
        AudioClip _pickup, _turn, _die, _start;

        public static Sfx Create(Transform parent)
        {
            var go = new GameObject("Sfx");
            go.transform.SetParent(parent, false);
            var sfx = go.AddComponent<Sfx>();

            sfx._sources = new AudioSource[6];
            for (int i = 0; i < sfx._sources.Length; i++)
            {
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                sfx._sources[i] = src;
            }

            sfx._pickup = Tone("pickup", 880f, 1320f, 0.16f, 9f, 0.15f, 0.30f);
            sfx._turn = Tone("turn", 300f, 220f, 0.07f, 22f, 0.5f, 0.14f);
            sfx._die = Tone("die", 320f, 60f, 0.55f, 4.5f, 0.75f, 0.32f);
            sfx._start = Tone("start", 440f, 880f, 0.22f, 6f, 0.1f, 0.28f);
            return sfx;
        }

        public void Pickup(int combo) => Play(_pickup, 1f + Mathf.Min(combo, 12) * 0.07f, 1f);
        public void Turn() => Play(_turn, Random.Range(0.94f, 1.06f), 0.6f);
        public void Die() => Play(_die, 1f, 1f);
        public void Start() => Play(_start, 1f, 1f);

        void Play(AudioClip clip, float pitch, float volume)
        {
            if (clip == null) return;
            var src = _sources[_next];
            _next = (_next + 1) % _sources.Length;
            src.pitch = pitch;
            src.PlayOneShot(clip, volume);
        }

        static AudioClip Tone(string name, float fromHz, float toHz, float seconds,
                              float decay, float squareness, float gain)
        {
            int count = Mathf.CeilToInt(Rate * seconds);
            var data = new float[count];
            float phase = 0f;

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                float hz = Mathf.Lerp(fromHz, toHz, t * t);
                phase += hz / Rate * Mathf.PI * 2f;

                float wave = Mathf.Sin(phase);
                if (squareness > 0f) wave = Mathf.Lerp(wave, Mathf.Sign(wave), squareness);

                float attack = Mathf.Min(1f, t * 80f);
                data[i] = wave * Mathf.Exp(-decay * t) * attack * gain;
            }

            var clip = AudioClip.Create(name, count, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
