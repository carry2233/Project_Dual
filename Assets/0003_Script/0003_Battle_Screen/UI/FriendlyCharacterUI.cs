using UnityEngine;

public class FriendlyCharacterUI : MonoBehaviour
{
    [Header("필수 참조")]
    [SerializeField] private FriendlyCharacterManager friendlyCharacterManager; // 아군 캐릭터 매니저 참조
    [SerializeField] private Transform gridLayoutRoot; // Grid Layout Group이 적용된 부모 오브젝트

    [SerializeField] private bool autoBuildOnStart = false; // Start에서 즉시 생성할지 여부

    [Header("현재 생성 상태")]
    [SerializeField] private int createdSlotCount; // 생성된 슬롯 개수

private void Awake() // 시작 시 참조 자동 연결
{
    if (friendlyCharacterManager == null)
    {
        friendlyCharacterManager = FindFirstObjectByType<FriendlyCharacterManager>(); // 씬에서 자동 탐색
    }

    if (gridLayoutRoot == null)
    {
        gridLayoutRoot = transform; // 루트가 없으면 자기 자신 사용
    }
}

private void Start() // 시작 시 필요하면 직접 생성 시도
{
    if (!autoBuildOnStart)
    {
        return; // 자동 생성 사용 안 하면 종료
    }

    BuildFriendlyCharacterListUI(); // 필요 시 직접 생성
}

public void BuildFriendlyCharacterListUI() // 아군 캐릭터 목록 UI 생성
{
    ClearExistingSlots(); // 기존 슬롯 제거

    if (friendlyCharacterManager == null)
    {
        return; // 매니저가 없으면 종료
    }

    var entryList = friendlyCharacterManager.FriendlyCharacterEntryList; // 아군 캐릭터 목록 참조

    for (int i = 0; i < entryList.Count; i++)
    {
        FriendlyCharacterManager.FriendlyCharacterEntry entry = entryList[i]; // 현재 엔트리 참조

        if (entry == null)
        {
            continue; // 비어 있으면 건너뜀
        }

        CharacterDuelAI targetCharacter = entry.CharacterDuelAI; // 현재 캐릭터 참조

        if (targetCharacter == null)
        {
            continue; // 캐릭터가 없으면 건너뜀
        }

        GlobalCharacterDefinition matchedDefinition = friendlyCharacterManager.FindDefinitionByCharacter(targetCharacter); // 캐릭터 정의 탐색

        if (matchedDefinition == null)
        {
            continue; // 정의가 없으면 건너뜀
        }

        if (matchedDefinition.BattleCharacterListUIPrefab == null)
        {
            continue; // 목록용 프리팹이 없으면 건너뜀
        }

        GameObject createdObject = Instantiate(matchedDefinition.BattleCharacterListUIPrefab, gridLayoutRoot); // 슬롯 프리팹 생성
        FriendlyCharacterListSlot slot = createdObject.GetComponent<FriendlyCharacterListSlot>(); // 슬롯 스크립트 참조

        if (slot != null)
        {
            slot.InitializeSlot(
                friendlyCharacterManager, // 아군 캐릭터 매니저 전달
                matchedDefinition, // 캐릭터 정의 전달
                targetCharacter, // 연결 캐릭터 전달
                entry.AssignedSelectionOrder); // 실제 선택 순서값 전달
        }

        createdSlotCount++; // 생성 개수 증가
    }
}

    private void ClearExistingSlots() // 기존 목록 슬롯 제거
    {
        createdSlotCount = 0; // 생성 개수 초기화

        if (gridLayoutRoot == null)
        {
            return; // 루트가 없으면 종료
        }

        for (int i = gridLayoutRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = gridLayoutRoot.GetChild(i); // 현재 자식 오브젝트 참조

            if (child == null)
            {
                continue; // 비어 있으면 건너뜀
            }

            Destroy(child.gameObject); // 기존 슬롯 제거
        }
    }

    private void OnEnable() // 활성화 시 목록 재구성 완료 이벤트 구독
{
    if (friendlyCharacterManager == null)
    {
        friendlyCharacterManager = FindFirstObjectByType<FriendlyCharacterManager>(); // 씬에서 자동 탐색
    }

    if (friendlyCharacterManager != null)
    {
        friendlyCharacterManager.OnFriendlyCharacterListRebuilt += BuildFriendlyCharacterListUI; // 아군 목록 재구성 완료 시 UI 생성 연결
    }
}

private void OnDisable() // 비활성화 시 이벤트 구독 해제
{
    if (friendlyCharacterManager != null)
    {
        friendlyCharacterManager.OnFriendlyCharacterListRebuilt -= BuildFriendlyCharacterListUI; // 이벤트 구독 해제
    }
}
}