using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System; // 이벤트 Action 사용
using Random = UnityEngine.Random;
public class FriendlyCharacterManager : MonoBehaviour
{
    [System.Serializable]
    public class FriendlyCharacterEntry
    {
        [SerializeField] private CharacterDuelAI characterDuelAI; // 등록된 아군 캐릭터 참조
        [SerializeField] private MoveCommandController moveCommandController; // 해당 캐릭터의 이동 명령 컨트롤러
        [SerializeField] private int assignedSelectionOrder; // 런타임에 부여된 실제 선택 순서값

        public CharacterDuelAI CharacterDuelAI => characterDuelAI; // 캐릭터 참조 반환
        public MoveCommandController MoveCommandController => moveCommandController; // 이동 명령 컨트롤러 반환
        public int AssignedSelectionOrder => assignedSelectionOrder; // 실제 선택 순서값 반환

        public FriendlyCharacterEntry(CharacterDuelAI targetCharacter, MoveCommandController targetController, int targetOrder)
        {
            characterDuelAI = targetCharacter; // 캐릭터 저장
            moveCommandController = targetController; // 컨트롤러 저장
            assignedSelectionOrder = targetOrder; // 순서값 저장
        }
    }

    [Header("필수 참조")]
    [SerializeField] private ClickMoveSystemManager clickMoveSystemManager; // 선택 반영용 클릭 이동 시스템 매니저

    [Header("캐릭터 정보 매니저 참조")]
    [SerializeField] private CharacterInfoManager characterInfoManager; // 전역 캐릭터 정보 매니저 참조
    public IReadOnlyList<GlobalCharacterDefinition> GlobalCharacterDefinitionList =>
    characterInfoManager != null ? characterInfoManager.GlobalCharacterDefinitionList : null; // 전역 정의 목록 반환

    [Header("아군 진영 설정")]
    [SerializeField] private List<int> friendlyTeamNumbers = new List<int>(); // 아군으로 판정할 팀 번호 리스트

    [Header("숫자키 선택 설정")]
    [SerializeField] private List<Key> selectionKeyList = new List<Key>()
    {
        Key.Digit1, // 1번 선택 키
        Key.Digit2, // 2번 선택 키
        Key.Digit3, // 3번 선택 키
        Key.Digit4, // 4번 선택 키
        Key.Digit5, // 5번 선택 키
        Key.Digit6, // 6번 선택 키
        Key.Digit7, // 7번 선택 키
        Key.Digit8, // 8번 선택 키
        Key.Digit9  // 9번 선택 키
    };

    [Header("상세 UI 토글 설정")]
    [SerializeField] private Key toggleDetailUIKey = Key.Tab; // 선택 캐릭터 상세 UI 토글 키
    [SerializeField] private Transform detailUIParent; // 상세 UI 생성 부모
    

    [Header("현재 아군 캐릭터 목록")]
    [SerializeField] private List<FriendlyCharacterEntry> friendlyCharacterEntryList = new List<FriendlyCharacterEntry>(); // 정렬 후 아군 캐릭터 목록

    [Header("현재 선택 상태")]
    [SerializeField] private CharacterDuelAI currentSelectedFriendlyCharacter; // 현재 선택된 아군 캐릭터
    [SerializeField] private GameObject currentSpawnedDetailUIObject; // 현재 생성된 상세 UI 오브젝트
    [SerializeField] private CharacterDuelAI currentDetailUIOwnerCharacter; // 현재 상세 UI를 소유한 캐릭터

