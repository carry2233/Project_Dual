using System; // 상태값 변경 알림 이벤트용
using UnityEngine;
using System.Collections; // 와해상태 코루틴 처리용
using System.Collections.Generic; // 상태효과 리스트 관리용
using UnityEngine.UI; // GridLayoutGroup 사용

/// <summary>
/// 캐릭터의 레벨, 전투 스탯, 체력, 와해량, 이동속도 계산값을 관리한다.
/// 기본 이동속도와 이동속도 퍼센트를 계산해 최종 이동속도를 만들고,
/// 해당 값을 NavigationMovementSystem에 적용한다.
/// </summary>
public class CharacterStatSystem : MonoBehaviour
{

[System.Serializable]
public class AppliedStatusEffectData
{
    [Header("상태효과 적용 정보")]
    [SerializeField] private StatusEffectDefinitionSO statusEffectDefinition; // 적용 중인 상태효과 정의 SO 참조
    [SerializeField] private int statusEffectID; // 적용 중인 상태효과 ID
    [SerializeField] private int currentStack; // 현재 중첩 수치
    [SerializeField] private bool hasDuration; // 지속시간 사용 여부
    [SerializeField] private float remainingDuration; // 현재 남은 지속시간
    [SerializeField] private float durationTickTimer; // 이 상태효과만 사용하는 개별 1초 타이머

    public StatusEffectDefinitionSO StatusEffectDefinition => statusEffectDefinition; // 상태효과 정의 SO 반환
    public int StatusEffectID => statusEffectID; // 상태효과 ID 반환
    public int CurrentStack => currentStack; // 현재 중첩 반환
    public bool HasDuration => hasDuration; // 지속시간 사용 여부 반환
    public float RemainingDuration => remainingDuration; // 남은 지속시간 반환
    public float DurationTickTimer => durationTickTimer; // 개별 타이머 반환

    public AppliedStatusEffectData(
        StatusEffectDefinitionSO newStatusEffectDefinition,
        int newStack,
        int maxStack,
        bool newHasDuration,
        float newRemainingDuration)
    {
        statusEffectDefinition = newStatusEffectDefinition; // 상태효과 정의 SO 저장
        statusEffectID = newStatusEffectDefinition != null ? newStatusEffectDefinition.StatusEffectID : 0; // 상태효과 ID 저장
        currentStack = Mathf.Clamp(newStack, 1, Mathf.Max(1, maxStack)); // 중첩 수치 보정
        hasDuration = newHasDuration; // 지속시간 사용 여부 저장
        remainingDuration = newHasDuration ? Mathf.Max(0f, newRemainingDuration) : -1f; // 남은 지속시간 저장
        durationTickTimer = 0f; // 개별 타이머 초기화
    }

    public void SetStatusEffectDefinition(StatusEffectDefinitionSO newStatusEffectDefinition) // 상태효과 정의 SO 재설정
    {
        if (newStatusEffectDefinition == null)
        {
            return; // 정의가 없으면 변경하지 않음
        }

        statusEffectDefinition = newStatusEffectDefinition; // 상태효과 정의 SO 저장
        statusEffectID = newStatusEffectDefinition.StatusEffectID; // SO 기준으로 ID 동기화
    }

    public void AddStack(int addStack, int maxStack) // 중첩 추가
    {
        currentStack = Mathf.Clamp(currentStack + Mathf.Max(1, addStack), 1, Mathf.Max(1, maxStack)); // 최대 중첩 제한 적용
    }

    public bool DecreaseStack(int decreaseStack) // 중첩 감소
    {
        currentStack = Mathf.Max(0, currentStack - Mathf.Max(1, decreaseStack)); // 중첩 감소 후 0 미만 방지
        return currentStack <= 0; // 중첩이 0 이하이면 제거 필요
    }

    public void ResetDuration(float newRemainingDuration) // 지속시간 초기화
    {
        if (!hasDuration)
        {
            remainingDuration = -1f; // 무한 지속 상태효과 표시값
            durationTickTimer = 0f; // 개별 타이머 초기화
            return;
        }

        remainingDuration = Mathf.Max(0f, newRemainingDuration); // 남은 지속시간 재설정
        durationTickTimer = 0f; // 개별 타이머 초기화
    }

    public bool TickDuration(float deltaTime, out bool reducedDuration) // 개별 타이머 기준 지속시간 감소
    {
        reducedDuration = false; // 이번 프레임 지속시간 감소 여부

        if (!hasDuration)
        {
            durationTickTimer = 0f; // 무한 지속이면 타이머 사용 안 함
            return false;
        }

        durationTickTimer += Mathf.Max(0f, deltaTime); // 개별 타이머 증가

        while (durationTickTimer >= 1f)
        {
            durationTickTimer -= 1f; // 1초 소비
            remainingDuration -= 1f; // 지속시간 1 감소
            reducedDuration = true; // UI 갱신 필요 표시
        }

        return IsDurationEnded(); // 지속시간 종료 여부 반환
    }

    public bool IsDurationEnded() // 지속시간 종료 여부
    {
        if (!hasDuration)
        {
            return false; // 무한 지속이면 종료되지 않음
        }

        return remainingDuration <= 0f; // 0 이하이면 종료
    }
}

private class StatusEffectDisplayData // 상태효과 슬롯 표시용 통합 데이터
{
    public int StatusEffectID { get; private set; } // 표시할 상태효과 ID
    public Sprite StatusEffectIcon { get; private set; } // 표시할 상태효과 아이콘
    public int StackValue { get; private set; } // 표시할 중첩 수치
    public float DurationValue { get; private set; } // 표시할 지속시간
    public bool HasDuration { get; private set; } // 지속시간 사용 여부

