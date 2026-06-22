#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;

namespace CampoDeTerraFC.Editor
{
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        private const string DONE_KEY = "CampoDeTerraFC_SetupDone_v5";

        static ProjectSetup() { EditorApplication.delayCall += RunSetup; }

        private static void RunSetup()
        {
            if (EditorPrefs.GetBool(DONE_KEY, false))
            {
                if (File.Exists("Assets/Scenes/Match.unity")) return;
                EditorPrefs.SetBool(DONE_KEY, false);
            }
            Debug.Log("[CampoDeTerraFC] === SETUP AUTOMATICO INICIADO ===");
            try
            {
                EnsureFolders(); SetupURP(); BuildScenes();
                AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
                EditorPrefs.SetBool(DONE_KEY, true);
                Debug.Log("[CampoDeTerraFC] === SETUP CONCLUIDO — 0 Errors. Abra Match.unity e pressione Play! ===");
                EditorSceneManager.OpenScene("Assets/Scenes/Match.unity");
            }
            catch (System.Exception ex)
            { Debug.LogError("[CampoDeTerraFC] Erro no setup: " + ex.Message + "\n" + ex.StackTrace); }
        }

        private static void EnsureFolders()
        {
            string[] fs = { "Assets/Scenes","Assets/Settings","Assets/Resources",
                "Assets/Prefabs/Characters","Assets/Prefabs/Ball","Assets/Prefabs/Field",
                "Assets/Prefabs/Goals","Assets/Prefabs/Camera","Assets/Prefabs/UI","Assets/Prefabs/Managers",
                "Assets/Materials/Field","Assets/Materials/Ball","Assets/Materials/Characters" };
            foreach (string f in fs)
                if (!AssetDatabase.IsValidFolder(f))
                    AssetDatabase.CreateFolder(Path.GetDirectoryName(f).Replace('\\','/'), Path.GetFileName(f));
        }

        private static void SetupURP()
        {
            string path = "Assets/Settings/UniversalRP.asset";
            if (File.Exists(path)) return;
            var urp = UniversalRenderPipelineAsset.Create();
            AssetDatabase.CreateAsset(urp, path);
            GraphicsSettings.defaultRenderPipeline = urp;
            QualitySettings.renderPipeline = urp;
        }

        private static void BuildScenes()
        { BuildBootstrap(); BuildMainMenu(); BuildMatch(); BuildPenalty(); BuildTraining(); }

        private static void BuildBootstrap()
        {
            var s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var gm = new GameObject("[GameManager]");
            gm.AddComponent<Core.GameManager>(); gm.AddComponent<SaveSystem.SaveManager>();
            var au = new GameObject("[AudioManager]");
            au.AddComponent<AudioListener>(); au.AddComponent<Audio.AudioManager>();
            new GameObject("[ObjectPoolManager]").AddComponent<Core.ObjectPoolManager>();
            new GameObject("[InputManager]").AddComponent<Input.InputManager>();
            new GameObject("[BootstrapLoader]").AddComponent<Core.BootstrapLoader>();
            EditorSceneManager.SaveScene(s, "Assets/Scenes/Bootstrap.unity");
        }

        private static void BuildMainMenu()
        {
            var s = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var cam = GameObject.Find("Main Camera") ?? new GameObject("Main Camera");
            if (!cam.GetComponent<UnityEngine.Camera>()) cam.AddComponent<UnityEngine.Camera>();
            cam.transform.position = new Vector3(0,2,-5); cam.tag = "MainCamera";
            var cv = CreateCanvas("MenuCanvas");
            ADDTMP(cv.transform,"Title","CAMPO DE TERRA FC",new Vector2(0,160),new Vector2(800,100),68,Color.white,FontStyles.Bold);
            ADDBTN(cv.transform,"BtnPelada",  "PELADA",       new Vector2(0, 40));
            ADDBTN(cv.transform,"BtnChampion","CAMPEONATO",   new Vector2(0,-30));
            ADDBTN(cv.transform,"BtnPenalty", "PENALTIS",     new Vector2(0,-100));
            ADDBTN(cv.transform,"BtnTraining","TREINO LIVRE", new Vector2(0,-170));
            ADDBTN(cv.transform,"BtnQuit",    "SAIR",         new Vector2(0,-260));
            cv.AddComponent<UI.MenuController>();
            ES();
            EditorSceneManager.SaveScene(s, "Assets/Scenes/MainMenu.unity");
        }

