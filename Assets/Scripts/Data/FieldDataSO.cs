using System;
using System.Collections;
using UnityEngine;
using CampoDeTerraFC.Ball;

namespace CampoDeTerraFC.Data
{
    // ==========================================================================
    //  FIELD DATA SO
    // ==========================================================================

    /// <summary>
    /// Dados de um campo de jogo como ScriptableObject.
    /// Define superfície, prefab, condições climáticas disponíveis e peculiaridades.
    /// </summary>
    [CreateAssetMenu(menuName = "Campo de Terra FC/Data/Field Data", fileName = "Field_")]
    public class FieldDataSO : ScriptableObject
    {
        [Header("Identificação")]
        public string FieldName = "Campão do Bairro";

        [TextArea]
        public string Description = "O campo de pelada mais famoso do bairro.";

        [Tooltip("Imagem de preview do campo (para tela de seleção).")]
        public Sprite PreviewImage;

        [Header("Superfície")]
        public SurfaceType SurfaceType = SurfaceType.Dirt;

        [Tooltip("Coeficiente de atrito desta superfície (sobrescreve GameConfig).")]
        [Range(0.1f, 1f)]
        public float FrictionCoefficient = 0.6f;

        [Tooltip("Elasticidade do bounce específica deste campo.")]
        [Range(0f, 1f)]
        public float BounceModifier = 1f;

        [Header("Prefab")]
        [Tooltip("Prefab da cena do campo.")]
        public GameObject FieldPrefab;

        [Header("Clima Disponível")]
        public WeatherType[] AvailableWeathers = { WeatherType.Sunny };

        [Header("Peculiaridades do Campo")]
        [Tooltip("Se o campo tem irregularidades que desviam a bola.")]
        public bool HasIrregularities = false;

        [Tooltip("Intensidade das irregularidades (0 = nenhuma, 1 = extrema).")]
        [Range(0f, 1f)]
        public float IrregularityIntensity = 0f;

        [Tooltip("Se o campo tem laterais delimitadas (false = pelada sem linha).")]
        public bool HasSideLines = true;

        [Tooltip("Se usa traves de madeira (som diferente ao bater).")]
        public bool WoodenGoalPosts = true;

        [Header("Atmosfera")]
        [Tooltip("Tipo de audiência/vizinhança.")]
        public AudienceType AudienceType = AudienceType.Neighborhood;

        [Tooltip("Capacidade máxima de torcida (afeta sons ambiente).")]
        [Range(0, 200)]
        public int AudienceCapacity = 30;
    }

    // ==========================================================================
    //  WEATHER SYSTEM
    // ==========================================================================

    /// <summary>
    /// Sistema de clima dinâmico.
    /// Afeta: visibilidade, atrito da bola, velocidade dos jogadores, partículas.
    /// </summary>
    public sealed class WeatherSystem : MonoBehaviour
    {
        // ===== REFERÊNCIAS =====

        [Header("Partículas de Clima")]
        [SerializeField] private ParticleSystem _rainParticles;
        [SerializeField] private ParticleSystem _dustWindParticles;
        [SerializeField] private ParticleSystem _fogParticles;

        [Header("Iluminação")]
        [SerializeField] private Light _sunLight;
        [SerializeField] private Gradient _skyColorByWeather;

        [Header("Efeito no Campo")]
        [SerializeField] private Material _fieldMaterial;

        // ===== ESTADO =====

        public WeatherType CurrentWeather { get; private set; } = WeatherType.Sunny;

        private BallController _ball;
        private Coroutine _transitionCoroutine;

        // ===== CONSTANTES DE MODIFICADORES =====

        // Modificadores de física por clima
        private static readonly float[] FRICTION_MODIFIERS =
        {
            1.0f,  // Sunny
            1.3f,  // Overcast
            1.6f,  // Rainy (lama)
            1.1f,  // Windy
            1.0f,  // Night
            1.8f   // HeavyRain
        };

        private static readonly float[] PLAYER_SPEED_MODIFIERS =
        {
            1.0f,  // Sunny
            0.97f, // Overcast
            0.88f, // Rainy
            0.93f, // Windy
            1.0f,  // Night
            0.80f  // HeavyRain
        };

        // ===== INICIALIZAÇÃO =====

        private void Start()
        {
            _ball = FindObjectOfType<BallController>();
        }

        // ===== API PÚBLICA =====

        /// <summary>
        /// Define o clima com transição suave.
        /// </summary>
        public void SetWeather(WeatherType weather, float transitionDuration = 2f)
        {
            if (_transitionCoroutine != null)
                StopCoroutine(_transitionCoroutine);

            _transitionCoroutine = StartCoroutine(TransitionToWeather(weather, transitionDuration));
        }

