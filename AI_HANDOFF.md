# AI Handoff

이 파일은 Claude Code와 Codex가 서로 작업 상태를 빠르게 이어받기 위한 인수인계 파일이다.
작업을 시작하는 AI는 `AGENTS.md`와 이 파일을 먼저 읽고, 작업을 마친 뒤 이 파일을 갱신한다.

## Project

- Engine: Unity 6 Universal 3D
- Project path: `C:\Users\User\project1`
- Final goal: 온라인 PvP 1인칭 3D 전투 게임
- Current phase: **8단계 완료** — 직업/스킬 추가 전 전투/부활/최적화 기초 구조 정리
- Current network status: 실제 네트워크 구현 전. 멀티플레이 전환을 고려한 구조로 설계됨.

## Current Rules

- `AGENTS.md`를 먼저 읽는다.
- 임시방편 패치 금지.
- 수정 전 원인, 중복, 병목을 먼저 확인한다.
- 입력, 명령, 이동, 카메라, UI, 설정을 분리한다.
- 최종 목표는 서버 권한 방식의 온라인 PvP다.
- 아직 구현하지 않는 것: 스킬, 포탑/넥서스, 직업, 실제 네트워크.

## Current Structure

```
Assets/
  GameConfig.asset          ← ScriptableObject 인스턴스 (수치 설정값)
  Scenes/
    SampleScene.unity       ← Bootstrap GO 포함, Main Camera 비활성화됨
  Scripts/
    GameBootstrap.cs        ← 씬 자동 구성 (Floor, Player, Bot, CrosshairUI 생성)
    Camera/
      FirstPersonCamera.cs  ← 마우스 룩, 앉기 카메라 높이 (로컬 플레이어 전용)
    Commands/
      PlayerCommand.cs      ← 프레임 입력 스냅샷 struct (서버 전송 단위 후보)
    Combat/
      Team.cs                    ← enum { Neutral, Blue, Red }
      DamageInfo.cs              ← 피해 데이터 struct (모든 공격 소스의 공통 단위)
      CharacterStats.cs          ← 캐릭터 스탯 struct (MaxHp, Armor, 피해 계산)
      HealthComponent.cs         ← 체력 상태 MonoBehaviour + TakeDamage 단일 진입점
      AttackResolver.cs          ← static 레이캐스트 쿼리 (플레이어/봇/서버가 동일 호출)
      BasicAttackController.cs   ← 좌클릭 쿨타임 관리 + AttackResolver 드라이브
    Config/
      GameConfig.cs         ← ScriptableObject, 모든 수치 중앙 관리
    GameState/
      RespawnController.cs  ← 사망 → 딜레이 → 위치초기화/HP초기화/무적 (플레이어·봇 공통)
    Bot/
      BotController.cs      ← 최근접 적 탐색 + 직선 추격 + 근접 공격 (enemies[] 주입, DamageInfo→TakeDamage)
    Debug/
      DebugDamageInput.cs   ← [디버그 전용] K 키 → 치명타 자해 (DamageInfo→TakeDamage 경로, 출하 전 삭제)
    Input/
      PlayerInputReader.cs  ← Unity Input → PlayerCommand 변환
    Networking/             ← (비어있음, 이후 단계)
    Player/
      PlayerEntity.cs       ← 캐릭터 상태 (PlayerId, Velocity, IsGrounded, IsCrouching)
      FirstPersonMotor.cs   ← 이동/점프/앉기/중력 (CharacterController 제어) + ResetState()
      LocalPlayerController.cs ← 로컬 플레이어 전용 오케스트레이터 (입력→모터/카메라)
    UI/
      CrosshairUI.cs        ← 화면 중앙 조준점 (OnGUI, 할당 없음)
      HealthDebugUI.cs      ← [임시] 월드 공간 HP 레이블 (OnGUI, 이벤트 구독)
      PlayerHUD.cs          ← 로컬 플레이어 HP 표시 + 피격 플래시 (OnGUI, 이벤트 구독, string 캐시)
```

## Scene Setup (SampleScene)

씬에 있는 오브젝트:
- `Bootstrap` — GameBootstrap 컴포넌트, config 슬롯에 GameConfig.asset 연결됨
- `Directional Light` — 기본 조명
- `Global Volume` — URP 후처리
- `Main Camera` — **비활성화됨** (GameBootstrap이 런타임에 새 카메라 생성)

