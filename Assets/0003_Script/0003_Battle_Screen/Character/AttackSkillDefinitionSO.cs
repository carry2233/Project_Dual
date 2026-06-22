using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewAttackSkillDefinition", menuName = "Project Dual/공격 기술 정의")]
public class AttackSkillDefinitionSO : ScriptableObject
{
    [Header("공격기술 이름")]
    [SerializeField] private string attackSkillName; // 공격기술 이름

    [Header("공격기술 정의용 ID")]
    [SerializeField] private int firstRowID; // 공격기술 정의용 A ID
    [SerializeField] private int secondRowID; // 공격기술 정의용 B ID

    [Header("공격기술 선택 UI")]
    [SerializeField] private DuelSkillSlot attackSkillSlotPrefab; // 공격기술 슬롯 프리팹

    [Header("공격기술 벽 확인 레이캐스트")]
    [SerializeField] private float attackSkillDirectionWallCheckDistance = 1f; // 공격기술 시전 방향 벽 확인 거리

    [Header("공격기술 돌진 사운드")]
    [SerializeField] private AudioClip dashSoundClip; // 돌진 시 재생할 효과음
    [SerializeField] private float dashSoundVolume = 1f; // 돌진 효과음 크기
    [SerializeField] private BattleAudioSettings.AudioGroupType dashSoundGroupType = BattleAudioSettings.AudioGroupType.SFX; // 돌진 효과음 출력 그룹

    [Header("공격기술 기본 판정")]
    [SerializeField] private float attackSkillCastDistance = 1.5f; // 공격기술 시전 판정거리
    [SerializeField] private Vector3 targetLocalCorrectionPosition; // 공격자 기준 피격자 보정 로컬 위치

    [Header("공격기술 사용 조건")]
    [SerializeField] private bool canUseOnlyOnBrokenTarget = true; // 와해상태 대상에게만 사용 여부

    [Header("공격 목록")]
    [SerializeField] private List<AttackExecutionData> attackExecutionList = new List<AttackExecutionData>(); // 실제 공격 실행 목록

    [Header("공격기술 시전 애니메이션 목록")]
    [SerializeField] private List<AttackSkillAnimationData> attackSkillAnimationList = new List<AttackSkillAnimationData>(); // 공격기술 재생 애니메이션 목록

    [Header("공격기술 넉백 / 방향전환 목록")]
    [SerializeField] private List<AttackSkillMotionEventData> attackSkillMotionEventList = new List<AttackSkillMotionEventData>(); // 넉백 및 방향전환 목록

    [Header("공격기술 사운드 목록")]
    [SerializeField] private List<AttackSkillSoundEventData> attackSkillSoundEventList = new List<AttackSkillSoundEventData>(); // 공격기술 중 재생할 사운드 목록

    [Header("________________________________________________________________________________")]

[Header("공격기술 종료 시 자신에게 부여할 상태효과 목록")]
[Tooltip("공격기술이 정상적으로 끝났을 때 시전자 자신에게 부여할 상태효과 목록입니다.")]
[SerializeField] private List<AttackSkillEndStatusEffectApplyData> selfStatusEffectApplyListOnAttackSkillEnd
    = new List<AttackSkillEndStatusEffectApplyData>(); // 공격기술 종료 시 자신에게 부여할 상태효과 목록

[Header("공격기술 종료 시 피격자에게 부여할 상태효과 목록")]
[Tooltip("공격기술이 정상적으로 끝났을 때 피격자에게 부여할 상태효과 목록입니다.")]
[SerializeField] private List<AttackSkillEndStatusEffectApplyData> targetStatusEffectApplyListOnAttackSkillEnd
    = new List<AttackSkillEndStatusEffectApplyData>(); // 공격기술 종료 시 피격자에게 부여할 상태효과 목록

[Header("특정 공격 치명타 적용 목록")]
[Tooltip("공격 목록 인덱스 기준으로 특정 공격에 치명타 피해/치명타 경험치 획득 여부를 예약합니다. 인덱스는 0부터 시작합니다.")]
[SerializeField] private List<AttackSkillCriticalApplyData> attackSkillCriticalApplyList
    = new List<AttackSkillCriticalApplyData>(); // 특정 공격 인덱스에 적용할 치명타 예약 목록

