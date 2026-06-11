using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class CharacterUIImageData
{
    [Header("표시 이미지")]
    public Sprite displaySprite;

    [Header("UI 위치")]
    public Vector2 anchoredPosition;

    [Header("UI 회전")]
    public Vector3 localEulerAngles;
}


/// <summary>
/// 캐릭터 전역 정의 ScriptableObject
/// - 캐릭터 식별 ID를 보관
/// - 상세 UI 프리팹, 전투 목록 UI 프리팹, 관리창 슬롯 프리팹을 연결
/// - 캐릭터 ID 기준 매칭 기능 제공
/// </summary>

[CreateAssetMenu(fileName = "NewGlobalCharacterDefinition", menuName = "Project Dual/캐릭터 전역 정의")]
public class GlobalCharacterDefinition : ScriptableObject
{
    [Header("캐릭터 식별값")]
    [SerializeField] private int firstRowID; // 캐릭터 1열 ID
    [SerializeField] private int secondRowID; // 캐릭터 2열 ID

[Header("허기 감소 설정")]
[SerializeField] private int hungerDecreaseIntervalMinute = 1; // 허기 감소가 실행될 분 간격
[SerializeField] private int hungerDecreaseAmount = 1; // 감소 주기마다 감소할 허기량

public int HungerDecreaseIntervalMinute => hungerDecreaseIntervalMinute; // 허기 감소 분 간격 반환
public int HungerDecreaseAmount => hungerDecreaseAmount; // 주기당 허기 감소량 반환


    [Header("인게임 캐릭터 프리팹")]
    [SerializeField] private GameObject inGameCharacterPrefab; // 전투씬에서 실제 생성할 캐릭터 프리팹

    [Header("아군 인벤토리 지급 순서")]
    public int displayPriority; // 아이템 지급 대상 나열 우선순위값

    [Header("상세 UI 프리팹")]
    [SerializeField] private GameObject detailUIPrefab; // 선택 캐릭터 상세 UI 프리팹

    [Header("전투씬 캐릭터 목록 UI 프리팹")]
    [SerializeField] private GameObject battleCharacterListUIPrefab; // 전투씬 목록 슬롯 프리팹

    [Header("관리창 캐릭터 슬롯 프리팹")]
    [SerializeField] private GameObject managementCharacterSlotPrefab; // 캐릭터 관리창 슬롯 프리팹

    [Header("인벤토리 캐릭터 슬롯 프리팹")]
    [SerializeField] private CharacterInventorySlot characterInventorySlotPrefab; // 인벤토리 분포창 캐릭터 슬롯 프리팹


[Header("________________________________________________________________________________")]

[Header("캐릭터 이름")]
[SerializeField] private string characterName; // 캐릭터 표시 이름

[Header("기본 최대허기")]
[SerializeField] private int baseMaxHunger = 100; // 기본 최대 허기


[Header("기본 최대체력")]
[SerializeField] private int baseMaxHealth; // 기본 최대체력

[Header("기본 공격력")]
[SerializeField] private int baseAttackPower; // 기본 공격력

[Header("기본 방어율")]
[SerializeField] private int baseDefenseValue; // 기본 방어율

[Header("기본 체급")]
[SerializeField] private int baseBodySize; // 기본 체급

[Header("기본 속도")]
[SerializeField] private int baseSpeedStat; // 기본 속도

[Header("기본 위력률")]
[SerializeField] private int basePowerRatePercent = 100; // 기본 위력률

[Header("기본 이동속도")]
[SerializeField] private float baseMoveSpeed = 5f; // 기본 이동속도

[Header("기본 이동속도율")]
[SerializeField] private int baseMoveSpeedPercent = 100; // 기본 이동속도율

[Header("기본 최대와해량")]
[SerializeField] private int baseMaxStaggerAmount = 100; // 기본 최대와해량

[Header("기본 와해 저항률")]
[SerializeField] private int baseStaggerResistancePercent; // 기본 와해 저항률


[Header("________________________________________________________________________________")]


[Header("레벨당 최대체력 증가값")]
[SerializeField] private int increaseMaxHealthPerLevel; // 레벨당 최대체력 증가값

[Header("레벨당 공격력 증가값")]
[SerializeField] private int increaseAttackPowerPerLevel; // 레벨당 공격력 증가값

[Header("레벨당 방어율 증가값")]
[SerializeField] private int increaseDefenseValuePerLevel; // 레벨당 방어율 증가값

[Header("레벨당 체급 증가값")]
[SerializeField] private int increaseBodySizePerLevel; // 레벨당 체급 증가값

[Header("레벨당 속도 증가값")]
[SerializeField] private int increaseSpeedStatPerLevel; // 레벨당 속도 증가값

[Header("레벨당 위력률 증가값")]
[SerializeField] private int increasePowerRatePercentPerLevel; // 레벨당 위력률 증가값

[Header("레벨당 이동속도 증가값")]
[SerializeField] private float increaseMoveSpeedPerLevel; // 레벨당 이동속도 증가값

[Header("레벨당 이동속도율 증가값")]
[SerializeField] private int increaseMoveSpeedPercentPerLevel; // 레벨당 이동속도율 증가값

[Header("레벨당 최대와해량 증가값")]
[SerializeField] private int increaseMaxStaggerAmountPerLevel; // 레벨당 최대와해량 증가값

[Header("레벨당 와해 저항률 증가값")]
[SerializeField] private int increaseStaggerResistancePercentPerLevel; // 레벨당 와해 저항률 증가값


[Header("________________________________________________________________________________")]


[Header("기본 충족 경험치")]
[SerializeField] private int baseRequiredExperience; // 기본충족경험치

[Header("레벨 경험치 계수")]
[SerializeField] private int requiredExperienceLevelMultiplier; // 레벨에 곱해질 계수


[Header("________________________________________________________________________________")]


[Header("공격기술 치명타 설정")]
[SerializeField] private int criticalAttackDamagePercent = 150; // 치명타 공격 시 적용될 피해 퍼센트
[SerializeField] private int criticalHitExperienceReward = 1; // 치명타 적중 시 획득 경험치

public int CriticalAttackDamagePercent => criticalAttackDamagePercent;
public int CriticalHitExperienceReward => criticalHitExperienceReward;

public CharacterInventorySlot CharacterInventorySlotPrefab => characterInventorySlotPrefab; // 인벤토리 캐릭터 슬롯 프리팹 반환

public List<CharacterUIImageData> characterUIImageList = new List<CharacterUIImageData>();

public GameObject ManagementCharacterSlotPrefab => managementCharacterSlotPrefab; // 캐릭터 관리창 슬롯 프리팹 반환

public GameObject InGameCharacterPrefab => inGameCharacterPrefab; // 인게임 캐릭터 프리팹 반환

