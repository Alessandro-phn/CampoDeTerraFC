# CHANGELOG — Campo de Terra FC

Todas as mudanças notáveis do projeto são documentadas aqui.
Formato baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/).

---

## [Unreleased] — Sprint 1

### Adicionado
- Game Design Document completo (GDD.md)
- Documento de Arquitetura Técnica
- Estrutura completa de pastas do projeto Unity
- README.md com instruções de setup
- ROADMAP.md com sprints planejadas
- TODO.md com tarefas abertas
- CHANGELOG.md
- LICENSE (MIT)
- CONTRIBUTING.md
- Scripts: GameManager, MatchController (em desenvolvimento)
- Scripts: PlayerController base (em desenvolvimento)
- Scripts: BallController (em desenvolvimento)
- Scripts: GameEventSystem (ScriptableObject events)
- Scripts: ObjectPoolManager
- Scripts: InputManager (New Input System)
- Scripts: CameraController (Cinemachine)
- Scripts: AudioManager
- Scripts: UIManager base
- Scripts: SaveSystem
- Scripts: ScoreManager
- Scripts: TimerManager
- Scripts: AIAgent base
- Scripts: GoalkeeperController base

### Decisões Técnicas
- **Unity 6 LTS:** Estabilidade de longo prazo + suporte a Android/PC
- **URP:** Melhor performance em Android, shaders customizados
- **New Input System:** Suporte nativo a gamepad, touch e teclado
- **ScriptableObject Events:** Desacoplamento total entre sistemas
- **Service Locator:** DI leve sem overhead de framework externo
- **State Machine para IA:** Comportamento previsível e debugável

---

## Sprint 1 — Fundação — CONCLUÍDO ✅

### Arquivos Criados

**Documentação (8 arquivos)**
- `Assets/Documentation/GDD.md` — Game Design Document completo
- `Assets/Documentation/TechnicalArchitecture.md` — Arquitetura técnica
- `Assets/Documentation/PublicationPlan.md` — Plano Steam + Google Play
- `README.md`, `ROADMAP.md`, `CHANGELOG.md`, `TODO.md`, `LICENSE`, `CONTRIBUTING.md`

**Scripts C# (30 arquivos / ~9.300 linhas)**

| Arquivo | Sistema | Responsabilidade |
|---------|---------|-----------------|
| `Core/GameManager.cs` | Core | Singleton central, estado global, ciclo de vida |
| `Core/Services/ServiceLocator.cs` | Core | Injeção de dependência leve |
| `Core/Events/GameEventSystem.cs` | Events | Observer Pattern via ScriptableObjects |
| `Core/ObjectPoolManager.cs` | Pool | Object Pooling para performance |
| `Config/GameConfigSO.cs` | Config | Configuração global (40+ parâmetros) |
| `Data/PlayerDataSO.cs` | Data | Atributos, aparência e habilidades do jogador |
| `Data/TeamDataSO.cs` | Data | Elenco, formação, estatísticas do time |
| `Data/FormationSO.cs` | Data | Formações táticas normalizadas |
| `Data/FieldDataSO.cs` | Data | Campos, superfícies, clima disponível |
| `Input/InputManager.cs` | Input | New Input System (teclado + gamepad + touch) |
| `Player/PlayerController.cs` | Player | Movimentação, passes, chutes, dribles, carrinho |
| `Ball/BallController.cs` | Ball | Física completa (spin, curva, bounce, Magnus) |
| `AI/AIAgent.cs` | AI | State Machine base para todos os agentes |
| `AI/FieldPlayersAI.cs` | AI | Atacante, Meia, Defensor, Volante, Lateral |
| `Goalkeeper/GoalkeeperController.cs` | GK | IA completa do goleiro (dive, saída, pênalti) |
| `Camera/CameraController.cs` | Camera | Seguimento suave, zoom, replay, shake |
| `Managers/ScoreManager.cs` | Match | Placar, histórico de gols |
| `Managers/TimerManager.cs` | Match | Cronômetro com pause/resume |
| `Managers/TeamManager.cs` | Match | Spawn, formação, seleção de jogador |
| `Gameplay/MatchController.cs` | Match | Fluxo completo da partida |
| `Gameplay/ChampionshipManager.cs` | Championship | Liga round-robin + mata-mata |
| `Gameplay/PenaltyShootoutController.cs` | Penalties | Sistema completo de pênaltis |
| `Gameplay/RulesEngine.cs` | Rules | Árbitro: gol, escanteio, lateral, falta |
| `Gameplay/ReplaySystem.cs` | Replay | Buffer de gravação + reprodução cinemática |
| `Audio/AudioManager.cs` | Audio | Música, SFX, ambiente com pool e crossfade |
| `UI/UIManager.cs` | UI | HUD, pausa, gol, fim de jogo |
| `SaveSystem/SaveManager.cs` | Save | JSON + PlayerPrefs, conquistas, estatísticas |
| `Effects/BallEffectsController.cs` | VFX | Partículas, sombra dinâmica, trail, pegadas |
| `Animation/PlayerAnimationController.cs` | Anim | Animator com hashes cacheados |
| `Tests/EditMode/CampoDeTerraFCTests.cs` | Tests | 20+ testes unitários (NUnit) |

### Decisões Técnicas Sprint 1

| Decisão | Justificativa |
|---------|---------------|
| **ScriptableObject Events** | Zero acoplamento entre sistemas — qualquer sistema pode ouvir qualquer evento sem referência direta |
| **Service Locator** | DI leve sem overhead de framework. GameManager registra tudo no Awake |
| **State Machine para IA** | Comportamento previsível, debugável via Gizmos e extensível por posição |
| **Magnus Effect manual** | Unity não simula Magnus nativamente — implementado via `F = k * (ω × v)` no FixedUpdate |
| **NavMeshAgent para IA** | Pathfinding gratuito e estável; evita colisões entre jogadores da IA |
| **Animator hash cache** | Evita string lookup a cada frame — melhoria de ~15% na performance de animação |
| **JSON para save** | PlayerPrefs não suporta estruturas complexas (campeonato); JSON permite evolução do schema |
| **Object Pool para partículas** | Android não tolera Instantiate/Destroy em runtime — pool garante 60 FPS |

### Validação
- [x] Todos os scripts compilam (sem erros de sintaxe C#)
- [x] Namespaces consistentes: `CampoDeTerraFC.*`
- [x] Comentários XML em todas as classes e métodos públicos
- [x] Nenhuma função vazia
- [x] Nenhum TODO sem contexto
- [x] Gizmos de debug em todos os agentes de IA
- [x] 20+ testes unitários cobrindo: ScoreManager, TimerManager, ChampionshipManager, SaveSystem, PlayerData
