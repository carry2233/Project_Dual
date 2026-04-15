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

[System.Serializable]
public class SoundPlaybackData
{
    [Header("사운드 클립")]
    [SerializeField] private AudioClip audioClip; // 재생할 사운드 클립

    [Header("사운드 크기")]
    [SerializeField] private float volume = 1f; // 사운드 크기 값

    [Header("사운드 출력 그룹")]
    [SerializeField] private BattleAudioSettings.AudioGroupType audioGroupType = BattleAudioSettings.AudioGroupType.SFX; // 출력 그룹

    public AudioClip AudioClip => audioClip; // 사운드 클립 반환
    public float Volume => volume; // 사운드 크기 반환
    public BattleAudioSettings.AudioGroupType AudioGroupType => audioGroupType; // 출력 그룹 반환
}

[System.Serializable]
public class DuelResultAnimationData
{
    [Header("결투 판정 애니메이션")]
    [SerializeField] private CharacterAnimationClipSO animationClip; // 해당 결과에서 재생할 애니메이션 클립
    [SerializeField] private bool useLoop; // 해당 결과 애니메이션 루프 여부

    public CharacterAnimationClipSO AnimationClip => animationClip; // 애니메이션 클립 반환
    public bool UseLoop => useLoop; // 루프 여부 반환
}

[Header("돌진 시작 사운드")]
[SerializeField] private SoundPlaybackData dashStartSoundData = new SoundPlaybackData(); // 돌진 시작 시 재생할 사운드 설정

[Header("결투 결과별 판정 애니메이션")]
[SerializeField] private DuelResultAnimationData hitResolveAnimationData = new DuelResultAnimationData(); // 적중 결과 애니메이션 설정
[SerializeField] private DuelResultAnimationData advantageResolveAnimationData = new DuelResultAnimationData(); // 우세 결과 애니메이션 설정
[SerializeField] private DuelResultAnimationData evenResolveAnimationData = new DuelResultAnimationData(); // 비등 결과 애니메이션 설정
[SerializeField] private DuelResultAnimationData disadvantageResolveAnimationData = new DuelResultAnimationData(); // 열세 결과 애니메이션 설정
[SerializeField] private DuelResultAnimationData damagedResolveAnimationData = new DuelResultAnimationData(); // 피격 결과 애니메이션 설정

[Header("결투 결과별 사운드")]
[SerializeField] private SoundPlaybackData hitResolveSoundData = new SoundPlaybackData(); // 적중 결과 사운드 설정
[SerializeField] private SoundPlaybackData advantageResolveSoundData = new SoundPlaybackData(); // 우세 결과 사운드 설정
[SerializeField] private SoundPlaybackData evenResolveSoundData = new SoundPlaybackData(); // 비등 결과 사운드 설정
[SerializeField] private SoundPlaybackData disadvantageResolveSoundData = new SoundPlaybackData(); // 열세 결과 사운드 설정
[SerializeField] private SoundPlaybackData damagedResolveSoundData = new SoundPlaybackData(); // 피격 결과 사운드 설정

public CharacterAnimationClipSO DashAnimationClip => dashAnimationClip; // 돌진 애니메이션 반환
public bool UseDashAnimationLoop => useDashAnimationLoop; // 돌진 애니메이션 루프 여부 반환
public SoundPlaybackData DashStartSoundData => dashStartSoundData; // 돌진 시작 사운드 설정 반환

[System.Serializable]
public class DuelResultRepelOverrideData
{
    [Header("덮어씌우기 설정")]
    [SerializeField] private bool useOverrideRepelForce; // 이 결투결과의 넉백값을 직접 덮어쓸지 여부
    [SerializeField] private float overrideRepelForce; // 덮어쓸 넉백값(음수면 반대 방향)

    public bool UseOverrideRepelForce => useOverrideRepelForce; // 덮어쓰기 사용 여부 반환
    public float OverrideRepelForce => overrideRepelForce; // 덮어쓸 넉백값 반환
}


[Header("넉백 덮어쓰기 설정__________________________________________________________")]


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


[System.Serializable]
public class DuelEffectSpawnData
{
    [Header("생성할 이펙트 프리팹 오브젝트")]
    [SerializeField] private GameObject effectPrefab; // 생성할 이펙트 프리팹

    [Header("생성 딜레이")]
    [SerializeField] private float spawnDelay; // 결과 발생 후 생성까지 대기 시간

    [Header("생성 후 유지 시간")]
    [SerializeField] private float effectLifetime = 1f; // 생성 후 유지 시간

    [Header("생성 시 재생 시작 시점 시간")]
    [SerializeField] private float effectStartTimelineTime; // 생성 직후 이 시점까지 재생된 상태로 시작할 시간

    [Header("이펙트 재생 기본속도 배율")]
    [SerializeField] private float effectPlaySpeedMultiplier = 1f; // 이펙트 기본 재생속도 배율

    [Header("방향별 생성 위치 보정 (-X 방향)")]
    [SerializeField] private Vector3 spawnPositionOffsetWhenFacingLeft; // X- 방향일 때 적용할 위치값

