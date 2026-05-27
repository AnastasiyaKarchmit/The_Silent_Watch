using UnityEngine;

namespace Core.Audio.Configs
{
    [CreateAssetMenu(fileName = "MusicConfig", menuName = "Configs/Audio/Music Config")]
    public sealed class MusicConfig : ScriptableObject
    {
        [SerializeField] private AudioClip clip;

        public AudioClip Clip => clip;
    }
}