using System;
using System.Collections.Generic;
using UnityEngine;

namespace CampoDeTerraFC.Core.Services
{
    /// <summary>
    /// ServiceLocator é um container leve de injeção de dependência.
    /// Permite que sistemas se registrem e sejam recuperados sem acoplamento direto.
    /// Padrão: Registro explícito por tipo base ou interface.
    /// </summary>
    public static class ServiceLocator
    {
        // ===== CONTAINER =====

        /// <summary>Dicionário de serviços registrados, indexados pelo tipo.</summary>
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        // ===== REGISTRO =====

        /// <summary>
        /// Registra um serviço no container.
        /// </summary>
        /// <typeparam name="T">Tipo base ou interface do serviço.</typeparam>
        /// <param name="service">Instância do serviço.</param>
        public static void Register<T>(T service) where T : class
        {
            Type key = typeof(T);

            if (service == null)
            {
                Debug.LogError($"[ServiceLocator] Tentativa de registrar serviço nulo do tipo {key.Name}.");
                return;
            }

            if (_services.ContainsKey(key))
            {
                Debug.LogWarning($"[ServiceLocator] Substituindo serviço existente do tipo {key.Name}.");
            }

            _services[key] = service;
            Debug.Log($"[ServiceLocator] Serviço registrado: {key.Name}");
        }

        // ===== RECUPERAÇÃO =====

        /// <summary>
        /// Recupera um serviço registrado. Lança exceção se não encontrado.
        /// </summary>
        /// <typeparam name="T">Tipo do serviço desejado.</typeparam>
        /// <returns>Instância do serviço.</returns>
        public static T Get<T>() where T : class
        {
            Type key = typeof(T);

            if (_services.TryGetValue(key, out object service))
            {
                return service as T;
            }

            Debug.LogError($"[ServiceLocator] Serviço não encontrado: {key.Name}. Certifique-se de registrá-lo no GameManager.");
            return null;
        }

        /// <summary>
        /// Tenta recuperar um serviço sem lançar exceção.
        /// </summary>
        /// <typeparam name="T">Tipo do serviço desejado.</typeparam>
        /// <param name="service">Serviço encontrado ou null.</param>
        /// <returns>True se o serviço foi encontrado.</returns>
        public static bool TryGet<T>(out T service) where T : class
        {
            Type key = typeof(T);

            if (_services.TryGetValue(key, out object rawService))
            {
                service = rawService as T;
                return service != null;
            }

            service = null;
            return false;
        }

        // ===== REMOÇÃO =====

        /// <summary>
        /// Remove um serviço específico do container.
        /// </summary>
        /// <typeparam name="T">Tipo do serviço a ser removido.</typeparam>
        public static void Unregister<T>() where T : class
        {
            Type key = typeof(T);

            if (_services.Remove(key))
            {
                Debug.Log($"[ServiceLocator] Serviço removido: {key.Name}");
            }
            else
            {
                Debug.LogWarning($"[ServiceLocator] Tentativa de remover serviço não registrado: {key.Name}");
            }
        }

        /// <summary>
        /// Remove todos os serviços do container. Chamado ao destruir o GameManager.
        /// </summary>
        public static void UnregisterAll()
        {
            _services.Clear();
            Debug.Log("[ServiceLocator] Todos os serviços foram removidos.");
        }

        // ===== DEBUG =====

        /// <summary>
        /// Verifica se um serviço está registrado.
        /// </summary>
        /// <typeparam name="T">Tipo do serviço.</typeparam>
        public static bool IsRegistered<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Retorna a quantidade de serviços registrados (debug).
        /// </summary>
        public static int ServiceCount => _services.Count;
    }
}
