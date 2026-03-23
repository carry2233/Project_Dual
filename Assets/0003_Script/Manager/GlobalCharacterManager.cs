using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전역 캐릭터 관리 매니저
/// - 게임 시작 시 씬의 CharacterDuelAI를 전부 수집
/// - 각 캐릭터의 1열 ID / 2열 ID / 개체별 ID를 함께 저장
/// - 개체별 ID가 없는 캐릭터에는 중복되지 않는 새 ID를 부여
/// </summary>
public class GlobalCharacterManager : MonoBehaviour
{
    [System.Serializable]
    public class CharacterEntry // 캐릭터 관리 정보 단위
    {
        [SerializeField] private CharacterDuelAI characterDuelAI; // 캐릭터 AI 참조
        [SerializeField] private int firstRowID; // 캐릭터 1열 ID
        [SerializeField] private int secondRowID; // 캐릭터 2열 ID
        [SerializeField] private int individualID; // 캐릭터 개체별 ID

        public CharacterDuelAI CharacterDuelAI => characterDuelAI; // 캐릭터 AI 반환
        public int FirstRowID => firstRowID; // 1열 ID 반환
        public int SecondRowID => secondRowID; // 2열 ID 반환
        public int IndividualID => individualID; // 개체별 ID 반환

        public CharacterEntry(CharacterDuelAI targetAI, int targetFirstRowID, int targetSecondRowID, int targetIndividualID) // 정보 초기화
        {
            characterDuelAI = targetAI; // 캐릭터 AI 저장
            firstRowID = targetFirstRowID; // 1열 ID 저장
            secondRowID = targetSecondRowID; // 2열 ID 저장
            individualID = targetIndividualID; // 개체별 ID 저장
        }
    }

    public static GlobalCharacterManager Instance { get; private set; } // 싱글톤 인스턴스

    [Header("등록된 캐릭터 목록")]
    [SerializeField] private List<CharacterEntry> characterEntryList = new List<CharacterEntry>(); // 씬에 등록된 캐릭터 정보 목록

    public IReadOnlyList<CharacterEntry> CharacterEntryList => characterEntryList; // 등록 목록 반환

    private void Awake() // 싱글톤 초기화
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // 중복 인스턴스 제거
            return;
        }

        Instance = this; // 싱글톤 인스턴스 저장
    }

    private void Start() // 게임 시작 시 캐릭터 목록 구성
    {
        RebuildCharacterList(); // 씬의 캐릭터 전체 재수집
    }

    public void RebuildCharacterList() // 씬의 캐릭터 목록 재구성
    {
        characterEntryList.Clear(); // 기존 목록 초기화

        CharacterDuelAI[] characterArray = FindObjectsByType<CharacterDuelAI>(FindObjectsSortMode.None); // 씬의 모든 CharacterDuelAI 탐색
        HashSet<int> usedIndividualIDSet = new HashSet<int>(); // 이미 사용 중인 개체별 ID 저장용
        int nextAvailableID = 1; // 새로 부여할 다음 개체별 ID

        for (int i = 0; i < characterArray.Length; i++)
        {
            CharacterDuelAI characterAI = characterArray[i]; // 현재 캐릭터 참조

            if (characterAI == null)
            {
                continue; // 비어 있으면 건너뜀
            }

            int currentIndividualID = characterAI.GetIndividualID(); // 현재 개체별 ID 확인

            if (currentIndividualID > 0)
            {
                usedIndividualIDSet.Add(currentIndividualID); // 이미 있는 유효 ID 등록
            }
        }

        nextAvailableID = GetNextAvailableIndividualID(usedIndividualIDSet, nextAvailableID); // 첫 사용 가능 ID 계산

        for (int i = 0; i < characterArray.Length; i++)
        {
            CharacterDuelAI characterAI = characterArray[i]; // 현재 캐릭터 참조

            if (characterAI == null)
            {
                continue; // 비어 있으면 건너뜀
            }

            int currentIndividualID = characterAI.GetIndividualID(); // 현재 개체별 ID 확인

            if (currentIndividualID <= 0)
            {
                currentIndividualID = nextAvailableID; // 새 개체별 ID 지정
                characterAI.SetIndividualID(currentIndividualID); // 캐릭터에 개체별 ID 저장
                usedIndividualIDSet.Add(currentIndividualID); // 사용 중 ID 목록에 등록
                nextAvailableID = GetNextAvailableIndividualID(usedIndividualIDSet, currentIndividualID + 1); // 다음 사용 가능 ID 계산
            }

            CharacterEntry newEntry = new CharacterEntry(
                characterAI, // 캐릭터 AI 저장
                characterAI.GetFirstRowID(), // 1열 ID 저장
                characterAI.GetSecondRowID(), // 2열 ID 저장
                currentIndividualID); // 개체별 ID 저장

            characterEntryList.Add(newEntry); // 목록에 등록
        }
    }

    private int GetNextAvailableIndividualID(HashSet<int> usedIndividualIDSet, int startID) // 사용 중이지 않은 다음 개체별 ID 계산
    {
        int candidateID = Mathf.Max(1, startID); // 1 이상부터 시작

        while (usedIndividualIDSet.Contains(candidateID))
        {
            candidateID++; // 이미 사용 중이면 다음 값 확인
        }

        return candidateID; // 사용 가능한 ID 반환
    }
}