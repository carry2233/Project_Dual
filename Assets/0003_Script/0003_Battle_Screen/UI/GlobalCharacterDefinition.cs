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

    [Header("상세 UI 프리팹")]
    [SerializeField] private GameObject detailUIPrefab; // 선택 캐릭터 상세 UI 프리팹

    [Header("전투씬 캐릭터 목록 UI 프리팹")]
    [SerializeField] private GameObject battleCharacterListUIPrefab; // 전투씬 목록 슬롯 프리팹

    [Header("관리창 캐릭터 슬롯 프리팹")]
    [SerializeField] private GameObject managementCharacterSlotPrefab; // 캐릭터 관리창 슬롯 프리팹

    [Header("인벤토리 캐릭터 슬롯 프리팹")]
    [SerializeField] private CharacterInventorySlot characterInventorySlotPrefab; // 인벤토리 분포창 캐릭터 슬롯 프리팹

public CharacterInventorySlot CharacterInventorySlotPrefab => characterInventorySlotPrefab; // 인벤토리 캐릭터 슬롯 프리팹 반환

public List<CharacterUIImageData> characterUIImageList = new List<CharacterUIImageData>();

public GameObject ManagementCharacterSlotPrefab => managementCharacterSlotPrefab; // 캐릭터 관리창 슬롯 프리팹 반환

    public int FirstRowID => firstRowID; // 캐릭터 1열 ID 반환
    public int SecondRowID => secondRowID; // 캐릭터 2열 ID 반환
    public GameObject DetailUIPrefab => detailUIPrefab; // 상세 UI 프리팹 반환
    public GameObject BattleCharacterListUIPrefab => battleCharacterListUIPrefab; // 목록 UI 프리팹 반환

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