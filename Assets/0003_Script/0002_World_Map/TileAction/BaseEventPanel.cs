using System.Collections.Generic; // List 사용
using System.Linq; // 정렬 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.UI; // Button 사용
using UnityEngine.SceneManagement; // 씬 이동 사용

/// <summary>
/// 이벤트 발생 판정, 이벤트 UI 표시, 비주얼 생성, 공용 버튼 제어를 담당하는 패널입니다.
/// </summary>
public class BaseEventPanel : MonoBehaviour
{
    [System.Serializable]
    public class EventButtonEntry
    {
        [Header("버튼 정보")]
        public int buttonID; // 버튼 ID
        public Button targetButton; // 담당 버튼
    }

    [Header("이벤트 매니저")]
    [SerializeField] private EventInfoManager eventInfoManager; // 이벤트 정보 관리자

    [Header("이벤트 UI")]
    [SerializeField] private GameObject eventUIParent; // 이벤트 UI 부모 오브젝트
    [SerializeField] private Transform eventVisualParent; // 이벤트 비주얼 프리팹 생성 부모
    [SerializeField] private Transform rewardSlotContent; // 획득 아이템 슬롯 생성 부모

    [Header("이벤트 발생 설정")]
    [Range(0f, 100f)]
    [SerializeField] private float eventTriggerChancePercent = 50f; // 탐색 종료 시 이벤트 발생 확률

    [Header("버튼 리스트")]
    [SerializeField] private List<EventButtonEntry> eventButtonList = new List<EventButtonEntry>(); // 버튼 제어 리스트

    [Header("이벤트 기능 스크립트")]
    [SerializeField] private List<ItemRewardEvent> itemRewardEventList = new List<ItemRewardEvent>(); // 아이템 획득 이벤트 리스트

    [Header("전투 이벤트 설정")]
[SerializeField] private List<BattleOccurrenceEvent> battleOccurrenceEventList = new List<BattleOccurrenceEvent>(); // 전투 발생 이벤트 리스트
[SerializeField] private string battleSceneName; // 전투 이벤트 버튼 클릭 시 이동할 씬 이름
[SerializeField] private SaveStorage saveStorage; // 전투 데이터 전달용 저장소

    private GameObject currentEventVisualInstance; // 현재 생성된 이벤트 비주얼 인스턴스

    private void Start() // 시작 시 참조 보정
    {
        if (eventInfoManager == null)
            eventInfoManager = EventInfoManager.Instance; // 전역 이벤트 매니저 참조

        if (saveStorage == null)
        {
            saveStorage = SaveStorage.Instance; // 저장소 전역 인스턴스 참조
        }
    }

    public void HideEventUI() // 이벤트 UI 비활성화
    {
        ClearCurrentEventVisual(); // 현재 이벤트 비주얼 제거
        ClearRewardSlots(); // 보상 슬롯 제거

        if (eventUIParent != null)
            eventUIParent.SetActive(false); // 이벤트 UI 부모 비활성화
    }

    public void TryTriggerRandomEvent() // 이벤트 발생 시도
    {
        if (eventInfoManager == null)
            eventInfoManager = EventInfoManager.Instance; // 전역 이벤트 매니저 재참조

        if (eventInfoManager == null)
            return;

        float randomValue = Random.Range(0f, 100f); // 이벤트 발생 판정용 랜덤값

        if (randomValue > eventTriggerChancePercent)
            return;

        EventInfoDefinition selectedEvent = SelectWeightedRandomEvent(); // 가중치 기반 이벤트 선택

        if (selectedEvent == null)
            return;

        ExecuteEvent(selectedEvent); // 선택된 이벤트 실행
    }

    private EventInfoDefinition SelectWeightedRandomEvent() // 가중치 기반 이벤트 선택
    {
        List<EventInfoDefinition> validEvents = eventInfoManager.eventInfoDefinitionList
            .Where(eventInfo => eventInfo != null && eventInfo.eventWeight > 0)
            .ToList();

        if (validEvents.Count <= 0)
            return null;

        int totalWeight = 0; // 전체 가중치

        for (int i = 0; i < validEvents.Count; i++)
        {
            totalWeight += Mathf.Max(0, validEvents[i].eventWeight); // 유효 가중치 합산
        }

        int randomWeight = Random.Range(1, totalWeight + 1); // 선택용 랜덤 가중치
        int currentWeight = 0; // 누적 가중치

        for (int i = 0; i < validEvents.Count; i++)
        {
            currentWeight += validEvents[i].eventWeight; // 누적 가중치 증가

            if (randomWeight <= currentWeight)
                return validEvents[i];
        }

        return validEvents[validEvents.Count - 1];
    }

