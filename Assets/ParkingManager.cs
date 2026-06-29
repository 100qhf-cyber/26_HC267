using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 주차장 UI + 차량 관리
/// UnityEngine.UI 패키지 없이 OnGUI만 사용 — 모든 Unity 버전 호환
/// </summary>
public class ParkingManager : MonoBehaviour
{
    // ── 차량 색상 ─────────────────────────────────────────────
    static readonly Color[] CAR_COLORS =
    {
        new Color(0.85f, 0.15f, 0.15f),
        new Color(0.15f, 0.38f, 0.80f),
        new Color(0.92f, 0.92f, 0.92f),
        new Color(0.50f, 0.50f, 0.55f),
        new Color(0.12f, 0.60f, 0.28f),
        new Color(0.90f, 0.72f, 0.08f),
        new Color(0.55f, 0.18f, 0.65f),
        new Color(0.95f, 0.52f, 0.10f),
    };
    int colorIdx = 0;

    MiniatureParkingLot lot;
    Dictionary<string, GameObject> parkedCars = new Dictionary<string, GameObject>();

    // ── UI 상태 ───────────────────────────────────────────────
    string inputText   = "";
    string statusMsg   = "구역을 입력하고 주차 버튼을 누르세요.";
    Color  statusColor = new Color(0.70f, 0.70f, 0.70f);

    const float PANEL_W = 270f;

    // ── GUIStyle ──────────────────────────────────────────────
    GUIStyle stPanel, stTitle, stLabel, stRowLabel;
    GUIStyle stInput, stBtnPark, stBtnDepart, stStatus;
    GUIStyle stCellFree, stCellOcc;
    bool     stylesReady = false;

    // ─────────────────────────────────────────────────────────
    void Start()
    {
#if UNITY_2023_1_OR_NEWER
        lot = FindFirstObjectByType<MiniatureParkingLot>();
#else
        lot = Object.FindObjectOfType<MiniatureParkingLot>();
#endif
    }

    // ── GUIStyle 초기화 (OnGUI 첫 호출 때 1회) ───────────────
    void InitStyles()
    {
        if (stylesReady) return;
        stylesReady = true;

        stPanel = new GUIStyle();
        stPanel.normal.background = Tex(new Color(0.08f, 0.10f, 0.14f, 0.96f));

        stTitle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        stTitle.normal.textColor = new Color(0.85f, 0.92f, 1f);

        stLabel = new GUIStyle(GUI.skin.label) { fontSize = 12 };
        stLabel.normal.textColor = new Color(0.60f, 0.68f, 0.80f);

        stRowLabel = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 11,
            fontStyle = FontStyle.Bold
        };
        stRowLabel.normal.textColor = new Color(0.65f, 0.75f, 1.0f);

        stInput = new GUIStyle(GUI.skin.textField)
        {
            fontSize  = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            fixedHeight = 48
        };
        stInput.normal.background  = Tex(new Color(0.16f, 0.20f, 0.26f));
        stInput.focused.background = Tex(new Color(0.20f, 0.24f, 0.32f));
        stInput.normal.textColor   = new Color(0.90f, 0.95f, 1.0f);
        stInput.focused.textColor  = new Color(0.90f, 0.95f, 1.0f);

        stBtnPark   = MakeBtnStyle(new Color(0.12f, 0.52f, 0.28f));
        stBtnDepart = MakeBtnStyle(new Color(0.52f, 0.15f, 0.15f));

