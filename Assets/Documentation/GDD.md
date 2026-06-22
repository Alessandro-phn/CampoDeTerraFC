# CAMPO DE TERRA FC — GAME DESIGN DOCUMENT v1.0

## 1. VISÃO GERAL

**Título:** Campo de Terra FC  
**Gênero:** Futebol Arcade / Sports  
**Engine:** Unity 6 LTS + URP  
**Plataformas:** Windows (Steam), Android (Google Play)  
**Classificação indicativa:** Livre  
**Público-alvo:** Todas as idades (foco em 8–35 anos)  
**Modo:** Single Player vs IA, Multiplayer local (2 jogadores), Campeonato  

---

## 2. CONCEITO CENTRAL

Campo de Terra FC captura a essência do futebol de rua brasileiro — a pelada do bairro.  
Campos de terra batida, traves de madeira, redes gastas, crianças descalças, grito da vizinhança.  
Nostalgia. Alegria. A autenticidade do futebol que não precisa de estádio.

**Pilares de Design:**
- **Nostalgia:** Visual e sonoro remetem à infância brasileira dos anos 80/90/2000
- **Acessibilidade:** Fácil de aprender em minutos
- **Profundidade:** Difícil de dominar — técnica, timing e leitura de jogo recompensados
- **Alegria:** Cada momento deve ser divertido

---

## 3. MECÂNICAS PRINCIPAIS

### 3.1 Movimentação
- Analógico / WASD para direção
- Sprint automático ao manter direção + botão
- Virada rápida com penalidade de velocidade
- Inércia realista proporcional ao peso do personagem

### 3.2 Sistema de Passe
| Tipo | Botão | Descrição |
|------|-------|-----------|
| Passe Curto | A / Q | Passe rasteiro para jogador mais próximo na direção |
| Passe Longo | X / E | Passe aéreo com curva e alcance maior |
| Lançamento | Y / R | Bola longa em profundidade para o atacante |

### 3.3 Sistema de Chute
| Tipo | Botão | Descrição |
|------|-------|-----------|
| Chute Colocado | B / F | Precisão, pouca força, canto da trave |
| Chute Forte | B (segurar) | Máxima potência, menos precisão |
| Cabeceio | B no ar | Automaticamente ao saltar com bola aérea |
| Carrinho | LB / Shift | Disputa física, pode gerar falta |

### 3.4 Sistema de Drible
- Drible básico: direção + botão de drible
- Dribles especiais: sequência de inputs (unlock por evolução do jogador)
- Toque de calcanhar, meia-lua, bicicleta

### 3.5 Física da Bola
- Peso: 430g (regulamentar)
- Spin aplicado no momento do contato
- Efeito lateral (curva)
- Efeito cobertura (parábola)
- Atrito variável: terra batida > grama > cimento
- Bounce realista com amortecimento

---

## 4. MODOS DE JOGO

### 4.1 Pelada
- Jogo rápido, 5 a 20 minutos
- Escolha de time, campo, clima
- 2 a 5 jogadores por lado (configurável)
- Sem arbitragem rígida (estilo pelada real)

### 4.2 Campeonato
- Formato liga (round-robin) + mata-mata
- 8 ou 16 times
- Classificação por pontos, saldo de gols, gols pró
- Quartas, Semifinal, Final, Terceiro Lugar
- Troféu e comemoração cinematográfica

### 4.3 Pênaltis
- Série de 5 cobranças
- Sistema de direção (mira) + potência
- Goleiro com IA que lê o corpo do cobrador
- Morte súbita se necessário

---

## 5. PERSONAGENS

### 5.1 Elenco Base
| ID | Nome | Posição | Habilidade especial |
|----|------|---------|---------------------|
| 01 | Pedrão | CA | Chute forte |
| 02 | Índio | ME | Drible desconcertante |
| 03 | Magrelo | LA | Velocidade extrema |
| 04 | Gordo | ZA | Força física |
| 05 | Tigrão | GO | Reflexo felino |
| 06 | Neguinho | ME | Toque e passe |
| 07 | Branquinha | CA | Cabeceio certeiro |
| 08 | Cacau | ZA | Carrinho preciso |
| 09 | Galinha | LA | Cruzamento preciso |
| 10 | Daninha | VO | Marcação implacável |

### 5.2 Atributos
- **Velocidade** (1–99)
- **Chute** (1–99)
- **Passe** (1–99)
- **Drible** (1–99)
- **Defesa** (1–99)
- **Físico** (1–99)
- **Reflexo** (1–99, goleiros)

---

