using UnityEngine;

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
}