        stStatus = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 12,
            wordWrap  = true,
            alignment = TextAnchor.MiddleCenter
        };

        stCellFree = MakeCellStyle(new Color(0.22f, 0.62f, 0.38f));
        stCellOcc  = MakeCellStyle(new Color(0.75f, 0.18f, 0.18f));
    }

    // ── OnGUI ─────────────────────────────────────────────────
    void OnGUI()
    {
        InitStyles();

        float pX = Screen.width - PANEL_W;
        float pH = Screen.height;

        // 배경 패널
        GUI.Box(new Rect(pX, 0, PANEL_W, pH), GUIContent.none, stPanel);

        float pad = 14f;
        GUILayout.BeginArea(new Rect(pX + pad, 18, PANEL_W - pad * 2, pH - 36));
        GUILayout.BeginVertical();

        // 타이틀
        GUILayout.Label("  주차장 제어", stTitle, GUILayout.Height(36));
        Sep();

        // 입력
        GUILayout.Label("구역 입력  (예: A3, C7)", stLabel, GUILayout.Height(22));
        GUILayout.Space(4);
        string prev = inputText;
        inputText = GUILayout.TextField(inputText, 3, stInput).ToUpper().Trim();
        // 한글 입력 방지 (영숫자만)
        if (inputText != prev)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (char ch in inputText)
                if ((ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9')) sb.Append(ch);
            inputText = sb.ToString();
        }

        GUILayout.Space(6);

        // 버튼 행
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("주차  ▶", stBtnPark))   OnPark();
        GUILayout.Space(8);
        if (GUILayout.Button("출차  ✕", stBtnDepart)) OnDepart();
        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        // 상태 텍스트
        stStatus.normal.textColor = statusColor;
        GUILayout.Label(statusMsg, stStatus, GUILayout.Height(40));
        Sep();

        // 현황 헤더
        GUILayout.BeginHorizontal();
        GUILayout.Label("주차 현황", stLabel);
        GUILayout.FlexibleSpace();
        GUILayout.Label($"{parkedCars.Count} / 24", stLabel);
        GUILayout.EndHorizontal();
        GUILayout.Space(4);

        // 칸 그리드
        foreach (string row in new[] { "A", "B", "C" })
        {
            GUILayout.Label($"  {row} 열", stRowLabel, GUILayout.Height(18));

            for (int half = 0; half < 2; half++)
            {
                GUILayout.BeginHorizontal();
                for (int c = 0; c < 4; c++)
                {
                    string id  = $"{row}{half * 4 + c + 1}";
                    bool   occ = parkedCars.ContainsKey(id);
                    if (GUILayout.Button(id, occ ? stCellOcc : stCellFree, GUILayout.Height(26)))
                        inputText = id;
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(2);
            }

            if (row != "C") { GUILayout.Space(4); Sep(); }
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    // ── 주차 ─────────────────────────────────────────────────
    void OnPark()
    {
        string id = inputText.Trim().ToUpper();
        if (!ValidateId(id)) return;

        if (parkedCars.ContainsKey(id))
        {
            SetStatus($"{id} 는 이미 주차 중입니다.", new Color(1f, 0.75f, 0.2f));
            return;
        }

        Vector3 pos = lot.WorldPos(id);
        Color   col = CAR_COLORS[colorIdx % CAR_COLORS.Length];
        colorIdx++;

        var car = SpawnCar(pos, id[0] != 'C', col);
        parkedCars[id] = car;
        SetStatus($"{id}  주차 완료!", new Color(0.4f, 0.9f, 0.5f));
        StartCoroutine(ScaleIn(car.transform));
    }

    // ── 출차 ─────────────────────────────────────────────────
    void OnDepart()
    {
        string id = inputText.Trim().ToUpper();
        if (!ValidateId(id)) return;

        if (!parkedCars.TryGetValue(id, out var car))
        {
            SetStatus($"{id} 에 주차된 차가 없습니다.", new Color(1f, 0.75f, 0.2f));
            return;
        }

        Destroy(car);
        parkedCars.Remove(id);
        SetStatus($"{id}  출차 완료.", new Color(0.7f, 0.7f, 0.7f));
    }

    bool ValidateId(string id)
    {
        if (lot == null || !lot.SpaceMap.ContainsKey(id))
        {
            SetStatus($"'{id}' 은 유효하지 않습니다.\nA1 ~ C8 사이로 입력하세요.", new Color(1f, 0.4f, 0.4f));
            return false;
        }
        return true;
    }

    void SetStatus(string msg, Color col) { statusMsg = msg; statusColor = col; }

    // ── 차량 생성 (로보택시 스타일) ───────────────────────────
    GameObject SpawnCar(Vector3 worldPos, bool faceDown, Color bodyColor)
    {
        float bW = MiniatureParkingLot.SW * 0.74f;   // 차 폭
        float bL = MiniatureParkingLot.SD * 0.58f;   // 차 길이
        float bH = 0.017f;   // 하체 높이
        float cH = 0.013f;   // 캐빈 높이
        float wR = 0.007f;   // 바퀴 반지름
        float wW = 0.006f;   // 바퀴 폭

        var carRoot = new GameObject("Car");
        carRoot.transform.position = worldPos + Vector3.up * 0.0005f;
        carRoot.transform.rotation = faceDown
            ? Quaternion.identity : Quaternion.Euler(0, 180, 0);

        // ── 헬퍼: 박스 ──────────────────────────────────────
        void B(string nm, Vector3 lp, Vector3 sc, Color col, bool sh = true)
            => MiniatureParkingLot.Box(nm, lp, sc, col, carRoot, sh);

        // ── 헬퍼: 실린더 (바퀴·돔용) ─────────────────────────
        void Cyl(string nm, Vector3 lp, Vector3 sc, Quaternion rot, Color col)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = nm;
            go.transform.SetParent(carRoot.transform);
            go.transform.localPosition = lp;
            go.transform.localScale    = sc;
            go.transform.localRotation = rot;
            Object.Destroy(go.GetComponent<Collider>());
            var mat = new Material(
                Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = col;
            var rend = go.GetComponent<Renderer>();
            rend.material          = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            rend.receiveShadows    = true;
        }
        // ────────────────────────────────────────────────────

        Color dk   = bodyColor * 0.80f;
        Color gls  = new Color(0.45f, 0.65f, 0.85f);
        Color crm  = new Color(0.78f, 0.78f, 0.80f);
        Color blk  = new Color(0.10f, 0.10f, 0.10f);
        Color hub  = new Color(0.65f, 0.65f, 0.68f);
        Color wht  = new Color(0.95f, 0.95f, 0.88f);
        Color red  = new Color(0.85f, 0.10f, 0.10f);
        Color snsr = new Color(0.20f, 0.20f, 0.22f);

        // 섀시 (하부 판)
        B("Chassis", new Vector3(0, bH*0.26f, 0),
            new Vector3(bW,       bH*0.52f, bL),       bodyColor);

        // 차체 메인
        B("Body",    new Vector3(0, bH*0.76f, 0),
            new Vector3(bW,       bH*0.55f, bL*0.88f), bodyColor);

        // 후드 (앞)
        B("Hood",    new Vector3(0, bH*0.57f, -bL*0.43f),
            new Vector3(bW*0.90f, bH*0.20f,   bL*0.17f), bodyColor);

        // 트렁크 (뒤)
        B("Trunk",   new Vector3(0, bH*0.57f,  bL*0.42f),
            new Vector3(bW*0.88f, bH*0.18f,   bL*0.14f), bodyColor);

        // 캐빈 (루프)
        B("Cabin",   new Vector3(0, bH+cH*0.50f, bL*0.02f),
            new Vector3(bW*0.80f, cH,            bL*0.54f), dk);

        // 앞 유리
        B("WindF",   new Vector3(0, bH+cH*0.36f, -bL*0.24f),
            new Vector3(bW*0.76f, cH*0.68f, 0.003f), gls, false);

        // 뒷 유리
        B("WindR",   new Vector3(0, bH+cH*0.35f,  bL*0.24f),
            new Vector3(bW*0.72f, cH*0.60f, 0.003f), gls, false);

        // 옆 유리 (좌·우)
        B("WinL", new Vector3(-bW*0.40f, bH+cH*0.42f, 0),
            new Vector3(0.002f, cH*0.55f, bL*0.36f), gls, false);
        B("WinR", new Vector3( bW*0.40f, bH+cH*0.42f, 0),
            new Vector3(0.002f, cH*0.55f, bL*0.36f), gls, false);

        // 앞 범퍼
        B("BumperF", new Vector3(0, bH*0.36f, -bL*0.50f),
            new Vector3(bW*0.86f, bH*0.36f, 0.003f), crm);

        // 뒷 범퍼
        B("BumperR", new Vector3(0, bH*0.35f,  bL*0.50f),
            new Vector3(bW*0.84f, bH*0.30f, 0.003f), crm);

        // 헤드라이트 (앞)
        B("HeadL", new Vector3(-bW*0.37f, bH*0.64f, -bL*0.50f),
            new Vector3(bW*0.18f, bH*0.22f, 0.002f), wht, false);
        B("HeadR", new Vector3( bW*0.37f, bH*0.64f, -bL*0.50f),
            new Vector3(bW*0.18f, bH*0.22f, 0.002f), wht, false);

        // 테일라이트 (뒤)
        B("TailL", new Vector3(-bW*0.38f, bH*0.64f, bL*0.50f),
            new Vector3(bW*0.20f, bH*0.20f, 0.002f), red, false);
        B("TailR", new Vector3( bW*0.38f, bH*0.64f, bL*0.50f),
            new Vector3(bW*0.20f, bH*0.20f, 0.002f), red, false);

        // 사이드 미러
        B("MirrorL", new Vector3(-bW*0.52f, bH+cH*0.15f, -bL*0.21f),
            new Vector3(0.005f, 0.004f, 0.009f), blk);
        B("MirrorR", new Vector3( bW*0.52f, bH+cH*0.15f, -bL*0.21f),
            new Vector3(0.005f, 0.004f, 0.009f), blk);

        // 로보택시 센서 돔 (루프 위)
        B("SensorBase", new Vector3(0, bH+cH+0.003f, bL*0.05f),
            new Vector3(0.013f, 0.004f, 0.018f), snsr);
        Cyl("SensorDome", new Vector3(0, bH+cH+0.008f, bL*0.05f),
            new Vector3(0.008f, 0.003f, 0.008f), Quaternion.identity, snsr);

        // ── 바퀴 (Cylinder, 옆으로 누임) ──────────────────────
        // Unity Cylinder: 반지름=0.5, 높이=2 (scale=1 기준)
        // wSc.x/z → 바퀴 반지름(=wR), wSc.y → 바퀴 폭 절반(=wW/2)
        float wxO = bW * 0.47f;
        float wzO = bL * 0.33f;
        Quaternion wRot = Quaternion.Euler(0, 0, 90f);          // 옆으로 눕히기
        Vector3 wSc = new Vector3(wR * 2f, wW / 2f, wR * 2f);  // 타이어
        Vector3 hSc = new Vector3(wR * 1.4f, wW * 0.12f, wR * 1.4f); // 허브캡

        var wpts = new (float x, float z, string n)[]
        {
            (-wxO,  wzO, "WheelRL"), ( wxO,  wzO, "WheelRR"),
            (-wxO, -wzO, "WheelFL"), ( wxO, -wzO, "WheelFR"),
        };
        foreach (var (wx, wz, wn) in wpts)
        {
            Cyl(wn, new Vector3(wx, wR, wz), wSc, wRot, blk);
            float hx = wx < 0 ? wx - wW * 0.55f : wx + wW * 0.55f;
            Cyl(wn + "_Hub", new Vector3(hx, wR, wz), hSc, wRot, hub);
        }

        return carRoot;
    }

    IEnumerator ScaleIn(Transform t)
    {
        float dur = 0.35f, e = 0f;
        t.localScale = Vector3.zero;
        while (e < dur)
        {
            e += Time.deltaTime;
            t.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, e / dur);
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    // ── UI 헬퍼 ───────────────────────────────────────────────
    void Sep()
    {
        GUILayout.Space(3);
        Rect r = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
        Color prev = GUI.color;
        GUI.color = new Color(1, 1, 1, 0.15f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = prev;
        GUILayout.Space(3);
    }

    static Texture2D Tex(Color col)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, col);
        t.Apply();
        return t;
    }

    static GUIStyle MakeBtnStyle(Color bg)
    {
        var s = new GUIStyle(GUI.skin.button)
        {
            fontSize    = 15,
            fontStyle   = FontStyle.Bold,
            fixedHeight = 48
        };
        s.normal.background  = Tex(bg);
        s.hover.background   = Tex(bg * 1.25f);
        s.active.background  = Tex(bg * 0.70f);
        s.normal.textColor   = Color.white;
        s.hover.textColor    = Color.white;
        s.active.textColor   = Color.white;
        return s;
    }

    static GUIStyle MakeCellStyle(Color bg)
    {
        var s = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 10,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        s.normal.background = Tex(bg);
        s.hover.background  = Tex(bg * 1.20f);
        s.active.background = Tex(bg * 0.75f);
        s.normal.textColor  = Color.white;
        s.hover.textColor   = Color.white;
        s.active.textColor  = Color.white;
        return s;
    }
}