        private static void BuildMatch()
        {
            var s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var lg = new GameObject("Directional Light");
            var l = lg.AddComponent<Light>(); l.type=LightType.Directional; l.intensity=1.2f; l.color=new Color(1f,.95f,.85f);
            lg.transform.rotation = Quaternion.Euler(50f,-30f,0f);
            RenderSettings.ambientIntensity = 0.8f;
            FIELD();
            GOAL(new Vector3(0,0, 35f), true);
            GOAL(new Vector3(0,0,-35f), false);
            var ball = BALL();
            TEAM(0); TEAM(1);
            GK(1, new Vector3(0,0.9f,-34f));
            GK(0, new Vector3(0,0.9f, 34f));
            var cr = CAMRIG(ball);
            cr.AddComponent<AudioListener>();
            var mgr = new GameObject("[Managers]");
            var mc = mgr.AddComponent<Gameplay.MatchController>(); mc.SetBall(ball.GetComponent<Ball.BallController>());
            mgr.AddComponent<Managers.ScoreManager>(); mgr.AddComponent<Managers.TimerManager>();
            mgr.AddComponent<Gameplay.RulesEngine>();
            new GameObject("[InputManager]").AddComponent<Input.InputManager>();
            var sp = new GameObject("BallSpawnPoint"); sp.transform.position = new Vector3(0f,.11f,0f);
            HUD();
            ES();
            EditorSceneManager.SaveScene(s, "Assets/Scenes/Match.unity");
        }

        private static void BuildPenalty()
        {
            var s = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var cam = GameObject.Find("Main Camera") ?? new GameObject("Main Camera");
            if (!cam.GetComponent<UnityEngine.Camera>()) cam.AddComponent<UnityEngine.Camera>();
            cam.transform.position=new Vector3(0,2,-12); cam.transform.rotation=Quaternion.Euler(10,0,0); cam.tag="MainCamera";
            var f=GameObject.CreatePrimitive(PrimitiveType.Plane); f.name="PenaltyField";
            f.transform.localScale=new Vector3(3,1,4); SC(f,new Color(.55f,.38f,.18f)); f.tag="Ground";
            GOAL(new Vector3(0,0,12),true);
            var b=BALL(); b.transform.position=new Vector3(0,.11f,6.3f);
            var gk=CAP("Goalkeeper_B",new Vector3(0,.9f,11.5f),new Color(1f,.5f,0f));
            gk.AddComponent<Goalkeeper.GoalkeeperController>().Initialize(null,1);
            gk.AddComponent<Goalkeeper.SimpleGoalkeeperAI>().SetTeamIndex(1);
            var cv=CreateCanvas("PenaltyHUD");
            ADDTMP(cv.transform,"PT","DISPUTA DE PENALTIS",new Vector2(0,200),new Vector2(600,80),40,Color.white,FontStyles.Bold);
            ADDTMP(cv.transform,"PS","0 x 0",new Vector2(0,120),new Vector2(200,60),48,Color.yellow,FontStyles.Bold);
            ES();
            EditorSceneManager.SaveScene(s, "Assets/Scenes/Penalty.unity");
        }

        private static void BuildTraining()
        {
            var s = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var cam=GameObject.Find("Main Camera")??new GameObject("Main Camera");
            if (!cam.GetComponent<UnityEngine.Camera>()) cam.AddComponent<UnityEngine.Camera>();
            cam.transform.position=new Vector3(0,15,-18); cam.transform.rotation=Quaternion.Euler(50,0,0); cam.tag="MainCamera";
            var f=GameObject.CreatePrimitive(PrimitiveType.Plane); f.name="TrainingField";
            f.transform.localScale=new Vector3(5,1,8); SC(f,new Color(.55f,.38f,.18f)); f.tag="Ground";
            var ball=BALL();
            GOAL(new Vector3(0,0,30f),true);
            var p=CAP("Player_Training",new Vector3(0,.9f,-5f),new Color(.2f,.5f,1f));
            p.AddComponent<Player.PlayerController>().SetAsHumanControlled();
            new GameObject("[InputManager]").AddComponent<Input.InputManager>();
            cam.AddComponent<Camera.MatchCamera>().SetBallTransform(ball.transform);
            var cv=CreateCanvas("TrainingHUD");
            ADDTMP(cv.transform,"Info","TREINO — WASD Mover | F Chutar | Q Passar",new Vector2(0,-300),new Vector2(900,50),18,Color.white,FontStyles.Normal);
            ES();
            EditorSceneManager.SaveScene(s, "Assets/Scenes/Training.unity");
        }