    public StatusEffectDisplayData(
        int statusEffectID,
        Sprite statusEffectIcon,
        int stackValue,
        float durationValue,
        bool hasDuration)
    {
        StatusEffectID = statusEffectID; // 표시할 상태효과 ID 저장
        StatusEffectIcon = statusEffectIcon; // 표시할 아이콘 저장
        StackValue = Mathf.Max(1, stackValue); // 중첩 수치 보정
        DurationValue = durationValue; // 지속시간 저장
        HasDuration = hasDuration; // 지속시간 사용 여부 저장
    }
}

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

[Header("________________________________________________________________________________")]

[Header("상태효과 적용 목록")]
[SerializeField] private List<AppliedStatusEffectData> appliedStatusEffectList = new List<AppliedStatusEffectData>(); // 현재 캐릭터에게 적용 중인 상태효과 목록

[Header("상태효과 표시 UI")]
[SerializeField] private Canvas statusEffectWorldCanvas; // 상태효과 슬롯을 표시할 월드 캔버스
[SerializeField] private GridLayoutGroup statusEffectSlotGridPanel; // 상태효과 슬롯들이 배치될 Grid Layout Group 패널
[SerializeField] private StatusEffectDisplaySlot statusEffectDisplaySlotPrefab; // 상태효과 표시 슬롯 프리팹
[SerializeField] private List<StatusEffectDisplaySlot> activeStatusEffectDisplaySlotList = new List<StatusEffectDisplaySlot>(); // 현재 생성된 상태효과 표시 슬롯 목록

[Header("현재 스탯 상태효과 중첩 합산")]
[SerializeField] private int currentPowerRateIncreaseStack; // 현재 적용 중인 위력률 증가 상태효과 총 중첩
[SerializeField] private int currentPowerRateDecreaseStack; // 현재 적용 중인 위력률 감소 상태효과 총 중첩
[SerializeField] private int currentSpeedIncreaseStack; // 현재 적용 중인 속도 증가 상태효과 총 중첩
[SerializeField] private int currentSpeedDecreaseStack; // 현재 적용 중인 속도 감소 상태효과 총 중첩
[SerializeField] private int currentBodySizeIncreaseStack; // 현재 적용 중인 체급 증가 상태효과 총 중첩
[SerializeField] private int currentBodySizeDecreaseStack; // 현재 적용 중인 체급 감소 상태효과 총 중첩
[SerializeField] private int currentAttackPowerIncreaseStack; // 현재 적용 중인 공격력 증가 상태효과 총 중첩
[SerializeField] private int currentAttackPowerDecreaseStack; // 현재 적용 중인 공격력 감소 상태효과 총 중첩
[SerializeField] private int currentDefenseIncreaseStack; // 현재 적용 중인 방어 증가 상태효과 총 중첩
[SerializeField] private int currentDefenseDecreaseStack; // 현재 적용 중인 방어 감소 상태효과 총 중첩
[SerializeField] private int currentStaggerResistanceIncreaseStack; // 현재 적용 중인 와해 저항률 증가 상태효과 총 중첩
[SerializeField] private int currentStaggerResistanceDecreaseStack; // 현재 적용 중인 와해 저항률 감소 상태효과 총 중첩
[SerializeField] private int currentMoveSpeedRateIncreaseStack; // 현재 적용 중인 이동속도률 증가 상태효과 총 중첩
[SerializeField] private int currentMoveSpeedRateDecreaseStack; // 현재 적용 중인 이동속도률 감소 상태효과 총 중첩

[Header("상태효과 최종 보정치")]
[SerializeField] private int finalPowerRateCorrection; // 위력률 증가/감소 상태효과를 계산한 최종 보정치
[SerializeField] private int finalSpeedCorrection; // 속도 증가/감소 상태효과를 계산한 최종 보정치
[SerializeField] private int finalBodySizeCorrection; // 체급 증가/감소 상태효과를 계산한 최종 보정치
[SerializeField] private int finalAttackPowerCorrection; // 공격력 증가/감소 상태효과를 계산한 최종 보정치
[SerializeField] private int finalDefenseCorrection; // 방어 증가/감소 상태효과를 계산한 최종 보정치
[SerializeField] private int finalStaggerResistanceCorrection; // 와해 저항률 증가/감소 상태효과를 계산한 최종 보정치
[SerializeField] private int finalMoveSpeedRateCorrection; // 이동속도률 증가/감소 상태효과를 계산한 최종 보정치

[Header("상태효과 적용 최종 스탯")]
[SerializeField] private int finalPowerRatePercent; // 기본 위력률에 상태효과 보정치를 더한 최종 위력률
[SerializeField] private int finalSpeedStat; // 기본 속도에 상태효과 보정치를 더한 최종 속도
[SerializeField] private int finalBodySize; // 기본 체급에 상태효과 보정치를 더한 최종 체급
[SerializeField] private int finalAttackPower; // 기본 공격력에 상태효과 보정치를 더한 최종 공격력
[SerializeField] private int finalDefenseValue; // 기본 방어에 상태효과 보정치를 더한 최종 방어
[SerializeField] private int finalStaggerResistancePercent;
[SerializeField] private int finalMoveSpeedPercent; // 기본 이동속도률에 상태효과 보정치를 더한 최종 이동속도률

public int AttackPower => finalAttackPower; // 최종 공격력 반환
public int DefenseValue => finalDefenseValue; // 최종 방어력 반환
public int BodySize => finalBodySize; // 최종 체급 반환
public int SpeedStat => finalSpeedStat; // 최종 속도 수치 반환
public int MoveSpeedPercent => finalMoveSpeedPercent; // 최종 이동속도 퍼센트 반환
public int PowerRatePercent => finalPowerRatePercent; // 최종 위력률 퍼센트 반환


public IReadOnlyList<AppliedStatusEffectData> AppliedStatusEffectList => appliedStatusEffectList; // 외부에서 현재 적용 중인 상태효과 목록을 읽기 위한 프로퍼티

public int FinalPowerRateCorrection => finalPowerRateCorrection; // 외부에서 최종 위력률 보정치를 읽기 위한 프로퍼티
public int FinalSpeedCorrection => finalSpeedCorrection; // 외부에서 최종 속도 보정치를 읽기 위한 프로퍼티
public int FinalBodySizeCorrection => finalBodySizeCorrection; // 외부에서 최종 체급 보정치를 읽기 위한 프로퍼티
public int FinalAttackPowerCorrection => finalAttackPowerCorrection; // 외부에서 최종 공격력 보정치를 읽기 위한 프로퍼티
public int FinalDefenseCorrection => finalDefenseCorrection; // 외부에서 최종 방어 보정치를 읽기 위한 프로퍼티
public int FinalStaggerResistanceCorrection => finalStaggerResistanceCorrection; // 외부에서 최종 와해 저항률 보정치를 읽기 위한 프로퍼티
public int FinalMoveSpeedRateCorrection => finalMoveSpeedRateCorrection; // 외부에서 최종 이동속도률 보정치를 읽기 위한 프로퍼티



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
public int MaxHealth => maxHealth; // 최대 체력 반환
public int CurrentHealth => currentHealth; // 현재 체력 반환
public float BaseMoveSpeed => baseMoveSpeed; // 기본 이동속도 반환
public float FinalMoveSpeed => finalMoveSpeed; // 최종 적용 이동속도 반환

public int BattleSpeed => battleSpeed; // 현재 저장된 전투속도 반환
public int MinimumSpeedRatePercent => minimumSpeedRatePercent; // 최소 속도율 반환
public int MaximumSpeedRatePercent => maximumSpeedRatePercent; // 최대 속도율 반환
public bool IsActionLocked => isActionLocked; // 행동 불가 여부 반환