        /// <summary>
        /// Define o clima imediatamente (sem transição).
        /// </summary>
        public void SetWeatherImmediate(WeatherType weather)
        {
            CurrentWeather = weather;
            ApplyWeatherEffects(weather, 1f);
        }

        // ===== TRANSIÇÃO =====

        private IEnumerator TransitionToWeather(WeatherType newWeather, float duration)
        {
            WeatherType oldWeather = CurrentWeather;
            CurrentWeather = newWeather;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                ApplyWeatherEffects(newWeather, t);
                yield return null;
            }

            ApplyWeatherEffects(newWeather, 1f);
            Debug.Log($"[WeatherSystem] Clima alterado para: {newWeather}");
        }

        // ===== APLICAÇÃO DOS EFEITOS =====

        private void ApplyWeatherEffects(WeatherType weather, float blend)
        {
            UpdateParticles(weather, blend);
            UpdateLighting(weather, blend);
            UpdateFieldMaterial(weather, blend);
        }

        private void UpdateParticles(WeatherType weather, float blend)
        {
            // Chuva
            bool shouldRain = weather == WeatherType.Rainy || weather == WeatherType.HeavyRain;
            if (_rainParticles != null)
            {
                if (shouldRain && !_rainParticles.isPlaying) _rainParticles.Play();
                else if (!shouldRain && _rainParticles.isPlaying) _rainParticles.Stop();

                if (shouldRain)
                {
                    ParticleSystem.EmissionModule em = _rainParticles.emission;
                    em.rateOverTime = Mathf.Lerp(50f, 200f, blend) *
                        (weather == WeatherType.HeavyRain ? 2f : 1f);
                }
            }

            // Vento
            bool shouldWind = weather == WeatherType.Windy;
            if (_dustWindParticles != null)
            {
                if (shouldWind && !_dustWindParticles.isPlaying) _dustWindParticles.Play();
                else if (!shouldWind && _dustWindParticles.isPlaying) _dustWindParticles.Stop();
            }
        }

        private void UpdateLighting(WeatherType weather, float blend)
        {
            if (_sunLight == null) return;

            float targetIntensity = weather switch
            {
                WeatherType.Sunny => 1.2f,
                WeatherType.Overcast => 0.6f,
                WeatherType.Rainy => 0.4f,
                WeatherType.Windy => 0.9f,
                WeatherType.Night => 0.1f,
                WeatherType.HeavyRain => 0.2f,
                _ => 1f
            };

            _sunLight.intensity = Mathf.Lerp(_sunLight.intensity, targetIntensity, blend);

            // Cor da luz
            Color targetColor = weather == WeatherType.Night
                ? new Color(0.4f, 0.5f, 0.8f)   // Azul noturno
                : weather == WeatherType.Sunny
                    ? new Color(1f, 0.95f, 0.8f) // Amarelo solar
                    : new Color(0.8f, 0.85f, 0.9f); // Cinza nublado

            _sunLight.color = Color.Lerp(_sunLight.color, targetColor, blend);
        }

        private void UpdateFieldMaterial(WeatherType weather, float blend)
        {
            if (_fieldMaterial == null) return;

            // Escurece o campo na chuva (lama)
            float wetness = (weather == WeatherType.Rainy || weather == WeatherType.HeavyRain)
                ? Mathf.Lerp(0f, 0.6f, blend)
                : 0f;

            if (_fieldMaterial.HasProperty("_Wetness"))
                _fieldMaterial.SetFloat("_Wetness", wetness);
        }

        // ===== GETTERS DE MODIFICADORES =====

        /// <summary>
        /// Retorna o modificador de atrito do clima atual.
        /// </summary>
        public float GetFrictionModifier()
        {
            return FRICTION_MODIFIERS[(int)CurrentWeather];
        }

        /// <summary>
        /// Retorna o modificador de velocidade dos jogadores no clima atual.
        /// </summary>
        public float GetPlayerSpeedModifier()
        {
            return PLAYER_SPEED_MODIFIERS[(int)CurrentWeather];
        }

        /// <summary>
        /// Retorna uma força de vento (afeta a trajetória da bola em dias ventosos).
        /// </summary>
        public Vector3 GetWindForce()
        {
            if (CurrentWeather != WeatherType.Windy) return Vector3.zero;

            return new Vector3(
                UnityEngine.Random.Range(-2f, 2f),
                0f,
                UnityEngine.Random.Range(-1f, 1f));
        }
    }

    // ===== ENUMS =====

    public enum WeatherType
    {
        Sunny = 0,
        Overcast = 1,
        Rainy = 2,
        Windy = 3,
        Night = 4,
        HeavyRain = 5
    }

    public enum AudienceType
    {
        None,
        Neighborhood,   // Vizinhos assistindo
        SmallCrowd,     // Galera do bairro
        School,         // Pátio da escola
        Street          // Rua fechada
    }
}
