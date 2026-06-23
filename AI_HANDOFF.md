# AI Handoff

이 파일은 Claude Code와 Codex가 서로 작업 상태를 빠르게 이어받기 위한 인수인계 파일이다.
작업을 시작하는 AI는 `AGENTS.md`와 이 파일을 먼저 읽고, 작업을 마친 뒤 이 파일을 갱신한다.

## Project

- Engine: Unity 6 Universal 3D
- Project path: `C:\Users\User\project1`
- Final goal: 온라인 PvP 1인칭 3D 전투 게임
- Current phase: **2단계 완료** — 전투 기반 구조 (Health/Damage/Team) 추가
- Current network status: 실제 네트워크 구현 전. 멀티플레이 전환을 고려한 구조로 설계됨.

## Current Rules

- `AGENTS.md`를 먼저 읽는다.
- 임시방편 패치 금지.
- 수정 전 원인, 중복, 병목을 먼저 확인한다.
- 입력, 명령, 이동, 카메라, UI, 설정을 분리한다.
- 최종 목표는 서버 권한 방식의 온라인 PvP다.
- 아직 구현하지 않는 것: 공격(좌클릭), 스킬, 봇, 포탑/넥서스, 실제 네트워크.
- DebugAttackInput(F키)은 임시 테스트 도구. 3단계 공격 시스템으로 교체 예정.

## Current Structure

```
Assets/
  GameConfig.asset          ← ScriptableObject 인스턴스 (수치 설정값)
  Scenes/
    SampleScene.unity       ← Bootstrap GO 포함, Main Camera 비활성화됨
  Scripts/
    GameBootstrap.cs        ← 씬 자동 구성 (Floor, Player, Dummy, CrosshairUI 생성)
    Camera/
      FirstPersonCamera.cs  ← 마우스 룩, 앉기 카메라 높이 (로컬 플레이어 전용)
    Commands/
      PlayerCommand.cs      ← 프레임 입력 스냅샷 struct (서버 전송 단위 후보)
    Combat/
      Team.cs               ← enum { Neutral, Blue, Red }
      DamageInfo.cs         ← 피해 데이터 struct (모든 공격 소스의 공통 단위)
      CharacterStats.cs     ← 캐릭터 스탯 struct (MaxHp, Armor, 피해 계산)
      HealthComponent.cs    ← 체력 상태 MonoBehaviour + TakeDamage 단일 진입점
      DebugAttackInput.cs   ← [임시] F키 → 레이캐스트 → TakeDamage 테스트 도구
    Config/
      GameConfig.cs         ← ScriptableObject, 모든 수치 중앙 관리 (Combat 수치 추가됨)
    GameState/              ← (비어있음, 이후 단계)
    Input/
      PlayerInputReader.cs  ← Unity Input → PlayerCommand 변환
    Networking/             ← (비어있음, 이후 단계)
    Player/
      PlayerEntity.cs       ← 캐릭터 상태 (PlayerId, Velocity, IsGrounded, IsCrouching)
      FirstPersonMotor.cs   ← 이동/점프/앉기/중력 (CharacterController 제어)
      LocalPlayerController.cs ← 로컬 플레이어 전용 오케스트레이터 (입력→모터/카메라)
    UI/
      CrosshairUI.cs        ← 화면 중앙 조준점 (OnGUI, 할당 없음)
      HealthDebugUI.cs      ← [임시] 더미 HP 표시 (OnGUI, 이벤트 구독 기반)
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
PlayerEntity           ← CharacterController, PlayerEntity, FirstPersonMotor, HealthComponent(Blue)
  └── CameraRoot       ← Camera (FoV 75, MainCamera 태그), AudioListener, UniversalAdditionalCameraData
LocalPlayer            ← PlayerInputReader, FirstPersonCamera, LocalPlayerController, DebugAttackInput
DummyEnemy             ← Capsule + CapsuleCollider, HealthComponent(Red)
DummyHealthUI          ← HealthDebugUI (더미 HP 화면 표시)
CrosshairUI            ← CrosshairUI
```

## Design Notes (Combat)

- **단일 피해 진입점**: 모든 공격 소스는 `HealthComponent.TakeDamage(DamageInfo)` 하나만 호출
- **아군 공격 방지**: `TakeDamage` 내부에서 `info.SourceTeam == target.Team` 체크 (Team.Neutral은 누구나 공격 가능)
- **DamageInfo struct**: 힙 할당 없음. 나중에 서버 전송 단위로 직렬화 예정
- **HealthComponent 이벤트**: `OnHealthChanged(current, max)` — UI 갱신용. `OnDeath` — GameState/리스폰 연결용
- **DebugAttackInput**: F키 + 레이캐스트. `GetComponentInParent` 사용으로 계층 구조 변경에 강건
- **HealthDebugUI**: 이벤트 구독 기반 (폴링 없음). `Camera.main`을 Start에서 캐싱

