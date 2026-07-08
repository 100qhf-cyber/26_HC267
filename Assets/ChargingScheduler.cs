using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 정규화 가중합 기반 동적 우선순위 큐 충전 스케줄러
///
/// 좌측 하단 : 차량 현황 (배터리 바, SOC, 속도, 출차)
/// 우측 하단 : 충전 큐 패널 (1행: 현재 충전 중, 이하: 대기 순서) + 가동 버튼
///
/// 점수 = W_DIST*(1-norm_d) + W_TIME*(1-norm_r) + W_BATT*norm_b
///   - 거리 가까울수록, 출차 임박할수록, 충전량 많을수록 높은 점수
///   - 가중치 균등 (각 1/3)
///
/// 규칙
///   - 충전 중에는 새 차량이 들어와도 중단하지 않음
///   - 레일이 목표 구역에 도착한 후에만 배터리 게이지 증가
///   - 충전 완료 후 레일 위치 갱신 → 큐 재정렬 → 다음 차량으로 이동
/// </summary>
public class ChargingScheduler : MonoBehaviour
{
    // ── 차량 데이터 ──────────────────────────────────────────────
    public class CarData
    {
        public string spaceId;
        public float  entryHour;
        public float  exitHour;
        public float  speedKW;
        public float  currentKWh;
        public float  maxKWh;
        public bool   doneCharging;

        public float SocPct      => currentKWh / maxKWh * 100f;
        public float NeedKWh     => Mathf.Max(0, maxKWh - currentKWh);
        public float HoursToFull => speedKW > 0 ? NeedKWh / speedKW : 0f;
        public bool  NeedsCharge => NeedKWh > 0.5f && !doneCharging;
    }

    // ── 시뮬레이션 ───────────────────────────────────────────────
    [Header("시뮬레이션")]
    public float simSpeed  = 1200f;   // 1실초 = simSpeed 시뮬초
    public float startHour = 0.0f;

    // ── 정규화 가중합 계수 (균등) ────────────────────────────────
    const float W_DIST = 1f / 3f;
    const float W_TIME = 1f / 3f;
    const float W_BATT = 1f / 3f;

    // ── 참조 ─────────────────────────────────────────────────────
    ParkingRail         rail;
    ParkingManager      parkMgr;
    MiniatureParkingLot lot;

    // ── 상태 ─────────────────────────────────────────────────────
    bool   running = false;
    float  simHour;
    string currentlyCharging;   // 현재 충전(이동 포함) 중인 구역 ID

    public readonly List<CarData> Cars        = new List<CarData>();
    List<CarData>                 chargeQueue = new List<CarData>();

    // ParkingManager UI / OnPark에서 접근하는 프로퍼티
    public bool             IsRunning          => running;
    public string           CurrentlyCharging  => currentlyCharging;
    public List<CarData>    ChargeQueue        => chargeQueue;
    public ParkingRail      Rail               => rail;
    public float            SimHour            => simHour;

    // ── 차량 색상 풀 ─────────────────────────────────────────────
    static readonly Color[] CAR_COLORS =
    {
        new Color(0.85f,0.15f,0.15f), new Color(0.15f,0.38f,0.80f),
        new Color(0.72f,0.72f,0.72f), new Color(0.50f,0.50f,0.55f),
        new Color(0.12f,0.60f,0.28f), new Color(0.90f,0.72f,0.08f),
        new Color(0.55f,0.18f,0.65f), new Color(0.95f,0.52f,0.10f),
        new Color(0.10f,0.70f,0.72f),
    };

    static readonly Dictionary<Color, Texture2D> s_texCache =
        new Dictionary<Color, Texture2D>();

    // ─────────────────────────────────────────────────────────────
    void Start()
    {
#if UNITY_2023_1_OR_NEWER
        rail    = FindFirstObjectByType<ParkingRail>();
        parkMgr = FindFirstObjectByType<ParkingManager>();
        lot     = FindFirstObjectByType<MiniatureParkingLot>();
#else
        rail    = Object.FindObjectOfType<ParkingRail>();
        parkMgr = Object.FindObjectOfType<ParkingManager>();
        lot     = Object.FindObjectOfType<MiniatureParkingLot>();
#endif
        if (rail != null) rail.onChargingDone = OnChargingDone;
        simHour = startHour;
        StartCoroutine(SpawnAfterDelay());
    }

