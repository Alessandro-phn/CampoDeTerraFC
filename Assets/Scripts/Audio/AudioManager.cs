using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CampoDeTerraFC.Core;

namespace CampoDeTerraFC.Audio
{
    /// <summary>
    /// Gerencia todo o áudio do jogo: música, efeitos sonoros e sons ambiente.
    /// Usa Object Pooling para AudioSources de SFX.
    /// Suporta fade in/out, ducking e transições suaves.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        // ===== COMPONENTES =====

        private AudioSource _musicSource;
        private AudioSource _ambientSource;
        private List<AudioSource> _sfxPool = new List<AudioSource>();

        // ===== CONFIGURAÇÃO =====

        [Header("Clips de Música")]
        [SerializeField] private AudioClip _mainMenuMusic;
        [SerializeField] private AudioClip _matchMusic;
        [SerializeField] private AudioClip _victoryMusic;
        [SerializeField] private AudioClip _defeatMusic;

        [Header("Clips de SFX")]
        [SerializeField] private AudioClip _kickShortSFX;
        [SerializeField] private AudioClip _kickStrongSFX;
        [SerializeField] private AudioClip _kickHeaderSFX;
        [SerializeField] private AudioClip _goalSFX;
        [SerializeField] private AudioClip _postHitSFX;
        [SerializeField] private AudioClip _whistleStartSFX;
        [SerializeField] private AudioClip _whistleFinalSFX;
        [SerializeField] private AudioClip _crowdGoalSFX;
        [SerializeField] private AudioClip _crowdOohSFX;
        [SerializeField] private AudioClip _footstepDirtSFX;
        [SerializeField] private AudioClip _slideSFX;
        [SerializeField] private AudioClip _netShakeSFX;

        [Header("Clips de Ambiente")]
        [SerializeField] private AudioClip _birdsSFX;
        [SerializeField] private AudioClip _windSFX;
        [SerializeField] private AudioClip _crowdIdleSFX;
        [SerializeField] private AudioClip _rainSFX;

        [Header("Volumes")]
        [SerializeField, Range(0f, 1f)] private float _masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float _musicVolume = 0.7f;
        [SerializeField, Range(0f, 1f)] private float _sfxVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float _ambientVolume = 0.5f;

        // ===== POOL DE SFX =====

        private const int SFX_POOL_SIZE = 12;
        private int _sfxPoolIndex;

        // ===== ESTADO =====

        private bool _isMusicPaused;
        private Coroutine _fadeMusicCoroutine;

        // ===== PROPRIEDADES =====

        public float MasterVolume
        {
            get => _masterVolume;
            set
            {
                _masterVolume = Mathf.Clamp01(value);
                ApplyVolumes();
            }
        }

        public float MusicVolume
        {
            get => _musicVolume;
            set
            {
                _musicVolume = Mathf.Clamp01(value);
                if (_musicSource != null)
                    _musicSource.volume = _musicVolume * _masterVolume;
            }
        }

        public float SFXVolume
        {
            get => _sfxVolume;
            set => _sfxVolume = Mathf.Clamp01(value);
        }

        // ===== UNITY LIFECYCLE =====

        private void Awake()
        {
            CreateAudioSources();
        }

        // ===== INICIALIZAÇÃO =====

        public void Initialize()
        {
            Debug.Log("[AudioManager] Inicializado.");
        }

        /// <summary>
        /// Cria os AudioSources dedicados e o pool de SFX.
        /// </summary>
        private void CreateAudioSources()
        {
            // Música
            _musicSource = CreateAudioSource("MusicSource", loop: true, volume: _musicVolume);

            // Ambiente
            _ambientSource = CreateAudioSource("AmbientSource", loop: true, volume: _ambientVolume);

            // Pool de SFX
            for (int i = 0; i < SFX_POOL_SIZE; i++)
            {
                AudioSource src = CreateAudioSource($"SFX_{i}", loop: false, volume: _sfxVolume);
                _sfxPool.Add(src);
            }
        }

        private AudioSource CreateAudioSource(string sourceName, bool loop, float volume)
        {
            GameObject obj = new GameObject(sourceName);
            obj.transform.SetParent(transform);

            AudioSource src = obj.AddComponent<AudioSource>();
            src.loop = loop;
            src.volume = volume * _masterVolume;
            src.playOnAwake = false;
            src.spatialBlend = 0f; // 2D (stereo)

            return src;
        }

        // ===== MÚSICA =====

