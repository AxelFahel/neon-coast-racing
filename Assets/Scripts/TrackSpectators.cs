using System.Collections.Generic;
using UnityEngine;

namespace NeonCoast {
/// <summary>
/// Sistema de Público e Pedestres do Circuito (Assistindo e Andando fora da pista).
/// Gera multidões vivas e pedestres caminhando pelas calçadas, calçadão da praia e atrás dos guard-rails.
/// Reagem dinamicamente à passagem dos supercarros com animações de comemoração, fotos e celulares brilhantes.
/// </summary>
public class TrackSpectators : MonoBehaviour {

    static readonly Vector3[] knots = {
        new Vector3(-100, 0, -120),
        new Vector3(-100, 0, -40),
        new Vector3(-95,  0,  65),
        new Vector3(-45,  2, 125),
        new Vector3( 55,  7, 125),
        new Vector3(115, 12,  65),
        new Vector3(115, 12, -10),
        new Vector3( 85,  7, -65),
        new Vector3(105,  2, -130),
        new Vector3( 45,  0, -185),
        new Vector3(-45,  0, -185)
    };

    public static Vector3 GetTrackPoint(float t) {
        float f = Mathf.Repeat(t, 1f) * knots.Length;
        int i = Mathf.FloorToInt(f);
        float u = f - i;
        Vector3 a = knots[(i + knots.Length - 1) % knots.Length];
        Vector3 b = knots[i % knots.Length];
        Vector3 c = knots[(i + 1) % knots.Length];
        Vector3 d = knots[(i + 2) % knots.Length];
        return 0.5f * ((2f * b) + (-a + c) * u + (2f * a - 5f * b + 4f * c - d) * u * u + (-a + 3f * b - 3f * c + d) * u * u * u);
    }

    public static Vector3 GetTrackForward(float t) {
        return (GetTrackPoint(t + 0.001f) - GetTrackPoint(t - 0.001f)).normalized;
    }

    public static Vector3 GetTrackRight(float t) {
        return Vector3.Cross(Vector3.up, GetTrackForward(t)).normalized;
    }

    enum SpectatorType { WatchingFan, CheeringFan, WalkingPedestrian, Photographer }

    class SpectatorInstance {
        public GameObject root;
        public SpectatorType type;
        public Transform head;
        public Transform torso;
        public Transform leftArm;
        public Transform rightArm;
        public Transform leftLeg;
        public Transform rightLeg;
        public Transform phoneLight;
        public Vector3 basePos;
        public Vector3 forwardDir;
        public float animOffset;
        public float walkSpeed;
        public float walkRange;
        public float walkProgress;
        public int walkDirection;
        public float flashTimer;
    }

    readonly List<SpectatorInstance> spectators = new List<SpectatorInstance>();
    Transform playerTransform;

    void Awake() {
        // Se já foi gerado no Editor, apenas mapeia e anima
        if (transform.childCount > 0) {
            MapExistingChildren();
            return;
        }
        SpawnAllSpectators();
    }

    void Start() {
        var player = FindFirstObjectByType<ArcadeCar>();
        if (player) playerTransform = player.transform;
    }

