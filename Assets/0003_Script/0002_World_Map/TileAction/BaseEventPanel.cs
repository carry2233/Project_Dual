using System.Collections.Generic; // List 사용
using System.Linq; // 정렬 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.UI; // Button 사용

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

    private GameObject currentEventVisualInstance; // 현재 생성된 이벤트 비주얼 인스턴스

    private void Start() // 시작 시 참조 보정
    {
        if (eventInfoManager == null)
            eventInfoManager = EventInfoManager.Instance; // 전역 이벤트 매니저 참조
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

        for (int i = 0; i < sortedResults.Count; i++)
        {
            ItemRewardEvent.RewardItemResult result = sortedResults[i];

            if (result.itemDefinition.rewardItemDisplaySlotPrefab == null)
                continue;

            GameObject slotObject = Instantiate(result.itemDefinition.rewardItemDisplaySlotPrefab, rewardSlotContent); // 슬롯 프리팹 생성
            EventSlot eventSlot = slotObject.GetComponent<EventSlot>(); // 이벤트 슬롯 컴포넌트 참조

            if (eventSlot != null)
            {
                eventSlot.SetSlot(result.itemDefinition, result.itemCount); // 슬롯 표시 갱신
            }
        }
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
    }
}