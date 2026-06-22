# CAMPO DE TERRA FC — DOCUMENTO DE ARQUITETURA TÉCNICA v1.0

## 1. STACK TECNOLÓGICA

| Componente | Tecnologia |
|------------|------------|
| Engine | Unity 6 LTS (6000.x) |
| Linguagem | C# 10 |
| Render Pipeline | URP 17.x |
| Física | Unity Physics (Rigidbody + Custom) |
| Input | Unity Input System (New) |
| Audio | Unity Audio (FMOD opcional futuro) |
| UI | UI Toolkit + uGUI (HUD) |
| Serialização | Newtonsoft.Json + Unity Serialization |
| DI | Manual Service Locator + ScriptableObject Injection |
| Testes | NUnit + Unity Test Framework |
| Build | Unity Build System + Addressables |

---

## 2. PRINCÍPIOS ARQUITETURAIS

### SOLID
- **S** — Cada classe tem uma única responsabilidade
- **O** — Extensível via interfaces e herança, fechado para modificação
- **L** — Subtipos substituem tipos base sem quebrar comportamento
- **I** — Interfaces granulares e específicas
- **D** — Dependências injetadas, nunca criadas internamente

### Clean Architecture (adaptada para Unity)
```
Camada de Apresentação (View)
    UI Panels, HUD, Menus
    ↕
Camada de Aplicação (Use Cases)
    Managers, GameFlow, MatchController
    ↕
Camada de Domínio (Entities)
    Player, Ball, Team, Match, Rules
    ↕
Camada de Infraestrutura (Data)
    SaveSystem, AudioManager, Config
```

---

## 3. DIAGRAMA DE CLASSES PRINCIPAL

```
GameManager (Singleton)
├── MatchController
│   ├── TeamManager
│   │   ├── Team [x2]
│   │   │   ├── PlayerController [x11]
│   │   │   └── GoalkeeperController [x1]
│   ├── BallController
│   ├── ScoreManager
│   ├── TimerManager
│   └── RulesEngine
├── AudioManager
├── InputManager
├── CameraController
├── UIManager
│   ├── HUDPanel
│   ├── PausePanel
│   └── GoalPanel
└── SaveSystem
    └── PlayerPrefsRepository

ScriptableObjects
├── PlayerDataSO
├── TeamDataSO
├── FieldDataSO
├── GameConfigSO
└── AudioClipsSO
```

---

## 4. SISTEMAS DE JOGO

### 4.1 Sistema de Input
```csharp
// Fluxo: InputAction → IInputHandler → PlayerController
InputManager → InputProvider (interface)
    ├── KeyboardInputProvider
    ├── GamepadInputProvider
    └── TouchInputProvider
```

### 4.2 Sistema de Física da Bola
```
BallController
├── BallPhysicsService     // cálculo de spin, curva, trajetória
├── BallCollisionHandler   // reações a colisão
├── BallNetworkSync        // futuro multiplayer
└── BallVisualEffects      // poeira, rastro, brilho
```

### 4.3 Sistema de IA
```
AIManager
└── AIAgent (base)
    ├── AttackerAI
    ├── MidfielderAI
    ├── DefenderAI
    ├── WingbackAI
    └── GoalkeeperAI

AIBrainSM (State Machine)
├── IdleState
├── ChaseState
├── MarkState
├── AttackState
├── PassState
├── ShootState
├── ReturnState
└── PressureState
```

### 4.4 Observer Pattern — Eventos do jogo
```
GameEventSystem (ScriptableObject Events)
├── OnGoalScored
├── OnMatchStart
├── OnMatchEnd
├── OnFoulCommitted
├── OnCornerKick
├── OnPenalty
├── OnPlayerSelected
└── OnTimerTick
```

### 4.5 Object Pool
```
ObjectPoolManager
├── BallPool
├── DustParticlePool
├── FootstepPool
├── UIPopupPool
└── AudioSourcePool
```

---

## 5. ESTRUTURA DE CENAS

```
Scenes/
├── Bootstrap.unity        // Cena inicial, carrega GameManager
├── MainMenu.unity         // Menu principal
├── TeamSelection.unity    // Seleção de times e formação
├── Match.unity            // Cena principal do jogo
├── GoalReplay.unity       // Replay de gol (câmera cinemática)
├── PenaltyShootout.unity  // Disputa de pênaltis
└── Championship.unity     // Tabela do campeonato
```

