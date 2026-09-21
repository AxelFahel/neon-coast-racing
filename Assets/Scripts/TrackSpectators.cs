using System.Collections.Generic;
using UnityEngine;

namespace NeonCoast {
/// <summary>
/// Sistema de Público e Pedestres do Circuito.
/// Personagens com proporções humanas realistas: cabeça arredondada, torso slim,
/// pernas e braços cilíndricos, sapatos, mochila, cabelo volumoso.
/// Animações vivas: torcida, caminhada, fotografia com flash.
/// Reagem dinamicamente aos carros passando.
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
        public Vector3 torsoBaseLocalPos;
        public Vector3 forwardDir;
        public float animOffset;
        public float walkSpeed;
        public float walkRange;
        public float walkProgress;
        public int walkDirection;
        public float flashTimer;
        // Variação de altura: fator de escala individual
        public float heightScale;
    }

    readonly List<SpectatorInstance> spectators = new List<SpectatorInstance>();
    Transform playerTransform;

    // Pool de materiais compartilhados
    Material matSkinLight, matSkinWarm, matSkinBrown, matSkinDark, matPants, matPantsDark;
    Material matCyan, matPink, matYellow, matWhite, matDark, matGreen, matOrange;
    Material matPhone, matShoe, matHair, matHairBlonde, matHairRed;
    Material matEyeWhite, matEyeDark, matLip;

    void Awake() {
        SpawnAllSpectators();
    }

    void Start() {
        var race = FindFirstObjectByType<RaceSession>();
        if (race && race.player) playerTransform = race.player.transform;
    }

    public void SpawnAllSpectators() {
        // Limpa instâncias prévias se existirem
        for (int i = transform.childCount - 1; i >= 0; i--) {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
        spectators.Clear();

        CreateMaterials();

        Material[] jacketMats = new Material[] { matCyan, matPink, matYellow, matWhite, matDark, matGreen, matOrange };
        Material[] pantsMats  = new Material[] { matPants, matPantsDark, matDark };
        Material[] skinMats   = new Material[] { matSkinLight, matSkinWarm, matSkinBrown, matSkinDark };
        Material[] hairMats   = new Material[] { matHair, matHairBlonde, matHairRed, matDark };

        // ── 1. RETA PRINCIPAL E LARGADA (t = 0.97 até 0.06) ───────────────────
        for (int i = 0; i < 36; i++) {
            float t = Mathf.Repeat(-0.025f + (i / 36f) * 0.08f, 1f);
            int side = (i % 2 == 0) ? -1 : 1;
            float distFromCenter = 10.4f + (i % 3) * 1.2f;
            Vector3 pos = GetTrackPoint(t) + GetTrackRight(t) * (distFromCenter * side) + Vector3.up * 0.55f;
            Vector3 toTrack = (GetTrackPoint(t) - pos).normalized; toTrack.y = 0;

            SpectatorType st = (i % 4 == 0) ? SpectatorType.Photographer :
                               (i % 3 == 0) ? SpectatorType.CheeringFan : SpectatorType.WatchingFan;
            Material jacket = jacketMats[i % jacketMats.Length];
            Material pants  = pantsMats[i % pantsMats.Length];
            Material skin   = skinMats[i % skinMats.Length];
            Material hair   = hairMats[i % hairMats.Length];
            CreateSpectator($"Fan_Start_{i}", pos, Quaternion.LookRotation(toTrack), st,
                            jacket, pants, skin, matPhone, hair, matShoe, i * 0.23f);
        }

        // ── 2. PEDESTRES CALÇADÃO DA PRAIA (t = 0.18 até 0.38) ───────────────
        for (int i = 0; i < 28; i++) {
            float t = 0.18f + (i / 28f) * 0.20f;
            float distFromCenter = 11.8f + (i % 2) * 1.5f;
            Vector3 pos = GetTrackPoint(t) - GetTrackRight(t) * distFromCenter + Vector3.up * 0.55f;
            Vector3 walkDir = GetTrackForward(t) * ((i % 2 == 0) ? 1 : -1);

            SpectatorType st = (i % 3 == 0) ? SpectatorType.WalkingPedestrian : SpectatorType.WatchingFan;
            Material jacket = jacketMats[(i + 2) % jacketMats.Length];
            Material pants  = pantsMats[(i + 1) % pantsMats.Length];
            Material skin   = skinMats[(i + 1) % skinMats.Length];
            Material hair   = hairMats[(i + 2) % hairMats.Length];
            var spec = CreateSpectator($"Promenade_Person_{i}", pos, Quaternion.LookRotation(walkDir), st,
                                       jacket, pants, skin, matPhone, hair, matShoe, i * 0.37f);
            if (spec != null) {
                spec.walkSpeed = 1.1f + (i % 3) * 0.25f;
                spec.walkRange = 3f;
                spec.walkDirection = (i % 2 == 0) ? 1 : -1;
            }
        }

        // ── 3. CURVA DO PORTO / CHICANE URBANA (t = 0.45 até 0.62) ───────────
        for (int i = 0; i < 26; i++) {
            float t = 0.45f + (i / 26f) * 0.17f;
            int side = (i % 2 == 0) ? 1 : -1;
            float distFromCenter = 10.6f + (i % 3) * 1.0f;
            Vector3 pos = GetTrackPoint(t) + GetTrackRight(t) * (distFromCenter * side) + Vector3.up * 0.55f;
            Vector3 toTrack = (GetTrackPoint(t) - pos).normalized; toTrack.y = 0;

            SpectatorType st = (i % 2 == 0) ? SpectatorType.CheeringFan : SpectatorType.Photographer;
            Material jacket = jacketMats[(i + 1) % jacketMats.Length];
            Material pants  = pantsMats[i % pantsMats.Length];
            Material skin   = skinMats[i % skinMats.Length];
            Material hair   = hairMats[(i + 1) % hairMats.Length];
            CreateSpectator($"Corner_Fan_{i}", pos, Quaternion.LookRotation(toTrack), st,
                            jacket, pants, skin, matPhone, hair, matShoe, i * 0.41f);
        }

        // ── 4. CALÇADAS DO VIADUTO (t = 0.72 até 0.94) ───────────────────────
        for (int i = 0; i < 28; i++) {
            float t = 0.72f + (i / 28f) * 0.22f;
            int side = (i % 2 == 0) ? -1 : 1;
            float distFromCenter = 11.2f + (i % 2) * 1.4f;
            Vector3 pos = GetTrackPoint(t) + GetTrackRight(t) * (distFromCenter * side) + Vector3.up * 0.55f;

            SpectatorType st = (i % 3 == 0) ? SpectatorType.WalkingPedestrian : SpectatorType.WatchingFan;
            Vector3 fwd = (st == SpectatorType.WalkingPedestrian) ? GetTrackForward(t) : (GetTrackPoint(t) - pos).normalized;
            fwd.y = 0;

            Material jacket = jacketMats[(i + 3) % jacketMats.Length];
            Material pants  = pantsMats[(i + 2) % pantsMats.Length];
            Material skin   = skinMats[i % skinMats.Length];
            Material hair   = hairMats[(i + 3) % hairMats.Length];
            var spec = CreateSpectator($"Viaduct_Person_{i}", pos, Quaternion.LookRotation(fwd), st,
                                       jacket, pants, skin, matPhone, hair, matShoe, i * 0.29f);
            if (spec != null) {
                spec.walkSpeed = 1.0f + (i % 2) * 0.3f;
                spec.walkRange = 3f;
                spec.walkDirection = (i % 2 == 0) ? 1 : -1;
            }
        }
    }

    void CreateMaterials() {
        var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? litShader;

        // Tons de pele variados (não apenas um padrão)
        matSkinLight = CreateMat(litShader, "SpecSkin_Light", new Color(0.93f, 0.76f, 0.62f), 0.0f, 0.34f);
        matSkinWarm  = CreateMat(litShader, "SpecSkin_Warm",  new Color(0.76f, 0.50f, 0.34f), 0.0f, 0.31f);
        matSkinBrown = CreateMat(litShader, "SpecSkin_Brown", new Color(0.52f, 0.32f, 0.21f), 0.0f, 0.29f);
        matSkinDark  = CreateMat(litShader, "SpecSkin_Dark",  new Color(0.27f, 0.17f, 0.12f), 0.0f, 0.27f);

        // Calças e shorts variados
        matPants     = CreateMat(litShader, "SpecPants_Dark",  new Color(0.10f, 0.12f, 0.16f), 0.0f, 0.18f);
        matPantsDark = CreateMat(litShader, "SpecPants_Navy",  new Color(0.06f, 0.10f, 0.22f), 0.0f, 0.20f);

        // Roupas neon variadas
        matCyan   = CreateMat(litShader, "SpecCyan",   new Color(0.05f, 0.62f, 0.72f), 0.0f, 0.48f, new Color(0.01f, 0.08f, 0.10f));
        matPink   = CreateMat(litShader, "SpecPink",   new Color(0.72f, 0.10f, 0.30f), 0.0f, 0.46f, new Color(0.09f, 0.01f, 0.03f));
        matYellow = CreateMat(litShader, "SpecYellow", new Color(0.78f, 0.62f, 0.12f), 0.0f, 0.42f, new Color(0.08f, 0.05f, 0.01f));
        matWhite  = CreateMat(litShader, "SpecWhite",  new Color(0.92f, 0.94f, 0.96f), 0.0f, 0.45f);
        matDark   = CreateMat(litShader, "SpecDark",   new Color(0.16f, 0.18f, 0.22f), 0.0f, 0.35f);
        matGreen  = CreateMat(litShader, "SpecGreen",  new Color(0.05f, 0.62f, 0.30f), 0.0f, 0.44f, new Color(0.01f, 0.07f, 0.025f));
        matOrange = CreateMat(litShader, "SpecOrange", new Color(0.82f, 0.36f, 0.04f), 0.0f, 0.43f, new Color(0.08f, 0.025f, 0.01f));

        // Cabelos variados
        matHair       = CreateMat(litShader, "SpecHair_Black",  new Color(0.08f, 0.07f, 0.07f), 0.0f, 0.30f);
        matHairBlonde = CreateMat(litShader, "SpecHair_Blonde", new Color(0.88f, 0.72f, 0.32f), 0.0f, 0.32f);
        matHairRed    = CreateMat(litShader, "SpecHair_Red",    new Color(0.62f, 0.14f, 0.10f), 0.0f, 0.28f);

        // Materiais faciais sem emissão: o rosto continua legível sob o neon,
        // mas não parece uma máscara ou um manequim luminoso.
        matEyeWhite = CreateMat(litShader, "SpecEye_White", new Color(0.90f, 0.92f, 0.90f), 0f, 0.38f);
        matEyeDark  = CreateMat(litShader, "SpecEye_Dark",  new Color(0.025f, 0.035f, 0.045f), 0f, 0.25f);
        matLip      = CreateMat(litShader, "SpecLip",       new Color(0.42f, 0.10f, 0.10f), 0f, 0.24f);

        // Acessórios
        matPhone = CreateMat(unlitShader, "SpecPhoneScreen", Color.white, 0f, 0f, new Color(1.8f, 2.2f, 2.5f));
        matShoe  = CreateMat(litShader, "SpecShoe",          new Color(0.12f, 0.12f, 0.14f), 0.0f, 0.55f);
    }

    SpectatorInstance CreateSpectator(string name, Vector3 pos, Quaternion rot, SpectatorType type,
                                      Material jacket, Material pants, Material skin,
                                      Material phone, Material hair, Material shoe, float offset) {
        // Somente personagens ao nível do chão
        if (pos.y > 1.2f) return null;
        pos.y = 0.02f;

        var root = new GameObject(name);
        root.transform.SetParent(transform, false);
        root.transform.position = pos;
        root.transform.rotation = rot;

        // Variações independentes de altura e biotipo evitam uma multidão de clones.
        float heightFactor = 0.90f + Mathf.Repeat(offset * 7.3f, 0.18f);
        float bodyBuild = 0.90f + Mathf.Repeat(offset * 11.7f, 0.20f);
        root.transform.localScale = new Vector3(bodyBuild, heightFactor, bodyBuild);

        // ──────────────────────────────────────────────────────────────────────
        // ANATOMIA HUMANA PROPORCIONAL
        // Referência: altura total ~1.75m (unidades Unity)
        // ──────────────────────────────────────────────────────────────────────

        // PERNAS — proporções adultas, com joelho na metade da perna
        // Coxa esquerda
        var leftThigh  = CreateLimb("Thigh_L",  root.transform, new Vector3(-0.105f, 0.86f, 0f), new Vector3(0.13f, 0.43f, 0.14f), pants);
        var rightThigh = CreateLimb("Thigh_R",  root.transform, new Vector3( 0.105f, 0.86f, 0f), new Vector3(0.13f, 0.43f, 0.14f), pants);
        // Canela
        var leftShin   = CreateLimb("Shin_L",   leftThigh.transform,  new Vector3(0f, -0.43f, 0.01f), new Vector3(0.105f, 0.39f, 0.11f), pants);
        var rightShin  = CreateLimb("Shin_R",   rightThigh.transform, new Vector3(0f, -0.43f, 0.01f), new Vector3(0.105f, 0.39f, 0.11f), pants);
        // Sapato
        CreateBox("Shoe_L", leftShin.transform,  new Vector3(0f, -0.20f, 0.05f), new Vector3(0.12f, 0.09f, 0.22f), shoe);
        CreateBox("Shoe_R", rightShin.transform, new Vector3(0f, -0.20f, 0.05f), new Vector3(0.12f, 0.09f, 0.22f), shoe);

        // QUADRIL — une pernas ao torso
        CreateBox("Hips", root.transform, new Vector3(0f, 0.94f, 0f), new Vector3(0.34f, 0.17f, 0.22f), pants);

        // TORSO — ombros mais largos e cintura visível
        var torso = CreateLimb("Torso", root.transform, new Vector3(0f, 1.48f, 0f), new Vector3(0.40f, 0.52f, 0.26f), jacket);
        CreateBox("Waist", root.transform, new Vector3(0f, 1.02f, 0f), new Vector3(0.29f, 0.16f, 0.20f), jacket);

        // PESCOÇO
        var neck = CreateLimb("Neck", torso.transform, new Vector3(0f, 0.08f, 0f), new Vector3(0.10f, 0.13f, 0.10f), skin);

        // CABEÇA — levemente oval, com face legível na direção +Z
        var head = CreateEllipsoid("Head", neck.transform, new Vector3(0f, 0.17f, 0f), new Vector3(0.31f, 0.38f, 0.30f), skin);

        // Olhos, pupilas, nariz, boca e orelhas quebram a silhueta de manequim.
        for (int eye = -1; eye <= 1; eye += 2) {
            CreateEllipsoid("Eye_" + eye, head.transform, new Vector3(eye * 0.058f, 0.025f, 0.148f), new Vector3(0.054f, 0.030f, 0.018f), matEyeWhite);
            CreateSphere("Pupil_" + eye, head.transform, new Vector3(eye * 0.058f, 0.025f, 0.160f), 0.012f, matEyeDark);
            var brow = CreateBox("Brow_" + eye, head.transform, new Vector3(eye * 0.058f, 0.064f, 0.153f), new Vector3(0.065f, 0.012f, 0.010f), hair);
            brow.transform.localRotation = Quaternion.Euler(0f, 0f, eye * -5f);
        }
        CreateEllipsoid("Nose", head.transform, new Vector3(0f, -0.015f, 0.158f), new Vector3(0.040f, 0.070f, 0.045f), skin);
        CreateBox("Mouth", head.transform, new Vector3(0f, -0.082f, 0.151f), new Vector3(0.075f, 0.014f, 0.012f), matLip);
        CreateSphere("Ear_L", head.transform, new Vector3(-0.158f, 0f, 0f), 0.042f, skin);
        CreateSphere("Ear_R", head.transform, new Vector3( 0.158f, 0f, 0f), 0.042f, skin);

        // CABELO — somente sobre o crânio; antes uma esfera inteira escondia o rosto.
        int hairStyle = Mathf.Abs((int)(offset * 97f)) % 3;
        CreateEllipsoid("Hair_Top", head.transform, new Vector3(0f, 0.125f, -0.025f), new Vector3(0.325f, 0.17f, 0.30f), hair);
        if (hairStyle == 1) {
            CreateBox("Hair_Back", head.transform, new Vector3(0f, -0.035f, -0.135f), new Vector3(0.26f, 0.28f, 0.08f), hair);
        } else if (hairStyle == 2) {
            CreateSphere("Hair_Curl_L", head.transform, new Vector3(-0.115f, 0.08f, -0.02f), 0.075f, hair);
            CreateSphere("Hair_Curl_R", head.transform, new Vector3( 0.115f, 0.08f, -0.02f), 0.075f, hair);
        }
        // Boné em parte da multidão, sem esconder a face.
        bool hasCap = ((int)(offset * 100f) % 4 == 0);
        if (hasCap) {
            CreateBox("Cap_Bill", head.transform, new Vector3(0f, 0.09f, 0.16f), new Vector3(0.22f, 0.035f, 0.13f), jacket);
            CreateEllipsoid("Cap_Crown", head.transform, new Vector3(0f, 0.135f, -0.01f), new Vector3(0.34f, 0.15f, 0.31f), jacket);
        }

        // BRAÇOS — cilíndricos com antebraço
        var leftUpperArm  = CreateLimb("UpperArm_L",  torso.transform, new Vector3(-0.27f, -0.05f, 0f), new Vector3(0.11f, 0.31f, 0.11f), jacket);
        var rightUpperArm = CreateLimb("UpperArm_R",  torso.transform, new Vector3( 0.27f, -0.05f, 0f), new Vector3(0.11f, 0.31f, 0.11f), jacket);
        var leftForearm   = CreateLimb("Forearm_L",   leftUpperArm.transform,  new Vector3(0f, -0.31f, 0.01f), new Vector3(0.09f, 0.27f, 0.09f), skin);
        var rightForearm  = CreateLimb("Forearm_R",   rightUpperArm.transform, new Vector3(0f, -0.31f, 0.01f), new Vector3(0.09f, 0.27f, 0.09f), skin);
        // Mãos
        CreateBox("Hand_L", leftForearm.transform,  new Vector3(0f, -0.16f, 0f), new Vector3(0.08f, 0.09f, 0.07f), skin);
        CreateBox("Hand_R", rightForearm.transform, new Vector3(0f, -0.16f, 0f), new Vector3(0.08f, 0.09f, 0.07f), skin);

        // Mochila em 30% dos personagens
        bool hasBackpack = ((int)(offset * 131f) % 3 == 0);
        if (hasBackpack) {
            CreateBox("Backpack", torso.transform, new Vector3(0f, 0.02f, -0.16f), new Vector3(0.22f, 0.32f, 0.12f), matDark ?? pants);
            CreateBox("Backpack_Pocket", torso.transform, new Vector3(0f, -0.08f, -0.22f), new Vector3(0.14f, 0.14f, 0.06f), jacket);
        }

        // Celular na mão direita para fotógrafos
        Transform phoneLightT = null;
        if (type == SpectatorType.Photographer) {
            var phoneObj = CreateBox("Phone", rightForearm.transform, new Vector3(0f, -0.10f, 0.06f), new Vector3(0.07f, 0.12f, 0.015f), phone);
            phoneLightT = phoneObj.transform;
            rightUpperArm.transform.localRotation = Quaternion.Euler(-60f, 12f, 0f);
            leftUpperArm.transform.localRotation  = Quaternion.Euler(-40f, -12f, 0f);
            rightForearm.transform.localRotation  = Quaternion.Euler(-20f, 0f, 0f);
        }

        // Três níveis de detalhe: o rig facial completo só é desenhado perto
        // da câmera; à distância entram proxies humanos leves. Os ossos acima
        // continuam dirigindo as animações do LOD principal.
        SetupHumanLOD(root, jacket, pants, skin, hair);

        var instance = new SpectatorInstance {
            root = root,
            type = type,
            head = head.transform,
            torso = torso.transform,
            leftArm = leftUpperArm.transform,
            rightArm = rightUpperArm.transform,
            leftLeg = leftThigh.transform,
            rightLeg = rightThigh.transform,
            phoneLight = phoneLightT,
            basePos = pos,
            torsoBaseLocalPos = torso.transform.localPosition,
            forwardDir = rot * Vector3.forward,
            animOffset = offset,
            walkSpeed = 1.2f,
            walkRange = 3f,
            walkDirection = 1,
            heightScale = heightFactor
        };

        spectators.Add(instance);
        return instance;
    }

    void SetupHumanLOD(GameObject root, Material jacket, Material pants, Material skin, Material hair) {
        var high = root.GetComponentsInChildren<Renderer>(true);

        var midRoot = new GameObject("LOD1 Human");
        midRoot.transform.SetParent(root.transform, false);
        var mid = new List<Renderer> {
            Proxy(PrimitiveType.Capsule, "LOD1 Torso", midRoot.transform, new Vector3(0, 1.25f, 0), new Vector3(.38f, .38f, .25f), jacket),
            Proxy(PrimitiveType.Sphere, "LOD1 Head", midRoot.transform, new Vector3(0, 1.72f, 0), new Vector3(.31f, .37f, .30f), skin),
            Proxy(PrimitiveType.Sphere, "LOD1 Hair", midRoot.transform, new Vector3(0, 1.82f, -.02f), new Vector3(.32f, .15f, .29f), hair),
            Proxy(PrimitiveType.Capsule, "LOD1 Leg L", midRoot.transform, new Vector3(-.11f, .48f, 0), new Vector3(.13f, .42f, .13f), pants),
            Proxy(PrimitiveType.Capsule, "LOD1 Leg R", midRoot.transform, new Vector3( .11f, .48f, 0), new Vector3(.13f, .42f, .13f), pants),
            Proxy(PrimitiveType.Capsule, "LOD1 Arm L", midRoot.transform, new Vector3(-.28f, 1.16f, 0), new Vector3(.10f, .30f, .10f), jacket),
            Proxy(PrimitiveType.Capsule, "LOD1 Arm R", midRoot.transform, new Vector3( .28f, 1.16f, 0), new Vector3(.10f, .30f, .10f), jacket)
        };

        var lowRoot = new GameObject("LOD2 Silhouette");
        lowRoot.transform.SetParent(root.transform, false);
        var low = new[] {
            Proxy(PrimitiveType.Capsule, "LOD2 Body", lowRoot.transform, new Vector3(0, .92f, 0), new Vector3(.38f, .78f, .25f), jacket),
            Proxy(PrimitiveType.Sphere, "LOD2 Head", lowRoot.transform, new Vector3(0, 1.72f, 0), new Vector3(.30f, .35f, .29f), skin)
        };

        var lodGroup = root.AddComponent<LODGroup>();
        lodGroup.fadeMode = LODFadeMode.CrossFade;
        lodGroup.animateCrossFading = true;
        lodGroup.SetLODs(new[] {
            // Na câmera de largada uma pessoa ocupa ~8-10% da altura da tela.
            // O limite antigo (.18) já trocava para o proxy sem rosto, fazendo
            // a torcida parecer composta por manequins. Mantém anatomia e face
            // completas nas distâncias em que o jogador consegue percebê-las.
            new LOD(.065f, high),
            new LOD(.022f, mid.ToArray()),
            new LOD(.007f, low)
        });
        lodGroup.RecalculateBounds();
    }

    Renderer Proxy(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material) {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        DestroyImmediate(go.GetComponent<Collider>());
        var renderer = go.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        return renderer;
    }

    // Cria um segmento cilíndrico de membro (Capsule com pivô no topo)
    GameObject CreateLimb(string n, Transform parent, Vector3 localPos, Vector3 scale, Material mat) {
        var pivot = new GameObject(n);
        pivot.transform.SetParent(parent, false);
        pivot.transform.localPosition = localPos;

        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Geometry";
        go.transform.SetParent(pivot.transform, false);
        // Pivô no topo: desloca a cápsula para baixo pelo seu raio de altura
        go.transform.localPosition = new Vector3(0, -scale.y * 0.5f, 0);
        go.transform.localScale = new Vector3(scale.x, scale.y * 0.5f, scale.z);
        DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return pivot;
    }

    // Cria uma esfera (cabeça, cabelo)
    GameObject CreateSphere(string n, Transform parent, Vector3 localPos, float radius, Material mat) {
        var pivot = new GameObject(n);
        pivot.transform.SetParent(parent, false);
        pivot.transform.localPosition = localPos;

        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Geometry";
        go.transform.SetParent(pivot.transform, false);
        go.transform.localScale = Vector3.one * radius * 2f;
        DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return pivot;
    }

    GameObject CreateEllipsoid(string n, Transform parent, Vector3 localPos, Vector3 scale, Material mat) {
        var pivot = new GameObject(n);
        pivot.transform.SetParent(parent, false);
        pivot.transform.localPosition = localPos;

        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Geometry";
        go.transform.SetParent(pivot.transform, false);
        go.transform.localScale = scale;
        DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return pivot;
    }

    // Cria um cubo simples (acessório)
    GameObject CreateBox(string n, Transform parent, Vector3 localPos, Vector3 scale, Material mat) {
        var pivot = new GameObject(n);
        pivot.transform.SetParent(parent, false);
        pivot.transform.localPosition = localPos;

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Geometry";
        go.transform.SetParent(pivot.transform, false);
        go.transform.localScale = scale;
        DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return pivot;
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

    void Update() {
        if (spectators.Count == 0) return;
        float time = Time.time;
        Vector3 playerPos = playerTransform ? playerTransform.position : Vector3.zero;

        for (int i = 0; i < spectators.Count; i++) {
            var s = spectators[i];
            if (s.root == null) continue;

            float localTime = time * 2.5f + s.animOffset;
            float distToPlayer = Vector3.Distance(s.root.transform.position, playerPos);
            if (distToPlayer > 150f) continue;
            bool playerNearby = distToPlayer < 24f;

            switch (s.type) {
                case SpectatorType.WalkingPedestrian:
                    // Animação de caminhada fluida com ciclo de passadas
                    s.walkProgress += Time.deltaTime * (s.walkSpeed / s.walkRange) * s.walkDirection;
                    if (s.walkProgress >= 1f)  { s.walkProgress = 1f;  s.walkDirection = -1; }
                    else if (s.walkProgress <= -1f) { s.walkProgress = -1f; s.walkDirection = 1; }

                    Vector3 moveOffset = s.forwardDir * (s.walkProgress * s.walkRange * 0.5f);
                    s.root.transform.position = s.basePos + moveOffset;
                    s.root.transform.rotation = Quaternion.LookRotation(s.forwardDir * s.walkDirection);

                    float walkCycle = time * s.walkSpeed * 5f + s.animOffset;
                    float walkAngle = Mathf.Sin(walkCycle) * 30f;
                    float kneeAngle = Mathf.Max(0f, -Mathf.Sin(walkCycle)) * 25f;

                    if (s.leftLeg)  s.leftLeg.localRotation  = Quaternion.Euler(walkAngle, 0, 0);
                    if (s.rightLeg) s.rightLeg.localRotation = Quaternion.Euler(-walkAngle, 0, 0);
                    // Ação dos braços oposta às pernas (marcha natural)
                    if (s.leftArm)  s.leftArm.localRotation  = Quaternion.Euler(-walkAngle * 0.6f, 0, 4f);
                    if (s.rightArm) s.rightArm.localRotation = Quaternion.Euler(walkAngle * 0.6f, 0, -4f);
                    // Leve balançar do torso
                    if (s.torso) {
                        float torsoSwing = Mathf.Sin(walkCycle * 2f) * 2.5f;
                        s.torso.localRotation = Quaternion.Euler(3f, torsoSwing, 0f);
                        s.torso.localPosition = s.torsoBaseLocalPos + Vector3.up * (Mathf.Abs(Mathf.Sin(walkCycle * 2f)) * 0.025f);
                    }
                    break;

                case SpectatorType.CheeringFan:
                    // Torcida energética: braços no alto, pulos quando carro passa
                    float cheerSpeed = playerNearby ? 7f : 2.8f;
                    float armSwing = Mathf.Sin(time * cheerSpeed + s.animOffset) * (playerNearby ? 22f : 8f);
                    float bodyBounce = Mathf.Abs(Mathf.Sin(time * cheerSpeed + s.animOffset)) * (playerNearby ? 0.09f : 0.02f);

                    if (s.leftArm)  s.leftArm.localRotation  = Quaternion.Euler(-118f + armSwing, -10f, -18f);
                    if (s.rightArm) s.rightArm.localRotation = Quaternion.Euler(-108f - armSwing,  10f,  18f);
                    if (s.torso) {
                        s.torso.localPosition = s.torsoBaseLocalPos + Vector3.up * bodyBounce;
                        // Leve rotação do tronco durante a torcida
                        float torsoRot = Mathf.Sin(time * cheerSpeed * 0.5f + s.animOffset) * 6f;
                        s.torso.localRotation = Quaternion.Euler(0f, torsoRot, 0f);
                    }
                    // Pernas dão um saltinho quando carro está perto
                    if (playerNearby) {
                        float legBounce = Mathf.Abs(Mathf.Sin(time * cheerSpeed + s.animOffset)) * 8f;
                        if (s.leftLeg)  s.leftLeg.localRotation  = Quaternion.Euler(-legBounce * 0.5f, 0, 0);
                        if (s.rightLeg) s.rightLeg.localRotation = Quaternion.Euler( legBounce * 0.5f, 0, 0);
                    }
                    // Cabeça segue o carro passando
                    if (s.head && playerNearby) {
                        Vector3 look = (playerPos - s.root.transform.position).normalized;
                        look.y = 0;
                        if (look.sqrMagnitude > 0.01f)
                            s.head.rotation = Quaternion.Slerp(s.head.rotation, Quaternion.LookRotation(look), Time.deltaTime * 7f);
                    }
                    break;

                case SpectatorType.Photographer:
                    // Segura o celular filmando, ajusta ângulo
                    float photoBob = Mathf.Sin(localTime * 0.8f) * 5f;
                    if (s.rightArm) s.rightArm.localRotation = Quaternion.Euler(-65f + photoBob, 12f, 8f);
                    if (s.leftArm)  s.leftArm.localRotation  = Quaternion.Euler(-50f - photoBob, -12f, -8f);

                    // Flash de foto quando carro passa perto
                    s.flashTimer -= Time.deltaTime;
                    if (playerNearby && s.flashTimer <= 0f) {
                        s.flashTimer = Random.Range(1.2f, 3.0f);
                        if (s.phoneLight) {
                            var rend = s.phoneLight.GetComponentInChildren<Renderer>();
                            if (rend) {
                                var block = new MaterialPropertyBlock();
                                block.SetColor("_BaseColor", Color.white);
                                block.SetColor("_EmissionColor", new Color(3f, 3.5f, 4f));
                                rend.SetPropertyBlock(block);
                            }
                        }
                    }
                    // Cabeça levemente inclinada para o visor
                    if (s.head) {
                        s.head.localRotation = Quaternion.Euler(-12f, 0f, 0f);
                        if (playerNearby) {
                            Vector3 look = (playerPos - s.root.transform.position).normalized;
                            look.y = 0;
                            if (look.sqrMagnitude > 0.01f)
                                s.head.rotation = Quaternion.Slerp(s.head.rotation, Quaternion.LookRotation(look), Time.deltaTime * 5f);
                        }
                    }
                    break;

                case SpectatorType.WatchingFan:
                default:
                    // Apoia no guard-rail, olhando a pista com leve balanço
                    float idleSwing = Mathf.Sin(time * 0.9f + s.animOffset) * 4f;
                    float idleLean  = Mathf.Sin(time * 1.3f + s.animOffset + 1f) * 2.5f;
                    if (s.torso) s.torso.localRotation = Quaternion.Euler(idleLean, idleSwing, 0f);

                    // Braços descansados, com leve balançar relaxado
                    if (s.leftArm)  s.leftArm.localRotation  = Quaternion.Euler(-12f + Mathf.Sin(time * 1.4f) * 4f, 0, 8f);
                    if (s.rightArm) s.rightArm.localRotation = Quaternion.Euler(-12f - Mathf.Sin(time * 1.4f) * 4f, 0, -8f);

                    // Cabeça segue o carro ou faz varredura casual da pista
                    if (s.head) {
                        if (playerNearby) {
                            Vector3 look = (playerPos - s.root.transform.position).normalized;
                            look.y = 0;
                            if (look.sqrMagnitude > 0.01f)
                                s.head.rotation = Quaternion.Slerp(s.head.rotation, Quaternion.LookRotation(look), Time.deltaTime * 5f);
                        } else {
                            float headScan = Mathf.Sin(time * 1.1f + s.animOffset) * 18f;
                            s.head.localRotation = Quaternion.Euler(-3f, headScan, 0f);
                        }
                    }
                    break;
            }
        }
    }
}
}
