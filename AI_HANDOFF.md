# AI Handoff

이 파일은 Claude Code와 Codex가 서로 작업 상태를 빠르게 이어받기 위한 인수인계 파일이다.
작업을 시작하는 AI는 `AGENTS.md`와 이 파일을 먼저 읽고, 작업을 마친 뒤 이 파일을 갱신한다.

## Project

- Engine: Unity 6 Universal 3D
- Project path: `C:\Users\User\project1`
- Final goal: 온라인 PvP 1인칭 3D 전투 게임
- Current phase: **53 완료** — R 스킬 3종 위치/상태 기준 수정. 전사R: 바닥 기준 상승·낙하 + 시전 중 유닛 통과. 도적R: 은신 해제 조건 → 백어택 성공 시만(피격/스킬 사용으로 해제 안 됨). 마법사R: 유성 낙하 중심·경고 위치를 바닥 기준 y로 보정. 이전: **52 완료** — 쿨타임 미적용 4종 수정: 전사Q/R asset 0→정상값, 도적E1 무한발사 방지(ProjectileFlying 상태), 도적E1 방향 3D화, 궁수Q 소비시 쿨타임. 이전: **51.1 완료** — HealthComponent self move speed buff를 source-slotted 구조로 리팩터링. RC×Overdrive 동시 활성 시 곱연산(×1.3×1.5=×1.95) 보장. 이전: **51단계 완료** — Archer R 궁극기 Overdrive 구현. R 발동 시 이동속도 ×1.5(10s), 공격속도 ×2.0(RC와 곱연산). Basic 탄이 OverdriveBasic으로 업그레이드되어 충돌 시 AoE 폭발(반경 2m, 피해 5). 폭발 보상(F게이지+Z쿨감)은 폭발당 1회만. 사망 시 이동속도 버프 정리. AbilityDebugUI R 슬롯: OVR X.Xs(주황)/쿨타임/READY. 이전: **50.1 완료** — Archer Basic 적중 시 Z 쿨타임 0.1s 감소. OnArcherBasicHit(float)가 F 게이지 증가와 Z 쿨감을 하나의 콜백으로 통합. BasicAttackController에서 shotType이 Basic이면 _onBasicHitCallback(OnArcherBasicHit), Q/enhanced면 _addFGaugeCallback(AddArcherFGauge)으로 분기. ReduceCooldown(AbilitySlot, float) AbilityController에 추가. 이전: **50단계 완료** — Archer Z 구르기(Roll) 구현. 입력 방향(WASD) 기준 수평 구르기. 현재 입력 없으면 마지막 이동 입력 방향, 그것도 없으면 뒤로. 8s 쿨타임, 거리 4m, 0.25s 지속. 구르기 성공 시 F 게이지 +10. StartDash(velocity, duration) 재사용으로 유닛 통과 자동 처리. 이전: **49.1 버그픽스 완료** — Archer F 버그 3개 수정 + 시작 게이지 100%. ① TickTimers에서 스턴/사망 시 F 즉시 종료(TickHeldInputs 미호출 케이스 대비). ② Barrage 탄환: TryConsumeShieldFrom을 damage+slow 앞에 호출해 보호막이 공격 이벤트 전체를 차단. ③ Basic탄/Q강화탄: HP 비교로 게이지 충전 판정(무적 대상 제외). ④ Init()에서 _fGauge = config.archerFMaxGauge. 이전: 49단계 완료 — Archer F 게이지형 난사(Barrage Gauge). 기본 공격 적중 +2, Q 강화탄 폭발 적중 +3, F 꾹 누르면 20/s 소모하며 0.1s마다 난사탄 발사(피해 3, 둔화 0.8x 0.4s). F 사용 중 이동속도 80%, 기본 공격 차단. 사망해도 게이지 유지. 이전: 48단계 완료 — Archer Q 구조 정리 + 보호막-강화탄 공격 이벤트 단위 차단. HealthComponent.TryConsumeShieldFrom(Team) 추가 — 강화탄 폭발의 피해+CC를 하나의 공격 이벤트로 묶어 보호막 1회 소모로 전부 차단. ExplodeAt() dedup 이후 damage 이전에 호출. TryBeginQ()에 no-cooldown 의도 명시 (공용 쿨타임 배열 미사용). 이전: 47단계 — Archer Q 강화탄 선택(Shock/Fire/Ice 순환). 이전: 46단계 — Archer 구조 최적화 4개 수정: ① 투사체 사거리 끝 충돌 누락 수정(step=Min(dist,range)), ② 투사체 오브젝트 풀링(s_pool, cap32), ③ SelfMoveSpeedMultiplier buff/penalty 2-레이어 분리, ④ CC 함수에 sourceTeam 추가로 보호막이 적 CC만 차단. 이전: 45단계 — Archer E 선사용 보호막(2.5s, cd14s). 적의 다음 1회 피해 또는 CC(스턴/넉백/슬로우/띄우기) 차단 후 즉시 소멸. HealthComponent에 `_shieldActive` + `ConsumeShield()` + `OnShieldConsumed` 이벤트로 중앙화. 시각: 반투명 cyan 구체 자식 GO (toggle). 이전: 44단계 Archer 투사체 Capsule 블릿 시각 + RC 래피드파이어 버프. 43단계 Archer 기본 투사체. 42단계 Mage 패시브+큰 화염구 스턴/넉백. 41단계 Mage R Meteor Judgment. 40단계 Mage Z Arcane Bolt. 39단계 Mage F 레이저. 38단계 Mage E 블랙홀. 37단계 Mage 화염구. 36단계 Mage RC 텔레포트. 35단계 Warrior R 상승/낙하 이동. 34단계 Warrior R 거검 강림. 33단계 Warrior F 난도질. 32단계 Warrior Z 파동 방향 수정. 31단계 Warrior Z 반격 후퇴기. 30단계 DamageTakenMultiplier. 29단계 Rogue Q 장판. 24단계 Rogue R 은신/백어택.
- Current network status: 실제 네트워크 구현 전. 멀티플레이 전환을 고려한 구조로 설계됨.

## Current Rules

- `AGENTS.md`를 먼저 읽는다.
- 임시방편 패치 금지.
- 수정 전 원인, 중복, 병목을 먼저 확인한다.
- 입력, 명령, 이동, 카메라, UI, 설정을 분리한다.
- 최종 목표는 서버 권한 방식의 온라인 PvP다.
- 아직 구현하지 않는 것: Warrior R 궁극기 게이지 시스템, Mage/Archer 실제 스킬 효과, 포탑/넥서스, 실제 네트워크.

## Current Structure

```
Assets/
  GameConfig.asset
  Scripts/
    GameBootstrap.cs
    Camera/FirstPersonCamera.cs
    Commands/PlayerCommand.cs
    Combat/
      Team.cs / CombatClass.cs / ClassAttackData.cs / ClassAbilityConfig.cs
      AbilitySlot.cs                   ← Z=5, Count=6
      StatusEffectMask.cs              ← [Flags] enum (Stunned)
      AbilityController.cs            ← 공통 쿨타임·대시·라우팅만. Rogue/Warrior면 전용 AbilityHandler 생성
      RogueAbilityHandler.cs          ← Rogue Q/E/Z/RC/R/F 전부. OnDashEnded, HandleOwnerDeath, ForceCleanup
      RogueDimensionalRift.cs
      RogueMarkedShurikenProjectile.cs ← Init()에 RogueAbilityHandler 참조
      RogueStunBomb.cs                 ← Z 스턴 폭탄 (Init() 패턴)
      DamageInfo.cs                   ← IgnoreArmor bool 추가  ★ 갱신
      CharacterStats.cs
      HealthComponent.cs               ← OnDamaged 이벤트 추가; TakeDamage에서 IgnoreArmor 처리  ★ 갱신
      AttackResolver.cs
      BasicAttackController.cs         ← OnAttackHit / OnAttackUsed 이벤트 추가 (은신 백어택용)  ★ 갱신
    Config/
      GameConfig.cs          ← 직업별 공격/쿨타임/스킬 수치 중앙 관리
    GameState/RespawnController.cs
    Bot/BotController.cs                 ← 스턴 중 이동/공격 차단 (중력 유지)  ★ 갱신
    Debug/DebugDamageInput.cs
    Input/PlayerInputReader.cs           ← SkillZPressed  ★ 갱신
    Networking/
    Player/PlayerEntity.cs / FirstPersonMotor.cs  ← MoveSpeedMultiplier 추가 (은신 이속 배율)  ★ 갱신
    Player/LocalPlayerController.cs      ← 스턴 중 이동/공격/스킬 차단, 카메라 회전 허용  ★ 갱신
    UI/
      CrosshairUI.cs / HealthDebugUI.cs / PlayerHUD.cs / ClassSelectionUI.cs
      AbilityDebugUI.cs      ← Rogue Q 충전 수/타이머 표시 추가  ★ 갱신
```

## Design Notes

### R 스킬 3종 위치/상태 기준 수정 — 53단계 (2026-06-27)

```
수정 1 — 전사 R: 바닥 기준 상승/낙하 + 유닛 통과

  문제:
    공중 R 시전 시 현재 높이 기준 상승 → rise+drop 합계 ≠ ground 귀환.
    시전 중 적/아군 유닛에게 충돌 물리 적용.

  수정:
    TryR() 시작 시 RaycastNonAlloc(down, max=riseHeight+5m, ~0, skipCharacters)로 지면 탐색.
    지면 찾으면: CC disable → transform.position = groundPos → CC enable.
    지면 못 찾으면: fallback(현재 위치).
    _rOrigin = groundPos → 판정/비주얼 앵커 = 바닥 기준.
    SetDashPassthrough(true) 추가 → R 시전 전 구간 유닛 통과.
    ClearRState(): _rState=Idle 먼저 → TryClearPassthrough() 호출.

  변경 파일: WarriorAbilityHandler.cs
    + s_groundHits RaycastHit[4] 정적 버퍼
    + TryFindGroundBelow() 헬퍼 (NonAlloc, HealthComponent 스킵)
    TryR(): 지면 스냅 + SetDashPassthrough(true)
    ClearRState(): TryClearPassthrough() 추가

  기존 유지: R 쿨타임, 무적, 피해/스턴 판정, 검 시각, 바닥 마커.

수정 2 — 도적 R: 은신 해제 조건 단순화

  이전 정책:
    RC/Q/E/Z/F 사용, 기본 공격 사용, 피격 → 은신 해제 또는 reveal.
  새 정책:
    은신이 풀리는 조건: 백어택 성공(IsBackstab && hit) 시만.
    스킬 사용, 비백어택 기본 공격, 피격 → 은신 유지.
    5초 자연 만료는 그대로.
    사망/ForceCleanup → BreakStealth 그대로 유지.

  변경 파일: RogueAbilityHandler.cs
    TryRCDash: BreakStealth() 제거
    TryQ: BreakStealth() 제거
    TryE (Idle/MarkedWaiting): BreakStealth() 제거
    TryZ: BreakStealth() 제거
    TryF (Idle/Window): BreakStealth() 제거
    OnBasicAttackUsed: 빈 함수 (은신 유지)
    OnTookDamage: 빈 함수 (ApplyReveal 제거)
    OnBasicAttackHit: 백어택 성공 후 BreakStealth() 추가

  변경 파일: AbilityDebugUI.cs
    _lastRReveal / _lastRRevealT 필드 제거
    ColReveal 색상 상수 제거
    UpdateRSlot Rogue 섹션: reveal 변수/분기 제거 → 단순 stealth/cd 표시

  기존 유지: 백어택 보너스 피해, IsBackstab 판정, 5초 지속, BreakStealth 자체 로직.
  reveal 관련 필드(_isRevealed 등)는 Handler에 잔존 — 사용 안 되는 죽은 코드이나
  구조 호환성 유지를 위해 제거하지 않음.

수정 3 — 마법사 R: 유성 타격 중심을 바닥 기준으로 보정

  문제:
    SpawnMeteorOrbSequence()가 ownerPos.y를 stormCenter.y로 사용
    → 공중/점프 시전 시 유성이 허공에서 터짐.
    경고 표시도 _impactPos 기준이므로 같이 틀어짐.

  수정:
    SpawnMeteorOrbSequence()에 TryFindGroundY() 정적 헬퍼 추가.
    stormCenter y: stormXZ 위치 아래 바닥 찾기 (cast 20m 위 → down 40m).
    orb spawnPos y: ownerPos 아래 바닥 찾기 (orb가 지면에서 상승 시작).
    바닥 못 찾으면: ownerPos.y fallback.

  변경 파일: MageAbilityHandler.cs
    + s_groundHits RaycastHit[4] 정적 버퍼
    + TryFindGroundY(Vector3 xzPos, out float groundY) 정적 헬퍼
    SpawnMeteorOrbSequence(): stormCenter.y + orbY 보정

  기존 유지: R 쿨타임, 유성 개수, 피해, 범위, 시전 제약, orb 상승 연출.

테스트:
  1. 전사 R을 점프 중 써도 바닥에서 상승 후 바닥에 착지하는지
  2. 전사 R 시전 중 유닛에게 막히지 않는지 (통과)
  3. 전사 R 종료 후 유닛 충돌 정상 복구
  4. 전사 R 사망 중 종료 후 passthrough 해제 (AbilityController.HandleOwnerDeath가 SetDashPassthrough(false) 호출)
  5. 도적 R → 은신 중 Q/E/F/Z/RC 사용 → 은신 유지
  6. 도적 R → 은신 중 피격 → 은신 유지, REVEALED 표시 없음
  7. 도적 R → 은신 중 정면 기본 공격(비백어택) → 은신 유지
  8. 도적 R → 은신 중 백어택 성공 → 은신 즉시 해제
  9. 도적 R 5초 → 자연 해제
  10. 마법사 R을 점프 중 시전 → 경고 표시와 유성 타격이 바닥 기준으로 발생
  11. 마법사 R 쿨타임/유성 수/피해 이상 없음
  12. 기존 Warrior Q/E/F/Z, Rogue Q/E/F/Z, Mage Q/E/F/Z 이상 없음
  13. 콘솔 에러 없음
```

### Archer Z — 50단계 Roll (2026-06-27)

```
설계:
  Z 입력 → 카메라 기준 수평 이동 방향으로 짧게 구름.
  방향 우선순위: 현재 프레임 MoveInput → 마지막 유효 MoveInput → (0,-1) = 뒤로.
  대각선 입력은 normalize → 직선 구르기와 동일 거리 보장.
  구르기 중 유닛 통과(SetDashPassthrough), 벽 통과 불가(CC 유지).
  구르기 중 피격·스킬·기본 공격 모두 허용.
  성공 시 F 게이지 +10 (최대치 초과 불가).

핵심 구조:
  ArcherAbilityHandler._cachedMoveInput — TickHeldInputs에서 이번 프레임 MoveInput 저장
  ArcherAbilityHandler._lastMoveInput   — 위 중 비영(非零) 값만 갱신 (마지막 유효 방향 기억)
  AbilityController.Tick() 순서 변경:
    TickHeldInputs가 TryActivate(Z) 보다 먼저 실행 → Z 발동 시 _cachedMoveInput이 현 프레임 값
  TryBeginRoll(): dead/stunned/쿨타임 체크 → 방향 계산 → _ac.StartDash(worldDir * speed, duration)
                 → SetCooldown(Z, 8s) → AddBarrageGauge(+10)
  HandleOwnerDeath(): _cachedMoveInput/_lastMoveInput 초기화 (다음 생에 기본값=뒤로)

변경 파일:
  GameConfig.cs          — archerRollDistance/Duration/Cooldown/GaugeGain 4개 필드 추가
  GameConfig.asset       — 4개 직렬화 값 추가
  ArcherAbilityHandler.cs — _cachedMoveInput/_lastMoveInput 필드, TryBeginRoll(), TryActivate에 Z 분기,
                             TickHeldInputs에 move cache 갱신, HandleOwnerDeath에 초기화
  AbilityController.cs  — Tick()에서 TickHeldInputs/_mageHandler.TickZ를 TryActivate 앞으로 이동

수치 (GameConfig):
  archerRollDistance  = 4m
  archerRollDuration  = 0.25s
  archerRollCooldown  = 8s
  archerRollGaugeGain = 10

UI:
  Z 슬롯은 AbilityDebugUI.UpdateZSlot() → UpdateStdSlot(5) 경로를 그대로 사용.
  쿨타임 배열 _cooldowns[(int)AbilitySlot.Z]에 SetCooldown(8s) → 기존 쿨타임 카운트다운 자동 표시.
  별도 UI 코드 불필요.
```

### 쿨타임 버그 수정 — 52 (2026-06-27)

```
수정 1 — 전사 Q/R/E/F/Z 쿨타임 (GameConfig.asset):
  warriorAbility: QCooldown 0→8, ECooldown 0→14, RCooldown 0→30, FCooldown 0→12, ZCooldown 미기재→18
  원인: asset 값이 0으로 남아있었음. GameConfig.cs 기본값(QCooldown=8 등)은 correct했으나
        asset이 override해 0으로 덮어씀.

수정 2 — 도적 E1 무한 발사 방지:
  EState 추가: ProjectileFlying (E1 발사 직후 ~ 적중/빗나감 해소 전)
  TryE(Idle): _eState = ProjectileFlying 설정 → Idle에서만 E1 발사 가능
  OnEProjectileHit: ProjectileFlying → MarkedWaiting (기존 E2 흐름 유지)
  OnEProjectileMissed (신규): ProjectileFlying → Idle. 쿨타임 없음 (비행 중 재발사가 이미 막혀 있어 충분)
  RogueMarkedShurikenProjectile: 사거리 만료/벽 충돌 시 OnEProjectileMissed 호출 추가
  HandleOwnerDeath: ClearEMark() → Idle 전환으로 ProjectileFlying 상태도 정리됨

수정 3 — 도적 E1 방향 3D화:
  FireEProjectile(): fwd.y = 0f 제거 → cameraTransform.forward 전체 사용
  스폰 위치: _ownerHealth.transform.position + standHeight*0.5 + fwd*0.8
            → _cameraTransform.position + fwd*0.5 (1인칭 조준과 일치)

수정 4 — 궁수 Q 쿨타임:
  정책: 강화탄 PRESS 시 쿨타임X, 강화탄 CONSUME(발사) 시 쿨타임 시작
  ConsumePendingShotType(): Shock/Fire/Ice 소비 시 _ac.SetCooldown(Q, archerAbility.QCooldown)
  TryBeginQ(): GetCooldownRemaining(Q) > 0 → block 추가
  GameConfig.cs: archerAbility 기본값에 QCooldown = 6f 추가
  GameConfig.asset: archerAbility.QCooldown 0→6

변경 파일:
  GameConfig.asset    — warriorAbility 4개 필드 수정, ZCooldown 추가, archerAbility.QCooldown 6
  GameConfig.cs       — archerAbility 기본값 QCooldown = 6f 추가
  RogueAbilityHandler.cs — EState.ProjectileFlying, OnEProjectileMissed, IsEFlying, FireEProjectile 방향
  RogueMarkedShurikenProjectile.cs — 범위만료/벽충돌 시 OnEProjectileMissed 호출
  AbilityController.cs — RogueIsEFlying 프로퍼티
  AbilityDebugUI.cs   — Rogue E 슬롯 "E1" 표시, Archer Q 쿨타임 표시
  ArcherAbilityHandler.cs — TryBeginQ Q쿨타임 게이트, ConsumePendingShotType 소비 시 쿨타임

테스트:
  1. 전사 Q 8s, E 14s, F 12s, Z 18s, R 30s 쿨타임 적용 확인 (UI + 실제 제한)
  2. 도적 E1 발사 후 재발사 불가, 표창 비행 중 E 슬롯 "E1" 표시 확인
  3. 도적 E1이 위/아래 방향으로도 날아가는지 확인 (카메라 가리키는 방향)
  4. 도적 E1 적중 → E2 재사용 정상 확인
  5. 도적 E1 빗나감(범위) → Idle 복귀, E 즉시 재사용 가능 확인
  6. 도적 E1 벽 충돌 → Idle 복귀 확인
  7. 궁수 Q 강화탄 선택(쿨타임 없음) → 발사 후 6s 쿨타임 시작 확인
  8. 궁수 Q 쿨타임 중 Q 재선택 불가 확인
  9. 궁수 Q UI: 강화탄 선택 시 SHOCK/FIRE/ICE, 쿨타임 중 Xs, 쿨타임 없으면 READY
  10. Warrior/Rogue/Mage/Archer 다른 스킬 이상 없음 확인
```

