using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CampoDeTerraFC.UI
{
    /// <summary>
    /// Controla a tela de Menu Principal.
    /// Liga automaticamente os botões criados pelo ProjectSetup.
    /// </summary>
    public class MenuController : MonoBehaviour
    {
        private void Start()
        {
            WireButton("BtnPelada",    () => SceneManager.LoadScene("Match"));
            WireButton("BtnChampion",  () => Debug.Log("Campeonato em breve!"));
            WireButton("BtnPenalty",   () => SceneManager.LoadScene("Penalty"));
            WireButton("BtnTraining",  () => SceneManager.LoadScene("Training"));
            WireButton("BtnQuit",      Application.Quit);
        }

        private void WireButton(string name, UnityEngine.Events.UnityAction action)
        {
            var go  = GameObject.Find(name);
            var btn = go?.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(action);
        }
    }
}