## 6. TIMES

| Time | Estilo | Cor 1 | Cor 2 |
|------|--------|-------|-------|
| Vila Operária FC | Físico e raça | Vermelho | Branco |
| Beira Rio SC | Técnico e veloz | Azul | Amarelo |
| Estrela do Bairro | Equilibrado | Verde | Branco |
| Favela United | Criativo e imprevisível | Preto | Laranja |
| Praça Central FC | Defensivo | Cinza | Azul |
| Santo André Kids | Jovem e atacante | Amarelo | Verde |
| Amigos do Bar | Caótico e divertido | Rosa | Roxo |
| Vila Nova SC | Tradicional e organizado | Azul | Branco |

---

## 7. CAMPOS

| Campo | Superfície | Clima | Peculiaridade |
|-------|-----------|-------|---------------|
| Campão do Bairro | Terra batida | Ensolarado | Campo irregular |
| Quadra da Escola | Cimento | Variável | Sem lateral |
| Pelada do Mangue | Grama baixa | Chuva frequente | Lama nos cantos |
| Terreno Baldio | Terra + pedra | Vento | Bola desvia |
| Rua Fechada | Asfalto | Noite | Postes iluminam |

---

## 8. PROGRESSÃO E CONQUISTAS

### Conquistas
- Primeiro Gol
- Hat-trick (3 gols num jogo)
- Gol de bicicleta
- Pênalti perfeito (canto)
- Defesa impossível (goleiro)
- Campeão do bairro (vencer campeonato)
- Invicto (campeonato sem perder)
- Goleada histórica (7+ gols)

---

## 9. INTERFACE

### HUD em Jogo
- Placar centralizado no topo
- Cronômetro regressivo
- Minimap (opcional nas configurações)
- Indicadores de posse de bola
- Seta de indicação do jogador controlado
- Flash de evento (gol, falta, escanteio)

### Menus
- **Menu Principal:** Animação de fundo (criança chutando na rua)
- **Seleção de Time:** Cartas estilo álbum de figurinhas
- **Campeonato:** Tabela estilo quadro negro da escola

---

## 10. ESTILO VISUAL

- Low poly expressivo (não ultra realista)
- Paleta quente: laranja, amarelo, vermelho terra, verde desbotado
- Sombras suaves (URP Lit Shader)
- Partículas de poeira ao correr e chutar
- Pegadas na terra
- Efeitos de impacto na bola
- Luz de fim de tarde como padrão

---

## 11. DESIGN DE ÁUDIO

### Música
- Samba-funk instrumental de abertura
- Música ambiente de bairro durante jogo
- Jingle de gol animado
- Música triste de derrota
- Fanfarra de campeonato

### Efeitos Sonoros
- Chute (couro, curto, longo)
- Defesa do goleiro
- Trave (metálico/madeira)
- Gol (rede balançando)
- Apito (início, fim, falta)
- Poeira ao correr
- Multidão/vizinhança reagindo
- Pássaros, vento, cachorro ao fundo

---

## 12. FLUXO DO JOGO

```
Menu Principal
    ├── Pelada → Selecionar Times → Selecionar Campo → Jogar
    ├── Campeonato → Criar/Carregar → Selecionar Time → Fase de Grupos → Mata-Mata → Final
    ├── Pênaltis → Selecionar Times → Cobrar
    └── Configurações → Áudio/Vídeo/Controles/Idioma
```

---

## 13. CONTROLES

### PC (Teclado)
| Ação | Tecla |
|------|-------|
| Mover | WASD / Setas |
| Sprint | Shift |
| Passe curto | Q |
| Passe longo | E |
| Lançamento | R |
| Chute / Confirmar | F |
| Drible | D (contexto) |
| Carrinho | Ctrl |
| Mudar jogador | Tab |
| Pause | Esc |

### PC (Gamepad - Xbox layout)
| Ação | Botão |
|------|-------|
| Mover | Analógico Esquerdo |
| Sprint | LT |
| Passe curto | A |
| Passe longo | X |
| Lançamento | Y |
| Chute | B |
| Drible | RB |
| Carrinho | LB |
| Mudar jogador | R1 |

### Mobile (Touch)
| Ação | Gesto |
|------|-------|
| Mover | Joystick virtual esquerdo |
| Sprint | Automático ao correr |
| Passe | Botão esquerdo (passar) |
| Chute | Botão direito (chutar) |
| Drible | Swipe no joystick |
| Carrinho | Botão deslizante |

---

*Documento criado pela equipe Campo de Terra FC — v1.0*
