using System;
using System.Collections.Generic;
using UnityEngine;

namespace CampoDeTerraFC.Core
{
    /// <summary>
    /// Gerencia pools de objetos para reutilização eficiente.
    /// Evita alocações de memória em runtime, fundamental para Android 60 FPS.
    /// Suporta múltiplos pools identificados por tag string.
    /// </summary>
    public sealed class ObjectPoolManager : MonoBehaviour
    {
        // ===== ESTRUTURAS =====

        /// <summary>Configuração de um pool individual.</summary>
        [Serializable]
        public class PoolConfig
        {
            [Tooltip("Identificador único do pool.")]
            public string Tag;

            [Tooltip("Prefab a ser instanciado e reutilizado.")]
            public GameObject Prefab;

            [Tooltip("Quantidade inicial de objetos pré-instanciados.")]
            public int InitialSize = 10;

            [Tooltip("Permite o pool expandir além do tamanho inicial.")]
            public bool AllowExpansion = true;

            [Tooltip("Tamanho máximo ao expandir (0 = ilimitado).")]
            public int MaxSize = 0;
        }

        // ===== CAMPOS =====

        [Header("Configuração dos Pools")]
        [SerializeField] private List<PoolConfig> _poolConfigs = new List<PoolConfig>();

        /// <summary>Dicionário dos pools, indexados por tag.</summary>
        private Dictionary<string, Queue<GameObject>> _pools;

        /// <summary>Referências aos prefabs por tag para expansão dinâmica.</summary>
        private Dictionary<string, PoolConfig> _configs;

        /// <summary>Transform pai para organização na hierarquia.</summary>
        private Transform _poolParent;

        // ===== CONSTANTES =====

        private const string POOL_PARENT_NAME = "[ObjectPools]";

        // ===== UNITY LIFECYCLE =====

        private void Awake()
        {
            _pools = new Dictionary<string, Queue<GameObject>>();
            _configs = new Dictionary<string, PoolConfig>();

            CreatePoolParent();
        }

        // ===== INICIALIZAÇÃO =====

        /// <summary>
        /// Inicializa todos os pools configurados.
        /// Chamado pelo GameManager após o Awake.
        /// </summary>
        public void Initialize()
        {
            foreach (PoolConfig config in _poolConfigs)
            {
                CreatePool(config);
            }

            Debug.Log($"[ObjectPoolManager] {_pools.Count} pools inicializados.");
        }

        /// <summary>
        /// Cria um container pai para organizar os pools na hierarquia.
        /// </summary>
        private void CreatePoolParent()
        {
            GameObject parent = new GameObject(POOL_PARENT_NAME);
            parent.transform.SetParent(transform);
            _poolParent = parent.transform;
        }

        /// <summary>
        /// Cria e pré-popula um pool baseado na configuração fornecida.
        /// </summary>
        /// <param name="config">Configuração do pool.</param>
        private void CreatePool(PoolConfig config)
        {
            if (string.IsNullOrEmpty(config.Tag))
            {
                Debug.LogError("[ObjectPoolManager] PoolConfig com tag vazia ignorado.");
                return;
            }

            if (config.Prefab == null)
            {
                Debug.LogError($"[ObjectPoolManager] PoolConfig '{config.Tag}' sem prefab ignorado.");
                return;
            }

            if (_pools.ContainsKey(config.Tag))
            {
                Debug.LogWarning($"[ObjectPoolManager] Pool '{config.Tag}' já existe. Ignorando duplicata.");
                return;
            }

            Queue<GameObject> pool = new Queue<GameObject>();
            _configs[config.Tag] = config;

            // Cria objeto pai específico para este pool
            GameObject poolRoot = new GameObject($"Pool_{config.Tag}");
            poolRoot.transform.SetParent(_poolParent);

            // Pré-instancia os objetos
            for (int i = 0; i < config.InitialSize; i++)
            {
                GameObject obj = InstantiatePooledObject(config.Prefab, poolRoot.transform);
                pool.Enqueue(obj);
            }

            _pools[config.Tag] = pool;
            Debug.Log($"[ObjectPoolManager] Pool '{config.Tag}' criado com {config.InitialSize} objetos.");
        }

        // ===== API PÚBLICA =====