    System.Collections.IEnumerator SpawnAfterDelay()
    {
        float waited = 0f;
        while (parkMgr != null && !parkMgr.ModelLoaded && waited < 15f)
        {
            waited += Time.deltaTime;
            yield return null;
        }
        yield return null;
        SetupDemoCars();
    }

    void SetupDemoCars()
    {
        // 시뮬레이션 시작 00:00 기준, 출차 8~20h
        // 11kW = AC 3상 일반 / 22kW = AC 3상 고속 / 50kW = DC 급속
        var data = new[]
        {
            new CarData { spaceId="A1", entryHour=0f, exitHour=18f, speedKW=11.0f, currentKWh=18f, maxKWh= 77f }, // 5.4h
            new CarData { spaceId="A3", entryHour=0f, exitHour=10f, speedKW=22.0f, currentKWh=12f, maxKWh= 64f }, // 2.4h
            new CarData { spaceId="A5", entryHour=0f, exitHour=14f, speedKW=11.0f, currentKWh=44f, maxKWh= 77f }, // 3.0h
            new CarData { spaceId="A7", entryHour=0f, exitHour=20f, speedKW=22.0f, currentKWh=25f, maxKWh=100f }, // 3.4h
            new CarData { spaceId="B2", entryHour=0f, exitHour= 8f, speedKW=50.0f, currentKWh= 8f, maxKWh= 77f }, // 1.4h
            new CarData { spaceId="B4", entryHour=0f, exitHour=16f, speedKW=11.0f, currentKWh=38f, maxKWh= 64f }, // 2.4h
            new CarData { spaceId="B6", entryHour=0f, exitHour=15f, speedKW=11.0f, currentKWh=18f, maxKWh= 60f }, // 3.8h
            new CarData { spaceId="C1", entryHour=0f, exitHour=12f, speedKW=11.0f, currentKWh= 8f, maxKWh= 40f }, // 2.9h
            new CarData { spaceId="C3", entryHour=0f, exitHour=17f, speedKW=22.0f, currentKWh=28f, maxKWh= 75f }, // 2.1h
        };
        int ci = 0;
        foreach (var d in data)
        {
            Cars.Add(d);
            parkMgr?.SpawnCarAt(d.spaceId, CAR_COLORS[ci++ % CAR_COLORS.Length]);
        }
    }

    // ── Update ───────────────────────────────────────────────────
    void Update()
    {
        if (!running) return;

        // 레일이 실제 충전 중일 때만 시간 진행 (이동 중·대기 중은 정지)
        if (rail != null && rail.IsCharging)
            simHour += Time.deltaTime * simSpeed / 3600f;

        // 배터리 증가: 레일이 목표 지점에 도착해 충전 중일 때만
        if (currentlyCharging != null && rail != null && rail.IsCharging)
        {
            var car = Cars.Find(c => c.spaceId == currentlyCharging);
            if (car != null && !car.doneCharging)
            {
                car.currentKWh += car.speedKW * Time.deltaTime * simSpeed / 3600f;
                if (car.currentKWh >= car.maxKWh)
                {
                    car.currentKWh   = car.maxKWh;
                    car.doneCharging = true;
                }
            }
        }

        // 출차 시각이 지난 대기 차량 자동 출차 (충전 중인 차는 OnChargingDone에서 처리)
        for (int i = Cars.Count - 1; i >= 0; i--)
        {
            var c = Cars[i];
            if (c.spaceId == currentlyCharging) continue;
            if (simHour >= c.exitHour)
            {
                string sid = c.spaceId;
                Cars.RemoveAt(i);
                chargeQueue.RemoveAll(x => x.spaceId == sid);
                parkMgr?.DepartCar(sid);
            }
        }

        // 레일 대기 중 + 충전 대상 없음 → 다음 차량 시작
        if (rail != null && rail.IsIdle && currentlyCharging == null)
            ChargeNext();
    }

    // ── 가동 시작 ────────────────────────────────────────────────
    public void StartOperation()
    {
        if (running) return;
        running = true;
        simHour = startHour;
        foreach (var c in Cars) c.doneCharging = false;
        currentlyCharging = null;
        RebuildQueue();
        ChargeNext();
    }