    [Header("전투씬 아군 생성 설정")]
[SerializeField] private SaveStorage saveStorage; // 저장 데이터 참조
[SerializeField] private GlobalCharacterManager globalCharacterManager; // 전역 캐릭터 관리자 참조
[SerializeField] private Transform friendlySpawnCenter; // 아군 생성 기준 위치
[SerializeField] private float friendlySpawnRadius = 3f; // 아군 생성 반지름

[Header("아군 평균 레벨")]
[SerializeField] private int friendlyAverageLevel = 1; // 현재 아군 평균 레벨

[Header("전투 아군 준비 상태")]
[SerializeField] private bool isFriendlyBattleSetupCompleted; // 아군 생성, 목록 구성, 평균 레벨 계산 완료 여부


[Header("결투기술 선택 UI")]
[SerializeField] private GameObject duelSkillListParentPanel; // 결투기술 목록 부모 패널
[SerializeField] private Transform duelSkillSlotListPanel; // 결투기술 슬롯들이 생성될 부모
[SerializeField] private List<DuelSkillSlot> spawnedDuelSkillSlotList = new List<DuelSkillSlot>(); // 생성된 결투기술 슬롯 목록

[Header("결투기술 선택 상태")]
[SerializeField] private CharacterDuelAI currentDuelSkillMenuOwner; // 현재 결투기술 목록을 연 캐릭터
[SerializeField] private DuelSkillSlot currentHoveredDuelSkillSlot; // 현재 마우스가 올라간 결투기술 슬롯

public bool IsFriendlyBattleSetupCompleted => isFriendlyBattleSetupCompleted; // 전투 아군 준비 완료 여부 반환

public int FriendlyAverageLevel => friendlyAverageLevel; // 아군 평균 레벨 반환

    public event Action OnFriendlyCharacterListRebuilt; // 아군 목록 재구성 완료 알림 이벤트

    public IReadOnlyList<FriendlyCharacterEntry> FriendlyCharacterEntryList => friendlyCharacterEntryList; // 아군 목록 반환
    public CharacterDuelAI CurrentSelectedFriendlyCharacter => currentSelectedFriendlyCharacter; // 현재 선택된 아군 캐릭터 반환
    public Key ToggleDetailUIKey => toggleDetailUIKey; // 상세 UI 토글 키 반환

private void Awake() // 시작 시 참조 자동 연결
{
    if (clickMoveSystemManager == null)
    {
        clickMoveSystemManager = FindFirstObjectByType<ClickMoveSystemManager>(); // 씬에서 자동 탐색
    }

    if (characterInfoManager == null)
    {
        characterInfoManager = CharacterInfoManager.Instance; // 전역 인스턴스 우선 참조
    }

    if (characterInfoManager == null)
    {
        characterInfoManager = FindFirstObjectByType<CharacterInfoManager>(); // 씬에 있는 매니저 자동 탐색
    }

    if (detailUIParent == null)
    {
        detailUIParent = transform; // 부모가 없으면 자기 자신 사용
    }
    
    if (saveStorage == null)
    {
        saveStorage = SaveStorage.Instance; // 저장소 전역 인스턴스 참조
    }

    if (globalCharacterManager == null)
    {
        globalCharacterManager = GlobalCharacterManager.Instance; // 글로벌 캐릭터 매니저 참조
    }
}

private void Start() // 시작 시 아군 생성 및 목록 구성
{
    isFriendlyBattleSetupCompleted = false; // 아군 전투 준비 시작 상태로 설정

    SpawnFriendlyCharactersFromSaveStorage(); // 저장된 소유 캐릭터 기반으로 아군 생성

    if (globalCharacterManager == null)
    {
        globalCharacterManager = GlobalCharacterManager.Instance; // 글로벌 캐릭터 매니저 재참조
    }

    if (globalCharacterManager != null)
    {
        globalCharacterManager.RebuildCharacterList(); // 씬 캐릭터 전체 등록
    }
    
    CloseDuelSkillMenu(); // 시작 시 결투기술 목록 UI 비활성화
    CloseDuelSkillMenu(); // 시작 시 결투기술 목록 UI 비활성화
    RebuildFriendlyCharacterList(); // 아군 목록 재구성
    RefreshFriendlyAverageLevel(); // 아군 평균 레벨 계산
    
    

    isFriendlyBattleSetupCompleted = true; // 아군 생성, 목록 구성, 평균 레벨 계산 완료
}

private void Update() // 매 프레임 숫자키 선택, 상세 UI, 결투기술 선택 처리
{
    HandleNumberKeySelectionInput(); // 숫자키 선택 처리
    HandleDetailUIToggleInput(); // 상세 UI 토글 키 처리
    HandleDuelSkillMenuReleaseInput(); // 결투기술 목록 우클릭 해제 처리
}