### HealthComponent self buff 리팩터링 — 51.1 (2026-06-27)

```
설계:
  기존: _selfBuffMult/_selfBuffTimer 단일 슬롯 (Mathf.Max 스태킹 → 서로 덮어씀)
  변경: _buffMults[3] / _buffTimers[3] 고정 배열 (enum SelfMoveSpeedSource 인덱스)
  - Generic(0): 기존 SetSelfMoveSpeedMultiplier 호환 경로; Warrior/Mage 레거시
  - ArcherRapidFire(1): RC 우클릭 버프
  - ArcherOverdrive(2): R 궁극기 버프
  SelfMoveSpeedMultiplier = product(활성 source 배열) × penalty
  각 source는 독립적으로 타이머가 만료되며 서로 영향 없음.

신규 API (HealthComponent):
  SetSelfMoveSpeedBuff(SelfMoveSpeedSource, float mult, float dur)
  ClearSelfMoveSpeedBuff(SelfMoveSpeedSource)

기존 API 유지:
  SetSelfMoveSpeedMultiplier — Generic 슬롯으로 라우팅(≥1) 또는 penalty(< 1)
  ClearSelfMoveSpeedMultiplier — 전체 buff 배열 + penalty 초기화
  ClearSelfMovePenalty — penalty만 초기화 (변경 없음)

변경 파일:
  SelfMoveSpeedSource.cs (신규) — enum 정의
  HealthComponent.cs             — 배열 구조, 새 API, Update 루프, Reinitialize
  ArcherAbilityHandler.cs        — RC/Overdrive가 각자 source로 SetBuff/ClearBuff 호출

검증 기대값:
  RC만: ×1.3   Overdrive만: ×1.5   동시: ×1.95
  F 난사 중: 위 곱에 archerFMoveSpeedMultiplier 추가 곱
  CancelRapidFire → ArcherOverdrive 버프 유지
  CancelOverdrive → ArcherRapidFire 버프 유지
  사망 후 Reinitialize → 모든 버프 0으로 초기화
```

### Archer R — 51단계 Overdrive (2026-06-27)

```
설계:
  R 발동 → 이동속도 ×1.5, 공격속도 ×2.0 (RC와 곱연산). 10s 지속, 30s 쿨타임.
  Basic 탄이 OverdriveBasic으로 자동 업그레이드: 크기 ×2, 충돌 반경 ×2, 충돌 시 AoE 폭발.
  Q 강화탄(Shock/Fire/Ice), F 난사탄(Barrage)은 업그레이드 제외.
  폭발(반경 2m, 피해 5): OverlapSphereNonAlloc + dedup + TryConsumeShieldFrom.
  폭발 보상(F게이지 +2 + Z쿨감 -0.1s): 폭발당 1회, 실제 HP 감소 시만.
  사망/ForceCleanup → CancelOverdrive() → ClearSelfMoveSpeedMultiplier().
  스턴 중: 공격 불가(기존 구조), 지속시간 계속 소모, R 상태 유지.

변경 파일:
  ArcherShotType.cs    — OverdriveBasic 열거값 추가
  GameConfig.cs        — archerOverdrive* 7개 필드 추가
  GameConfig.asset     — 7개 직렬화 값 추가
  ArcherAbilityHandler.cs — R 상태(_isOverdrive/_overdriveTimer), TryBeginOverdrive/CancelOverdrive,
                             AttackSpeedMultiplier = RC × Overdrive 곱,
                             TickTimers overdrive 타이머, TryActivate R 분기,
                             HandleOwnerDeath/ForceCleanup에 CancelOverdrive 추가
  AbilityController.cs — ArcherIsOverdrive / ArcherOverdriveTimer getter 추가
  BasicAttackController.cs — shotType == Basic && ArcherIsOverdrive → OverdriveBasic 업그레이드,
                              OverdriveBasic도 _onBasicHitCallback 사용
  ArcherBasicProjectile.cs — Init()에 OverdriveBasic 스케일/반경 조정,
                              Update()에 OverdriveBasic 히트 분기(ExplodeOverdriveAt),
                              ExplodeOverdriveAt() 추가, ApplyGlow에 OverdriveBasic 노글로우 처리
  AbilityDebugUI.cs    — _lastArcherOverdrive/T 필드, ColArcherOverdrive 색상,
                         UpdateArcherRSlot() 추가, UpdateRSlot()에 IsArcherRCMode 분기 추가

핵심 구조:
  공격속도:
    AttackSpeedMultiplier = rcMult * overdriveMult (각각 비활성 시 1f)
    effectiveCooldown = baseCooldown / AttackSpeedMultiplier

  OverdriveBasic 탄 결정:
    BasicAttackController.Tick(): ConsumePendingShotType() == Basic && ArcherIsOverdrive
    → shotType = OverdriveBasic (Q 선택 중이면 Q 유지 — Overdrive 영향 없음)

  OverdriveBasic 투사체:
    Init(): _radius *= scale, transform.localScale *= scale
    Update(): OverdriveBasic → ExplodeOverdriveAt(contact point)
    ExplodeOverdriveAt(): OverlapSphereNonAlloc + dedup + shield check + TakeDamage + 보상 1회

수치 (GameConfig):
  archerOverdriveDuration                 = 10s
  archerOverdriveCooldown                 = 30s
  archerOverdriveMoveSpeedMultiplier      = 1.5
  archerOverdriveAttackSpeedMultiplier    = 2.0
  archerOverdriveProjectileScaleMultiplier = 2.0
  archerOverdriveExplosionRadius          = 2.0m
  archerOverdriveExplosionDamage          = 5
```

테스트:
  1. R 사용 → 이동속도 +50% 체감 확인
  2. R 사용 → 기본 공격 속도 +100% (쿨타임 0.5s → 0.25s) 확인
  3. RC + R 동시: 공격속도 1.5 × 2.0 = 3.0배 확인
  4. R 중 Basic 탄 크기가 2배로 커지는지 확인
  5. R 중 Q 강화탄(Shock/Fire/Ice)은 기존 효과만 나가는지 확인
  6. R 중 F 난사탄은 크기 변화 없는지 확인
  7. R Basic 탄이 적/벽에 충돌 시 2m 폭발하는지 확인
  8. 폭발이 아군/자기 자신에게 피해 없는지 확인
  9. 보호막 대상 폭발 → 피해/보상 없는지 확인
  10. 폭발로 2명 이상 맞혀도 F게이지+Z쿨감이 1회만 적용되는지 확인
  11. R 지속시간 10s, 쿨타임 30s UI 확인
  12. R 중 사망 → 이동속도 즉시 복구 확인
  13. AbilityDebugUI R 슬롯: OVR X.Xs(주황) → 쿨타임 → READY 확인
  14. 콘솔 새 에러 없음
  15. Warrior/Rogue/Mage 기존 스킬 이상 없음 확인

### Archer Z — 50.1 Basic 적중 쿨타임 감소 (2026-06-27)

```
설계:
  Archer Basic 탄이 실제 HP를 감소시켰을 때만 Z 구르기 쿨타임 -0.1s.
  Q 강화탄·F 난사탄·보호막·무적 대상은 제외.

구조:
  AbilityController.OnArcherBasicHit(float gaugeAmount):
    AddBarrageGauge(gaugeAmount) + ReduceCooldown(AbilitySlot.Z, archerRollCooldownRefundOnBasicHit)
  AbilityController.ReduceCooldown(AbilitySlot slot, float amount):
    _cooldowns[i] = Mathf.Max(0f, _cooldowns[i] - amount)
  BasicAttackController:
    _onBasicHitCallback = OnArcherBasicHit (Basic 전용: 게이지+쿨감)
    _addFGaugeCallback  = AddArcherFGauge  (Q 전용: 게이지만)
    Tick()에서 shotType == Basic → _onBasicHitCallback, else → _addFGaugeCallback

  ArcherBasicProjectile은 변경 없음.
  기존 HP 비교(_cachedHp < hpBefore) 조건 그대로 유지 — 보호막/무적 자동 제외.

변경 파일:
  GameConfig.cs     — archerRollCooldownRefundOnBasicHit = 0.1f
  GameConfig.asset  — 직렬화 값 0.1 추가
  AbilityController.cs — OnArcherBasicHit(float) + ReduceCooldown(AbilitySlot, float) 추가
  BasicAttackController.cs — _onBasicHitCallback 필드, Start()에서 캐시, Tick()에서 shotType 분기

수치:
  archerRollCooldownRefundOnBasicHit = 0.1s
```

테스트:
  1. Basic 탄으로 적 HP 감소 → Z 쿨타임 0.1s 감소 확인
  2. 보호막 대상 Basic 적중 → Z 쿨타임 변화 없음 확인
  3. Q 강화탄 폭발로 적 HP 감소 → Z 쿨타임 변화 없음 확인
  4. F 난사탄 적중 → Z 쿨타임 변화 없음 확인
  5. Z 쿨타임 0일 때 Basic 적중 → 0 아래로 안 내려가는 것 확인
  6. 기존 F 게이지 +2/hit 정상 유지 확인
  7. Q 강화탄 F 게이지 +3 정상 유지 확인
  8. 콘솔 새 에러 없음

테스트:
  1. W/A/S/D 각 방향으로 Z → 해당 카메라 기준 방향으로 구르는지 확인
  2. W+D 대각선 Z → 직선 구르기와 동일 거리 확인
  3. 이동 없이 Z → 마지막 이동 방향으로 구르는지 확인
  4. 게임 시작 후 아무 이동도 없이 Z → 뒤로 구르는지 확인
  5. 구르기 중 다른 캐릭터를 통과하는지 확인
  6. 구르기 중 벽 통과 불가 확인
  7. Z 쿨타임 8s UI 카운트다운 확인
  8. 구르기 성공 후 F 게이지 +10 확인 (최대치 초과 없음)
  9. 구르기 중 기본 공격 및 다른 스킬 사용 가능 확인
  10. 스턴/사망 중 Z → 발동 안 됨 확인
  11. 콘솔 새 에러 없음
  12. Warrior/Rogue/Mage 기존 스킬 이상 없음 확인

### Archer F — 49단계 Barrage Gauge (2026-06-27)

```
설계:
  기본 공격(Basic shot)이 적에게 피해를 줄 때 게이지 +2.
  Q 강화탄 폭발이 적을 1명 이상 맞힐 때 게이지 +3 (1회/폭발).
  F 난사탄(Barrage)은 게이지를 충전하지 않음 (무한 루프 방지).
  F를 꾹 누르는 동안 20/s 소모하며 0.1초마다 1발 발사.
  게이지 10 미만이면 시작 불가. 0이 되면 자동 종료.
  사망해도 게이지 유지(리스폰 후 재사용 가능). 사망 중 발사 불가.

변경 파일:
  PlayerCommand.cs       — SkillFHeld, SkillFReleased 추가
  PlayerInputReader.cs   — GetKey(F), GetKeyUp(F) 읽기 추가
  GameConfig.cs          — archerF* 11개 필드 추가 (Archer — Barrage Gauge header)
  GameConfig.asset       — 11개 직렬화 값 추가
  ArcherShotType.cs      — Barrage 열거값 추가
  HealthComponent.cs     — ClearSelfMovePenalty() 추가 (penalty 레이어만 제거)
  ArcherBasicProjectile.cs — SpawnBarrage() 정적 팩토리, Barrage 타입 처리,
                              onHitGaugeGain System.Action<float> 콜백, GetFromPool helper
  ArcherAbilityHandler.cs  — cameraTransform 저장, F 게이지 시스템,
                              TickHeldInputs(PlayerCommand), StartFBarrage/EndFBarrage/FireBarrageShot,
                              AddBarrageGauge, 공개 getter ArcherFGauge/ArcherFMaxGauge/ArcherIsFiring
  AbilityController.cs   — ArcherFGauge/ArcherFMaxGauge/ArcherIsFiring/AddArcherFGauge 노출,
                            ShouldBlockBasicAttack에 ArcherIsFiring 포함,
                            Tick()에서 _archerHandler?.TickHeldInputs(cmd) 호출
  BasicAttackController.cs — ShouldBlockBasicAttack 체크 추가,
                              _addFGaugeCallback 캐시(Start()에서 1회 할당),
                              Spawn() 호출에 callback 전달
  AbilityDebugUI.cs      — UpdateArcherFSlot() 추가, ColArcherBarrage 색상,
                            UpdateFSlot()에 Archer 분기 최우선 추가

핵심 구조:
  게이지 충전 경로:
    Basic hit → 기존 Spawn() 5번째 파라미터 onHitGaugeGain = _addFGaugeCallback
               → ArcherBasicProjectile.Update()에서 !blocked 시 Invoke(+2)
    Q shot    → Spawn() 동일 콜백 → ExplodeAt()에서 첫 비차단 적중 시 Invoke(+3)
    Barrage   → SpawnBarrage()는 null 전달 → 게이지 충전 없음

  이동속도:
    StartFBarrage(): SetSelfMoveSpeedMultiplier(0.8f, 99999f) — penalty 레이어
    EndFBarrage():   ClearSelfMovePenalty() — penalty만 제거, RC buff는 유지
    RC + F 동시: buff(1.3) × penalty(0.8) = 1.04 — 기존 2-레이어 구조 자동 처리

  기본 공격 차단:
    ShouldBlockBasicAttack에 ArcherIsFiring 포함 →
    BasicAttackController.Tick() 초기에 return (Mage Z와 동일 경로)

  F 입력:
    SkillFPressed(GetKeyDown) — 기존 Warrior/Rogue/Mage F에서만 사용
    SkillFHeld(GetKey)        — Archer의 TickHeldInputs에서 소비
    둘 다 PlayerCommand에 공존, 서로 간섭 없음
```

테스트:
  1. 기본 공격 적중 → F 게이지 +2 확인
  2. Q Shock/Fire/Ice 폭발 적중 → +3 확인 (1명 이상)
  3. F 난사탄 적중으로는 게이지 증가 없음 확인
  4. 게이지 100 초과 없음 확인
  5. 게이지 10 미만 F 시작 불가 확인
  6. F 꾹 누르면 0.1s마다 탄환 발사 확인
  7. F 떼면 즉시 멈춤 확인
  8. 게이지 0 → 자동 종료 확인
  9. F 사용 중 좌클릭 기본 공격 차단 확인
  10. F 중 Q pending 유지 (소모 안 됨) 확인
  11. F 탄환 피해 3, 둔화 -20% 0.4s 확인
  12. F 중 이동속도 80% 확인
  13. RC + F 동시: 1.3 × 0.8 = 1.04 이동속도 확인
  14. 사망 → F 발사 중단, 게이지 유지 확인
  15. 리스폰 후 남은 게이지로 F 재사용 가능 확인
  16. AbilityDebugUI F 슬롯: X%(초록)/FIRE\nX%(보라)/0%(회색) 확인
  17. Warrior/Rogue/Mage 기존 F 동작 이상 없음 확인
  18. 콘솔 새 에러 없음 확인

### Archer — 48단계 Q 구조 정리 + 보호막 이벤트 차단 (2026-06-27)

```
수정 1 — Q 쿨타임/선택 구조 명시 (ArcherAbilityHandler.TryBeginQ)
  원인: Q는 탄종 선택 토글이라 공용 쿨타임이 없어야 하나, 구조 문서화가 미흡.
  수정: TryBeginQ() 주석에 no-cooldown 의도 명시:
    • _ac.SetCooldown(Q, ...) 호출 없음 — 의도적 설계
    • _archerHandler가 Archer 모든 슬롯을 선취하므로 QCooldown config 값은 무시됨
    • 발사 속도 제한은 좌클릭 attack cooldown이 담당
  검증: SkillQPressed = GetKeyDown(Q) — 프레임당 1회만 발생, 중복 사이클링 없음.

수정 2 — 보호막이 강화탄 damage+CC 묶음을 1회 이벤트로 차단 (HealthComponent + ArcherBasicProjectile)
  문제:
    ExplodeAt()에서 TakeDamage() 호출 시 shield 소모 → _shieldActive = false
    → 이어지는 ApplyStun/ApplySlow의 shield 체크는 false → CC 통과
    → 보호막이 피해만 막고 CC는 통과하는 버그

  원인: TakeDamage/ApplyStun/ApplySlow가 각각 독립적으로 shield를 체크.
  강화탄 한 발(damage+CC)은 하나의 공격 이벤트이어야 함.

  수정: HealthComponent.TryConsumeShieldFrom(Team sourceTeam) 신규 추가
    - _shieldActive가 없으면 false 반환
    - IsHostileSource(sourceTeam)가 아니면 false 반환
    - 위 두 조건 만족 시 ConsumeShield() 호출 후 true 반환
    - 단일 소비 보장: 이후 재호출은 항상 false

  ExplodeAt() 수정:
    dedup 추적(s_explodeHCBuf[hitCount++]) 이후, TakeDamage 이전에:
      if (hc.TryConsumeShieldFrom(_ownerTeam)) continue;
    → true: 해당 대상의 damage+CC 전체 스킵 (보호막 1회 소모로 이벤트 단위 차단)
    → false: 기존 TakeDamage/CC 정상 적용

  dedup 이후 배치 이유:
    동일 hc가 s_explodeBuf에 여러 collider로 등록된 경우,
    첫 encounter에서 shield 소모 + continue 후 추적 누락 → 두 번째 collider에서
    shield 없이 피해 적용되는 버그 방지.
    s_explodeHCBuf에 먼저 추가 → already=true → 두 번째 collider에서 skip 보장.

  Basic탄: TakeDamage 내부 shield 체크가 처리 (CC 없으므로 이슈 없음, 변경 없음).

정리:
  - Shock 보호막 있으면 → 피해+기절 모두 차단 (1회 소모)
  - Ice 보호막 있으면 → 피해+둔화 모두 차단 (1회 소모)
  - Fire 보호막 있으면 → 피해 차단 (1회 소모)
  - Basic 보호막 있으면 → 피해 차단 (기존 TakeDamage 처리 유지)
  - 아군/자기 자신 → 기존대로 skip
  - 보호막 없으면 → 기존대로 전체 적용

변경 파일:
  HealthComponent.cs   — TryConsumeShieldFrom(Team) 추가
  ArcherBasicProjectile.cs — ExplodeAt()에 shield 이벤트 차단 추가
  ArcherAbilityHandler.cs  — TryBeginQ() no-cooldown 주석 명시
```

테스트:
  1. Q 여러 번 → Shock→Fire→Ice→Shock 순환, 쿨타임 없이 즉시 전환 확인
  2. Q 슬롯 UI: SHOCK(노랑)/FIRE(빨강)/ICE(파랑)/READY(초록) 정상 표시
  3. Q 쿨타임 숫자가 UI에 나타나지 않음 확인
  4. E 보호막 상태에서 Shock 탄 맞으면 → 피해+기절 모두 차단, 보호막 1회 소모
  5. E 보호막 상태에서 Ice 탄 맞으면 → 피해+둔화 모두 차단, 보호막 1회 소모
  6. E 보호막 상태에서 Fire 탄 맞으면 → 피해 차단, 보호막 1회 소모
  7. 보호막 없는 상태에서 Shock → 피해+기절 정상 적용
  8. 보호막 없는 상태에서 Ice → 피해+둔화 정상 적용
  9. E 보호막이 Basic 탄도 여전히 막는지 확인
  10. 아군/자기 자신에게 강화탄 피해/CC 없음 확인
  11. Warrior Stun/Knockback/LaunchImpulse에 보호막 동작 이상 없음 확인
  12. Warrior/Rogue/Mage 기존 기능 정상 확인
  13. 콘솔 새 에러 없음

### Archer Q — 강화탄 선택 (Enhanced Shot, 2026-06-27)