---

## 6. SCRIPTABLE OBJECTS

```
Data/
├── PlayerDataSO
│   ├── string playerName
│   ├── Sprite portrait
│   ├── PlayerPosition position
│   └── PlayerStats stats (velocity, shoot, pass, dribble, defense, physical)
├── TeamDataSO
│   ├── string teamName
│   ├── Color primaryColor, secondaryColor
│   ├── Sprite logo
│   ├── List<PlayerDataSO> squad
│   └── FormationSO defaultFormation
├── FormationSO
│   ├── string formationName (4-3-3, 4-4-2, etc.)
│   └── List<Vector2> positions (percentual do campo)
├── FieldDataSO
│   ├── string fieldName
│   ├── SurfaceType surface
│   ├── float frictionCoefficient
│   └── GameObject fieldPrefab
└── GameConfigSO
    ├── float matchDuration
    ├── int maxPlayers
    ├── DifficultyLevel difficulty
    └── AudioConfig audioConfig
```

---

## 7. CONVENÇÕES DE CÓDIGO

### Nomenclatura
- **Classes:** PascalCase — `PlayerController`, `BallPhysicsService`
- **Interfaces:** IPascalCase — `IInputHandler`, `IGoalkeeper`
- **Métodos:** PascalCase — `HandleInput()`, `ApplyForce()`
- **Propriedades:** PascalCase — `CurrentSpeed`, `IsGrounded`
- **Campos privados:** _camelCase — `_rigidbody`, `_ballData`
- **Constantes:** SCREAMING_SNAKE — `MAX_SPEED`, `BALL_WEIGHT`
- **Eventos:** On+Verb — `OnGoalScored`, `OnMatchEnd`
- **ScriptableObjects:** PascalCase+SO — `PlayerDataSO`

### Organização de arquivo
```csharp
// 1. Using statements
// 2. Namespace
// 3. XML doc comment
// 4. Attributes
// 5. Class declaration
//    a. Constants
//    b. Static fields
//    c. SerializeField privates
//    d. Private fields
//    e. Properties
//    f. Unity lifecycle methods
//    g. Public methods
//    h. Private methods
//    i. Nested classes/structs
```

---

## 8. PERFORMANCE TARGETS

| Plataforma | FPS Alvo | Polígonos Máx | Draw Calls Máx |
|------------|----------|---------------|----------------|
| Android (mid) | 60 | 150k | 80 |
| Android (high) | 60 | 300k | 120 |
| PC (min) | 60 | 500k | 200 |
| PC (recomendado) | 120 | 1M | 400 |

### Estratégias de Otimização
- **Object Pooling:** Partículas, áudio, projéteis, UI
- **LOD Groups:** Personagens (3 níveis), ambiente (3 níveis)
- **Occlusion Culling:** Objetos fora da câmera
- **Texture Atlases:** Redução de draw calls de UI
- **Static Batching:** Objetos do cenário imóveis
- **GPU Instancing:** Grama, pedras, objetos repetidos
- **Addressables:** Carregamento assíncrono de assets

---

## 9. PLANO DE TESTES

### Testes de Unidade (EditMode)
- Lógica de placar
- Regras do jogo (impedimento, falta)
- Serialização de dados de save
- Cálculos de física da bola
- IA — decisão de jogada

### Testes de Integração (PlayMode)
- Fluxo completo de partida
- Sistema de câmera
- Input → Personagem → Bola
- Gol → Placar → Replay → Reinício

### Testes de Performance
- 22 personagens em campo (PC e Android)
- 50 partículas simultâneas
- Transição de cena < 2s

---

## 10. DEPENDÊNCIAS EXTERNAS

| Pacote | Versão | Uso |
|--------|--------|-----|
| Input System | 1.7.x | Controles multiplataforma |
| Cinemachine | 3.x | Câmera dinâmica e replay |
| TextMeshPro | 3.x | UI de alta qualidade |
| Addressables | 1.21.x | Carregamento de assets |
| Newtonsoft.Json | 3.x | Serialização de save |
| Universal RP | 17.x | Render pipeline |
| Shadergraph | 17.x | Shaders customizados |
| VFX Graph | 17.x | Efeitos de partícula |

---

*Documento criado pela equipe Campo de Terra FC — Arquitetura v1.0*
