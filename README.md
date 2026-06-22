# CampoDeTerraFC

Jogo de futebol desenvolvido na Unity 2022.3 LTS com Universal Render Pipeline (URP).

---

## Sobre o Projeto

CampoDeTerraFC é um jogo de futebol independente em desenvolvimento.  
O projeto usa Unity 2022.3 LTS com URP como pipeline de renderização.

---

## Requisitos

- Unity **2022.3 LTS** (versão exata recomendada: mesma do ProjectSettings/ProjectVersion.txt)
- Universal Render Pipeline (já incluído via Packages/)
- Windows 10/11 ou macOS 12+

---

## Como Abrir o Projeto

1. Clone o repositório:
   ```
   git clone https://github.com/SEU_USUARIO/CampoDeTerraFC.git
   ```
2. Abra o **Unity Hub**
3. Clique em **Add > Add project from disk**
4. Selecione a pasta `CampoDeTerraFC/`
5. Aguarde o Unity recompilar a pasta `Library/` (primeira abertura demora alguns minutos)
6. Abra a cena principal em `Assets/_Project/Scenes/`

> A pasta `Library/` não está no repositório — o Unity a gera automaticamente na primeira abertura.

---

## Estrutura do Projeto

```
Assets/
└── _Project/
    ├── Scenes/         — cenas do jogo
    ├── Scripts/        — código C#
    ├── Prefabs/        — objetos reutilizáveis
    ├── Materials/      — materiais URP
    ├── Textures/       — texturas e sprites
    ├── Models/         — modelos 3D
    ├── Animations/     — animações e controllers
    ├── Audio/          — música e efeitos sonoros
    └── UI/             — elementos de interface
```

---

## Pipeline de Renderização

Este projeto usa **URP (Universal Render Pipeline)**.  
Os assets de configuração URP estão em `Assets/Rendering/`.

Se materiais aparecerem rosa ao abrir o projeto:
```
Edit > Rendering > Materials > Convert All Built-in Materials to URP
```

---

## Convenção de Commits

| Prefixo | Uso |
|---|---|
| `feat:` | nova funcionalidade ou asset |
| `fix:` | correção de bug |
| `chore:` | manutenção, upgrade, organização |
| `docs:` | documentação |
| `refactor:` | refatoração sem mudança de comportamento |

---

## Status do Desenvolvimento

- [x] Estrutura base do projeto
- [x] URP configurado
- [x] Repositório Git inicializado
- [ ] Mecânicas de jogo
- [ ] Interface do jogador
- [ ] Cenas do campeonato

---

## Licença

Projeto privado — todos os direitos reservados.