    public void StopOperation()
    {
        running           = false;
        currentlyCharging = null;
        chargeQueue.Clear();
    }

    // ── 정규화 가중합으로 큐 재정렬 ──────────────────────────────
    // 호출 시점: 가동 시작 / 새 차량 입차 / 충전 완료 후
    void RebuildQueue()
    {
        if (lot == null) { chargeQueue.Clear(); return; }

        Vector2 railPos    = rail != null ? rail.CurrentRailPos : Vector2.zero;
        var     candidates = Cars
            .Where(c => c.NeedsCharge && c.spaceId != currentlyCharging)
            .ToList();

        if (candidates.Count == 0) { chargeQueue.Clear(); return; }

        int n      = candidates.Count;
        var dists  = new float[n];
        var rems   = new float[n];
        var batts  = new float[n];

        for (int i = 0; i < n; i++)
        {
            var c = candidates[i];
            if (lot.SpaceMap.TryGetValue(c.spaceId, out var sp))
                dists[i] = Mathf.Abs(sp.localPos.x - railPos.x)
                          + Mathf.Abs(sp.localPos.z - railPos.y);
            rems[i]  = Mathf.Max(0.01f, c.exitHour - simHour);
            batts[i] = Mathf.Max(0.01f, c.HoursToFull);
        }

        float maxD = Mathf.Max(0.0001f, dists.Max());
        float maxR = Mathf.Max(0.0001f, rems.Max());
        float maxB = Mathf.Max(0.0001f, batts.Max());

        var scored = new List<(CarData car, float score)>();
        for (int i = 0; i < n; i++)
        {
            float nd    = dists[i] / maxD;
            float nr    = rems[i]  / maxR;
            float nb    = batts[i] / maxB;
            float score = W_DIST*(1-nd) + W_TIME*(1-nr) + W_BATT*nb;
            scored.Add((candidates[i], score));
        }

        scored.Sort((a, b) => b.score.CompareTo(a.score));
        chargeQueue = scored.Select(x => x.car).ToList();
    }

    // ── 큐 앞 차량 꺼내 충전 시작 ───────────────────────────────
    void ChargeNext()
    {
        if (chargeQueue.Count == 0)
        {
            RebuildQueue();
            if (chargeQueue.Count == 0) { running = false; return; }
        }

        var next = chargeQueue[0];
        chargeQueue.RemoveAt(0);
        currentlyCharging = next.spaceId;

        // 레일 이동 + 충전 소요 실제 시간
        float realSec = Mathf.Max(2f, next.HoursToFull * 3600f / simSpeed);
        rail?.StartCharging(next.spaceId, realSec);
    }

    // ── 새 차량 입차 — 씬 스폰 포함 (SetupDemoCars용) ──────────
    public void AddCar(CarData car, Color color)
    {
        Cars.Add(car);
        parkMgr?.SpawnCarAt(car.spaceId, color);
        if (running) RebuildQueue();
    }

    // ── 새 차량 입차 — 이미 스폰된 차량 등록만 (OnPark용) ───────
    public void RegisterCar(CarData car)
    {
        if (Cars.Exists(c => c.spaceId == car.spaceId)) return;
        Cars.Add(car);
        if (running) RebuildQueue();
    }

    // ── 출차 (외부 호출) ─────────────────────────────────────────
    public void RemoveCar(string spaceId)
    {
        Cars.RemoveAll(c => c.spaceId == spaceId);
        chargeQueue.RemoveAll(c => c.spaceId == spaceId);
        if (currentlyCharging == spaceId)
        {
            rail?.CancelCharging();
            currentlyCharging = null;
        }
    }

    // ── 충전 완료 콜백 ────────────────────────────────────────────
    void OnChargingDone(string spaceId)
    {
        var car = Cars.Find(c => c.spaceId == spaceId);
        if (car != null) car.currentKWh = car.maxKWh;

        Cars.RemoveAll(c => c.spaceId == spaceId);
        chargeQueue.RemoveAll(c => c.spaceId == spaceId);
        currentlyCharging = null;
        parkMgr?.DepartCar(spaceId);

        // 레일 위치 갱신 후 큐 재정렬
        if (running) RebuildQueue();
    }

}
