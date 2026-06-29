using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 미니어처 주차장 3D 모형 — 900×900mm, 1:1 실척
/// 구역: A1~A8 / B1~B8 / C1~C8 (총 24칸)
/// 바닥: 회색 아스팔트 + 흰색 주차선
/// </summary>
public class MiniatureParkingLot : MonoBehaviour
{
    // ── 치수 (m) ─────────────────────────────────────────────
    public const float PW     = 0.900f;
    public const float PD     = 0.900f;
    public const float PT     = 0.010f;   // 판 두께
    public const float MARGIN = 0.075f;
    public const float SW     = 0.0775f;  // 주차칸 폭
    public const float SD     = 0.160f;   // 주차칸 깊이
    public const float HA     = 0.135f;   // 가로통로 폭
    public const float VA     = 0.130f;   // 세로통로 폭

    const float FLOOR_T = 0.001f;   // 바닥 타일 두께
    const float LINE_W  = 0.003f;   // 주차선 폭 (3 mm)
    const float LINE_T  = 0.001f;   // 주차선 두께
    const float OUTER_H = 0.018f;   // 외벽 높이
    const float OUTER_T = 0.004f;   // 외벽 두께

    [Header("색상")]
    public Color colAsphalt = new Color(0.27f, 0.27f, 0.27f);   // 아스팔트 회색
    public Color colLine    = new Color(0.92f, 0.92f, 0.90f);   // 흰색 주차선
    public Color colWall    = new Color(0.18f, 0.18f, 0.20f);   // 테두리 벽

    [Header("표시")]
    public bool showLabels     = true;
    public bool showSpaceLines = true;

    // ── 공간 데이터 ──────────────────────────────────────────
    public struct SpaceInfo
    {
        public string  id;
        public Vector3 localPos;
    }

    public Dictionary<string, SpaceInfo> SpaceMap { get; private set; }
        = new Dictionary<string, SpaceInfo>();

    public float R3Bot  => MARGIN;
    public float R2Bot  => MARGIN + SD + HA;
    public float R1Bot  => MARGIN + 2f * SD + 2f * HA;
    public float LStart => MARGIN;
    public float RStart => MARGIN + 4f * SW + VA;
    public float SurfY  => PT + FLOOR_T;

    private GameObject root;

    void Start() => Build();

    [ContextMenu("모형 생성")]
    public void Build()
    {
        Clear();
        root = new GameObject("MiniatureParkingLot");
        root.transform.SetParent(transform);
        root.transform.localPosition = Vector3.zero;

        BuildSlab();
        BuildWalls();
        if (showSpaceLines) BuildSpaceLines();
        CollectSpaces();
        if (showLabels) BuildLabels();

        Debug.Log($"[MiniPark] 생성: {SpaceMap.Count}칸");
    }

    // ── 1. 아스팔트 슬래브 ───────────────────────────────────
    void BuildSlab()
    {
        // 베이스 판
        Box("Slab",
            new Vector3(PW / 2f, PT / 2f, PD / 2f),
            new Vector3(PW, PT, PD),
            colAsphalt, root, shadow: true);

        // 표면 (살짝 밝은 아스팔트 — 미세한 깊이감)
        Box("Surface",
            new Vector3(PW / 2f, PT + FLOOR_T / 2f, PD / 2f),
            new Vector3(PW, FLOOR_T, PD),
            new Color(colAsphalt.r + 0.04f, colAsphalt.g + 0.04f, colAsphalt.b + 0.04f),
            root, shadow: false);
    }

    // ── 2. 외벽 ─────────────────────────────────────────────
    void BuildWalls()
    {
        var g  = Child("Walls");
        float wy = PT + OUTER_H / 2f;
        float t  = OUTER_T;

        Box("W_B", new Vector3(PW / 2f,          wy, t / 2f),        new Vector3(PW,  OUTER_H, t),  colWall, g, true);
        Box("W_T", new Vector3(PW / 2f,          wy, PD - t / 2f),   new Vector3(PW,  OUTER_H, t),  colWall, g, true);
        Box("W_L", new Vector3(t / 2f,           wy, PD / 2f),       new Vector3(t,   OUTER_H, PD), colWall, g, true);
        Box("W_R", new Vector3(PW - t / 2f,      wy, PD / 2f),       new Vector3(t,   OUTER_H, PD), colWall, g, true);
    }

