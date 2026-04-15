using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System; // 이벤트 Action 사용

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

    [Header("캐릭터 전역 정의 목록")]
    [SerializeField] private List<GlobalCharacterDefinition> globalCharacterDefinitionList = new List<GlobalCharacterDefinition>(); // 캐릭터 전역 정의 목록

    [Header("현재 아군 캐릭터 목록")]
    [SerializeField] private List<FriendlyCharacterEntry> friendlyCharacterEntryList = new List<FriendlyCharacterEntry>(); // 정렬 후 아군 캐릭터 목록

    [Header("현재 선택 상태")]
    [SerializeField] private CharacterDuelAI currentSelectedFriendlyCharacter; // 현재 선택된 아군 캐릭터
    [SerializeField] private GameObject currentSpawnedDetailUIObject; // 현재 생성된 상세 UI 오브젝트
    [SerializeField] private CharacterDuelAI currentDetailUIOwnerCharacter; // 현재 상세 UI를 소유한 캐릭터

    public event Action OnFriendlyCharacterListRebuilt; // 아군 목록 재구성 완료 알림 이벤트

    public IReadOnlyList<FriendlyCharacterEntry> FriendlyCharacterEntryList => friendlyCharacterEntryList; // 아군 목록 반환
    public CharacterDuelAI CurrentSelectedFriendlyCharacter => currentSelectedFriendlyCharacter; // 현재 선택된 아군 캐릭터 반환
    public IReadOnlyList<GlobalCharacterDefinition> GlobalCharacterDefinitionList => globalCharacterDefinitionList; // 전역 정의 목록 반환
    public Key ToggleDetailUIKey => toggleDetailUIKey; // 상세 UI 토글 키 반환

    private void Awake() // 시작 시 참조 자동 연결
    {
        if (clickMoveSystemManager == null)
        {
            clickMoveSystemManager = FindFirstObjectByType<ClickMoveSystemManager>(); // 씬에서 자동 탐색
        }

        if (detailUIParent == null)
        {
            detailUIParent = transform; // 부모가 없으면 자기 자신 사용
        }
    }

private void Start() // 시작 시 아군 목록 구성
{
    RebuildFriendlyCharacterList(); // 아군 목록 재구성
}

    private void Update() // 매 프레임 숫자키 선택 및 상세 UI 토글 처리
    {
        HandleNumberKeySelectionInput(); // 숫자키 선택 처리
        HandleDetailUIToggleInput(); // 상세 UI 토글 키 처리
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

        currentSpawnedDetailUIObject = Instantiate(matchedDefinition.DetailUIPrefab, detailUIParent); // 상세 UI 생성
        currentDetailUIOwnerCharacter = targetCharacter; // 현재 UI 소유 캐릭터 저장
    }

    public GlobalCharacterDefinition FindDefinitionByCharacter(CharacterDuelAI targetCharacter) // 캐릭터와 일치하는 정의 탐색
    {
        if (targetCharacter == null)
        {
            return null; // 대상이 없으면 null 반환
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
}