using System; // 상태값 변경 알림 이벤트용
using UnityEngine;

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

    RefreshFinalMoveSpeed(); // 최종 이동속도 계산 및 적용
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
    int safeRawDamage = Mathf.Max(0, rawDamage); // 음수 피해 방지
    int clampedDefensePercent = Mathf.Clamp(defenseValue, 0, 100); // 방어력 퍼센트 범위 제한

    int finalDamage = (safeRawDamage * (100 - clampedDefensePercent)) / 100; // 방어력 반영 최종 피해 계산

    if (finalDamage <= 0)
    {
        return 0; // 최종 피해가 없으면 종료
    }

    currentHealth = Mathf.Max(0, currentHealth - finalDamage); // 현재 체력 차감
    NotifyStatusValueChanged(); // 체력 UI 즉시 갱신 알림

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
}