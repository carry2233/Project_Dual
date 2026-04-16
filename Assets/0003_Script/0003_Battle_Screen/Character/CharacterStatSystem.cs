using UnityEngine;

/// <summary>
/// 캐릭터 스탯 보관 스크립트
/// - 현재 단계에서는 기능 없이 변수만 관리
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
[SerializeField] private float moveSpeedPerSpeedPoint = 1f; // 속도 1당 이동속도 적용값

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
public float MoveSpeedPerSpeedPoint => moveSpeedPerSpeedPoint; // 속도 1당 이동속도 적용값 반환

public int BattleSpeed => battleSpeed; // 현재 저장된 전투속도 반환
public int MinimumSpeedRatePercent => minimumSpeedRatePercent; // 최소 속도율 반환
public int MaximumSpeedRatePercent => maximumSpeedRatePercent; // 최대 속도율 반환
public int PowerRatePercent => powerRatePercent; // 위력률 퍼센트 반환
public bool IsActionLocked => isActionLocked; // 행동 불가 여부 반환

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

    return finalStaggerDamage; // 실제 적용된 최종 와해피해 반환
}
}