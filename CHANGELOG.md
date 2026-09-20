## [Unreleased]

- Correções visuais (20/09/2026): carro de vitrine cinemático e sem gravidade; plataforma com anel vazado e refletor apontado para o carro.
- Cidade: pivôs de escala unitária impedem que janelas, telhados e outdoors herdem dimensões gigantes dos prédios. Migração idempotente dos objetos antigos ao abrir a cena NeonCoast; alterações pendentes não são salvas automaticamente.
- Aerofólios: eliminadas faces invertidas sobre os mesmos vértices que anulavam as normais; carbono com reflexão mais suave.
- Validação destas correções (20/09/2026): Editor conectado pelo Unity CLI; carro de vitrine estável por 24 segundos; cidade migrada e salva; cinco malhas de aerofólios/winglets sem normais inválidas; console sem erros no teste. Build Windows concluído com zero erros (avisos de shaders Sentis e configuração opcional do Pipeline). Executável inspecionado visualmente sem faixas gigantes ou clarão no aerofólio; teste de luz de posição/freio aprovado.

- Correção validada no standalone: shader próprio para lanternas vermelhas, incluído via Resources, sem reflexão branca. Build 19/09/2026 22:56 sem erros; teste automático de luz de posição/freio aprovado e capturas do executável inspecionadas.

### Corrigido
- Público: pivôs sem herança de escala, membros arredondados, cabelo e calçados; roupas sem emissão neon.
- Público limitado a trechos baixos, deslocamento reduzido e animação distante suspensa.
- Reações do público acompanham o jogador correto, em vez do primeiro veículo encontrado.
- Lanternas: contraste entre posição e freio, freio visível durante a largada e materiais do trânsito sem duplicação na lista de atualização.

### Validação
- Build Windows gerado em 19/09/2026 às 22:48 (horário local), zero erros, 145852609 bytes. Pessoas e lanternas inspecionadas no Editor; execução do novo standalone ainda não testada.
- Terceira luz de freio separada; lente vermelho-rubi e menor reflexão branca.
- Busca global de carros a cada quadro substituída por registro de veículos ativos.

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
