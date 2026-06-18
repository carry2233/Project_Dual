using UnityEngine;

/// <summary>
/// 기본 스탯 상태효과 정의 관리자
/// - 위력률 증가/감소, 공격력 증가/감소 같은 "스탯 직접 보정 상태효과"의 ID, 중첩 1당 적용값, 대표 이미지를 관리한다.
/// - DontDestroyOnLoad 싱글톤으로 유지된다.
/// </summary>
public class BaseStatStatusEffectDefinitionManager : MonoBehaviour
{
public static BaseStatStatusEffectDefinitionManager Instance { get; private set; }

[Header("위력률 증가 상태효과")]
[SerializeField] private int powerRateIncreaseStatusEffectID = 1001; // 위력률 증가 상태효과 ID
[SerializeField] private int powerRateIncreaseApplyValuePerStack = 1; // 중첩 1당 위력률 증가값
[SerializeField] private Sprite powerRateIncreaseIcon; // 위력률 증가 아이콘
[TextArea(2, 5)]
[SerializeField] private string powerRateIncreaseDescription; // 위력률 증가 설명
[SerializeField] private int powerRateIncreaseSlotSortPriority = 100; // 위력률 증가 슬롯 정렬 우선순위

[Header("위력률 감소 상태효과")]
[SerializeField] private int powerRateDecreaseStatusEffectID = 1002; // 위력률 감소 상태효과 ID
[SerializeField] private int powerRateDecreaseApplyValuePerStack = 1; // 중첩 1당 위력률 감소값
[SerializeField] private Sprite powerRateDecreaseIcon; // 위력률 감소 아이콘
[TextArea(2, 5)]
[SerializeField] private string powerRateDecreaseDescription; // 위력률 감소 설명
[SerializeField] private int powerRateDecreaseSlotSortPriority = 101; // 위력률 감소 슬롯 정렬 우선순위

[Header("속도 증가 상태효과")]
[SerializeField] private int speedIncreaseStatusEffectID = 1003; // 속도 증가 상태효과 ID
[SerializeField] private int speedIncreaseApplyValuePerStack = 1; // 중첩 1당 속도 증가값
[SerializeField] private Sprite speedIncreaseIcon; // 속도 증가 아이콘
[TextArea(2, 5)]
[SerializeField] private string speedIncreaseDescription; // 속도 증가 설명
[SerializeField] private int speedIncreaseSlotSortPriority = 102; // 속도 증가 슬롯 정렬 우선순위

[Header("속도 감소 상태효과")]
[SerializeField] private int speedDecreaseStatusEffectID = 1004; // 속도 감소 상태효과 ID
[SerializeField] private int speedDecreaseApplyValuePerStack = 1; // 중첩 1당 속도 감소값
[SerializeField] private Sprite speedDecreaseIcon; // 속도 감소 아이콘
[TextArea(2, 5)]
[SerializeField] private string speedDecreaseDescription; // 속도 감소 설명
[SerializeField] private int speedDecreaseSlotSortPriority = 103; // 속도 감소 슬롯 정렬 우선순위

[Header("체급 증가 상태효과")]
[SerializeField] private int bodySizeIncreaseStatusEffectID = 1005; // 체급 증가 상태효과 ID
[SerializeField] private int bodySizeIncreaseApplyValuePerStack = 1; // 중첩 1당 체급 증가값
[SerializeField] private Sprite bodySizeIncreaseIcon; // 체급 증가 아이콘
[TextArea(2, 5)]
[SerializeField] private string bodySizeIncreaseDescription; // 체급 증가 설명
[SerializeField] private int bodySizeIncreaseSlotSortPriority = 104; // 체급 증가 슬롯 정렬 우선순위

[Header("체급 감소 상태효과")]
[SerializeField] private int bodySizeDecreaseStatusEffectID = 1006; // 체급 감소 상태효과 ID
[SerializeField] private int bodySizeDecreaseApplyValuePerStack = 1; // 중첩 1당 체급 감소값
[SerializeField] private Sprite bodySizeDecreaseIcon; // 체급 감소 아이콘
[TextArea(2, 5)]
[SerializeField] private string bodySizeDecreaseDescription; // 체급 감소 설명
[SerializeField] private int bodySizeDecreaseSlotSortPriority = 105; // 체급 감소 슬롯 정렬 우선순위

[Header("공격력 증가 상태효과")]
[SerializeField] private int attackPowerIncreaseStatusEffectID = 1007; // 공격력 증가 상태효과 ID
[SerializeField] private int attackPowerIncreaseApplyValuePerStack = 1; // 중첩 1당 공격력 증가값
[SerializeField] private Sprite attackPowerIncreaseIcon; // 공격력 증가 아이콘
[TextArea(2, 5)]
[SerializeField] private string attackPowerIncreaseDescription; // 공격력 증가 설명
[SerializeField] private int attackPowerIncreaseSlotSortPriority = 106; // 공격력 증가 슬롯 정렬 우선순위

[Header("공격력 감소 상태효과")]
[SerializeField] private int attackPowerDecreaseStatusEffectID = 1008; // 공격력 감소 상태효과 ID
[SerializeField] private int attackPowerDecreaseApplyValuePerStack = 1; // 중첩 1당 공격력 감소값
[SerializeField] private Sprite attackPowerDecreaseIcon; // 공격력 감소 아이콘
[TextArea(2, 5)]
[SerializeField] private string attackPowerDecreaseDescription; // 공격력 감소 설명
[SerializeField] private int attackPowerDecreaseSlotSortPriority = 107; // 공격력 감소 슬롯 정렬 우선순위

[Header("방어 증가 상태효과")]
[SerializeField] private int defenseIncreaseStatusEffectID = 1009; // 방어 증가 상태효과 ID
[SerializeField] private int defenseIncreaseApplyValuePerStack = 1; // 중첩 1당 방어 증가값
[SerializeField] private Sprite defenseIncreaseIcon; // 방어 증가 아이콘
[TextArea(2, 5)]
[SerializeField] private string defenseIncreaseDescription; // 방어 증가 설명
[SerializeField] private int defenseIncreaseSlotSortPriority = 108; // 방어 증가 슬롯 정렬 우선순위

[Header("방어 감소 상태효과")]
[SerializeField] private int defenseDecreaseStatusEffectID = 1010; // 방어 감소 상태효과 ID
[SerializeField] private int defenseDecreaseApplyValuePerStack = 1; // 중첩 1당 방어 감소값
[SerializeField] private Sprite defenseDecreaseIcon; // 방어 감소 아이콘
[TextArea(2, 5)]
[SerializeField] private string defenseDecreaseDescription; // 방어 감소 설명
[SerializeField] private int defenseDecreaseSlotSortPriority = 109; // 방어 감소 슬롯 정렬 우선순위

[Header("와해 저항률 증가 상태효과")]
[SerializeField] private int staggerResistanceIncreaseStatusEffectID = 1011; // 와해 저항률 증가 상태효과 ID
[SerializeField] private int staggerResistanceIncreaseApplyValuePerStack = 1; // 중첩 1당 와해 저항률 증가값
[SerializeField] private Sprite staggerResistanceIncreaseIcon; // 와해 저항률 증가 아이콘
[TextArea(2, 5)]
[SerializeField] private string staggerResistanceIncreaseDescription; // 와해 저항률 증가 설명
[SerializeField] private int staggerResistanceIncreaseSlotSortPriority = 110; // 와해 저항률 증가 슬롯 정렬 우선순위

[Header("와해 저항률 감소 상태효과")]
[SerializeField] private int staggerResistanceDecreaseStatusEffectID = 1012; // 와해 저항률 감소 상태효과 ID
[SerializeField] private int staggerResistanceDecreaseApplyValuePerStack = 1; // 중첩 1당 와해 저항률 감소값
[SerializeField] private Sprite staggerResistanceDecreaseIcon; // 와해 저항률 감소 아이콘
[TextArea(2, 5)]
[SerializeField] private string staggerResistanceDecreaseDescription; // 와해 저항률 감소 설명
[SerializeField] private int staggerResistanceDecreaseSlotSortPriority = 111; // 와해 저항률 감소 슬롯 정렬 우선순위

[Header("이동속도률 증가 상태효과")]
[SerializeField] private int moveSpeedRateIncreaseStatusEffectID = 1013; // 이동속도률 증가 상태효과 ID
[SerializeField] private int moveSpeedRateIncreaseApplyValuePerStack = 1; // 중첩 1당 이동속도률 증가값
[SerializeField] private Sprite moveSpeedRateIncreaseIcon; // 이동속도률 증가 아이콘
[TextArea(2, 5)]
[SerializeField] private string moveSpeedRateIncreaseDescription; // 이동속도률 증가 설명
[SerializeField] private int moveSpeedRateIncreaseSlotSortPriority = 112; // 이동속도률 증가 슬롯 정렬 우선순위

[Header("이동속도률 감소 상태효과")]
[SerializeField] private int moveSpeedRateDecreaseStatusEffectID = 1014; // 이동속도률 감소 상태효과 ID
[SerializeField] private int moveSpeedRateDecreaseApplyValuePerStack = 1; // 중첩 1당 이동속도률 감소값
[SerializeField] private Sprite moveSpeedRateDecreaseIcon; // 이동속도률 감소 아이콘
[TextArea(2, 5)]
[SerializeField] private string moveSpeedRateDecreaseDescription; // 이동속도률 감소 설명
[SerializeField] private int moveSpeedRateDecreaseSlotSortPriority = 113; // 이동속도률 감소 슬롯 정렬 우선순위

