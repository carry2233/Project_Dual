using System.Collections.Generic; // List 사용
using System.Linq; // 정렬 사용
using UnityEngine; // Unity 기본 기능 사용

/// <summary>
/// 아이템 획득 이벤트의 보상 계산, UI 표시, 아군 인벤토리 지급을 담당합니다.
/// </summary>
public class ItemRewardEvent : MonoBehaviour
{
    [System.Serializable]
    public class ItemBundle
    {
        [Header("아이템 모둠 ID")]
        public int itemBundleID; // 아이템 모둠 ID

        [Header("아이템 목록")]
        public List<GlobalItemDefinition> itemList = new List<GlobalItemDefinition>(); // 모둠에 포함된 아이템 목록
    }

    [System.Serializable]
    public class RewardItemResult
    {
        [Header("아이템 정보")]
        public GlobalItemDefinition itemDefinition; // 획득 아이템 정의

        [Header("획득 수")]
        public int itemCount; // 획득한 아이템 수
    }

    [Header("담당 이벤트 ID")]
    [SerializeField] private int targetEventID; // 이 스크립트가 처리할 이벤트 ID

    [Header("참조")]
    [SerializeField] private ItemDefinitionList itemDefinitionList; // 아이템 정의 목록
    [SerializeField] private FriendlyCharacterInfoManager friendlyCharacterInfoManager; // 아군 캐릭터 정보 관리자
    [SerializeField] private SaveStorage saveStorage; // 세이브 저장 관리자

    [Header("아이템 모둠")]
    [SerializeField] private List<ItemBundle> itemBundleList = new List<ItemBundle>(); // 아이템 모둠 리스트

    [Header("획득 가치 설정")]
    [SerializeField] private int minRewardValue = 1; // 최소 획득 총 가치
    [SerializeField] private int maxRewardValue = 10; // 최대 획득 총 가치

    [Header("랜덤 계산 안전 설정")]
    [SerializeField] private int maxPickTryCount = 200; // 무한 반복 방지용 최대 선택 시도 횟수

    private void Start() // 시작 시 참조 보정
    {
        if (itemDefinitionList == null)
            itemDefinitionList = ItemDefinitionList.Instance; // 전역 아이템 정의 목록 참조

        if (friendlyCharacterInfoManager == null)
            friendlyCharacterInfoManager = FriendlyCharacterInfoManager.Instance; // 전역 아군 정보 관리자 참조

        if (saveStorage == null)
            saveStorage = SaveStorage.Instance; // 전역 세이브 관리자 참조
    }

    public bool CanHandleEvent(int eventID) // 이 이벤트 ID를 처리할 수 있는지 확인
    {
        return targetEventID == eventID;
    }

    public void ExecuteRewardEvent(BaseEventPanel baseEventPanel, EventInfoDefinition eventInfoDefinition) // 아이템 획득 이벤트 실행
    {
        ItemBundle selectedBundle = SelectRandomBundle(); // 랜덤 아이템 모둠 선택

        if (selectedBundle == null)
            return;

        int rewardValue = Random.Range(minRewardValue, maxRewardValue + 1); // 총 획득 가치 랜덤 결정
        List<RewardItemResult> rewardResults = CalculateRewardItems(selectedBundle, rewardValue); // 획득 아이템 계산

        if (baseEventPanel != null)
        {
            baseEventPanel.CreateRewardSlots(rewardResults); // 획득 아이템 슬롯 생성
        }

        GiveRewardsToFriendlyInventories(rewardResults); // 아군 인벤토리에 아이템 지급
    }

    private ItemBundle SelectRandomBundle() // 랜덤 아이템 모둠 선택
    {
        List<ItemBundle> validBundles = itemBundleList
            .Where(bundle => bundle != null && bundle.itemList != null && bundle.itemList.Count > 0)
            .ToList();

        if (validBundles.Count <= 0)
            return null;

        int randomIndex = Random.Range(0, validBundles.Count); // 랜덤 인덱스 선택
        return validBundles[randomIndex];
    }

    private List<RewardItemResult> CalculateRewardItems(ItemBundle selectedBundle, int rewardValue) // 가치 한도 기반 획득 아이템 계산
    {
        List<RewardItemResult> rewardResults = new List<RewardItemResult>(); // 최종 보상 결과
        int remainingValue = Mathf.Max(0, rewardValue); // 남은 획득 가능 가치
        int tryCount = 0; // 선택 시도 횟수

        List<GlobalItemDefinition> validItems = selectedBundle.itemList
            .Where(item => item != null && item.itemValue > 0 && item.itemValue <= remainingValue)
            .ToList();

        while (remainingValue > 0 && validItems.Count > 0 && tryCount < maxPickTryCount)
        {
            tryCount++; // 시도 횟수 증가

            List<GlobalItemDefinition> affordableItems = validItems
                .Where(item => item != null && item.itemValue > 0 && item.itemValue <= remainingValue)
                .ToList();

            if (affordableItems.Count <= 0)
                break;

            GlobalItemDefinition selectedItem = affordableItems[Random.Range(0, affordableItems.Count)]; // 획득 아이템 랜덤 선택

            AddRewardResult(rewardResults, selectedItem, 1); // 보상 결과에 1개 추가
            remainingValue -= selectedItem.itemValue; // 남은 가치 차감
        }

        return rewardResults
            .Where(result => result != null && result.itemDefinition != null && result.itemCount > 0)
            .OrderBy(result => result.itemDefinition.displayPriority)
            .ToList();
    }