        // ----------- CAMPO -----------
        private static void FIELD()
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Plane); go.name="Field_Ground";
            go.transform.localScale=new Vector3(5f,1f,7f); go.tag="Ground";
            int gl=LayerMask.NameToLayer("Ground"); if(gl>=0) go.layer=gl;
            var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color=new Color(.55f,.38f,.18f); mat.SetFloat("_Smoothness",.1f);
            go.GetComponent<Renderer>().material=mat;
            AssetDatabase.CreateAsset(mat,"Assets/Materials/Field/DirtField.mat");
            LINE("CL",new Vector3(0,.01f,0),  new Vector3(5f,1f,.02f));
            LINE("LL",new Vector3(-24.5f,.01f,0),new Vector3(.02f,1f,7f));
            LINE("RL",new Vector3( 24.5f,.01f,0),new Vector3(.02f,1f,7f));
            LINE("TL",new Vector3(0,.01f, 34.5f),new Vector3(5f,1f,.02f));
            LINE("BL",new Vector3(0,.01f,-34.5f),new Vector3(5f,1f,.02f));
        }
        private static void LINE(string n,Vector3 p,Vector3 sc)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Plane); go.name=n;
            go.transform.position=p; go.transform.localScale=sc;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var mat=new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.color=Color.white;
            go.GetComponent<Renderer>().material=mat;
        }
        private static void GOAL(Vector3 pos,bool isTop)
        {
            var root=new GameObject(isTop?"Goal_B":"Goal_A"); root.transform.position=pos; root.tag="Goal";
            float w=7.32f,h=2.44f,d=1.5f,r=.06f;
            var mat=new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.color=Color.white;
            POST("PL",root.transform,new Vector3(-w/2,h/2,0),new Vector3(r*2,h,r*2),mat);
            POST("PR",root.transform,new Vector3( w/2,h/2,0),new Vector3(r*2,h,r*2),mat);
            POST("CB",root.transform,new Vector3(0,h,0),new Vector3(w+r*2,r*2,r*2),mat);
            var tGo=new GameObject("GoalTrigger"); tGo.transform.SetParent(root.transform);
            tGo.transform.localPosition=new Vector3(0,h/2,d/4*(isTop?1:-1));
            var bc=tGo.AddComponent<BoxCollider>(); bc.size=new Vector3(w,h+.5f,d/2); bc.isTrigger=true;
            var gt=tGo.AddComponent<Gameplay.GoalTrigger>(); gt.GoalIndex=isTop?1:0;
        }
        private static void POST(string n,Transform pr,Vector3 lp,Vector3 sz,Material mat)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder); go.name=n; go.transform.SetParent(pr);
            go.transform.localPosition=lp; go.transform.localScale=new Vector3(sz.x,sz.y/2f,sz.z);
            go.GetComponent<Renderer>().material=mat; go.tag="Post";
        }
        private static GameObject BALL()
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name="Ball"; go.tag="Ball";
            go.transform.position=new Vector3(0,.11f,0); go.transform.localScale=Vector3.one*.22f;
            int bl=LayerMask.NameToLayer("Ball"); if(bl>=0) go.layer=bl;
            SC(go,Color.white);
            var rb=go.AddComponent<Rigidbody>(); rb.mass=.43f; rb.drag=.3f; rb.angularDrag=.5f;
            rb.interpolation=RigidbodyInterpolation.Interpolate; rb.collisionDetectionMode=CollisionDetectionMode.Continuous;
            var pm=new PhysicMaterial("Ball"); pm.bounciness=.65f; pm.dynamicFriction=.4f; pm.staticFriction=.5f;
            go.GetComponent<SphereCollider>().material=pm;
            go.AddComponent<Ball.BallController>(); return go;
        }
        private static void TEAM(int ti)
        {
            Color col=ti==0?new Color(.2f,.3f,1f):new Color(1f,.15f,.15f);
            var root=new GameObject(ti==0?"Team_A":"Team_B");
            int pl=LayerMask.NameToLayer("Player");
            Vector3[] posA={new Vector3(-6f,.9f,-20f),new Vector3(6f,.9f,-20f),new Vector3(0f,.9f,-14f),new Vector3(-5f,.9f,-8f),new Vector3(5f,.9f,-8f)};
            Vector3[] posB={new Vector3(-6f,.9f, 20f),new Vector3(6f,.9f, 20f),new Vector3(0f,.9f, 14f),new Vector3(-5f,.9f, 8f),new Vector3(5f,.9f, 8f)};
            string[] names={"Def_Esq","Def_Dir","Meia","Ata_Esq","Ata_Dir"};
            Vector3[] poses=ti==0?posA:posB;
            for(int i=0;i<5;i++){
                var p=CAP(string.Format("[Team{0}] {1}",ti==0?"A":"B",names[i]),poses[i],col);
                p.transform.SetParent(root.transform); p.tag=ti==0?"TeamA":"TeamB";
                if(pl>=0) p.layer=pl;
                var pc=p.AddComponent<Player.PlayerController>(); pc.Initialize(null,ti);
                if(ti==0&&i==4) pc.SetAsHumanControlled();
                else p.AddComponent<AI.SimpleFieldAI>().SetTeamIndex(ti);
            }
        }
        private static void GK(int ti,Vector3 pos)
        {
            Color col=ti==0?new Color(1f,.8f,0f):new Color(0f,.8f,.2f);
            var gk=CAP(string.Format("[Team{0}] GK",ti==0?"A":"B"),pos,col); gk.tag="Goalkeeper";
            gk.AddComponent<Goalkeeper.GoalkeeperController>().Initialize(null,ti);
            gk.AddComponent<Goalkeeper.SimpleGoalkeeperAI>().SetTeamIndex(ti);
        }
        private static GameObject CAMRIG(GameObject ball)
        {
            var rig=new GameObject("CameraRig"); rig.transform.position=new Vector3(0,18,-22); rig.transform.rotation=Quaternion.Euler(55,0,0);
            var cgo=new GameObject("Main Camera"); cgo.transform.SetParent(rig.transform);
            cgo.transform.localPosition=Vector3.zero; cgo.transform.localRotation=Quaternion.identity; cgo.tag="MainCamera";
            cgo.AddComponent<UnityEngine.Camera>().fieldOfView=60f;
            rig.AddComponent<Camera.MatchCamera>().SetBallTransform(ball.transform); return rig;
        }
        private static void HUD()
        {
            var cv=CreateCanvas("HUD_Canvas");
            var bg=PANEL(cv.transform,"ScoreBackground",new Vector2(0,-30),new Vector2(420,60),new Color(0,0,0,.75f));
            ADDTMP(bg.transform,"ScoreA_TMP","0",new Vector2(-90,0),new Vector2(80,50),40,Color.white,FontStyles.Bold);
            ADDTMP(bg.transform,"ScoreSep",  "X",new Vector2(0,0),  new Vector2(40,50),30,Color.gray, FontStyles.Normal);
            ADDTMP(bg.transform,"ScoreB_TMP","0",new Vector2(90,0), new Vector2(80,50),40,Color.white,FontStyles.Bold);
            ADDTMP(cv.transform,"Timer_TMP","10:00",new Vector2(0,-90),new Vector2(180,50),30,Color.white,FontStyles.Normal);
            ADDTMP(cv.transform,"TeamA_Name","AZUL",    new Vector2(-210,-30),new Vector2(160,40),20,new Color(.4f,.6f,1f), FontStyles.Normal);
            ADDTMP(cv.transform,"TeamB_Name","VERMELHO", new Vector2( 210,-30),new Vector2(180,40),20,new Color(1f,.4f,.4f),FontStyles.Normal);
            var gp=PANEL(cv.transform,"GoalPanel",new Vector2(0,60),new Vector2(500,150),new Color(0,0,0,.8f));
            ADDTMP(gp.transform,"GoalText","GOL!",Vector2.zero,new Vector2(400,120),80,Color.yellow,FontStyles.Bold);
            gp.SetActive(false);
            var pp=PANEL(cv.transform,"PausePanel",Vector2.zero,new Vector2(420,300),new Color(0,0,0,.9f));
            ADDTMP(pp.transform,"PauseTitle","PAUSADO",new Vector2(0,90),new Vector2(360,60),42,Color.white,FontStyles.Bold);
            ADDBTN(pp.transform,"BtnResume",   "CONTINUAR",      new Vector2(0, 10));
            ADDBTN(pp.transform,"BtnQuitMatch","MENU PRINCIPAL", new Vector2(0,-60));
            pp.SetActive(false);
            cv.AddComponent<UI.HUDController>();
        }
        private static GameObject CAP(string n,Vector3 pos,Color col)
        {
            var root=new GameObject(n); root.transform.position=pos;
            var body=GameObject.CreatePrimitive(PrimitiveType.Capsule); body.name="Body"; body.transform.SetParent(root.transform);
            body.transform.localPosition=Vector3.zero; body.transform.localScale=new Vector3(.5f,.9f,.5f);
            SC(body,col); Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());
            var head=GameObject.CreatePrimitive(PrimitiveType.Sphere); head.name="Head"; head.transform.SetParent(root.transform);
            head.transform.localPosition=new Vector3(0,1f,0); head.transform.localScale=Vector3.one*.35f;
            SC(head,new Color(.8f,.6f,.45f));
            var cc=root.AddComponent<CapsuleCollider>(); cc.height=1.8f; cc.radius=.3f; cc.center=new Vector3(0,.9f,0);
            var rb=root.AddComponent<Rigidbody>(); rb.freezeRotation=true; rb.mass=70f; rb.drag=5f;
            rb.interpolation=RigidbodyInterpolation.Interpolate; rb.collisionDetectionMode=CollisionDetectionMode.Continuous;
            var arr=GameObject.CreatePrimitive(PrimitiveType.Cylinder); arr.name="PlayerArrow"; arr.transform.SetParent(root.transform);
            arr.transform.localPosition=new Vector3(0,2.2f,0); arr.transform.localScale=new Vector3(.1f,.3f,.1f);
            Object.DestroyImmediate(arr.GetComponent<Collider>()); SC(arr,Color.yellow); arr.SetActive(false);
            return root;
        }
        private static GameObject CreateCanvas(string n)
        {
            var go=new GameObject(n);
            var c=go.AddComponent<Canvas>(); c.renderMode=RenderMode.ScreenSpaceOverlay;
            var cs=go.AddComponent<CanvasScaler>();
            cs.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution=new Vector2(1920,1080); cs.matchWidthOrHeight=.5f;
            go.AddComponent<GraphicRaycaster>(); return go;
        }
        private static GameObject PANEL(Transform p,string n,Vector2 pos,Vector2 sz,Color col)
        {
            var go=new GameObject(n); go.transform.SetParent(p,false);
            var rt=go.AddComponent<RectTransform>(); rt.anchoredPosition=pos; rt.sizeDelta=sz; rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);
            go.AddComponent<Image>().color=col; return go;
        }
        private static void ADDTMP(Transform p,string n,string text,Vector2 pos,Vector2 sz,float fs,Color col,FontStyles st)
        {
            var go=new GameObject(n); go.transform.SetParent(p,false);
            var rt=go.AddComponent<RectTransform>(); rt.anchoredPosition=pos; rt.sizeDelta=sz; rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);
            var tmp=go.AddComponent<TextMeshProUGUI>(); tmp.text=text; tmp.fontSize=fs; tmp.color=col; tmp.fontStyle=st;
            tmp.alignment=TextAlignmentOptions.Center; tmp.enableWordWrapping=false;
        }
        private static void ADDBTN(Transform p,string n,string label,Vector2 pos)
        {
            var go=new GameObject(n); go.transform.SetParent(p,false);
            var rt=go.AddComponent<RectTransform>(); rt.anchoredPosition=pos; rt.sizeDelta=new Vector2(300,55); rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);
            go.AddComponent<Image>().color=new Color(.15f,.15f,.15f,.95f);
            var btn=go.AddComponent<Button>(); var colors=btn.colors;
            colors.highlightedColor=new Color(.3f,.6f,1f); colors.pressedColor=new Color(.1f,.4f,.9f);
            btn.colors=colors;
            ADDTMP(go.transform,"Lbl",label,Vector2.zero,new Vector2(280,50),22,Color.white,FontStyles.Normal);
        }
        private static void SC(GameObject go,Color col)
        {
            var mat=new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.color=col;
            go.GetComponent<Renderer>().material=mat;
        }
        private static void ES()
        {
            if(Object.FindObjectOfType<EventSystem>()!=null) return;
            var go=new GameObject("EventSystem");
            go.AddComponent<EventSystem>(); go.AddComponent<StandaloneInputModule>();
        }

        [MenuItem("Campo de Terra FC/Setup Completo do Projeto")]
        public static void ForceSetup() { EditorPrefs.SetBool(DONE_KEY,false); RunSetup(); }
        [MenuItem("Campo de Terra FC/Abrir Cena Match")]
        public static void OpenMatch() { EditorSceneManager.OpenScene("Assets/Scenes/Match.unity"); }
        [MenuItem("Campo de Terra FC/Abrir Cena MainMenu")]
        public static void OpenMenu() { EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity"); }
        [MenuItem("Campo de Terra FC/Recriar Apenas Cenas")]
        public static void RebuildScenes() { BuildScenes(); AssetDatabase.SaveAssets(); AssetDatabase.Refresh(); OpenMatch(); }
    }
}
#endif
