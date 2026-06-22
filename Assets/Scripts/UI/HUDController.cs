using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

namespace CampoDeTerraFC.UI
{
    public sealed class HUDController : MonoBehaviour
    {
        private TextMeshProUGUI _scoreTmpA, _scoreTmpB, _timerTmp, _teamNameA, _teamNameB;
        private GameObject _goalPanel, _pausePanel, _matchEndPanel;
        private TextMeshProUGUI _goalText;
        private bool _isPaused;

        private void Start()
        {
            _scoreTmpA = FindTMP("ScoreA_TMP");
            _scoreTmpB = FindTMP("ScoreB_TMP");
            _timerTmp  = FindTMP("Timer_TMP");
            _teamNameA = FindTMP("TeamA_Name");
            _teamNameB = FindTMP("TeamB_Name");
            _goalPanel  = GameObject.Find("GoalPanel");
            _pausePanel = GameObject.Find("PausePanel");
            if (_goalPanel  != null) { _goalText = _goalPanel.GetComponentInChildren<TextMeshProUGUI>(); _goalPanel.SetActive(false); }
            if (_pausePanel != null) _pausePanel.SetActive(false);
            WireButton("BtnResume",    () => { _isPaused = false; if (_pausePanel) _pausePanel.SetActive(false); Time.timeScale = 1f; });
            WireButton("BtnQuitMatch", () => { Time.timeScale = 1f; SceneManager.LoadScene("MainMenu"); });
            UpdateScore(0, 0);
            UpdateTimer(600f);
        }

        private void Update() { if (UnityEngine.Input.GetKeyDown(KeyCode.Escape)) TogglePause(); }

        public void UpdateScore(int a, int b)
        {
            if (_scoreTmpA) _scoreTmpA.text = a.ToString();
            if (_scoreTmpB) _scoreTmpB.text = b.ToString();
        }

        public void UpdateTimer(float seconds)
        {
            if (_timerTmp == null) return;
            int m = Mathf.FloorToInt(seconds / 60f);
            int s = Mathf.FloorToInt(seconds % 60f);
            _timerTmp.text  = string.Format("{0:00}:{1:00}", m, s);
            _timerTmp.color = seconds <= 60f ? Color.red : Color.white;
        }

        public void SetTeamNames(string nameA, string nameB)
        {
            if (_teamNameA) _teamNameA.text = nameA;
            if (_teamNameB) _teamNameB.text = nameB;
        }

        public void ShowGoal(int scoringTeam)
        {
            if (_goalPanel == null) return;
            _goalPanel.SetActive(true);
            if (_goalText) _goalText.text = "GOL!\n" + (scoringTeam == 0 ? "AZUL" : "VERMELHO");
        }

        public void HideGoal() { if (_goalPanel) _goalPanel.SetActive(false); }

        public void TogglePause()
        {
            _isPaused = !_isPaused;
            if (_pausePanel) _pausePanel.SetActive(_isPaused);
            Time.timeScale = _isPaused ? 0f : 1f;
            Core.GameManager.Instance?.ChangeState(_isPaused ? Core.GameState.Paused : Core.GameState.Playing);
        }

        public void ShowMatchEnd(int scoreA, int scoreB)
        {
            if (_matchEndPanel != null) { _matchEndPanel.SetActive(true); return; }
            _matchEndPanel = BuildEndPanel(scoreA, scoreB);
        }

        private GameObject BuildEndPanel(int a, int b)
        {
            var canvas = GetComponent<Canvas>() ?? FindObjectOfType<Canvas>();
            if (canvas == null) return null;
            var panel = new GameObject("MatchEndPanel");
            panel.transform.SetParent(canvas.transform, false);
            var rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.88f);
            AddTMP(panel.transform, "R", a > b ? "VITORIA AZUL!" : b > a ? "VITORIA VERMELHO!" : "EMPATE!",
                new Vector2(0,80), new Vector2(700,80), 52, Color.white, FontStyles.Bold);
            AddTMP(panel.transform, "S", string.Format("AZUL {0}  X  {1} VERMELHO", a, b),
                new Vector2(0,10), new Vector2(600,60), 38, Color.yellow, FontStyles.Normal);
            AddButton(panel.transform, "Jogar Novamente", new Vector2(0,-70),  () => { Time.timeScale=1f; SceneManager.LoadScene(SceneManager.GetActiveScene().name); });
            AddButton(panel.transform, "Menu Principal",  new Vector2(0,-140), () => { Time.timeScale=1f; SceneManager.LoadScene("MainMenu"); });
            return panel;
        }

        public void Initialize() {}
        public void ShowHUD()    {}
        public void ShowPause()  { if (!_isPaused) TogglePause(); }
        public void HidePause()  { if (_isPaused)  TogglePause(); }

        private TextMeshProUGUI FindTMP(string n) { var g=GameObject.Find(n); return g?.GetComponent<TextMeshProUGUI>(); }
        private void WireButton(string n, UnityEngine.Events.UnityAction a) { var g=GameObject.Find(n); g?.GetComponent<Button>()?.onClick.AddListener(a); }

        private void AddTMP(Transform p, string n, string text, Vector2 pos, Vector2 size, float fs, Color col, FontStyles style)
        {
            var go=new GameObject(n); go.transform.SetParent(p,false);
            var rt=go.AddComponent<RectTransform>(); rt.anchoredPosition=pos; rt.sizeDelta=size; rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);
            var tmp=go.AddComponent<TextMeshProUGUI>(); tmp.text=text; tmp.fontSize=fs; tmp.color=col; tmp.fontStyle=style; tmp.alignment=TextAlignmentOptions.Center;
        }

        private void AddButton(Transform p, string label, Vector2 pos, UnityEngine.Events.UnityAction action)
        {
            var go=new GameObject(label+"_Btn"); go.transform.SetParent(p,false);
            var rt=go.AddComponent<RectTransform>(); rt.anchoredPosition=pos; rt.sizeDelta=new Vector2(320,60); rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);
            go.AddComponent<Image>().color=new Color(.15f,.15f,.15f);
            go.AddComponent<Button>().onClick.AddListener(action);
            AddTMP(go.transform,"Lbl",label,Vector2.zero,new Vector2(300,50),24,Color.white,FontStyles.Normal);
        }
    }

    public sealed class UIManager : MonoBehaviour
    {
        public void Initialize() {}
        public void ShowHUD()    {}
        public void ShowPause()  { FindObjectOfType<HUDController>()?.ShowPause(); }
        public void HidePause()  { FindObjectOfType<HUDController>()?.HidePause(); }
        public void UpdateScore(int a, int b) { FindObjectOfType<HUDController>()?.UpdateScore(a,b); }
        public void UpdateTimer(float t)      { FindObjectOfType<HUDController>()?.UpdateTimer(t); }
    }
}
