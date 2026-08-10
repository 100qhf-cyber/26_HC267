# 26_HC267

# 디지털 트윈 기반 로봇 레일 전기차 충전 시스템

##사용한 기술
- Python
- Aduiono

# ⚡ 백만볼트 — 자동 무선 EV 충전 시스템

> 한이음 드림업(Hanium Dream-Up) 팀 프로젝트
> XY·Z축 레일 구동 + 무선전력전송 + 디지털 트윈으로 구현한 **주차 구역 기반 자동 무선 충전 시스템**

---

## 📌 프로젝트 개요

주차장에 정차한 전기차를 향해 충전 코일이 **스스로 이동·정렬**하여 무선으로 전력을 전송하는 시스템입니다.
사용자가 앱에서 주차 구역과 출차 시간을 입력하면, XY 레일이 해당 구역으로 코일을 이송하고 Z축이 상승해 차량 하부 코일과 정렬한 뒤, INA219 전력 측정을 기반으로 미세 정렬하여 **최대 전력점에서 충전**합니다.

- **타깃 유스케이스**: 택시 차고지 / 주차장 등 정차 위치가 정형화된 환경
- **핵심 가치**: 케이블 없는 자동 충전, 실시간 전력 모니터링, 디지털 트윈 연동

---

## 🏗️ 시스템 구성

시스템은 4개 파트로 분리되어 동작합니다.

| 파트 | 역할 | 주요 하드웨어 |
|------|------|--------------|
| ① 레일 이동 (XY) | 스텝수 기반 구역 이동, 리미트 스위치 호밍, 오도착 시 원점 복귀 | OrangeBoard, DFRobot Stepper Shield, 리미트 스위치 4개 |
| ② Z축 승강 | 지정 높이 상승 후 XY 미세조정 | Arduino Nano, A4988(KS0152), T8 리드스크류 |
| ③ 송신부 + 릴레이 | Z축 상승 완료 시 릴레이로 무선충전 송신부 ON | SZH-EK378, 릴레이 모듈 |
| ④ 수신부 | INA219 전력 측정 → 블루투스 → PC → Unity 그래프 | INA219, T3168(RX) |

---

## 🔧 하드웨어 상세

### 레일 이동 제어 (XY축)
- DFRobot Dual Bipolar Stepper Shield 사용
  - X축: STEP=D6, DIR=D7, EN=D8
  - Y축: STEP=D5, DIR=D4, EN=D12
- 리미트 스위치 4개 (INPUT_PULLUP, 눌림 시 LOW)
  - X원점(MIN)=D9 / X끝(MAX)=D11 / Y원점(MIN)=D10 / Y끝(MAX)=D3
- 1/8 마이크로스테핑, 1,600 steps/rev, GT2 타이밍 벨트
- 스텝 펄스 반주기 500µs

> **용어 풀이**
> - **STEP**: 스텝 펄스 핀. HIGH→LOW 펄스 1회당 모터가 1스텝 회전
> - **DIR** (Direction, 방향): HIGH/LOW 값으로 모터 회전 방향(정방향/역방향)을 지정
> - **EN** (Enable, 활성화): 모터 드라이버 On/Off 핀. 이 실드는 **LOW일 때 구동, HIGH일 때 정지(비활성)**
> - **마이크로스테핑**: 1스텝을 잘게 쪼개 정밀도를 높이는 방식. 1/8이면 한 스텝을 8등분
> - **INPUT_PULLUP**: 입력 핀 내부에 풀업 저항을 연결해 평소 HIGH를 유지하다, 스위치가 눌리면 LOW로 떨어지게 하는 설정

### Z축 승강 기구
- T8 리드스크류 + 스텝모터 + 가이드봉 2개 + 개방형 아크릴 코일 상판 구조로 **자작**
- 코일 고정 상판·바닥판은 직접 모델링 후 3D 프린팅 (Bambu Lab P1S, PETG HF)
- Z축 최하단 리미트 스위치로 원점 보장

### 전력 측정 (수신부)
- INA219를 I2C로 연결 (SDA=A4, SCL=A5)
- 코일 정렬 피드백 및 최대 전력점 탐색에 활용

> **용어 풀이**
> - **INA219**: 전압·전류·전력을 측정하는 센서 IC
> - **I2C**: 2가닥(SDA=데이터, SCL=클럭) 선으로 여러 장치와 통신하는 시리얼 프로토콜

### 무선 통신
- HC-05(master) / HC-06(slave) 블루투스 모듈, AT 커맨드로 SPP 설정
- 수신부 나노 결선: D2→모듈 TXD, D3→모듈 RXD → `SoftwareSerial(2,3)`
- 프로토콜: `'m'` 전력 고속 스트리밍 시작 / `'s'` 중지 / `'R'` 단발 쿼리

> **용어 풀이**
> - **SPP** (Serial Port Profile): 블루투스를 시리얼 통신처럼 쓰게 해주는 규격
> - **AT 커맨드**: 모듈 설정을 바꾸는 명령어 (이름·통신속도·페어링 등)
> - **TXD/RXD**: 송신(Transmit)/수신(Receive) 핀. 한쪽 TXD는 반대쪽 RXD와 교차 연결

