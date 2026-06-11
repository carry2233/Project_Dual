using System; // 상태값 변경 알림 이벤트용
using UnityEngine;
using System.Collections; // 와해상태 코루틴 처리용

/// <summary>
/// 캐릭터의 레벨, 전투 스탯, 체력, 와해량, 이동속도 계산값을 관리한다.
/// 기본 이동속도와 이동속도 퍼센트를 계산해 최종 이동속도를 만들고,
/// 해당 값을 NavigationMovementSystem에 적용한다.
/// </summary>
public class CharacterStatSystem : MonoBehaviour
{
    [Header("레벨 관련")]
    [SerializeField] private int levelstats; // 레벨 수치

    [Header("공격 관련")]
    [SerializeField] private int attackPower; // 공격력

    [Header("방어 관련")]
    [SerializeField] private int defenseValue; // 방어력 = 받은 피해감소

    [Header("체급 관련")]
    [SerializeField] private int bodySize; // 체급

    [Header("체력 관련")]
    [SerializeField] private int maxHealth; // 최대 체력
    [SerializeField] private int currentHealth; // 현재 체력

[Header("속도 관련")]
[SerializeField] private int speedStat; // 기본 속도 수치
[SerializeField] private float baseMoveSpeed = 5f; // 기본 이동속도
[SerializeField] private int moveSpeedPercent = 100; // 이동속도 퍼센트
[SerializeField] private float finalMoveSpeed; // 최종 적용 이동속도
[SerializeField] private NavigationMovementSystem navigationMovementSystem; // 이동속도를 적용할 네비 이동 시스템

[Header("결투 속도 관련")]
[SerializeField] private int battleSpeed; // 이번 결투에 계산되어 적용된 전투속도
[SerializeField] private int minimumSpeedRatePercent = 90; // 결투 시 랜덤으로 뽑힐 최소 속도율(%)
[SerializeField] private int maximumSpeedRatePercent = 110; // 결투 시 랜덤으로 뽑힐 최대 속도율(%)

    [Header("위력 관련")]
    [SerializeField] private int powerRatePercent  = 100; // 위력률 퍼센트

    [Header("와해 관련")]
[SerializeField] private int currentStaggerAmount; // 현재 와해량
[SerializeField] private int maxStaggerAmount = 100; // 최대 와해량
[SerializeField] private int staggerResistancePercent; // 와해 저지율(%)

public int CurrentStaggerAmount => currentStaggerAmount; // 현재 와해량 반환
public int MaxStaggerAmount => maxStaggerAmount; // 최대 와해량 반환
public int StaggerResistancePercent => staggerResistancePercent; // 와해 저지율 반환

    [Header("행동 상태 관련")]
    [SerializeField] private bool isActionLocked; // 현재 행동 불가 여부

    [Header("________________________________________________________________________________")]

    [Header("와해 상태 설정")]
[SerializeField] private CharacterAnimationPlayer characterAnimationPlayer; // 와해 애니메이션 재생기 참조
[SerializeField] private CharacterAnimationClipSO brokenLoopAnimationClip; // 와해 중 루프 재생 애니메이션
[SerializeField] private CharacterAnimationClipSO brokenReleaseAnimationClip; // 와해 해제 애니메이션
[SerializeField] private float brokenMaintainDuration = 3f; // 와해 유지시간
[SerializeField] private float brokenReleaseAnimationWaitTime = 0.5f; // 와해해제 애니메이션 대기시간

[Header("현재 와해 상태")]
[SerializeField] private bool isBrokenState; // 현재 와해상태 여부
[SerializeField] private float currentBrokenTimer; // 현재 와해 유지 타이머

[SerializeField] private NavigationMovementSystem NavigationMovementSystem; // 이동속도를 적용하고 와해 중 이동을 정지할 네비 이동 시스템

[SerializeField] private int deathBrokenClearFrameCount = 5; // 사망 직후 와해상태를 반복 해제할 프레임 수

private Coroutine deathBrokenClearCoroutine; // 사망 직후 와해상태 반복 해제 코루틴


[Header("________________________________________________________________________________")]


[Header("사망 상태")]
[SerializeField] private bool isDead; // 현재 사망 여부

public bool IsDead => isDead; // 사망 여부 반환

[SerializeField] private GlobalCharacterManager globalCharacterManager; // 사망 상태를 전달할 전역 캐릭터 매니저
[SerializeField] private CharacterDuelAI characterDuelAI; // 본인 캐릭터 AI 참조


[Header("________________________________________________________________________________")]


[Header("공격기술 치명타 상태")]
[SerializeField] private bool applyCriticalToNextAttackSkill; // 다음 공격기술에 치명타 피해 적용 여부
[SerializeField] private bool gainCriticalExperienceOnNextAttackSkillHit; // 다음 공격기술 치명타 적중 시 경험치 획득 여부

public bool ApplyCriticalToNextAttackSkill => applyCriticalToNextAttackSkill;
public bool GainCriticalExperienceOnNextAttackSkillHit => gainCriticalExperienceOnNextAttackSkillHit;


private Coroutine brokenStateCoroutine; // 와해상태 진행 코루틴

public bool IsBrokenState => isBrokenState; // 와해상태 여부 반환

