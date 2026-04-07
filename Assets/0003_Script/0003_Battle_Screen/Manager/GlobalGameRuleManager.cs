using UnityEngine;

/// <summary>
/// 전역 게임 규칙 매니져
/// - 결투 판정 규칙
/// - 속도 차이 기반 위력률 보정 규칙
/// - 결과별 밀려나는 힘 규칙
/// </summary>
public class GlobalGameRuleManager : MonoBehaviour
{
    public enum DuelResultType
    {
        Hit,        // 적중
        Advantage,  // 우세
        Even,       // 비등
        Disadvantage, // 열세
        Damaged     // 피격
    }

    public static GlobalGameRuleManager Instance { get; private set; } // 싱글톤 인스턴스

    [Header("차이율 판정 구간")]
    [SerializeField] private float damagedThreshold = -25f; // 피격 최소 기준값
    [SerializeField] private float disadvantageThreshold = -5f; // 열세 최소 기준값
    [SerializeField] private float evenMinThreshold = -4f; // 비등 최소 기준값
    [SerializeField] private float evenMaxThreshold = 4f; // 비등 최대 기준값
    [SerializeField] private float advantageThreshold = 5f; // 우세 최소 기준값
    [SerializeField] private float hitThreshold = 25f; // 적중 최소 기준값

    [Header("속도 차이 기반 위력률 증가 규칙")]
    [SerializeField] private int speedDifferenceUnit = 1; // 속도 차이 n 기준값
    [SerializeField] private int bonusPowerRatePercentPerUnit = 5; // 속도 차이 n당 증가할 위력률 퍼센트

[Header("결과별 밀려나는 힘")]
[SerializeField] private float evenRepelForce = 5f; // 비등 시 양측 기본 넉백값(음수면 반대 방향)
[SerializeField] private float advantageRepelForce = 4f; // 우세 시 기본 넉백값(음수면 반대 방향)
[SerializeField] private float disadvantageRepelForce = 7f; // 열세 시 기본 넉백값(음수면 반대 방향)
[SerializeField] private float hitRepelForce = 4f; // 적중 시 기본 넉백값(음수면 반대 방향)
[SerializeField] private float damagedRepelForce = 8f; // 피격 시 기본 넉백값(음수면 반대 방향)

    public struct DuelCombatDebugData
{
    public int rolledSpeedRatePercent; // 이번 결투에서 뽑힌 속도율
    public int battleSpeed; // 계산된 전투속도
    public int battleSpeedDifference; // 상대와의 전투속도 차이
    public int bonusPowerRatePercent; // 전투속도 차이로 얻은 추가 위력률
    public int basePowerRatePercent; // 원래 위력률
    public int finalPowerRatePercent; // 최종 위력률
    public int attackPower; // 공격력
    public int attackPowerValue; // 최종 공격위력값
}

    private void Awake() // 싱글톤 초기화
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // 중복 인스턴스 제거
            return;
        }

        Instance = this; // 싱글톤 인스턴스 저장
    }

public int GetBonusPowerRateFromSpeed(int selfSpeed, int otherSpeed) // 기존 호출 호환용 메서드
{
    return GetBonusPowerRateFromBattleSpeed(selfSpeed, otherSpeed); // 전투속도 기준 계산으로 연결
}

    public int CalculateAttackPowerValue(CharacterStatSystem selfStat, CharacterStatSystem otherStat) // 결투용 공격위력값 계산
    {
        if (selfStat == null || otherStat == null)
        {
            return 0; // 스탯 참조가 없으면 0 반환
        }

        int bonusPowerRate = GetBonusPowerRateFromSpeed(selfStat.SpeedStat, otherStat.SpeedStat); // 속도 차이에 따른 추가 위력률 계산
        int finalPowerRatePercent = selfStat.PowerRatePercent + bonusPowerRate; // 최종 위력률 계산
        int attackPowerValue = (finalPowerRatePercent * selfStat.AttackPower) / 100; // 공격위력값 계산

        return attackPowerValue; // 최종 공격위력값 반환
    }

    public float CalculateDifferenceRate(int selfAttackPowerValue, int otherAttackPowerValue) // 차이율 계산
    {
        float averageValue = (selfAttackPowerValue + otherAttackPowerValue) / 2f; // 두 공격위력값 평균 계산

        if (Mathf.Approximately(averageValue, 0f))
        {
            return 0f; // 평균이 0이면 차이율 0 처리
        }

        float differenceValue = selfAttackPowerValue - otherAttackPowerValue; // 두 값의 차이량 계산
        return (differenceValue / averageValue) * 100f; // 차이율 퍼센트 반환
    }

    public DuelResultType EvaluateDuelResult(float differenceRate) // 차이율에 따른 결과 판정
    {
        if (differenceRate <= damagedThreshold)
        {
            return DuelResultType.Damaged; // 피격
        }

        if (differenceRate >= hitThreshold)
        {
            return DuelResultType.Hit; // 적중
        }

        if (differenceRate >= advantageThreshold)
        {
            return DuelResultType.Advantage; // 우세
        }

        if (differenceRate >= evenMinThreshold && differenceRate <= evenMaxThreshold)
        {
            return DuelResultType.Even; // 비등
        }

        return DuelResultType.Disadvantage; // 열세
    }