        /// <summary>
        /// Inicia a música do tipo especificado com fade in.
        /// </summary>
        public void PlayMusic(MusicType type)
        {
            AudioClip clip = type switch
            {
                MusicType.MainMenu => _mainMenuMusic,
                MusicType.Match => _matchMusic,
                MusicType.Victory => _victoryMusic,
                MusicType.Defeat => _defeatMusic,
                _ => null
            };

            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] Clip de música não configurado para: {type}");
                return;
            }

            if (_fadeMusicCoroutine != null)
                StopCoroutine(_fadeMusicCoroutine);

            _fadeMusicCoroutine = StartCoroutine(CrossfadeMusic(clip, fadeDuration: 1.5f));
        }

        /// <summary>
        /// Faz crossfade suave entre músicas.
        /// </summary>
        private IEnumerator CrossfadeMusic(AudioClip newClip, float fadeDuration)
        {
            float startVolume = _musicSource.volume;
            float targetVolume = _musicVolume * _masterVolume;

            // Fade out
            float elapsed = 0f;
            while (elapsed < fadeDuration * 0.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                _musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / (fadeDuration * 0.5f));
                yield return null;
            }

            _musicSource.Stop();
            _musicSource.clip = newClip;
            _musicSource.Play();

            // Fade in
            elapsed = 0f;
            while (elapsed < fadeDuration * 0.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                _musicSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / (fadeDuration * 0.5f));
                yield return null;
            }

            _musicSource.volume = targetVolume;
        }

        public void PauseMusic()
        {
            if (_musicSource.isPlaying)
            {
                _musicSource.Pause();
                _isMusicPaused = true;
            }
        }

        public void ResumeMusic()
        {
            if (_isMusicPaused)
            {
                _musicSource.UnPause();
                _isMusicPaused = false;
            }
        }

        public void StopMusic()
        {
            _musicSource.Stop();
        }

        // ===== SFX =====

        /// <summary>
        /// Reproduz um efeito sonoro usando o pool de AudioSources.
        /// </summary>
        public void PlaySFX(SFXType type, float pitchVariation = 0.05f)
        {
            AudioClip clip = GetSFXClip(type);
            if (clip == null) return;

            AudioSource src = GetNextSFXSource();
            src.clip = clip;
            src.volume = _sfxVolume * _masterVolume;
            src.pitch = 1f + UnityEngine.Random.Range(-pitchVariation, pitchVariation);
            src.Play();
        }

        /// <summary>
        /// Reproduz um SFX em uma posição 3D do mundo.
        /// </summary>
        public void PlaySFXAtPosition(SFXType type, Vector3 position)
        {
            AudioClip clip = GetSFXClip(type);
            if (clip == null) return;

            AudioSource.PlayClipAtPoint(clip, position, _sfxVolume * _masterVolume);
        }

        private AudioClip GetSFXClip(SFXType type)
        {
            return type switch
            {
                SFXType.KickShort => _kickShortSFX,
                SFXType.KickStrong => _kickStrongSFX,
                SFXType.KickHeader => _kickHeaderSFX,
                SFXType.Goal => _goalSFX,
                SFXType.PostHit => _postHitSFX,
                SFXType.WhistleStart => _whistleStartSFX,
                SFXType.FinalWhistle => _whistleFinalSFX,
                SFXType.CrowdGoal => _crowdGoalSFX,
                SFXType.CrowdOoh => _crowdOohSFX,
                SFXType.FootstepDirt => _footstepDirtSFX,
                SFXType.Slide => _slideSFX,
                SFXType.NetShake => _netShakeSFX,
                _ => null
            };
        }

        /// <summary>
        /// Retorna o próximo AudioSource disponível do pool (round-robin).
        /// </summary>
        private AudioSource GetNextSFXSource()
        {
            _sfxPoolIndex = (_sfxPoolIndex + 1) % SFX_POOL_SIZE;
            AudioSource src = _sfxPool[_sfxPoolIndex];

            // Se estiver tocando algo importante, pega o próximo
            int attempts = 0;
            while (src.isPlaying && attempts < SFX_POOL_SIZE)
            {
                _sfxPoolIndex = (_sfxPoolIndex + 1) % SFX_POOL_SIZE;
                src = _sfxPool[_sfxPoolIndex];
                attempts++;
            }

            return src;
        }

        // ===== AMBIENTE =====

        /// <summary>
        /// Inicia o som ambiente do campo.
        /// </summary>
        public void PlayAmbient(AmbientType type)
        {
            AudioClip clip = type switch
            {
                AmbientType.Birds => _birdsSFX,
                AmbientType.Wind => _windSFX,
                AmbientType.CrowdIdle => _crowdIdleSFX,
                AmbientType.Rain => _rainSFX,
                _ => null
            };

            if (clip == null) return;

            _ambientSource.clip = clip;
            _ambientSource.volume = _ambientVolume * _masterVolume;
            _ambientSource.Play();
        }

        // ===== VOLUME GERAL =====

        private void ApplyVolumes()
        {
            if (_musicSource != null)
                _musicSource.volume = _musicVolume * _masterVolume;

            if (_ambientSource != null)
                _ambientSource.volume = _ambientVolume * _masterVolume;
        }
    }

    // ===== ENUMS =====

    public enum MusicType
    {
        MainMenu,
        Match,
        Victory,
        Defeat,
        Championship
    }

    public enum SFXType
    {
        KickShort,
        KickStrong,
        KickHeader,
        Goal,
        PostHit,
        WhistleStart,
        FinalWhistle,
        CrowdGoal,
        CrowdOoh,
        FootstepDirt,
        Slide,
        NetShake,
        ButtonClick,
        MenuConfirm
    }

    public enum AmbientType
    {
        Birds,
        Wind,
        CrowdIdle,
        Rain,
        Dogs,
        Children
    }
}