    public bool IsFriendlyTeam(int teamNumber) // 해당 팀 번호가 아군인지 반환
    {
        return friendlyTeamNumbers.Contains(teamNumber); // 아군 팀 번호 목록 포함 여부 반환
    }

    public bool IsFriendlyCharacter(CharacterDuelAI targetCharacter) // 해당 캐릭터가 아군인지 반환
    {
        if (targetCharacter == null)
        {
            return false; // 대상이 없으면 false
        }

        return IsFriendlyTeam(targetCharacter.TeamNumber); // 팀 번호 기준 아군 판정
    }

public void RebuildFriendlyCharacterList() // 씬의 아군 캐릭터 목록 재구성
{
    friendlyCharacterEntryList.Clear(); // 기존 목록 초기화

    CharacterDuelAI[] allCharacterArray = FindObjectsByType<CharacterDuelAI>(FindObjectsSortMode.None); // 씬의 모든 캐릭터 탐색
    List<CharacterDuelAI> foundFriendlyList = new List<CharacterDuelAI>(); // 임시 아군 목록

    for (int i = 0; i < allCharacterArray.Length; i++)
    {
        CharacterDuelAI targetCharacter = allCharacterArray[i]; // 현재 검사 캐릭터 참조

        if (targetCharacter == null)
        {
            continue; // 비어 있으면 건너뜀
        }

        if (!IsFriendlyCharacter(targetCharacter))
        {
            continue; // 아군이 아니면 건너뜀
        }

        foundFriendlyList.Add(targetCharacter); // 아군 목록에 추가
    }

    foundFriendlyList.Sort(CompareFriendlyCharacterPriority); // 우선순위 기준으로 정렬

    for (int i = 0; i < foundFriendlyList.Count; i++)
    {
        CharacterDuelAI targetCharacter = foundFriendlyList[i]; // 현재 정렬된 캐릭터
        MoveCommandController targetController = targetCharacter.GetComponent<MoveCommandController>(); // 이동 명령 컨트롤러 탐색
        int assignedOrder = i + 1; // 실제 선택 순서값 1부터 부여

        targetCharacter.SetAssignedFriendlySelectionOrder(assignedOrder); // 캐릭터에 실제 순서값 저장

        FriendlyCharacterEntry newEntry = new FriendlyCharacterEntry(
            targetCharacter, // 캐릭터 저장
            targetController, // 이동 명령 컨트롤러 저장
            assignedOrder); // 실제 순서값 저장

        friendlyCharacterEntryList.Add(newEntry); // 최종 목록에 등록
    }

    OnFriendlyCharacterListRebuilt?.Invoke(); // 목록 재구성 완료 알림
}

    private int CompareFriendlyCharacterPriority(CharacterDuelAI a, CharacterDuelAI b) // 우선순위 기준 정렬 비교
    {
        if (a == null && b == null)
        {
            return 0; // 둘 다 null이면 동일
        }

        if (a == null)
        {
            return 1; // a가 null이면 뒤로
        }

        if (b == null)
        {
            return -1; // b가 null이면 앞으로
        }

        int compareResult = a.FriendlySelectionPriority.CompareTo(b.FriendlySelectionPriority); // 우선순위 기준값 비교

        if (compareResult != 0)
        {
            return compareResult; // 우선순위 차이가 있으면 그 결과 반환
        }

        compareResult = a.FirstRowID.CompareTo(b.FirstRowID); // 1열 ID 비교

        if (compareResult != 0)
        {
            return compareResult; // 1열 ID 차이가 있으면 그 결과 반환
        }

        return a.SecondRowID.CompareTo(b.SecondRowID); // 마지막으로 2열 ID 비교
    }