```
신규 파일:
  Assets/Scripts/Combat/ArcherShotType.cs
    namespace Game.Combat { public enum ArcherShotType { Basic, Shock, Fire, Ice } }

Q 입력 흐름:
  AbilityController.TryActivate(Q) → ArcherAbilityHandler.TryActivate(Q) → TryBeginQ()
  TryBeginQ(): 사망/스턴 시 false. _pendingShotType 순환 Basic→Shock→Fire→Ice→Shock(wrap).
  Q 쿨타임 없음 — 탄종 순환은 즉시 적용, 속도 제한은 좌클릭 쿨타임이 담당.

ArcherAbilityHandler 추가 API:
  bool            HasPendingShotType  — pending이 Basic이 아닌지
  ArcherShotType  PendingShotType     — 현재 선택된 탄종 (읽기 전용)
  ArcherShotType  ConsumePendingShotType() — 값 반환 후 Basic으로 초기화
  HandleOwnerDeath()/ForceCleanup(): _pendingShotType = Basic으로 리셋

AbilityController 추가 노출:
  bool           ArcherHasPendingShot
  ArcherShotType ArcherPendingShotType
  ArcherShotType ConsumeArcherPendingShotType()  ← BasicAttackController가 호출

BasicAttackController 발사 흐름 (Archer):
  1. ConsumeArcherPendingShotType() — pending 탄종 가져오고 Basic으로 초기화
  2. ArcherBasicProjectile.Spawn(..., shotType)

BotController: ArcherBasicProjectile.Spawn(..., ArcherShotType.Basic) 명시적 전달

ArcherBasicProjectile 변경:
  신규 필드: _shotType, _config, _glowGO, _glowRenderer
  신규 statics: s_explodeBuf[16], s_explodeHCBuf[16], s_glowShockMat, s_glowFireMat, s_glowIceMat
  Init() 시그니처 변경: (... ArcherShotType shotType, GameConfig config) 추가
  Spawn() 시그니처 변경: shotType = ArcherShotType.Basic 기본값 파라미터 추가
  CreateNew(): body Capsule + Glow Sphere 자식 GO 1회 생성 (풀 재사용 시 생략)
    Glow localScale = (2.0, 1.43, 2.0) — parent 비균일 스케일 (0.20,0.28,0.20) 보정 → 세계 ~0.40m
  ApplyGlow(shotType): Basic→glowGO.SetActive(false), Shock/Fire/Ice→SetActive+sharedMaterial
  glow material 캐시: URP/Unlit + color (Shock=노랑, Fire=빨강, Ice=파랑)
  ReturnToPool(): SetActive(false)로 glow 자식도 함께 비활성

Update() 충돌 분기:
  Basic:       기존 단일 대상 TakeDamage
  Shock/Fire/Ice: ExplodeAt(transform.position + _direction * bestDist)

ExplodeAt(center):
  Physics.OverlapSphereNonAlloc(center, radius, s_explodeBuf, _layerMask)
  대상 필터: hc != null, hc != _ownerHealth, hc.Team != _ownerTeam, !hc.IsDead, hc.IsTargetable
  dedup: s_explodeHCBuf(max 16) 선형 탐색
  피해 후: Shock→ApplyStun(_ownerTeam), Ice→ApplySlow(_ownerTeam)
  Fire: 피해만
  GC 해제: hitCount 반복 s_explodeHCBuf[i] = null

AbilityDebugUI Q 슬롯 (Archer):
  UpdateQSlot(): IsArcherRCMode이면 UpdateArcherQSlot()으로 먼저 라우팅
  UpdateArcherQSlot(): _lastArcherQShot 변경 감지 → _status[1]/bgColors[1] 갱신
  색상: Shock=노랑(1.0,0.92,0.05), Fire=빨강(0.88,0.15,0.02), Ice=파랑(0.05,0.55,0.92)
  표시: "SHOCK" / "FIRE" / "ICE" / "READY" — 문자열 고정, 매 프레임 할당 없음

GameConfig 추가 필드 (Archer — Enhanced Shot Q):
  archerQShockRadius=1.2, archerQShockDamage=5, archerQShockStunDuration=0.7
  archerQFireRadius=2.0, archerQFireDamage=10
  archerQIceRadius=3.0, archerQIceDamage=4, archerQIceSlowMultiplier=0.7, archerQIceSlowDuration=2.0
```

테스트:
  1. Archer Q → 디버그 Q 슬롯이 SHOCK(노랑)으로 전환되는지
  2. 재차 Q → FIRE(빨강), 다시 Q → ICE(파랑), 다시 Q → SHOCK 순환 확인
  3. 좌클릭 1발 발사 후 Q 슬롯이 READY로 돌아오는지 확인
  4. Basic탄: 적 1명만 피해, 관통 없음 확인
  5. Shock탄(1.2m): 소범위 폭발 + 기절 0.7s 적용, 아군/환경 피해 없음
  6. Fire탄(2.0m): 중범위 폭발 + 피해 10, CC 없음
  7. Ice탄(3.0m): 대범위 폭발 + 피해 4 + 이동속도 -30% 2초
  8. 벽/바닥 직격 시 강화탄 폭발 발생 확인
  9. E 보호막 상태에서 Shock/Ice 맞았을 때 보호막 1회 소모 확인
  10. 투사체 풀링: 발사 후 glow 색상이 이전 탄종으로 남지 않는지 확인
  11. Rapid Fire(우클릭) 중 강화탄 1발만 소모되는지 확인
  12. Warrior/Rogue/Mage 기존 기능 정상 동작 확인
  13. 콘솔 새 에러 없음

### Archer — 46단계 구조 최적화 (2026-06-27)

```
수정 1 — ArcherBasicProjectile 사거리 끝 판정 누락 수정
  이전: dist = speed*dt 로 rangeRemaining 먼저 소모 → <=0 이면 SphereCast 스킵
  수정: step = Mathf.Min(dist, rangeRemaining) 로 SphereCast 후 이동, 마지막에 소모
        Update(): lifetime 체크 → step 계산 → SphereCast → hit/miss → position += step, range -= step → range<=0 ReturnToPool

수정 2 — ArcherBasicProjectile 오브젝트 풀링
  s_pool: static List<ArcherBasicProjectile>(cap=16), 최대 32개 보관 (cap 초과 시 Destroy)
  Spawn(): 풀에서 꺼내 SetActive(true) + SetPositionAndRotation + Init() 재사용
           풀 비었으면 CreatePrimitive + Collider제거 + sharedMaterial + AddComponent (1회)
  ReturnToPool(): SetActive(false) + pool.Add(this) (cap 미만) or Destroy (cap 초과)
  null 항목 처리: while 루프로 pool 끝에서 null 제거

수정 3 — SelfMoveSpeedMultiplier buff/penalty 2-레이어 분리 (HealthComponent.cs)
  이전: _selfMoveMult(단일슬롯, Mathf.Min = 감속 우선) — archer 버프(1.3)와 구조적 불일치
  수정:
    _selfBuffMult / _selfBuffTimer — buff (>=1, Mathf.Max wins)
    _selfPenMult  / _selfPenTimer  — penalty (<1, Mathf.Min wins)
    SelfMoveSpeedMultiplier = buff * penalty (곱)
  라우팅: SetSelfMoveSpeedMultiplier(mult, dur) — mult>=1 → buff; mult<1 → penalty
  정리: ClearSelfMoveSpeedMultiplier() → 두 레이어 모두 초기화 (기존 API 유지)
  기존 Warrior(감속)/Mage(레이저 슬로우) → penalty 레이어. Archer 버프 → buff 레이어. 간섭 없음.
  Reinitialize()/Update() → 두 타이머 모두 처리

수정 4 — 보호막이 적 CC만 차단 (HealthComponent + 전 CC 호출부)
  이전: ApplyStun/Knockback/LaunchImpulse/Slow 에 sourceTeam 없어 모든 CC가 보호막 소모
  수정:
    함수 시그니처: ApplyStun(float, Team sourceTeam = Neutral) 등 — 하위 호환 기본값
    IsHostileSource(Team): sourceTeam != Team || Team == Neutral (friendly = 같은 팀+비중립)
    차단 조건: _shieldActive && IsHostileSource(sourceTeam)
  업데이트된 호출부 (11곳):
    MageFireballProjectile: ApplyStun(_ownerTeam), ApplyKnockback(_ownerTeam)
    MageArcaneBoltProjectile: ApplyStun(_ownerTeam)
    MageAbilityHandler (레이저): ApplyKnockback(_ownerHealth.Team)
    MageBlackholeZone: ApplySlow(_ownerTeam)
    RogueGiantShurikenZone: ApplySlow(_sourceTeam)
    RogueStunBomb: ApplyStun(_ownerTeam)
    WarriorZWave: ApplyKnockback(_ownerHc.Team)
    WarriorAbilityHandler Q: ApplyLaunchImpulse(_ownerHealth.Team)
    WarriorAbilityHandler F: ApplyKnockback(_ownerHealth.Team)
    WarriorAbilityHandler R: ApplyStun(_ownerHealth.Team)
```

테스트:
  1. Archer 기본 공격 정상 발사 확인
  2. 사거리 경계(30m) 부근 적 명중 확인
  3. 초당 2발/우클릭 중 3발 속도 유지 확인
  4. 풀링: Archer 발사 후 투사체 SetActive(false)로 반환, 재사용 확인 (Console 새 에러 없음)
  5. 우클릭 이동속도 +30% 실제 적용 확인 (Warrior guard 감속도 정상)
  6. E 보호막 → 적 피해 1회 차단 후 소멸 확인
  7. E 보호막 → 적 CC (스턴/넉백/슬로우) 1회 차단 후 소멸 확인
  8. E 보호막 → 아군 효과/자기 효과로 소모되지 않는 것 확인
  9. 사망/리스폰 후 rapid fire, shield, self speed modifier 상태 남지 않음 확인
  10. Warrior/Rogue/Mage 주요 스킬 (Z파동, Q급습, 블랙홀 등) 정상 동작 확인

### Archer — RC 래피드파이어 버프 (Rapid Fire)

```
구현 파일: ArcherAbilityHandler.cs (신규) / AbilityController.cs / BasicAttackController.cs
           AbilityDebugUI.cs / GameConfig.cs

핸들러 패턴: WarriorAbilityHandler 등과 동일한 Init/TickTimers/TryActivate/HandleOwnerDeath/ForceCleanup
AbilityController.Start()에서 Archer이면 AddComponent<ArcherAbilityHandler>().Init(...)

RC 버프 흐름:
  우클릭 → AbilityController.Tick(cmd.RightClickPressed) → TryActivate(RC)
  → ArcherAbilityHandler.TryActivate(RC) → TryBeginRapidFire()
  → _isRapidFiring = true, _rapidFireTimer = 3s
  → ownerHealth.SetSelfMoveSpeedMultiplier(1.3f, 3s) — HC 자동 만료
  → SetCooldown(RC, 10s)

이동속도 반영 경로:
  HC._selfMoveMult = 1.3 → SelfMoveSpeedMultiplier 프로퍼티 반환
  → FirstPersonMotor/BotController: finalSpeed = base * SelfMoveSpeedMult * MoveSpeedMult

공격속도 반영 경로 (플레이어만):
  BasicAttackController._abilityController (Start()에서 GetComponent 1회 캐시)
  → abilityController.ArcherAttackSpeedMultiplier → _archerHandler.AttackSpeedMultiplier
  effectiveCooldown = archerBasicProjectileCooldown / multiplier = 0.5 / 1.5 = 0.333s

버프 종료:
  자동: TickTimers에서 _rapidFireTimer → 0 → _isRapidFiring = false
         HC._selfMoveTimer 자동 감소 → SelfMoveSpeedMultiplier = 1f
  수동: HandleOwnerDeath / ForceCleanup → CancelRapidFire() → ClearSelfMoveSpeedMultiplier()

UI (AbilityDebugUI RC 슬롯):
  IsArcherRCMode 분기 → UpdateArcherRCSlot()
  버프 중: "RAPID\nX.Xs" 강철 파란색(0.10, 0.42, 0.65)
  쿨타임 중: Fmt(cdT)+"s" / READY
```

| 필드 | 기본값 | 설명 |
|------|--------|------|
| `archerRapidFireDuration`              | 3 s | 버프 지속 시간 |
| `archerRapidFireMoveSpeedMultiplier`   | 1.3 | 이동속도 배율 (+30%) |
| `archerRapidFireAttackSpeedMultiplier` | 1.5 | 공격속도 배율 (+50%) |
| `archerRapidFireCooldown`              | 10 s | 우클릭 쿨타임 |

테스트:
  1. 우클릭 → RC 슬롯 "RAPID X.Xs" (강철 파란색) 확인
  2. 버프 중 이동속도 증가 체감 확인
  3. 버프 중 기본 공격 간격 ≈ 0.33s (초당 3회) 확인
  4. 3s 후 이동속도/공격속도 복구 확인
  5. 버프 중 사망 → 리스폰 후 이속 정상 복구 확인
  6. 쿨타임 10s 카운트다운 UI 확인
  7. 버프 중 우클릭 재시도 → 재발동 없음 확인

### Archer — 기본 공격 투사체 (Basic Projectile)

```
구현 파일: ArcherBasicProjectile.cs (신규) / BasicAttackController.cs / BotController.cs / GameConfig.cs

입력 흐름:
  좌클릭 1회 (AttackPressed, 홀드 무시) → BasicAttackController.Tick()
  → _isArcher 분기 → ArcherBasicProjectile.Spawn(ownerHealth, origin, dir, config)
  → OnAttackUsed?.Invoke() (발사 시 쿨타임 소비, 탄착과 무관)
  봇(Archer): BotController.Update() → _isArcher 분기 → 동일 Spawn() 호출

ArcherBasicProjectile — 구조:
  Spawn() : 정적 팩토리. Sphere primitive 생성 + Collider 제거 + static sharedMaterial + AddComponent.
  Init()  : ownerTeam, ownerHealth, damage, speed, range, radius, direction, layerMask 주입.
  Update(): dist = speed * dt. SphereCastNonAlloc(pos, radius, dir, s_hits[8], dist, layerMask).
            self/ally/dead skip. 가장 가까운 blocker(enemy or env) 충돌 시:
              enemy HealthComponent → TakeDamage → Destroy
              env (null HC)         →              Destroy
            _lifetime(3s) 또는 _rangeRemaining <= 0 → Destroy
            충돌 없으면 transform.position += dir * dist
  비주얼: 구리-청동색 Capsule (0.72, 0.40, 0.08). 로컬 Y = 진행방향.
           localScale = (0.20, 0.28, 0.20) → 직경 0.20m, 길이 0.56m. static s_mat 캐시.
           Quaternion.FromToRotation(Vector3.up, direction)으로 항상 진행방향 정렬.

BotController 변경:
  _isArcher 캐시 (Start() 1회). Archer 봇 attackRange = archerBasicProjectileRange(30m).
  sqDist <= 900 일 때 ArcherBasicProjectile.Spawn(). 비Archer는 기존 AttackResolver 유지.
  Archer 봇은 30m 이내에서 멈추고 0.5s cd로 발사 — 소형 맵에서 항상 사정거리 안에 있음.
```

| 필드 | 기본값 | 설명 |
|------|--------|------|
| `archerBasicProjectileDamage`   | 5 | 기본 피해 |
| `archerBasicProjectileCooldown` | 0.5 s | 발사 쿨타임 (초당 2회) |
| `archerBasicProjectileRange`    | 30 m | 최대 사거리 |
| `archerBasicProjectileSpeed`    | 80 m/s | 투사체 속도 |
| `archerBasicProjectileRadius`   | 0.18 m | 충돌 구체 반지름 |

테스트:
  1. PlayerStartingClass = Archer, 좌클릭 1회 → 황금 구체 발사 확인
  2. 좌클릭 홀드 → 0.5s 이후 1발씩만 추가 발사 (자동 연사 없음)
  3. 사거리 30m 이상 → 투사체 소멸 확인
  4. 적에게 직격 → HP 5 감소, 투사체 소멸 확인
  5. 아군 통과 → 피해 없음, 관통 확인
  6. 벽/바닥 충돌 → 투사체 소멸 (피해 없음) 확인
  7. Warrior/Rogue/Mage 기본 공격 → 기존 raycast 방식 동일하게 동작 확인
  8. Archer 봇 → 30m 이내에서 ArcherBasicProjectile Spawn 확인

### Mage Primary — 화염구 탄창/충전/발사

```
구현 파일: MageAbilityHandler.cs / MageFireballProjectile.cs / AbilityController.cs
          BasicAttackController.cs / PlayerCommand.cs / PlayerInputReader.cs
          GameConfig.cs / AbilityDebugUI.cs

입력 흐름:
  Q 키 1회 누름 → AbilityController.Tick(cmd.SkillQPressed) → TryActivate(Q)
  → MageAbilityHandler.TryActivate(Q) → TryFireQ()
  좌클릭 기본 공격은 기존 BasicAttackController 경로 그대로 사용 (화염구 무관)
  자동 연사 없음 — Q 1회당 1발, 0.2s 발사 간격 제한만 존재

탄창 / 충전:
  _ammo (int): 0 ~ mageFireballMaxAmmo(15)
  _rechargeTimer: TickTimers에서 매 프레임 감소. _ammo < max일 때만 감소.
    0이 되면 _ammo++, timer = mageFireballRechargeInterval(2.0s) 재설정.
    _ammo == max이면 timer를 interval로 리셋 (다음 소모 직후 바로 카운트 시작).
  사망 중에도 TickTimers 계속 실행 → 탄창 자동 회복.

발사 로직 (TickPrimary):
  - held && _fireTimer == 0 && !Dead && !Stunned && _ammo > 0 → SpawnFireball
  - _totalFireCount++: 1-based. _totalFireCount % mageBigFireballEvery(5) == 0 → big
  - _ammo--, _fireTimer = mageFireballFireInterval(0.2s)
  - 사망/cleanup 후에도 _ammo, _totalFireCount, _rechargeTimer 유지 (ClearTeleportState에서 미초기화)

투사체 (MageFireballProjectile.cs):
  - Init(ownerTeam, ownerHealth, damage, speed, range, radius, direction, layerMask)
  - Update(): dist = speed * dt, SphereCastNonAlloc(pos, radius, dir, s_hits[8], dist, layerMask)
  - 히트 처리:
      HealthComponent 있음 → 자신/아군/사망 skip, 나머지 → TakeDamage + Destroy
      HealthComponent 없음 → 환경 geometry → Destroy
  - _lifetime(3s) 초과 또는 _rangeRemaining <= 0 → Destroy
  - static RaycastHit[] s_hits[8] 공유 (Update는 단일 스레드이므로 안전)
  - TODO: 오브젝트 풀로 교체 예정

비주얼:
  - Sphere primitive, 콜라이더 제거 (자체 SphereCast 사용)
  - 일반: 주황 (1.0, 0.45, 0) sharedMaterial, scale = radius*2
  - 대형: 진빨 (1.0, 0.10, 0) sharedMaterial, scale = bigRadius*2
  - Material 2개 static 캐시 (반복 생성 없음)

UI (AbilityDebugUI Q 슬롯):
  - Mage이면 UpdateMageAmmoSlot() 우선
  - _ammo == 0: "0/15", ColCooldown
  - nextBig ((_totalFireCount+1) % 5 == 0): "BIG\nammo/max", ColBigFireball (deep red)
  - 일반: "FIRE\nammo/max", ColFireball (orange)
  - 변경 시에만 문자열 갱신 (_lastMageAmmo, _lastMageMaxAmmo, _lastMageNextBig 추적)

테스트:
  1. PlayerStartingClass = Mage, 좌클릭 홀드 → 0.2초마다 주황 구체 발사 확인
  2. ammo 카운터가 15에서 감소, 2초마다 +1 회복 확인
  3. 5번째, 10번째, 15번째 발사 → 더 크고 진한 빨간 구체 확인
  4. 적(Bot) 직격 → HP 감소 (일반 6, 대형 15) 확인
  5. 아군에게 발사 → 피해 없음 확인
  6. 벽에 맞으면 구체 사라짐 확인
  7. ammo 0 → 발사 안 됨, 2초마다 회복 확인
  8. 사망 → 발사 불가, 부활 후 탄창/카운트 유지 확인
  9. Warrior/Rogue 클래스 좌클릭 기본 공격 정상 작동 확인
  10. AbilityDebugUI Q 슬롯에 FIRE X/15 / BIG X/15 표시 확인
```

