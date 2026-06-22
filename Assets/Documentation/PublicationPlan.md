# PLANO DE PUBLICAÇÃO — Campo de Terra FC

## 🖥️ STEAM (Windows)

### Requisitos Mínimos
| Componente | Mínimo | Recomendado |
|------------|--------|-------------|
| SO | Windows 10 64-bit | Windows 11 64-bit |
| CPU | Intel Core i3-6100 | Intel Core i5-9400 |
| RAM | 4 GB | 8 GB |
| GPU | NVIDIA GTX 960 / AMD RX 480 | NVIDIA GTX 1060 / AMD RX 580 |
| DirectX | Versão 11 | Versão 12 |
| Armazenamento | 2 GB | 4 GB |

### Checklist Steam
- [ ] Criar conta Steam Direct (taxa de US$ 100 por jogo)
- [ ] Preencher Steam Partner Dashboard
- [ ] Criar página da loja (screenshots, trailer, descrição)
- [ ] Configurar Steamworks SDK no Unity
- [ ] Implementar Steam Achievements via Steamworks.NET
- [ ] Implementar Steam Leaderboards (opcional v1.1)
- [ ] Build Windows 64-bit com IL2CPP
- [ ] Testar instalação limpa
- [ ] Submeter para revisão Steam (prazo ~5 dias úteis)
- [ ] Configurar preço (sugestão: US$ 4,99 — US$ 7,99)
- [ ] Data de lançamento

### Assets de Loja Steam
- [ ] Capsule image: 460x215px (obrigatório)
- [ ] Header image: 460x215px
- [ ] Main capsule: 616x353px
- [ ] Screenshots (mínimo 5): 1280x720px ou 1920x1080px
- [ ] Trailer: 30–90 segundos, 1080p

---

## 📱 GOOGLE PLAY (Android)

### Especificações de Build
| Item | Valor |
|------|-------|
| Min SDK | Android 8.0 (API 26) |
| Target SDK | Android 14 (API 34) |
| Arquitetura | ARM64 (obrigatório a partir de 2019) + ARMv7 |
| Formato | AAB (Android App Bundle — obrigatório) |
| Tamanho máx. | 150 MB (APK) / ilimitado com Play Asset Delivery |

### Configurações Unity para Android
```
Player Settings → Android:
- Company Name: [SeuNome]
- Product Name: Campo de Terra FC
- Package Name: com.seudev.campodeterrafc
- Version: 1.0.0
- Bundle Version Code: 1
- Minimum API Level: Android 8.0
- Target API Level: Automatic (highest installed)
- Scripting Backend: IL2CPP
- Target Architectures: ARMv7 + ARM64
- Internet Access: Not Required
- Write Permission: External (SD Card) → apenas se salvar externamente
- Graphics API: OpenGLES3 + Vulkan (ordenado)
```

### Otimizações Android Obrigatórias
- [ ] Qualidade gráfica: Mobile Low/Medium por padrão
- [ ] Texture Compression: ASTC
- [ ] Addressables para assets grandes (>50 MB)
- [ ] Object Pooling para partículas
- [ ] LOD em todos os personagens (3 níveis)
- [ ] Occlusion Culling configurado
- [ ] Profiling no dispositivo alvo (mínimo Snapdragon 660)
- [ ] Memória máxima: < 300 MB em runtime
- [ ] Bateria: sem uso excessivo em segundo plano

### Checklist Google Play
- [ ] Criar conta Google Play Developer (taxa de US$ 25 única)
- [ ] Criar app no Google Play Console
- [ ] Preencher ficha do aplicativo (PT-BR + EN)
- [ ] Screenshots: mínimo 2 por tipo (telefone, tablet 7", tablet 10")
- [ ] Ícone: 512x512px (PNG, sem alpha no bordas)
- [ ] Banner destaque: 1024x500px
- [ ] Vídeo promocional (YouTube, opcional)
- [ ] Política de privacidade (obrigatório)
- [ ] Questionário de classificação IARC (equivale a ESRB/PEGI)
- [ ] Declaração de conteúdo para menores (COPPA)
- [ ] Build AAB assinado com keystore
- [ ] Teste na faixa interna → alfa → beta → produção

### Keystore
```
# NUNCA commitar o keystore no Git!
# Adicionar ao .gitignore:
*.keystore
*.jks
key.properties
```

---

## 🔑 LICENÇAS DE ASSETS

### Assets Gratuitos Recomendados (Uso Comercial)

| Asset | Fonte | Licença |
|-------|-------|---------|
| Kenney Sports Pack | kenney.nl | CC0 |
| Mixamo Animations | mixamo.com | Gratuito p/ uso comercial |
| Freesound.org (SFX) | freesound.org | CC0 / CC-BY |
| Google Fonts (UI) | fonts.google.com | OFL |
| OpenGameArt (sprites) | opengameart.org | CC0 / CC-BY |

### Assets que Precisam de Licença
- Músicas: usar royalty-free (Pixabay, Incompetech, Epidemic Sound)
- Modelos 3D de campo: criar ou comprar no Unity Asset Store

---

## 🗓️ CRONOGRAMA DE LANÇAMENTO

```
Mês 1–3   → Sprint 1–4 (fundação, jogabilidade, IA, menus)
Mês 4–5   → Sprint 5–6 (campeonato, save, android, otimização)
Mês 6     → Sprint 7 (polimento, QA)
Mês 6 fim → Release Candidate + submissão Steam/Google Play
Mês 7     → Launch v1.0
Mês 8+    → Patches, conteúdo adicional, v1.1 com multiplayer
```

---

## 💰 MONETIZAÇÃO

### Steam (Premium)
- Preço sugerido: **R$ 19,90 – R$ 29,90** (≈ US$ 3,99 – US$ 5,99)
- DLC futura: "Pack de Times Históricos"

### Google Play (Freemium)
- Download gratuito
- Removedor de anúncios: R$ 9,90 (compra única)
- Pack de times extras: R$ 4,90
- SEM pay-to-win
- Anúncios: banner inferior apenas no menu, nunca durante o jogo

---

*Plano de publicação — Campo de Terra FC v1.0*