    public int LevelStats => levelstats; // 레벨 수치 반환
public int AttackPower => attackPower; // 공격력 반환
public int DefenseValue => defenseValue; // 방어력 반환
public int BodySize => bodySize; // 체급 반환
public int MaxHealth => maxHealth; // 최대 체력 반환
public int CurrentHealth => currentHealth; // 현재 체력 반환
public int SpeedStat => speedStat; // 기본 속도 수치 반환
public float BaseMoveSpeed => baseMoveSpeed; // 기본 이동속도 반환
public int MoveSpeedPercent => moveSpeedPercent; // 이동속도 퍼센트 반환
public float FinalMoveSpeed => finalMoveSpeed; // 최종 적용 이동속도 반환

public int BattleSpeed => battleSpeed; // 현재 저장된 전투속도 반환
public int MinimumSpeedRatePercent => minimumSpeedRatePercent; // 최소 속도율 반환
public int MaximumSpeedRatePercent => maximumSpeedRatePercent; // 최대 속도율 반환
public int PowerRatePercent => powerRatePercent; // 위력률 퍼센트 반환
public bool IsActionLocked => isActionLocked; // 행동 불가 여부 반환

    public event Action<CharacterStatSystem> OnStatusValueChanged; // 스탯 값 변경 알림 이벤트

    

private void Awake() // 시작 시 현재값 범위 보정
{
    currentHealth = Mathf.Clamp(currentHealth, 0, Mathf.Max(0, maxHealth)); // 현재 체력 범위 보정
    currentStaggerAmount = Mathf.Clamp(currentStaggerAmount, 0, Mathf.Max(0, maxStaggerAmount)); // 현재 와해량 범위 보정

    if (navigationMovementSystem == null)
    {
        navigationMovementSystem = GetComponent<NavigationMovementSystem>(); // 네비 이동 시스템 자동 참조
    }

    if (characterAnimationPlayer == null)
    {
        characterAnimationPlayer = GetComponent<CharacterAnimationPlayer>(); // 캐릭터 애니메이션 재생기 자동 참조
    }

    if (characterDuelAI == null)
    {
        characterDuelAI = GetComponent<CharacterDuelAI>(); // 캐릭터 AI 자동 참조
    }

    if (globalCharacterManager == null)
    {
        globalCharacterManager = GlobalCharacterManager.Instance; // 전역 캐릭터 매니저 참조
    }

    RefreshFinalMoveSpeed(); // 최종 이동속도 계산 및 적용
    CheckDeathState(); // 시작 시 체력이 0이면 사망 처리
}

private void Update() // 매 프레임 사망 상태 확인
{
    CheckDeathState(); // 현재 체력 기반 사망 상태 확인
}