### GameConfig 마법사 화염구 수치
| 필드 | 기본값 | 설명 |
|------|--------|------|
| `mageFireballMaxAmmo` | 15 | 최대 탄창 |
| `mageFireballRechargeInterval` | 2.0 s | 탄 1개 충전 간격 |
| `mageFireballFireInterval` | 0.2 s | 발사 간격 |
| `mageFireballSpeed` | 18 m/s | 투사체 속도 |
| `mageFireballRange` | 20 m | 최대 사거리 |
| `mageFireballDamage` | 6 | 일반 화염구 피해 |
| `mageBigFireballEvery` | 5 | N번째마다 대형 |
| `mageBigFireballDamage` | 15 | 대형 화염구 피해 |
| `mageBigFireballScale` | 2.5 | 대형 비주얼 배율 (현재 미사용, radius 비율로 처리) |
| `mageFireballRadius` | 0.25 m | 일반 구 반지름 |
| `mageBigFireballRadius` | 0.65 m | 대형 구 반지름 |

### Mage RC — 2연속 텔레포트 (Blink)

```
구현 파일: MageAbilityHandler.cs / GameConfig.cs / AbilityController.cs / AbilityDebugUI.cs

동작:
  - 우클릭 → TryRC() → 텔레포트 1회 (6m 수평 방향)
  - 성공 시 _chargesUsed++. chargesUsed < maxCharges(2) → recast window 0.5s 시작
  - 0.5s 안에 우클릭 재입력 → 텔레포트 2회 → RightClickCooldown(8s) 시작
  - 0.5s 내 재입력 없으면 → recast window 만료 시 쿨타임 시작
  - 첫 텔레포트 실패: 쿨타임 미소모, recast window 시작 안 함
  - 두 번째 텔레포트 실패: recast window 유지, 남은 시간 내 재시도 가능

텔레포트 방향:
  - cameraTransform.forward Y=0 수평 벡터 (카메라가 위/아래 향해도 수평 이동)
  - sqrMagnitude < 0.01이면 ownerHealth.transform.forward Y=0 fallback

안전 위치 탐색 (IsSafeDestination + TryFindSafeDestination):
  - OverlapCapsuleNonAlloc(CharacterController 치수, layerMask=-1, Ignore Triggers)
  - 자기 자신 CC 제외, HealthComponent 있는 콜라이더(캐릭터) 제외
  - 나머지 solid 콜라이더 있으면 unsafe → 실패
  - 정확한 위치 실패 시 s_tpOffsets[4]로 수직 샘플링 (±0.5m, ±1.0m)
  - 모두 실패 시 텔레포트 자체 실패

텔레포트 실행 (ExecuteTeleport):
  - CC.enabled=false → transform.position=dest → CC.enabled=true
  - cc.Move는 여전히 FirstPersonMotor.Tick에서만 호출됨
  - 도착 후 0.15s 동안 _passthroughTimer → SetDashPassthrough(true)
  - 타이머 만료 시 TryEndZPassthrough() → dash 없으면 passthrough 해제

쿨타임/재입력 정책:
  - _chargesUsed >= mageTeleportMaxCharges(2) → 쿨타임 시작, window 닫힘
  - recast window 만료 → 쿨타임 시작, _chargesUsed 초기화
  - 사망/ForceCleanup → ClearTeleportState (passthrough 즉시 해제)

passthrough 보호 (TryClearPassthrough):
  - AbilityController.TryClearPassthrough()에 _mageHandler.IsPassthroughActive 조건 추가
  - 다른 스킬이 dash passthrough를 끝낼 때 Mage passthrough 중이면 유지됨

UI (AbilityDebugUI):
  - RC 슬롯 (index 0): case 0 → UpdateRCSlot() → IsMageRCMode이면 UpdateMageRCSlot()
  - recast window 중: teal-blue, "TP2\nX.Xs"
  - 쿨타임 중: 어두운 회색, "X.Xs"
  - 준비: 초록, "READY"
  - 비-Mage: 기존 UpdateStdSlot(0) fallback
```

### Mage F — 광범위 레이저 (Wide Area Laser)

```
구현 파일: MageAbilityHandler.cs / GameConfig.cs / AbilityController.cs / AbilityDebugUI.cs

동작:
  - F 입력 → TryBeginLaser() → _isCastingLaser=true, FCooldown=18s
  - 0.5s 시전 후 BeginLaserActive(): _isLaserActive=true, _laserTimer=5s, _laserTickTimer=0 (즉시 첫 틱)
  - 활성 중 매 프레임: SetSelfMoveSpeedMultiplier(0f, tickInterval+0.05s) 갱신 → 이동 잠금
  - 활성 중 매 프레임: UpdateLaserVisual() → LaserVisualRoot 위치/회전을 카메라 방향으로 갱신
  - 0.5s마다 FireLaserTick() 호출
  - 5s 후 ClearLaserState(): 이동 잠금 해제(ClearSelfMoveSpeedMultiplier) + 시각 오브젝트 파괴
  - 활성 중 모든 다른 스킬(RC/Q/E/R/Z) 차단: TryActivate 최상단 if (_isLaserActive) return false

활성 중 다른 스킬 차단 이유:
  - 레이저는 이동 잠금 채널 스킬 → 텔레포트/Q/E와 동시 사용하면 이동 잠금이 해제될 위험
  - 사망 이벤트로 HandleOwnerDeath → ClearLaserState (이동 잠금 즉시 해제) 보장

FireLaserTick():
  - OverlapCapsuleNonAlloc(camera_pos → camera_pos + forward*range, radius, attackLayerMask)
  - 각 collider → GetComponent<HealthComponent>() / GetComponentInParent<HealthComponent>()
  - 자신/아군/비타깃 제거
  - dedup: _hitThisTick[8] (인스턴스 배열, 이번 틱 처리된 HC 목록)
  - LoS raycast: origin→target_torso 방향으로 Physics.Raycast → 첫 번째 hit이 target HC인지 확인
    → 다른 오브젝트가 먼저 맞으면 스킵 (벽 뒤 적 제외)
  - TakeDamage(mageLaserDamage=5) + ApplyKnockback(수평 레이저 방향, mageLaserKnockbackSpeed=5, tickInterval)

시각 효과:
  - SpawnLaserVisual(): 큐브 프리미티브 (충돌 제거), scale=(d, d, range), 보라색 sharedMaterial s_laserMat
  - 부모(_laserVisualRoot) 위치 = camera_pos + forward * (range/2), 회전 = LookRotation(forward)
  - 매 프레임 UpdateLaserVisual()로 카메라 방향 추종

UI (AbilityDebugUI F 슬롯):
  - IsMageRCMode → UpdateMageFSlot() 우선 분기
  - 시전 중: deep purple, "CAST\nX.Xs"
  - 활성 중: bright purple, "LASER\nX.Xs"
  - 쿨타임: "X.Xs" / READY
  - 변경 시에만 문자열 갱신 (_lastMageFCasting, _lastMageFActive, _lastMageFCastT, _lastMageFActiveT 추적)

테스트:
  1. F 누름 → 0.5s 시전 → 보라 빔 비주얼 생성, WASD 이동 불가, 카메라 회전 허용 확인
  2. 레이저가 카메라 방향을 실시간 추종하는지 확인
  3. 0.5s마다 적(봇) 피해 5 + 넉백 확인
  4. 벽 뒤 적에게 피해 없는지 확인
  5. 5s 후 빔 사라지고 이동 복구 확인
  6. 레이저 활성 중 RC/Q/E 입력 차단 확인
  7. 레이저 중 사망 → 즉시 이동 복구 + 빔 사라짐 확인
  8. AbilityDebugUI F 슬롯: CAST X.Xs → LASER X.Xs → 18s 쿨타임 → READY 순서 확인
```

### Mage Z — Arcane Bolt (아케인 볼트)

```
구현 파일: MageAbilityHandler.cs / MageArcaneBoltProjectile.cs / AbilityController.cs
          AbilityDebugUI.cs / PlayerCommand.cs / PlayerInputReader.cs / GameConfig.cs

입력 흐름:
  Z 키 → TryBeginAiming() → _zState = Aiming
  Aiming 중 좌클릭(AttackPressed) → Charging, _chargeTimer = 0
  Charging 중 좌클릭 홀드(AttackHeld) → _chargeTimer += dt (최대 mageArcaneBoltMaxChargeTime=2s)
  Charging 중 좌클릭 릴리즈(AttackReleased) → FireArcaneBolt(), _zState = Idle, 쿨타임 8s 시작
  Aiming/Charging 중 Z 재입력 → CancelZState() (쿨타임 미소모)

취소/유지 정책:
  - Z 키 재입력: TryActivate(Z) → CancelZState() (쿨타임 미소모)
  - RC 입력:     TryActivate(RC) → TryRC() 그대로 실행, Z 상태는 유지됨.
                 텔레포트 성공/실패 무관하게 Aiming/Charging 유지.
                 텔레포트 후 좌클릭 release 시 현재 카메라 방향으로 발사.
  - 레이저 활성: TryActivate 최상단 if (_isLaserActive) return false → Z 진입 차단
  - 사망/ForceCleanup: CancelZState() 자동 호출
  - 스턴/사망: TickTimers()에서 _zState != Idle && (IsDead || IsStunned) → CancelZState()
               (Tick이 스턴 중 미호출되므로 TickTimers 내부에서 정리)
  - Q/E/F/R 입력: _zState != Idle 가드로 차단 (취소 아님, 무시)

공격 차단 (ShouldBlockBasicAttack):
  MageAbilityHandler.IsBlockingBasicAttack = _zState != Idle
  AbilityController.ShouldBlockBasicAttack = _mageHandler.IsBlockingBasicAttack
  LocalPlayerController: if (!_ability.ShouldBlockBasicAttack) _attack.Tick(cmd)
  (Mage는 BasicAttackController에서도 Mage 가드로 기본 공격 차단 — 이중 보호)

충전 비율 계산:
  chargeRatio = Clamp01(_chargeTimer / mageArcaneBoltMaxChargeTime)
  damage = Lerp(minDamage=5,  maxDamage=20,  chargeRatio)
  speed  = Lerp(minSpeed=8,   maxSpeed=30,   chargeRatio)  (m/s)
  stun   = Lerp(minStun=0.2,  maxStun=1.5,   chargeRatio)  (s)
  → 즉시 릴리즈: 피해 5, 속도 8 m/s, 스턴 0.2s
  → 2s 풀 충전: 피해 20, 속도 30 m/s, 스턴 1.5s

투사체 (MageArcaneBoltProjectile.cs):
  Init(ownerTeam, ownerHealth, damage, stunDuration, speed, range, radius, direction, layerMask)
  스폰: camera.position + forward * 0.3m, 파란 구체(0.10, 0.35, 1.0), radius=0.35m 크기
  Update: SphereCastNonAlloc → 가장 가까운 유효 히트 선택
    - HealthComponent 있음 → 자신/아군/사망 skip → TakeDamage + ApplyStun + Destroy
    - HealthComponent 없음 → 환경 → Destroy
  rangeRemaining -= speed*dt → 0 이하 → Destroy (최대 사거리 18m)
  static RaycastHit[] s_hits[8] 공유 (Update는 단일 스레드이므로 안전)

비주얼:
  Sphere primitive, 콜라이더 제거 (자체 SphereCast 사용)
  파란색 (0.10, 0.35, 1.0) sharedMaterial (s_boltMat 정적 캐시)
  크기 = radius * 2 = 0.7m diameter (충전 비율 무관하게 고정)

UI (AbilityDebugUI Z 슬롯, index 5):
  IsMageRCMode → UpdateMageZSlot() 우선 분기, 아니면 UpdateStdSlot(5)
  Aiming 중: 파란색(ColAimBolt), "AIM"
  Charging 중: 밝은 파란색(ColChargeBolt), "CHG\nX.Xs" (누적 충전 시간)
  쿨타임 중: "X.Xs" / READY
  변경 시에만 문자열 갱신 (_lastMageZAiming, _lastMageZCharging, _lastMageZChargeT 추적)

입력 구조 추가:
  PlayerCommand.cs: AttackHeld (bool), AttackReleased (bool) 추가
  PlayerInputReader.cs: GetButton("Fire1"), GetButtonUp("Fire1") 추가
  (AttackPressed는 기존부터 존재)
```

### Mage 패시브 — Q 탄약 회복 + 큰 화염구 효과

```
구현 파일: MageAbilityHandler.cs / MageFireballProjectile.cs / GameConfig.cs

패시브 — Q 탄약 회복:
  비Q 스킬이 실제로 성공했을 때만 AddFireballAmmoFromPassive() 호출
  _ammo = Mathf.Min(_ammo + magePassiveAmmoGain, mageFireballMaxAmmo)
  이미 최대면 변화 없음

  호출 시점:
    RC: TryPerformTeleport() 성공 직후 (텔레포트당 1회, 2연속 시 최대 +6)
    E:  TryBeginBlackhole() 캐스팅 시작 직후
    F:  TryBeginLaser() 캐스팅 시작 직후
    Z:  FireArcaneBolt()에서 SetCooldown 직후 (Aiming 진입/취소로 회복 불가)
    R:  TryBeginR() SpawnMeteorOrbSequence 전에 호출
    Q:  회복 없음

큰 화염구 추가 효과:
  MageFireballProjectile.Init에 stunDuration/knockbackSpeed/knockbackDuration 주입
  일반 화염구: 세 값 모두 0f → 기존 피해만 적용 (조건 분기 없이 값으로 제어)
  큰 화염구 적중 시 (bestHC != null):
    1. TakeDamage (기존 동일)
    2. _stunDuration > 0  → ApplyStun(stunDuration)
    3. _knockbackSpeed > 0 && _knockbackDuration > 0
       → kbDir = _direction, y=0, 너무 작으면 forward fallback
       → ApplyKnockback(kbDir.normalized * kbSpeed, kbDuration)
  아군/본인/환경 충돌은 기존 검사로 이미 걸러짐
```

| 필드 | 기본값 | 설명 |
|------|--------|------|
| `magePassiveAmmoGain` | 3 | 비Q 스킬 성공 시 Q 탄약 회복량 |
| `mageBigFireballStunDuration` | 0.5 s | 큰 화염구 적중 스턴 |
| `mageBigFireballKnockbackSpeed` | 4.0 m/s | 큰 화염구 넉백 속도 |
| `mageBigFireballKnockbackDuration` | 0.25 s | 큰 화염구 넉백 지속 |

### Mage R — 운석 심판 (Meteor Judgment)

```
구현 파일: MageAbilityHandler.cs / MageMeteorOrbSequence.cs / MageMeteorStorm.cs
          MageMeteorProjectile.cs / AbilityController.cs / AbilityDebugUI.cs / GameConfig.cs

동작:
  R 입력 → TryBeginR() → RCooldown=30s 즉시 시작, _isRCasting=true, _rOrbTimer=2s
  → SpawnMeteorOrbSequence(): 붉은 구체(1m) 시전자 앞 1.5m에서 생성
  → MageMeteorOrbSequence.Update(): 2초 동안 15m 상승 (Lerp t)
  → 2초 후: 구체 파괴 + MageMeteorStorm 생성 + OrbSeq 자기 파괴
  → MageMeteorStorm.Update(): 0.2s 딜레이 후 20발 유성, 3s 동안 0.15s 간격으로 생성
  → MageMeteorProjectile: 경고 디스크(0.45s) → 낙하(0.45s) → 충돌 피해 30

입력 제약:
  - R 시전 중 이동/점프/회전/기본공격/다른 스킬 모두 허용
  - R 자체는 쿨타임(30s)으로 중복 시전 방지
  - Z Aiming/Charging 중이면 R이 차단됨 (Z 블록 정책 유지)
  - 레이저 활성 중 R 차단 (TryActivate 최상단 if (_isLaserActive) 가드)

스톰 중심 계산:
  - 시전 시점 카메라 전방 XZ 벡터 * mageMeteorAimDistance(10m) + 시전자 위치
  - 시전 후 시전자 이동/회전과 무관하게 고정

유지/취소 정책:
  - 생성된 OrbSeq/Storm/Meteor는 시전자 사망 시에도 계속 진행 (취소 없음)
  - HandleOwnerDeath: _isRCasting=false, _rOrbTimer=0 (UI 정리만, 월드 오브젝트 유지)
  - _isRCasting은 UI "CAST" 표시 전용 (2s 후 자동 false)

MageMeteorOrbSequence (월드 오브젝트):
  - Init(ownerTeam, ownerHealth, config, stormCenter) — AddComponent 직후 호출
  - Update: _riseTimer += dt, t = Clamp01(t / riseDuration)
  - _orbGo(별도 GameObject) 위치 = riseStart + up * (riseHeight * t)
  - t >= 1: orbGo 파괴 → SpawnStorm() → self-Destroy

MageMeteorStorm (월드 오브젝트):
  - Init() 후 Update에서 while 루프로 유성 생성 (대형 프레임 드랍 대응)
  - interval = stormDuration / count = 3.0 / 20 = 0.15s
  - 첫 유성: stormDelay(0.2s) 후
  - selfDestructTime = delay + duration + warningDuration + fallDuration + 0.5s = 4.6s

MageMeteorProjectile (월드 오브젝트):
  - Init(ownerTeam, ownerHealth, config, impactPos)
  - SpawnWarningIndicator(): 납작 구체 디스크 (impactRadius*2, y=0.05f, height=0.05f), 주황-적색
  - Update Phase 1 (warningDuration=0.45s): startPos 대기
  - Update Phase 2 (fallDuration=0.45s): Lerp(startPos → impactPos)
  - Impact(): OverlapSphereNonAlloc(impactPos, impactRadius, s_cols[16], attackLayerMask)
    → 자신 제외, 아군 제외, IsTargetable 체크, s_hcBuf[16] dedup (per-meteor)
    → TakeDamage(mageMeteorDamage=30)
  - OnDestroy: warningGo 정리 (안전망)
  - static Material s_meteorMat (주황-적), s_warningMat (어두운 적), GetMeteorMat() public static

UI (AbilityDebugUI R 슬롯, index 3):
  - UpdateRSlot → IsMageRCMode이면 UpdateMageRSlot() 분기
  - _isRCasting 중: dark red-orange (ColMeteor), "CAST"
  - 쿨타임 중: "X.Xs" / READY
  - 변경 시에만 갱신 (_lastMageRCasting, _lastCdTenths[3] 추적)
```

### GameConfig Mage R Meteor Judgment 수치
| 필드 | 기본값 | 설명 |
|------|--------|------|
| `mageMeteorOrbRiseDuration` | 2.0 s | 구체 상승 시간 |
| `mageMeteorOrbRiseHeight` | 15.0 m | 구체 상승 높이 |
| `mageMeteorStormDelay` | 0.2 s | 구체 폭발 후 첫 유성까지 딜레이 |
| `mageMeteorStormDuration` | 3.0 s | 전체 유성 생성 시간 |
| `mageMeteorStormRadius` | 15.0 m | 스톰 반경 (지름 30m) |
| `mageMeteorCount` | 20 | 유성 수 |
| `mageMeteorImpactRadius` | 1.5 m | 유성 충돌 피해 반경 |
| `mageMeteorDamage` | 30.0 | 유성 1개당 피해 |
| `mageMeteorFallHeight` | 15.0 m | 유성 낙하 시작 높이 |
| `mageMeteorFallDuration` | 0.45 s | 유성 낙하 시간 |
| `mageMeteorWarningDuration` | 0.45 s | 경고 디스크 대기 시간 |
| `mageMeteorVisualRadius` | 0.35 m | 유성 구체 반지름 |
| `mageMeteorAimDistance` | 10.0 m | 스톰 중심까지 거리 |
| `mageAbility.RCooldown` | 30 s | 쿨타임 (시전 즉시 시작) |

