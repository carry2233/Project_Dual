using UnityEngine;

[CreateAssetMenu(fileName = "NewDuelSkillDefinition", menuName = "Project Dual/결투 기술 정의")]
public class DuelSkillDefinitionSO : ScriptableObject
{
[Header("기술 이름")]
[SerializeField] private string skillName; // 기술 이름

[Header("기술 정의용 ID")]
[SerializeField] private int firstRowID; // 기술 정의용 1열 ID
[SerializeField] private int secondRowID; // 기술 정의용 2열 ID

[Header("기술 속도율 범위")]
[SerializeField] private int minimumSpeedRatePercent = 90; // 이 기술의 최소 속도율
[SerializeField] private int maximumSpeedRatePercent = 110; // 이 기술의 최대 속도율

[Header("기술 연출 애니메이션")]
[SerializeField] private CharacterAnimationClipSO dashAnimationClip; // 돌진 시 재생할 애니메이션 클립
[SerializeField] private bool useDashAnimationLoop = false; // 돌진 애니메이션 루프 재생 여부
[SerializeField] private CharacterAnimationClipSO duelResolveAnimationClip; // 결투 판정 시 재생할 애니메이션 클립
[SerializeField] private bool useDuelResolveAnimationLoop = false; // 결투 판정 애니메이션 루프 재생 여부

[System.Serializable]
public class DuelResultRepelOverrideData
{
    [Header("덮어씌우기 설정")]
    [SerializeField] private bool useOverrideRepelForce; // 이 결투결과의 넉백값을 직접 덮어쓸지 여부
    [SerializeField] private float overrideRepelForce; // 덮어쓸 넉백값(음수면 반대 방향)