    public int PowerRateIncreaseStatusEffectID => powerRateIncreaseStatusEffectID;
    public int PowerRateDecreaseStatusEffectID => powerRateDecreaseStatusEffectID;
    public int SpeedIncreaseStatusEffectID => speedIncreaseStatusEffectID;
    public int SpeedDecreaseStatusEffectID => speedDecreaseStatusEffectID;
    public int BodySizeIncreaseStatusEffectID => bodySizeIncreaseStatusEffectID;
    public int BodySizeDecreaseStatusEffectID => bodySizeDecreaseStatusEffectID;
    public int AttackPowerIncreaseStatusEffectID => attackPowerIncreaseStatusEffectID;
    public int AttackPowerDecreaseStatusEffectID => attackPowerDecreaseStatusEffectID;
    public int DefenseIncreaseStatusEffectID => defenseIncreaseStatusEffectID;
    public int DefenseDecreaseStatusEffectID => defenseDecreaseStatusEffectID;
    public int StaggerResistanceIncreaseStatusEffectID => staggerResistanceIncreaseStatusEffectID;
    public int StaggerResistanceDecreaseStatusEffectID => staggerResistanceDecreaseStatusEffectID;
    public int MoveSpeedRateIncreaseStatusEffectID => moveSpeedRateIncreaseStatusEffectID;
    public int MoveSpeedRateDecreaseStatusEffectID => moveSpeedRateDecreaseStatusEffectID;