    private void NotifyStatusValueChanged() // UI 즉시 갱신 알림 전달
    {
        OnStatusValueChanged?.Invoke(this); // 구독 중인 UI들에게 현재 상태 변경 알림
    }

public void SetActionLocked(bool locked) // 행동 가능 여부 설정
{
    isActionLocked = locked; // 행동 불가 여부 저장
}

public void SetBattleSpeed(int newBattleSpeed) // 현재 결투용 전투속도 저장
{
    battleSpeed = Mathf.Max(0, newBattleSpeed); // 음수 방지 후 저장
}

public int ApplyHealthDamage(int rawDamage) // 방어력 퍼센트를 반영한 최종 체력 피해 적용
{
    if (isDead)
    {
        return 0; // 이미 사망했으면 피해 적용 중단
    }

    int safeRawDamage = Mathf.Max(0, rawDamage); // 음수 피해 방지
    int clampedDefensePercent = Mathf.Clamp(defenseValue, 0, 100); // 방어력 퍼센트 범위 제한

    int finalDamage = (safeRawDamage * (100 - clampedDefensePercent)) / 100; // 방어력 반영 최종 피해 계산

    if (finalDamage <= 0)
    {
        return 0; // 최종 피해가 없으면 종료
    }

currentHealth = Mathf.Max(0, currentHealth - finalDamage); // 현재 체력 차감

NotifyStatusValueChanged(); // 체력 UI 즉시 갱신 알림

CheckDeathState(); // 체력이 0 이하가 되었는지 즉시 확인

    return finalDamage; // 실제 적용된 최종 피해 반환
}

public int ApplyStaggerDamage(int rawStaggerDamage) // 저지율 퍼센트를 반영한 최종 와해피해 적용
{
    int safeRawStaggerDamage = Mathf.Max(0, rawStaggerDamage); // 음수 와해피해 방지
    int clampedResistancePercent = Mathf.Clamp(staggerResistancePercent, 0, 100); // 저지율 퍼센트 범위 제한

    int finalStaggerDamage = (safeRawStaggerDamage * (100 - clampedResistancePercent)) / 100; // 저지율 반영 최종 와해피해 계산

    if (finalStaggerDamage <= 0)
    {
        return 0; // 최종 와해피해가 없으면 종료
    }

    currentStaggerAmount = Mathf.Clamp(currentStaggerAmount + finalStaggerDamage, 0, Mathf.Max(0, maxStaggerAmount)); // 현재 와해량 증가
    NotifyStatusValueChanged(); // 와해 UI 즉시 갱신 알림

    if (currentStaggerAmount >= maxStaggerAmount)
    {
        StartBrokenState(); // 최대 와해량 도달 시 와해상태 시작
    }

    return finalStaggerDamage; // 실제 적용된 최종 와해피해 반환
}

public void RefreshFinalMoveSpeed() // 최종 이동속도를 계산하고 이동 시스템에 적용합니다.
{
    float calculatedMoveSpeed = baseMoveSpeed * (moveSpeedPercent / 100f); // 기본 이동속도와 퍼센트 계산
    finalMoveSpeed = Mathf.Floor(calculatedMoveSpeed * 10f) / 10f; // 소수점 첫째 자리까지만 남기고 뒤는 버림

    if (navigationMovementSystem != null)
    {
        navigationMovementSystem.SetMoveSpeed(finalMoveSpeed); // 이동 시스템에 최종 이동속도 적용
    }

    NotifyStatusValueChanged(); // UI 갱신 알림
}

public void SetLevelStats(int newLevelStats) // 레벨 수치 설정
{
    levelstats = Mathf.Max(1, newLevelStats); // 최소 1 이상으로 보정
    NotifyStatusValueChanged(); // UI 갱신 알림
}

public void StartBrokenState() // 와해상태 시작
{
    if (isBrokenState)
    {
        return; // 이미 와해상태면 중복 실행 방지
    }

    if (brokenStateCoroutine != null)
    {
        StopCoroutine(brokenStateCoroutine); // 기존 와해 코루틴 정리
    }

    brokenStateCoroutine = StartCoroutine(BrokenStateRoutine()); // 와해상태 코루틴 시작
}


private void ReleaseBrokenState() // 와해상태 해제
{
    if (brokenStateCoroutine != null)
    {
        StopCoroutine(brokenStateCoroutine); // 와해 코루틴 정리
        brokenStateCoroutine = null; // 코루틴 참조 초기화
    }

    StartCoroutine(BrokenReleaseRoutine()); // 와해해제 애니메이션 처리 시작
}

private IEnumerator BrokenReleaseRoutine() // 와해해제 애니메이션 후 행동 복구
{
    if (characterAnimationPlayer != null)
    {
        characterAnimationPlayer.PlayBrokenReleaseAnimation(brokenReleaseAnimationClip); // 와해해제 애니메이션 재생
    }

    float safeWaitTime = Mathf.Max(0f, brokenReleaseAnimationWaitTime); // 대기시간 보정

    if (safeWaitTime > 0f)
    {
        yield return new WaitForSeconds(safeWaitTime); // 해제 애니메이션 대기
    }

    isBrokenState = false; // 와해상태 해제
    currentStaggerAmount = 0; // 와해상태 해제 후 현재 와해량 초기화
    SetActionLocked(false); // 행동 잠금 해제

    if (characterAnimationPlayer != null)
    {
        characterAnimationPlayer.StopManualAnimation(); // 기존 자동 행동 애니메이션으로 복귀
    }

    NotifyStatusValueChanged(); // UI 갱신
}

public void CancelBrokenStateByAttackSkillHit() // 공격기술 피격 진입으로 와해상태를 즉시 종료 처리
{
    if (!isBrokenState)
    {
        return; // 와해상태가 아니면 종료
    }

    if (brokenStateCoroutine != null)
    {
        StopCoroutine(brokenStateCoroutine); // 와해 유지 코루틴 정지
        brokenStateCoroutine = null; // 코루틴 참조 초기화
    }

    isBrokenState = false; // 와해상태 해제
    currentBrokenTimer = 0f; // 와해 유지 타이머 초기화

    if (characterAnimationPlayer != null)
    {
        characterAnimationPlayer.ClearBrokenAnimationLock(); // 공격기술 피격으로 넘어가므로 와해 루프 고정 해제
    }

    SetActionLocked(true); // 공격기술 피격 상태가 이어지므로 행동 잠금은 유지
    NotifyStatusValueChanged(); // UI 갱신
}

private IEnumerator BrokenStateRoutine() // 와해상태 유지 및 해제 처리
{
    isBrokenState = true; // 와해상태 설정
    currentBrokenTimer = Mathf.Max(0f, brokenMaintainDuration); // 유지시간 초기화
    SetActionLocked(true); // 와해 중 행동 잠금

    if (navigationMovementSystem != null)
    {
        navigationMovementSystem.StopMove(); // 와해 시작 즉시 일반 이동 정지
    }

    if (characterAnimationPlayer != null)
    {
        characterAnimationPlayer.PlayBrokenLoopAnimation(brokenLoopAnimationClip); // 와해 루프 애니메이션 고정 재생
    }

    while (currentBrokenTimer > 0f)
    {
        if (navigationMovementSystem != null)
        {
            navigationMovementSystem.StopMove(); // 와해 유지 중 일반 이동 재개 방지
        }

        if (characterAnimationPlayer != null)
        {
            characterAnimationPlayer.PlayBrokenLoopAnimation(brokenLoopAnimationClip); // idle/move가 끼어들어도 와해 루프 유지
        }

        currentBrokenTimer -= Time.deltaTime; // 와해 유지시간 감소
        yield return null;
    }

    ReleaseBrokenState(); // 와해 해제 실행
}

private void CheckDeathState() // 체력 기준 사망 상태 확인
{
    if (currentHealth > 0)
    {
        return; // 체력이 남아있으면 종료
    }

    ExecuteDeath(); // 사망 처리 실행
}

public void ExecuteDeath() // 사망 처리
{
    if (isDead)
    {
        return; // 중복 사망 방지
    }

    isDead = true; // 사망 여부 체크
    StartDeathBrokenClearRoutine(); // 사망 직후 설정 프레임 동안 와해상태 반복 해제
    SetActionLocked(true); // 모든 행동 잠금

    if (navigationMovementSystem != null)
    {
        navigationMovementSystem.StopMove(); // 이동 정지
    }

    if (characterAnimationPlayer != null)
    {
characterAnimationPlayer.PlayDeathLoopAnimation(); // 사망 애니메이션 루프 재생
    }

    NotifyStatusValueChanged(); // UI 갱신
}

private void StartDeathBrokenClearRoutine() // 사망 직후 와해상태 반복 해제 시작
{
    if (deathBrokenClearCoroutine != null)
    {
        StopCoroutine(deathBrokenClearCoroutine); // 기존 반복 해제 코루틴 정리
        deathBrokenClearCoroutine = null; // 코루틴 참조 초기화
    }

    deathBrokenClearCoroutine = StartCoroutine(DeathBrokenClearRoutine()); // 사망 직후 와해상태 반복 해제 시작
}

private IEnumerator DeathBrokenClearRoutine() // 설정한 프레임 동안 와해상태 반복 해제
{
    int safeFrameCount = Mathf.Max(1, deathBrokenClearFrameCount); // 최소 1프레임 보장

    for (int i = 0; i < safeFrameCount; i++)
    {
        ForceClearBrokenStateByDeath(); // 사망 상태 기준 와해상태 강제 해제
        yield return null; // 다음 프레임까지 대기
    }

    deathBrokenClearCoroutine = null; // 반복 해제 완료
}

private void ForceClearBrokenStateByDeath() // 사망 상태에서 와해상태 강제 해제
{
    if (brokenStateCoroutine != null)
    {
        StopCoroutine(brokenStateCoroutine); // 와해 유지 코루틴 정지
        brokenStateCoroutine = null; // 코루틴 참조 초기화
    }

    isBrokenState = false; // 와해상태 해제
    currentBrokenTimer = 0f; // 와해 유지 타이머 초기화

    if (characterAnimationPlayer != null)
    {
        characterAnimationPlayer.ClearBrokenAnimationLock(); // 와해 애니메이션 잠금 해제
    }
}

public void ApplyOwnedCharacterStatData(SaveStorage.OwnedCharacterStatData statData) // 저장된 캐릭터 스탯 적용
{
    if (statData == null)
    {
        return; // 데이터가 없으면 종료
    }

    levelstats = Mathf.Max(1, statData.levelstats); // 레벨 적용
    attackPower = statData.attackPower; // 공격력 적용
    defenseValue = statData.defenseValue; // 방어율 적용
    bodySize = statData.bodySize; // 체급 적용

    maxHealth = Mathf.Max(0, statData.maxHealth); // 최대체력 적용
    currentHealth = Mathf.Clamp(statData.currentHealth, 0, maxHealth); // 현재체력 적용

    speedStat = statData.speedStat; // 속도 적용
    powerRatePercent = statData.powerRatePercent; // 위력률 적용

    baseMoveSpeed = statData.baseMoveSpeed; // 기본 이동속도 적용
    moveSpeedPercent = statData.moveSpeedPercent; // 이동속도율 적용

    maxStaggerAmount = Mathf.Max(0, statData.maxStaggerAmount); // 최대와해량 적용
    currentStaggerAmount = Mathf.Clamp(statData.currentStaggerAmount, 0, maxStaggerAmount); // 현재와해량 적용
    staggerResistancePercent = statData.staggerResistancePercent; // 와해 저항률 적용

    RefreshFinalMoveSpeed(); // 최종 이동속도 재계산
    NotifyStatusValueChanged(); // UI 갱신 알림
}

public void ApplyCalculatedStatsFromDefinition(
    GlobalCharacterDefinition definition,
    int targetLevel) // 캐릭터 정의와 레벨 기준 계산 스탯 적용
{
    if (definition == null)
    {
        return; // 정의가 없으면 종료
    }

    SaveStorage.OwnedCharacterStatData calculatedData = definition.CreateCalculatedStatData(
        0,
        0,
        0,
        targetLevel,
        0); // 전투용 계산 스탯 생성

    ApplyOwnedCharacterStatData(calculatedData); // 계산된 스탯 적용
}

public void SetNextAttackSkillCriticalState(bool useCriticalDamage, bool gainExperience) // 다음 공격기술 치명타 상태 설정
{
    applyCriticalToNextAttackSkill = useCriticalDamage;
    gainCriticalExperienceOnNextAttackSkillHit = gainExperience;
}

public bool ConsumeNextAttackSkillCriticalDamageState() // 다음 공격기술 치명타 피해 상태 1회 소비
{
    bool result = applyCriticalToNextAttackSkill;
    applyCriticalToNextAttackSkill = false;
    return result;
}

public bool ConsumeNextAttackSkillCriticalExperienceState() // 다음 공격기술 치명타 경험치 상태 1회 소비
{
    bool result = gainCriticalExperienceOnNextAttackSkillHit;
    gainCriticalExperienceOnNextAttackSkillHit = false;
    return result;
}



}