    public bool UseOverrideRepelForce => useOverrideRepelForce; // 덮어쓰기 사용 여부 반환
    public float OverrideRepelForce => overrideRepelForce; // 덮어쓸 넉백값 반환
}

[Header("자신에게 적용되는 넉백 덮어쓰기")]
[SerializeField] private DuelResultRepelOverrideData selfHitRepelOverride = new DuelResultRepelOverrideData(); // 적중 시 자신용 넉백 설정
[SerializeField] private DuelResultRepelOverrideData selfAdvantageRepelOverride = new DuelResultRepelOverrideData(); // 우세 시 자신용 넉백 설정
[SerializeField] private DuelResultRepelOverrideData selfEvenRepelOverride = new DuelResultRepelOverrideData(); // 비등 시 자신용 넉백 설정
[SerializeField] private DuelResultRepelOverrideData selfDisadvantageRepelOverride = new DuelResultRepelOverrideData(); // 열세 시 자신용 넉백 설정
[SerializeField] private DuelResultRepelOverrideData selfDamagedRepelOverride = new DuelResultRepelOverrideData(); // 피격 시 자신용 넉백 설정

[Header("상대에게 적용되는 넉백 덮어쓰기")]
[SerializeField] private DuelResultRepelOverrideData targetHitRepelOverride = new DuelResultRepelOverrideData(); // 적중 시 상대용 넉백 설정
[SerializeField] private DuelResultRepelOverrideData targetAdvantageRepelOverride = new DuelResultRepelOverrideData(); // 우세 시 상대용 넉백 설정
[SerializeField] private DuelResultRepelOverrideData targetEvenRepelOverride = new DuelResultRepelOverrideData(); // 비등 시 상대용 넉백 설정
[SerializeField] private DuelResultRepelOverrideData targetDisadvantageRepelOverride = new DuelResultRepelOverrideData(); // 열세 시 상대용 넉백 설정
[SerializeField] private DuelResultRepelOverrideData targetDamagedRepelOverride = new DuelResultRepelOverrideData(); // 피격 시 상대용 넉백 설정

public string SkillName => skillName; // 기술 이름 반환
public int FirstRowID => firstRowID; // 1열 ID 반환
public int SecondRowID => secondRowID; // 2열 ID 반환
public int MinimumSpeedRatePercent => minimumSpeedRatePercent; // 최소 속도율 반환
public int MaximumSpeedRatePercent => maximumSpeedRatePercent; // 최대 속도율 반환
public CharacterAnimationClipSO DashAnimationClip => dashAnimationClip; // 돌진 애니메이션 반환
public bool UseDashAnimationLoop => useDashAnimationLoop; // 돌진 애니메이션 루프 여부 반환
public CharacterAnimationClipSO DuelResolveAnimationClip => duelResolveAnimationClip; // 판정 애니메이션 반환
public bool UseDuelResolveAnimationLoop => useDuelResolveAnimationLoop; // 판정 애니메이션 루프 여부 반환

public bool TryGetSelfRepelForceOverride(GlobalGameRuleManager.DuelResultType resultType, out float overrideForce) // 자신에게 적용할 넉백 덮어쓰기 반환 시도
{
    DuelResultRepelOverrideData targetData = GetSelfRepelOverrideData(resultType); // 해당 결과의 자신용 설정 가져오기

    if (targetData != null && targetData.UseOverrideRepelForce)
    {
        overrideForce = targetData.OverrideRepelForce; // 덮어쓸 넉백값 반환
        return true; // 자신용 덮어쓰기 사용
    }

    overrideForce = 0f; // 기본값 반환
    return false; // 자신용 덮어쓰기 미사용
}

public bool TryGetTargetRepelForceOverride(GlobalGameRuleManager.DuelResultType resultType, out float overrideForce) // 상대에게 적용할 넉백 덮어쓰기 반환 시도
{
    DuelResultRepelOverrideData targetData = GetTargetRepelOverrideData(resultType); // 해당 결과의 상대용 설정 가져오기

    if (targetData != null && targetData.UseOverrideRepelForce)
    {
        overrideForce = targetData.OverrideRepelForce; // 덮어쓸 넉백값 반환
        return true; // 상대용 덮어쓰기 사용
    }

    overrideForce = 0f; // 기본값 반환
    return false; // 상대용 덮어쓰기 미사용
}

private DuelResultRepelOverrideData GetSelfRepelOverrideData(GlobalGameRuleManager.DuelResultType resultType) // 결과 타입에 맞는 자신용 설정 반환
{
    switch (resultType)
    {
        case GlobalGameRuleManager.DuelResultType.Hit:
            return selfHitRepelOverride; // 적중 시 자신용 설정 반환

        case GlobalGameRuleManager.DuelResultType.Advantage:
            return selfAdvantageRepelOverride; // 우세 시 자신용 설정 반환

        case GlobalGameRuleManager.DuelResultType.Even:
            return selfEvenRepelOverride; // 비등 시 자신용 설정 반환

        case GlobalGameRuleManager.DuelResultType.Disadvantage:
            return selfDisadvantageRepelOverride; // 열세 시 자신용 설정 반환

        case GlobalGameRuleManager.DuelResultType.Damaged:
            return selfDamagedRepelOverride; // 피격 시 자신용 설정 반환
    }

    return null; // 예외 상황 기본값
}

private DuelResultRepelOverrideData GetTargetRepelOverrideData(GlobalGameRuleManager.DuelResultType resultType) // 결과 타입에 맞는 상대용 설정 반환
{
    switch (resultType)
    {
        case GlobalGameRuleManager.DuelResultType.Hit:
            return targetHitRepelOverride; // 적중 시 상대용 설정 반환

        case GlobalGameRuleManager.DuelResultType.Advantage:
            return targetAdvantageRepelOverride; // 우세 시 상대용 설정 반환

        case GlobalGameRuleManager.DuelResultType.Even:
            return targetEvenRepelOverride; // 비등 시 상대용 설정 반환

        case GlobalGameRuleManager.DuelResultType.Disadvantage:
            return targetDisadvantageRepelOverride; // 열세 시 상대용 설정 반환

        case GlobalGameRuleManager.DuelResultType.Damaged:
            return targetDamagedRepelOverride; // 피격 시 상대용 설정 반환
    }

    return null; // 예외 상황 기본값
}
}