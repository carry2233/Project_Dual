using System.Collections.Generic; // 리스트 사용
using UnityEngine; // Unity 기본 기능

/// <summary>
/// 캐릭터 정보 전역 관리 매니저
/// - GlobalCharacterDefinition 리스트를 전역 보관
/// - DontDestroyOnLoad로 씬 전환 후에도 유지
/// - 캐릭터 식별값 기준으로 정의 검색 제공
/// </summary>
public class CharacterInfoManager : MonoBehaviour
{
    public static CharacterInfoManager Instance { get; private set; } // 전역 접근용 인스턴스

    [Header("캐릭터 전역 정의 목록")]
    [SerializeField] private List<GlobalCharacterDefinition> globalCharacterDefinitionList = new List<GlobalCharacterDefinition>(); // 전역 캐릭터 정의 리스트

    public IReadOnlyList<GlobalCharacterDefinition> GlobalCharacterDefinitionList => globalCharacterDefinitionList; // 전역 정의 리스트 반환

    public int GetCriticalAttackDamagePercent(int firstRowID, int secondRowID) // 캐릭터 치명타 피해 퍼센트 반환
{
    GlobalCharacterDefinition definition = FindDefinitionByID(firstRowID, secondRowID);

    if (definition == null)
        return 100;

    return Mathf.Max(0, definition.CriticalAttackDamagePercent);
}

public int GetCriticalHitExperienceReward(int firstRowID, int secondRowID) // 캐릭터 치명타 경험치 반환
{
    GlobalCharacterDefinition definition = FindDefinitionByID(firstRowID, secondRowID);

    if (definition == null)
        return 0;

    return Mathf.Max(0, definition.CriticalHitExperienceReward);
}

    private void Awake() // 싱글톤 초기화 및 씬 유지 설정
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // 이미 인스턴스가 있으면 중복 제거
            return;
        }

        Instance = this; // 현재 오브젝트를 전역 인스턴스로 등록
        DontDestroyOnLoad(gameObject); // 씬 전환 시에도 유지
    }

    public GlobalCharacterDefinition FindDefinitionByCharacter(CharacterDuelAI targetCharacter) // 캐릭터 기준 전역 정의 탐색
    {
        if (targetCharacter == null)
        {
            return null; // 대상이 없으면 종료
        }

        for (int i = 0; i < globalCharacterDefinitionList.Count; i++)
        {
            GlobalCharacterDefinition definition = globalCharacterDefinitionList[i]; // 현재 정의 참조

            if (definition == null)
            {
                continue; // 비어 있으면 건너뜀
            }

            if (!definition.IsMatch(targetCharacter))
            {
                continue; // 식별값이 다르면 건너뜀
            }

            return definition; // 일치하는 정의 반환
        }

        return null; // 찾지 못했으면 null 반환
    }

    public GlobalCharacterDefinition FindDefinitionByID(int firstRowID, int secondRowID) // ID 기준 전역 정의 탐색
    {
        for (int i = 0; i < globalCharacterDefinitionList.Count; i++)
        {
            GlobalCharacterDefinition definition = globalCharacterDefinitionList[i]; // 현재 정의 참조

            if (definition == null)
            {
                continue; // 비어 있으면 건너뜀
            }

            if (!definition.IsMatch(firstRowID, secondRowID))
            {
                continue; // 식별값이 다르면 건너뜀
            }

            return definition; // 일치하는 정의 반환
        }

        return null; // 찾지 못했으면 null 반환
    }
}