    public string AttackSkillName => attackSkillName; // 공격기술 이름 반환
    public int FirstRowID => firstRowID; // A ID 반환
    public int SecondRowID => secondRowID; // B ID 반환
    public DuelSkillSlot AttackSkillSlotPrefab => attackSkillSlotPrefab; // 공격기술 슬롯 프리팹 반환
    public float AttackSkillDirectionWallCheckDistance => attackSkillDirectionWallCheckDistance; // 공격기술 시전 방향 벽 확인 거리 반환
    public AudioClip DashSoundClip => dashSoundClip; // 돌진 효과음 반환
    public float DashSoundVolume => dashSoundVolume; // 돌진 효과음 크기 반환
    public BattleAudioSettings.AudioGroupType DashSoundGroupType => dashSoundGroupType; // 돌진 효과음 그룹 반환
    public float AttackSkillCastDistance => attackSkillCastDistance; // 공격기술 시전 판정거리 반환
    public Vector3 TargetLocalCorrectionPosition => targetLocalCorrectionPosition; // 피격자 보정 로컬 위치 반환
    public bool CanUseOnlyOnBrokenTarget => canUseOnlyOnBrokenTarget; // 와해상태 전용 여부 반환
    public IReadOnlyList<AttackExecutionData> AttackExecutionList => attackExecutionList; // 공격 목록 반환
    public IReadOnlyList<AttackSkillAnimationData> AttackSkillAnimationList => attackSkillAnimationList; // 애니메이션 목록 반환
    public IReadOnlyList<AttackSkillMotionEventData> AttackSkillMotionEventList => attackSkillMotionEventList; // 모션 이벤트 목록 반환
    public IReadOnlyList<AttackSkillSoundEventData> AttackSkillSoundEventList => attackSkillSoundEventList; // 사운드 이벤트 목록 반환

    public IReadOnlyList<AttackSkillEndStatusEffectApplyData> SelfStatusEffectApplyListOnAttackSkillEnd
    => selfStatusEffectApplyListOnAttackSkillEnd; // 공격기술 종료 시 자신에게 부여할 상태효과 목록 반환

    public IReadOnlyList<AttackSkillEndStatusEffectApplyData> TargetStatusEffectApplyListOnAttackSkillEnd
    => targetStatusEffectApplyListOnAttackSkillEnd; // 공격기술 종료 시 피격자에게 부여할 상태효과 목록 반환

    public IReadOnlyList<AttackSkillCriticalApplyData> AttackSkillCriticalApplyList
    => attackSkillCriticalApplyList; // 특정 공격 치명타 적용 목록 반환

    [System.Serializable]
    public class AttackEffectSpawnData
    {
        [Header("생성할 이펙트 프리팹")]
        [SerializeField] private CharacterEffectInstance effectPrefab; // 생성할 이펙트 프리팹

        [Header("이펙트 생성 설정")]
        [SerializeField] private float spawnDelay; // 공격 적중 후 이펙트 생성 딜레이
        [SerializeField] private float effectLifetime = 1f; // 이펙트 유지 시간
        [SerializeField] private float effectStartTimelineTime; // 이펙트 시작 타임라인 시간
        [SerializeField] private float effectPlaySpeedMultiplier = 1f; // 이펙트 재생속도 배율

        [Header("방향별 생성 위치 보정")]
        [SerializeField] private Vector3 spawnPositionOffsetWhenFacingLeft; // X- 방향일 때 위치 보정
        [SerializeField] private Vector3 spawnPositionOffsetWhenFacingRight; // X+ 방향일 때 위치 보정