    [Header("방향별 생성 회전 보정 (-X 방향)")]
    [SerializeField] private Vector3 spawnRotationOffsetWhenFacingLeft; // X- 방향일 때 적용할 회전값

    [Header("방향별 생성 위치 보정 (+X 방향)")]
    [SerializeField] private Vector3 spawnPositionOffsetWhenFacingRight; // X+ 방향일 때 적용할 위치값

    [Header("방향별 생성 회전 보정 (+X 방향)")]
    [SerializeField] private Vector3 spawnRotationOffsetWhenFacingRight; // X+ 방향일 때 적용할 회전값

    public GameObject EffectPrefab => effectPrefab; // 이펙트 프리팹 반환
    public float SpawnDelay => spawnDelay; // 생성 딜레이 반환
    public float EffectLifetime => effectLifetime; // 유지 시간 반환
    public float EffectStartTimelineTime => effectStartTimelineTime; // 시작 타임라인 시간 반환
    public float EffectPlaySpeedMultiplier => effectPlaySpeedMultiplier; // 재생속도 배율 반환

    public Vector3 SpawnPositionOffsetWhenFacingLeft => spawnPositionOffsetWhenFacingLeft; // X- 방향 위치값 반환
    public Vector3 SpawnRotationOffsetWhenFacingLeft => spawnRotationOffsetWhenFacingLeft; // X- 방향 회전값 반환
    public Vector3 SpawnPositionOffsetWhenFacingRight => spawnPositionOffsetWhenFacingRight; // X+ 방향 위치값 반환
    public Vector3 SpawnRotationOffsetWhenFacingRight => spawnRotationOffsetWhenFacingRight; // X+ 방향 회전값 반환
}

[System.Serializable]
public class DuelResultEffectData
{
    [Header("캐릭터 사이 위치 생성 여부")]
    [SerializeField] private bool useBetweenCharactersEffect; // 두 캐릭터 사이 위치에 생성할지 여부
    [SerializeField] private DuelEffectSpawnData betweenCharactersEffectData = new DuelEffectSpawnData(); // 두 캐릭터 사이 생성 설정

    [Header("본인 부모 오브젝트 자식 생성 여부")]
    [SerializeField] private bool useSelfParentEffect; // 본인 결투 이펙트 부모의 자식으로 생성할지 여부
    [SerializeField] private DuelEffectSpawnData selfParentEffectData = new DuelEffectSpawnData(); // 본인 부모 생성 설정

    [Header("상대 부모 오브젝트 자식 생성 여부")]
    [SerializeField] private bool useTargetParentEffect; // 상대 결투 이펙트 부모의 자식으로 생성할지 여부
    [SerializeField] private DuelEffectSpawnData targetParentEffectData = new DuelEffectSpawnData(); // 상대 부모 생성 설정

    public bool UseBetweenCharactersEffect => useBetweenCharactersEffect; // 두 캐릭터 사이 생성 여부 반환
    public DuelEffectSpawnData BetweenCharactersEffectData => betweenCharactersEffectData; // 두 캐릭터 사이 생성 데이터 반환

    public bool UseSelfParentEffect => useSelfParentEffect; // 본인 부모 생성 여부 반환
    public DuelEffectSpawnData SelfParentEffectData => selfParentEffectData; // 본인 부모 생성 데이터 반환

    public bool UseTargetParentEffect => useTargetParentEffect; // 상대 부모 생성 여부 반환
    public DuelEffectSpawnData TargetParentEffectData => targetParentEffectData; // 상대 부모 생성 데이터 반환
}

[Header("결투 결과별 이펙트")]
[SerializeField] private DuelResultEffectData hitResolveEffectData = new DuelResultEffectData(); // 적중 결과 이펙트 설정
[SerializeField] private DuelResultEffectData advantageResolveEffectData = new DuelResultEffectData(); // 우세 결과 이펙트 설정
[SerializeField] private DuelResultEffectData evenResolveEffectData = new DuelResultEffectData(); // 비등 결과 이펙트 설정
[SerializeField] private DuelResultEffectData disadvantageResolveEffectData = new DuelResultEffectData(); // 열세 결과 이펙트 설정
[SerializeField] private DuelResultEffectData damagedResolveEffectData = new DuelResultEffectData(); // 피격 결과 이펙트 설정



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

public bool TryGetResolveAnimationData(
    GlobalGameRuleManager.DuelResultType resultType, // 조회할 결투 결과
    out CharacterAnimationClipSO animationClip, // 반환할 애니메이션 클립
    out bool useLoop) // 반환할 루프 여부
{
    DuelResultAnimationData targetData = GetResolveAnimationData(resultType); // 결과 타입에 맞는 애니메이션 데이터 가져오기

    if (targetData == null || targetData.AnimationClip == null)
    {
        animationClip = null; // 기본값 초기화
        useLoop = false; // 기본값 초기화
        return false; // 설정 없음
    }

    animationClip = targetData.AnimationClip; // 애니메이션 클립 반환
    useLoop = targetData.UseLoop; // 루프 여부 반환
    return true; // 설정 사용 가능
}