    private void ExecuteEvent(EventInfoDefinition selectedEvent) // 이벤트 실행
    {
        if (eventUIParent != null)
            eventUIParent.SetActive(true); // 이벤트 UI 부모 활성화

        ClearCurrentEventVisual(); // 이전 이벤트 비주얼 제거
        ClearRewardSlots(); // 이전 보상 슬롯 제거
        SpawnEventVisual(selectedEvent); // 이벤트 비주얼 생성

        ItemRewardEvent rewardEvent = FindItemRewardEvent(selectedEvent.eventID); // 아이템 획득 이벤트 탐색

        if (rewardEvent != null)
        {
            rewardEvent.ExecuteRewardEvent(this, selectedEvent); // 아이템 획득 이벤트 실행
        }

        BattleOccurrenceEvent battleEvent = FindBattleOccurrenceEvent(selectedEvent.eventID); // 전투 발생 이벤트 탐색

        if (battleEvent != null)
        {
            CreateBattleEventSlot(battleEvent); // 전투 이벤트 슬롯 생성
        }
    }

    private void SpawnEventVisual(EventInfoDefinition selectedEvent) // 이벤트 비주얼 생성
    {
        if (selectedEvent == null || selectedEvent.eventVisualPrefab == null || eventVisualParent == null)
            return;

        currentEventVisualInstance = Instantiate(selectedEvent.eventVisualPrefab, eventVisualParent); // 비주얼 프리팹 생성
    }

    private ItemRewardEvent FindItemRewardEvent(int eventID) // 이벤트 ID에 맞는 획득 이벤트 찾기
    {
        for (int i = 0; i < itemRewardEventList.Count; i++)
        {
            ItemRewardEvent rewardEvent = itemRewardEventList[i];

            if (rewardEvent == null)
                continue;

            if (rewardEvent.CanHandleEvent(eventID))
                return rewardEvent;
        }

        return null;
    }

public void CreateRewardSlots(List<ItemRewardEvent.RewardItemResult> rewardResults) // 획득 아이템 슬롯 생성
{
    if (rewardSlotContent == null || rewardResults == null)
        return;

    ClearRewardSlots(); // 기존 슬롯 제거

    List<ItemRewardEvent.RewardItemResult> sortedResults = rewardResults
        .Where(result => result != null && result.itemDefinition != null && result.itemCount > 0)
        .OrderBy(result => result.itemDefinition.displayPriority)
        .ToList();

    int createdSlotCount = 0; // 실제 생성된 슬롯 수

    for (int i = 0; i < sortedResults.Count; i++)
    {
        ItemRewardEvent.RewardItemResult result = sortedResults[i]; // 현재 보상 결과

        if (result.itemDefinition.rewardItemDisplaySlotPrefab == null)
            continue;

        GameObject slotObject = Instantiate(result.itemDefinition.rewardItemDisplaySlotPrefab, rewardSlotContent); // 슬롯 프리팹 생성
        EventSlot eventSlot = slotObject.GetComponent<EventSlot>(); // 이벤트 슬롯 컴포넌트 참조

        if (eventSlot != null)
        {
            eventSlot.SetSlot(result.itemDefinition, result.itemCount); // 슬롯 표시 갱신
        }

        createdSlotCount++; // 실제 생성된 슬롯 수 증가
    }

    RefreshRewardSlotContentHeight(createdSlotCount); // 생성된 슬롯 수 기준으로 Content 높이 갱신
}

    public void SetButtonInteractable(int buttonID, bool isInteractable) // 버튼 상호작용 상태 변경
    {
        for (int i = 0; i < eventButtonList.Count; i++)
        {
            EventButtonEntry entry = eventButtonList[i];

            if (entry == null || entry.targetButton == null)
                continue;

            if (entry.buttonID != buttonID)
                continue;

            entry.targetButton.interactable = isInteractable; // 버튼 상호작용 상태 적용
            return;
        }
    }

    private void ClearCurrentEventVisual() // 현재 이벤트 비주얼 제거
    {
        if (currentEventVisualInstance == null)
            return;

        Destroy(currentEventVisualInstance); // 생성된 비주얼 제거
        currentEventVisualInstance = null; // 참조 초기화
    }

private void ClearRewardSlots() // 획득 아이템 슬롯 전체 제거
{
    if (rewardSlotContent == null)
        return;

    for (int i = rewardSlotContent.childCount - 1; i >= 0; i--)
    {
        Destroy(rewardSlotContent.GetChild(i).gameObject); // 자식 슬롯 제거
    }

    RefreshRewardSlotContentHeight(0); // 슬롯 제거 후 Content 높이 초기화
}