    private void HandleNumberKeySelectionInput() // 숫자키 입력 처리
    {
        if (Keyboard.current == null)
        {
            return; // 키보드가 없으면 종료
        }

        int checkCount = Mathf.Min(selectionKeyList.Count, friendlyCharacterEntryList.Count); // 검사 가능한 최대 개수 계산

        for (int i = 0; i < checkCount; i++)
        {
            Key targetKey = selectionKeyList[i]; // 현재 검사 키

            if (!Keyboard.current[targetKey].wasPressedThisFrame)
            {
                continue; // 이번 프레임에 눌리지 않았으면 건너뜀
            }

            SelectFriendlyCharacterByOrder(i + 1); // 해당 순서 캐릭터 선택
            return; // 한 개만 처리하고 종료
        }
    }

    private void HandleDetailUIToggleInput() // 상세 UI 토글 입력 처리
    {
        if (Keyboard.current == null)
        {
            return; // 키보드가 없으면 종료
        }

        if (!Keyboard.current[toggleDetailUIKey].wasPressedThisFrame)
        {
            return; // 토글 키 입력이 없으면 종료
        }

        ToggleDetailUIForCurrentSelectedFriendlyCharacter(); // 현재 선택된 아군 캐릭터 상세 UI 토글
    }

    public void SelectFriendlyCharacterByOrder(int targetOrder) // 실제 선택 순서값 기준 캐릭터 선택
    {
        FriendlyCharacterEntry targetEntry = GetFriendlyCharacterEntryByOrder(targetOrder); // 순서값에 맞는 캐릭터 찾기

        if (targetEntry == null)
        {
            return; // 대상이 없으면 종료
        }

        if (targetEntry.MoveCommandController == null)
        {
            return; // 이동 명령 컨트롤러가 없으면 종료
        }

        if (clickMoveSystemManager == null)
        {
            return; // 클릭 이동 시스템 매니저가 없으면 종료
        }

        clickMoveSystemManager.SelectUnitExternally(targetEntry.MoveCommandController); // 클릭 이동 시스템 매니저에 선택 전달
    }

    public void SetCurrentSelectedFriendlyCharacter(CharacterDuelAI targetCharacter) // 현재 선택된 아군 캐릭터 동기화
    {
        if (targetCharacter != null && !IsFriendlyCharacter(targetCharacter))
        {
            return; // 아군이 아니면 반영하지 않음
        }

        currentSelectedFriendlyCharacter = targetCharacter; // 현재 선택된 아군 캐릭터 저장
    }