    public int FirstRowID => firstRowID; // 캐릭터 1열 ID 반환
    public int SecondRowID => secondRowID; // 캐릭터 2열 ID 반환
    public string CharacterName => characterName; // 캐릭터 표시 이름 반환
    public int BaseMaxHunger => baseMaxHunger; // 기본 최대 허기 반환
    public GameObject DetailUIPrefab => detailUIPrefab; // 상세 UI 프리팹 반환
    public GameObject BattleCharacterListUIPrefab => battleCharacterListUIPrefab; // 목록 UI 프리팹 반환

    public int BaseRequiredExperience => baseRequiredExperience; // 기본충족경험치 반환
public int RequiredExperienceLevelMultiplier => requiredExperienceLevelMultiplier; // 레벨 경험치 계수 반환

public int CalculateRequiredExperience(int level) // 레벨업 필요 경험치 계산
{
    int safeLevel = Mathf.Max(1, level); // 최소 레벨 보정
    return baseRequiredExperience + safeLevel * requiredExperienceLevelMultiplier; // 필요 경험치 계산
}

public SaveStorage.OwnedCharacterStatData CreateCalculatedStatData(
    int targetFirstRowID,
    int targetSecondRowID,
    int targetIndividualID,
    int targetLevel,
    int currentExperience) // 정의값 기준 저장 스탯 데이터 생성
{
    int safeLevel = Mathf.Max(1, targetLevel); // 최소 레벨 보정

    SaveStorage.OwnedCharacterStatData statData = new SaveStorage.OwnedCharacterStatData(); // 저장 스탯 데이터 생성

    statData.firstRowID = targetFirstRowID; // 1열 ID 적용
    statData.secondRowID = targetSecondRowID; // 2열 ID 적용
    statData.individualID = targetIndividualID; // 개체 ID 적용

    statData.levelstats = safeLevel; // 레벨 적용
    statData.currentExperience = Mathf.Max(0, currentExperience); // 현재 경험치 적용
    statData.levelUpRequiredExperience = CalculateRequiredExperience(safeLevel); // 레벨업 필요 경험치 적용

    statData.maxHealth = baseMaxHealth + safeLevel * increaseMaxHealthPerLevel; // 최대체력 계산
    statData.currentHealth = statData.maxHealth; // 현재체력은 최대체력으로 시작
    statData.attackPower = baseAttackPower + safeLevel * increaseAttackPowerPerLevel; // 공격력 계산
    statData.defenseValue = baseDefenseValue + safeLevel * increaseDefenseValuePerLevel; // 방어율 계산
    statData.bodySize = baseBodySize + safeLevel * increaseBodySizePerLevel; // 체급 계산
    statData.speedStat = baseSpeedStat + safeLevel * increaseSpeedStatPerLevel; // 속도 계산
    statData.powerRatePercent = basePowerRatePercent + safeLevel * increasePowerRatePercentPerLevel; // 위력률 계산

    statData.baseMoveSpeed = baseMoveSpeed + safeLevel * increaseMoveSpeedPerLevel; // 이동속도 계산
    statData.moveSpeedPercent = baseMoveSpeedPercent + safeLevel * increaseMoveSpeedPercentPerLevel; // 이동속도율 계산
    statData.finalMoveSpeed = Mathf.Floor((statData.baseMoveSpeed * (statData.moveSpeedPercent / 100f)) * 10f) / 10f; // 최종 이동속도 계산

    statData.maxStaggerAmount = baseMaxStaggerAmount + safeLevel * increaseMaxStaggerAmountPerLevel; // 최대와해량 계산
    statData.currentStaggerAmount = 0; // 현재 와해량 초기화
    statData.staggerResistancePercent = baseStaggerResistancePercent + safeLevel * increaseStaggerResistancePercentPerLevel; // 와해 저항률 계산

    return statData; // 계산된 스탯 반환
}

    public bool IsMatch(CharacterDuelAI targetCharacter) // 대상 캐릭터와 식별값 일치 여부 반환
    {
        if (targetCharacter == null)
        {
            return false; // 대상이 없으면 false
        }

        return targetCharacter.FirstRowID == firstRowID && targetCharacter.SecondRowID == secondRowID; // 두 식별값 모두 같으면 true
    }

    public bool IsMatch(int targetFirstRowID, int targetSecondRowID) // ID 두 값과 식별값 일치 여부 반환
{
    return targetFirstRowID == firstRowID && targetSecondRowID == secondRowID; // 두 식별값 모두 같으면 true
}
}