        [Header("방향별 생성 회전 보정")]
        [SerializeField] private Vector3 spawnRotationOffsetWhenFacingLeft; // X- 방향일 때 회전 보정
        [SerializeField] private Vector3 spawnRotationOffsetWhenFacingRight; // X+ 방향일 때 회전 보정

        [Header("방향별 생성 스케일 보정")]
        [SerializeField] private Vector3 spawnScaleWhenFacingLeft = Vector3.one; // X- 방향일 때 스케일 보정
        [SerializeField] private Vector3 spawnScaleWhenFacingRight = Vector3.one; // X+ 방향일 때 스케일 보정

        public CharacterEffectInstance EffectPrefab => effectPrefab; // 이펙트 프리팹 반환
        public float SpawnDelay => spawnDelay; // 생성 딜레이 반환
        public float EffectLifetime => effectLifetime; // 유지 시간 반환
        public float EffectStartTimelineTime => effectStartTimelineTime; // 시작 타임라인 시간 반환
        public float EffectPlaySpeedMultiplier => effectPlaySpeedMultiplier; // 재생속도 배율 반환
        public Vector3 SpawnPositionOffsetWhenFacingLeft => spawnPositionOffsetWhenFacingLeft; // X- 위치 보정 반환
        public Vector3 SpawnPositionOffsetWhenFacingRight => spawnPositionOffsetWhenFacingRight; // X+ 위치 보정 반환
        public Vector3 SpawnRotationOffsetWhenFacingLeft => spawnRotationOffsetWhenFacingLeft; // X- 회전 보정 반환
        public Vector3 SpawnRotationOffsetWhenFacingRight => spawnRotationOffsetWhenFacingRight; // X+ 회전 보정 반환

        public Vector3 SpawnScaleWhenFacingLeft => spawnScaleWhenFacingLeft; // X- 스케일 보정 반환
        public Vector3 SpawnScaleWhenFacingRight => spawnScaleWhenFacingRight; // X+ 스케일 보정 반환
    }

    [System.Serializable]
    public class AttackExecutionData
    {
        [Header("공격 실행 애니메이션")]
        [SerializeField] private int animationListIndex; // 공격 실행 기준 애니메이션 리스트 인덱스
        [SerializeField] private float attackStartDelay; // 공격 실행 시작 딜레이

        [Header("위력률 계산")]
        [SerializeField] private bool usePowerRateCorrection; // 위력보정값 적용 여부
        [SerializeField] private int powerRateCorrectionValue; // 최종위력률에 더할 값

        [SerializeField] private bool useFixedPowerRate; // 위력고정값 적용 여부
        [SerializeField] private int fixedPowerRateValue = 100; // 최종위력률을 고정할 값

        [Header("공격 적중 이펙트")]
        [SerializeField] private bool useHitEffect; // 공격 적중 이펙트 생성 여부
        [SerializeField] private AttackEffectSpawnData hitEffectData = new AttackEffectSpawnData(); // 공격 적중 이펙트 설정

        [Header("공격 결과 설정")]
        [SerializeField] private bool isFinishingAttack; // 결정타 여부

        public int AnimationListIndex => animationListIndex; // 애니메이션 인덱스 반환
        public float AttackStartDelay => attackStartDelay; // 공격 시작 딜레이 반환
        public bool UsePowerRateCorrection => usePowerRateCorrection; // 위력보정 적용 여부 반환
        public int PowerRateCorrectionValue => powerRateCorrectionValue; // 위력보정값 반환
        public bool UseFixedPowerRate => useFixedPowerRate; // 위력고정 적용 여부 반환
        public int FixedPowerRateValue => fixedPowerRateValue; // 위력고정값 반환
        public bool UseHitEffect => useHitEffect; // 이펙트 사용 여부 반환
        public AttackEffectSpawnData HitEffectData => hitEffectData; // 이펙트 설정 반환
        public bool IsFinishingAttack => isFinishingAttack; // 결정타 여부 반환

