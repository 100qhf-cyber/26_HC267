using UnityEngine;

/// <summary>
/// 씬 원클릭 자동 구성
/// 빈 GameObject에 이 컴포넌트 하나만 추가하고 Play
/// </summary>
public class MiniParkSceneSetup : MonoBehaviour
{
    void Awake()
    {
        SetupLighting();
        SetupModel();
        SetupCamera();
        SetupParkingManager();
    }

    void SetupLighting()
    {
#if UNITY_2023_1_OR_NEWER
        Light sun = FindFirstObjectByType<Light>();
#else
        Light sun = Object.FindObjectOfType<Light>();
#endif
        if (sun == null)
        {
            sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
        }
        sun.intensity          = 1.4f;
        sun.color              = new Color(1.0f, 0.98f, 0.94f);  // 맑은 낮 햇빛
        sun.shadows            = LightShadows.Soft;
        sun.shadowStrength     = 0.65f;
        sun.transform.rotation = Quaternion.Euler(55f, -35f, 0f);

        RenderSettings.ambientMode      = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight     = new Color(0.50f, 0.52f, 0.58f);
        RenderSettings.ambientIntensity = 1f;
    }

    void SetupModel()
    {
        var go = new GameObject("ParkingModel");
        go.AddComponent<MiniatureParkingLot>();
    }

    void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam    = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }
        if (cam.GetComponent<TopViewCamera>() == null)
            cam.gameObject.AddComponent<TopViewCamera>();
    }

    void SetupParkingManager()
    {
#if UNITY_2023_1_OR_NEWER
        if (FindFirstObjectByType<ParkingManager>() != null) return;
#else
        if (Object.FindObjectOfType<ParkingManager>() != null) return;
#endif
        var go = new GameObject("ParkingManager");
        go.AddComponent<ParkingManager>();
    }
}