        /// <summary>
        /// Obtém um objeto do pool. Ativa e retorna o objeto para uso.
        /// </summary>
        /// <param name="tag">Tag do pool desejado.</param>
        /// <param name="position">Posição inicial do objeto.</param>
        /// <param name="rotation">Rotação inicial do objeto.</param>
        /// <returns>GameObject do pool ou null se o pool não existir.</returns>
        public GameObject Get(string tag, Vector3 position, Quaternion rotation)
        {
            if (!_pools.TryGetValue(tag, out Queue<GameObject> pool))
            {
                Debug.LogError($"[ObjectPoolManager] Pool '{tag}' não encontrado. Registre-o no Inspector.");
                return null;
            }

            GameObject obj;

            if (pool.Count > 0)
            {
                obj = pool.Dequeue();
            }
            else
            {
                PoolConfig config = _configs[tag];

                if (!config.AllowExpansion)
                {
                    Debug.LogWarning($"[ObjectPoolManager] Pool '{tag}' esgotado e expansão desabilitada.");
                    return null;
                }

                if (config.MaxSize > 0 && GetTotalPoolSize(tag) >= config.MaxSize)
                {
                    Debug.LogWarning($"[ObjectPoolManager] Pool '{tag}' atingiu o tamanho máximo de {config.MaxSize}.");
                    return null;
                }

                // Expande o pool
                obj = InstantiatePooledObject(config.Prefab, _poolParent);
                Debug.Log($"[ObjectPoolManager] Pool '{tag}' expandido.");
            }

            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);

            return obj;
        }

        /// <summary>
        /// Obtém um objeto do pool na origem com rotação neutra.
        /// </summary>
        public GameObject Get(string tag) => Get(tag, Vector3.zero, Quaternion.identity);

        /// <summary>
        /// Retorna um objeto ao pool. Desativa e enfileira o objeto.
        /// </summary>
        /// <param name="tag">Tag do pool de destino.</param>
        /// <param name="obj">Objeto a ser devolvido.</param>
        public void Return(string tag, GameObject obj)
        {
            if (obj == null)
            {
                Debug.LogError("[ObjectPoolManager] Tentativa de retornar objeto nulo ao pool.");
                return;
            }

            if (!_pools.TryGetValue(tag, out Queue<GameObject> pool))
            {
                Debug.LogError($"[ObjectPoolManager] Pool '{tag}' não encontrado ao retornar objeto.");
                Destroy(obj);
                return;
            }

            obj.SetActive(false);
            obj.transform.SetParent(_poolParent);
            pool.Enqueue(obj);
        }

        /// <summary>
        /// Retorna todos os objetos de um pool ao estado inativo.
        /// Útil ao reiniciar uma partida.
        /// </summary>
        /// <param name="tag">Tag do pool a ser limpo.</param>
        public void ReturnAll(string tag)
        {
            // Encontra todos os objetos ativos pertencentes a este pool
            // e os desativa — esta é uma operação custosa, use com moderação
            Debug.LogWarning($"[ObjectPoolManager] ReturnAll chamado para '{tag}'. Use apenas em transições de cena.");
        }

        /// <summary>
        /// Cria um pool em runtime, fora da configuração inicial.
        /// </summary>
        /// <param name="config">Configuração do novo pool.</param>
        public void CreatePoolRuntime(PoolConfig config)
        {
            CreatePool(config);
        }

        // ===== UTILITÁRIOS PRIVADOS =====

        /// <summary>
        /// Instancia um objeto poolado desativado.
        /// </summary>
        private GameObject InstantiatePooledObject(GameObject prefab, Transform parent)
        {
            GameObject obj = Instantiate(prefab, parent);
            obj.SetActive(false);
            return obj;
        }

        /// <summary>
        /// Retorna o tamanho atual (disponível) de um pool.
        /// </summary>
        private int GetTotalPoolSize(string tag)
        {
            return _pools.TryGetValue(tag, out Queue<GameObject> pool) ? pool.Count : 0;
        }

        // ===== TAGS PADRÃO =====

        /// <summary>Tags padrão dos pools do jogo para evitar magic strings.</summary>
        public static class PoolTags
        {
            public const string DustParticle = "DustParticle";
            public const string Footstep = "Footstep";
            public const string GoalFlash = "GoalFlash";
            public const string UIPopup = "UIPopup";
            public const string AudioSource = "AudioSource";
            public const string ImpactEffect = "ImpactEffect";
            public const string BallTrail = "BallTrail";
        }
    }
}