    private void RefreshRewardSlotContentHeight(int createdSlotCount) // 보상 슬롯 Content 높이 갱신
{
    if (rewardSlotContent == null)
        return;

    RectTransform contentRectTransform = rewardSlotContent as RectTransform; // Content RectTransform 참조

    if (contentRectTransform == null)
        return;

    GridLayoutGroup gridLayoutGroup = rewardSlotContent.GetComponent<GridLayoutGroup>(); // Grid Layout Group 참조

    if (gridLayoutGroup == null)
        return;

    int slotCount = Mathf.Max(0, createdSlotCount); // 음수 방지
    float cellHeight = gridLayoutGroup.cellSize.y; // 셀 Y 크기
    float spacingY = gridLayoutGroup.spacing.y; // Y축 간격

    float contentHeight = 0f; // 최종 Content 높이

    if (slotCount > 0)
    {
        contentHeight = (slotCount * cellHeight) + (spacingY * (slotCount - 1)); // 슬롯 수 기준 높이 계산
    }

    Vector2 sizeDelta = contentRectTransform.sizeDelta; // 현재 Content 크기
    sizeDelta.y = contentHeight; // 계산된 높이 적용
    contentRectTransform.sizeDelta = sizeDelta; // Content 크기 반영
}

private BattleOccurrenceEvent FindBattleOccurrenceEvent(int eventID) // 이벤트 ID에 맞는 전투 이벤트 찾기
{
    for (int i = 0; i < battleOccurrenceEventList.Count; i++)
    {
        BattleOccurrenceEvent battleEvent = battleOccurrenceEventList[i]; // 현재 전투 이벤트

        if (battleEvent == null)
        {
            continue; // 비어 있으면 건너뜀
        }

        if (battleEvent.CanHandleEvent(eventID))
        {
            return battleEvent; // 처리 가능한 전투 이벤트 반환
        }
    }

    return null; // 찾지 못하면 null 반환
}

private void CreateBattleEventSlot(BattleOccurrenceEvent battleEvent) // 전투 이벤트 슬롯 생성
{
    CreateBattleEventSlot(battleEvent, true, true); // 기본 전투 이벤트 슬롯 생성
}

private void CreateBattleEventSlot(
    BattleOccurrenceEvent battleEvent,
    bool clearBeforeCreate,
    bool isInteractable) // 전투 이벤트 슬롯 생성
{
    if (battleEvent == null || battleEvent.EventSlotPrefab == null || rewardSlotContent == null)
    {
        return; // 필수값이 없으면 종료
    }

    if (clearBeforeCreate == true)
    {
        ClearRewardSlots(); // 필요할 때만 기존 슬롯 제거
    }

    EventSlot eventSlot = Instantiate(battleEvent.EventSlotPrefab, rewardSlotContent); // 슬롯 프리팹 그대로 생성

    Button slotButton = eventSlot.GetComponentInChildren<Button>(); // 슬롯 내부 버튼 탐색

    if (slotButton != null)
    {
        slotButton.onClick.RemoveAllListeners(); // 기존 클릭 이벤트 제거
        slotButton.interactable = isInteractable; // 버튼 상호작용 여부 설정

        if (isInteractable == true)
        {
            slotButton.onClick.AddListener(() => OnBattleEventSlotClicked(battleEvent)); // 전투 이벤트 클릭 연결
        }
    }

    RefreshRewardSlotContentHeight(rewardSlotContent.childCount); // 현재 슬롯 수 기준 높이 갱신
}

private void OnBattleEventSlotClicked(BattleOccurrenceEvent battleEvent) // 전투 이벤트 슬롯 클릭 처리
{
    if (battleEvent == null)
    {
        return; // 이벤트가 없으면 종료
    }

    if (saveStorage == null)
    {
        saveStorage = SaveStorage.Instance; // 저장소 재참조
    }

    if (saveStorage != null)
    {
        saveStorage.StoreBattleEventRuntimeData(battleEvent); // 전투 이벤트 데이터 저장
    }

    if (!string.IsNullOrEmpty(battleSceneName))
    {
        SceneManager.LoadScene(battleSceneName); // 전투 씬 이동
    }
}

public void TryShowReturnedBattleEventResult() // 전투 복귀 후 직전 전투 이벤트 결과 UI 표시 시도
{
    if (saveStorage == null)
    {
        saveStorage = SaveStorage.Instance; // 저장소 재참조
    }

    if (saveStorage == null || saveStorage.HasExecutedBattle == false)
    {
        return; // 전투 수행 상태가 아니면 종료
    }

    int eventID = saveStorage.LastExecutedEventID; // 직전 실행 이벤트 ID 가져오기

    EventInfoDefinition eventInfoDefinition = FindEventInfoDefinition(eventID); // 이벤트 정보 찾기
    BattleOccurrenceEvent battleEvent = FindBattleOccurrenceEvent(eventID); // 전투 이벤트 찾기

    if (eventInfoDefinition == null || battleEvent == null)
    {
        saveStorage.ClearBattleReturnState(); // 잘못된 상태면 초기화
        return;
    }

    ShowReturnedBattleEventResult(eventInfoDefinition, battleEvent); // 전투 복귀 결과 UI 표시
    saveStorage.ClearBattleReturnState(); // 한 번 표시 후 상태 초기화
}

private void ShowReturnedBattleEventResult(EventInfoDefinition eventInfoDefinition, BattleOccurrenceEvent battleEvent) // 전투 복귀 결과 UI 표시
{
    if (eventUIParent != null)
        eventUIParent.SetActive(true); // 이벤트 UI 부모 활성화

    ClearCurrentEventVisual(); // 이전 이벤트 비주얼 제거
    ClearRewardSlots(); // 이전 슬롯 제거

    SpawnAfterBattleEventVisual(battleEvent); // 전투 이후 전용 이벤트 비주얼 생성
    CreateBattleEventSlot(battleEvent, true, false); // 전투 슬롯 생성, 버튼 상호작용 비활성화

    if (battleEvent.ExecuteItemRewardAfterBattle == false)
    {
        return; // 전투 후 아이템 획득이 꺼져 있으면 종료
    }

    if (battleEvent.PostBattleItemRewardEvent == null)
    {
        return; // 연결된 아이템 획득 이벤트가 없으면 종료
    }

    battleEvent.PostBattleItemRewardEvent.ExecuteRewardEventWithoutClearingSlots(this); // 전투 슬롯 유지한 채 아이템 획득 실행
}

private EventInfoDefinition FindEventInfoDefinition(int eventID) // 이벤트 ID에 맞는 이벤트 정보 찾기
{
    if (eventInfoManager == null)
        eventInfoManager = EventInfoManager.Instance; // 이벤트 매니저 재참조

    if (eventInfoManager == null || eventInfoManager.eventInfoDefinitionList == null)
        return null; // 이벤트 목록이 없으면 null 반환

    for (int i = 0; i < eventInfoManager.eventInfoDefinitionList.Count; i++)
    {
        EventInfoDefinition eventInfoDefinition = eventInfoManager.eventInfoDefinitionList[i]; // 현재 이벤트 정보

        if (eventInfoDefinition == null)
            continue; // 비어 있으면 건너뜀

        if (eventInfoDefinition.eventID == eventID)
            return eventInfoDefinition; // ID가 같으면 반환
    }

    return null; // 찾지 못하면 null 반환
}

public void AppendRewardSlots(List<ItemRewardEvent.RewardItemResult> rewardResults) // 기존 슬롯 유지 후 획득 아이템 슬롯 추가
{
    if (rewardSlotContent == null || rewardResults == null)
        return; // 필수값이 없으면 종료

    List<ItemRewardEvent.RewardItemResult> sortedResults = rewardResults
        .Where(result => result != null && result.itemDefinition != null && result.itemCount > 0)
        .OrderBy(result => result.itemDefinition.displayPriority)
        .ToList(); // 표시 우선순위 기준 정렬

    for (int i = 0; i < sortedResults.Count; i++)
    {
        ItemRewardEvent.RewardItemResult result = sortedResults[i]; // 현재 보상 결과

        if (result.itemDefinition.rewardItemDisplaySlotPrefab == null)
            continue; // 슬롯 프리팹이 없으면 건너뜀

        GameObject slotObject = Instantiate(result.itemDefinition.rewardItemDisplaySlotPrefab, rewardSlotContent); // 슬롯 프리팹 생성
        EventSlot eventSlot = slotObject.GetComponent<EventSlot>(); // 이벤트 슬롯 컴포넌트 참조

        if (eventSlot != null)
        {
            eventSlot.SetSlot(result.itemDefinition, result.itemCount); // 슬롯 표시 갱신
        }
    }

    RefreshRewardSlotContentHeight(rewardSlotContent.childCount); // 현재 전체 슬롯 수 기준 높이 갱신
}

private void SpawnAfterBattleEventVisual(BattleOccurrenceEvent battleEvent) // 전투 이후 이벤트 비주얼 생성
{
    if (battleEvent == null || battleEvent.AfterBattleEventVisualPrefab == null || eventVisualParent == null)
        return; // 필수값이 없으면 종료

    currentEventVisualInstance = Instantiate(
        battleEvent.AfterBattleEventVisualPrefab,
        eventVisualParent); // 전투 이후 전용 비주얼 프리팹 생성
}

public void StartBattleEventDirectly(BattleOccurrenceEvent battleEvent) // 전투 이벤트를 슬롯 클릭 없이 즉시 실행
{
    OnBattleEventSlotClicked(battleEvent); // 기존 전투 이벤트 실행 흐름 재사용
}









}