    public void ToggleDetailUIForCurrentSelectedFriendlyCharacter() // 현재 선택된 아군 캐릭터 상세 UI 토글
    {
        ToggleDetailUIForCharacter(currentSelectedFriendlyCharacter); // 현재 선택 캐릭터 기준 토글
    }

public void ToggleDetailUIForCharacter(CharacterDuelAI targetCharacter) // 특정 캐릭터 상세 UI 토글
{
    if (targetCharacter == null)
    {
        return; // 대상이 없으면 종료
    }

    if (!IsFriendlyCharacter(targetCharacter))
    {
        return; // 아군이 아니면 종료
    }

    if (currentSpawnedDetailUIObject != null && currentDetailUIOwnerCharacter == targetCharacter)
    {
        Destroy(currentSpawnedDetailUIObject); // 같은 캐릭터의 UI가 이미 있으면 삭제
        currentSpawnedDetailUIObject = null; // 현재 생성 UI 참조 초기화
        currentDetailUIOwnerCharacter = null; // UI 소유 캐릭터 초기화
        return;
    }

    if (currentSpawnedDetailUIObject != null)
    {
        Destroy(currentSpawnedDetailUIObject); // 다른 캐릭터 UI가 있으면 먼저 삭제
        currentSpawnedDetailUIObject = null; // 현재 생성 UI 참조 초기화
        currentDetailUIOwnerCharacter = null; // UI 소유 캐릭터 초기화
    }

    GlobalCharacterDefinition matchedDefinition = FindDefinitionByCharacter(targetCharacter); // 캐릭터에 맞는 정의 탐색

    if (matchedDefinition == null)
    {
        return; // 정의가 없으면 종료
    }

    if (matchedDefinition.DetailUIPrefab == null)
    {
        return; // 상세 UI 프리팹이 없으면 종료
    }

    CharacterStatSystem targetStatSystem = targetCharacter.GetComponent<CharacterStatSystem>(); // 대상 캐릭터의 스탯 시스템 탐색

    currentSpawnedDetailUIObject = Instantiate(matchedDefinition.DetailUIPrefab, detailUIParent); // 상세 UI 생성
    currentDetailUIOwnerCharacter = targetCharacter; // 현재 UI 소유 캐릭터 저장

    CharacterDetailInfoWindow detailInfoWindow = currentSpawnedDetailUIObject.GetComponent<CharacterDetailInfoWindow>(); // 상세 UI 스크립트 탐색

    if (detailInfoWindow != null)
    {
        detailInfoWindow.Initialize(targetStatSystem); // 상세 UI에 스탯 시스템 전달
    }
}

public GlobalCharacterDefinition FindDefinitionByCharacter(CharacterDuelAI targetCharacter) // 캐릭터와 일치하는 정의 탐색
{
    if (targetCharacter == null)
    {
        return null; // 대상이 없으면 null 반환
    }

    if (characterInfoManager == null)
    {
        characterInfoManager = CharacterInfoManager.Instance; // 전역 인스턴스 재참조 시도
    }

    if (characterInfoManager == null)
    {
        characterInfoManager = FindFirstObjectByType<CharacterInfoManager>(); // 씬에서 재탐색 시도
    }

    if (characterInfoManager == null)
    {
        return null; // 정보 매니저가 없으면 종료
    }

    return characterInfoManager.FindDefinitionByCharacter(targetCharacter); // 전역 정보 매니저에 탐색 위임
}

    public FriendlyCharacterEntry GetFriendlyCharacterEntryByOrder(int targetOrder) // 실제 선택 순서값으로 목록 엔트리 탐색
    {
        for (int i = 0; i < friendlyCharacterEntryList.Count; i++)
        {
            FriendlyCharacterEntry entry = friendlyCharacterEntryList[i]; // 현재 엔트리 참조

            if (entry == null)
            {
                continue; // 비어 있으면 건너뜀
            }

            if (entry.AssignedSelectionOrder != targetOrder)
            {
                continue; // 순서값이 다르면 건너뜀
            }

            return entry; // 일치하는 엔트리 반환
        }

        return null; // 찾지 못했으면 null 반환
    }

    public bool IsCharacterCurrentlySelected(CharacterDuelAI targetCharacter) // 해당 캐릭터가 현재 선택 중인지 반환
    {
        return currentSelectedFriendlyCharacter == targetCharacter; // 현재 선택 캐릭터와 같은지 반환
    }