### 무선 충전
- 송신부 SZH-EK378 (12V 입력, 5V/3A 출력) + 릴레이 스위칭
- 과결합(frequency splitting) 현상 분석 — 코일 사이 박막 삽입 시 전력 증가 원인 규명
- 코일 강화 방안 검토: 페라이트 시트, 리츠선, 리피터 코일

---

## 💻 핵심 코드

### 1) 스텝 펄스 발생

모터를 1스텝 움직이는 최소 단위 함수입니다. STEP 핀에 HIGH→LOW 펄스를 한 번 주고, 사이사이 500µs 지연을 넣어 펄스 폭을 확보합니다.

```cpp
void stepOnce(int stepPin) {
  digitalWrite(stepPin, HIGH);
  delayMicroseconds(STEP_HALF_US);   // STEP_HALF_US = 500 (펄스 반주기, µs)
  digitalWrite(stepPin, LOW);
  delayMicroseconds(STEP_HALF_US);
}
```

> 이 함수를 몇 번 호출하느냐가 곧 **이동 스텝수**가 되며, 40 steps/mm 환산으로 실제 이동 거리를 결정합니다.

### 2) 축 구조체로 X·Y 통합 관리

X축·Y축을 같은 로직으로 다루기 위해 핀·스위치·방향값을 하나의 구조체로 묶었습니다. 덕분에 캘리브레이션 함수 하나로 두 축을 모두 처리할 수 있습니다.

```cpp
struct Axis {
  int stepPin, dirPin, minSw, maxSw;   // STEP핀, DIR핀, 원점(MIN)스위치, 끝점(MAX)스위치
  bool dirMinus, dirPlus;              // (-)방향/(+)방향에 해당하는 DIR 값
  const char* label;                   // 로그 출력용 이름("X"/"Y")
};

// dirMinus=HIGH, dirPlus=LOW → 원점 방향은 DIR=HIGH, 끝점 방향은 DIR=LOW
Axis xAxis = {X_STEP, X_DIR, X_MIN_SW, X_MAX_SW, HIGH, LOW, "X"};
Axis yAxis = {Y_STEP, Y_DIR, Y_MIN_SW, Y_MAX_SW, HIGH, LOW, "Y"};
```

### 3) 비상 정지

측정 도중 언제든 시리얼로 `s`를 보내면 모든 모터 드라이버를 비활성(EN=HIGH)하고 무한 대기해 즉시 정지합니다. 모든 이동 루프 안에서 반복 호출됩니다.

```cpp
void checkStop() {
  if (Serial.available()) {
    if (tolower(Serial.read()) == 's') {
      Serial.println(F("\n!! 비상 정지 !!"));
      digitalWrite(X_EN, HIGH);   // EN=HIGH → 드라이버 비활성(정지)
      digitalWrite(Y_EN, HIGH);
      while (true) delay(1000);   // 재부팅 전까지 정지 유지
    }
  }
}
```

### 4) 캘리브레이션 — 최대 스텝수 측정 + 방향 자동 보정

한 축의 원점→끝점 스텝수를 측정하는 핵심 함수입니다. 원점을 찾다가 **반대편 스위치가 먼저 눌리면 배선·극성이 뒤집힌 것으로 보고 DIR을 자동 반전**합니다.

```cpp
long calibrateAxis(Axis &ax) {
  // (1) 원점(-) 방향으로 이동하며 MIN 스위치 탐색
  digitalWrite(ax.dirPin, ax.dirMinus);
  while (digitalRead(ax.minSw) != LOW) {     // MIN이 눌릴 때까지(LOW) 진행
    checkStop();
    if (digitalRead(ax.maxSw) == LOW) {      // 반대편(MAX)이 먼저 눌리면 방향이 반대
      bool tmp = ax.dirMinus;                // → DIR 값 서로 교환(자동 반전)
      ax.dirMinus = ax.dirPlus; ax.dirPlus = tmp;
      digitalWrite(ax.dirPin, ax.dirMinus);
      while (digitalRead(ax.maxSw) == LOW) { checkStop(); stepOnce(ax.stepPin); }
      for (int i = 0; i < BACKOFF_STEPS; i++){ checkStop(); stepOnce(ax.stepPin); } // 스위치 이탈
      continue;
    }
    stepOnce(ax.stepPin);
  }

  // (2) 끝점(+) 방향으로 이동하며 스텝수 카운트
  digitalWrite(ax.dirPin, ax.dirPlus);
  while (digitalRead(ax.minSw) == LOW) { checkStop(); stepOnce(ax.stepPin); } // MIN 이탈
  long steps = 0;
  while (digitalRead(ax.maxSw) != LOW) {     // MAX가 눌릴 때까지 세며 이동
    checkStop(); stepOnce(ax.stepPin); steps++;
  }

  // (3) MAX 스위치에서 200스텝 백오프 → 다음 사이클이 스위치 위에서 끝나는 것 방지
  digitalWrite(ax.dirPin, ax.dirMinus);
  for (int i = 0; i < BACKOFF_STEPS; i++){ checkStop(); stepOnce(ax.stepPin); }
  return steps;   // 이 축의 총 이동 스텝수 반환
}
```