    public int PowerRateIncreaseApplyValuePerStack => Mathf.Max(0, powerRateIncreaseApplyValuePerStack);
    public int PowerRateDecreaseApplyValuePerStack => Mathf.Max(0, powerRateDecreaseApplyValuePerStack);
    public int SpeedIncreaseApplyValuePerStack => Mathf.Max(0, speedIncreaseApplyValuePerStack);
    public int SpeedDecreaseApplyValuePerStack => Mathf.Max(0, speedDecreaseApplyValuePerStack);
    public int BodySizeIncreaseApplyValuePerStack => Mathf.Max(0, bodySizeIncreaseApplyValuePerStack);
    public int BodySizeDecreaseApplyValuePerStack => Mathf.Max(0, bodySizeDecreaseApplyValuePerStack);
    public int AttackPowerIncreaseApplyValuePerStack => Mathf.Max(0, attackPowerIncreaseApplyValuePerStack);
    public int AttackPowerDecreaseApplyValuePerStack => Mathf.Max(0, attackPowerDecreaseApplyValuePerStack);
    public int DefenseIncreaseApplyValuePerStack => Mathf.Max(0, defenseIncreaseApplyValuePerStack);
    public int DefenseDecreaseApplyValuePerStack => Mathf.Max(0, defenseDecreaseApplyValuePerStack);
    public int StaggerResistanceIncreaseApplyValuePerStack => Mathf.Max(0, staggerResistanceIncreaseApplyValuePerStack);
    public int StaggerResistanceDecreaseApplyValuePerStack => Mathf.Max(0, staggerResistanceDecreaseApplyValuePerStack);
    public int MoveSpeedRateIncreaseApplyValuePerStack => Mathf.Max(0, moveSpeedRateIncreaseApplyValuePerStack);
    public int MoveSpeedRateDecreaseApplyValuePerStack => Mathf.Max(0, moveSpeedRateDecreaseApplyValuePerStack);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public Sprite GetBaseStatStatusEffectIcon(int baseStatStatusEffectID)
    {
        if (baseStatStatusEffectID == powerRateIncreaseStatusEffectID) return powerRateIncreaseIcon;
        if (baseStatStatusEffectID == powerRateDecreaseStatusEffectID) return powerRateDecreaseIcon;
        if (baseStatStatusEffectID == speedIncreaseStatusEffectID) return speedIncreaseIcon;
        if (baseStatStatusEffectID == speedDecreaseStatusEffectID) return speedDecreaseIcon;
        if (baseStatStatusEffectID == bodySizeIncreaseStatusEffectID) return bodySizeIncreaseIcon;
        if (baseStatStatusEffectID == bodySizeDecreaseStatusEffectID) return bodySizeDecreaseIcon;
        if (baseStatStatusEffectID == attackPowerIncreaseStatusEffectID) return attackPowerIncreaseIcon;
        if (baseStatStatusEffectID == attackPowerDecreaseStatusEffectID) return attackPowerDecreaseIcon;
        if (baseStatStatusEffectID == defenseIncreaseStatusEffectID) return defenseIncreaseIcon;
        if (baseStatStatusEffectID == defenseDecreaseStatusEffectID) return defenseDecreaseIcon;
        if (baseStatStatusEffectID == staggerResistanceIncreaseStatusEffectID) return staggerResistanceIncreaseIcon;
        if (baseStatStatusEffectID == staggerResistanceDecreaseStatusEffectID) return staggerResistanceDecreaseIcon;
        if (baseStatStatusEffectID == moveSpeedRateIncreaseStatusEffectID) return moveSpeedRateIncreaseIcon;
        if (baseStatStatusEffectID == moveSpeedRateDecreaseStatusEffectID) return moveSpeedRateDecreaseIcon;

        return null;
    }

