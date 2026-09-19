# Neon Coast Racing 🏁

> **Primeira versão jogável (*First Playable*)**  
> Desenvolvido em **Unity 6000.3.24f1** com **Universal Render Pipeline (URP) 17.3.0** para PC (Windows).

---

## 🎮 Controles

| Ação | Teclado | Controle (Gamepad) |
| :--- | :--- | :--- |
| **Acelerar** | `W` ou `Seta Cima` | Gatilho Direito (`RT`) *(analógico)* |
| **Frear / Ré** | `S` ou `Seta Baixo` | Gatilho Esquerdo (`LT`) *(analógico)* |
| **Direção (Virar)** | `A` / `D` ou `Setas` | Analógico Esquerdo *(analógico)* |
| **Freio de Mão** | `Espaço` | Botão `X` (Xbox) / `Quadrado` (PlayStation) |
| **Nitro (Boost)** | `Shift` | Botão `A` / `RB` (Xbox) ou `X` / `R1` (PS) |
| **Alternar Câmera** | `C` | Botão `Y` (Xbox) / `Triângulo` (PS) |
| **Recuperar Carro** | `R` | Botão `B` (Xbox) / `Círculo` (PS) |
| **Pausar** | `Esc` | Botão `Start` / `Options` |

---

## 🏎️ Visão Geral

Este marco contém um circuito litorâneo contínuo com variações de elevação, seção coberta, entroncamento do porto, barreiras de colisão, carro esportivo customizado, suspensão nas quatro rodas, direção progressiva, campo de visão (FOV) dinâmico sensível à velocidade, nitro, cronometragem com sistema de checkpoints em 3 voltas, HUD de alta visibilidade e ambientação noturna estilo *neon synthwave*.

* **Sem custos com assets de terceiros**: Todas as geometrias e texturas procedurais foram criadas internamente.
* **Pacotes oficiais da Unity**: URP (incluindo Shader Graph), Input System, Cinemachine, ProBuilder, TextMeshPro.
* **Otimização**: Probes de reflexão pré-calculadas (baked cubemaps), geometrias marcadas como estáticas e materiais compatíveis com GPU Instancing.

---

## 🏆 Modos de Jogo & Fluxo

* **Corrida de Circuito (Circuit Race)**: Disputa de 3 voltas contra 3 oponentes controlados por IA, incluindo tráfego civil no percurso.
* **Tomada de Tempo (Time Trial)**: Modo solo para buscar a melhor volta com pista livre (sem rivais ou tráfego civil). Salva recordes pessoais de melhor volta e tempo total no `PlayerPrefs` (`NCR_BestLap`, `NCR_BestRace`), exibindo divisões de tempo e badges no HUD.
* **Menu Principal (`MainMenu.cs`)**: Tela inicial neon com opções de Iniciar Corrida, Tomada de Tempo, Seletor de Veículos e Sair.
* **Menu de Pausa (`PauseMenu.cs`)**: Overlay em jogo para Continuar, Reiniciar ou Retornar ao Menu Principal.
* **Resultados da Corrida (`RaceResults.cs`)**: Tela final exibindo posição, tempo total decorrido, melhor volta e badges de recorde.

---

## 🚗 Garagem & Seleção de Veículos

Escolha e personalize o seu veículo diretamente no Menu Principal:

* **Aster GT (Coastal Spec)**: Carro equilibrado para corrida, com resposta rápida de esterçamento e aderência progressiva.
* **Valkyrie Apex (Hyper Interceptor)**: Aceleração bruta e velocidade máxima extrema (+35% de torque do motor, máxima de 260 km/h e esterçamento mais firme).
* **Shinobi R-Spec (Drift Specialist)**: Ajustado para curvas agressivas, alto ângulo de esterçamento (36°), menor fricção lateral e +80% de regeneração de nitro durante drifts.
* **Acabamentos e Cores**: *Ion Cyan*, *Coral Neon*, *Solar Amber* e *Phantom Violet*. Configurações salvas no `PlayerPrefs` e aplicadas imediatamente na física e na pintura metálica.