    // ── 3. 흰색 주차 구역선 ──────────────────────────────────
    void BuildSpaceLines()
    {
        var g  = Child("SpaceLines");
        float ly = SurfY + LINE_T / 2f;

        float[] rowBots = { R1Bot, R2Bot, R3Bot };

        foreach (float rBot in rowBots)
        {
            float zCenter = rBot + SD / 2f;
            float zBot    = rBot;
            float zTop    = rBot + SD;

            foreach (float xStart in new[] { LStart, RStart })
            {
                float xCenter = xStart + 2f * SW;   // 4칸 가운데

                // ── 가로선 (상·하) ──
                Box($"H_Bot_{rBot:F2}_{xStart:F2}",
                    new Vector3(xCenter, ly, zBot),
                    new Vector3(4f * SW, LINE_T, LINE_W),
                    colLine, g, false);

                Box($"H_Top_{rBot:F2}_{xStart:F2}",
                    new Vector3(xCenter, ly, zTop),
                    new Vector3(4f * SW, LINE_T, LINE_W),
                    colLine, g, false);

                // ── 세로선 (칸 경계 5개) ──
                for (int ci = 0; ci <= 4; ci++)
                {
                    float px = xStart + ci * SW;
                    Box($"V_{rBot:F2}_{xStart:F2}_{ci}",
                        new Vector3(px, ly, zCenter),
                        new Vector3(LINE_W, LINE_T, SD),
                        colLine, g, false);
                }
            }
        }
    }

    // ── 4. 공간 데이터 수집 ──────────────────────────────────
    void CollectSpaces()
    {
        SpaceMap.Clear();
        float[] rowBots  = { R1Bot, R2Bot, R3Bot };
        string[] rowNames = { "A", "B", "C" };

        for (int ri = 0; ri < 3; ri++)
        {
            float zc = rowBots[ri] + SD / 2f;
            for (int ci = 0; ci < 4; ci++)
            {
                string idL = $"{rowNames[ri]}{ci + 1}";
                string idR = $"{rowNames[ri]}{ci + 5}";
                SpaceMap[idL] = new SpaceInfo { id = idL,
                    localPos = new Vector3(LStart + ci * SW + SW / 2f, SurfY, zc) };
                SpaceMap[idR] = new SpaceInfo { id = idR,
                    localPos = new Vector3(RStart + ci * SW + SW / 2f, SurfY, zc) };
            }
        }
    }

    // ── 5. 라벨 ──────────────────────────────────────────────
    void BuildLabels()
    {
        var g = Child("Labels");
        foreach (var kv in SpaceMap)
        {
            var sp = kv.Value;
            var go = new GameObject($"Lbl_{sp.id}");
            go.transform.SetParent(g.transform);
            go.transform.localPosition =
                new Vector3(sp.localPos.x, SurfY + 0.013f, sp.localPos.z);
            go.transform.localRotation = Quaternion.Euler(90, 0, 0);

            var tm = go.AddComponent<TextMesh>();
            tm.text          = sp.id;
            tm.fontSize      = 60;
            tm.characterSize = 0.0017f;
            tm.anchor        = TextAnchor.MiddleCenter;
            tm.fontStyle     = FontStyle.Bold;
            tm.color         = new Color(0.88f, 0.88f, 0.86f);  // 흰색에 가까운 회색
        }
    }

    // ── 공개 API ─────────────────────────────────────────────
    public Vector3 WorldPos(string id)
    {
        if (!SpaceMap.TryGetValue(id, out var sp)) return Vector3.zero;
        return root.transform.TransformPoint(sp.localPos);
    }

    public Vector3 LotCenter =>
        root ? root.transform.position + new Vector3(PW / 2f, PT, PD / 2f)
             : transform.position      + new Vector3(PW / 2f, PT, PD / 2f);

    // ── 유틸 ─────────────────────────────────────────────────
    GameObject Child(string name)
    {
        var g = new GameObject(name);
        g.transform.SetParent(root.transform);
        g.transform.localPosition = Vector3.zero;
        return g;
    }

    public static GameObject Box(string name, Vector3 lpos, Vector3 scale,
                                  Color color, GameObject parent, bool shadow)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        if (parent) go.transform.SetParent(parent.transform);
        go.transform.localPosition = lpos;
        go.transform.localScale    = scale;
        Object.Destroy(go.GetComponent<Collider>());

        var mat = new Material(
            Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        mat.color = color;

        var rend = go.GetComponent<Renderer>();
        rend.material          = mat;
        rend.shadowCastingMode = shadow
            ? UnityEngine.Rendering.ShadowCastingMode.On
            : UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = shadow;
        return go;
    }

    [ContextMenu("모형 삭제")]
    public void Clear()
    {
        SpaceMap?.Clear();
        if (root) DestroyImmediate(root);
        var ex = transform.Find("MiniatureParkingLot");
        if (ex) DestroyImmediate(ex.gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.15f);
        Gizmos.DrawWireCube(
            transform.position + new Vector3(PW / 2f, PT / 2f, PD / 2f),
            new Vector3(PW, PT + OUTER_H, PD));
    }
}