    private void AddRewardResult(List<RewardItemResult> rewardResults, GlobalItemDefinition itemDefinition, int addCount) // 보상 결과 누적
    {
        if (rewardResults == null || itemDefinition == null || addCount <= 0)
            return;

        for (int i = 0; i < rewardResults.Count; i++)
        {
            RewardItemResult result = rewardResults[i];

            if (result == null || result.itemDefinition == null)
                continue;

            bool isSameItem =
                result.itemDefinition.itemAID == itemDefinition.itemAID &&
                result.itemDefinition.itemBID == itemDefinition.itemBID;

            if (!isSameItem)
                continue;

            result.itemCount += addCount; // 같은 아이템이면 수량 증가
            return;
        }

        RewardItemResult newResult = new RewardItemResult
        {
            itemDefinition = itemDefinition,
            itemCount = addCount
        };

        rewardResults.Add(newResult); // 새 보상 결과 추가
    }

    private void GiveRewardsToFriendlyInventories(List<RewardItemResult> rewardResults) // 아군 인벤토리에 보상 지급
    {
        if (rewardResults == null || rewardResults.Count <= 0)
            return;

        List<CharacterInventory> sortedInventories = FindSortedFriendlyInventories(); // 지급 대상 인벤토리 정렬 목록

        if (sortedInventories.Count <= 0)
            return;

        List<RewardItemResult> sortedRewards = rewardResults
            .Where(result => result != null && result.itemDefinition != null && result.itemCount > 0)
            .OrderBy(result => result.itemDefinition.displayPriority)
            .ToList();

        int targetInventoryIndex = 0; // 현재 지급 대상 인덱스

        for (int i = 0; i < sortedRewards.Count; i++)
        {
            RewardItemResult reward = sortedRewards[i];

            for (int count = 0; count < reward.itemCount; count++)
            {
                CharacterInventory targetInventory = sortedInventories[targetInventoryIndex]; // 현재 지급 대상
                targetInventory.AddOrMergeItem(reward.itemDefinition, 1); // 아이템 1개 지급
                SaveCharacterInventory(targetInventory); // 지급 후 세이브 저장

                targetInventoryIndex++; // 다음 지급 대상 이동

                if (targetInventoryIndex >= sortedInventories.Count)
                    targetInventoryIndex = 0; // 끝까지 갔으면 처음 대상으로 순회
            }
        }
    }

    private List<CharacterInventory> FindSortedFriendlyInventories() // 아군 인벤토리 정렬 목록 찾기
    {
        CharacterInventory[] inventories = FindObjectsOfType<CharacterInventory>(true); // 비활성 포함 인벤토리 탐색

        return inventories
            .Where(inventory => inventory != null)
            .OrderBy(inventory => GetCharacterPriority(inventory))
            .ToList();
    }

    private int GetCharacterPriority(CharacterInventory inventory) // 캐릭터 지급 우선순위 가져오기
    {
        if (inventory == null || friendlyCharacterInfoManager == null)
            return int.MaxValue;

        FriendlyCharacterDefinition friendlyDefinition = friendlyCharacterInfoManager.FindDefinitionByID(
            inventory.firstRowID,
            inventory.secondRowID
        );

        if (friendlyDefinition == null || friendlyDefinition.globalCharacterDefinition == null)
            return int.MaxValue;

        return friendlyDefinition.globalCharacterDefinition.displayPriority;
    }

    private void SaveCharacterInventory(CharacterInventory characterInventory) // 캐릭터 인벤토리 세이브 저장
    {
        if (saveStorage == null || characterInventory == null)
            return;

        SaveStorage.OwnedCharacterInventorySaveData saveData = new SaveStorage.OwnedCharacterInventorySaveData
        {
            firstRowID = characterInventory.firstRowID,
            secondRowID = characterInventory.secondRowID,
            individualID = characterInventory.individualID,
            items = new List<SaveStorage.OwnedCharacterInventoryItemSaveData>()
        };

        for (int i = 0; i < characterInventory.storedItems.Count; i++)
        {
            CharacterInventoryItemData itemData = characterInventory.storedItems[i];

            if (itemData == null || itemData.itemDefinition == null || itemData.totalCount <= 0)
                continue;

            SaveStorage.OwnedCharacterInventoryItemSaveData itemSaveData = new SaveStorage.OwnedCharacterInventoryItemSaveData
            {
                itemAID = itemData.itemDefinition.itemAID,
                itemBID = itemData.itemDefinition.itemBID,
                count = itemData.totalCount
            };

            saveData.items.Add(itemSaveData); // 저장용 아이템 데이터 추가
        }

        saveStorage.SetCurrentOwnedCharacterInventoryData(saveData); // 현재 세이브 인벤토리 갱신
    }

    public void ExecuteRewardEventWithoutClearingSlots(BaseEventPanel baseEventPanel) // 기존 슬롯을 유지한 채 아이템 획득 이벤트 실행
{
    ItemBundle selectedBundle = SelectRandomBundle(); // 랜덤 아이템 모둠 선택

    if (selectedBundle == null)
        return; // 선택 가능한 모둠이 없으면 종료

    int rewardValue = Random.Range(minRewardValue, maxRewardValue + 1); // 총 획득 가치 랜덤 결정
    List<RewardItemResult> rewardResults = CalculateRewardItems(selectedBundle, rewardValue); // 획득 아이템 계산

    if (baseEventPanel != null)
    {
        baseEventPanel.AppendRewardSlots(rewardResults); // 기존 전투 슬롯 유지 후 보상 슬롯 추가
    }

    GiveRewardsToFriendlyInventories(rewardResults); // 아군 인벤토리에 아이템 지급
}
}