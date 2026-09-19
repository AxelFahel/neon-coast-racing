using UnityEngine;

namespace NeonCoast {
[DefaultExecutionOrder(-20)]
public class PaceDriver : MonoBehaviour {
    public Vector3[] path;
    public float cruiseSpeed = 21f;
    public float laneOffset;
    public bool traffic;
    public float aggression = .5f;

    public int Recoveries { get; private set; }

    ArcadeCar car;
    ArcadeCar[] vehicles;
    ArcadeCar playerCar;
    float currentLane;
    float stuckTime;
    float reverseTimer;
    int closest;

    // Constantes de tunelamento e navegação
    const float StuckThreshold    = 3.0f;
    const float LookaheadBase     = 8f;
    const float TunnelHalfWidth   = 2.8f;
    const float TunnelHalfHeight  = 3.0f;
    const float TunnelLength      = 24f;
    const float RaycastSideDist   = 2.2f;

    void Awake() {
        car = GetComponent<ArcadeCar>();
        car.automation = true;
        currentLane = laneOffset;
    }

    void Start() {
        RefreshVehicleList();
        // Para carros de corrida (não tráfego urbano), eleva a velocidade competitiva
        // alinhando com a potência real e personalidade dos pilotos rivais
        if (!traffic && car != null) {
            var racer = GetComponent<GridRacer>();
            string rName = racer ? racer.driverName : "";
            if (rName.Contains("KAI")) {
                aggression = 0.94f;
                cruiseSpeed = Mathf.Max(cruiseSpeed, car.topSpeed * 0.95f);
            } else if (rName.Contains("NOVA")) {
                aggression = 0.86f;
                cruiseSpeed = Mathf.Max(cruiseSpeed, car.topSpeed * 0.92f);
            } else {
                aggression = 0.78f;
                cruiseSpeed = Mathf.Max(cruiseSpeed, car.topSpeed * 0.88f);
            }
        }
    }

    /// <summary>
    /// Re-escaneia todos os veículos na cena e localiza o jogador.
    /// </summary>
    public void RefreshVehicleList() {
        vehicles = FindObjectsByType<ArcadeCar>(FindObjectsSortMode.None);
        playerCar = null;
        foreach (var v in vehicles) {
            if (v && !v.automation) {
                playerCar = v;
                break;
            }
        }
    }

