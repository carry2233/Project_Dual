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
    [SerializeField] private CharacterFactionType factionType; // 캐릭터 진영 타입
    [SerializeField] private bool isDead; // 캐릭터 사망 여부

    public CharacterDuelAI CharacterDuelAI => characterDuelAI; // 캐릭터 AI 반환
    public int FirstRowID => firstRowID; // 1열 ID 반환
    public int SecondRowID => secondRowID; // 2열 ID 반환
    public int IndividualID => individualID; // 개체별 ID 반환
    public CharacterFactionType FactionType => factionType; // 진영 타입 반환
    public bool IsDead => isDead; // 사망 여부 반환

    public CharacterEntry(CharacterDuelAI targetAI, int targetFirstRowID, int targetSecondRowID, int targetIndividualID, CharacterFactionType targetFactionType, bool targetIsDead) // 정보 초기화
    {
        characterDuelAI = targetAI; // 캐릭터 AI 저장
        firstRowID = targetFirstRowID; // 1열 ID 저장
        secondRowID = targetSecondRowID; // 2열 ID 저장
        individualID = targetIndividualID; // 개체별 ID 저장
        factionType = targetFactionType; // 진영 타입 저장
        isDead = targetIsDead; // 사망 여부 저장
    }

    public void SetDead(bool dead) // 사망 여부 설정
    {
        isDead = dead; // 사망 여부 저장
    }
}

    public enum CharacterFactionType
{
    Friendly, // 아군
    Enemy // 적군
}

    public static GlobalCharacterManager Instance { get; private set; } // 싱글톤 인스턴스

    [Header("외부 저장 참조")]
    [SerializeField] private SaveStorage saveStorage; // 아군 사망 저장용 저장소 참조

    [Header("등록된 캐릭터 목록")]
    [SerializeField] private List<CharacterEntry> characterEntryList = new List<CharacterEntry>(); // 씬에 등록된 캐릭터 정보 목록

    public IReadOnlyList<CharacterEntry> CharacterEntryList => characterEntryList; // 등록 목록 반환