        public int CalculateFinalPowerRate(int casterBasePowerRate) // 최종위력률 계산
        {
            int finalPowerRate = casterBasePowerRate; // 시전자 기본 위력률로 시작

            if (usePowerRateCorrection)
            {
                finalPowerRate += powerRateCorrectionValue; // 보정값 더하기, 음수면 감소
            }

            if (useFixedPowerRate)
            {
                finalPowerRate = fixedPowerRateValue; // 고정값 사용 시 최종위력률 덮어쓰기
            }

            return finalPowerRate; // 최종위력률 반환
        }

        public int CalculateAttackDamage(int casterAttackPower, int casterBasePowerRate) // 공격기술 피해량 계산
        {
            int finalPowerRate = CalculateFinalPowerRate(casterBasePowerRate); // 최종위력률 계산
            return Mathf.Max(0, (finalPowerRate * casterAttackPower) / 100); // 최종 피해량 반환
        }
    }

    [System.Serializable]
    public class AttackSkillAnimationData
    {
        [Header("재생할 애니메이션")]
        [SerializeField] private CharacterAnimationClipSO animationClip; // 재생할 캐릭터 애니메이션
        [SerializeField] private float delayAfterEnd; // 끝까지 재생 후 다음 애니메이션 전 딜레이

        public CharacterAnimationClipSO AnimationClip => animationClip; // 애니메이션 반환
        public float DelayAfterEnd => delayAfterEnd; // 종료 후 딜레이 반환
    }

    [System.Serializable]
    public class AttackSkillMotionEventData
    {
        [Header("넉백 적용 기준 애니메이션")]
        [SerializeField] private int knockbackAnimationListIndex; // 넉백이 적용될 애니메이션 리스트 인덱스

        [Header("넉백값")]
        [SerializeField] private float selfKnockbackValue; // 본인에게 적용할 넉백값
        [SerializeField] private float targetKnockbackValue; // 피격자에게 적용할 넉백값

        [Header("넉백 적용 여부")]
        [SerializeField] private bool useSelfKnockback; // 본인 넉백 적용 여부
        [SerializeField] private bool useTargetKnockback; // 피격자 넉백 적용 여부

        [Header("넉백 시작 딜레이")]
        [SerializeField] private float selfKnockbackStartDelay; // 본인 넉백 시작 딜레이
        [SerializeField] private float targetKnockbackStartDelay; // 피격자 넉백 시작 딜레이

        [Header("방향전환 기준 애니메이션")]
        [SerializeField] private int directionFlipAnimationListIndex; // 방향전환이 적용될 애니메이션 리스트 인덱스

        [Header("방향전환 적용 여부")]
        [SerializeField] private bool useSelfDirectionFlip; // 본인 방향전환 여부
        [SerializeField] private bool useTargetDirectionFlip; // 피격자 방향전환 여부

        [Header("방향전환 시작 딜레이")]
        [SerializeField] private float selfDirectionFlipStartDelay; // 본인 방향전환 시작 딜레이
        [SerializeField] private float targetDirectionFlipStartDelay; // 피격자 방향전환 시작 딜레이

        public int KnockbackAnimationListIndex => knockbackAnimationListIndex; // 넉백 기준 애니메이션 인덱스 반환
        public float SelfKnockbackValue => selfKnockbackValue; // 본인 넉백값 반환
        public float TargetKnockbackValue => targetKnockbackValue; // 피격자 넉백값 반환
        public bool UseSelfKnockback => useSelfKnockback; // 본인 넉백 여부 반환
        public bool UseTargetKnockback => useTargetKnockback; // 피격자 넉백 여부 반환
        public float SelfKnockbackStartDelay => selfKnockbackStartDelay; // 본인 넉백 딜레이 반환
        public float TargetKnockbackStartDelay => targetKnockbackStartDelay; // 피격자 넉백 딜레이 반환
        public int DirectionFlipAnimationListIndex => directionFlipAnimationListIndex; // 방향전환 기준 인덱스 반환
        public bool UseSelfDirectionFlip => useSelfDirectionFlip; // 본인 방향전환 여부 반환
        public bool UseTargetDirectionFlip => useTargetDirectionFlip; // 피격자 방향전환 여부 반환
        public float SelfDirectionFlipStartDelay => selfDirectionFlipStartDelay; // 본인 방향전환 딜레이 반환
        public float TargetDirectionFlipStartDelay => targetDirectionFlipStartDelay; // 피격자 방향전환 딜레이 반환
    }

