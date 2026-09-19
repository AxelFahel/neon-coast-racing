using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NeonCoast {
/// <summary>
/// Construtor de modelo 3D real de supercarro esportivo:
/// Utiliza malha 3D autêntica de supercarro com portas detalhadas, janelas em vidro fumê,
/// para-brisa, colunas A/B/C, capô com vincos, entradas de ar laterais, espelhos retrovisores,
/// difusor traseiro, lanternas em LED e rodas esportivas com rotação física real.
/// </summary>
public static class CarVisualsOverhaul {

    static Mesh cachedBodyMesh;
    static Mesh cachedGlassMesh;
    static Mesh cachedHeadlightsMesh;
    static Mesh cachedWheelMesh;

    public static void RebuildCarVisuals(ArcadeCar car, VehicleData vehicle, PaintData paintData) {
        if (!car) return;

        Transform coach = car.bodyVisual;
        if (!coach) {
            var vGO = new GameObject("Aster GT Coachwork");
            vGO.transform.SetParent(car.transform, false);
            coach = vGO.transform;
            car.bodyVisual = coach;
        }

        // Limpa peças antigas da carroceria para reconstrução limpa
        var toDestroy = new List<GameObject>();
        foreach (Transform child in coach) {
            toDestroy.Add(child.gameObject);
        }
        foreach (var g in toDestroy) {
            Object.DestroyImmediate(g);
        }

        // ── 1. MATERIAIS AUTOMOTIVOS PBR DE ALTA FIDELIDADE ─────────────────────
        var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        // Pintura automotiva perolizada metálica
        var paintMat = new Material(litShader);
        paintMat.name = "SupercarPaint_" + paintData.name;
        paintMat.SetColor("_BaseColor", paintData.color);
        paintMat.SetFloat("_Metallic", 0.85f);
        paintMat.SetFloat("_Smoothness", 0.94f);
        paintMat.EnableKeyword("_EMISSION");
        paintMat.SetColor("_EmissionColor", paintData.color * 0.16f); // Realce noturno suave
        paintMat.SetFloat("_Cull", 0f); // Double-sided para acabamento impecável
        paintMat.enableInstancing = true;

        // Vidro automotivo escurecido fumê com alto índice de reflexão
        var glassMat = new Material(litShader);
        glassMat.name = "SupercarGlass";
        glassMat.SetColor("_BaseColor", new Color(0.04f, 0.06f, 0.10f, 0.96f));
        glassMat.SetFloat("_Metallic", 0.90f);
        glassMat.SetFloat("_Smoothness", 0.98f);
        glassMat.SetFloat("_Cull", 0f);
        glassMat.enableInstancing = true;

        // Fibra de carbono para apêndices aerodinâmicos
        var carbonMat = new Material(litShader);
        carbonMat.name = "SupercarCarbon";
        carbonMat.SetColor("_BaseColor", new Color(0.08f, 0.09f, 0.11f));
        carbonMat.SetFloat("_Metallic", 0.45f);
        carbonMat.SetFloat("_Smoothness", 0.82f);
        carbonMat.SetFloat("_Cull", 0f);
        carbonMat.enableInstancing = true;

        // Moldura e lentes escuras das lanternas traseiras (vidro fumê automotivo)
        var tailHousingMat = new Material(litShader);
        tailHousingMat.name = "SupercarTailHousing";
        tailHousingMat.SetColor("_BaseColor", new Color(0.04f, 0.04f, 0.06f));
        tailHousingMat.SetFloat("_Metallic", 0.60f);
        tailHousingMat.SetFloat("_Smoothness", 0.95f);
        tailHousingMat.SetFloat("_Cull", 0f);
        tailHousingMat.enableInstancing = true;

        // Elementos LED das lanternas traseiras (vermelho rubi puro, nítido e sem clarão branco)
        var unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? litShader;
        var ledRedMat = new Material(unlitShader);
        ledRedMat.name = "SupercarTailLED";
        Color baseTailRed = new Color(0.88f, 0.02f, 0.04f);
        ledRedMat.SetColor("_BaseColor", baseTailRed);
        ledRedMat.SetColor("_Color", baseTailRed);
        if (ledRedMat.HasProperty("_EmissionColor")) {
            ledRedMat.EnableKeyword("_EMISSION");
            ledRedMat.SetColor("_EmissionColor", baseTailRed * 0.85f);
        }
        ledRedMat.SetFloat("_Cull", 0f);
        ledRedMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;

        // Faróis dianteiros LED afiados (ciano-gelo / branco xenônio)
        var ledCyanMat = new Material(litShader);
        ledCyanMat.name = "SupercarHeadLED";
        ledCyanMat.SetColor("_BaseColor", new Color(0.78f, 0.95f, 1.0f));
        ledCyanMat.EnableKeyword("_EMISSION");
        ledCyanMat.SetColor("_EmissionColor", new Color(0.78f, 0.95f, 1.0f) * 4.8f);
        ledCyanMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;

        // Rodas: Liga de alumínio forjado polido
        var wheelRimMat = new Material(litShader);
        wheelRimMat.name = "WheelAlloy";
        wheelRimMat.SetColor("_BaseColor", new Color(0.72f, 0.75f, 0.80f));
        wheelRimMat.SetFloat("_Metallic", 0.92f);
        wheelRimMat.SetFloat("_Smoothness", 0.90f);
        wheelRimMat.enableInstancing = true;

        // Rodas: Borracha de alto desempenho
        var tireMat = new Material(litShader);
        tireMat.name = "TireRubber";
        tireMat.SetColor("_BaseColor", new Color(0.04f, 0.04f, 0.05f));
        tireMat.SetFloat("_Metallic", 0.05f);
        tireMat.SetFloat("_Smoothness", 0.20f);
        tireMat.enableInstancing = true;

        // Freios: Pinça esportiva vermelha racing
        var caliperMat = new Material(litShader);
        caliperMat.name = "BrakeCaliperRed";
        caliperMat.SetColor("_BaseColor", new Color(0.95f, 0.06f, 0.12f));
        caliperMat.SetFloat("_Metallic", 0.40f);
        caliperMat.SetFloat("_Smoothness", 0.85f);

        // ── 2. CARREGAMENTO DAS MALHAS 3D REAIS DO SUPERCARRO ───────────────────
        EnsureMeshesLoaded();

        // A) LATARIA ESCULPIDA PRINCIPAL (PORTAS, PARA-LAMAS, CAPÔ E TRASEIRA)
        if (cachedBodyMesh != null) {
            var bodyGO = CreateMeshObject("Supercar Body Shell", cachedBodyMesh, paintMat, coach);
            // Ajustes sutis por modelo de veículo
            if (vehicle.id == "valkyrie_apex") {
                bodyGO.transform.localScale = new Vector3(1.04f, 0.96f, 1.02f);
            } else if (vehicle.id == "shinobi_rspec") {
                bodyGO.transform.localScale = new Vector3(1.02f, 1.00f, 1.00f);
            }
        }

        // B) JANELAS E PARA-BRISA EM VIDRO FUMÊ
        if (cachedGlassMesh != null) {
            var glassGO = CreateMeshObject("Supercar Glass Canopy", cachedGlassMesh, glassMat, coach);
            if (vehicle.id == "valkyrie_apex") {
                glassGO.transform.localScale = new Vector3(1.04f, 0.96f, 1.02f);
            } else if (vehicle.id == "shinobi_rspec") {
                glassGO.transform.localScale = new Vector3(1.02f, 1.00f, 1.00f);
            }
        }

        // C) FARÓIS DIANTEIROS AUTÊNTICOS EM LED XENÔNIO ILUMINADO
        if (cachedHeadlightsMesh != null) {
            var hlMeshGO = CreateMeshObject("Supercar Headlights Mesh", cachedHeadlightsMesh, ledCyanMat, coach);
            if (vehicle.id == "valkyrie_apex") {
                hlMeshGO.transform.localScale = new Vector3(1.04f, 0.96f, 1.02f);
            } else if (vehicle.id == "shinobi_rspec") {
                hlMeshGO.transform.localScale = new Vector3(1.02f, 1.00f, 1.00f);
            }
        }

        // ── 3. PACOTE AERODINÂMICO EM FIBRA DE CARBONO ──────────────────────────
        // Para a versão Hyper / Drift, adiciona aerofólio traseiro GT de alta sustentação
        if (vehicle.id == "valkyrie_apex" || vehicle.id == "shinobi_rspec") {
            Mesh wingMesh = GenerateGTWing(vehicle);
            CreateMeshObject("GT Rear Wing", wingMesh, carbonMat, coach);
        }

        // ── 4. ILUMINAÇÃO LED (LANTERNA TRASEIRA E FARÓIS) ──────────────────────
        // Molduras/lentes fumê das lanternas traseiras (esquerda e direita)
        Mesh housingMesh = GenerateTailHousing(vehicle);
        var housingGO = CreateMeshObject("Tail light housing", housingMesh, tailHousingMat, coach);
        if (vehicle.id == "valkyrie_apex") {
            housingGO.transform.localScale = new Vector3(1.04f, 0.96f, 1.02f);
        } else if (vehicle.id == "shinobi_rspec") {
            housingGO.transform.localScale = new Vector3(1.02f, 1.00f, 1.00f);
        }

        // Elementos LED das lanternas traseiras (assinatura em Y e terceira luz de freio)
        Mesh tailMesh = GenerateTailLight(vehicle);
        var tailGO = CreateMeshObject("Tail light bar", tailMesh, ledRedMat, coach);
        if (vehicle.id == "valkyrie_apex") {
            tailGO.transform.localScale = new Vector3(1.04f, 0.96f, 1.02f);
        } else if (vehicle.id == "shinobi_rspec") {
            tailGO.transform.localScale = new Vector3(1.02f, 1.00f, 1.00f);
        }

        // Faróis dianteiros afiados com filetes DRL
        Mesh headMesh = GenerateHeadlights(vehicle);
        CreateMeshObject("Headlight clusters", headMesh, ledCyanMat, coach);

        // ── 5. RECONSTRUÇÃO DAS 4 RODAS COM ROTAÇÃO FÍSICA REAL ─────────────────
        if (car.wheelVisuals != null) {
            for (int i = 0; i < car.wheelVisuals.Length; i++) {
                var wheel = car.wheelVisuals[i];
                if (!wheel) continue;

                // Limpa filhos antigos da roda
                var oldChildren = new List<GameObject>();
                foreach (Transform child in wheel) oldChildren.Add(child.gameObject);
                foreach (var child in oldChildren) Object.DestroyImmediate(child);

                bool isLeft = (i % 2 == 0);

                if (cachedWheelMesh != null) {
                    var wheelGO = CreateMeshObject("Wheel Mesh", cachedWheelMesh, wheelRimMat, wheel);
                    if (!isLeft) {
                        // Inverte o aro direito para face externa ficar para fora
                        wheelGO.transform.localRotation = Quaternion.Euler(0, 180f, 0);
                    }
                } else {
                    // Fallback com roda procedural raiada
                    RebuildProceduralWheel(wheel, isLeft, wheelRimMat, tireMat);
                }

                // Pinça de freio vermelha racing fixada
                var caliperGO = CreateMeshObject("Brake Caliper", CreateCaliperMesh(), caliperMat, wheel);
                caliperGO.transform.localPosition = new Vector3(isLeft ? 0.04f : -0.04f, 0.16f, 0.08f);
                caliperGO.transform.localRotation = Quaternion.Euler(0, 0, isLeft ? 22f : -22f);
            }
        }

        // Coloca todas as peças na camada 2 (Ignore Raycast)
        foreach (var tr in coach.GetComponentsInChildren<Transform>(true)) {
            tr.gameObject.layer = 2;
        }
    }

    static void EnsureMeshesLoaded() {
        if (cachedBodyMesh == null) {
            string bodyText = LoadModelText("Supercar_Body");
            if (!string.IsNullOrEmpty(bodyText)) {
                cachedBodyMesh = ParseObjMesh(bodyText, "Supercar_Body_Mesh");
            }
        }
        if (cachedGlassMesh == null) {
            string glassText = LoadModelText("Supercar_Glass");
            if (!string.IsNullOrEmpty(glassText)) {
                cachedGlassMesh = ParseObjMesh(glassText, "Supercar_Glass_Mesh");
            }
        }
        if (cachedHeadlightsMesh == null) {
            string hlText = LoadModelText("Supercar_Headlights");
            if (!string.IsNullOrEmpty(hlText)) {
                cachedHeadlightsMesh = ParseObjMesh(hlText, "Supercar_Headlights_Mesh");
            }
        }
        if (cachedWheelMesh == null) {
            string wheelText = LoadModelText("Supercar_Wheel");
            if (!string.IsNullOrEmpty(wheelText)) {
                cachedWheelMesh = ParseObjMesh(wheelText, "Supercar_Wheel_Mesh");
            }
        }
    }

    static string LoadModelText(string assetName) {
        // 1. Tenta carregar via Resources (ideal para Standalone Build)
        var textAsset = Resources.Load<TextAsset>("Vehicles/" + assetName);
        if (textAsset != null && !string.IsNullOrEmpty(textAsset.text)) {
            return textAsset.text;
        }

        // 2. Tenta carregar diretamente do caminho de arquivos do projeto
        string[] searchPaths = new string[] {
            Path.Combine(Application.dataPath, "Resources", "Vehicles", assetName + ".txt"),
            Path.Combine(Application.dataPath, "Vehicles", assetName + ".obj"),
            Path.Combine(Application.dataPath, "Vehicles", assetName + ".txt")
        };

        foreach (var p in searchPaths) {
            if (File.Exists(p)) {
                try {
                    return File.ReadAllText(p);
                } catch { }
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
                        // Triângulo
                        int i0 = ParseObjIndex(parts[1]);
                        int i1 = ParseObjIndex(parts[2]);
                        int i2 = ParseObjIndex(parts[3]);
                        if (i0 >= 0 && i1 >= 0 && i2 >= 0) {
                            triangles.Add(i0); triangles.Add(i1); triangles.Add(i2);
                        }
                    } else if (parts.Length == 5) {
                        // Quadrilátero dividido em 2 triângulos
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
        if (int.TryParse(numStr, out int idx)) {
            return idx - 1; // OBJ é 1-indexed
        }
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
    // AEROFÓLIO GT EM FIBRA DE CARBONO COM PERFIL AERODINÂMICO
    // ────────────────────────────────────────────────────────────────────────────
    static Mesh GenerateGTWing(VehicleData vehicle) {
        float span = (vehicle.id == "valkyrie_apex") ? 2.25f : 1.95f;
        float wingY = 1.12f;
        float wingZ = -1.98f;

        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        const int numSpan = 12;
        const int numAirfoil = 8;
        float[] afX = new float[] { 0.15f, 0.10f, 0.00f, -0.10f, -0.15f, -0.10f, 0.00f, 0.10f };
        float[] afY = new float[] { 0.00f, 0.024f, 0.032f, 0.020f, 0.002f, -0.014f, -0.010f, -0.006f };

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

        // Endplates e suportes swan-neck
        for (int s = -1; s <= 1; s += 2) {
            float xPlate = s * (span * 0.5f);
            AddQuad(verts, uvs, triangles,
                new Vector3(xPlate, wingY - 0.10f, wingZ - 0.18f),
                new Vector3(xPlate, wingY - 0.10f, wingZ + 0.18f),
                new Vector3(xPlate, wingY + 0.12f, wingZ + 0.18f),
                new Vector3(xPlate, wingY + 0.12f, wingZ - 0.18f)
            );

            float xPylon = s * 0.46f;
            AddQuad(verts, uvs, triangles,
                new Vector3(xPylon - 0.016f, 0.74f, wingZ + 0.08f),
                new Vector3(xPylon + 0.016f, 0.74f, wingZ + 0.08f),
                new Vector3(xPylon + 0.016f, wingY, wingZ - 0.02f),
                new Vector3(xPylon - 0.016f, wingY, wingZ - 0.02f)
            );
        }

        var mesh = new Mesh { name = "GTWing_" + vehicle.id };
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

        // Molduras fumê escuras para a lanterna esquerda e direita (sem cobrir o centro da lataria)
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

        // 1. LANTERNAS TRASEIRAS LATERAIS (ESQUERDA E DIREITA INDEPENDENTES)
        // O centro do veículo permanece limpo para exibir a pintura, grade e difusor
        float[] chevronX = new float[] { 0.36f, 0.50f, 0.64f };

        for (int s = -1; s <= 1; s += 2) {
            float xIn  = s * 0.28f;
            float xOut = s * 0.72f;

            // Bordo superior em LED da lanterna
            const int edgeSegs = 6;
            for (int i = 0; i < edgeSegs; i++) {
                float t0 = (float)i / edgeSegs;
                float t1 = (float)(i + 1) / edgeSegs;
                float x0 = Mathf.Lerp(xIn, xOut, t0);
                float x1 = Mathf.Lerp(xIn, xOut, t1);
                Vector3 pTop0 = new Vector3(x0, 0.935f, ZLED(x0));
                Vector3 pTop1 = new Vector3(x1, 0.935f, ZLED(x1));
                AddRibbon(verts, uvs, triangles, pTop0, pTop1, 0.006f);

                Vector3 pBot0 = new Vector3(x0, 0.845f, ZLED(x0));
                Vector3 pBot1 = new Vector3(x1, 0.845f, ZLED(x1));
                AddRibbon(verts, uvs, triangles, pBot0, pBot1, 0.005f);
            }

            // Fechamento lateral do cluster da lanterna
            Vector3 cOutBot = new Vector3(xOut, 0.845f, ZLED(xOut));
            Vector3 cOutTop = new Vector3(xOut, 0.935f, ZLED(xOut));
            AddRibbon(verts, uvs, triangles, cOutBot, cOutTop, 0.006f);

            Vector3 cInBot = new Vector3(xIn, 0.845f, ZLED(xIn));
            Vector3 cInTop = new Vector3(xIn, 0.935f, ZLED(xIn));
            AddRibbon(verts, uvs, triangles, cInBot, cInTop, 0.005f);

            // Assinatura icônica Lamborghini: 3 flechas 'Y' nítidas dentro da lanterna
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

                AddRibbon(verts, uvs, triangles, pTop,  pApex, 0.006f);
                AddRibbon(verts, uvs, triangles, pBot,  pApex, 0.006f);
                AddRibbon(verts, uvs, triangles, pStem, pApex, 0.005f);
            }
        }

        // 2. TERCEIRA LUZ DE FREIO CENTRAL ELEVADA (CHMSL)
        // Filete sutil no bordo de fuga do deck traseiro
        float highSpan = 0.14f;
        float highY = 1.135f;
        Vector3 h0 = new Vector3(-highSpan, highY, -2.260f - 0.010f);
        Vector3 h1 = new Vector3( highSpan, highY, -2.260f - 0.010f);
        AddRibbon(verts, uvs, triangles, h0, h1, 0.006f);

        var mesh = new Mesh { name = "TailLightBar" };
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static Mesh GenerateHeadlights(VehicleData vehicle) {
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        for (int s = -1; s <= 1; s += 2) {
            // Lâmina DRL em LED ao longo do contorno do farol
            Vector3 p1 = new Vector3(s * 0.55f, 0.67f, 2.14f);
            Vector3 p2 = new Vector3(s * 0.79f, 0.71f, 1.95f);
            Vector3 p3 = new Vector3(s * 0.81f, 0.76f, 1.84f);
            Vector3 p4 = new Vector3(s * 0.63f, 0.67f, 2.09f);
            AddQuad(verts, uvs, triangles, p1, p2, p3, p4);

            // Projetor duplo de xenônio interno
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

    static void RebuildProceduralWheel(Transform wheel, bool isLeft, Material rimMat, Material tireMat) {
        // Fallback procedural tire & rim
        var tireGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tireGO.name = "Tire";
        tireGO.transform.SetParent(wheel, false);
        tireGO.transform.localRotation = Quaternion.Euler(0, 0, 90f);
        tireGO.transform.localScale = new Vector3(0.72f, 0.18f, 0.72f);
        Object.DestroyImmediate(tireGO.GetComponent<Collider>());
        tireGO.GetComponent<Renderer>().sharedMaterial = tireMat;

        var rimGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rimGO.name = "Rim";
        rimGO.transform.SetParent(wheel, false);
        rimGO.transform.localRotation = Quaternion.Euler(0, 0, 90f);
        rimGO.transform.localScale = new Vector3(0.52f, 0.185f, 0.52f);
        Object.DestroyImmediate(rimGO.GetComponent<Collider>());
        rimGO.GetComponent<Renderer>().sharedMaterial = rimMat;
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
        int idx = verts.Count;
        verts.Add(v0); verts.Add(v1); verts.Add(v2); verts.Add(v3);
        uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(1, 0)); uvs.Add(new Vector2(1, 1)); uvs.Add(new Vector2(0, 1));
        // Face frontal (olhando em direção a +Z / frente do carro)
        triangles.Add(idx); triangles.Add(idx + 1); triangles.Add(idx + 2);
        triangles.Add(idx); triangles.Add(idx + 2); triangles.Add(idx + 3);
        // Face posterior (olhando em direção a -Z / câmera traseira)
        triangles.Add(idx); triangles.Add(idx + 2); triangles.Add(idx + 1);
        triangles.Add(idx); triangles.Add(idx + 3); triangles.Add(idx + 2);
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