    public string GetBaseStatStatusEffectName(int baseStatStatusEffectID) // 기본 스탯 상태효과 이름 반환
{
    if (baseStatStatusEffectID == powerRateIncreaseStatusEffectID) return "위력률 증가";
    if (baseStatStatusEffectID == powerRateDecreaseStatusEffectID) return "위력률 감소";
    if (baseStatStatusEffectID == speedIncreaseStatusEffectID) return "속도 증가";
    if (baseStatStatusEffectID == speedDecreaseStatusEffectID) return "속도 감소";
    if (baseStatStatusEffectID == bodySizeIncreaseStatusEffectID) return "체급 증가";
    if (baseStatStatusEffectID == bodySizeDecreaseStatusEffectID) return "체급 감소";
    if (baseStatStatusEffectID == attackPowerIncreaseStatusEffectID) return "공격력 증가";
    if (baseStatStatusEffectID == attackPowerDecreaseStatusEffectID) return "공격력 감소";
    if (baseStatStatusEffectID == defenseIncreaseStatusEffectID) return "방어 증가";
    if (baseStatStatusEffectID == defenseDecreaseStatusEffectID) return "방어 감소";
    if (baseStatStatusEffectID == staggerResistanceIncreaseStatusEffectID) return "와해 저항률 증가";
    if (baseStatStatusEffectID == staggerResistanceDecreaseStatusEffectID) return "와해 저항률 감소";
    if (baseStatStatusEffectID == moveSpeedRateIncreaseStatusEffectID) return "이동속도률 증가";
    if (baseStatStatusEffectID == moveSpeedRateDecreaseStatusEffectID) return "이동속도률 감소";

    return "알 수 없는 상태효과";
}

public string GetBaseStatStatusEffectDescription(int baseStatStatusEffectID) // 기본 스탯 상태효과 설명 반환
{
    if (baseStatStatusEffectID == powerRateIncreaseStatusEffectID) return powerRateIncreaseDescription;
    if (baseStatStatusEffectID == powerRateDecreaseStatusEffectID) return powerRateDecreaseDescription;
    if (baseStatStatusEffectID == speedIncreaseStatusEffectID) return speedIncreaseDescription;
    if (baseStatStatusEffectID == speedDecreaseStatusEffectID) return speedDecreaseDescription;
    if (baseStatStatusEffectID == bodySizeIncreaseStatusEffectID) return bodySizeIncreaseDescription;
    if (baseStatStatusEffectID == bodySizeDecreaseStatusEffectID) return bodySizeDecreaseDescription;
    if (baseStatStatusEffectID == attackPowerIncreaseStatusEffectID) return attackPowerIncreaseDescription;
    if (baseStatStatusEffectID == attackPowerDecreaseStatusEffectID) return attackPowerDecreaseDescription;
    if (baseStatStatusEffectID == defenseIncreaseStatusEffectID) return defenseIncreaseDescription;
    if (baseStatStatusEffectID == defenseDecreaseStatusEffectID) return defenseDecreaseDescription;
    if (baseStatStatusEffectID == staggerResistanceIncreaseStatusEffectID) return staggerResistanceIncreaseDescription;
    if (baseStatStatusEffectID == staggerResistanceDecreaseStatusEffectID) return staggerResistanceDecreaseDescription;
    if (baseStatStatusEffectID == moveSpeedRateIncreaseStatusEffectID) return moveSpeedRateIncreaseDescription;
    if (baseStatStatusEffectID == moveSpeedRateDecreaseStatusEffectID) return moveSpeedRateDecreaseDescription;

    return string.Empty;
}

public int GetBaseStatStatusEffectSortPriority(int baseStatStatusEffectID) // 기본 스탯 상태효과 슬롯 정렬 우선순위 반환
{
    if (baseStatStatusEffectID == powerRateIncreaseStatusEffectID) return powerRateIncreaseSlotSortPriority;
    if (baseStatStatusEffectID == powerRateDecreaseStatusEffectID) return powerRateDecreaseSlotSortPriority;
    if (baseStatStatusEffectID == speedIncreaseStatusEffectID) return speedIncreaseSlotSortPriority;
    if (baseStatStatusEffectID == speedDecreaseStatusEffectID) return speedDecreaseSlotSortPriority;
    if (baseStatStatusEffectID == bodySizeIncreaseStatusEffectID) return bodySizeIncreaseSlotSortPriority;
    if (baseStatStatusEffectID == bodySizeDecreaseStatusEffectID) return bodySizeDecreaseSlotSortPriority;
    if (baseStatStatusEffectID == attackPowerIncreaseStatusEffectID) return attackPowerIncreaseSlotSortPriority;
    if (baseStatStatusEffectID == attackPowerDecreaseStatusEffectID) return attackPowerDecreaseSlotSortPriority;
    if (baseStatStatusEffectID == defenseIncreaseStatusEffectID) return defenseIncreaseSlotSortPriority;
    if (baseStatStatusEffectID == defenseDecreaseStatusEffectID) return defenseDecreaseSlotSortPriority;
    if (baseStatStatusEffectID == staggerResistanceIncreaseStatusEffectID) return staggerResistanceIncreaseSlotSortPriority;
    if (baseStatStatusEffectID == staggerResistanceDecreaseStatusEffectID) return staggerResistanceDecreaseSlotSortPriority;
    if (baseStatStatusEffectID == moveSpeedRateIncreaseStatusEffectID) return moveSpeedRateIncreaseSlotSortPriority;
    if (baseStatStatusEffectID == moveSpeedRateDecreaseStatusEffectID) return moveSpeedRateDecreaseSlotSortPriority;

    return 9999;
}

public bool IsBaseStatStatusEffectID(int baseStatStatusEffectID) // 기본 스탯 상태효과 ID인지 확인
{
    return
        baseStatStatusEffectID == powerRateIncreaseStatusEffectID ||
        baseStatStatusEffectID == powerRateDecreaseStatusEffectID ||
        baseStatStatusEffectID == speedIncreaseStatusEffectID ||
        baseStatStatusEffectID == speedDecreaseStatusEffectID ||
        baseStatStatusEffectID == bodySizeIncreaseStatusEffectID ||
        baseStatStatusEffectID == bodySizeDecreaseStatusEffectID ||
        baseStatStatusEffectID == attackPowerIncreaseStatusEffectID ||
        baseStatStatusEffectID == attackPowerDecreaseStatusEffectID ||
        baseStatStatusEffectID == defenseIncreaseStatusEffectID ||
        baseStatStatusEffectID == defenseDecreaseStatusEffectID ||
        baseStatStatusEffectID == staggerResistanceIncreaseStatusEffectID ||
        baseStatStatusEffectID == staggerResistanceDecreaseStatusEffectID ||
        baseStatStatusEffectID == moveSpeedRateIncreaseStatusEffectID ||
        baseStatStatusEffectID == moveSpeedRateDecreaseStatusEffectID;
}






}