    [System.Serializable]
    public class AttackSkillSoundEventData
    {
        [Header("재생할 사운드")]
        [SerializeField] private AudioClip audioClip; // 재생할 사운드
        [SerializeField] private float volume = 1f; // 사운드 크기
        [SerializeField] private BattleAudioSettings.AudioGroupType audioGroupType = BattleAudioSettings.AudioGroupType.SFX; // 출력 그룹

        [Header("재생 기준")]
        [SerializeField] private int animationListIndex; // 재생 기준 애니메이션 리스트 인덱스
        [SerializeField] private float soundStartDelay; // 소리 재생 시작 딜레이

        public AudioClip AudioClip => audioClip; // 사운드 반환
        public float Volume => volume; // 사운드 크기 반환
        public BattleAudioSettings.AudioGroupType AudioGroupType => audioGroupType; // 출력 그룹 반환
        public int AnimationListIndex => animationListIndex; // 애니메이션 인덱스 반환
        public float SoundStartDelay => soundStartDelay; // 시작 딜레이 반환
    }

    [System.Serializable]
public class AttackSkillEndStatusEffectApplyData
{
    [Header("부여할 상태효과")]
    [SerializeField] private StatusEffectDefinitionSO statusEffectDefinition; // 부여할 상태효과 ScriptableObject

    [Header("부여 중첩값")]
    [Tooltip("상태효과를 몇 중첩 부여할지 설정합니다. 최소 1 이상으로 보정됩니다.")]
    [SerializeField] private int applyStack = 1; // 부여할 상태효과 중첩값

    [Header("부여 지속값")]
    [Tooltip("상태효과가 지속시간 개념을 사용할 때만 적용됩니다. 지속시간이 없는 상태효과라면 이 값은 무시됩니다.")]
    [SerializeField] private float applyDuration = 1f; // 부여할 지속시간, 비지속 상태효과면 무시

    public StatusEffectDefinitionSO StatusEffectDefinition => statusEffectDefinition; // 부여할 상태효과 반환
    public int ApplyStack => Mathf.Max(1, applyStack); // 부여 중첩값 반환
    public float ApplyDuration => Mathf.Max(0f, applyDuration); // 부여 지속값 반환
}

[System.Serializable]
public class AttackSkillCriticalApplyData
{
    [Header("해당될 공격 리스트 인덱스")]
    [Tooltip("AttackExecutionList 기준 인덱스입니다. 0부터 시작합니다.")]
    [SerializeField] private int attackExecutionListIndex; // 치명타 설정이 적용될 공격 목록 인덱스

    [Header("치명타 피해 적용 여부")]
    [SerializeField] private bool applyCriticalDamage; // 해당 공격에 치명타 피해를 적용할지 여부

    [Header("치명타 경험치 획득 여부")]
    [SerializeField] private bool gainCriticalExperience; // 해당 공격 적중 시 치명타 경험치를 획득할지 여부

    public int AttackExecutionListIndex => attackExecutionListIndex; // 치명타 적용 공격 인덱스 반환
    public bool ApplyCriticalDamage => applyCriticalDamage; // 치명타 피해 적용 여부 반환
    public bool GainCriticalExperience => gainCriticalExperience; // 치명타 경험치 획득 여부 반환
}






}