    public bool IsRegisteredFriendlyCharacter(CharacterDuelAI targetCharacter) // 아군 목록에 실제 등록된 캐릭터인지 확인
{
    if (targetCharacter == null)
    {
        return false; // 대상이 없으면 false
    }

    for (int i = 0; i < friendlyCharacterEntryList.Count; i++)
    {
        FriendlyCharacterEntry entry = friendlyCharacterEntryList[i]; // 현재 엔트리 참조

        if (entry == null)
        {
            continue; // 비어 있으면 건너뜀
        }

        if (entry.CharacterDuelAI == targetCharacter)
        {
            return true; // 실제 목록에 등록된 캐릭터면 true
        }
    }

    return false; // 목록에 없으면 false
}

public bool IsRegisteredFriendlyMoveCommandController(MoveCommandController targetController) // 아군 목록에 실제 등록된 이동 컨트롤러인지 확인
{
    if (targetController == null)
    {
        return false; // 대상이 없으면 false
    }

    for (int i = 0; i < friendlyCharacterEntryList.Count; i++)
    {
        FriendlyCharacterEntry entry = friendlyCharacterEntryList[i]; // 현재 엔트리 참조

        if (entry == null)
        {
            continue; // 비어 있으면 건너뜀
        }

        if (entry.MoveCommandController == targetController)
        {
            return true; // 실제 목록에 등록된 컨트롤러면 true
        }
    }

    return false; // 목록에 없으면 false
}

public void AssignStatusUIToSlot(FriendlyCharacterListSlot targetSlot) // 슬롯에 상태 UI 갱신 대상 캐릭터를 배정
{
    if (targetSlot == null)
    {
        return; // 슬롯이 없으면 종료
    }

    CharacterDuelAI targetCharacter = targetSlot.TargetCharacter; // 슬롯이 담당하는 캐릭터 참조

    if (targetCharacter == null)
    {
        targetSlot.BindStatusUI(null); // 캐릭터가 없으면 UI 연결 해제
        return;
    }

    CharacterStatSystem targetStatSystem = targetCharacter.GetComponent<CharacterStatSystem>(); // 대상 캐릭터의 스탯 시스템 탐색
    targetSlot.BindStatusUI(targetStatSystem); // 슬롯의 상태 UI에 스탯 시스템 배정
}

private void SpawnFriendlyCharactersFromSaveStorage() // 저장된 소유 캐릭터 목록 기반 아군 생성
{
    if (saveStorage == null)
    {
        saveStorage = SaveStorage.Instance; // 저장소 재참조
    }

    if (saveStorage == null)
    {
        return; // 저장소가 없으면 종료
    }

    if (characterInfoManager == null)
    {
        characterInfoManager = CharacterInfoManager.Instance; // 캐릭터 정보 매니저 재참조
    }

    if (characterInfoManager == null)
    {
        return; // 캐릭터 정보 매니저가 없으면 종료
    }

    IReadOnlyList<SaveStorage.OwnedCharacterData> ownedCharacterList = saveStorage.CurrentOwnedCharacterList; // 현재 소유 캐릭터 목록

    for (int i = 0; i < ownedCharacterList.Count; i++)
    {
        SaveStorage.OwnedCharacterData ownedCharacter = ownedCharacterList[i]; // 현재 소유 캐릭터 데이터

        if (ownedCharacter == null)
        {
            continue; // 비어 있으면 건너뜀
        }

        GlobalCharacterDefinition definition = characterInfoManager.FindDefinitionByID(
            ownedCharacter.firstRowID,
            ownedCharacter.secondRowID); // 캐릭터 정의 탐색

        if (definition == null || definition.InGameCharacterPrefab == null)
        {
            continue; // 정의 또는 프리팹이 없으면 건너뜀
        }

        Vector3 spawnPosition = GetRandomFriendlySpawnPosition(); // 랜덤 생성 위치 계산

        GameObject spawnedObject = Instantiate(
            definition.InGameCharacterPrefab,
            spawnPosition,
            Quaternion.identity); // 아군 캐릭터 생성

        CharacterDuelAI duelAI = spawnedObject.GetComponent<CharacterDuelAI>(); // 결투 AI 참조

        if (duelAI != null)
        {
            duelAI.SetCharacterIDs(
                ownedCharacter.firstRowID,
                ownedCharacter.secondRowID,
                ownedCharacter.individualID); // 저장된 식별 ID 적용
        }

        ApplySavedLevelToFriendlyCharacter(spawnedObject, ownedCharacter); // 저장된 레벨 적용
    }
}

private Vector3 GetRandomFriendlySpawnPosition() // 아군 생성 랜덤 위치 계산
{
    Vector3 centerPosition = friendlySpawnCenter != null ? friendlySpawnCenter.position : transform.position; // 기준 위치
    Vector2 randomCircle = Random.insideUnitCircle * Mathf.Max(0f, friendlySpawnRadius); // 원형 범위 랜덤값

    return centerPosition + new Vector3(randomCircle.x, randomCircle.y, 0f); // XY 평면 기준 위치 반환
}

private void ApplySavedLevelToFriendlyCharacter(GameObject characterObject, SaveStorage.OwnedCharacterData ownedCharacter) // 저장 레벨 적용
{
    if (characterObject == null || ownedCharacter == null)
    {
        return; // 대상이 없으면 종료
    }

    CharacterStatSystem statSystem = characterObject.GetComponent<CharacterStatSystem>(); // 스탯 시스템 참조

    if (statSystem == null)
    {
        return; // 스탯 시스템이 없으면 종료
    }

    IReadOnlyList<SaveStorage.OwnedCharacterStatData> statList = saveStorage.CurrentOwnedCharacterStatList; // 저장된 스탯 목록

    for (int i = 0; i < statList.Count; i++)
    {
        SaveStorage.OwnedCharacterStatData statData = statList[i]; // 현재 스탯 데이터

        if (statData == null)
        {
            continue; // 비어 있으면 건너뜀
        }

        if (statData.firstRowID != ownedCharacter.firstRowID)
        {
            continue; // 1열 ID가 다르면 건너뜀
        }

        if (statData.secondRowID != ownedCharacter.secondRowID)
        {
            continue; // 2열 ID가 다르면 건너뜀
        }

        if (statData.individualID != ownedCharacter.individualID)
        {
            continue; // 개체별 ID가 다르면 건너뜀
        }

        statSystem.SetLevelStats(statData.levelstats); // 저장된 레벨 적용
        return; // 적용 완료 후 종료
    }
}

public void RefreshFriendlyAverageLevel() // 아군 평균 레벨 계산
{
    int totalLevel = 0; // 레벨 합계
    int validCount = 0; // 유효 캐릭터 수

    for (int i = 0; i < friendlyCharacterEntryList.Count; i++)
    {
        FriendlyCharacterEntry entry = friendlyCharacterEntryList[i]; // 현재 아군 정보

        if (entry == null || entry.CharacterDuelAI == null)
        {
            continue; // 비어 있으면 건너뜀
        }

        CharacterStatSystem statSystem = entry.CharacterDuelAI.GetComponent<CharacterStatSystem>(); // 스탯 참조

        if (statSystem == null)
        {
            continue; // 스탯이 없으면 건너뜀
        }

        totalLevel += statSystem.LevelStats; // 레벨 합산
        validCount++; // 유효 수 증가
    }

    friendlyAverageLevel = validCount > 0 ? Mathf.Max(1, totalLevel / validCount) : 1; // 평균 레벨 저장
}

public void OpenDuelSkillMenu(CharacterDuelAI targetCharacter, Vector3 targetWorldPosition) // 결투기술 목록 열기
{
    if (targetCharacter == null)
    {
        return; // 대상 캐릭터가 없으면 종료
    }

    if (duelSkillListParentPanel == null || duelSkillSlotListPanel == null)
    {
        return; // UI 참조가 없으면 종료
    }

    currentDuelSkillMenuOwner = targetCharacter; // 현재 목록 소유 캐릭터 저장
    currentHoveredDuelSkillSlot = null; // 기존 호버 슬롯 초기화

    ClearDuelSkillSlots(); // 기존 슬롯 삭제

    duelSkillListParentPanel.transform.position = targetWorldPosition; // 호버 이미지 위치로 패널 이동
    duelSkillListParentPanel.SetActive(true); // 부모 패널 활성화

    IReadOnlyList<DuelSkillDefinitionSO> skillList = targetCharacter.DuelSkillList; // 캐릭터 보유 결투기술 목록 가져오기

    if (skillList == null)
    {
        return; // 기술 목록이 없으면 종료
    }

    for (int i = 0; i < skillList.Count; i++)
    {
        DuelSkillDefinitionSO skill = skillList[i]; // 현재 생성할 결투기술

        if (skill == null || skill.DuelSkillSlotPrefab == null)
        {
            continue; // 기술 또는 프리팹이 없으면 건너뜀
        }

        DuelSkillSlot spawnedSlot = Instantiate(skill.DuelSkillSlotPrefab, duelSkillSlotListPanel); // 슬롯 생성
        spawnedSlot.Initialize(this, skill, i); // 슬롯 초기화
        spawnedDuelSkillSlotList.Add(spawnedSlot); // 생성 목록에 등록
    }
}

public void SetHoveredDuelSkillSlot(DuelSkillSlot targetSlot) // 현재 호버 중인 결투기술 슬롯 설정
{
    currentHoveredDuelSkillSlot = targetSlot; // 호버 슬롯 저장
}

public void ClearHoveredDuelSkillSlot(DuelSkillSlot targetSlot) // 현재 호버 중인 결투기술 슬롯 해제
{
    if (currentHoveredDuelSkillSlot != targetSlot)
    {
        return; // 다른 슬롯이면 무시
    }

    currentHoveredDuelSkillSlot = null; // 호버 슬롯 초기화
}

private void HandleDuelSkillMenuReleaseInput() // 우클릭 해제 시 결투기술 선택 또는 취소
{
    if (Mouse.current == null)
    {
        return; // 마우스가 없으면 종료
    }

    if (duelSkillListParentPanel == null || !duelSkillListParentPanel.activeSelf)
    {
        return; // 목록 UI가 열려있지 않으면 종료
    }

    if (!Mouse.current.rightButton.wasReleasedThisFrame)
    {
        return; // 우클릭 해제 프레임이 아니면 종료
    }

    DuelSkillSlot releasedSlot = FindDuelSkillSlotUnderMouse(); // 해제 위치의 슬롯 탐색

if (releasedSlot != null && currentDuelSkillMenuOwner != null)
{
    if (releasedSlot.SlotType == DuelSkillSlot.DuelSkillSlotType.AttackSkill)
    {
        currentDuelSkillMenuOwner.SetCurrentSelectedAttackSkill(releasedSlot.AttackSkillDefinition); // 공격기술 선택
    }
    else
    {
        currentDuelSkillMenuOwner.SetCurrentSelectedDuelSkill(releasedSlot.DuelSkillDefinition); // 결투기술 선택
    }
}

    CloseDuelSkillMenu(); // 슬롯 위든 아니든 패널 닫기
}

private DuelSkillSlot FindDuelSkillSlotUnderMouse() // 현재 마우스 위치 아래의 결투기술 슬롯 탐색
{
    if (Mouse.current == null)
    {
        return null; // 마우스가 없으면 null
    }

    Vector2 mousePosition = Mouse.current.position.ReadValue(); // 현재 마우스 위치

    for (int i = 0; i < spawnedDuelSkillSlotList.Count; i++)
    {
        DuelSkillSlot slot = spawnedDuelSkillSlotList[i]; // 검사할 슬롯

        if (slot == null)
        {
            continue; // 슬롯이 없으면 건너뜀
        }

        if (slot.IsScreenPointInsideSlot(mousePosition))
        {
            return slot; // 마우스 위치 안에 있는 슬롯 반환
        }
    }

    return currentHoveredDuelSkillSlot; // Rect 판정 실패 시 기존 호버 슬롯 보조 사용
}

private void CloseDuelSkillMenu() // 결투기술 목록 닫기
{
    ClearDuelSkillSlots(); // 생성 슬롯 삭제

    currentDuelSkillMenuOwner = null; // 목록 소유 캐릭터 초기화
    currentHoveredDuelSkillSlot = null; // 호버 슬롯 초기화

    if (duelSkillListParentPanel != null)
    {
        duelSkillListParentPanel.SetActive(false); // 부모 패널 비활성화
    }
}

private void ClearDuelSkillSlots() // 생성된 결투기술 슬롯 삭제
{
    for (int i = 0; i < spawnedDuelSkillSlotList.Count; i++)
    {
        if (spawnedDuelSkillSlotList[i] == null)
        {
            continue; // 이미 삭제된 슬롯은 건너뜀
        }

        Destroy(spawnedDuelSkillSlotList[i].gameObject); // 슬롯 오브젝트 삭제
    }

    spawnedDuelSkillSlotList.Clear(); // 생성 슬롯 목록 초기화
}







}