---

## 📊 HUD de Alta Visibilidade

Interface redesenhada para máximo contraste e legibilidade:
* **Renderização ScreenSpaceOverlay**: O HUD opera fora do pós-processamento de câmera, evitando textos borrados por bloom ou motion blur.
* **Cartões em Vidro Fumê (*Frosted Obsidian*)**: Painéis escuros translúcidos (`rgba(5, 10, 18, 0.90)`) com filetes em neon ciano/coral.
* **Instrumentos em Tempo Real**:
  * **Status (Topo Esquerdo)**: Nome do veículo, posição na corrida (`POS 1/4`), contagem de voltas (`VOLTA 1/3`) e rastreamento de checkpoints.
  * **Cronômetro (Topo Direito)**: Cronometragem digital precisa, divisão de tempo em relação ao recorde e badges pulsantes de `[NITRO]` e `[DRIFT]`.
  * **Instrumentação (Inferior Direito)**: Velocímetro digital amplo, conta-giros animado, marcha engatada (`GEAR 1-6`) e barra de nitro com efeito de pulso ao ativar.
  * **Banners de Alerta (Centro)**: Contagem regressiva de largada ("3", "2", "1", "VAI!"), alerta de última volta ("ÚLTIMA VOLTA!") e aviso de contra-mão ("SENTIDO CONTRÁRIO!").

---

## ✨ Efeitos Visuais (VFX) & Áudio

* **Luzes de Freio Dinâmicas**: Lanternas traseiras aumentam intensamente a emissão de luz vermelha ao frear ou acionar o freio de mão.
* **Tremor de Câmera de Alta Velocidade**: Micro-vibrações na câmera ao passar de 140 km/h ou ao acionar o nitro.
* **Marcas de Pneu (Skidmarks)**: Marcas dinâmicas deixadas no asfalto alinhadas ao contato das rodas traseiras durante derrapagens (> 12 km/h).
* **Faíscas de Colisão**: Emissão de partículas luminosas nos pontos de contato com guard-rails e barreiras.
* **Exaustão de Nitro**: Cones de partículas em neon ciano nos escapamentos durante o boost.
* **Áudio Procedural**: Sons sintetizados em tempo real (ronco do motor com 6 harmônicos e simulação de marchas, chiado de pneus, ruído de vento em alta velocidade, impacto com barreiras e fanfarra de chegada).

---

## 🛠️ Ordem de Construção (Build Order no Unity Editor)

Caso precise regenerar ou atualizar a cena do jogo via Editor:
1. `Neon Coast/Build Initial Racing Scene` — Gera o circuito e o veículo do jogador.
2. `Neon Coast/Apply Racing and Art Pass` — Adiciona rivais, tráfego, detalhes da pista e fachadas de prédios.
3. `Neon Coast/Build Main Menu Scene` — Cria a cena do menu principal com o mostrador de veículos.
4. `Neon Coast/Rebuild High-Visibility HUD` — Instala o painel ScreenSpaceOverlay na cena NeonCoast.
5. `Neon Coast/Add Pause and Results UI` — Adiciona menu de pausa e tela de resultados.
6. `Neon Coast/Add Race Audio` — Adiciona beeps de contagem regressiva e fanfarra de término.
7. `Neon Coast/Restore Ultra-Crisp Visuals` — Calibra o antialiasing (SMAA), remove borrões de movimento e ajusta o bloom.

---

## 📦 Versionamento e Releases

Este projeto segue o padrão [Semantic Versioning (SemVer)](https://semver.org/lang/pt-BR/) e [Conventional Commits](https://www.conventionalcommits.org/pt-br/):
* As alterações e histórico de versões são documentados no [`CHANGELOG.md`](./CHANGELOG.md).
* As versões oficiais de release são marcadas com tags Git (ex: `v0.1.0`) e publicadas no [GitHub Releases](https://github.com/AxelFahel/neon-coast-racing/releases).