public bool TryGetResolveSoundData(
    GlobalGameRuleManager.DuelResultType resultType, // 조회할 결투 결과
    out AudioClip audioClip, // 반환할 사운드 클립
    out float volume, // 반환할 사운드 크기
    out BattleAudioSettings.AudioGroupType audioGroupType) // 반환할 출력 그룹
{
    SoundPlaybackData targetData = GetResolveSoundData(resultType); // 결과 타입에 맞는 사운드 데이터 가져오기

    if (targetData == null || targetData.AudioClip == null)
    {
        audioClip = null; // 기본값 초기화
        volume = 1f; // 기본값 초기화
        audioGroupType = BattleAudioSettings.AudioGroupType.SFX; // 기본값 초기화
        return false; // 설정 없음
    }

    audioClip = targetData.AudioClip; // 사운드 클립 반환
    volume = targetData.Volume; // 사운드 크기 반환
    audioGroupType = targetData.AudioGroupType; // 출력 그룹 반환
    return true; // 설정 사용 가능
}

public bool TryGetDashStartSoundData(
    out AudioClip audioClip, // 반환할 사운드 클립
    out float volume, // 반환할 사운드 크기
    out BattleAudioSettings.AudioGroupType audioGroupType) // 반환할 출력 그룹
{
    if (dashStartSoundData == null || dashStartSoundData.AudioClip == null)
    {
        audioClip = null; // 기본값 초기화
        volume = 1f; // 기본값 초기화
        audioGroupType = BattleAudioSettings.AudioGroupType.SFX; // 기본값 초기화
        return false; // 설정 없음
    }

    audioClip = dashStartSoundData.AudioClip; // 사운드 클립 반환
    volume = dashStartSoundData.Volume; // 사운드 크기 반환
    audioGroupType = dashStartSoundData.AudioGroupType; // 출력 그룹 반환
    return true; // 설정 사용 가능
}

private DuelResultAnimationData GetResolveAnimationData(GlobalGameRuleManager.DuelResultType resultType) // 결과 타입에 맞는 판정 애니메이션 데이터 반환
{
    switch (resultType)
    {
        case GlobalGameRuleManager.DuelResultType.Hit:
            return hitResolveAnimationData; // 적중 애니메이션 데이터 반환

        case GlobalGameRuleManager.DuelResultType.Advantage:
            return advantageResolveAnimationData; // 우세 애니메이션 데이터 반환

        case GlobalGameRuleManager.DuelResultType.Even:
            return evenResolveAnimationData; // 비등 애니메이션 데이터 반환

        case GlobalGameRuleManager.DuelResultType.Disadvantage:
            return disadvantageResolveAnimationData; // 열세 애니메이션 데이터 반환

        case GlobalGameRuleManager.DuelResultType.Damaged:
            return damagedResolveAnimationData; // 피격 애니메이션 데이터 반환
    }

    return null; // 예외 상황 기본값
}

private SoundPlaybackData GetResolveSoundData(GlobalGameRuleManager.DuelResultType resultType) // 결과 타입에 맞는 판정 사운드 데이터 반환
{
    switch (resultType)
    {
        case GlobalGameRuleManager.DuelResultType.Hit:
            return hitResolveSoundData; // 적중 사운드 데이터 반환

        case GlobalGameRuleManager.DuelResultType.Advantage:
            return advantageResolveSoundData; // 우세 사운드 데이터 반환

        case GlobalGameRuleManager.DuelResultType.Even:
            return evenResolveSoundData; // 비등 사운드 데이터 반환

        case GlobalGameRuleManager.DuelResultType.Disadvantage:
            return disadvantageResolveSoundData; // 열세 사운드 데이터 반환

        case GlobalGameRuleManager.DuelResultType.Damaged:
            return damagedResolveSoundData; // 피격 사운드 데이터 반환
    }

    return null; // 예외 상황 기본값
}

public bool TryGetResolveEffectData(
    GlobalGameRuleManager.DuelResultType resultType, // 조회할 결투 결과
    out DuelResultEffectData effectData) // 반환할 결과별 이펙트 데이터
{
    effectData = GetResolveEffectData(resultType); // 결과 타입에 맞는 이펙트 데이터 가져오기
    return effectData != null; // 데이터가 있으면 true
}

private DuelResultEffectData GetResolveEffectData(GlobalGameRuleManager.DuelResultType resultType) // 결과 타입에 맞는 이펙트 데이터 반환
{
    switch (resultType)
    {
        case GlobalGameRuleManager.DuelResultType.Hit:
            return hitResolveEffectData; // 적중 결과 이펙트 데이터 반환

        case GlobalGameRuleManager.DuelResultType.Advantage:
            return advantageResolveEffectData; // 우세 결과 이펙트 데이터 반환

        case GlobalGameRuleManager.DuelResultType.Even:
            return evenResolveEffectData; // 비등 결과 이펙트 데이터 반환

        case GlobalGameRuleManager.DuelResultType.Disadvantage:
            return disadvantageResolveEffectData; // 열세 결과 이펙트 데이터 반환

        case GlobalGameRuleManager.DuelResultType.Damaged:
            return damagedResolveEffectData; // 피격 결과 이펙트 데이터 반환
    }

    return null; // 예외 상황 기본값
}

}