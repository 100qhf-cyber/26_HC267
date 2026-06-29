using UnityEngine;

/// <summary>
/// 주차장 모형 카메라 컨트롤러 — 탑뷰 / 쿼터뷰 전환 가능
///
///  Tab          : 탑뷰 ↔ 쿼터뷰 전환
///  우클릭 드래그 : 화면 패닝
///  스크롤 휠     : 줌 인/아웃
///  Q / E        : 쿼터뷰에서 좌/우 회전
///  F            : 모형 전체 화면 맞추기
///  R            : 카메라 초기화
/// </summary>
[RequireComponent(typeof(Camera))]
public class TopViewCamera : MonoBehaviour
{
    [Header("탑뷰")]
    public float topHeight   = 1.10f;   // 탑뷰 카메라 높이
    public float topFOV      = 50f;

    [Header("쿼터뷰")]
    public float qPitch      = 52f;     // 수직 각도
    public float qYaw        = 45f;     // 초기 수평 각도 (대각선)
    public float qDistance   = 1.20f;   // 초기 거리
    public float rotSpeed    = 80f;     // Q/E 회전 속도 (도/초)

    [Header("공통")]
    public float zoomMin     = 0.20f;
    public float zoomMax     = 3.00f;
    public float zoomSpeed   = 0.20f;
    public float panSmooth   = 14f;

    [Header("타겟 (비우면 자동 탐색)")]
    public MiniatureParkingLot target;

    // ── 내부 상태 ──
    private bool    topMode = false;    // 기본 쿼터뷰
    private float   zoom,   tZoom;
    private float   yaw,    tYaw;
    private Vector3 focus,  tFocus;

    private Vector3 dragWorld;
    private Vector3 focusOnDrag;
    private Camera  cam;

    static readonly Vector3 DEFAULT_CENTER = new Vector3(0.45f, 0.01f, 0.45f);

    void Start()
    {
        cam = GetComponent<Camera>();
        if (target == null)
#if UNITY_2023_1_OR_NEWER
            target = FindFirstObjectByType<MiniatureParkingLot>();
#else
            target = Object.FindObjectOfType<MiniatureParkingLot>();
#endif
        ResetCamera();
    }

    [ContextMenu("카메라 초기화")]
    public void ResetCamera()
    {
        topMode = false;
        zoom = tZoom = qDistance;
        yaw  = tYaw  = qYaw;
        focus = tFocus = GetCenter();

        cam.fieldOfView   = topMode ? topFOV : 50f;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane  = 10f;
        cam.backgroundColor = new Color(0.15f, 0.17f, 0.20f);
        cam.clearFlags      = CameraClearFlags.SolidColor;

        Apply();
    }

    void Update()
    {
        Keys();
        Zoom();
        Pan();

        // 보간
        float t = Time.deltaTime * panSmooth;
        zoom  = Mathf.Lerp(zoom,  tZoom, t);
        yaw   = Mathf.LerpAngle(yaw, tYaw, t);
        focus = Vector3.Lerp(focus, tFocus, t);

        Apply();
    }

    void Keys()
    {
        if (Input.GetKeyDown(KeyCode.Tab)) ToggleMode();
        if (Input.GetKeyDown(KeyCode.R))   ResetCamera();
        if (Input.GetKeyDown(KeyCode.F))   FocusAll();

        if (!topMode)
        {
            if (Input.GetKey(KeyCode.Q)) tYaw -= rotSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.E)) tYaw += rotSpeed * Time.deltaTime;
        }
    }

    void Zoom()
    {
        float s = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(s) < 0.001f) return;
        tZoom = Mathf.Clamp(tZoom - s * zoomSpeed * zoom, zoomMin, zoomMax);
    }

    void Pan()
    {
        if (Input.GetMouseButtonDown(1))
        {
            dragWorld   = RayToGround(Input.mousePosition);
            focusOnDrag = focus;
        }
        if (Input.GetMouseButton(1))
        {
            Vector3 cur = RayToGround(Input.mousePosition);
            tFocus = focusOnDrag + (dragWorld - cur);
        }
    }

    void Apply()
    {
        if (topMode)
        {
            transform.position = focus + Vector3.up * zoom;
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
        else
        {
            var rot = Quaternion.Euler(qPitch, yaw, 0f);
            transform.position = focus + rot * (Vector3.back * zoom);
            transform.rotation = rot;
        }
    }

    void ToggleMode()
    {
        topMode = !topMode;
        if (topMode)
        {
            tZoom = topHeight;
            tYaw  = 0f;
        }
        else
        {
            tZoom = qDistance;
            tYaw  = qYaw;
        }
    }

    void FocusAll()
    {
        tFocus = GetCenter();
        tZoom  = topMode ? topHeight : qDistance;
    }

    Vector3 GetCenter() =>
        target != null ? target.LotCenter : DEFAULT_CENTER;

    Vector3 RayToGround(Vector3 screenPos)
    {
        var ray = cam.ScreenPointToRay(screenPos);
        float groundY = target != null ? target.transform.position.y : 0f;
        float t = ray.direction.y == 0f ? 100f : (groundY - ray.origin.y) / ray.direction.y;
        if (t < 0f) t = 100f;
        return ray.origin + ray.direction * t;
    }
}
