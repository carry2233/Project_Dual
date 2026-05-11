using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 아군 캐릭터 정의 정보를 전역으로 관리하는 매니저입니다.
/// </summary>
public class FriendlyCharacterInfoManager : MonoBehaviour
{
    public static FriendlyCharacterInfoManager Instance { get; private set; } // 전역 접근용 인스턴스

    [Header("아군 캐릭터 정의 목록")]
    public List<FriendlyCharacterDefinition> friendlyCharacterDefinitionList = new List<FriendlyCharacterDefinition>(); // 아군 캐릭터 정의 리스트

    private void Awake() // 싱글톤 초기화 및 씬 유지 설정
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public FriendlyCharacterDefinition FindDefinitionByID(int firstRowID, int secondRowID) // 캐릭터 ID 기준 아군 정의 찾기
    {
        for (int i = 0; i < friendlyCharacterDefinitionList.Count; i++)
        {
            FriendlyCharacterDefinition definition = friendlyCharacterDefinitionList[i];

            if (definition == null)
                continue;

            if (!definition.IsMatch(firstRowID, secondRowID))
                continue;

            return definition;
        }

        return null;
    }
}