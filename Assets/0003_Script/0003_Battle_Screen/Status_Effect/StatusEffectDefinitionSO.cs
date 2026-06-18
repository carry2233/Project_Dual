using UnityEngine;

/// <summary>
/// 상태효과 정의
/// - 캐릭터에게 적용될 하나의 상태효과 정보를 정의한다.
/// - 이 상태효과가 어떤 기본 스탯 상태효과 중첩을 부여하는지 설정한다.
/// </summary>
[CreateAssetMenu(fileName = "StatusEffectDefinition", menuName = "Project_Dual/상태효과 정의")]
public class StatusEffectDefinitionSO : ScriptableObject
{
[Header("상태효과 기본 정보")]
[SerializeField] private int statusEffectID; // 상태효과 고유 ID

[SerializeField] private string statusEffectName; // 상태효과 표시 이름

[SerializeField] private Sprite statusEffectIcon; // 상태효과 슬롯에 표시할 이미지

[SerializeField] private string statusEffectShortDescription; // 상태효과 짧은 설명

[TextArea(3, 8)]
[SerializeField] private string statusEffectDescription; // 상태효과 상세 설명

[SerializeField] private int statusEffectSlotSortPriority; // 상태효과 슬롯 나열 우선순위, 낮을수록 먼저 표시

[Tooltip("체크하면 지속시간이 1초마다 1씩 줄어들고, 0이 되면 제거됩니다. 체크하지 않으면 무한 지속됩니다.")]
[SerializeField] private bool hasDuration; // 지속시간 사용 여부

[Tooltip("이 상태효과 자체가 캐릭터에게 적용될 수 있는 최대 중첩 수치입니다.")]
[SerializeField] private int maxStackLimit = 1; // 최대 중첩 수치

    [Header("위력률 상태효과 중첩 부여값")]
    [SerializeField] private int powerRateIncreaseStackValue;
    [SerializeField] private int powerRateDecreaseStackValue;

    [Header("속도 상태효과 중첩 부여값")]
    [SerializeField] private int speedIncreaseStackValue;
    [SerializeField] private int speedDecreaseStackValue;

    [Header("체급 상태효과 중첩 부여값")]
    [SerializeField] private int bodySizeIncreaseStackValue;
    [SerializeField] private int bodySizeDecreaseStackValue;

    [Header("공격력 상태효과 중첩 부여값")]
    [SerializeField] private int attackPowerIncreaseStackValue;
    [SerializeField] private int attackPowerDecreaseStackValue;

    [Header("방어 상태효과 중첩 부여값")]
    [SerializeField] private int defenseIncreaseStackValue;
    [SerializeField] private int defenseDecreaseStackValue;

    [Header("와해 저항률 상태효과 중첩 부여값")]
    [SerializeField] private int staggerResistanceIncreaseStackValue;
    [SerializeField] private int staggerResistanceDecreaseStackValue;

    [Header("이동속도률 상태효과 중첩 부여값")]
    [SerializeField] private int moveSpeedRateIncreaseStackValue;
    [SerializeField] private int moveSpeedRateDecreaseStackValue;

public int StatusEffectID => statusEffectID; // 상태효과 ID 반환
public string StatusEffectName => statusEffectName; // 상태효과 이름 반환
public Sprite StatusEffectIcon => statusEffectIcon; // 상태효과 이미지 반환
public string StatusEffectShortDescription => statusEffectShortDescription; // 짧은 설명 반환
public string StatusEffectDescription => statusEffectDescription; // 상세 설명 반환
public int StatusEffectSlotSortPriority => statusEffectSlotSortPriority; // 슬롯 정렬 우선순위 반환
public bool HasDuration => hasDuration; // 지속시간 사용 여부 반환
public int MaxStackLimit => Mathf.Max(1, maxStackLimit); // 최대 중첩 수치 반환
    public int PowerRateIncreaseStackValue => Mathf.Max(0, powerRateIncreaseStackValue);
    public int PowerRateDecreaseStackValue => Mathf.Max(0, powerRateDecreaseStackValue);

    public int SpeedIncreaseStackValue => Mathf.Max(0, speedIncreaseStackValue);
    public int SpeedDecreaseStackValue => Mathf.Max(0, speedDecreaseStackValue);

    public int BodySizeIncreaseStackValue => Mathf.Max(0, bodySizeIncreaseStackValue);
    public int BodySizeDecreaseStackValue => Mathf.Max(0, bodySizeDecreaseStackValue);

    public int AttackPowerIncreaseStackValue => Mathf.Max(0, attackPowerIncreaseStackValue);
    public int AttackPowerDecreaseStackValue => Mathf.Max(0, attackPowerDecreaseStackValue);

    public int DefenseIncreaseStackValue => Mathf.Max(0, defenseIncreaseStackValue);
    public int DefenseDecreaseStackValue => Mathf.Max(0, defenseDecreaseStackValue);

    public int StaggerResistanceIncreaseStackValue => Mathf.Max(0, staggerResistanceIncreaseStackValue);
    public int StaggerResistanceDecreaseStackValue => Mathf.Max(0, staggerResistanceDecreaseStackValue);

    public int MoveSpeedRateIncreaseStackValue => Mathf.Max(0, moveSpeedRateIncreaseStackValue);
    public int MoveSpeedRateDecreaseStackValue => Mathf.Max(0, moveSpeedRateDecreaseStackValue);

    public bool HasAnyBaseStatStatusEffect // 이 상태효과가 스탯 상태효과를 하나라도 부여하는지 여부
{
    get
    {
        return
            PowerRateIncreaseStackValue > 0 ||
            PowerRateDecreaseStackValue > 0 ||
            SpeedIncreaseStackValue > 0 ||
            SpeedDecreaseStackValue > 0 ||
            BodySizeIncreaseStackValue > 0 ||
            BodySizeDecreaseStackValue > 0 ||
            AttackPowerIncreaseStackValue > 0 ||
            AttackPowerDecreaseStackValue > 0 ||
            DefenseIncreaseStackValue > 0 ||
            DefenseDecreaseStackValue > 0 ||
            StaggerResistanceIncreaseStackValue > 0 ||
            StaggerResistanceDecreaseStackValue > 0 ||
            MoveSpeedRateIncreaseStackValue > 0 ||
            MoveSpeedRateDecreaseStackValue > 0;
    }
}





}