### GameConfig Mage Z Arcane Bolt 수치
| 필드 | 기본값 | 설명 |
|------|--------|------|
| `mageArcaneBoltMaxChargeTime` | 2.0 s | 풀 충전 도달 시간 |
| `mageArcaneBoltMinSpeed` | 8.0 m/s | 즉시 릴리즈 속도 |
| `mageArcaneBoltMaxSpeed` | 30.0 m/s | 풀 충전 속도 |
| `mageArcaneBoltRange` | 18.0 m | 최대 사거리 |
| `mageArcaneBoltRadius` | 0.35 m | 투사체 구 반지름 |
| `mageArcaneBoltMinStun` | 0.2 s | 즉시 릴리즈 스턴 |
| `mageArcaneBoltMaxStun` | 1.5 s | 풀 충전 스턴 |
| `mageArcaneBoltMinDamage` | 5.0 | 즉시 릴리즈 피해 |
| `mageArcaneBoltMaxDamage` | 20.0 | 풀 충전 피해 |
| `mageAbility.ZCooldown` | 8 s | 발사 후 쿨타임 (취소 시 미소모) |

### GameConfig Mage F 레이저 수치
| 필드 | 기본값 | 설명 |
|------|--------|------|
| `mageLaserCastTime` | 0.5 s | 빔 시작 전 시전 딜레이 |
| `mageLaserDuration` | 5.0 s | 빔 지속 시간 |
| `mageLaserTickInterval` | 0.5 s | 피해/넉백 판정 간격 |
| `mageLaserRange` | 15.0 m | 빔 길이 |
| `mageLaserRadius` | 2.0 m | 빔 히트 반경 (캡슐 판정) |
| `mageLaserDamage` | 5.0 | 틱당 피해량 |
| `mageLaserKnockbackSpeed` | 5.0 m/s | 틱당 넉백 속도 |
| `mageLaserVisualRadius` | 1.0 m | 비주얼 빔 반경 |
| `mageAbility.FCooldown` | 18 s | 쿨타임 (asset 직렬화 완료) |

### GameConfig Mage 수치
| 필드 | 기본값 | 설명 |
|------|--------|------|
| `mageTeleportDistance` | 6.0 m | 텔레포트 이동 거리 |
| `mageTeleportRecastWindow` | 0.5 s | 2회째 입력 가능 창 |
| `mageTeleportMaxCharges` | 2 | 1세트당 최대 텔레포트 횟수 |
| `mageTeleportPassthroughDuration` | 0.15 s | 도착 후 캐릭터 충돌 통과 시간 |
| `mageAbility.RightClickCooldown` | 8 s | 쿨타임 |

### Warrior R — 거검 강림 (Great Sword Descent)

```
구현 파일: WarriorAbilityHandler.cs / GameConfig.cs / HealthComponent.cs / AbilityController.cs / AbilityDebugUI.cs

동작:
  - R 입력 → TryR() → _rState=Casting, RCooldown=30s
  - _rOrigin = 시전 시점 플레이어 위치 (판정 앵커 — 이동해도 유지)
  - 시전 시작 즉시 SetAbilityInvulnerable(true) → 무적
  - 1.9s 상승 (SelfMoveVelocity = up * 5/1.9 ≈ 2.63 m/s): 플레이어가 5m 위로 올라감
  - 0.1s 낙하 (SelfMoveVelocity = down * 5/0.1 = 50 m/s): 드라마틱한 급강하
  - 2.0s 후 FireRImpact() → _rOrigin 기준 15x15m 박스 내 적 전원 3초 기절 (피해 없음)
  - ClearRState() → 이동 속도 0, 무적 해제, 이펙트 정리
  - 시전 중 Q/RC/Z 신규 발동 차단. F/E는 동시 사용 가능.
  - 사망/비활성화 시 ClearRState()로 무적+이동+이펙트 모두 정리.

_rOrigin 정책:
  - TryR() 호출 시 _ownerHealth.transform.position을 _rOrigin에 저장
  - FireRImpact의 박스 중심 = _rOrigin + up * standHeight * 0.5
  - SpawnRVisual의 root = _rOrigin
  - 시전 중 플레이어가 어디로 이동해도 판정+시각화는 _rOrigin 고정

강제 수직 이동 (SelfMoveVelocity):
  - R이 Casting이고 _rMoveVelocity.sqrMagnitude > 0.001: _rMoveVelocity 반환 (최우선)
  - 그다음 Q 상승/낙하 흐름
  - DashVelocity.y != 0이므로 FirstPersonMotor에서 gravity 미적용 (올바름)
  - DashVelocity.sqrMagnitude > 0 → horizontal 오버라이드 → 시전 중 WASD 차단됨 (의도된 동작)

무적 두 레이어:
  - _respawnInvul: RespawnController.SetInvulnerable(bool) 전용
  - _abilityInvul: SetAbilityInvulnerable(bool) 전용 (warrior R 등)
  - IsInvulnerable = _respawnInvul || _abilityInvul
  - 리스폰 무적과 R 무적이 서로 독립적 — 한쪽을 끄더라도 다른 쪽 유지
  - Initialize/Reinitialize: 둘 다 false로 초기화

15x15m 박스 판정 (FireRImpact):
  - center = _rOrigin + up * (standHeight * 0.5f)  ← 현재 위치 아님
  - halfExtents = (7.5, 2.5, 7.5) → warriorRAreaSize=15m, warriorRAreaHeight=5m
  - Physics.OverlapBoxNonAlloc (Quaternion.identity, attackLayerMask, QueryTriggerInteraction.Ignore)
  - 아군 제외, 자기 자신 제외, s_hitCache[16] dedup
  - ApplyStun(warriorRStunDuration=3s) — 피해 없음 (이번 단계)

임시 이펙트:
  SpawnRVisual() — TryR() 시 호출, floor marker만 생성:
    - _rSwordRoot를 _rOrigin에 배치
    - floor: 15×0.1×15 황금색 큐브 (AoE 예고 영역)
  SpawnRSword() — 낙하 시작(t=1.9s)에 TickTimers에서 한 번 호출:
    - pivot.localPosition = (0, warriorRRiseHeight + bladeH*0.5, 0) — 상승 최고점 위에서 시작
    - blade: 강철색 큐브, guard/handle: 황금색 (모두 sharedMaterial 캐시)
    - TickTimers에서 0.1s 동안 localY를 Lerp(시작Y, bladeH*0.35, progress)로 하강 애니메이션
  _rSwordRoot Destroy → 자식(floor + sword pivot) 일괄 정리

Q/RC/Z 차단:
  - TryRCDash, TryQ, TryZ: _rState == RState.Casting → return false 가드 추가
  - F와 E는 차단하지 않음 (동시 활성 허용)

UI (AbilityDebugUI):
  - IsWarriorGuardMode(= 전사 여부) 기준 UpdateWarriorRSlot() 분기
  - 시전 중: purple, "ULT\nX.Xs" (남은 시전 시간)
  - 대기 중: 일반 쿨타임/READY 표시

나중에 궁극기 게이지 시스템으로 교체할 때:
  - TryR() 쿨타임 체크 부분만 게이지 체크로 교체하면 됨
  - 시전/무적/이펙트/판정 로직은 유지
```

### GameConfig Warrior R 수치
| 필드 | 기본값 | 설명 |
|------|--------|------|
| `warriorRCastTime` | 2.0 s | 시전 시간 (rise 1.9s + drop 0.1s) |
| `warriorRRiseDuration` | 1.9 s | 상승 지속 시간 |
| `warriorRDropDuration` | 0.1 s | 낙하 지속 시간 |
| `warriorRRiseHeight` | 5.0 m | 상승 높이 (낙하 시 동일 거리 복귀) |
| `warriorRAreaSize` | 15.0 m | 정사각형 AoE 한 변 길이 |
| `warriorRAreaHeight` | 5.0 m | 박스 높이 (중심 위아래 2.5m) |
| `warriorRStunDuration` | 3.0 s | 기절 지속 시간 |
| `warriorRSwordScaleMultiplier` | 10.0 | 검 비주얼 스케일 배수 |
| `warriorAbility.RCooldown` | 30 s | 쿨타임 |

### Warrior F — 난도질 (Slash Barrage)

```
구현 파일: WarriorAbilityHandler.cs / GameConfig.cs / AbilityController.cs / AbilityDebugUI.cs

동작:
  - F 입력 → TryF() → _fState=Casting, FCooldown=12s
  - 2.0s 시전: 0.1s마다 시각 이펙트(SpawnFVisual), 0.5s마다 피해 판정(FireFHit)
  - 총 4회 피해 판정. 1~3회: warriorFDamage=5, 마지막: warriorFFinalDamage=10 + knockback
  - 시전 중 재입력 무시. 다른 스킬(Q/RC/Z)과 동시 사용 가능.
  - 사망/비활성화 시 ClearFState()로 정리.

전방 반구 판정 (FireFHit):
  - center = owner.position + up * (standHeight * 0.5f)
  - Physics.OverlapSphereNonAlloc(center, 3.5m, ...)
  - 각 Collider에 closest = ClosestPoint(center), dir = closest - center
  - Vector3.Dot(cameraForward3D, dir) >= 0 → 전방 반구 포함 → 피해
  - Dot < 0 → 후방 → 제외. dir.y 제거하지 않음 (3D 반구)
  - cameraForward.y=0 성분이 너무 작으면 (< sqrt(0.1)) owner.transform.forward 사용
  - 같은 tick에서 같은 HealthComponent는 1회만 피해 (s_fHitCache[8] per-tick dedup)
  - 다음 0.5s tick에서는 같은 대상 다시 피해 가능

이동속도 감소:
  - SetSelfMoveSpeedMultiplier(0.5f, 2.0s) → HealthComponent.SelfMoveSpeedMultiplier 구조
  - E 가드와 동시 시전 시:
    - F 시작: min(0.7, 0.5) = 0.5 (강한 쪽 우선), timer = max(E_remaining, 2.0)
    - F 종료 → ClearFState: Clear → E 아직 활성이면 SetSelfMoveSpeedMultiplier(0.7, guardTimer) 재적용
    - E 종료 → EndGuard: Clear → F 아직 활성이면 SetSelfMoveSpeedMultiplier(0.5, fRemaining) 재적용

시각 이펙트 (SpawnFVisual):
  - 0.1s마다 전방에 얇은 대각선 큐브 (scale 0.12×2.0×0.12, 은백색)
  - 짝수 tick: 오른쪽 (+50° Z회전), 홀수: 왼쪽 (-50° Z회전) 교대
  - 0.12s 후 Destroy (lifetime = interval + 0.02s)
  - sharedMaterial 캐시(s_fMat), 매 프레임 생성 없음

UI (AbilityDebugUI):
  - IsWarriorGuardMode(= 전사 여부) 기준 UpdateWarriorFSlot() 분기
  - 시전 중: dark red, "SLSH\nX.Xs" (남은 시전 시간)
  - 대기 중: 일반 쿨타임/READY 표시
```

### GameConfig Warrior F 수치
| 필드 | 기본값 | 설명 |
|------|--------|------|
| `warriorFDuration` | 2.0 s | 총 시전 시간 |
| `warriorFDamageInterval` | 0.5 s | 피해 판정 간격 (4회) |
| `warriorFVisualInterval` | 0.1 s | 시각 이펙트 간격 |
| `warriorFDamage` | 5 | 일반 틱 피해 |
| `warriorFFinalDamage` | 10 | 마지막(4번째) 틱 피해 |
| `warriorFRadius` | 3.5 m | 전방 반구 반경 |
| `warriorFMoveSpeedMultiplier` | 0.5 | 시전 중 이속 배율 |
| `warriorFFinalKnockback` | 1.5 m/s | 마지막 틱 넉백 |
| `warriorAbility.FCooldown` | 12 s | 쿨타임 |

### StatusEffectMask + 스턴 시스템

```
StatusEffectMask: [Flags] enum — None=0, Stunned=1<<0 (확장 가능)
HealthComponent 필드: _activeEffects, _stunTimer
HealthComponent.Update(): _stunTimer 감소 → 0 도달 시 Stunned 플래그 해제
ApplyStun(duration): Mathf.Max로 기존 타이머와 비교 → 긴 쪽 유지
Reinitialize(): 리스폰 시 _activeEffects, _stunTimer 초기화

스턴 차단 위치:
  LocalPlayerController: 스턴 시 cmd 이동/스킬 입력 전부 0, dashVel=0, 카메라 회전은 허용
  BasicAttackController: attackerHealth.IsStunned → 공격 차단
  BotController: IsStunned → vertMotion만 적용하고 return
```

**카메라 회전 스턴 중 허용 이유:** FirstPersonCamera.Tick은 LocalPlayerController에서 스턴 체크 밖에서 호출됨. 스턴 중에도 카메라 회전을 허용하는 것이 FPS 장르 관례이고(몸은 굳어있지만 시야는 유지), 별도 차단 로직 없이 자연스럽게 동작한다.

### Rogue Z 순간이동 + 스턴 폭탄

```
TryRogueZ():
  ① 쿨타임 체크 (_cooldowns[Z])
  ② backward = -cameraFwd (수평화)
  ③ TryFindTeleportDestination(): rogueZTeleportDistance부터 1m까지 1m 단위로 시도 (배열 없음, 루프 계산)
     IsSafeDestination(): OverlapCapsuleNonAlloc → HC 없는 콜라이더 = 지형 = 실패
  ④ 안전 위치 없으면 → return false (쿨타임 소모 없음)
  ⑤ bombOrigin = 현재 위치 캡처
  ⑥ PerformTeleport(dest): CC.enabled=false → transform.position=dest → CC.enabled=true
  ⑦ SetDashPassthrough(true) + _zPassthroughTimer = rogueZPassthroughDuration (0.2s)
  ⑧ SpawnStunBomb(bombOrigin): fuseTime 후 반경 내 적 피해+스턴
  ⑨ _cooldowns[Z] = GetBaseCooldown(Z) = _abilityConfig.ZCooldown (10s)

PerformTeleport 설계 근거:
  - CC.Move 단일 흐름 규칙: Move는 여전히 FirstPersonMotor.Tick에서만 호출
  - transform.position 직접 설정이 Unity CC 텔레포트의 표준 방식 (CC disable → move → enable)
  - AbilityController.Tick → ability.Tick(cmd) → PerformTeleport → 이후 motor.Tick 순서 보장

텔레포트 후 캐릭터 겹침 처리 (_zPassthroughTimer):
  - Z 텔레포트 성공 직후 SetDashPassthrough(true) → 0.2s 동안 캐릭터 충돌 무시
  - 타이머 만료 시 TryClearPassthrough() 호출 → _isDashing도 false이면 SetDashPassthrough(false)
  - RC 대시 중 Z 사용 시: 대시 종료가 먼저 오더라도 _zPassthroughTimer > 0이면 passthrough 유지
  - OnDisable/OnDestroy: _zPassthroughTimer = 0 + SetDashPassthrough(false) 강제 정리

ZCooldown 구조:
  - ClassAbilityConfig.ZCooldown 필드 추가 (Serializable struct)
  - rogueAbility.ZCooldown = 10 (GameConfig.asset 직렬화)
  - GetBaseCooldown(AbilitySlot.Z) → _abilityConfig.ZCooldown 반환
  - 기존 config.rogueZCooldown 필드 제거 — rogueAbility.ZCooldown으로 통합
  ⚠ Inspector 확인 필요: Unity에서 GameConfig.asset을 열어 rogueAbility.ZCooldown = 10 확인

RogueStunBomb.Update(): fuseTimer 감소 → 0 이하 → Explode() → Destroy(gameObject)
Explode(): OverlapSphereNonAlloc → 적에게 TakeDamage + ApplyStun → SpawnBlastVisual (0.15s 오렌지 구체)
```

### GameConfig Rogue Z 수치 전체
| 필드 | 기본값 | 설명 |
|------|--------|------|
| `rogueZTeleportDistance` | 6 m | 후방 텔레포트 최대 거리 (이 값~1m 사이 1m 단위로 탐색) |
| `rogueZBombFuseTime` | 0.3 s | 폭발 딜레이 |
| `rogueZBlastRadius` | 2 m | 폭발 반경 |
| `rogueZDamage` | 3 | 테스트 피해량 |
| `rogueZStunDuration` | 1 s | 스턴 지속 시간 |
| `rogueZPassthroughDuration` | 0.2 s | 텔레포트 후 캐릭터 충돌 무시 시간 |
| `rogueAbility.ZCooldown` | 10 s | Z 쿨타임 (ClassAbilityConfig 구조체, asset 직렬화) |

### Rogue E 상태머신 (E1 → E2)

```
E 상태: RogueEState { Idle, MarkedWaiting, Dashing }

E1 (TryActivateRogueE, Idle 상태):
  - 쿨타임이 0이어야 발동 가능
  - FireEProjectile() 호출 → RogueMarkedShurikenProjectile 스폰
  - 쿨타임 시작 안 함 (E2 대기)

RogueMarkedShurikenProjectile.Update():
  - 방향으로 이동, OverlapSphereNonAlloc 판정
  - 아군/사망 → 통과, 적 → 피해 + OnEProjectileHit() 콜백
  - 지형(HC 없음) → 소멸

OnEProjectileHit():
  → ApplyEMark(target): 상태 MarkedWaiting, markTimer = 3s, 빨간 구체 표식 생성

E2 (TryActivateRogueE, MarkedWaiting 상태):
  - 쿨타임이 0이어야 발동 가능
  - StartE2Dash(): dest = target.pos - target.fwd * 1.5m, StartDash(vel, dur)
  - 상태 Dashing, 쿨타임 = ECooldown (8s) 시작

Update() 동안 Dashing:
  - Tick() 가드: _rogueEState == Dashing → 모든 어빌리티 입력 차단
  - DashVelocity = _dashHorizontalVelocity → FirstPersonMotor 오버라이드

OnDashEnded() (대시 타이머 만료):
  - Dashing 상태 → Idle로 전환 (쿨타임은 이미 시작됨)
  - 非-Dashing → RC 대시 종료 → 균열 생성

표식 만료 (MarkedWaiting 상태에서 markTimer ≤ 0):
  → ClearEMark(): Idle로 전환, 표식 시각화 제거, 쿨타임 없음 (즉시 E1 재사용 가능)
```

**ECooldown 정책:** E2 성공 → 8s 쿨타임. E1만 맞히고 표식 만료 → 쿨타임 없음 (E1 즉시 재사용).

### GameConfig Rogue E 수치 전체
| 필드 | 기본값 | 설명 |
|------|--------|------|
| `rogueEDamage` | 10 | E1 표창 피해량 |
| `rogueEProjectileSpeed` | 18 m/s | 표창 이동 속도 |
| `rogueERange` | 15 m | 표창 최대 비행 거리 |
| `rogueEMarkDuration` | 3 s | 표식 지속 시간 |
| `rogueEDashSpeed` | 20 m/s | E2 강제 이동 속도 |
| `rogueEArrivalOffset` | 1.5 m | 타겟 뒤쪽 도착 거리 |
| `rogueAbility.ECooldown` | 8 s | E2 성공 후 쿨타임 |

### Rogue Q 2충전 시스템

```
게임 시작:  _rogueQCharges = rogueQMaxCharges (2)

Q 사용 (TryActivateRogueQ):
  ① _rogueQLockoutTimer > 0  → 차단
  ② _rogueQCharges <= 0     → 차단
  ③ charges == maxCharges   → 리차지 타이머 시작 (QCooldown)
     charges < maxCharges   → 타이머 이미 진행 중, 리셋 안 함
  ④ charges--, lockoutTimer = rogueQCastLockout (1s)
  ⑤ LaunchGiantShuriken() → 거대 표창 장판 생성

Update():
  charges < max 동안:
    rechargeTimer -= dt
    → 0이 되면 charges++
    → 아직 max 미만이면 타이머 다시 시작 (QCooldown)
    → max 도달하면 타이머 0으로 정지
  lockoutTimer > 0이면:
    lockoutTimer -= dt (Mathf.Max 0f)
```