Play 시 런타임 생성:
```
Floor                  ← Plane (100×100)
PlayerEntity           ← CharacterController, PlayerEntity, FirstPersonMotor, HealthComponent(Blue), RespawnController
  └── CameraRoot       ← Camera (FoV 75, MainCamera 태그), AudioListener, UniversalAdditionalCameraData
LocalPlayer            ← PlayerInputReader, FirstPersonCamera, LocalPlayerController, BasicAttackController, DebugDamageInput
BlueBot_0              ← CharacterController, HealthComponent(Blue), RespawnController, BotController (enemies=RedTeam)
  └── Mesh             ← 파란 Capsule (CapsuleCollider 제거됨)
BlueBot_1              ← (동일 구조)
RedBot_0               ← CharacterController, HealthComponent(Red), RespawnController, BotController (enemies=BlueTeam)
  └── Mesh             ← 빨간 Capsule (CapsuleCollider 제거됨)
RedBot_1, RedBot_2     ← (동일 구조)
BlueBot_0_UI ~ RedBot_2_UI ← HealthDebugUI × 5 (봇별 월드 HP 레이블)
PlayerHUD              ← PlayerHUD (화면 좌하단 HP + 피격 플래시)
CrosshairUI            ← CrosshairUI
```

팀 구성: Blue = [PlayerEntity, BlueBot_0, BlueBot_1], Red = [RedBot_0, RedBot_1, RedBot_2]
스폰: Blue at z=-5 (spread X=3), Red at z=+5 (spread X=3)

## Design Notes (Combat)

- **단일 피해 진입점**: 모든 공격 소스는 `HealthComponent.TakeDamage(DamageInfo)` 하나만 호출
- **아군 공격 방지**: `TakeDamage` 내부에서 `info.SourceTeam == target.Team` 체크 (Team.Neutral은 누구나 공격 가능)
- **DamageInfo struct**: 힙 할당 없음. 나중에 서버 전송 단위로 직렬화 예정
- **HealthComponent 이벤트**: `OnHealthChanged(current, max)` — UI 갱신용. `OnDeath` — GameState/리스폰 연결용
- **AttackResolver**: `static bool TryHit(origin, dir, range, attackerTeam, layerMask, out target)` — 순수 물리 쿼리, 상태 없음. `layerMask + QueryTriggerInteraction.Ignore` 적용. 봇/서버 동일 호출 가능
- **attackLayerMask**: `GameConfig.attackLayerMask = -1` (Everything). Inspector에서 트리거/UI 레이어 제외 가능
- **RespawnController**: `OnDeath` 구독 → `RespawnRoutine()` 코루틴 → CC disable/위치 이동/CC enable → `_motor?.ResetState()` → `Reinitialize()` → 무적 타이머. 플레이어·봇 모두 동일 컴포넌트 사용. `_motor`는 플레이어만 non-null
- **FirstPersonMotor.ResetState()**: `_verticalVelocity = 0, _entity.IsGrounded = false`. RespawnController가 텔레포트 직후 호출해 낙하 중 사망 시 잔류 속도를 제거
- **BotController**: `enemies: HealthComponent[]` 배열(부트스트랩 주입). `FindNearestAliveEnemy()` — O(N) 고정 소형 배열, sqrMagnitude로 sqrt 회피. Update()의 추격/공격 범위 비교도 `_sqDetectRange/_sqAttackRange`(Start에서 제곱 캐시)로 sqrt 없이 처리
- **debugCombatLogs**: `GameConfig.debugCombatLogs = false` 기본값. BasicAttackController 명중 로그, BotController 공격 로그를 이 플래그로 제어. 기본 false → 콘솔 스팸 없음
- **BasicAttackController 안전 보장**: Start()에서 `attackerHealth == null` 이면 LogError + `enabled = false`. Tick()에서 `attackerHealth.IsDead` 이면 skip. `Team.Neutral` fallback 제거 — null은 명시적 오류로 처리
- **3v3 팀 빌드**: GameBootstrap.WireEnemyLists() — 생성 후 일괄 적팀 배열 주입. 블루봇→enemies=RedTeam[], 레드봇→enemies=BlueTeam[]. 플레이어도 BlueTeam에 포함되어 레드봇의 타겟이 됨
- **사망 중 입력 처리**: `LocalPlayerController._isDead` — 카메라는 유지(죽어서도 시점 유지), Motor/Attack만 잠금
- **무적 상태**: `HealthComponent.IsInvulnerable` — `TakeDamage` 최상단 가드. `SetInvulnerable(bool)` 으로 RespawnController가 제어
- **Reinitialize**: HP 복구 + IsDead = false + IsInvulnerable = false + OnRespawned 이벤트 발행

## Last Completed

