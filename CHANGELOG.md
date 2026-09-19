# Changelog

Todas as alterações notáveis deste projeto serão documentadas neste arquivo.

O formato é baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/),
e este projeto adere ao [Semantic Versioning](https://semver.org/lang/pt-BR/).

---

## [0.2.2] - 2026-09-19

### Modificado
- **Pós-Processamento e Iluminação Noturna (`NeonNight.asset`)**:
  - Ajuste de Tonemapping para modo Neutro e Bloom com Clamp a 10.0, eliminando superexposição e burnouts brancos em luzes intensas.
- **Visual dos Carros e Rodas (`CarVisualsOverhaul.cs`)**:
  - Pneus procedurais em cilindro ajustados ao redor dos aros 3D para acabamento impecável.
  - Adição de luz pontual vermelha sutil sob a traseira (`Car_TailGlow`) projetando luz suave no asfalto.
- **Sincronização de Cenas e Build**:
  - Cenas `NeonCoast.unity` e `MainMenu.unity` salvas e sincronizadas nas configurações do projeto.

---

## [0.2.1] - 2026-09-19

### Corrigido
- **Eliminação do Clarão Branco nas Lanternas Traseiras**:
  - Removido multiplicador excessivo de intensidade HDR que causava sobreexposição (*white burnout*) no Tonemapping ACES e Bloom da câmera.
  - Calibração do material `SupercarTailLED` para vermelho rubi puro (`Color(0.88, 0.02, 0.04)`) preservando a nitidez sem emitir clarão branco.
  - Atualização dos estados dinâmicos de freio (`tailNormal` e `tailBraking`) com intensidades calibradas.
  - Purga completa de cubos primitivos residuais e luzes espúrias em todos os veículos da cena (jogador, rivais e tráfego civil).
  - Adicionado `ApplyTrafficSpecs` no `ArcadeCar.cs` para garantir que o tráfego civil também utilize materiais limpos sem clarão.

---

## [0.2.0] - 2026-09-19

### Adicionado
- **Suporte Oficial a Redes Neurais com Unity Sentis**:
  - Inclusão da dependência nativa e gratuita `com.unity.sentis: 2.1.3` no `Packages/manifest.json`.
- **IA de Pilotagem Competitiva Avançada (`PaceDriver.cs`)**:
  - **Frenagem Preditiva em Curvas (*Corner Anticipation*)**: Cálculo de curvatura à frente com desaceleração ideal antes do ingresso na curva.
  - **Traçado Ideal (*Apex Hunting*)**: Linha de corrida inteligente buscando o ápice e otimizando a tangência nas curvas.
  - **Sistema de Vácuo (*Slipstream*) e Ultrapassagem Ativa**: Detecção de arrasto atrás de oponentes com troca proativa de faixa para manobras de ultrapassagem.
  - **Gestão Tática de Nitro**: Ativação inteligente do boost em retas, saídas de curvas e disputas diretas.
  - **Drift com Freio de Mão em Curvas Fechadas (*Hairpin Drift*)**: Controle autônomo de `aiHandbrake` para rotacionar o carro em cotovelos.
  - **Sensores de Colisão Laterais**: Raycasts direcionais para evitar prensar o jogador ou colidir contra guard-rails.
  - **Dificuldade Adaptativa (*Rubberbanding*)**: Ajuste dinâmico de ritmo para corridas competitivas do início ao fim.
  - **Recuperação Inteligente com Manobra de Ré**: Desvencilhamento autônomo ao detectar travamentos contra obstáculos.
- **Controle de Automação Estendido em `ArcadeCar.cs`**:
  - Suporte ao campo `aiHandbrake` para automação.

---

## [0.1.0] - 2026-09-19

### Adicionado
- **Circuito Noturno Contínuo**: Pista litorânea completa com elevação, túnel/seção coberta, cruzamento de porto e barreiras de colisão com guard-rails.
- **Sistema de Veículos & Física**:
  - Simulação de suspensão nas 4 rodas com `WheelCollider`.
  - Direção progressiva e assistência de estabilidade de drift.
  - 3 modelos selecionáveis na Garagem: *Aster GT* (balanceado), *Valkyrie Apex* (velocidade bruta) e *Shinobi R-Spec* (drift especializado).
  - 4 pinturas neon metálicas: *Ion Cyan*, *Coral Neon*, *Solar Amber* e *Phantom Violet*.
- **Modos de Jogo**:
  - *Circuit Race*: Disputa de 3 voltas contra 3 oponentes por IA e veículos civis.
  - *Time Trial*: Tomada de tempo solo com persistência de recordes locais (`PlayerPrefs`).
- **Interface & HUD de Alta Visibilidade**:
  - Painéis *ScreenSpaceOverlay* com acabamento *Frosted Obsidian*.
  - Velocímetro digital, conta-giros, medidor de nitro e indicador de marchas (1-6).
  - Cronômetro digital com parciais de volta e banners de alerta ("3, 2, 1, VAI!", "ÚLTIMA VOLTA!").
  - Menus completos de Início, Pausa e Resultados da corrida.
- **Efeitos Visuais (VFX) & Áudio Procedural**:
  - Marcas de pneu no asfalto (*Skidmarks*) em derrapagens.
  - Faíscas em impactos e exaustão de nitro com partículas neon.
  - Síntese de áudio procedural sem dependência de assets de áudio externos (motor com 6 harmônicos, vento, chiado de pneus e fanfarra).
- **Documentação**:
  - README oficial em Português com guia de controles e ordem de build.