    void Update() {
        if (path == null || path.Length < 4) return;

        // ── 1. Localiza o nó do traçado mais próximo ────────────────────────
        float bestDistSq = float.MaxValue;
        for (int i = 0; i < path.Length; i++) {
            float d = (path[i] - transform.position).sqrMagnitude;
            if (d < bestDistSq) {
                bestDistSq = d;
                closest = i;
            }
        }

        // ── 2. Análise de Curvatura Preditiva (Corner Anticipation) ──────────
        // Examina o traçado à frente para calcular a velocidade segura da curva
        float maxCurvatureAngle = 0f;
        int checkSteps = Mathf.Clamp((int)(car.SpeedKmh / 15f) + 2, 3, 7);
        for (int s = 1; s < checkSteps; s++) {
            int pPrev = (closest + s - 1) % path.Length;
            int pCurr = (closest + s) % path.Length;
            int pNext = (closest + s + 1) % path.Length;
            Vector3 d1 = (path[pCurr] - path[pPrev]).normalized;
            Vector3 d2 = (path[pNext] - path[pCurr]).normalized;
            float angle = Vector3.Angle(d1, d2);
            if (angle > maxCurvatureAngle) maxCurvatureAngle = angle;
        }

        // ── 3. Dinâmica de Faixa & Ultrapassagem (Racing Line & Overtake) ────
        float desiredLane = laneOffset;
        float obstacleLimit = cruiseSpeed;
        bool slipstreaming = false;
        bool wantOvertake = false;

        if (vehicles != null) {
            foreach (var other in vehicles) {
                if (!other || other == car) continue;
                Vector3 localOther = transform.InverseTransformPoint(other.transform.position);

                bool inFrontTunnel = localOther.z > 0 && localOther.z < TunnelLength &&
                                     Mathf.Abs(localOther.x) < TunnelHalfWidth &&
                                     Mathf.Abs(localOther.y) < TunnelHalfHeight;

                if (!inFrontTunnel) continue;

                // Vácuo (Slipstream) detectado quando alinhado logo à frente
                if (localOther.z < 18f && Mathf.Abs(localOther.x) < 1.4f) {
                    slipstreaming = true;
                }

                // Ajusta limite de velocidade com base no carro à frente
                obstacleLimit = Mathf.Min(obstacleLimit,
                    Mathf.Max(0, other.ForwardSpeed) + (localOther.z - 7.5f) * 0.75f);

                // Decisão tática de ultrapassagem para rivais (não tráfego)
                if (!traffic && localOther.z > 4.5f) {
                    wantOvertake = true;
                    float leftCandidate = -3.2f;
                    float rightCandidate = 3.2f;
                    bool leftFree = IsLaneFree(leftCandidate);
                    bool rightFree = IsLaneFree(rightCandidate);

                    if (leftFree && (!rightFree || laneOffset > 0)) desiredLane = leftCandidate;
                    else if (rightFree) desiredLane = rightCandidate;
                }
            }
        }

        // Traçado dinâmico em curvas (Apex Hunting) para rivais livres
        if (!traffic && !wantOvertake && maxCurvatureAngle > 10f) {
            // Em curvas acentuadas, posiciona-se no apex ideal
            int apexNode = (closest + 3) % path.Length;
            Vector3 tangent = (path[(apexNode + 1) % path.Length] - path[apexNode]).normalized;
            Vector3 cross = Vector3.Cross(Vector3.up, tangent);
            float turnDirection = Vector3.Dot(cross, transform.forward);
            desiredLane = (turnDirection > 0 ? -1f : 1f) * 2.0f;
        }

        // Transição suave para a faixa desejada
        float laneSpeed = traffic ? 1.0f : Mathf.Lerp(2.2f, 4.0f, aggression);
        currentLane = Mathf.MoveTowards(currentLane, desiredLane, Time.deltaTime * laneSpeed);

        // ── 4. Ponto Alvo (Lookahead com Pure Pursuit) ──────────────────────
        int next = (closest + 1) % path.Length;
        float lookaheadDist = LookaheadBase + Mathf.Max(0, car.ForwardSpeed) * 0.7f;
        for (int step = 0; step < path.Length; step++) {
            lookaheadDist -= Vector3.Distance(path[next], path[(next + 1) % path.Length]);
            if (lookaheadDist <= 0) break;
            next = (next + 1) % path.Length;
        }

        Vector3 pathTangent = (path[(next + 1) % path.Length] - path[(next + path.Length - 1) % path.Length]).normalized;
        Vector3 aimPoint = path[next] + Vector3.Cross(Vector3.up, pathTangent).normalized * currentLane;
        Vector3 localAim = transform.InverseTransformPoint(aimPoint);

        // ── 5. Esterçamento (Steering & Side Sensor Repulsion) ──────────────
        float pursuitAngle = Mathf.Atan2(2f * 2.63f * localAim.x,
            Mathf.Max(1f, localAim.x * localAim.x + localAim.z * localAim.z)) * Mathf.Rad2Deg;

        // Sensores laterais (Raycasts) para repelir colisões com guard-rails ou outros carros
        float avoidanceSteer = 0f;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        if (Physics.Raycast(rayOrigin, transform.right, out RaycastHit hitRight, RaycastSideDist)) {
            avoidanceSteer -= Mathf.Lerp(0.5f, 0f, hitRight.distance / RaycastSideDist);
        }
        if (Physics.Raycast(rayOrigin, -transform.right, out RaycastHit hitLeft, RaycastSideDist)) {
            avoidanceSteer += Mathf.Lerp(0.5f, 0f, hitLeft.distance / RaycastSideDist);
        }

        float maxSteerDeg = Mathf.Lerp(car.steerAngleMax, car.steerAngleMin,
            Mathf.Clamp01(Mathf.Abs(car.ForwardSpeed) / car.topSpeed));
        float normalizedSteer = Mathf.Clamp((pursuitAngle / maxSteerDeg) + avoidanceSteer, -1f, 1f);

        // ── 6. Dificuldade Adaptativa (Rubberbanding) ────────────────────────
        float rubberbandFactor = 1.0f;
        if (!traffic && playerCar != null) {
            float distToPlayer = Vector3.Distance(transform.position, playerCar.transform.position);
            Vector3 toPlayer = playerCar.transform.position - transform.position;
            bool playerIsAhead = Vector3.Dot(transform.forward, toPlayer) > 0;

            if (playerIsAhead && distToPlayer > 35f) {
                // Jogador muito à frente: rival acelera moderadamente para disputar
                rubberbandFactor = Mathf.Lerp(1.0f, 1.18f, Mathf.Clamp01((distToPlayer - 35f) / 60f));
            } else if (!playerIsAhead && distToPlayer > 45f) {
                // Jogador muito atrás: rival dá uma margem para o jogador recuperar
                rubberbandFactor = Mathf.Lerp(1.0f, 0.88f, Mathf.Clamp01((distToPlayer - 45f) / 60f));
            }
        }

        // ── 7. Controle de Velocidade, Curva e Frenagem ─────────────────────
        // Redução preditiva para a curva
        float cornerSpeedCap = cruiseSpeed;
        if (maxCurvatureAngle > 8f) {
            float cornerRatio = Mathf.Clamp01(maxCurvatureAngle / 40f);
            cornerSpeedCap = Mathf.Lerp(cruiseSpeed, cruiseSpeed * 0.48f, cornerRatio);
        }

        float effectiveCruise = (cruiseSpeed * rubberbandFactor);
        float targetSpeed = Mathf.Min(effectiveCruise, cornerSpeedCap);

        if (Mathf.Abs(currentLane - desiredLane) > 1.2f || traffic) {
            targetSpeed = Mathf.Min(targetSpeed, obstacleLimit);
        }
        if (obstacleLimit < 2.5f) targetSpeed = 0f;

        // Vácuo dá um boost na velocidade desejada
        if (slipstreaming && !traffic) targetSpeed *= 1.12f;

        // Frenagem por freio de mão em curvas extremamente fechadas (Hairpin Drift)
        bool needDrift = !traffic && maxCurvatureAngle > 38f && car.SpeedKmh > 52f && aggression > 0.6f;
        car.aiHandbrake = needDrift;

        // Aplicação do acelerador / freio
        if (reverseTimer > 0) {
            // Manobra de ré para desvencilhar
            reverseTimer -= Time.deltaTime;
            car.testThrottle = -0.8f;
            car.testSteer = -normalizedSteer;
            car.aiBoost = false;
        } else {
            if (car.ForwardSpeed > targetSpeed + 0.8f) {
                car.testThrottle = -1.0f; // Freio ativo
            } else if (car.ForwardSpeed < targetSpeed) {
                car.testThrottle = 1.0f;  // Aceleração total
            } else {
                car.testThrottle = 0.2f;  // Manutenção de velocidade
            }
            car.testSteer = normalizedSteer;

            // ── 8. Gestão Tática do Nitro ──────────────────────────────────
            // Usa nitro em retas, na saída de curvas ou em ultrapassagens
            bool straightTrack = Mathf.Abs(pursuitAngle) < 3.5f && maxCurvatureAngle < 8f;
            bool highAggressionOpportunity = wantOvertake || slipstreaming;

            car.aiBoost = !traffic &&
                          (straightTrack || highAggressionOpportunity) &&
                          car.SpeedKmh > 48f &&
                          car.Nitro > 0.35f &&
                          obstacleLimit >= 14f &&
                          aggression > 0.4f;
        }

        // ── 9. Detecção de Bloqueio & Recuperação Inteligente ────────────────
        if (car.controlsEnabled && car.SpeedKmh < 2.0f && reverseTimer <= 0) {
            stuckTime += Time.deltaTime;
        } else {
            stuckTime = Mathf.Max(0, stuckTime - Time.deltaTime * 0.5f);
        }

        // Tenta ré antes de forçar o reset total
        if (stuckTime > StuckThreshold && reverseTimer <= 0) {
            reverseTimer = 1.2f;
            stuckTime = 0;
        }

        // Reset completo se capotar, cair ou permanecer travado após a tentativa de ré
        bool fellOffTrack = transform.position.y < path[closest].y - 5.5f;
        if (stuckTime > (StuckThreshold * 2.5f) || fellOffTrack) {
            Vector3 forward = (path[(closest + 1) % path.Length] - path[closest]).normalized;
            car.SetSpawn(
                path[closest] + Vector3.Cross(Vector3.up, forward) * laneOffset + Vector3.up * 0.8f,
                Quaternion.LookRotation(forward));
            car.ResetCar();
            stuckTime = 0;
            reverseTimer = 0;
            Recoveries++;
        }
    }

    /// <summary>
    /// Verifica se uma faixa candidata está livre de outros carros.
    /// </summary>
    bool IsLaneFree(float candidateLane) {
        if (vehicles == null) return true;
        foreach (var v in vehicles) {
            if (!v || v == car) continue;
            Vector3 p = transform.InverseTransformPoint(v.transform.position);
            if (Mathf.Abs(p.z) < 14f && Mathf.Abs(p.x - (candidateLane - currentLane)) < TunnelHalfWidth) {
                return false;
            }
        }
        return true;
    }

    void OnDisable() {
        if (!car) return;
        car.automation   = false;
        car.testThrottle = 0;
        car.testSteer    = 0;
        car.aiBoost      = false;
        car.aiHandbrake  = false;
    }
}
}