**핵심:** 리차지 타이머는 충전이 줄어드는 순간에만 시작되고 (max→max-1), 연속 사용에서는 타이머를 리셋하지 않는다. 즉, 충전 2→1→0 연속 사용 시 두 번째 사용은 타이머를 덮어쓰지 않고 이미 진행 중인 타이머를 이어간다.

### GameConfig Rogue Q 수치 전체
| 필드 | 기본값 | 설명 |
|------|--------|------|
| `rogueQDamage` | 15 | 타겟당 피해량 |
| `rogueQRange` | 5m | OverlapSphere 반경 |
| `rogueQAngle` | 90° | 부채꼴 전체 각도 |
| `rogueQVisualDuration` | 0.15s | 시각화 GO 수명 |
| `rogueQMaxCharges` | 2 | 최대 충전 수 |
| `rogueQCastLockout` | 1s | 사용 후 재사용 잠금 시간 |
| `rogueAbility.QCooldown` | 5s | 충전 1개당 리차지 시간 |

### AbilityDebugUI Rogue Q 표시 형식
| 상태 | 표시 예 |
|------|---------|
| 충전 2/2 (만충) | `Q 2/2` |
| 충전 1/2, 리차지 4.5s | `Q 1/2 4.5` |
| 충전 0/2, 리차지 3.2s | `Q 0/2 3.2` |
| 충전 1/2, 잠금 0.8s | `Q 1/2 L:0.8` |

`UpdateRogueQLabel()` — charges/tenths/lockout 세 값이 변할 때만 string 재생성 (기존 BuildLabel과 동일한 패턴)

### AbilityController 공개 프로퍼티 (Rogue Q)
```csharp
public bool  IsRogueQChargeMode  // true if _combatClass == Rogue
public int   RogueQCharges       // 현재 충전 수
public int   RogueQMaxCharges    // config.rogueQMaxCharges
public float RogueQRechargeTimer // 다음 충전까지 남은 시간
public float RogueQLockoutTimer  // 다음 사용 가능까지 남은 시간
```

### Ability 쿨타임 전체 표
| 직업 | RightClick | Q | E | R | Z | 비고 |
|------|-----------|---|---|---|---|------|
| Warrior | 15s | 8s | 14s | — | 18s | RC 대시, Q 상승/하강 착지 강타, E 철벽 가드, Z 반격 후퇴기 구현 |
| Rogue | 12s | 충전제 | 8s(E2) | 10s | 10s | RC 균열, Q 거대 표창 장판, E1+E2, R 은신, F 3타 돌진 베기, Z 순간이동+스턴 폭탄 구현 |
| Archer | 0s | 0s | 0s | — | — | 미구현 |
| Mage | 8s | - | 12s | — | 18s | RC 2연속 텔레포트, Q 화염구 탄창, E 블랙홀, F 광범위 레이저, Z Arcane Bolt(8s) 구현. R 미구현 |

### AbilityController / RogueAbilityHandler 역할 분리 (22단계~)

```
AbilityController (공통 레이어):
  - 공개: Tick(cmd), TryActivate(slot), GetCooldownRemaining(slot)
  - 공개: IsDashing, DashVelocity (FirstPersonMotor용)
  - 공개: IsRogueQChargeMode, RogueQCharges 등 (AbilityDebugUI 위임 프로퍼티)
  - internal: StartDash(), SetDashPassthrough(), TryEndZPassthrough()
  - internal: GetCooldown(), SetCooldown(), AbilityConfig, GetBaseCooldown()
  - 보유 상태: _cooldowns[], _isDashing, _dashTimer, _dashHorizontalVelocity

RogueAbilityHandler (Rogue 전용 레이어):
  - AbilityController.Start()에서 AddComponent<RogueAbilityHandler>().Init(this,...) 생성
  - 보유 상태: Q 충전, E 상태머신, Z 타이머, 균열 스케줄, NonAlloc 버퍼, Material 캐시
  - AbilityController가 호출하는 콜백: TickTimers(dt), OnDashEnded(), HandleOwnerDeath(), ForceCleanup()
  - OnEProjectileHit(): RogueMarkedShurikenProjectile이 직접 호출

TryClearPassthrough 흐름 (RC 대시 + Z passthrough 공존 처리):
  대시 종료 → TryClearPassthrough() → _rogueHandler.IsZPassthrough 확인
    true  → 아직 Z 타이머 중 → passthrough 유지
    false → SetDashPassthrough(false)
  Z 타이머 종료 → _ac.TryEndZPassthrough() → _isDashing 확인
    true  → 아직 대시 중 → passthrough 유지
    false → SetDashPassthrough(false)
```

### Rogue R 은신 / 백어택 (24단계)

```
TryR():
  ① _ac.GetCooldown(R) > 0 → 차단
  ② _isStealthed → 차단 (이미 은신 중)
  ③ _isStealthed = true, _stealthTimer = rogueStealthDuration (5s)
  ④ _ac.SetCooldown(R, RCooldown) = 10s
  ⑤ _motor.MoveSpeedMultiplier = rogueStealthMoveSpeedMultiplier (2f)
  ⑥ 캐릭터 Renderer 전체 disabled (GetComponentsInChildren<Renderer> → Init에서 캐시)
  ⑦ 작은 보라 구체 (_stealthVisual) 머리 위 생성 (프로토타입 표시)

BreakStealth() [스킬 성공/공격/사망/만료]:
  ① _isStealthed = false, _stealthTimer = 0
  ② _motor.MoveSpeedMultiplier = 1f
  ③ Renderer 전체 enabled
  ④ _stealthVisual Destroy
  ⑤ ClearReveal() → _revealVisual Destroy, _isRevealed=false, Renderer re-hide if still stealthed

피격 중 노출 (OnTookDamage → ApplyReveal()):
  ① _isStealthed && !_isRevealed 조건에서만 실행
  ② _isRevealed = true, _revealTimer = rogueStealthRevealOnHitDuration (1s)
  ③ Renderer 전체 re-enabled (캐릭터 보임)
  ④ 노란 구체 (_revealVisual) 생성
  → 1s 후 ClearReveal(): revealVisual 제거, 아직 은신 중이면 Renderer 다시 hidden

백어택 (OnBasicAttackHit → IsBackstab()):
  ① OnAttackHit은 OnAttackUsed보다 먼저 발화 → _isStealthed가 아직 true
  ② IsBackstab: Vector3.Angle(target.forward, toAttacker) > (180 - rogueBackstabAngle * 0.5)
     rogueBackstabAngle=90 → 타겟 정후방 45도 이내에서 성공
  ③ 성공 시 target.TakeDamage(rogueBackstabBonusDamage, IgnoreArmor=true)
  → OnAttackUsed 발화 → BreakStealth()
  → BasicAttackController가 기본 피해 적용

이벤트 구독/해제:
  Init: basicAttack.OnAttackHit += OnBasicAttackHit; basicAttack.OnAttackUsed += OnBasicAttackUsed; ownerHealth.OnDamaged += OnTookDamage
  OnDestroy: -= 해제 + BreakStealth()
  ForceCleanup: BreakStealth()
  HandleOwnerDeath: BreakStealth()

스킬 사용 시 은신 해제 조건:
  TryRCDash, TryQ, TryE, TryZ: 쿨타임/조건 통과 직후(성공 확정 시점)에 BreakStealth() 호출
  TryR 자체는 BreakStealth 호출 안 함 (은신을 거는 스킬)
  온쿨/조건 미충족으로 실패 → 은신 유지
```

### GameConfig Rogue R 수치
| 필드 | 기본값 | 설명 |
|------|--------|------|
| `rogueStealthDuration` | 5 s | 은신 지속 시간 |
| `rogueStealthMoveSpeedMultiplier` | 2.0 | 은신 중 이동속도 배율 (MoveSpeedMultiplier) |
| `rogueStealthRevealOnHitDuration` | 1 s | 피격 시 노출 창 |
| `rogueBackstabBonusDamage` | 30 | 백어택 추가 피해 (방어 무시) |
| `rogueBackstabAngle` | 90° | 백어택 판정 후방 콘 각도 (45도×2) |
| `rogueAbility.RCooldown` | 10 s | R 쿨타임 |

### cc.Move 단일 호출 규칙 (유지)
FirstPersonMotor.Tick 내부에서만 호출.

### CombatClass enum 순서 (직렬화 값 고정)
```
Warrior = 0  Archer = 1  Rogue = 2  Mage = 3
```

## Last Completed

- **40단계: 마법사 Z Arcane Bolt** (Claude Code)
  - `PlayerCommand.cs`: `AttackHeld bool`, `AttackReleased bool` 추가.
  - `PlayerInputReader.cs`: `AttackHeld = GetButton("Fire1")`, `AttackReleased = GetButtonUp("Fire1")` 추가.
  - `GameConfig.cs`: `[Header("Mage — Arcane Bolt (Z)")]` 9개 필드. `mageAbility.ZCooldown = 8f` 기본값.
  - `GameConfig.asset`: arcane bolt 9개 필드 + `mageAbility.ZCooldown: 8` 직렬화.
  - `MageArcaneBoltProjectile.cs` (신규): `Init()` 패턴. `Update()`: SphereCastNonAlloc, 적 → TakeDamage + ApplyStun + Destroy, 환경 → Destroy, range 초과 → Destroy. static `s_hits[8]` 공유.
  - `MageAbilityHandler.cs`:
    - `private enum ZState { Idle, Aiming, Charging }` 추가.
    - 필드 추가: `_zState`, `_chargeTimer`, `s_boltMat`.
    - 프로퍼티 추가: `IsBlockingBasicAttack`, `IsZAiming`, `IsZCharging`, `ZChargeTimer`.
    - `TryActivate`: _zState != Idle 가드 추가 (RC/Z → CancelZState, 나머지 → return false). `AbilitySlot.Z → TryBeginAiming()` 라우팅 추가.
    - `TickZ(PlayerCommand cmd)` 신규: Aiming 중 AttackPressed → Charging. Charging 중 AttackReleased → FireArcaneBolt; AttackHeld → chargeTimer++.
    - `HandleOwnerDeath/ForceCleanup`: `CancelZState()` 추가.
    - 신규 메서드: `TryBeginAiming()`, `CancelZState()`, `FireArcaneBolt()`, `GetBoltMat()`.
    - `using Game.Commands` 추가.
  - `AbilityController.cs`: `MageIsZAiming`, `MageIsZCharging`, `MageZChargeTimer`, `ShouldBlockBasicAttack` 프로퍼티 추가. `Tick(cmd)` 끝에 `_mageHandler?.TickZ(cmd)` 추가.
  - `LocalPlayerController.cs`: `if (!_ability.ShouldBlockBasicAttack) _attack.Tick(cmd)` 추가.
  - `AbilityDebugUI.cs`: `_lastMageZAiming/Charging/ChargeT` 필드. `ColAimBolt/ColChargeBolt` 색상. `case 5: UpdateZSlot()` 추가. `UpdateZSlot()` + `UpdateMageZSlot()` 신규.

- **39단계: 마법사 F 광범위 레이저** (Claude Code)
  - `GameConfig.cs`: `[Header("Mage — Laser (F)")]` 8개 필드. `mageAbility.FCooldown = 18f`.
  - `GameConfig.asset`: `mageAbility.FCooldown: 18` 직렬화.
  - `MageAbilityHandler.cs`:
    - 필드 추가: `_isCastingLaser`, `_laserCastTimer`, `_isLaserActive`, `_laserTimer`, `_laserTickTimer`, `_laserVisualRoot`, `s_laserBuf[8]` (static), `_hitThisTick[8]` (instance), `s_laserMat` (static).
    - 프로퍼티 추가: `IsCastingLaser`, `IsLaserActive`, `LaserCastTimer`, `LaserTimer`.
    - `TryActivate`: 최상단 `if (_isLaserActive) return false;` 추가. `AbilitySlot.F → TryBeginLaser()` 라우팅 추가.
    - `TickTimers`: 레이저 캐스트 타이머 + 활성 타이머 + 매 프레임 SetSelfMoveSpeedMultiplier + UpdateLaserVisual + 0.5s 틱 판정 추가.
    - `HandleOwnerDeath/ForceCleanup`: 레이저 캔슬 추가 (`if (_isLaserActive) ClearLaserState()`).
    - 신규 메서드: `TryBeginLaser`, `BeginLaserActive`, `FireLaserTick`, `SpawnLaserVisual`, `UpdateLaserVisual`, `ClearLaserVisual`, `ClearLaserState`.
    - `FireLaserTick`: OverlapCapsuleNonAlloc(camera→tip, mageLaserRadius) + dedup(_hitThisTick) + LoS raycast(Physics.Raycast→첫 hit이 target HC인지 확인) + TakeDamage + ApplyKnockback(수평).
    - `ClearLaserState`: `ClearSelfMoveSpeedMultiplier()` + `ClearLaserVisual()`.
  - `AbilityController.cs`: `MageIsCastingLaser`, `MageIsLaserActive`, `MageLaserCastTimer`, `MageLaserTimer` 프로퍼티 추가.
  - `AbilityDebugUI.cs`: `_lastMageFCasting/Active/CastT/ActiveT` 필드. `ColLaser` 색상. `UpdateFSlot` 상단에 Mage 분기. `UpdateMageFSlot()` 신규.

- **38단계: 마법사 E 블랙홀** (Claude Code)
  - `GameConfig.cs`: `[Header("Mage — Blackhole (E)")]` 9개 필드. `mageAbility.ECooldown = 12f`.
  - `HealthComponent.cs`: `_pullVelocity/PullTimer`, `PullVelocity` 프로퍼티, `ApplyPull()` 신규. Knockback과 독립 레이어 (Always-overwrite 정책). `Reinitialize()`에 초기화 추가. `Update()`에 타이머 tick 추가.
  - `FirstPersonMotor.cs`: `ApplyMovement()`에 `pull = hc.PullVelocity` 추가. `motion = horizontal + effectiveY + kb + pull`. CharacterController.Move를 타므로 벽 관통 없음.
  - `MageBlackholeZone.cs` (신규): `Init(ownerTeam, ownerHealth, config)`. `Update()`: duration 감소 → 만료 시 Destroy. `_tickTimer`마다 `ApplyZoneEffects()`. `OverlapSphereNonAlloc(-1, 16개 버퍼)`. 대상 필터: 자신/아군/IsTargetable=false 제외. slow `ApplySlow(0.7, 0.25s)`. pull: `toCenter.y=0` 수평화 후 `ApplyPull(방향*pullSpeed, 0.25s)`.
  - `MageAbilityHandler.cs`: `_isCastingBlackhole`, `_blackholeCastTimer` 추가. `TryActivate(E)` → `TryBeginBlackhole()`. 시전 시작 즉시 ECooldown 세팅. `TickTimers`에 캐스트 타이머 감소 → 완료 시 `SpawnBlackholeZone()`. 사망 시 cast 취소, 이미 생성된 zone은 독립 수명. `SpawnBlackholeVisual`: 보라색 sphere, sharedMaterial 캐시.
  - `AbilityController.cs`: `MageIsCastingBlackhole`, `MageBlackholeCastTimer` 프로퍼티.
  - `AbilityDebugUI.cs`: `ColBlackhole`, `_lastMageECasting/T`, `UpdateMageESlot()`. E슬롯 Mage 분기 추가.

  **주의 사항**: Bot은 `FirstPersonMotor`를 사용하지 않으므로 pull 영향 없음 (slow는 적용됨). Bot pull 지원 시 BotController에 `PullVelocity` 처리 추가 필요.

- **37단계: 마법사 화염구 탄창/충전/발사 시스템** (Claude Code)
  - `PlayerCommand.cs`: `AttackHeld bool` 추가.
  - `PlayerInputReader.cs`: `AttackHeld = Input.GetButton("Fire1")` 추가.
  - `GameConfig.cs`: `[Header("Mage — Fireball")]` 11개 필드 추가.
  - `BasicAttackController.cs`: `Tick()` 상단에 `if (CombatClass.Mage) return` 가드.
  - `AbilityController.cs`: `TickPrimary` 라우팅 (`_mageHandler?.TickPrimary(cmd.AttackHeld)`). 프로퍼티 3개 추가: `MageFireballAmmo`, `MageFireballMaxAmmo`, `MageNextFireballIsBig`.
  - `MageAbilityHandler.cs`: `_ammo/_rechargeTimer/_fireTimer/_totalFireCount` 상태 추가. `TickTimers`에 충전 로직. `TickPrimary(bool held)` 신규. `SpawnFireball(bool isBig)` 신규. static Material 2개 캐시. `ClearTeleportState`는 fireball 상태 미초기화(사망 유지 설계).
  - `MageFireballProjectile.cs` (신규): `Init()` 패턴. `Update()`: SphereCastNonAlloc, 캐릭터/환경 구분 히트, range/lifetime 초과 Destroy.
  - `AbilityDebugUI.cs`: `ColFireball/ColBigFireball` 신규. `UpdateQSlot` 상단에 Mage 분기. `UpdateMageAmmoSlot()` 신규.

- **36단계: 마법사 기본 구조 + 우클릭 2연속 텔레포트** (Claude Code)
  - `GameConfig.cs`: `[Header("Mage — Teleport (RC)")]` 4개 필드 추가. `mageAbility = new ClassAbilityConfig { RightClickCooldown = 8f }`.
  - `MageAbilityHandler.cs` (신규): `_chargesUsed / _inRecastWindow / _recastTimer / _passthroughTimer` 상태. `TryRC()`: 쿨타임 체크 → 텔레포트 시도 → 충전 카운트 → recast window/쿨타임 관리. `IsSafeDestination()`: OverlapCapsuleNonAlloc -1 레이어, 자기 CC+캐릭터 제외, 솔리드 충돌 시 false. `TryFindSafeDestination()`: 정확한 목적지 → ±0.5/1.0m 수직 샘플링 (고정 배열). `ExecuteTeleport()`: CC disable/position/enable + passthrough 0.15s.
  - `AbilityController.cs`: `_mageHandler` 필드. Start에서 Mage이면 AddComponent. Update에서 TickTimers. TryActivate에서 Mage 라우팅. TryClearPassthrough에 `mageActive` 조건 추가. HandleOwnerDeath/OnDisable/OnDestroy에 `_mageHandler?.ForceCleanup()` 추가. 브릿지 프로퍼티 3개: `IsMageRCMode`, `MageIsInRecastWindow`, `MageRecastTimer`.
  - `AbilityDebugUI.cs`: `_lastMageRecast/_lastMageRecastT` 필드. `ColTeleport` 색상. switch case 0 추가 → `UpdateRCSlot()`. `UpdateRCSlot()` + `UpdateMageRCSlot()` 신규 메서드.

- **35단계: Warrior R 상승/낙하 이동 + _rOrigin 판정 고정** (Claude Code)
  - `GameConfig.cs`: `warriorRRiseDuration=1.9f`, `warriorRDropDuration=0.1f`, `warriorRRiseHeight=5.0f` 추가
  - `WarriorAbilityHandler.cs`:
    - 필드 추가: `_rOrigin`, `_rMoveVelocity`, `_rSwordPivot`
    - `SelfMoveVelocity`: R Casting + velocity > 0 조건 최우선 분기 추가
    - `TryRCDash/TryQ/TryZ`: `_rState == RState.Casting` 가드 추가 (Q/RC/Z 차단)
    - `TryR()`: `_rOrigin = position` 저장, `_rMoveVelocity = up * riseSpeed` 설정
    - TickTimers R 블록: rise/drop 2단계 분기. drop 시작 시 `SpawnRSword()` 1회 호출 + pivot localY Lerp 애니메이션
    - `SpawnRVisual()`: floor marker만 생성, _rOrigin 기준
    - `SpawnRSword()` 신규: pivot을 상승 최고점 위에 배치 → TickTimers가 0.1s 동안 Lerp로 하강
    - `FireRImpact()`: `_ownerHealth.transform.position` → `_rOrigin` 교체
    - `ClearRState()`: `_rMoveVelocity=zero`, `_rSwordPivot=null` 추가