    public void SpawnAllSpectators() {
        // Limpa instâncias prévias se existirem
        for (int i = transform.childCount - 1; i >= 0; i--) {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
        spectators.Clear();

        // Cria materiais compartilhados leves
        var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? litShader;

        Material matSkin = CreateMat(litShader, "SpecSkin", new Color(0.88f, 0.68f, 0.54f), 0.1f, 0.3f);
        Material matPants = CreateMat(litShader, "SpecPants", new Color(0.12f, 0.14f, 0.18f), 0.0f, 0.2f);
        Material matCyan = CreateMat(litShader, "SpecCyan", new Color(0.05f, 0.85f, 1.0f), 0.0f, 0.7f, new Color(0.05f, 0.85f, 1f) * 1.5f);
        Material matPink = CreateMat(litShader, "SpecPink", new Color(1.0f, 0.12f, 0.45f), 0.0f, 0.7f, new Color(1f, 0.12f, 0.45f) * 1.5f);
        Material matYellow = CreateMat(litShader, "SpecYellow", new Color(1.0f, 0.82f, 0.10f), 0.0f, 0.6f, new Color(1f, 0.82f, 0.1f) * 1.2f);
        Material matWhite = CreateMat(litShader, "SpecWhite", new Color(0.92f, 0.94f, 0.96f), 0.0f, 0.5f);
        Material matDark = CreateMat(litShader, "SpecDark", new Color(0.18f, 0.20f, 0.24f), 0.0f, 0.4f);
        Material matPhone = CreateMat(unlitShader, "SpecPhoneScreen", Color.white, 0f, 0f, new Color(1.8f, 2.2f, 2.5f));

        Material[] jacketMats = new Material[] { matCyan, matPink, matYellow, matWhite, matDark };

        // ── 1. RETA PRINCIPAL E LARGADA (t = 0.97 até 0.06) ───────────────────
        // Grandes grupos de torcedores vibrando, tirando fotos e assistindo
        for (int i = 0; i < 36; i++) {
            float t = Mathf.Repeat(-0.025f + (i / 36f) * 0.08f, 1f);
            int side = (i % 2 == 0) ? -1 : 1;
            float distFromCenter = 10.4f + (i % 3) * 1.2f;
            Vector3 pos = GetTrackPoint(t) + GetTrackRight(t) * (distFromCenter * side) + Vector3.up * 0.55f;
            Vector3 toTrack = (GetTrackPoint(t) - pos).normalized; toTrack.y = 0;

            SpectatorType st = (i % 4 == 0) ? SpectatorType.Photographer :
                               (i % 3 == 0) ? SpectatorType.CheeringFan : SpectatorType.WatchingFan;
            Material jacket = jacketMats[i % jacketMats.Length];
            CreateSpectator($"Fan_Start_{i}", pos, Quaternion.LookRotation(toTrack), st, jacket, matPants, matSkin, matPhone, i * 0.23f);
        }

        // ── 2. PEDESTRES ANDANDO PELO CALÇADÃO DA PRAIA (t = 0.18 até 0.38) ───
        // Pessoas passeando suavemente pelas calçadas fora do circuito
        for (int i = 0; i < 28; i++) {
            float t = 0.18f + (i / 28f) * 0.20f;
            float distFromCenter = 11.8f + (i % 2) * 1.5f;
            Vector3 pos = GetTrackPoint(t) - GetTrackRight(t) * distFromCenter + Vector3.up * 0.55f;
            Vector3 walkDir = GetTrackForward(t) * ((i % 2 == 0) ? 1 : -1);

            SpectatorType st = (i % 3 == 0) ? SpectatorType.WalkingPedestrian : SpectatorType.WatchingFan;
            Material jacket = jacketMats[(i + 2) % jacketMats.Length];
            var spec = CreateSpectator($"Promenade_Person_{i}", pos, Quaternion.LookRotation(walkDir), st, jacket, matPants, matSkin, matPhone, i * 0.37f);
            if (spec != null) {
                spec.walkSpeed = 1.1f + (i % 3) * 0.25f;
                spec.walkRange = 12f + (i % 4) * 4f;
                spec.walkDirection = (i % 2 == 0) ? 1 : -1;
            }
        }

        // ── 3. CURVA DO PORTO / CHICANE URBANA (t = 0.45 até 0.62) ────────────
        // Torcedores em pontos estratégicos de frenagem e curvas
        for (int i = 0; i < 26; i++) {
            float t = 0.45f + (i / 26f) * 0.17f;
            int side = (i % 2 == 0) ? 1 : -1;
            float distFromCenter = 10.6f + (i % 3) * 1.0f;
            Vector3 pos = GetTrackPoint(t) + GetTrackRight(t) * (distFromCenter * side) + Vector3.up * 0.55f;
            Vector3 toTrack = (GetTrackPoint(t) - pos).normalized; toTrack.y = 0;

            SpectatorType st = (i % 2 == 0) ? SpectatorType.CheeringFan : SpectatorType.Photographer;
            Material jacket = jacketMats[(i + 1) % jacketMats.Length];
            CreateSpectator($"Corner_Fan_{i}", pos, Quaternion.LookRotation(toTrack), st, jacket, matPants, matSkin, matPhone, i * 0.41f);
        }

        // ── 4. CALÇADAS DO VIADUTO E RETORNO (t = 0.72 até 0.94) ──────────────
        // Pedestres caminhando e pessoas apoiadas no gradil olhando o viaduto
        for (int i = 0; i < 28; i++) {
            float t = 0.72f + (i / 28f) * 0.22f;
            int side = (i % 2 == 0) ? -1 : 1;
            float distFromCenter = 11.2f + (i % 2) * 1.4f;
            Vector3 pos = GetTrackPoint(t) + GetTrackRight(t) * (distFromCenter * side) + Vector3.up * 0.55f;

            SpectatorType st = (i % 3 == 0) ? SpectatorType.WalkingPedestrian : SpectatorType.WatchingFan;
            Vector3 fwd = (st == SpectatorType.WalkingPedestrian) ? GetTrackForward(t) : (GetTrackPoint(t) - pos).normalized;
            fwd.y = 0;

            Material jacket = jacketMats[(i + 3) % jacketMats.Length];
            var spec = CreateSpectator($"Viaduct_Person_{i}", pos, Quaternion.LookRotation(fwd), st, jacket, matPants, matSkin, matPhone, i * 0.29f);
            if (spec != null) {
                spec.walkSpeed = 1.0f + (i % 2) * 0.3f;
                spec.walkRange = 10f + (i % 3) * 3f;
                spec.walkDirection = (i % 2 == 0) ? 1 : -1;
            }
        }
    }

    SpectatorInstance CreateSpectator(string name, Vector3 pos, Quaternion rot, SpectatorType type, Material jacket, Material pants, Material skin, Material phone, float offset) {
        var root = new GameObject(name);
        root.transform.SetParent(transform, false);
        root.transform.position = pos;
        root.transform.rotation = rot;

        // Torso / Jaqueta Streetwear
        var torso = CreateBox("Torso", root.transform, new Vector3(0, 1.12f, 0), new Vector3(0.36f, 0.48f, 0.22f), jacket);

        // Cabeça com boné/viseira cyberpunk
        var head = CreateBox("Head", torso.transform, new Vector3(0, 0.36f, 0), new Vector3(0.20f, 0.22f, 0.20f), skin);
        CreateBox("Visor", head.transform, new Vector3(0, 0.04f, 0.10f), new Vector3(0.21f, 0.06f, 0.06f), jacket);

        // Pernas (com calça e tênis)
        var leftLeg = CreateBox("Leg_L", root.transform, new Vector3(-0.10f, 0.46f, 0), new Vector3(0.12f, 0.64f, 0.13f), pants);
        var rightLeg = CreateBox("Leg_R", root.transform, new Vector3(0.10f, 0.46f, 0), new Vector3(0.12f, 0.64f, 0.13f), pants);

        // Braços
        var leftArm = CreateBox("Arm_L", torso.transform, new Vector3(-0.24f, 0.10f, 0), new Vector3(0.10f, 0.44f, 0.11f), jacket);
        var rightArm = CreateBox("Arm_R", torso.transform, new Vector3(0.24f, 0.10f, 0), new Vector3(0.10f, 0.44f, 0.11f), jacket);

        Transform phoneLightT = null;
        if (type == SpectatorType.Photographer) {
            // Celular na mão direita
            var phoneObj = CreateBox("Phone", rightArm.transform, new Vector3(0, -0.22f, 0.12f), new Vector3(0.08f, 0.14f, 0.02f), phone);
            phoneLightT = phoneObj.transform;
            rightArm.transform.localRotation = Quaternion.Euler(-65f, 15f, 0);
            leftArm.transform.localRotation = Quaternion.Euler(-45f, -15f, 0);
        }

        var instance = new SpectatorInstance {
            root = root,
            type = type,
            head = head.transform,
            torso = torso.transform,
            leftArm = leftArm.transform,
            rightArm = rightArm.transform,
            leftLeg = leftLeg.transform,
            rightLeg = rightLeg.transform,
            phoneLight = phoneLightT,
            basePos = pos,
            forwardDir = rot * Vector3.forward,
            animOffset = offset,
            walkSpeed = 1.2f,
            walkRange = 12f,
            walkDirection = 1
        };

        spectators.Add(instance);
        return instance;
    }

    GameObject CreateBox(string n, Transform parent, Vector3 localPos, Vector3 scale, Material mat) {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = n;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    Material CreateMat(Shader shader, string name, Color color, float metallic, float smooth, Color emission = default) {
        var m = new Material(shader) { name = name };
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (emission.maxColorComponent > 0) {
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", emission);
        }
        m.enableInstancing = true;
        return m;
    }

    void MapExistingChildren() {
        spectators.Clear();
        foreach (Transform child in transform) {
            var torso = child.Find("Torso");
            if (!torso) continue;
            var head = torso.Find("Head");
            var armL = torso.Find("Arm_L");
            var armR = torso.Find("Arm_R");
            var legL = child.Find("Leg_L");
            var legR = child.Find("Leg_R");

            SpectatorType st = child.name.Contains("Promenade") || child.name.Contains("Walk")
                ? SpectatorType.WalkingPedestrian
                : child.name.Contains("Photo") ? SpectatorType.Photographer : SpectatorType.CheeringFan;

            spectators.Add(new SpectatorInstance {
                root = child.gameObject,
                type = st,
                head = head,
                torso = torso,
                leftArm = armL,
                rightArm = armR,
                leftLeg = legL,
                rightLeg = legR,
                basePos = child.position,
                forwardDir = child.forward,
                animOffset = Random.value * 6f,
                walkSpeed = 1.2f,
                walkRange = 12f,
                walkDirection = 1
            });
        }
    }

    void Update() {
        if (spectators.Count == 0) return;
        float time = Time.time;
        Vector3 playerPos = playerTransform ? playerTransform.position : Vector3.zero;

        for (int i = 0; i < spectators.Count; i++) {
            var s = spectators[i];
            if (s.root == null) continue;

            float localTime = time * 2.5f + s.animOffset;
            float distToPlayer = Vector3.Distance(s.root.transform.position, playerPos);
            bool playerNearby = distToPlayer < 24f;

            switch (s.type) {
                case SpectatorType.WalkingPedestrian:
                    // Animação de caminhada com ciclo de passadas e deslocamento linear
                    s.walkProgress += Time.deltaTime * (s.walkSpeed / s.walkRange) * s.walkDirection;
                    if (s.walkProgress >= 1f) { s.walkProgress = 1f; s.walkDirection = -1; }
                    else if (s.walkProgress <= -1f) { s.walkProgress = -1f; s.walkDirection = 1; }

                    Vector3 moveOffset = s.forwardDir * (s.walkProgress * s.walkRange * 0.5f);
                    s.root.transform.position = s.basePos + moveOffset;
                    s.root.transform.rotation = Quaternion.LookRotation(s.forwardDir * s.walkDirection);

                    // Ciclo de passada: pernas alternam
                    float walkAngle = Mathf.Sin(time * s.walkSpeed * 5f + s.animOffset) * 28f;
                    if (s.leftLeg) s.leftLeg.localRotation = Quaternion.Euler(walkAngle, 0, 0);
                    if (s.rightLeg) s.rightLeg.localRotation = Quaternion.Euler(-walkAngle, 0, 0);
                    if (s.leftArm) s.leftArm.localRotation = Quaternion.Euler(-walkAngle * 0.7f, 0, 0);
                    if (s.rightArm) s.rightArm.localRotation = Quaternion.Euler(walkAngle * 0.7f, 0, 0);
                    if (s.torso) s.torso.localPosition = new Vector3(0, 1.12f + Mathf.Abs(Mathf.Sin(time * s.walkSpeed * 10f)) * 0.03f, 0);
                    break;

                case SpectatorType.CheeringFan:
                    // Vibração de torcida: braços no alto, pulinhos quando carro passa
                    float cheerSpeed = playerNearby ? 6f : 2.5f;
                    float armSwing = Mathf.Sin(time * cheerSpeed + s.animOffset) * (playerNearby ? 55f : 30f);
                    float bodyBounce = Mathf.Abs(Mathf.Sin(time * cheerSpeed + s.animOffset)) * (playerNearby ? 0.08f : 0.02f);

                    if (s.leftArm) s.leftArm.localRotation = Quaternion.Euler(-140f + armSwing, 0, -15f);
                    if (s.rightArm) s.rightArm.localRotation = Quaternion.Euler(-140f - armSwing, 0, 15f);
                    if (s.torso) s.torso.localPosition = new Vector3(0, 1.12f + bodyBounce, 0);
                    if (s.head && playerNearby) {
                        Vector3 look = (playerPos - s.root.transform.position).normalized;
                        s.head.rotation = Quaternion.Slerp(s.head.rotation, Quaternion.LookRotation(look), Time.deltaTime * 6f);
                    }
                    break;

                case SpectatorType.Photographer:
                    // Segura o celular filmando a corrida, tirando fotos
                    float photoBob = Mathf.Sin(localTime * 0.8f) * 4f;
                    if (s.rightArm) s.rightArm.localRotation = Quaternion.Euler(-70f + photoBob, 15f, 0);
                    if (s.leftArm) s.leftArm.localRotation = Quaternion.Euler(-55f - photoBob, -15f, 0);

                    // Flash sutil de foto quando carro passa perto
                    s.flashTimer -= Time.deltaTime;
                    if (playerNearby && s.flashTimer <= 0f) {
                        s.flashTimer = Random.Range(1.5f, 3.5f);
                        if (s.phoneLight) {
                            var rend = s.phoneLight.GetComponent<Renderer>();
                            if (rend && rend.material) rend.material.SetColor("_Color", Color.white * 4f);
                        }
                    }
                    break;

                case SpectatorType.WatchingFan:
                default:
                    // Apoia no guard-rail, olhando para a passagem dos carros
                    float idleHead = Mathf.Sin(time * 1.2f + s.animOffset) * 12f;
                    if (s.head) {
                        if (playerNearby) {
                            Vector3 look = (playerPos - s.root.transform.position).normalized;
                            s.head.rotation = Quaternion.Slerp(s.head.rotation, Quaternion.LookRotation(look), Time.deltaTime * 5f);
                        } else {
                            s.head.localRotation = Quaternion.Euler(0, idleHead, 0);
                        }
                    }
                    if (s.leftArm) s.leftArm.localRotation = Quaternion.Euler(-15f + Mathf.Sin(time * 1.5f) * 5f, 0, 0);
                    if (s.rightArm) s.rightArm.localRotation = Quaternion.Euler(-15f - Mathf.Sin(time * 1.5f) * 5f, 0, 0);
                    break;
            }
        }
    }
}
}