    public event Action<CharacterStatSystem> OnStatusValueChanged; // 스탯 값 변경 알림 이벤트


    public int RawAttackPower => attackPower; // 상태효과가 적용되지 않은 기본 공격력 반환
public int RawDefenseValue => defenseValue; // 상태효과가 적용되지 않은 기본 방어력 반환
public int RawBodySize => bodySize; // 상태효과가 적용되지 않은 기본 체급 반환
public int RawSpeedStat => speedStat; // 상태효과가 적용되지 않은 기본 속도 반환
public int RawMoveSpeedPercent => moveSpeedPercent; // 상태효과가 적용되지 않은 기본 이동속도률 반환
public int RawPowerRatePercent => powerRatePercent; // 상태효과가 적용되지 않은 기본 위력률 반환
public int RawStaggerResistancePercent => staggerResistancePercent; // 상태효과가 적용되지 않은 기본 와해 저항률 반환

public float RawCalculatedMoveSpeed // 상태효과가 적용되지 않은 기본 이동속도 계산값 반환
{
    get
    {
        int safeMoveSpeedPercent = Mathf.Max(0, moveSpeedPercent); // 기본 이동속도률 음수 방지
        float calculatedMoveSpeed = baseMoveSpeed * (safeMoveSpeedPercent / 100f); // 기본 이동속도 계산
        return Mathf.Floor(calculatedMoveSpeed * 10f) / 10f; // 소수점 첫째 자리까지 버림 처리
    }
}

    

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

    RefreshStatusEffectFinalStats(); // 상태효과 포함 최종 스탯 계산
    CheckDeathState(); // 시작 시 체력이 0이면 사망 처리
}

private void Start() // 씬 시작 시 상태효과 UI와 전역 매니저 참조 정리
{
    if (globalCharacterManager == null)
    {
        globalCharacterManager = GlobalCharacterManager.Instance; // 전역 캐릭터 매니저 자동 참조
    }

    RefreshStatusEffectDisplaySlots(); // 시작 시 상태효과 슬롯 UI 갱신
}