- **34단계: Warrior R "거검 강림" 궁극기** (Claude Code)
  - `HealthComponent.cs`: `IsInvulnerable` computed property, `_respawnInvul`/`_abilityInvul` 분리, `SetAbilityInvulnerable(bool)` 추가
  - `GameConfig.cs`: Warrior R 5개 수치 필드, RCooldown=30s
  - `WarriorAbilityHandler.cs`: `RState` enum, `TryR/FireRImpact/SpawnRVisual/ClearRState`, R 슬롯 브릿지 프로퍼티
  - `AbilityController.cs`: `WarriorIsRCasting`, `WarriorRCastTimer` 브릿지
  - `AbilityDebugUI.cs`: `UpdateWarriorRSlot()` (보라색, ULT/쿨타임), `_lastRCasting/_lastRCastT` 추적 필드

- **32단계: Warrior Z 반격 후퇴기** (Claude Code)
  - `GameConfig.cs`: `[Header("Warrior — Parry Retreat (Z)")]` 섹션 8개 필드 추가. `warriorAbility.ZCooldown = 18f`.
  - `WarriorAbilityHandler.cs`:
    - `ZState` enum (Idle/Active), `_zState/_zTimer/_zOrigin/_zWave2Done` 필드
    - `s_zWaveMat` 정적 캐시 추가
    - `TryActivate` switch: `AbilitySlot.Z => TryZ()` 추가
    - `TickTimers`: Z 타이머 블록 추가 (Q early-return 이전에 배치)
    - `HandleOwnerDeath / ForceCleanup`: `ClearZState()` 추가
    - 신규 메서드: `TryZ()`, `FireZWave(Vector3)`, `CollectBoxHits(...)`, `ClearZState()`, `SpawnZWaveVisual(Vector3)`, `GetZWaveMat()`
  - `AbilityController.cs`: 변경 없음 — Z 후퇴는 공유 `StartDash`를 사용하므로 `TryClearPassthrough`가 자동 처리
  - `AbilityDebugUI.cs`: 변경 없음 — Z 슬롯(index 5)은 기존 `default: UpdateStdSlot(5)` 경로로 쿨타임 표시

- **31단계: Warrior E 이동속도 구조 개선 + HealthComponent SelfMoveSpeedMultiplier** (Claude Code)
  - `HealthComponent.cs`: `SelfMoveSpeedMultiplier` + `SetSelfMoveSpeedMultiplier/ClearSelfMoveSpeedMultiplier` 추가. Update 타이머, Reinitialize 초기화.
  - `FirstPersonMotor.cs`: `selfMult = HC.SelfMoveSpeedMultiplier`, speed에 3계층 곱산.
  - `BotController.cs`: `moveSpeedMult = MoveSpeedMultiplier * SelfMoveSpeedMultiplier`.
  - `WarriorAbilityHandler.cs`: `_motor` 필드 제거, `using Game.Player` 제거. TryE/EndGuard에서 HC 기반 API 사용.

- **30단계: Warrior Q/RC 구조 수정 + 사망 정책 확정** (Claude Code)
  - `WarriorAbilityHandler.cs`: `TryQ()`에 `_ac.IsDashing` 가드 추가, `TryRCDash()`에 `_qState == QState.Casting` 가드 추가. 실패 입력은 쿨타임 미소모.
  - `WarriorAbilityHandler.cs`: `HandleOwnerDeath()`가 `ClearQCast()`를 호출하도록 정리. Q는 착지/isGrounded에 의존하므로 현재 로컬 모터 구조에서는 사망 시 명시 취소 정책.
  - `AI_HANDOFF.md`: `dashPassthroughControllers`는 현재 플레이어 AbilityController에만 주입됨. 봇 스킬/PvP 전 모든 AC에 자기 제외 CC 목록 주입 구조로 일반화 필요.
  - 남은 메모: 깨진 주석 문자(`??`, `횞`)는 기능 문제는 아니나 추후 주석 정리 때 제거 권장.

- **29단계: Rogue Q 거대 표창 장판 + 슬로우 시스템** (Claude Code)
  - `GameConfig.cs`: Q섹션 Fan Attack → Giant Shuriken Zone으로 교체. 신규 필드: `rogueQZoneSize=3`, `rogueQTravelDistance=3`, `rogueQTravelDuration=0.3`, `rogueQZoneDuration=2`, `rogueQDamagePerSecond=5`, `rogueQTickInterval=0.5`, `rogueQSlowMultiplier=0.7`, `rogueQSlowDuration=1.5`. 유지: `rogueQMaxCharges`, `rogueQCastLockout`.
  - `HealthComponent.cs`: 슬로우 시스템 추가. `MoveSpeedMultiplier` read-only 프로퍼티. `ApplySlow(mult, dur)`: 더 강한 mult 유지, 더 긴 duration 유지. `Reinitialize()`에서 초기화.
  - `FirstPersonMotor.cs`: `_healthComponent` Awake 캐시. 이동속도 = baseSpeed × abilityMult × debuffMult.
  - `BotController.cs`: `botMoveSpeed * botHealth.MoveSpeedMultiplier` 적용.
  - `RogueAbilityHandler.cs`: 구 FanAttack/SpawnQFanVisual/GetQFanMat/s_fanBuf/s_fanHit 제거. `TryQ()` → `LaunchGiantShuriken()`. 전방 0.6m 앞 지면에 스폰.
  - `RogueGiantShurikenZone.cs` (신규): 0.3s 전진 3m → 스냅 → 2s 정지. 0.5s tick마다 `OverlapBoxNonAlloc`, tick당 2.5 피해 + `ApplySlow(0.7, 1.5)`. 틱 내 중복 제거. 청록 큐브 비주얼(3×0.18×3), 180°/s 회전.

- **28단계: F hit cache 버그 수정 + UI 개선** (Claude Code)
  - `RogueAbilityHandler.cs`: `s_fBuf` + `_fHitCache` 16→32. `FSlashTick`의 핵심 버그 수정 — `TakeDamage`를 캐시 기록 블록(`if (_fHitCount < _fHitCache.Length) { ... }`) 안으로 이동. 기존에는 캐시가 꽉 찬 경우에도 피해가 발생해 미추적 대상이 매 tick 중복 피해를 받을 수 있었음. 새 읽기 전용 프로퍼티: `StealthTimer`, `IsRevealed`, `RevealTimer`, `F3SpeedTimer`.
  - `AbilityController.cs`: `RogueIsStealthed`, `RogueStealthTimer`, `RogueIsRevealed`, `RogueRevealTimer`, `RogueF3SpeedTimer` 브릿지 프로퍼티 추가.
  - `PlayerHUD.cs`: 전면 재작성. 340×32px 중앙 하단 HP바. 색상: HP>50% 초록, ≤50% 노랑, ≤25% 빨강. 사망 시 어두운 빨강 + "DEAD". `OnDeath` 이벤트 구독 추가. 리스폰 시 `HandleHealthChanged`에서 `_isDead = false` 복원. 문자열은 이벤트에서만 빌드.
  - `AbilityDebugUI.cs`: 전면 재작성. 슬롯 112×60px, 6개, 하단 중앙. 배경색: 준비=초록, 쿨타임=어두운 회색, R은신=보라, R노출=금색, F콤보창=파랑, F3속도버프=청록. Q슬롯: `charges/max`, lockout/recharge 분기. R슬롯: STEALTH/REVEALED/쿨타임/READY 분기. F슬롯: 콤보창(F2/F3)→속도버프→쿨타임→READY 분기. GUIStyle 최초 1회 생성. 변경 감지: 정수 tenths 비교로 `Update`에서만 문자열 빌드, `OnGUI`는 pre-built string 참조만.

- **27단계: Rogue F 3타 콤보 확장** (Claude Code)
  - `GameConfig.cs/asset`: 구 단일 F 5개 필드 → 15개 per-step 필드(`Step1~3 Distance/Duration/Damage`, `Range/Angle/ComboWindow/InputLock/Step3MoveSpeedMultiplier/Step3MoveSpeedDuration`). `FCooldown 6→9`.
  - `RogueAbilityHandler.cs`:
    - `FComboState` enum(`Idle/Dashing/Window`) + `_fCombo/Step/Window/InputLock/Step3SpeedTimer` 필드로 `_fDashActive bool` 대체
    - 속도 배율 분리: `_stealthSpeedMult`, `_f3SpeedMult` + `RefreshMotorSpeed()` 헬퍼. 항상 곱연산(`_stealthSpeedMult * _f3SpeedMult`)을 motor에 적용. R/F3 동시 활성은 구조상 불가(F가 BreakStealth 선행)하나, 동시 활성 시 3x로 곱산되는 정책으로 기록.
    - `TryF()`: Idle→1타, Window→다음 타 분기. 쿨타임은 콤보 시작 시 설정하지 않음.
    - `StartFStep(int step)`: 방향 고정, per-step hit cache 리셋, `StartDash(dist,dur)`, 비주얼 생성.
    - `OnDashEnded()`: step<3 → Window 오픈, step==3 → F3 속도 버프 + `SetCooldown(F)` + `ClearFCombo()`.
    - TickTimers: `_fInputLock` 카운트다운, Window 만료 시 `SetCooldown + ClearFCombo()`, `_fStep3SpeedTimer` 카운트다운.
    - `FSlashTick()`: `_fComboStep`으로 per-step 피해 선택, `hc.Team == _ownerHealth.Team` 아군 필터 추가.
    - `SpawnFSlashVisual(step,dist,dur)`: 3타는 크기(0.18×1.1) + `s_fSlash3Mat`(진한 보라) 구분.
    - `ClearFCombo()`: 상태+캐시 전체 정리. `HandleOwnerDeath/ForceCleanup`에서 F3 버프도 초기화.
  - `AbilityController.cs`: `IsRogueFComboMode`, `RogueFComboStep`, `RogueFComboWindowTimer` 브릿지 프로퍼티 추가.
  - `AbilityDebugUI.cs`: F 슬롯이 콤보 Window 상태일 때 `"F2 1.8"` 형식으로 표시 (`UpdateRogueFComboLabel()`).

- **26단계: Rogue F 그림자 베기** (Claude Code)
  - `GameConfig.cs`: `rogueShadowSlashDistance/Duration/Damage/Range/Angle` 추가; `rogueAbility.FCooldown = 6f`
  - `GameConfig.asset`: 위 5개 수치 + `rogueAbility.FCooldown = 6` 직렬화
  - `RogueAbilityHandler.cs`:
    - 필드 추가: `_fDashActive`, `_fDashDirection`, `_fHitCache[16]`, `_fHitCount`, `s_fBuf[16]`, `s_fSlashMat`
    - `TryActivate`: `AbilitySlot.F => TryF()` 케이스 추가
    - `TryF()`: 쿨타임/대시중/E2중 차단 → BreakStealth → 캐스트 시점 방향 고정 → StartDash(4m, 0.2s) → SlashVisual 생성
    - `FSlashTick()`: TickTimers에서 `_fDashActive` 동안 매 프레임 호출 → OverlapSphereNonAlloc → 전방 45도 콘 → 인스턴스별 `_fHitCache`로 중복 차단 → TakeDamage(25, IgnoreArmor=false)
    - `ClearFDash()`: `_fDashActive=false`, 캐시 null 초기화
    - `OnDashEnded()`: E2 → F → RC 순서로 분기 (`if _fDashActive → ClearFDash`)
    - `HandleOwnerDeath/ForceCleanup`: ClearFDash() 추가
    - `SpawnFSlashVisual()`: 얇은 흰/보라 큐브 0.25s 수명
    - `GetFSlashMat()`: `new Color(0.9f, 0.85f, 1f)` 정적 캐시
  - 백어택 보너스: F 자체 피해는 IgnoreArmor=false 기본 피해만. R 은신 중 F 사용 시 먼저 BreakStealth 후 F 피해 적용 (백어택 보너스 없음)
  - 피해 판정 타이밍: TickTimers에서 `_fDashActive && !_isDashing` 경계 없이 매 프레임 — AbilityController.Update에서 TickTimers 먼저 실행 후 dash 타이머 체크 → 마지막 프레임도 FSlashTick 호출됨

- **25단계: 은신 봇 탐지 차단 + 백어택 피드백 + 체력바 개선** (Claude Code)
  - `StatusEffectMask.cs`: `Stealthed = 1 << 1` 추가
  - `HealthComponent.cs`: `IsStealthed` / `IsTargetable` (=`!IsDead && !IsStealthed`) 프로퍼티; `SetStealthed(bool)` 추가
  - `RogueAbilityHandler.cs`: `TryR()` 및 `BreakStealth()`에서 `_ownerHealth.SetStealthed(true/false)` 호출; 백어택 성공 시 `_basicAttack.FireBackstabLanded(bonus)` 호출
  - `BasicAttackController.cs`: `OnBackstabLanded` 이벤트 + `internal FireBackstabLanded(float)` 추가
  - `BotController.cs`: `FindNearestAliveEnemy`에서 `e.IsDead` → `!e.IsTargetable`; 공격 블록에도 `target.IsTargetable` 가드 추가
  - `HealthDebugUI.cs`: OnGUI 체력바로 교체 — 팀 색상(파랑/빨강) fill + 검정 아웃라인 + 빈 부분 어두운 회색; 숫자 텍스트 제거
  - `PlayerHUD.cs`: `attackController` 필드 추가; `OnBackstabLanded` 구독 → "BACKSTAB +30" 1.5s 화면 중앙 표시 (노랑 폰트, 0.5s 페이드아웃); `GUIStyle` 피해 발생 시에만 생성
  - `GameBootstrap.cs`: `hud.attackController = attackCtrl` 연결
  - 은신 중 피격 → 1s 노출 상태에서도 `IsTargetable = false` 유지 (봇 타게팅 불가)

- **24단계: Rogue R 은신/백어택** (Claude Code)
  - `DamageInfo.cs`: `bool IgnoreArmor` 필드 추가 (방어 무시 고정 피해용)
  - `HealthComponent.cs`: `OnDamaged` 이벤트 추가; `TakeDamage`에서 `IgnoreArmor` 분기 처리
  - `BasicAttackController.cs`: `OnAttackHit` / `OnAttackUsed` 이벤트 추가; Tick 재구성 — 히트 이벤트를 먼저 발화한 뒤 어택유즈드 발화(은신 해제 전에 백어택 체크 가능하도록)
  - `FirstPersonMotor.cs`: `MoveSpeedMultiplier = 1f` 필드 추가; `ApplyMovement`에서 speed에 곱함; `ResetState()`에서 리셋
  - `GameConfig.cs`: `rogueStealthDuration/rogueStealthMoveSpeedMultiplier/rogueStealthRevealOnHitDuration/rogueBackstabBonusDamage/rogueBackstabAngle` 추가; `rogueAbility.RCooldown=10f` 기본값 반영
  - `GameConfig.asset`: 위 5개 수치 + `rogueAbility.RCooldown=10` 직렬화
  - `RogueAbilityHandler.cs` 대폭 갱신:
    - `using Game.Player;` 추가 (FirstPersonMotor 참조)
    - 필드 추가: `_motor`, `_basicAttack`, `_isStealthed`, `_stealthTimer`, `_isRevealed`, `_revealTimer`, `_stealthVisual`, `_revealVisual`, `_ownerRenderers[]`
    - Static 캐시 추가: `s_stealthMat`(보라), `s_revealMat`(노랑)
    - `IsStealthed` 프로퍼티 추가
    - `Init()`: motor/basicAttack 캐시, Renderer 배열 캐시, OnAttackHit/OnAttackUsed/OnDamaged 구독
    - `OnDestroy()` 추가: 이벤트 해제 + BreakStealth
    - `TickTimers()`: R 은신 타이머, 노출 타이머 추가
    - `TryActivate()`: case R → TryR() 추가 (F는 여전히 미구현)
    - `TryRCDash/TryQ/TryE/TryZ`: 성공 확정 시점에 BreakStealth() 호출
    - 추가 메서드: `TryR()`, `BreakStealth()`, `ApplyReveal()`, `ClearReveal()`, `IsBackstab()`, `OnBasicAttackHit()`, `OnBasicAttackUsed()`, `OnTookDamage()`, `GetStealthMat()`, `GetRevealMat()`
    - `HandleOwnerDeath()` / `ForceCleanup()`: BreakStealth() 추가

- **23단계: 깨진 유니코드 주석 정리** (Claude Code)
  - `AbilityController.cs` / `RogueAbilityHandler.cs`: `─`/`—`/`→` → ASCII `// ---`/`--`/`->` 치환
  - `AGENTS.md` / `AI_HANDOFF.md`: AbilityHandler 정책 문서화

- **22단계: RogueAbilityHandler 분리** (Claude Code)
  - `RogueAbilityHandler.cs` (신규): Rogue 전용 상태(Q 충전, E 상태머신, Z 타이머, 균열 스케줄), NonAlloc 버퍼(s_fanBuf, s_fanHit, s_zBuf), Material 캐시(s_qFanMat 등 5개), 모든 Rogue 능력 구현 로직
  - `AbilityController.cs` 대폭 축소: 공통 쿨타임 배열, 대시 상태(_isDashing/_dashTimer/_dashHorizontalVelocity), DashVelocity, 라우팅(TryActivate → 핸들러 위임)만 남김
    - `internal` API: `StartDash()×2`, `SetDashPassthrough()`, `TryEndZPassthrough()`, `GetCooldown()`, `SetCooldown()`, `AbilityConfig`, `GetBaseCooldown()`
    - Rogue 프로퍼티(IsRogueQChargeMode 등): 핸들러로 위임
    - `HandleOwnerDeath()`: 대시 중단 후 `_rogueHandler?.HandleOwnerDeath()` 위임
    - `TryClearPassthrough()`: `_rogueHandler.IsZPassthrough` 확인 후 판단
  - `RogueMarkedShurikenProjectile.cs`: `Init()` 인자 `AbilityController` → `RogueAbilityHandler`. 이제 프로젝타일이 핸들러를 직접 참조

- **21단계: 구조 버그 수정** (Claude Code)
  - `AbilityController.cs`:
    - `ownerHealth.OnDeath += HandleOwnerDeath` 구독 (Start)
    - `OnDestroy`에서 구독 해제 (`if (ownerHealth != null)` 가드 포함)
    - `HandleOwnerDeath()` 추가: 대시 중단, Z passthrough 정리, ClearEMark(), 균열 스케줄 취소
    - **Q 충전/쿨타임 사망 시 정책**: 유지. 리스폰 딜레이(3s) 동안 쿨타임이 자연 감소하도록 설계. 즉시 사용 가능해지는 경우 상대방 유리 효과.
    - static Material 캐시 4개 추가: `s_eShurikenMat`, `s_eMarkMat`, `s_zBombMat`, `s_riftMat`
    - Getter 메서드 4개 추가: `GetEShurikenMat`, `GetEMarkMat`, `GetZBombMat`, `GetRiftMat`
    - `FireEProjectile`, `ApplyEMark`, `SpawnStunBomb`, `SpawnRift`: `material.color =` → `sharedMaterial = GetXxxMat()`
  - `RogueStunBomb.cs`:
    - `s_blastMat` 정적 Material 필드 + `GetBlastMat()` 추가
    - `SpawnBlastVisual`: `material.color =` → `sharedMaterial = GetBlastMat()`
  - `AI_HANDOFF.md`: Z 텔레포트 목적지 캐릭터 정책 오기 수정 (목적지에 캐릭터 있어도 텔레포트 성공, 0.2s passthrough 처리)

- **20단계 보완: Rogue Z 구조 정리** (Claude Code)
  - `ClassAbilityConfig.cs`: ZCooldown 필드 추가
  - `GameConfig.cs`: rogueZCooldown 제거 → rogueAbility.ZCooldown으로 통합; rogueZPassthroughDuration 추가
  - `GameConfig.asset`: rogueAbility.ZCooldown = 10 직렬화; rogueZPassthroughDuration = 0.2 직렬화
  - `AbilityController.cs`:
    - s_teleportDistances 하드코딩 배열 제거
    - TryFindTeleportDestination: config.rogueZTeleportDistance 기반 루프 계산 (할당 없음)
    - _zPassthroughTimer 필드 추가 → 텔레포트 후 캐릭터 겹침 방지 (SetDashPassthrough 재사용)
    - TryClearPassthrough() 추가: dash + Z 타이머 모두 만료 시에만 passthrough 해제
    - GetBaseCooldown: AbilitySlot.Z → _abilityConfig.ZCooldown 반환
    - TryRogueZ: GetBaseCooldown(Z) 사용, 텔레포트 후 SetDashPassthrough(true) + _zPassthroughTimer
    - OnDisable/OnDestroy: _zPassthroughTimer 강제 초기화

