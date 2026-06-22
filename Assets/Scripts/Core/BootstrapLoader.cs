using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CampoDeTerraFC.Core
{
    /// <summary>
    /// Aguarda o GameManager inicializar e carrega a cena do menu principal.
    /// </summary>
    public class BootstrapLoader : MonoBehaviour
    {
        [SerializeField] private float _loadDelay = 0.5f;

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(_loadDelay);
            SceneManager.LoadScene("MainMenu");
        }
    }
}