> **BACKOFF_STEPS = 200**: 스위치가 눌린 지점에서 바로 멈추면 다음 사이클 시작 판정이 꼬이므로, 스위치를 벗어나는 여유 스텝을 둡니다.

### 5) 3사이클 평균 + 결과 출력

측정 오차를 줄이기 위해 X·Y를 **3사이클(`RUNS=3`) 왕복**한 뒤 평균을 내고, 이동 코드에 그대로 붙여넣을 수 있는 상수 형태로 출력합니다.

```cpp
for (int run = 0; run < RUNS; run++) {         // RUNS = 3
  xMaxArr[run] = calibrateAxis(xAxis);
  yMaxArr[run] = calibrateAxis(yAxis);
}

long xSum = 0, ySum = 0;
for (int i = 0; i < RUNS; i++) { xSum += xMaxArr[i]; ySum += yMaxArr[i]; }

// 평균 스텝수를 상수 선언문으로 그대로 출력 (복사해서 이동 코드에 사용)
Serial.print(F("long X_MAX_STEPS = ")); Serial.print(xSum / RUNS); Serial.println(F(";"));
Serial.print(F("long Y_MAX_STEPS = ")); Serial.print(ySum / RUNS); Serial.println(F(";"));
```

---

## 📐 XY 캘리브레이션 요약

리미트 스위치를 이용해 각 축의 **최대 이동 스텝수**를 자동 측정하고, 이를 실거리(mm)로 환산해 주차 구역 좌표를 매핑합니다.

### 시리얼 명령어
| 입력 | 동작 |
|------|------|
| `x` / `y` | 먼저 측정할 축 선택 |
| `m` | 측정 시작 |
| `s` | 비상 정지 (모터 EN OFF 후 정지) |

### 동작 순서 (축당 1사이클)
1. **원점(−) 탐색** — MIN 스위치가 눌릴 때까지 이동
2. **끝점(+) 탐색** — MIN을 벗어난 뒤 MAX 스위치까지 이동하며 스텝 카운트
3. **백오프** — MAX 스위치에서 200스텝 물러나 스위치 위에서 사이클이 끝나는 것을 방지

### 결과 산출
- X·Y 각각 3사이클 왕복 후 스텝수 평균 계산
- **40 steps/mm** 기준으로 mm 환산
- `X_MAX_STEPS` / `Y_MAX_STEPS` 상수를 출력해 이동 코드에 반영

### 실측값
| 축 | 전체 길이 | 중앙값 이동 |
|----|-----------|-------------|
| X | 86 mm | 77 mm |
| Y | 89 mm | 78 mm |

---

## 🎯 정밀 정렬 알고리즘

송신부에 전력을 계속 투입한 상태에서 한 축을 스캔하며 INA219 전력 피크 스텝을 탐색 후 복귀합니다.

- **OrangeBoard**: 모터 이동 제어
- **Arduino Nano**: INA219 전력값을 블루투스로 전송
- **PC (Python)**: 두 COM 포트를 취합 → 피크 스텝 저장 → 복귀 지휘

---

## 🖥️ 디지털 트윈 (Unity)

- 주차장 모형: 1.2m × 1.2m 아크릴, 24구역 (6×4 그리드), 차량 주행로 반영
- INA219 데이터를 실시간 그래프로 시각화하여 **최대 전력점 확인**
- `SerialManager.cs` 브리지로 하드웨어↔Unity 연동, MQTT 통신 계층 도입

> **용어 풀이**
> - **디지털 트윈**: 실제 하드웨어를 가상 공간에 실시간으로 복제한 모델
> - **MQTT**: 센서·기기 간 경량 메시지 통신 프로토콜(IoT에서 널리 사용)

---

## 📱 모바일 앱

- 주차 구역 입력 → 좌표 인식 → 스텝수 계산으로 자동 이송
- 차량 정보 및 출차 시간 입력 화면 제공

---

## 📂 문서

- 제작설계서 (H/W 서브시스템 · 서비스 흐름도 · 화면 설계서 포함)
- 배선도, Z축 도면
- 프로젝트 소개 영상 (1분 30초, 자막 기반)

---

## 🚧 개발 현황

- [x] XY 레일 하드웨어 및 캘리브레이션 (스텝수 자동 측정 + 방향 자동 보정)
- [x] INA219 전력 측정 및 블루투스 스트리밍
- [x] 무선충전 송수신부 릴레이 스위칭
- [x] 정밀 정렬 알고리즘 (단일 축)
- [ ] 조그(jog) 모드 등 수동 제어 기능 통합
- [ ] Z축 기구 최종 조립 및 통합
- [ ] XY·Z축 동시 구동 (스텝모터 수량 확보 후)
- [ ] Unity 디지털 트윈 최종 연동
- [ ] 전원부 소형화

---

## 👥 팀

한이음 드림업 프로젝트 팀 **백만볼트**