- **8단계: 전투/부활/최적화 기초 구조 정리** (Claude Code)
  - `BasicAttackController.cs`: `attackerHealth` null 검증 강화(Start에서 LogError+disable), `IsDead` guard(Tick 상단), `Team.Neutral` fallback 제거, `attackLayerMask` 파라미터 전달, `debugCombatLogs` 로그 게이팅
  - `AttackResolver.cs`: `TryHit` 시그니처에 `LayerMask layerMask` 추가, `Physics.Raycast` → `QueryTriggerInteraction.Ignore` 적용
  - `FirstPersonMotor.cs`: `public void ResetState()` 추가 (`_verticalVelocity=0, IsGrounded=false`)
  - `RespawnController.cs`: `_motor: FirstPersonMotor` Start에서 캐시, 텔레포트 후 `_motor?.ResetState()` 호출 (`using Game.Player` 추가)
  - `BotController.cs`: `_sqDetectRange/_sqAttackRange` Start에서 제곱값 캐시, Update() `Vector3.Distance(sqrt)` → `sqrMagnitude` 비교로 교체, `debugCombatLogs` 로그 게이팅
  - `GameConfig.cs`: `attackLayerMask: LayerMask = -1` (Everything 기본), `debugCombatLogs: bool = false` 추가
- **8단계 추가 최적화** (Claude Code)
  - `UI/HealthDebugUI.cs`: `_hpText` 캐시 추가 — `HandleHealthChanged`에서만 재빌드, `OnGUI`에서 문자열 보간 제거 (`PlayerHUD`와 동일 패턴)
  - `Combat/HealthComponent.cs`: 사망 `Debug.Log`를 `#if UNITY_EDITOR`로 감쌈 — 빌드에서 string 할당 없음, GameConfig 의존성 없음
- **7단계: 플레이어 HUD** (Claude Code)
  - `UI/PlayerHUD.cs`: OnHealthChanged 구독, _hpText 캐시, 피격 플래시
  - `Config/GameConfig.cs`: `damageFlashDuration = 0.25f` 추가
  - `GameBootstrap.cs`: CreatePlayer() 끝에 PlayerHUD GO 추가
- **6단계: 3v3 팀 구조** (Claude Code)
  - `Bot/BotController.cs`, `Config/GameConfig.cs`, `GameBootstrap.cs`
- **5단계: 단순 봇** / **4단계: GameState** / **3단계: 기본 공격** / **2단계: 전투 기반 구조** / **1단계: 이동·카메라** — 완료

## In Progress

없음.

## Next Task

로드맵 기준 **9단계: 직업 구조** 또는 **UI 심화**

**옵션 A — 직업 구조**: `CharacterClass` ScriptableObject (Warrior/Archer 분리 스탯), GameBootstrap에서 팀별 직업 배치
**옵션 B — UI 심화**: HP 바(uGUI), 사망/부활 카운트다운, 킬/데스 카운트

## Do Not Do Yet

- 스킬 구현
- 포탑/넥서스
- 실제 네트워크 (Netcode/Mirror/FishNet)
- 미니맵/점수판
- 복잡한 이펙트/사운드

## Known Issues

- `Failed to determine dll type` 경고 (Newtonsoft.Json, AiEditorToolsSdk) — Unity 패키지 DLL 임포터 경고, 플레이에 영향 없음.
- Unity AI Navigation 서명 경고 — 현재 단계에서 불필요, 방치.

## Notes For Next AI

- 작업 전 `AGENTS.md`와 `AI_HANDOFF.md`를 읽는다.
- GameConfig.asset이 Bootstrap GO의 Inspector에 연결돼 있어야 Play가 작동한다.
- `attackLayerMask`는 기본값 -1(Everything). 프로젝트에 레이어가 추가되면 Inspector에서 Combat 오브젝트 레이어만 선택하도록 좁혀야 한다.
- `debugCombatLogs`는 기본 false. 전투 로그를 보려면 GameConfig.asset Inspector에서 체크.
- `AttackResolver.TryHit` 시그니처: `(origin, direction, range, attackerTeam, layerMask, out target)` — 호출 시 layerMask 필수.
- `LocalPlayerController`와 `FirstPersonCamera`는 로컬 플레이어 전용. 원격 플레이어는 `PlayerEntity + FirstPersonMotor`만 가진다.
- `PlayerCommand` struct는 나중에 서버 전송 단위로 사용 예정. 무겁게 만들지 말 것.
- 수치(이동속도, 체력 등)는 반드시 `GameConfig`에서 관리한다.
- 작업 후 `Last Completed`, `In Progress`, `Next Task`, `Known Issues`를 갱신한다.