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
}