## Last Completed

- **1단계 전체 구현 완료** (Claude Code)
  - GameConfig, PlayerCommand, PlayerInputReader
  - PlayerEntity, FirstPersonMotor, FirstPersonCamera
  - LocalPlayerController, CrosshairUI, GameBootstrap
- SampleScene에 Bootstrap GO 추가, Main Camera 비활성화
- GameConfig.asset 생성 및 Inspector 연결
- `activeInputHandler: 1 → 2` 수정 (Legacy + New Input System 둘 다 활성)
- Play 모드 동작 확인 완료
- 한글 주석/로그 → 영문 변환 (`GameBootstrap.cs`)
- 전체 .cs 파일 UTF-8 BOM 재저장 (Unity Console 한글 깨짐 방지)
- **코드 최적화 및 정리** (Claude Code)
  - `LocalPlayerController.config` 미사용 필드 제거, `GameBootstrap`의 dead assignment 제거
  - `LocalPlayerController.Start`: 1회 검증 후 실패 시 LogError + `enabled = false` (`_initialized` bool 불필요)
  - `FirstPersonCamera.Start`: `_ready` bool 유지 + 실패 시 LogError 추가 (Tick이 외부 호출이라 enabled=false 불가)
  - AGENTS.md 위반 WHAT 주석 제거 (PlayerCommand inline, CrosshairUI 방향, FirstPersonMotor/InputReader/PlayerEntity 블록)
  - `new Vector3(0f, 0f, 0f)` → `Vector3.zero` 스타일 통일
  - 전체 .cs 파일 UTF-8 BOM 재확인
- **2단계: 전투 기반 구조** (Claude Code)
  - `Combat/` 폴더: Team, DamageInfo, CharacterStats, HealthComponent, DebugAttackInput
  - `UI/HealthDebugUI`: 더미 HP 표시 (이벤트 구독, OnGUI)
  - GameConfig: Combat 수치 추가 (baseMaxHp=100, baseArmor=0, debugAttackDamage=25)
  - GameBootstrap: 플레이어에 HealthComponent(Blue) 추가, DummyEnemy(Red) 생성, DebugAttackInput 연결

## In Progress

없음.

## Next Task

AGENTS.md 장기 계획 기준으로 **3단계: 기본 공격 시스템**

- DebugAttackInput(F키) 제거 또는 비활성화
- AttackCommand struct 도입 (PlayerCommand처럼 순수 데이터)
- 좌클릭 → AttackCommand → 레이캐스트 판정 → DamageInfo → TakeDamage 흐름 구현
- 판정 로직을 별도 AttackResolver(또는 CombatSystem)에 분리 (서버 이식 고려)

## Do Not Do Yet

- 공격 구현
- 스킬 구현
- 봇 AI
- 포탑/넥서스
- 직업 시스템
- 실제 네트워크 (Netcode/Mirror/FishNet)
- 미니맵/점수판
- 복잡한 이펙트/사운드

## Known Issues

- `Failed to determine dll type` 경고 (Newtonsoft.Json, AiEditorToolsSdk) — Unity 패키지 DLL 임포터 경고, 플레이에 영향 없음. Library 삭제 후 재임포트로 해결 가능하나 지금은 방치.
- Unity AI Navigation 서명 경고 — 현재 단계에서 불필요, 방치.

## Notes For Next AI

- 작업 전 `AGENTS.md`와 `AI_HANDOFF.md`를 읽는다.
- GameConfig.asset이 Bootstrap GO의 Inspector에 연결돼 있어야 Play가 작동한다.
- `LocalPlayerController`와 `FirstPersonCamera`는 로컬 플레이어 전용. 원격 플레이어는 `PlayerEntity + FirstPersonMotor`만 가진다.
- `PlayerCommand` struct는 나중에 서버 전송 단위로 사용 예정. 무겁게 만들지 말 것.
- 수치(이동속도, 체력 등)는 반드시 `GameConfig`에서 관리한다.
- 작업 후 `Last Completed`, `In Progress`, `Next Task`, `Known Issues`를 갱신한다.
- 씬/프리팹 변경 시 어떤 오브젝트에 어떤 컴포넌트를 붙여야 하는지 기록한다.
