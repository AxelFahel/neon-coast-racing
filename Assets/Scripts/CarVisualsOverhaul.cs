using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NeonCoast {
/// <summary>
/// Construtor e customizador 3D para os supercarros:
/// Reconstrói lataria, kit aerodinâmico exclusivo por modelo (Aster GT, Valkyrie Apex, Shinobi R-Spec),
/// vidros fumê, faróis LED/xenônio, lanternas traseiras com assinatura luminosa, difusores,
/// aerofólios e rodas esportivas com rotação e pinças de freio.
/// </summary>
public static class CarVisualsOverhaul {

    static Mesh cachedBodyMesh;
    static Mesh cachedGlassMesh;
    static Mesh cachedHeadlightsMesh;
    static Mesh cachedWheelMesh;
    static Mesh cachedTireMesh;

    public static void RebuildCarVisuals(ArcadeCar car, VehicleData vehicle, PaintData paintData) {
        if (!car) return;

        Transform coach = car.bodyVisual;
        if (!coach) {
            var vGO = new GameObject("Aster GT Coachwork");
            vGO.transform.SetParent(car.transform, false);
            coach = vGO.transform;
            car.bodyVisual = coach;
        }

        // Limpa peças antigas da carroceria de forma segura tanto no Editor quanto em Runtime
        var toDestroy = new List<GameObject>();
        foreach (Transform child in coach) {
            toDestroy.Add(child.gameObject);
        }
        foreach (var g in toDestroy) {
            if (Application.isPlaying) {
                g.SetActive(false);
                Object.Destroy(g);
            } else {
                Object.DestroyImmediate(g);
            }
        }

        // ── 1. MATERIAIS AUTOMOTIVOS PBR REALISTAS ────────────────────────────
        var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        // Pintura automotiva realista — camada base + clearcoat (verniz polido)
        var paintMat = new Material(litShader);
        paintMat.name = "SupercarPaint_" + paintData.name;
        Color paintBase = paintData.color * 0.88f;
        paintBase.a = 1f;
        paintMat.SetColor("_BaseColor", paintBase);
        paintMat.SetColor("_Color", paintBase);
        paintMat.SetFloat("_Metallic", 0.15f);
        paintMat.SetFloat("_Smoothness", 0.94f);
        paintMat.SetFloat("_Cull", 0f);
        paintMat.enableInstancing = true;

        // Vidro automotivo escurecido fumê refletivo
        var glassMat = new Material(litShader);
        glassMat.name = "SupercarGlass";
        Color glassCol = new Color(0.04f, 0.06f, 0.10f, 0.90f);
        glassMat.SetColor("_BaseColor", glassCol);
        glassMat.SetColor("_Color", glassCol);
        glassMat.SetFloat("_Metallic", 0.1f);
        glassMat.SetFloat("_Smoothness", 0.97f);
        glassMat.SetFloat("_Cull", 0f);
        glassMat.enableInstancing = true;

        // Fibra de carbono acetinada
        var carbonMat = new Material(litShader);
        carbonMat.name = "SupercarCarbon";
        Color carbonCol = new Color(0.07f, 0.075f, 0.08f);
        carbonMat.SetColor("_BaseColor", carbonCol);
        carbonMat.SetColor("_Color", carbonCol);
        carbonMat.SetFloat("_Metallic", 0.0f);
        carbonMat.SetFloat("_Smoothness", 0.3f);
        carbonMat.SetFloat("_Cull", 0f);
        carbonMat.enableInstancing = true;

        // Moldura rubi fumê da lanterna traseira
        var rearLampShader = Resources.Load<Shader>("VehicleRearLamp") ?? litShader;
        var tailHousingMat = new Material(rearLampShader);
        tailHousingMat.name = "SupercarTailHousing";
        tailHousingMat.SetColor("_BaseColor", new Color(0.35f, 0.004f, 0.008f, 1f));
        tailHousingMat.SetColor("_Color", new Color(0.35f, 0.004f, 0.008f, 1f));
        tailHousingMat.SetFloat("_Metallic", 0.0f);
        tailHousingMat.SetFloat("_Smoothness", 0.35f);
        tailHousingMat.EnableKeyword("_EMISSION");
        tailHousingMat.SetColor("_EmissionColor", new Color(.20f, .001f, .002f));
        tailHousingMat.SetFloat("_Cull", 0f);
        tailHousingMat.enableInstancing = true;

        // Elementos LED da lanterna traseira (vermelho rubi puro)
        var ledRedMat = new Material(rearLampShader);
        ledRedMat.name = "SupercarTailLED";
        Color baseTailRed = new Color(0.96f, 0.02f, 0.04f, 1f);
        ledRedMat.SetColor("_BaseColor", baseTailRed);
        ledRedMat.SetColor("_Color", baseTailRed);
        ledRedMat.SetFloat("_Metallic", 0.0f);
        ledRedMat.SetFloat("_Smoothness", 0.25f);
        ledRedMat.EnableKeyword("_EMISSION");
        ledRedMat.SetColor("_EmissionColor", new Color(2.2f, 0.02f, 0.03f));
        ledRedMat.SetFloat("_Cull", 0f);

        // Faróis LED xenônio branco-frio
        var ledCyanMat = new Material(litShader);
        ledCyanMat.name = "SupercarHeadLED";
        ledCyanMat.SetColor("_BaseColor", new Color(0.92f, 0.96f, 1.0f));
        ledCyanMat.SetColor("_Color", new Color(0.92f, 0.96f, 1.0f));
        ledCyanMat.SetFloat("_Metallic", 0.0f);
        ledCyanMat.SetFloat("_Smoothness", 0.92f);
        ledCyanMat.EnableKeyword("_EMISSION");
        ledCyanMat.SetColor("_EmissionColor", new Color(2.6f, 2.9f, 3.2f));

        // Aro de roda em liga usinada
        var wheelRimMat = new Material(litShader);
        wheelRimMat.name = "WheelAlloy";
        wheelRimMat.SetColor("_BaseColor", new Color(0.68f, 0.69f, 0.72f));
        wheelRimMat.SetColor("_Color", new Color(0.68f, 0.69f, 0.72f));
        wheelRimMat.SetFloat("_Metallic", 0.95f);
        wheelRimMat.SetFloat("_Smoothness", 0.78f);
        wheelRimMat.enableInstancing = true;

        // Pneu esportivo borracha mate
        var tireMat = new Material(litShader);
        tireMat.name = "TireRubber";
        tireMat.SetColor("_BaseColor", new Color(0.06f, 0.06f, 0.065f));
        tireMat.SetColor("_Color", new Color(0.06f, 0.06f, 0.065f));
        tireMat.SetFloat("_Metallic", 0.0f);
        tireMat.SetFloat("_Smoothness", 0.12f);
        tireMat.enableInstancing = true;

        // Pinça de freio esportiva vermelha
        var caliperMat = new Material(litShader);
        caliperMat.name = "BrakeCaliperRed";
        caliperMat.SetColor("_BaseColor", new Color(0.90f, 0.04f, 0.08f));
        caliperMat.SetColor("_Color", new Color(0.90f, 0.04f, 0.08f));
        caliperMat.SetFloat("_Metallic", 0.10f);
        caliperMat.SetFloat("_Smoothness", 0.82f);

        // Alumínio escovado (intercooler, ponteiras)
        var aluminumMat = new Material(litShader);
        aluminumMat.name = "AluminumMetal";
        aluminumMat.SetColor("_BaseColor", new Color(0.85f, 0.87f, 0.90f));
        aluminumMat.SetColor("_Color", new Color(0.85f, 0.87f, 0.90f));
        aluminumMat.SetFloat("_Metallic", 0.90f);
        aluminumMat.SetFloat("_Smoothness", 0.60f);

        // ── 2. CARREGAMENTO E SILHUETA EXCLUSIVA DE CADA MODELO ───────────────────
        EnsureMeshesLoaded();

        // A) LATARIA PRINCIPAL ESCULPIDA COM PROPORÇÕES EXCLUSIVAS
        if (cachedBodyMesh != null) {
            var bodyGO = CreateMeshObject("Supercar Body Shell", cachedBodyMesh, paintMat, coach);
            if (vehicle.id == "valkyrie_apex") {
                // Valkyrie: Hipercarro baixo, largo, agressivo (estilo Koenigsegg / Bolide)
                bodyGO.transform.localScale = new Vector3(1.12f, 0.88f, 1.06f);
            } else if (vehicle.id == "shinobi_rspec") {
                // Shinobi: Cupê esportivo / Tuner JDM compacto e encorpado (estilo GT-R / Supra)
                bodyGO.transform.localScale = new Vector3(0.98f, 1.04f, 0.98f);
            } else {
                // Aster GT: Gran Turismo elegante e equilibrado
                bodyGO.transform.localScale = new Vector3(1.00f, 1.00f, 1.00f);
            }
        } else {
            Mesh proceduralBody = GenerateProceduralBody(vehicle);
            CreateMeshObject("Supercar Body Shell", proceduralBody, paintMat, coach);
        }

        // B) JANELAS E PARA-BRISA EM VIDRO FUMÊ
        if (cachedGlassMesh != null) {
            var glassGO = CreateMeshObject("Supercar Glass Canopy", cachedGlassMesh, glassMat, coach);
            if (vehicle.id == "valkyrie_apex") {
                glassGO.transform.localScale = new Vector3(1.12f, 0.88f, 1.06f);
            } else if (vehicle.id == "shinobi_rspec") {
                glassGO.transform.localScale = new Vector3(0.98f, 1.04f, 0.98f);
            } else {
                glassGO.transform.localScale = new Vector3(1.00f, 1.00f, 1.00f);
            }
        } else {
            Mesh glassBody = GenerateProceduralGlass(vehicle);
            CreateMeshObject("Supercar Glass Canopy", glassBody, glassMat, coach);
        }

        // C) FARÓIS EM LED
        if (cachedHeadlightsMesh != null) {
            var hlMeshGO = CreateMeshObject("Supercar Headlights Mesh", cachedHeadlightsMesh, ledCyanMat, coach);
            if (vehicle.id == "valkyrie_apex") hlMeshGO.transform.localScale = new Vector3(1.12f, 0.88f, 1.06f);
            else if (vehicle.id == "shinobi_rspec") hlMeshGO.transform.localScale = new Vector3(0.98f, 1.04f, 0.98f);
        }

        // ── 3. KIT AERODINÂMICO EXCLUSIVO E DISTINTO POR MODELO ───────────────────
        if (vehicle.id == "valkyrie_apex") {
            // ── VALKYRIE APEX: KIT HIPERCARRO LE MANS ──
            // 1. Aerofólio Hypercar Swan-Neck maciço
            Mesh wingMesh = GenerateValkyrieWing();
            CreateMeshObject("Valkyrie_SwanNeck_Wing", wingMesh, carbonMat, coach);

            // 2. Tomada de ar no teto (Roof Scoop / Snorkel)
            var roofScoop = Box("Valkyrie_RoofScoop", coach, new Vector3(0f, 1.25f, -0.35f), new Vector3(0.38f, 0.16f, 0.65f), carbonMat);
            Box("RoofScoop_Intake", roofScoop.transform, new Vector3(0f, 0f, 0.33f), new Vector3(0.32f, 0.12f, 0.08f), carbonMat);

            // 3. Barbatana de tubarão central (Shark Fin dorsal)
            Box("Valkyrie_SharkFin", coach, new Vector3(0f, 1.10f, -1.25f), new Vector3(0.04f, 0.28f, 1.15f), carbonMat);

            // 4. Canards / Dive Planes dianteiros em fibra de carbono (flicks aerodinâmicos nos cantos)
            for (int s = -1; s <= 1; s += 2) {
                var canard1 = Box("Canard_Upper_" + s, coach, new Vector3(s * 0.92f, 0.52f, 1.82f), new Vector3(0.24f, 0.025f, 0.22f), carbonMat);
                canard1.transform.localRotation = Quaternion.Euler(s * 8f, s * -15f, s * 18f);
                var canard2 = Box("Canard_Lower_" + s, coach, new Vector3(s * 0.94f, 0.38f, 1.92f), new Vector3(0.26f, 0.025f, 0.24f), carbonMat);
                canard2.transform.localRotation = Quaternion.Euler(s * 10f, s * -18f, s * 22f);
            }

            // 5. Difusor traseiro extremo com 4 aletas verticais
            var diffBase = Box("Valkyrie_DiffuserBase", coach, new Vector3(0f, 0.26f, -2.18f), new Vector3(1.75f, 0.10f, 0.45f), carbonMat);
            for (int f = -2; f <= 2; f++) {
                if (f == 0) continue;
                Box("DiffuserFin_" + f, diffBase.transform, new Vector3(f * 0.36f, -0.06f, 0f), new Vector3(0.035f, 0.18f, 0.44f), carbonMat);
            }

        } else if (vehicle.id == "shinobi_rspec") {
            // ── SHINOBI R-SPEC: KIT DRIFT / JDM TUNER ──
            // 1. Aerofólio de Drift duplo com hastes de alumínio altas
            Mesh driftWing = GenerateShinobiDriftWing();
            CreateMeshObject("Shinobi_DriftWing", driftWing, carbonMat, coach);

            // 2. Boca do para-choque com Intercooler de alumínio exposto
            var intercooler = Box("Shinobi_Intercooler", coach, new Vector3(0f, 0.36f, 2.08f), new Vector3(0.85f, 0.26f, 0.14f), aluminumMat);
            Box("Intercooler_Grill", intercooler.transform, new Vector3(0f, 0f, 0.08f), new Vector3(0.88f, 0.28f, 0.02f), carbonMat);

            // 3. Capô esportivo com saídas de ar triplas (Hood Louvers)
            for (int l = 0; l < 3; l++) {
                float lz = 1.05f + l * 0.28f;
                var louverL = Box("Louver_L_" + l, coach, new Vector3(-0.35f, 0.72f - l * 0.04f, lz), new Vector3(0.25f, 0.025f, 0.16f), carbonMat);
                louverL.transform.localRotation = Quaternion.Euler(-18f, -10f, 0f);
                var louverR = Box("Louver_R_" + l, coach, new Vector3( 0.35f, 0.72f - l * 0.04f, lz), new Vector3(0.25f, 0.025f, 0.16f), carbonMat);
                louverR.transform.localRotation = Quaternion.Euler(-18f,  10f, 0f);
            }

            // 4. Alargadores de para-lama rebitados (Widebody Overfenders) nos 4 cantos
            for (int s = -1; s <= 1; s += 2) {
                var flareFront = Box("Overfender_Front_" + s, coach, new Vector3(s * 0.98f, 0.50f, 1.30f), new Vector3(0.14f, 0.32f, 0.75f), paintMat);
                flareFront.transform.localRotation = Quaternion.Euler(0f, s * 4f, s * -10f);
                var flareRear = Box("Overfender_Rear_" + s, coach, new Vector3(s * 1.00f, 0.52f, -1.30f), new Vector3(0.16f, 0.34f, 0.80f), paintMat);
                flareRear.transform.localRotation = Quaternion.Euler(0f, s * -4f, s * -12f);
            }

            // 5. Escapamento canhão de titânio inclinado
            var exhaust = Box("Shinobi_TitaniumExhaust", coach, new Vector3(0.55f, 0.32f, -2.22f), new Vector3(0.16f, 0.16f, 0.38f), aluminumMat);
            exhaust.transform.localRotation = Quaternion.Euler(6f, -15f, 0f);

        } else {
            // ── ASTER GT: GRAN TURISMO AERODINÂMICO ELEGANTE ──
            // 1. Spoilers discretos: Ducktail integrado na tampa do porta-malas
            var ducktail = Box("Aster_DucktailLip", coach, new Vector3(0f, 0.96f, -2.12f), new Vector3(1.55f, 0.06f, 0.22f), carbonMat);
            ducktail.transform.localRotation = Quaternion.Euler(-22f, 0f, 0f);

            // 2. Splitter frontal elegante em fibra de carbono
            var splitter = Box("Aster_FrontSplitter", coach, new Vector3(0f, 0.24f, 2.05f), new Vector3(1.82f, 0.05f, 0.38f), carbonMat);
            for (int s = -1; s <= 1; s += 2) {
                Box("SplitterWinglet_" + s, splitter.transform, new Vector3(s * 0.88f, 0.06f, 0.08f), new Vector3(0.04f, 0.14f, 0.25f), carbonMat);
            }

            // 3. Extratores de ar elegantes no capô
            Box("Aster_HoodVent_L", coach, new Vector3(-0.36f, 0.69f, 1.25f), new Vector3(0.18f, 0.02f, 0.35f), carbonMat);
            Box("Aster_HoodVent_R", coach, new Vector3( 0.36f, 0.69f, 1.25f), new Vector3(0.18f, 0.02f, 0.35f), carbonMat);

            // 4. Ponteiras duplas de escapamento central cromadas
            Box("Aster_Exhaust_L", coach, new Vector3(-0.11f, 0.33f, -2.24f), new Vector3(0.11f, 0.11f, 0.25f), aluminumMat);
            Box("Aster_Exhaust_R", coach, new Vector3( 0.11f, 0.33f, -2.24f), new Vector3(0.11f, 0.11f, 0.25f), aluminumMat);
        }

        // ── 4. ILUMINAÇÃO LED TRASEIRA COM ASSINATURA ESPECÍFICA ─────────────────
        Mesh housingMesh = GenerateTailHousing(vehicle);
        CreateMeshObject("Tail light housing", housingMesh, tailHousingMat, coach);

        Mesh tailMesh = GenerateTailLight(vehicle);
        var tailGO = CreateMeshObject("Tail light bar", tailMesh, ledRedMat, coach);

        var stopVertices = new List<Vector3>();
        var stopUV = new List<Vector2>();
        var stopTriangles = new List<int>();
        AddRibbon(stopVertices, stopUV, stopTriangles, new Vector3(-.14f, 1.135f, -2.27f), new Vector3(.14f, 1.135f, -2.27f), .012f);
        var stopMesh = new Mesh { name = "CenterStopLamp" };
        stopMesh.SetVertices(stopVertices); stopMesh.SetUVs(0, stopUV); stopMesh.SetTriangles(stopTriangles, 0); stopMesh.RecalculateNormals();
        var stop = CreateMeshObject("Center stop lamp", stopMesh, ledRedMat, coach);
        stop.transform.localScale = tailGO.transform.localScale;
        stop.GetComponent<Renderer>().enabled = false;

        // Luz pontual vermelha traseira de glow
        var tailGlowGO = new GameObject("Car_TailGlow");
        tailGlowGO.transform.SetParent(coach, false);
        tailGlowGO.transform.localPosition = new Vector3(0, 0.80f, -2.1f);
        var tailGlowLight = tailGlowGO.AddComponent<Light>();
        tailGlowLight.type = LightType.Point;
        tailGlowLight.color = new Color(1.0f, 0.05f, 0.06f);
        tailGlowLight.intensity = 1.2f;
        tailGlowLight.range = 4.5f;
        tailGlowLight.shadows = LightShadows.None;

        // Faróis em LED com filetes DRL
        Mesh headMesh = GenerateHeadlights(vehicle);
        CreateMeshObject("Headlight clusters", headMesh, ledCyanMat, coach);

        // ── 5. RODAS ESPORTIVAS REALISTAS ─────────────────────────────────────────
        // Caso A: O carro possui wheelVisuals do sistema de física (corrida em pista)
        if (car.wheelVisuals != null && car.wheelVisuals.Length > 0) {
            for (int i = 0; i < car.wheelVisuals.Length; i++) {
                var wheel = car.wheelVisuals[i];
                if (!wheel) continue;

                var oldChildren = new List<GameObject>();
                foreach (Transform child in wheel) oldChildren.Add(child.gameObject);
                foreach (var child in oldChildren) {
                    if (Application.isPlaying) { child.SetActive(false); Object.Destroy(child); }
                    else Object.DestroyImmediate(child);
                }

                bool isLeft = (i % 2 == 0);

                var tireGO = CreateMeshObject("Performance Tire", GenerateTireMesh(), tireMat, wheel);
                tireGO.transform.localPosition = Vector3.zero;
                tireGO.transform.localRotation = Quaternion.identity;

                if (cachedWheelMesh != null) {
                    var wheelGO = CreateMeshObject("Wheel Mesh", cachedWheelMesh, wheelRimMat, wheel);
                    wheelGO.transform.localScale = new Vector3(0.96f, 0.96f, 0.96f);
                    if (!isLeft) wheelGO.transform.localRotation = Quaternion.Euler(0, 180f, 0);
                } else {
                    RebuildProceduralWheel(wheel, isLeft, wheelRimMat, tireMat);
                }

                var caliperGO = CreateMeshObject("Brake Caliper", CreateCaliperMesh(), caliperMat, wheel);
                caliperGO.transform.localPosition = new Vector3(isLeft ? 0.04f : -0.04f, 0.16f, 0.08f);
                caliperGO.transform.localRotation = Quaternion.Euler(0, 0, isLeft ? 22f : -22f);
            }
        } else {
            // Caso B: Carro de vitrine / menu principal (sem física ativa de rodas)
            // Cria 4 rodas completas assentadas nas posições canônicas sob coach
            Vector3[] wheelPositions = new Vector3[] {
                new Vector3(-0.96f, 0.36f,  1.32f), // Dianteira Esquerda
                new Vector3( 0.96f, 0.36f,  1.32f), // Dianteira Direita
                new Vector3(-0.96f, 0.36f, -1.32f), // Traseira Esquerda
                new Vector3( 0.96f, 0.36f, -1.32f)  // Traseira Direita
            };

            for (int i = 0; i < 4; i++) {
                bool isLeft = (i % 2 == 0);
                var wheelAnchor = new GameObject("ShowcaseWheel_" + i);
                wheelAnchor.transform.SetParent(coach, false);
                wheelAnchor.transform.localPosition = wheelPositions[i];

                var tireGO = CreateMeshObject("Tire", GenerateTireMesh(), tireMat, wheelAnchor.transform);
                tireGO.transform.localPosition = Vector3.zero;

                if (cachedWheelMesh != null) {
                    var wheelGO = CreateMeshObject("Rim", cachedWheelMesh, wheelRimMat, wheelAnchor.transform);
                    wheelGO.transform.localScale = new Vector3(0.96f, 0.96f, 0.96f);
                    if (!isLeft) wheelGO.transform.localRotation = Quaternion.Euler(0, 180f, 0);
                } else {
                    RebuildProceduralWheel(wheelAnchor.transform, isLeft, wheelRimMat, tireMat);
                }

                var caliperGO = CreateMeshObject("Caliper", CreateCaliperMesh(), caliperMat, wheelAnchor.transform);
                caliperGO.transform.localPosition = new Vector3(isLeft ? 0.04f : -0.04f, 0.16f, 0.08f);
            }
        }

        // Camada 2 para não interferir em raycasts de câmera/cenário
        foreach (var tr in coach.GetComponentsInChildren<Transform>(true)) {
            tr.gameObject.layer = 2;
        }
    }

    static GameObject Box(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat) {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        if (Application.isPlaying) Object.Destroy(go.GetComponent<Collider>());
        else Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    static void EnsureMeshesLoaded() {
        if (cachedBodyMesh == null) {
            string bodyText = LoadModelText("Supercar_Body");
            if (!string.IsNullOrEmpty(bodyText)) cachedBodyMesh = ParseObjMesh(bodyText, "Supercar_Body_Mesh");
        }
        if (cachedGlassMesh == null) {
            string glassText = LoadModelText("Supercar_Glass");
            if (!string.IsNullOrEmpty(glassText)) cachedGlassMesh = ParseObjMesh(glassText, "Supercar_Glass_Mesh");
        }
        if (cachedHeadlightsMesh == null) {
            string hlText = LoadModelText("Supercar_Headlights");
            if (!string.IsNullOrEmpty(hlText)) cachedHeadlightsMesh = ParseObjMesh(hlText, "Supercar_Headlights_Mesh");
        }
        if (cachedWheelMesh == null) {
            string wheelText = LoadModelText("Supercar_Wheel");
            if (!string.IsNullOrEmpty(wheelText)) cachedWheelMesh = ParseObjMesh(wheelText, "Supercar_Wheel_Mesh");
        }
    }

    static string LoadModelText(string assetName) {
        var textAsset = Resources.Load<TextAsset>("Vehicles/" + assetName);
        if (textAsset != null && !string.IsNullOrEmpty(textAsset.text)) return textAsset.text;

        string[] searchPaths = new string[] {
            Path.Combine(Application.dataPath, "Resources", "Vehicles", assetName + ".txt"),
            Path.Combine(Application.dataPath, "Vehicles", assetName + ".obj"),
            Path.Combine(Application.dataPath, "Vehicles", assetName + ".txt")
        };

        foreach (var p in searchPaths) {
            if (File.Exists(p)) {
                try { return File.ReadAllText(p); } catch { }
            }
        }
        return null;
    }

    public static Mesh ParseObjMesh(string text, string meshName) {
        var verts = new List<Vector3>();
        var triangles = new List<int>();

        using (var reader = new StringReader(text)) {
            string line;
            while ((line = reader.ReadLine()) != null) {
                line = line.Trim();
                if (line.StartsWith("v ")) {
                    var parts = line.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 4) {
                        if (float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x) &&
                            float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y) &&
                            float.TryParse(parts[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float z)) {
                            verts.Add(new Vector3(x, y, z));
                        }
                    }
                } else if (line.StartsWith("f ")) {
                    var parts = line.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 4) {
                        int i0 = ParseObjIndex(parts[1]);
                        int i1 = ParseObjIndex(parts[2]);
                        int i2 = ParseObjIndex(parts[3]);
                        if (i0 >= 0 && i1 >= 0 && i2 >= 0) {
                            triangles.Add(i0); triangles.Add(i1); triangles.Add(i2);
                        }
                    } else if (parts.Length == 5) {
                        int i0 = ParseObjIndex(parts[1]);
                        int i1 = ParseObjIndex(parts[2]);
                        int i2 = ParseObjIndex(parts[3]);
                        int i3 = ParseObjIndex(parts[4]);
                        if (i0 >= 0 && i1 >= 0 && i2 >= 0 && i3 >= 0) {
                            triangles.Add(i0); triangles.Add(i1); triangles.Add(i2);
                            triangles.Add(i0); triangles.Add(i2); triangles.Add(i3);
                        }
                    }
                }
            }
        }

        var mesh = new Mesh { name = meshName };
        if (verts.Count > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(verts);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        return mesh;
    }

    static int ParseObjIndex(string part) {
        string numStr = part.Split('/')[0];
        if (int.TryParse(numStr, out int idx)) return idx - 1;
        return -1;
    }

    static GameObject CreateMeshObject(string name, Mesh mesh, Material mat, Transform parent) {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    // ────────────────────────────────────────────────────────────────────────────
    // AEROFÓLIO HIPERCARRO SWAN-NECK (VALKYRIE APEX)
    // ────────────────────────────────────────────────────────────────────────────
    static Mesh GenerateValkyrieWing() {
        float span = 2.30f;
        float wingY = 1.16f;
        float wingZ = -2.02f;

        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        const int numSpan = 14;
        const int numAirfoil = 8;
        float[] afX = new float[] { 0.16f, 0.11f, 0.00f, -0.11f, -0.16f, -0.11f, 0.00f, 0.11f };
        float[] afY = new float[] { 0.00f, 0.026f, 0.035f, 0.022f, 0.002f, -0.015f, -0.011f, -0.006f };

        for (int i = 0; i < numSpan; i++) {
            float t = (float)i / (numSpan - 1);
            float x = Mathf.Lerp(-span * 0.5f, span * 0.5f, t);
            for (int j = 0; j < numAirfoil; j++) {
                verts.Add(new Vector3(x, wingY + afY[j], wingZ + afX[j]));
                uvs.Add(new Vector2(t, (float)j / numAirfoil));
            }
        }

        for (int i = 0; i < numSpan - 1; i++) {
            for (int j = 0; j < numAirfoil; j++) {
                int nextJ = (j + 1) % numAirfoil;
                int a = i * numAirfoil + j;
                int b = i * numAirfoil + nextJ;
                int c = (i + 1) * numAirfoil + j;
                int d = (i + 1) * numAirfoil + nextJ;
                triangles.Add(a); triangles.Add(b); triangles.Add(c);
                triangles.Add(b); triangles.Add(d); triangles.Add(c);
            }
        }

        // Endplates gigantes de hipercarro e pilares swan-neck suspensos
        for (int s = -1; s <= 1; s += 2) {
            float xPlate = s * (span * 0.5f);
            AddDoubleSidedQuad(verts, uvs, triangles,
                new Vector3(xPlate, wingY - 0.16f, wingZ - 0.22f),
                new Vector3(xPlate, wingY - 0.16f, wingZ + 0.22f),
                new Vector3(xPlate, wingY + 0.16f, wingZ + 0.22f),
                new Vector3(xPlate, wingY + 0.16f, wingZ - 0.22f)
            );

            float xPylon = s * 0.44f;
            AddDoubleSidedQuad(verts, uvs, triangles,
                new Vector3(xPylon - 0.018f, 0.72f, wingZ + 0.12f),
                new Vector3(xPylon + 0.018f, 0.72f, wingZ + 0.12f),
                new Vector3(xPylon + 0.018f, wingY + 0.04f, wingZ - 0.04f),
                new Vector3(xPylon - 0.018f, wingY + 0.04f, wingZ - 0.04f)
            );
        }

        var mesh = new Mesh { name = "ValkyrieHyperWing" };
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // ────────────────────────────────────────────────────────────────────────────
    // AEROFÓLIO ALTO DE DRIFT / TUNER (SHINOBI R-SPEC)
    // ────────────────────────────────────────────────────────────────────────────
    static Mesh GenerateShinobiDriftWing() {
        float span = 2.05f;
        float wingY = 1.28f; // Aerofólio alto de drift
        float wingZ = -1.90f;

        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        // Lâmina principal de drift com ângulo de ataque acentuado
        AddDoubleSidedQuad(verts, uvs, triangles,
            new Vector3(-span * 0.5f, wingY - 0.04f, wingZ - 0.14f),
            new Vector3( span * 0.5f, wingY - 0.04f, wingZ - 0.14f),
            new Vector3( span * 0.5f, wingY + 0.05f, wingZ + 0.14f),
            new Vector3(-span * 0.5f, wingY + 0.05f, wingZ + 0.14f)
        );

        // Lâmina secundária inferior (Dual-Element Drift Wing)
        AddDoubleSidedQuad(verts, uvs, triangles,
            new Vector3(-span * 0.46f, wingY - 0.14f, wingZ - 0.08f),
            new Vector3( span * 0.46f, wingY - 0.14f, wingZ - 0.08f),
            new Vector3( span * 0.46f, wingY - 0.08f, wingZ + 0.09f),
            new Vector3(-span * 0.46f, wingY - 0.08f, wingZ + 0.09f)
        );

        // Placas laterais trapezoidais de drift
        for (int s = -1; s <= 1; s += 2) {
            float xPlate = s * (span * 0.5f);
            AddDoubleSidedQuad(verts, uvs, triangles,
                new Vector3(xPlate, wingY - 0.22f, wingZ - 0.18f),
                new Vector3(xPlate, wingY - 0.22f, wingZ + 0.18f),
                new Vector3(xPlate, wingY + 0.14f, wingZ + 0.18f),
                new Vector3(xPlate, wingY + 0.14f, wingZ - 0.18f)
            );

            // Hastes de alumínio perfuradas estilo competição de drift
            float xPylon = s * 0.52f;
            AddDoubleSidedQuad(verts, uvs, triangles,
                new Vector3(xPylon - 0.015f, 0.76f, wingZ - 0.05f),
                new Vector3(xPylon + 0.015f, 0.76f, wingZ - 0.05f),
                new Vector3(xPylon + 0.015f, wingY,  wingZ + 0.02f),
                new Vector3(xPylon - 0.015f, wingY,  wingZ + 0.02f)
            );
        }

        var mesh = new Mesh { name = "ShinobiDriftWing" };
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // ────────────────────────────────────────────────────────────────────────────
    // GERAÇÃO PROCEDURAL DE CORPO E VIDROS (FALLBACK)
    // ────────────────────────────────────────────────────────────────────────────
    static Mesh GenerateProceduralBody(VehicleData vehicle) {
        float[] zSlices; float[] widths; float[] heights; float[] yBases;

        if (vehicle.id == "valkyrie_apex") {
            zSlices = new float[] { 2.30f, 1.90f, 1.20f, 0.50f, -0.40f, -1.10f, -1.85f, -2.30f };
            widths  = new float[] { 0.64f, 1.02f, 1.10f, 1.12f,  1.10f,  1.04f,  0.86f,  0.62f };
            heights = new float[] { 0.36f, 0.54f, 0.66f, 0.64f,  0.62f,  0.55f,  0.45f,  0.34f };
            yBases  = new float[] { 0.28f, 0.26f, 0.25f, 0.24f,  0.24f,  0.25f,  0.26f,  0.28f };
        } else if (vehicle.id == "shinobi_rspec") {
            zSlices = new float[] { 2.00f, 1.65f, 1.00f, 0.20f, -0.60f, -1.20f, -1.80f, -2.05f };
            widths  = new float[] { 0.58f, 0.88f, 0.96f, 0.94f,  0.92f,  0.90f,  0.76f,  0.55f };
            heights = new float[] { 0.48f, 0.70f, 0.85f, 0.88f,  0.87f,  0.84f,  0.70f,  0.48f };
            yBases  = new float[] { 0.30f, 0.28f, 0.27f, 0.26f,  0.26f,  0.27f,  0.28f,  0.30f };
        } else {
            zSlices = new float[] { 2.18f, 1.80f, 1.10f, 0.30f, -0.50f, -1.20f, -1.85f, -2.20f };
            widths  = new float[] { 0.60f, 0.90f, 0.98f, 1.00f,  0.98f,  0.94f,  0.78f,  0.56f };
            heights = new float[] { 0.42f, 0.65f, 0.78f, 0.80f,  0.78f,  0.72f,  0.58f,  0.40f };
            yBases  = new float[] { 0.28f, 0.26f, 0.25f, 0.24f,  0.24f,  0.25f,  0.26f,  0.28f };
        }

        return BuildBodyMesh(vehicle.id + "_body", zSlices, widths, heights, yBases);
    }

    static Mesh GenerateProceduralGlass(VehicleData vehicle) {
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        float glassY = 0.68f, glassZ = 0.85f, glassH = 0.34f, glassLen = 0.70f;
        if (vehicle.id == "valkyrie_apex") { glassY = 0.60f; glassZ = 0.90f; glassH = 0.26f; glassLen = 0.80f; }
        else if (vehicle.id == "shinobi_rspec") { glassY = 0.72f; glassZ = 0.80f; glassH = 0.40f; glassLen = 0.60f; }

        float w = (vehicle.id == "valkyrie_apex") ? 1.02f : (vehicle.id == "shinobi_rspec") ? 0.88f : 0.92f;

        AddQuad(verts, uvs, triangles,
            new Vector3(-w * 0.85f, glassY,                    glassZ + glassLen * 0.1f),
            new Vector3( w * 0.85f, glassY,                    glassZ + glassLen * 0.1f),
            new Vector3( w * 0.75f, glassY + glassH,           glassZ - glassLen),
            new Vector3(-w * 0.75f, glassY + glassH,           glassZ - glassLen));

        float rearSlope = (vehicle.id == "shinobi_rspec") ? 0.05f : 0.15f;
        AddDoubleSidedQuad(verts, uvs, triangles,
            new Vector3(-w * 0.70f, glassY + glassH * 0.15f,  -1.10f),
            new Vector3( w * 0.70f, glassY + glassH * 0.15f,  -1.10f),
            new Vector3( w * 0.55f, glassY + glassH,           -1.10f - glassLen * rearSlope),
            new Vector3(-w * 0.55f, glassY + glassH,           -1.10f - glassLen * rearSlope));

        for (int s = -1; s <= 1; s += 2) {
            AddDoubleSidedQuad(verts, uvs, triangles,
                new Vector3(s * w * 0.92f, glassY,           glassZ - glassLen * 0.1f),
                new Vector3(s * w * 0.92f, glassY + glassH,  glassZ - glassLen),
                new Vector3(s * w * 0.92f, glassY + glassH,  -1.10f - glassLen * rearSlope),
                new Vector3(s * w * 0.90f, glassY,           -1.10f));
        }

        var mesh = new Mesh { name = vehicle.id + "_glass" };
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static Mesh BuildBodyMesh(string meshName, float[] zSlices, float[] widths, float[] heights, float[] yBases) {
        int sections = zSlices.Length;
        const int vertsPerSection = 7;
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        for (int j = 0; j < sections; j++) {
            float w = widths[j];
            float h = heights[j];
            float y0 = yBases[j];
            float z = zSlices[j];

            verts.Add(new Vector3(-w,        y0,          z));
            verts.Add(new Vector3(-w * 0.72f, y0 + h,     z));
            verts.Add(new Vector3( 0f,        y0 + h + 0.02f, z));
            verts.Add(new Vector3( w * 0.72f, y0 + h,     z));
            verts.Add(new Vector3( w,         y0,         z));
            verts.Add(new Vector3( w * 0.38f, y0 - 0.04f, z));
            verts.Add(new Vector3(-w * 0.38f, y0 - 0.04f, z));

            for (int k = 0; k < vertsPerSection; k++) uvs.Add(new Vector2((float)k / (vertsPerSection - 1), (float)j / (sections - 1)));
        }

        for (int j = 0; j < sections - 1; j++) {
            int baseA = j * vertsPerSection;
            int baseB = (j + 1) * vertsPerSection;
            for (int k = 0; k < vertsPerSection; k++) {
                int next = (k + 1) % vertsPerSection;
                int a = baseA + k, b = baseA + next, c = baseB + k, d = baseB + next;
                triangles.Add(a); triangles.Add(b); triangles.Add(c);
                triangles.Add(b); triangles.Add(d); triangles.Add(c);
                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);
            }
        }

        int frontCenter = verts.Count;
        Vector3 fc = Vector3.zero;
        for (int k = 0; k < vertsPerSection; k++) fc += verts[k];
        fc /= vertsPerSection;
        verts.Add(fc); uvs.Add(new Vector2(0.5f, 0f));
        for (int k = 0; k < vertsPerSection; k++) {
            triangles.Add(frontCenter); triangles.Add((k + 1) % vertsPerSection); triangles.Add(k);
        }

        int back = (sections - 1) * vertsPerSection;
        int backCenter = verts.Count;
        Vector3 bc = Vector3.zero;
        for (int k = 0; k < vertsPerSection; k++) bc += verts[back + k];
        bc /= vertsPerSection;
        verts.Add(bc); uvs.Add(new Vector2(0.5f, 1f));
        for (int k = 0; k < vertsPerSection; k++) {
            triangles.Add(backCenter); triangles.Add(back + k); triangles.Add(back + (k + 1) % vertsPerSection);
        }

        var mesh = new Mesh { name = meshName };
        if (verts.Count > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // ────────────────────────────────────────────────────────────────────────────
    // ILUMINAÇÃO LED (LANTERNA TRASEIRA E FARÓIS)
    // ────────────────────────────────────────────────────────────────────────────
    static Mesh GenerateTailHousing(VehicleData vehicle) {
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();
        System.Func<float, float> ZHousing = x => -2.235f + 0.355f * x * x - 0.006f;

        for (int s = -1; s <= 1; s += 2) {
            float xIn = s * 0.27f;
            float xOut = s * 0.73f;
            const int segs = 6;
            for (int i = 0; i < segs; i++) {
                float t0 = (float)i / segs;
                float t1 = (float)(i + 1) / segs;
                float x0 = Mathf.Lerp(xIn, xOut, t0);
                float x1 = Mathf.Lerp(xIn, xOut, t1);
                float z0 = ZHousing(x0);
                float z1 = ZHousing(x1);

                AddDoubleSidedQuad(verts, uvs, triangles,
                    new Vector3(x0, 0.835f, z0),
                    new Vector3(x1, 0.835f, z1),
                    new Vector3(x1, 0.945f, z1),
                    new Vector3(x0, 0.945f, z0)
                );
            }
        }

        var mesh = new Mesh { name = "TailLightHousing" };
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static Mesh GenerateTailLight(VehicleData vehicle) {
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();
        System.Func<float, float> ZLED = x => -2.235f + 0.355f * x * x - 0.015f;
        float[] chevronX = new float[] { 0.36f, 0.50f, 0.64f };

        for (int s = -1; s <= 1; s += 2) {
            float xIn  = s * 0.28f;
            float xOut = s * 0.72f;

            const int edgeSegs = 6;
            for (int i = 0; i < edgeSegs; i++) {
                float t0 = (float)i / edgeSegs;
                float t1 = (float)(i + 1) / edgeSegs;
                float x0 = Mathf.Lerp(xIn, xOut, t0);
                float x1 = Mathf.Lerp(xIn, xOut, t1);
                Vector3 pTop0 = new Vector3(x0, 0.935f, ZLED(x0));
                Vector3 pTop1 = new Vector3(x1, 0.935f, ZLED(x1));
                AddRibbon(verts, uvs, triangles, pTop0, pTop1, 0.014f);

                Vector3 pBot0 = new Vector3(x0, 0.845f, ZLED(x0));
                Vector3 pBot1 = new Vector3(x1, 0.845f, ZLED(x1));
                AddRibbon(verts, uvs, triangles, pBot0, pBot1, 0.014f);
            }

            Vector3 cOutBot = new Vector3(xOut, 0.845f, ZLED(xOut));
            Vector3 cOutTop = new Vector3(xOut, 0.935f, ZLED(xOut));
            AddRibbon(verts, uvs, triangles, cOutBot, cOutTop, 0.014f);

            Vector3 cInBot = new Vector3(xIn, 0.845f, ZLED(xIn));
            Vector3 cInTop = new Vector3(xIn, 0.935f, ZLED(xIn));
            AddRibbon(verts, uvs, triangles, cInBot, cInTop, 0.014f);

            foreach (float cx in chevronX) {
                float xApex  = s * (cx + 0.045f);
                float xInner = s * (cx - 0.038f);
                float yMid   = 0.890f;
                float yTop   = 0.922f;
                float yBot   = 0.858f;

                Vector3 pApex = new Vector3(xApex,  yMid, ZLED(xApex));
                Vector3 pTop  = new Vector3(xInner, yTop, ZLED(xInner));
                Vector3 pBot  = new Vector3(xInner, yBot, ZLED(xInner));
                Vector3 pStem = new Vector3(s * (cx - 0.055f), yMid, ZLED(s * (cx - 0.055f)));

                AddRibbon(verts, uvs, triangles, pTop,  pApex, 0.012f);
                AddRibbon(verts, uvs, triangles, pBot,  pApex, 0.012f);
                AddRibbon(verts, uvs, triangles, pStem, pApex, 0.012f);
            }
        }

        var mesh = new Mesh { name = "TailLightBar" };
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static Mesh GenerateTireMesh() {
        if (cachedTireMesh != null) return cachedTireMesh;

        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        Vector2[] profile = new Vector2[] {
            new Vector2(-0.108f, 0.245f),
            new Vector2(-0.134f, 0.275f),
            new Vector2(-0.140f, 0.320f),
            new Vector2(-0.132f, 0.348f),
            new Vector2(-0.108f, 0.362f),
            new Vector2(-0.040f, 0.366f),
            new Vector2( 0.000f, 0.368f),
            new Vector2( 0.040f, 0.366f),
            new Vector2( 0.108f, 0.362f),
            new Vector2( 0.132f, 0.348f),
            new Vector2( 0.140f, 0.320f),
            new Vector2( 0.134f, 0.275f),
            new Vector2( 0.108f, 0.245f),
            new Vector2( 0.000f, 0.238f)
        };

        const int radialSegments = 32;
        int pts = profile.Length;

        for (int i = 0; i <= radialSegments; i++) {
            float angle = (float)i / radialSegments * Mathf.PI * 2f;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            for (int j = 0; j < pts; j++) {
                verts.Add(new Vector3(profile[j].x, cos * profile[j].y, sin * profile[j].y));
                uvs.Add(new Vector2((float)i / radialSegments * 4f, (float)j / (pts - 1)));
            }
        }

        for (int i = 0; i < radialSegments; i++) {
            for (int j = 0; j < pts - 1; j++) {
                int a = i * pts + j;
                int b = (i + 1) * pts + j;
                int c = (i + 1) * pts + (j + 1);
                int d = i * pts + (j + 1);

                triangles.Add(a); triangles.Add(b); triangles.Add(c);
                triangles.Add(a); triangles.Add(c); triangles.Add(d);
            }
        }

        cachedTireMesh = new Mesh { name = "SupercarPerformanceTire" };
        cachedTireMesh.SetVertices(verts);
        cachedTireMesh.SetUVs(0, uvs);
        cachedTireMesh.SetTriangles(triangles, 0);
        cachedTireMesh.RecalculateNormals();
        cachedTireMesh.RecalculateBounds();
        return cachedTireMesh;
    }

    static void RebuildProceduralWheel(Transform wheel, bool isLeft, Material rimMat, Material tireMat) {
        var tireGO = CreateMeshObject("Performance Tire", GenerateTireMesh(), tireMat, wheel);
        tireGO.transform.localPosition = Vector3.zero;
        tireGO.transform.localRotation = Quaternion.identity;

        var rimGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rimGO.name = "Rim";
        rimGO.transform.SetParent(wheel, false);
        rimGO.transform.localRotation = Quaternion.Euler(0, 0, 90f);
        rimGO.transform.localScale = new Vector3(0.52f, 0.185f, 0.52f);
        if (Application.isPlaying) Object.Destroy(rimGO.GetComponent<Collider>());
        else Object.DestroyImmediate(rimGO.GetComponent<Collider>());
        rimGO.GetComponent<Renderer>().sharedMaterial = rimMat;
    }

    static Mesh GenerateHeadlights(VehicleData vehicle) {
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        for (int s = -1; s <= 1; s += 2) {
            Vector3 p1 = new Vector3(s * 0.55f, 0.67f, 2.14f);
            Vector3 p2 = new Vector3(s * 0.79f, 0.71f, 1.95f);
            Vector3 p3 = new Vector3(s * 0.81f, 0.76f, 1.84f);
            Vector3 p4 = new Vector3(s * 0.63f, 0.67f, 2.09f);
            AddQuad(verts, uvs, triangles, p1, p2, p3, p4);

            Vector3 prj1 = new Vector3(s * 0.64f, 0.61f, 2.18f);
            Vector3 prj2 = new Vector3(s * 0.76f, 0.65f, 2.05f);
            Vector3 prj3 = new Vector3(s * 0.75f, 0.68f, 2.05f);
            Vector3 prj4 = new Vector3(s * 0.64f, 0.64f, 2.18f);
            AddQuad(verts, uvs, triangles, prj1, prj2, prj3, prj4);
        }

        var mesh = new Mesh { name = "HeadlightMesh" };
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static Mesh CreateCaliperMesh() {
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        Vector3 size = new Vector3(0.065f, 0.09f, 0.14f);
        float hx = size.x * 0.5f;
        float hy = size.y * 0.5f;
        float hz = size.z * 0.5f;

        Vector3[] boxVerts = new Vector3[] {
            new Vector3(-hx, -hy, -hz), new Vector3(hx, -hy, -hz),
            new Vector3(hx,  hy, -hz), new Vector3(-hx,  hy, -hz),
            new Vector3(-hx, -hy,  hz), new Vector3(hx, -hy,  hz),
            new Vector3(hx,  hy,  hz), new Vector3(-hx,  hy,  hz)
        };

        int[] boxTris = new int[] {
            0,2,1, 0,3,2, 4,5,6, 4,6,7, 0,1,5, 0,5,4, 3,6,2, 3,7,6, 0,4,7, 0,7,3, 1,2,6, 1,6,5
        };

        verts.AddRange(boxVerts);
        triangles.AddRange(boxTris);
        for (int i = 0; i < boxVerts.Length; i++) uvs.Add(new Vector2(0.5f, 0.5f));

        var mesh = new Mesh { name = "ProceduralCaliper" };
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static void AddQuad(List<Vector3> verts, List<Vector2> uvs, List<int> triangles, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3) {
        int idx = verts.Count;
        verts.Add(v0); verts.Add(v1); verts.Add(v2); verts.Add(v3);
        uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(1, 0)); uvs.Add(new Vector2(1, 1)); uvs.Add(new Vector2(0, 1));
        triangles.Add(idx); triangles.Add(idx + 1); triangles.Add(idx + 2);
        triangles.Add(idx); triangles.Add(idx + 2); triangles.Add(idx + 3);
    }

    static void AddDoubleSidedQuad(List<Vector3> verts, List<Vector2> uvs, List<int> triangles, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3) {
        // These materials use Cull Off. Reversed triangles sharing the same
        // vertices cancel their normals and create invalid lighting on the wings.
        AddQuad(verts, uvs, triangles, v0, v1, v2, v3);
    }

    static void AddRibbon(List<Vector3> verts, List<Vector2> uvs, List<int> triangles, Vector3 pA, Vector3 pB, float halfThickness) {
        Vector3 dir = (pB - pA).normalized;
        Vector3 normal = new Vector3(0, 0, -1f);
        Vector3 perp = Vector3.Cross(dir, normal).normalized * halfThickness;
        if (perp.sqrMagnitude < 0.00001f) perp = new Vector3(0, halfThickness, 0);

        AddDoubleSidedQuad(verts, uvs, triangles,
            pA - perp,
            pB - perp,
            pB + perp,
            pA + perp
        );
    }
}
}