private void Update() // 매 프레임 사망 상태와 상태효과 지속시간 확인
{
    CheckDeathState(); // 현재 체력 기반 사망 상태 확인
    UpdateStatusEffectDuration(); // 상태효과 지속시간 처리
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
    int clampedDefensePercent = Mathf.Clamp(finalDefenseValue, 0, 100); // 최종 방어력 퍼센트 범위 제한

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
    int clampedResistancePercent = Mathf.Clamp(finalStaggerResistancePercent, 0, 100); // 최종 저지율 퍼센트 범위 제한  

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
    int safeMoveSpeedPercent = Mathf.Max(0, finalMoveSpeedPercent); // 최종 이동속도률 음수 방지
    float calculatedMoveSpeed = baseMoveSpeed * (safeMoveSpeedPercent / 100f); // 기본 이동속도와 최종 퍼센트 계산

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

    RefreshStatusEffectFinalStats(); // 상태효과 포함 최종 스탯 재계산
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

public bool ApplyStatusEffectByID(int statusEffectID, int addStack = 1, float overrideDuration = -1f) // ID 기준 상태효과 적용
{
    if (StatusEffectDefinitionList.Instance == null)
    {
        return false; // 상태효과 정의 리스트가 없으면 실패
    }

    StatusEffectDefinitionSO definition = StatusEffectDefinitionList.Instance.GetStatusEffectDefinitionByID(statusEffectID); // ID로 상태효과 정의 탐색

    if (definition == null)
    {
        return false; // 상태효과 정의가 없으면 실패
    }

    return ApplyStatusEffect(definition, addStack, overrideDuration); // 정의 기반 상태효과 적용
}

public bool ApplyStatusEffectWithoutDuration(StatusEffectDefinitionSO definition, int applyStack = 1) // 지속시간 없는 상태효과 적용
{
    if (definition == null)
    {
        return false; // 상태효과 정의가 없으면 실패
    }

    if (definition.HasDuration)
    {
        Debug.LogWarning("ApplyStatusEffectWithoutDuration은 지속시간이 없는 상태효과에만 사용해야 합니다.");
        return false; // 지속형 상태효과면 실패
    }

    return ApplyStatusEffect(definition, Mathf.Max(1, applyStack), -1f); // 지속시간 없이 상태효과 적용
}

public bool ApplyStatusEffect(StatusEffectDefinitionSO definition, int addStack = 1, float overrideDuration = -1f) // 상태효과 정의 기반 적용
{
    if (definition == null)
    {
        return false; // 정의가 없으면 적용 실패
    }

    if (definition.HasDuration && overrideDuration < 0f)
    {
        Debug.LogWarning("지속시간이 있는 상태효과는 overrideDuration 값을 0 이상으로 전달해야 합니다.");
        return false; // 지속형 상태효과는 외부에서 지속시간을 넘겨야 함
    }

    int safeAddStack = Mathf.Max(1, addStack); // 추가 중첩값 보정
    int maxStack = Mathf.Max(1, definition.MaxStackLimit); // 최대 중첩값 보정
    float applyDuration = definition.HasDuration ? Mathf.Max(0f, overrideDuration) : -1f; // 적용할 지속시간 계산

    AppliedStatusEffectData existingData = GetAppliedStatusEffectData(definition.StatusEffectID); // 기존 적용 데이터 탐색

    if (existingData != null)
    {
        existingData.SetStatusEffectDefinition(definition); // 기존 데이터에 상태효과 SO 참조 보정
        existingData.AddStack(safeAddStack, maxStack); // 기존 상태효과 중첩 증가

        if (definition.HasDuration)
        {
            existingData.ResetDuration(applyDuration); // 지속형 상태효과면 지속시간 갱신
        }

        RefreshStatusEffectFinalStats(); // 상태효과 포함 최종 스탯 재계산
        RefreshStatusEffectDisplaySlots(); // 상태효과 UI 갱신
        return true;
    }

    AppliedStatusEffectData newData = new AppliedStatusEffectData(
        definition, // 상태효과 정의 SO 참조
        safeAddStack, // 적용 중첩
        maxStack, // 최대 중첩
        definition.HasDuration, // 지속시간 사용 여부
        applyDuration); // 적용 지속시간

    appliedStatusEffectList.Add(newData); // 적용 목록에 새 상태효과 추가

    RefreshStatusEffectFinalStats(); // 상태효과 포함 최종 스탯 재계산
    RefreshStatusEffectDisplaySlots(); // 상태효과 UI 갱신
    return true;
}
public bool RemoveStatusEffectByID(int statusEffectID) // ID 기준 상태효과 제거
{
    for (int i = appliedStatusEffectList.Count - 1; i >= 0; i--)
    {
        AppliedStatusEffectData data = appliedStatusEffectList[i]; // 현재 적용 데이터

        if (data == null)
        {
            continue; // 비어 있으면 건너뜀
        }

        if (data.StatusEffectID != statusEffectID)
        {
            continue; // ID가 다르면 건너뜀
        }

        appliedStatusEffectList.RemoveAt(i); // 상태효과 제거
        RefreshStatusEffectFinalStats(); // 최종 스탯 재계산
        RefreshStatusEffectDisplaySlots(); // 상태효과 슬롯 UI 갱신
        return true;
    }

    return false; // 제거 대상 없음
}

public bool DecreaseStatusEffectStackByID(int statusEffectID, int decreaseStack = 1) // ID 기준 상태효과 중첩 감소
{
    for (int i = appliedStatusEffectList.Count - 1; i >= 0; i--)
    {
        AppliedStatusEffectData data = appliedStatusEffectList[i]; // 현재 적용 데이터

        if (data == null)
        {
            continue; // 비어 있으면 건너뜀
        }

        if (data.StatusEffectID != statusEffectID)
        {
            continue; // ID가 다르면 건너뜀
        }

        bool shouldRemove = data.DecreaseStack(decreaseStack); // 중첩 감소 후 제거 필요 여부 확인

        if (shouldRemove)
        {
            appliedStatusEffectList.RemoveAt(i); // 중첩이 0 이하이면 상태효과 제거
        }

        RefreshStatusEffectFinalStats(); // 최종 스탯 재계산
        RefreshStatusEffectDisplaySlots(); // 상태효과 UI 갱신
        return true;
    }

    return false; // 감소 대상 없음
}

public void ClearAllStatusEffects() // 모든 상태효과 제거
{
    appliedStatusEffectList.Clear(); // 적용 중인 상태효과 목록 초기화

    RefreshStatusEffectFinalStats(); // 최종 스탯 재계산
    RefreshStatusEffectDisplaySlots(); // 상태효과 UI 갱신
}
private AppliedStatusEffectData GetAppliedStatusEffectData(int statusEffectID) // 현재 적용 중인 상태효과 검색
{
    for (int i = 0; i < appliedStatusEffectList.Count; i++)
    {
        AppliedStatusEffectData data = appliedStatusEffectList[i];

        if (data == null)
        {
            continue;
        }

        if (data.StatusEffectID == statusEffectID)
        {
            return data;
        }
    }

    return null;
}

private void UpdateStatusEffectDuration() // 상태효과별 개별 타이머 기준 지속시간 감소
{
    if (appliedStatusEffectList == null || appliedStatusEffectList.Count <= 0)
    {
        return; // 적용 중인 상태효과가 없으면 종료
    }

    bool removedAnyStatusEffect = false; // 제거된 상태효과 존재 여부
    bool changedAnyDisplayValue = false; // 지속시간 표시값 변경 여부

    for (int i = appliedStatusEffectList.Count - 1; i >= 0; i--)
    {
        AppliedStatusEffectData data = appliedStatusEffectList[i]; // 현재 상태효과 데이터

        if (data == null)
        {
            appliedStatusEffectList.RemoveAt(i); // 비어 있는 데이터 제거
            removedAnyStatusEffect = true; // 제거 발생 체크
            continue;
        }

        bool reducedDuration; // 이번 프레임에서 지속시간 수치가 감소했는지 여부
        bool durationEnded = data.TickDuration(Time.deltaTime, out reducedDuration); // 상태효과 개별 타이머로 지속시간 처리

        if (reducedDuration)
        {
            changedAnyDisplayValue = true; // 지속시간 UI 갱신 필요
        }

        if (durationEnded)
        {
            appliedStatusEffectList.RemoveAt(i); // 지속시간이 끝난 상태효과 제거
            removedAnyStatusEffect = true; // 제거 발생 체크
        }
    }

    if (removedAnyStatusEffect)
    {
        RefreshStatusEffectFinalStats(); // 제거된 상태효과가 있으면 최종 스탯 재계산
    }

    if (removedAnyStatusEffect || changedAnyDisplayValue)
    {
        RefreshStatusEffectDisplaySlots(); // 지속시간 또는 제거 상태를 UI에 반영
    }
}

private void RefreshStatusEffectFinalStats() // 상태효과를 포함한 최종 스탯 계산
{
    ResetCurrentBaseStatStatusEffectStacks(); // 스탯 상태효과 중첩 합산값 초기화

    for (int i = 0; i < appliedStatusEffectList.Count; i++)
    {
        AppliedStatusEffectData appliedData = appliedStatusEffectList[i]; // 현재 적용 중인 상태효과 데이터

        if (appliedData == null)
        {
            continue; // 비어 있으면 건너뜀
        }

        StatusEffectDefinitionSO definition = GetAppliedStatusEffectDefinition(appliedData); // 적용 데이터에서 상태효과 정의 SO 가져오기

        if (definition == null)
        {
            continue; // 상태효과 정의를 찾지 못하면 스탯 상태효과 적용 불가
        }

        int statusStack = Mathf.Max(1, appliedData.CurrentStack); // 현재 상태효과 중첩값 보정

        currentPowerRateIncreaseStack += definition.PowerRateIncreaseStackValue * statusStack; // 위력률 증가 중첩 합산
        currentPowerRateDecreaseStack += definition.PowerRateDecreaseStackValue * statusStack; // 위력률 감소 중첩 합산

        currentSpeedIncreaseStack += definition.SpeedIncreaseStackValue * statusStack; // 속도 증가 중첩 합산
        currentSpeedDecreaseStack += definition.SpeedDecreaseStackValue * statusStack; // 속도 감소 중첩 합산

        currentBodySizeIncreaseStack += definition.BodySizeIncreaseStackValue * statusStack; // 체급 증가 중첩 합산
        currentBodySizeDecreaseStack += definition.BodySizeDecreaseStackValue * statusStack; // 체급 감소 중첩 합산

        currentAttackPowerIncreaseStack += definition.AttackPowerIncreaseStackValue * statusStack; // 공격력 증가 중첩 합산
        currentAttackPowerDecreaseStack += definition.AttackPowerDecreaseStackValue * statusStack; // 공격력 감소 중첩 합산

        currentDefenseIncreaseStack += definition.DefenseIncreaseStackValue * statusStack; // 방어 증가 중첩 합산
        currentDefenseDecreaseStack += definition.DefenseDecreaseStackValue * statusStack; // 방어 감소 중첩 합산

        currentStaggerResistanceIncreaseStack += definition.StaggerResistanceIncreaseStackValue * statusStack; // 와해 저항률 증가 중첩 합산
        currentStaggerResistanceDecreaseStack += definition.StaggerResistanceDecreaseStackValue * statusStack; // 와해 저항률 감소 중첩 합산

        currentMoveSpeedRateIncreaseStack += definition.MoveSpeedRateIncreaseStackValue * statusStack; // 이동속도률 증가 중첩 합산
        currentMoveSpeedRateDecreaseStack += definition.MoveSpeedRateDecreaseStackValue * statusStack; // 이동속도률 감소 중첩 합산
    }

    CalculateFinalStatCorrectionValues(); // 합산된 중첩을 실제 보정치로 변환
    ApplyFinalStatValues(); // 기본 스탯 + 보정치로 최종 스탯 적용
    RefreshFinalMoveSpeed(); // 최종 이동속도 재계산
}

private StatusEffectDefinitionSO GetAppliedStatusEffectDefinition(AppliedStatusEffectData appliedData) // 적용 데이터에서 상태효과 정의 SO 찾기
{
    if (appliedData == null)
    {
        return null; // 적용 데이터가 없으면 실패
    }

    if (appliedData.StatusEffectDefinition != null)
    {
        return appliedData.StatusEffectDefinition; // 이미 SO 참조가 있으면 그대로 사용
    }

    if (StatusEffectDefinitionList.Instance == null)
    {
        return null; // SO 참조도 없고 정의 리스트도 없으면 실패
    }

    StatusEffectDefinitionSO definition = StatusEffectDefinitionList.Instance.GetStatusEffectDefinitionByID(appliedData.StatusEffectID); // ID 기준으로 정의 검색

    if (definition != null)
    {
        appliedData.SetStatusEffectDefinition(definition); // 찾은 SO를 적용 데이터에 다시 저장
    }

    return definition; // 찾은 정의 반환
}

private void ResetCurrentBaseStatStatusEffectStacks() // 스탯 상태효과 중첩 합산값 초기화
{
    currentPowerRateIncreaseStack = 0;
    currentPowerRateDecreaseStack = 0;

    currentSpeedIncreaseStack = 0;
    currentSpeedDecreaseStack = 0;

    currentBodySizeIncreaseStack = 0;
    currentBodySizeDecreaseStack = 0;

    currentAttackPowerIncreaseStack = 0;
    currentAttackPowerDecreaseStack = 0;

    currentDefenseIncreaseStack = 0;
    currentDefenseDecreaseStack = 0;

    currentStaggerResistanceIncreaseStack = 0;
    currentStaggerResistanceDecreaseStack = 0;

    currentMoveSpeedRateIncreaseStack = 0;
    currentMoveSpeedRateDecreaseStack = 0;
}

private void CalculateFinalStatCorrectionValues() // 증가/감소 중첩을 실제 보정치로 변환
{
    BaseStatStatusEffectDefinitionManager manager = BaseStatStatusEffectDefinitionManager.Instance;

    if (manager == null)
    {
        finalPowerRateCorrection = 0;
        finalSpeedCorrection = 0;
        finalBodySizeCorrection = 0;
        finalAttackPowerCorrection = 0;
        finalDefenseCorrection = 0;
        finalStaggerResistanceCorrection = 0;
        finalMoveSpeedRateCorrection = 0;
        return;
    }

    int powerRateIncreaseValue = currentPowerRateIncreaseStack * manager.PowerRateIncreaseApplyValuePerStack;
    int powerRateDecreaseValue = currentPowerRateDecreaseStack * manager.PowerRateDecreaseApplyValuePerStack;
    finalPowerRateCorrection = powerRateIncreaseValue - powerRateDecreaseValue;

    int speedIncreaseValue = currentSpeedIncreaseStack * manager.SpeedIncreaseApplyValuePerStack;
    int speedDecreaseValue = currentSpeedDecreaseStack * manager.SpeedDecreaseApplyValuePerStack;
    finalSpeedCorrection = speedIncreaseValue - speedDecreaseValue;

    int bodySizeIncreaseValue = currentBodySizeIncreaseStack * manager.BodySizeIncreaseApplyValuePerStack;
    int bodySizeDecreaseValue = currentBodySizeDecreaseStack * manager.BodySizeDecreaseApplyValuePerStack;
    finalBodySizeCorrection = bodySizeIncreaseValue - bodySizeDecreaseValue;

    int attackPowerIncreaseValue = currentAttackPowerIncreaseStack * manager.AttackPowerIncreaseApplyValuePerStack;
    int attackPowerDecreaseValue = currentAttackPowerDecreaseStack * manager.AttackPowerDecreaseApplyValuePerStack;
    finalAttackPowerCorrection = attackPowerIncreaseValue - attackPowerDecreaseValue;

    int defenseIncreaseValue = currentDefenseIncreaseStack * manager.DefenseIncreaseApplyValuePerStack;
    int defenseDecreaseValue = currentDefenseDecreaseStack * manager.DefenseDecreaseApplyValuePerStack;
    finalDefenseCorrection = defenseIncreaseValue - defenseDecreaseValue;

    int staggerResistanceIncreaseValue = currentStaggerResistanceIncreaseStack * manager.StaggerResistanceIncreaseApplyValuePerStack;
    int staggerResistanceDecreaseValue = currentStaggerResistanceDecreaseStack * manager.StaggerResistanceDecreaseApplyValuePerStack;
    finalStaggerResistanceCorrection = staggerResistanceIncreaseValue - staggerResistanceDecreaseValue;

    int moveSpeedRateIncreaseValue = currentMoveSpeedRateIncreaseStack * manager.MoveSpeedRateIncreaseApplyValuePerStack;
    int moveSpeedRateDecreaseValue = currentMoveSpeedRateDecreaseStack * manager.MoveSpeedRateDecreaseApplyValuePerStack;
    finalMoveSpeedRateCorrection = moveSpeedRateIncreaseValue - moveSpeedRateDecreaseValue;
}

private void ApplyFinalStatValues() // 기본 스탯 + 보정치로 실제 최종 스탯 적용
{
    finalPowerRatePercent = Mathf.Max(0, powerRatePercent + finalPowerRateCorrection);
    finalSpeedStat = Mathf.Max(0, speedStat + finalSpeedCorrection);
    finalBodySize = Mathf.Max(0, bodySize + finalBodySizeCorrection);
    finalAttackPower = Mathf.Max(0, attackPower + finalAttackPowerCorrection);
    finalDefenseValue = Mathf.Max(0, defenseValue + finalDefenseCorrection);
    finalStaggerResistancePercent = Mathf.Max(0, staggerResistancePercent + finalStaggerResistanceCorrection);
    finalMoveSpeedPercent = Mathf.Max(0, moveSpeedPercent + finalMoveSpeedRateCorrection);
}

private void RefreshStatusEffectDisplaySlots() // 현재 적용 중인 상태효과 슬롯 UI 전체 갱신
{
    ClearStatusEffectDisplaySlots(); // 기존 슬롯 제거

    List<StatusEffectDisplayData> displayDataList = new List<StatusEffectDisplayData>(); // 실제로 표시할 상태효과 슬롯 데이터 목록

    AddAppliedStatusEffectDisplayData(displayDataList); // 일반 상태효과 슬롯 데이터 추가
    AddCurrentBaseStatStatusEffectDisplayData(displayDataList); // 현재 스탯 상태효과 슬롯 데이터 추가

    if (statusEffectWorldCanvas != null)
    {
        statusEffectWorldCanvas.gameObject.SetActive(displayDataList.Count > 0); // 표시할 슬롯이 있을 때만 월드 캔버스 활성화
    }

    if (statusEffectSlotGridPanel == null || statusEffectDisplaySlotPrefab == null)
    {
        return; // 슬롯 생성에 필요한 참조가 없으면 종료
    }

    if (displayDataList.Count <= 0)
    {
        return; // 표시할 상태효과가 없으면 종료
    }

    displayDataList.Sort(CompareStatusEffectDisplayDataPriority); // 우선순위 낮은 순으로 정렬

    for (int i = 0; i < displayDataList.Count; i++)
    {
        StatusEffectDisplayData displayData = displayDataList[i]; // 생성할 슬롯 데이터

        StatusEffectDisplaySlot newSlot = Instantiate(statusEffectDisplaySlotPrefab, statusEffectSlotGridPanel.transform); // 슬롯 생성

        newSlot.Initialize(
            displayData.StatusEffectID, // 표시할 상태효과 ID
            displayData.StatusEffectIcon, // 표시할 이미지
            displayData.StackValue, // 현재 중첩
            displayData.DurationValue, // 현재 지속시간
            displayData.HasDuration, // 지속시간 사용 여부
            globalCharacterManager); // 설명 UI를 담당할 전역 매니저

        activeStatusEffectDisplaySlotList.Add(newSlot); // 생성 슬롯 목록에 추가
    }
}

private void ClearStatusEffectDisplaySlots() // 생성된 상태효과 슬롯 제거
{
    for (int i = activeStatusEffectDisplaySlotList.Count - 1; i >= 0; i--)
    {
        StatusEffectDisplaySlot slot = activeStatusEffectDisplaySlotList[i]; // 현재 슬롯

        if (slot != null)
        {
            Destroy(slot.gameObject); // 슬롯 오브젝트 제거
        }
    }

    activeStatusEffectDisplaySlotList.Clear(); // 슬롯 목록 초기화
}

private int CompareStatusEffectDisplayPriority(AppliedStatusEffectData left, AppliedStatusEffectData right) // 상태효과 슬롯 정렬 비교
{
    int leftPriority = GetStatusEffectDisplaySortPriority(left.StatusEffectID); // 왼쪽 상태효과 우선순위
    int rightPriority = GetStatusEffectDisplaySortPriority(right.StatusEffectID); // 오른쪽 상태효과 우선순위

    if (leftPriority != rightPriority)
    {
        return leftPriority.CompareTo(rightPriority); // 우선순위 낮은 순 정렬
    }

    return left.StatusEffectID.CompareTo(right.StatusEffectID); // 우선순위가 같으면 ID 낮은 순 정렬
}

private int GetStatusEffectDisplaySortPriority(int statusEffectID) // 상태효과 표시 우선순위 반환
{
    if (globalCharacterManager == null)
    {
        globalCharacterManager = GlobalCharacterManager.Instance; // 전역 캐릭터 매니저 재참조
    }

    return globalCharacterManager != null
        ? globalCharacterManager.GetStatusEffectSortPriority(statusEffectID)
        : 9999; // 우선순위 반환
}

private Sprite GetStatusEffectDisplayIcon(int statusEffectID) // 상태효과 표시 아이콘 반환
{
    if (globalCharacterManager == null)
    {
        globalCharacterManager = GlobalCharacterManager.Instance; // 전역 캐릭터 매니저 재참조
    }

    return globalCharacterManager != null
        ? globalCharacterManager.GetStatusEffectIcon(statusEffectID)
        : null; // 아이콘 반환
}

private void AddAppliedStatusEffectDisplayData(List<StatusEffectDisplayData> displayDataList) // 일반 상태효과 슬롯 데이터 추가
{
    if (displayDataList == null)
    {
        return; // 표시 목록이 없으면 종료
    }

    if (appliedStatusEffectList == null || appliedStatusEffectList.Count <= 0)
    {
        return; // 적용 중인 일반 상태효과가 없으면 종료
    }

    for (int i = 0; i < appliedStatusEffectList.Count; i++)
    {
        AppliedStatusEffectData data = appliedStatusEffectList[i]; // 현재 적용 중인 일반 상태효과 데이터

        if (data == null)
        {
            continue; // 비어 있으면 건너뜀
        }

        AddStatusEffectDisplayDataIfValid(
            displayDataList, // 표시 목록
            data.StatusEffectID, // 일반 상태효과 ID
            data.CurrentStack, // 일반 상태효과 현재 중첩
            data.RemainingDuration, // 일반 상태효과 남은 지속시간
            data.HasDuration); // 일반 상태효과 지속시간 사용 여부
    }
}

private void AddCurrentBaseStatStatusEffectDisplayData(List<StatusEffectDisplayData> displayDataList) // 현재 합산된 스탯 상태효과 슬롯 데이터 추가
{
    if (displayDataList == null)
    {
        return; // 표시 목록이 없으면 종료
    }

    BaseStatStatusEffectDefinitionManager manager = BaseStatStatusEffectDefinitionManager.Instance; // 스탯 상태효과 정의 관리자 참조

    if (manager == null)
    {
        return; // 스탯 상태효과 정의 관리자가 없으면 표시 불가
    }

    AddStatusEffectDisplayDataIfValid(displayDataList, manager.PowerRateIncreaseStatusEffectID, currentPowerRateIncreaseStack, -1f, false); // 위력률 증가 표시
    AddStatusEffectDisplayDataIfValid(displayDataList, manager.PowerRateDecreaseStatusEffectID, currentPowerRateDecreaseStack, -1f, false); // 위력률 감소 표시

    AddStatusEffectDisplayDataIfValid(displayDataList, manager.SpeedIncreaseStatusEffectID, currentSpeedIncreaseStack, -1f, false); // 속도 증가 표시
    AddStatusEffectDisplayDataIfValid(displayDataList, manager.SpeedDecreaseStatusEffectID, currentSpeedDecreaseStack, -1f, false); // 속도 감소 표시

    AddStatusEffectDisplayDataIfValid(displayDataList, manager.BodySizeIncreaseStatusEffectID, currentBodySizeIncreaseStack, -1f, false); // 체급 증가 표시
    AddStatusEffectDisplayDataIfValid(displayDataList, manager.BodySizeDecreaseStatusEffectID, currentBodySizeDecreaseStack, -1f, false); // 체급 감소 표시

    AddStatusEffectDisplayDataIfValid(displayDataList, manager.AttackPowerIncreaseStatusEffectID, currentAttackPowerIncreaseStack, -1f, false); // 공격력 증가 표시
    AddStatusEffectDisplayDataIfValid(displayDataList, manager.AttackPowerDecreaseStatusEffectID, currentAttackPowerDecreaseStack, -1f, false); // 공격력 감소 표시

    AddStatusEffectDisplayDataIfValid(displayDataList, manager.DefenseIncreaseStatusEffectID, currentDefenseIncreaseStack, -1f, false); // 방어 증가 표시
    AddStatusEffectDisplayDataIfValid(displayDataList, manager.DefenseDecreaseStatusEffectID, currentDefenseDecreaseStack, -1f, false); // 방어 감소 표시

    AddStatusEffectDisplayDataIfValid(displayDataList, manager.StaggerResistanceIncreaseStatusEffectID, currentStaggerResistanceIncreaseStack, -1f, false); // 와해 저항률 증가 표시
    AddStatusEffectDisplayDataIfValid(displayDataList, manager.StaggerResistanceDecreaseStatusEffectID, currentStaggerResistanceDecreaseStack, -1f, false); // 와해 저항률 감소 표시

    AddStatusEffectDisplayDataIfValid(displayDataList, manager.MoveSpeedRateIncreaseStatusEffectID, currentMoveSpeedRateIncreaseStack, -1f, false); // 이동속도률 증가 표시
    AddStatusEffectDisplayDataIfValid(displayDataList, manager.MoveSpeedRateDecreaseStatusEffectID, currentMoveSpeedRateDecreaseStack, -1f, false); // 이동속도률 감소 표시
}

private void AddStatusEffectDisplayDataIfValid(
    List<StatusEffectDisplayData> displayDataList,
    int statusEffectID,
    int stackValue,
    float durationValue,
    bool hasDuration) // 유효한 상태효과만 표시 목록에 추가
{
    if (displayDataList == null)
    {
        return; // 표시 목록이 없으면 종료
    }

    if (statusEffectID <= 0)
    {
        return; // ID가 유효하지 않으면 표시하지 않음
    }

    if (stackValue <= 0)
    {
        return; // 중첩이 0 이하이면 표시하지 않음
    }

    Sprite displayIcon = GetStatusEffectDisplayIcon(statusEffectID); // 상태효과 아이콘 검색

    StatusEffectDisplayData displayData = new StatusEffectDisplayData(
        statusEffectID, // 표시할 상태효과 ID
        displayIcon, // 표시할 아이콘
        stackValue, // 표시할 중첩 수치
        durationValue, // 표시할 지속시간
        hasDuration); // 지속시간 사용 여부

    displayDataList.Add(displayData); // 표시 목록에 추가
}

private int CompareStatusEffectDisplayDataPriority(StatusEffectDisplayData left, StatusEffectDisplayData right) // 통합 상태효과 슬롯 정렬 비교
{
    int leftPriority = GetStatusEffectDisplaySortPriority(left.StatusEffectID); // 왼쪽 상태효과 우선순위
    int rightPriority = GetStatusEffectDisplaySortPriority(right.StatusEffectID); // 오른쪽 상태효과 우선순위

    if (leftPriority != rightPriority)
    {
        return leftPriority.CompareTo(rightPriority); // 우선순위 낮은 순 정렬
    }

    return left.StatusEffectID.CompareTo(right.StatusEffectID); // 우선순위가 같으면 ID 낮은 순 정렬
}




}