public float GetRepelForceByResult(DuelResultType resultType) // 결과에 따른 기본 넉백값 반환(음수 허용)
{
    switch (resultType)
    {
        case DuelResultType.Hit:
            return hitRepelForce; // 적중 기본 넉백값 반환

        case DuelResultType.Advantage:
            return advantageRepelForce; // 우세 기본 넉백값 반환

        case DuelResultType.Even:
            return evenRepelForce; // 비등 기본 넉백값 반환

        case DuelResultType.Disadvantage:
            return disadvantageRepelForce; // 열세 기본 넉백값 반환

        case DuelResultType.Damaged:
            return damagedRepelForce; // 피격 기본 넉백값 반환
    }

    return 0f; // 예외 상황 기본값
}

    public int RollBattleSpeedRatePercent(CharacterStatSystem statSystem) // 결투에 사용할 랜덤 속도율 결정
{
    if (statSystem == null)
    {
        return 100; // 참조가 없으면 기본 100% 반환
    }

    int minRate = Mathf.Min(statSystem.MinimumSpeedRatePercent, statSystem.MaximumSpeedRatePercent); // 최소/최대 순서 보정
    int maxRate = Mathf.Max(statSystem.MinimumSpeedRatePercent, statSystem.MaximumSpeedRatePercent); // 최소/최대 순서 보정

    return Random.Range(minRate, maxRate + 1); // 최대값 포함 랜덤 정수 반환
}

public int CalculateBattleSpeed(CharacterStatSystem statSystem, int rolledSpeedRatePercent) // 속도율과 기본 속도로 전투속도 계산
{
    if (statSystem == null)
    {
        return 0; // 참조가 없으면 0 반환
    }

    return (rolledSpeedRatePercent * statSystem.SpeedStat) / 100; // (속도율 × 기본속도) / 100
}

public int GetBonusPowerRateFromBattleSpeed(int selfBattleSpeed, int otherBattleSpeed) // 전투속도 차이에 따른 추가 위력률 계산
{
    if (selfBattleSpeed <= otherBattleSpeed)
    {
        return 0; // 더 빠르지 않으면 보정 없음
    }

    int speedDifference = selfBattleSpeed - otherBattleSpeed; // 전투속도 차이 계산
    int unitCount = speedDifferenceUnit <= 0 ? 0 : speedDifference / speedDifferenceUnit; // 기준 단위 개수 계산

    return unitCount * bonusPowerRatePercentPerUnit; // 최종 추가 위력률 반환
}


public int CalculateFinalPowerRatePercent(CharacterStatSystem selfStat, int selfBattleSpeed, int otherBattleSpeed) // 최종 위력률 계산
{
    if (selfStat == null)
    {
        return 0; // 참조가 없으면 0 반환
    }

    int bonusPowerRate = GetBonusPowerRateFromBattleSpeed(selfBattleSpeed, otherBattleSpeed); // 전투속도 차이 기반 보정값 계산
    return selfStat.PowerRatePercent + bonusPowerRate; // 기본 위력률 + 추가 위력률
}

public int CalculateAttackPowerValue(CharacterStatSystem selfStat, int finalPowerRatePercent) // 최종 위력률을 반영한 공격위력값 계산
{
    if (selfStat == null)
    {
        return 0; // 참조가 없으면 0 반환
    }

    return (finalPowerRatePercent * selfStat.AttackPower) / 100; // (최종 위력률 × 공격력) / 100
}

public DuelCombatDebugData CreateDuelCombatDebugData(
    CharacterStatSystem selfStat, // 자신의 스탯
    int rolledSpeedRatePercent, // 자신에게 뽑힌 속도율
    int selfBattleSpeed, // 자신의 전투속도
    int otherBattleSpeed) // 상대의 전투속도
{
    DuelCombatDebugData debugData = new DuelCombatDebugData(); // 디버그 데이터 생성

    if (selfStat == null)
    {
        return debugData; // 참조가 없으면 기본값 반환
    }

    debugData.rolledSpeedRatePercent = rolledSpeedRatePercent; // 속도율 저장
    debugData.battleSpeed = selfBattleSpeed; // 전투속도 저장
    debugData.battleSpeedDifference = selfBattleSpeed - otherBattleSpeed; // 전투속도 차이 저장
    debugData.bonusPowerRatePercent = GetBonusPowerRateFromBattleSpeed(selfBattleSpeed, otherBattleSpeed); // 추가 위력률 계산
    debugData.basePowerRatePercent = selfStat.PowerRatePercent; // 기본 위력률 저장
    debugData.finalPowerRatePercent = debugData.basePowerRatePercent + debugData.bonusPowerRatePercent; // 최종 위력률 계산
    debugData.attackPower = selfStat.AttackPower; // 공격력 저장
    debugData.attackPowerValue = CalculateAttackPowerValue(selfStat, debugData.finalPowerRatePercent); // 공격위력값 계산

    return debugData; // 계산 결과 반환
}

public string GetDuelResultKoreanText(DuelResultType resultType) // 결투 결과를 한국어 문자열로 변환
{
    switch (resultType)
    {
        case DuelResultType.Hit:
            return "적중"; // Hit -> 적중

        case DuelResultType.Advantage:
            return "우세"; // Advantage -> 우세

        case DuelResultType.Even:
            return "비등"; // Even -> 비등

        case DuelResultType.Disadvantage:
            return "열세"; // Disadvantage -> 열세

        case DuelResultType.Damaged:
            return "피격"; // Damaged -> 피격
    }

    return "알수없음"; // 예외 상황 기본값
}

public int RollBattleSpeedRatePercent(int minimumSpeedRatePercent, int maximumSpeedRatePercent) // 직접 전달된 최소/최대 속도율 기준 랜덤값 결정
{
    int minRate = Mathf.Min(minimumSpeedRatePercent, maximumSpeedRatePercent); // 최소/최대 순서 보정
    int maxRate = Mathf.Max(minimumSpeedRatePercent, maximumSpeedRatePercent); // 최소/최대 순서 보정

    return Random.Range(minRate, maxRate + 1); // 최대값 포함 랜덤 정수 반환
}

}