- **20단계: Rogue Z 역위상 스턴 폭탄** (Claude Code)
  - `StatusEffectMask.cs` (신규): [Flags] enum, Stunned 비트 플래그
  - `RogueStunBomb.cs` (신규): Init() 패턴, OverlapSphereNonAlloc 판정, TakeDamage+ApplyStun, 폭발 시각화
  - `HealthComponent.cs`: _activeEffects/_stunTimer 필드, IsStunned 프로퍼티, ApplyStun(), Update() 타이머, Reinitialize() 초기화
  - `AbilitySlot.cs`: Z=5, Count=6
  - `PlayerCommand.cs`: SkillZPressed 추가
  - `PlayerInputReader.cs`: KeyCode.Z 매핑
  - `AbilityController.cs`: TryRogueZ/TryFindTeleportDestination/IsSafeDestination/PerformTeleport/SpawnStunBomb, Tick에 Z 추가
  - `GameConfig.cs`: rogueZ* 수치 필드
  - `GameConfig.asset`: rogueZ 수치 직렬화
  - `LocalPlayerController.cs`: 스턴 중 이동/공격/스킬 차단, 카메라 회전 허용
  - `BasicAttackController.cs`: IsStunned 공격 차단
  - `BotController.cs`: IsStunned 이동/공격 차단 (중력 유지)
  - `AbilityDebugUI.cs`: Z 슬롯 추가 (배열 6개로 확장)
- **19단계: Rogue E 표창 투척 + 도약** (Claude Code)
  - `RogueMarkedShurikenProjectile.cs` (신규): Init() 패턴, OverlapSphereNonAlloc 판정, `OnEProjectileHit` 콜백
  - `AbilityController.cs`: `RogueEState` enum, E 상태 필드 4개, `TryActivateRogueE()`, `FireEProjectile()`, `OnEProjectileHit()`, `ApplyEMark()`, `StartE2Dash()`, `ClearEMark()`, `StartDash(Vector3,float)` 오버로드, `OnDashEnded()` E2/RC 분기, `OnDisable/OnDestroy`에 `ClearEMark()` 추가
  - `GameConfig.cs`: rogueE 6개 필드 추가
  - `GameConfig.asset`: rogueE 수치 직렬화, `rogueAbility.ECooldown = 8`
- **18단계: Rogue Q 2충전 시스템** — 완료
- **17단계: Rogue Q 시각화 + 대시 유닛 통과** — 완료
- **16단계: Rogue Q 표창 부채꼴 베기** — 완료
- **15단계 이하** — 완료

## In Progress

없음.

## Warrior E — 철벽 가드 (31단계)

```
TryE():
  ① E 쿨타임 체크 (_cooldowns[E])
  ② _isGuarding 중이면 차단 (재입력 무시)
  ③ _ac.SetCooldown(E, ECooldown=12s)
  ④ _isGuarding=true, _guardTimer=warriorGuardDuration(2s)
  ⑤ _ownerHealth.SetDamageTakenMultiplier(0.5f, 2s) — 피해 50% 감소
  ⑥ _motor.MoveSpeedMultiplier = 0.7f            — 이동속도 30% 감소 (능력 계층)
  ⑦ SpawnGuardVisual() — 금색 구체 1.8× 자식 오브젝트, 가드 종료 시 파괴

TickTimers():
  _guardTimer -= dt → 0 도달 시 EndGuard()

EndGuard():
  _isGuarding=false, _guardTimer=0
  _ownerHealth.ClearDamageTakenMultiplier()  — 명시적 조기 종료
  _motor.MoveSpeedMultiplier = 1f
  DestroyGuardVisual()

HandleOwnerDeath():
  ClearQCast() + EndGuard() — Q/가드 모두 즉시 취소

피해 감소 계층 (HealthComponent):
  DamageTakenMultiplier property — _damageTakenTimer > 0 ? _damageTakenMult : 1f
  TakeDamage()에서 finalDamage *= DamageTakenMultiplier 적용
  IgnoreArmor=true 피해에도 적용 (가드는 모든 피해 감소)
  SetDamageTakenMultiplier: 강한 감소(낮은 mult) 우선, 긴 지속시간 우선
  ClearDamageTakenMultiplier: 즉시 1f로 복원
  Reinitialize(): _damageTakenMult=1f, _damageTakenTimer=0f 초기화

이동속도 계층 (HealthComponent 기반):
  SelfMoveSpeedMultiplier (자기 스킬 계층) — 가드 시 SetSelfMoveSpeedMultiplier(0.7f, 4s)
    EndGuard() → ClearSelfMoveSpeedMultiplier() (명시 종료)
    Reinitialize() → _selfMoveMult=1f, _selfMoveTimer=0 (리스폰 초기화)
    HealthComponent.Update() → _selfMoveTimer 자동 감소 (타이머 만료도 복원)
  MoveSpeedMultiplier (적 디버프 계층) — 기존 슬로우 구조 유지
  FirstPersonMotor: speed *= debuffMult * selfMult (두 계층 모두 곱산)
  BotController: moveSpeedMult = MoveSpeedMultiplier * SelfMoveSpeedMultiplier (봇도 동일 계층)
  FirstPersonMotor.MoveSpeedMultiplier (모터 능력 계층) — Rogue 은신/F3 버프 전용, 미변경

AbilityController 브릿지:
  IsWarriorGuardMode  — _warriorHandler != null (전사 여부)
  WarriorIsGuarding   — IsGuarding 위임
  WarriorGuardTimer   — GuardTimer 위임

AbilityDebugUI E슬롯:
  IsWarriorGuardMode가 true → UpdateESlot() 전용 경로
  가드 중: "GUARD\nX.Xs" (금색 배경 ColGuard)
  가드 해제 후: 쿨타임 또는 READY (기존 패턴)
  아닌 직업: UpdateStdSlot(2) 폴백
```

### GameConfig Warrior E 수치
| 필드 | 기본값 | 설명 |
|------|--------|------|
| `warriorGuardDuration` | 4 s | 가드 지속 시간 |
| `warriorGuardDamageMultiplier` | 0.5 | 받는 피해 배율 (0.5=50% 감소) |
| `warriorGuardMoveSpeedMultiplier` | 0.7 | 이동속도 배율 (0.7=30% 감소, SelfMoveSpeedMultiplier) |
| `warriorAbility.ECooldown` | 14 s | E 쿨타임 |

## Warrior Z — 반격 후퇴기 (32단계)

```
TryZ():
  ① Z 쿨타임 체크 (_cooldowns[Z])
  ② _zState == Active → 차단 (재입력 무시)
  ③ _ac.IsDashing → 차단 (RC 대시/E2 도약 중)
  ④ _qState == Casting → 차단 (Q 도약 중)
  ⑤ SetCooldown(Z, ZCooldown=18s)
  ⑥ 카메라 전방 반대 방향 = 후퇴 방향 (수평)
  ⑦ _zOrigin = 현재 위치, _zState = Active
  ⑧ _ac.StartDash(-fwd * speed, 0.35s) → 공유 dash + 유닛 통과(SetDashPassthrough)
  ⑨ FireZWave(_zOrigin) → 발동 위치 기준 X파동 1회

TickTimers():
  _zState == Active → _zTimer += dt
  _zTimer >= warriorZWaveDelay(0.5s) → FireZWave(현재 위치) → ClearZState()

FireZWave(position):
  halfExtents = (ZWaveWidth/2, standHeight/2, ZWaveLength/2)
  OverlapBoxNonAlloc ×2 (rot +45°, rot −45°) → CollectBoxHits 공유 dedup 캐시
  TakeDamage(ZWaveDamage=15) + ApplyKnockback(dir * ZWaveKnockback=1.5, 0.3s)
  SpawnZWaveVisual: 두 큐브 ±45° 회전, ZWaveDuration(0.45s) 후 Destroy

HandleOwnerDeath():
  ClearQCast() + EndGuard() + ClearZState()

충돌 정책:
  - Q 시전 중 Z 차단: TryZ에서 _qState==Casting 체크
  - RC 대시 중 Z 차단: _ac.IsDashing 체크
  - E 가드 중 Z 허용: 두 스킬은 독립 상태, 동시 활성 가능 (후퇴 중 가드 유지)
  - 유닛 통과: StartDash가 SetDashPassthrough(true) 호출, 대시 종료 시 TryClearPassthrough() 자동 정리
  - 벽 통과 없음: dash velocity를 cc.Move에 전달 → CC가 지형 블록 처리

이동속도 계층 미적용:
  Z는 dash velocity 직접 주입이므로 SelfMoveSpeedMultiplier와 무관하게 고정 속도 후퇴
```

### GameConfig Warrior Z 수치
| 필드 | 기본값 | 설명 |
|------|--------|------|
| `warriorZRetreatDistance` | 6 m | 후퇴 거리 |
| `warriorZRetreatDuration` | 0.35 s | 대시 지속 시간 |
| `warriorZWaveDelay` | 0.5 s | 1파동 → 2파동 지연 |
| `warriorZWaveLength` | 12 m | X 팔 전체 길이 |
| `warriorZWaveWidth` | 1.8 m | X 팔 폭 |
| `warriorZWaveDuration` | 0.45 s | 파동 시각화 수명 |
| `warriorZWaveDamage` | 15 | 파동당 피해량 |
| `warriorZWaveKnockback` | 1.5 m/s | 넉백 크기 |
| `warriorAbility.ZCooldown` | 18 s | Z 쿨타임 |

## Warrior Q 정책 요약 (구조 수정 후 확정)

```
Q/RC 겹침 방지:
  TryQ()       : IsDashing 체크 추가 → RC 대시 중 Q 시작 불가
  TryRCDash()  : _qState==Casting 체크 추가 → Q 시전 중 RC 대시 시작 불가
  쿨타임 소모  : 두 경우 모두 실패 시 쿨타임 미소모 (체크를 SetCooldown 앞에 배치)

사망 정책 (HandleOwnerDeath):
  ClearQCast() 호출로 Q 상태 전체 초기화 + TryClearPassthrough()
  Q는 모터 isGrounded에 의존 → 사망 후 모터 정지 시 착지 판정 미정의 → 명시적 취소
  서버 권한 PvP 단계에서 타격 이벤트를 모터와 분리할 때 재검토

유닛 통과 (dashPassthroughControllers):
  현재: 플레이어 AbilityController에만 주입 (GameBootstrap.WireAbilityPassthrough)
  봇/PvP 확장 시: 모든 AbilityController에 자기 제외 CC 목록 주입 구조로 일반화 필요
  코드 확장 지점: GameBootstrap.WireAbilityPassthrough() — 현재 주석으로 명시됨

AbilityController.TryClearPassthrough 조건:
  !_isDashing && !_rogueHandler.IsZPassthrough && !_warriorHandler.IsQCasting
  (Warrior Z 후퇴는 공유 dash 타이머를 사용하므로 _isDashing이 passthrough를 커버, 별도 조건 불필요)
```

## Next Task

**옵션 A — 마법사 R: 유성 궁극기**: 지정 범위에 다수 유성 낙하, 폭발 피해
**옵션 B — 마법사 Z: 보호막/회피 계열**: 방어기 또는 점멸 계열
**옵션 C — 마법사 Z: 반격기**: 다음 피격을 흡수해 반사 또는 무적 유지
**옵션 D — UI 심화**: uGUI HP 바, 킬/데스 카운트, 부활 카운트다운
**옵션 E — 맵 확장**: 벽/엄폐물 추가

## Do Not Do Yet

- Mage R/Z, Archer 실제 스킬 효과
- 포탑/넥서스
- 정식 로비/매칭 UI
- 실제 네트워크

## Known Issues

- Rogue R 은신 비주얼: Renderer.enabled 토글 + 작은 보라 구체/노란 구체 (프로토타입) — 나중에 VFX/셰이더 아웃라인으로 교체. 현재는 모든 관찰자에게 동일하게 적용(자신/적 구분 없음).
- BasicAttackController.OnAttackHit/OnAttackUsed는 로컬 플레이어 전용. 봇 기본 공격은 BotController에서 직접 AttackResolver 호출 — 봇 Rogue 기본 공격 은신 해제/백어택 미지원.
- Rogue Q 시각화는 청록색 거대 표창 장판 Cube (프로토타입) — 나중에 VFX로 교체.
- Rogue E 표창은 회색 납작 Cube, 표식은 빨간 Sphere (프로토타입) — 나중에 VFX로 교체.
- Rogue Z 폭탄은 작은 보라 Sphere, 폭발은 오렌지 Sphere 0.15s (프로토타입) — 나중에 VFX로 교체.
- 균열 시각화는 불투명 보라색 Cube — 나중에 VFX로 교체.
- OmniSharp가 GameConfig.cs에 "Using directive is unnecessary" 힌트를 표시하지만 IDE 오진단임 (using Game.Combat 필요).
- Mage 텔레포트 `IsSafeDestination()`은 `OverlapCapsuleNonAlloc`에 layerMask=-1(전체)을 사용한다. 레이어 분리 시 환경 전용 레이어 마스크로 교체하면 성능 개선 가능.
- Mage 텔레포트 안전 위치 샘플링은 수직 방향만 (±0.5m, ±1.0m). 벽 앞 막혀 있으면 수평 샘플링이 필요할 수 있음. 현재 프로토타입 수준에서는 허용.
- Warrior F `SpawnFVisual()`은 0.1s마다 `new GameObject`를 생성한다. 2s 시전 중 최대 20개. 출시 전 오브젝트 풀로 교체 필요 (현재 프로토타입 수준에서는 허용).
- Warrior R `SpawnRSword()`는 낙하 시작 시 1회만 생성하므로 빈도 문제 없음.
- Warrior Z 파동 비주얼: 반투명 청록 큐브 ×2 (프로토타입) — 나중에 VFX로 교체. `GetZWaveMat()`에서 Standard/Transparent 모드 설정.
- dashPassthroughControllers는 플레이어 AbilityController에만 주입됨. 봇 스킬/PvP 전에 GameBootstrap.WireAbilityPassthrough를 모든 AC에 일반화해야 함.
- E2 도착 지점이 벽 안에 있을 경우 CharacterController.Move가 자연스럽게 미끄러지지만 정확한 뒤 위치 보장 없음.
- Z 텔레포트: 목적지에 캐릭터(봇/플레이어)가 있어도 텔레포트 성공. IsSafeDestination은 HC를 가진 콜라이더(캐릭터 CC)를 안전한 것으로 간주하며, 텔레포트 후 0.2s _zPassthroughTimer가 밀림/끼임을 방지. 지형(HC 없는 콜라이더)만 해당 거리를 실패 처리.

## Notes For Next AI

- 작업 전 `AGENTS.md`와 `AI_HANDOFF.md`를 읽는다.
- **AbilityController vs RogueAbilityHandler 역할 분리**:
  - `AbilityController`: 공통 쿨타임 배열, 대시 상태(_isDashing/_dashTimer/_dashHorizontalVelocity), DashVelocity 프로퍼티, Tick/TryActivate 라우팅. Rogue/Warrior는 모든 TryActivate를 각 전용 핸들러로 위임.
  - `RogueAbilityHandler`: Rogue 전용 상태(Q 충전, E 상태머신, Z 타이머, 균열 스케줄), 모든 Rogue 로직 구현. AbilityController가 Start()에서 `gameObject.AddComponent<RogueAbilityHandler>().Init(this,...)`로 생성.
  - AC → 핸들러 `internal` API: `StartDash()×2`, `SetDashPassthrough()`, `TryEndZPassthrough()`, `GetCooldown()`, `SetCooldown()`, `AbilityConfig`, `GetBaseCooldown()`
  - 핸들러 → AC 콜백: `TickTimers(dt)`, `OnDashEnded()`, `HandleOwnerDeath()`, `ForceCleanup()`
- **Rogue Q 충전 시스템** (핸들러 내부): `_qLockoutTimer`는 연속 사용 방지, `_qRechargeTimer`는 충전 회복. 별도 흐름.
- **Rogue E 상태머신** (핸들러 내부): `_eState`가 `Dashing`이면 `IsE2Dashing=true` → AC.Tick()에서 모든 입력 차단. `OnDashEnded()`는 핸들러에서 Dashing vs RC 분기 처리. `ForceCleanup()`에서 `ClearEMark()` 호출.
- **Rogue Z 텔레포트** (핸들러 내부): 핸들러가 `PerformTeleport` (CC disable → position → enable), `_ac.SetDashPassthrough(true)`, `_zPassthroughTimer` 관리. 타이머 만료 시 `_ac.TryEndZPassthrough()` 호출. cc.Move는 여전히 FirstPersonMotor.Tick에서만.
- **RogueMarkedShurikenProjectile**: `Init()` 마지막 인자가 `RogueAbilityHandler`임 (`AbilityController` 아님).
- **직업별 AbilityHandler 정책** (22단계부터 적용):
  - 모든 직업 스킬 로직은 `XxxAbilityHandler`에 작성한다. `AbilityController`에 직접 추가하지 않는다.
  - **Rogue R**: 구현 완료. `RogueAbilityHandler._isStealthed/TryR()/BreakStealth()` 참조.
  - **Rogue F**: 구현 완료. `RogueAbilityHandler`의 F 3타 콤보/속도 버프 상태 참조.
  - **Warrior RC/Q/E/Z/R**: 구현 완료. `WarriorAbilityHandler`가 RC 대시, Q 상승/하강 착지 강타, E 철벽 가드, Z 반격 후퇴기, R 거검 강림을 담당. Q/RC/Z는 R 시전 중 차단됨. 사망 시 모두 명시적으로 취소.
  - **Mage Q (화염구)**: 탄창 15, 0.2s 발사 간격, 2s/탄 충전, 5번째마다 대형 화염구.
  - **Mage E (블랙홀)**: 1s 시전 → zone 생성. 5s 지속. 반경 5m, 이동속도 -30%, 수평 끌림 2m/s. 사망 시 cast 취소, 생성된 zone은 유지.
  - **Mage F (레이저)**: 0.5s 시전 → 5s 채널. 활성 중 이동 잠금(SetSelfMoveSpeedMultiplier 0, 매 프레임 갱신). 0.5s마다 OverlapCapsuleNonAlloc + LoS raycast → 피해 5 + 수평 넉백 5m/s. 활성 중 모든 다른 스킬 차단(TryActivate 최상단). 사망 시 즉시 취소+ClearSelfMoveSpeedMultiplier. FCooldown = 18s.
  - **Mage RC (텔레포트)**: 우클릭 2연속 텔레포트. R/Z는 미구현.
  - **Archer 스킬**: 구현 시점에 `ArcherAbilityHandler`를 신규 파일로 생성.
  - 빈 Handler 파일을 미리 만들지 않는다. 실제 스킬 구현 시점에 파일을 생성한다.
  - Handler 연결 방식: `AbilityController.Start()`에서 `gameObject.AddComponent<XxxAbilityHandler>().Init(this,...)`.
- **AbilitySlot 추가 시**: AbilitySlot.cs Count++, PlayerCommand.SkillXPressed, PlayerInputReader.Read(), AbilityController.Tick, AbilityDebugUI 배열 확장 필요.
- **스턴 추가**: `ApplyStun(float)` 호출. 스턴 중인 캐릭터는 LocalPlayerController/BotController에서 이동/공격/스킬 차단.
- **cc.Move 단일 호출**: FirstPersonMotor.Tick 내부에서만.
- 수치는 반드시 `GameConfig`에서 관리한다.
- 작업 후 `Last Completed`, `In Progress`, `Next Task`, `Known Issues`, `Do Not Do Yet`을 갱신한다.