private void Awake() // 싱글톤 초기화
{
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject); // 중복 제거
        return;
    }

    Instance = this; // 싱글톤 등록

    if (saveStorage == null)
    {
        saveStorage = SaveStorage.Instance; // 저장소 자동 참조
    }
}

    private void Start() // 게임 시작 시 캐릭터 목록 구성
    {
        if (saveStorage == null)
        {
        saveStorage = SaveStorage.Instance; // 저장소 자동 참조
        }

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

CharacterStatSystem statSystem = characterAI.GetComponent<CharacterStatSystem>(); // 스탯 시스템 참조

CharacterEntry newEntry = new CharacterEntry(
    characterAI, // 캐릭터 AI 저장
    characterAI.GetFirstRowID(), // 1열 ID 저장
    characterAI.GetSecondRowID(), // 2열 ID 저장
    currentIndividualID, // 개체별 ID 저장
    ResolveFactionType(characterAI), // 진영 타입 저장
    statSystem != null && statSystem.IsDead); // 사망 여부 저장

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

public void RegisterCharacter(CharacterDuelAI targetCharacter) // 단일 캐릭터 등록
{
    RegisterCharacter(targetCharacter, ResolveFactionType(targetCharacter)); // 진영 자동 판단 후 등록
}

public void RegisterCharacter(CharacterDuelAI targetCharacter, CharacterFactionType factionType) // 진영 타입을 지정해 단일 캐릭터 등록
{
    if (targetCharacter == null)
    {
        return; // 대상이 없으면 종료
    }

    int currentIndividualID = targetCharacter.GetIndividualID(); // 현재 개체별 ID 확인

    if (currentIndividualID <= 0 || IsIndividualIDAlreadyRegistered(currentIndividualID))
    {
        currentIndividualID = GetNextRuntimeIndividualID(); // 사용 가능한 새 ID 계산
        targetCharacter.SetIndividualID(currentIndividualID); // 캐릭터에 새 ID 적용
    }

    CharacterStatSystem statSystem = targetCharacter.GetComponent<CharacterStatSystem>(); // 스탯 시스템 참조

    CharacterEntry newEntry = new CharacterEntry(
        targetCharacter, // 캐릭터 AI 저장
        targetCharacter.GetFirstRowID(), // 1열 ID 저장
        targetCharacter.GetSecondRowID(), // 2열 ID 저장
        currentIndividualID, // 개체별 ID 저장
        factionType, // 진영 타입 저장
        statSystem != null && statSystem.IsDead); // 사망 여부 저장

    characterEntryList.Add(newEntry); // 등록 목록에 추가
}

public CharacterDuelAI FindCharacterByID(int firstRowID, int secondRowID, int individualID) // ID 기준 캐릭터 탐색
{
    for (int i = 0; i < characterEntryList.Count; i++)
    {
        CharacterEntry entry = characterEntryList[i]; // 현재 등록 정보

        if (entry == null)
        {
            continue; // 비어 있으면 건너뜀
        }

        if (entry.FirstRowID != firstRowID)
        {
            continue; // 1열 ID가 다르면 건너뜀
        }

        if (entry.SecondRowID != secondRowID)
        {
            continue; // 2열 ID가 다르면 건너뜀
        }

        if (entry.IndividualID != individualID)
        {
            continue; // 개체별 ID가 다르면 건너뜀
        }

        return entry.CharacterDuelAI; // 일치 캐릭터 반환
    }

    return null; // 찾지 못하면 null 반환
}

private bool IsIndividualIDAlreadyRegistered(int targetIndividualID) // 개체별 ID 중복 여부 확인
{
    for (int i = 0; i < characterEntryList.Count; i++)
    {
        CharacterEntry entry = characterEntryList[i]; // 현재 등록 정보

        if (entry == null)
        {
            continue; // 비어 있으면 건너뜀
        }

        if (entry.IndividualID == targetIndividualID)
        {
            return true; // 이미 사용 중이면 true
        }
    }

    return false; // 중복 없음
}

private int GetNextRuntimeIndividualID() // 런타임용 다음 개체별 ID 계산
{
    HashSet<int> usedIDSet = new HashSet<int>(); // 사용 중 ID 저장

    for (int i = 0; i < characterEntryList.Count; i++)
    {
        CharacterEntry entry = characterEntryList[i]; // 현재 등록 정보

        if (entry == null)
        {
            continue; // 비어 있으면 건너뜀
        }

        if (entry.IndividualID > 0)
        {
            usedIDSet.Add(entry.IndividualID); // 유효 ID 등록
        }
    }

    return GetNextAvailableIndividualID(usedIDSet, 1); // 사용 가능한 ID 반환
}

private CharacterFactionType ResolveFactionType(CharacterDuelAI targetCharacter) // 캐릭터 조작 타입 기준 진영 판단
{
    if (targetCharacter == null)
    {
        return CharacterFactionType.Enemy; // 대상이 없으면 적군 기본 처리
    }

    if (targetCharacter.CurrentControlMode == CharacterDuelAI.ControlMode.PlayerControlled)
    {
        return CharacterFactionType.Friendly; // 플레이어 조작형이면 아군
    }

    return CharacterFactionType.Enemy; // 그 외는 적군
}

public void NotifyCharacterDeath(CharacterDuelAI deadCharacter) // 캐릭터 사망 알림 처리
{
    if (deadCharacter == null)
    {
        return; // 대상이 없으면 종료
    }

    CharacterEntry targetEntry = FindEntryByCharacter(deadCharacter); // 등록 정보 탐색

    if (targetEntry == null)
    {
        return; // 등록 정보가 없으면 종료
    }

    targetEntry.SetDead(true); // 엔트리 사망 여부 체크

    if (targetEntry.FactionType == CharacterFactionType.Friendly)
    {
        MarkFriendlyCharacterDead(targetEntry); // 아군 사망 저장 처리
        return;
    }

    ReleaseEnemyCharacterID(deadCharacter); // 적군 ID 반환 처리
}

private CharacterEntry FindEntryByCharacter(CharacterDuelAI targetCharacter) // 캐릭터 참조 기준 엔트리 탐색
{
    for (int i = 0; i < characterEntryList.Count; i++)
    {
        CharacterEntry entry = characterEntryList[i]; // 현재 엔트리

        if (entry == null)
        {
            continue; // 비어 있으면 건너뜀
        }

        if (entry.CharacterDuelAI == targetCharacter)
        {
            return entry; // 일치 엔트리 반환
        }
    }

    return null; // 찾지 못하면 null
}

private void MarkFriendlyCharacterDead(CharacterEntry targetEntry) // 아군 사망 상태 저장
{
    if (saveStorage == null)
    {
        saveStorage = SaveStorage.Instance; // 저장소 재참조
    }

    if (saveStorage == null || targetEntry == null)
    {
        return; // 저장소 또는 엔트리가 없으면 종료
    }

    saveStorage.SetOwnedCharacterDeadState(
        targetEntry.FirstRowID,
        targetEntry.SecondRowID,
        targetEntry.IndividualID,
        true); // 현재 소유 캐릭터 목록에 사망 체크
}

private void ReleaseEnemyCharacterID(CharacterDuelAI deadEnemy) // 적군 사망 시 개체별 ID 반환
{
    if (deadEnemy == null)
    {
        return; // 대상이 없으면 종료
    }

    int deadIndividualID = deadEnemy.GetIndividualID(); // 사망한 적군 ID 저장
    deadEnemy.SetIndividualID(0); // 적군 개체별 ID 비우기

    for (int i = characterEntryList.Count - 1; i >= 0; i--)
    {
        CharacterEntry entry = characterEntryList[i]; // 현재 엔트리

        if (entry == null)
        {
            continue; // 비어 있으면 건너뜀
        }

        if (entry.IndividualID == deadIndividualID)
        {
            characterEntryList.RemoveAt(i); // 목록에서 제거해 ID 재사용 가능하게 처리
            return;
        }
    }
}









}