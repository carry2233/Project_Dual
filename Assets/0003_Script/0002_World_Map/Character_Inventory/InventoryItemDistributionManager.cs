using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터별 인벤토리 생성, 캐릭터 슬롯 선택, 아이템 슬롯 표시, 페이지 이동을 관리하는 스크립트입니다.
/// </summary>
public class InventoryItemDistributionManager : MonoBehaviour
{
    [Header("참조")]
    public SaveStorage saveStorage; // 세이브 저장소 참조
    public ItemDefinitionList itemDefinitionList; // 아이템 정의 리스트 참조
    public CharacterInfoManager characterInfoManager; // 캐릭터 정보 매니저 참조

    [Header("외부 입력 잠금 참조")]
    public TileSelectionManager tileSelectionManager; // 타일 선택 및 월드맵 이동 잠금 제어 스크립트

    [Header("패널")]
    public GameObject distributionInventoryPanel; // 분포 인벤토리 패널

    [Header("패널 버튼")]
    public Button openInventoryPanelButton; // 인벤토리 패널 활성화 버튼
    public Button closeInventoryPanelButton; // 인벤토리 패널 비활성화 버튼

    [Header("캐릭터 인벤토리 프리팹 생성")]
    public CharacterInventory characterInventoryPrefab; // 캐릭터 인벤토리 프리팹
    public Transform characterInventoryPrefabParent; // 캐릭터 인벤토리 프리팹 부모

    [Header("캐릭터 슬롯 UI")]
    public Transform characterSlotParent; // 캐릭터 슬롯 부모

    [Header("아이템 슬롯 UI")]
    public DistributionInventorySlot distributionInventorySlotPrefab; // 분포 인벤토리 슬롯 프리팹
    public Transform inventorySlotParent; // 인벤토리 슬롯 부모
    public int slotCountPerPage = 24; // 한 페이지당 슬롯 수

    [Header("페이지 UI")]
    public Button previousPageButton; // 이전 페이지 버튼
    public Button nextPageButton; // 다음 페이지 버튼
    public TMP_Text pageIndexText; // 현재 페이지 / 총 페이지 텍스트

    private readonly List<CharacterInventory> characterInventories = new List<CharacterInventory>(); // 생성된 캐릭터 인벤토리 목록
    private readonly List<CharacterInventorySlot> characterSlots = new List<CharacterInventorySlot>(); // 생성된 캐릭터 슬롯 목록
    private readonly List<DistributionInventorySlot> inventorySlots = new List<DistributionInventorySlot>(); // 생성된 아이템 슬롯 목록

    private CharacterInventory selectedCharacterInventory; // 현재 선택된 캐릭터 인벤토리
    private List<InventoryDisplaySlotData> currentDisplaySlotItems = new List<InventoryDisplaySlotData>(); // 현재 선택 캐릭터의 슬롯 단위 표시 아이템 목록

    private int currentPageIndex; // 현재 페이지 인덱스
    private int totalPageCount; // 총 페이지 수

    private void Start() // 씬 시작 처리
    {
        FindReferences();
        BindButtons();
        InitializePanelState();
        CreateInventorySlots();
        CreateCharacterInventoriesFromSave();
        CreateCharacterSlots();
        SelectFirstCharacterInventory();
    }

private void FindReferences() // 필요한 매니저 참조 찾기
{
    if (saveStorage == null)
        saveStorage = FindObjectOfType<SaveStorage>();

    if (itemDefinitionList == null)
        itemDefinitionList = FindObjectOfType<ItemDefinitionList>();

    if (characterInfoManager == null)
        characterInfoManager = FindObjectOfType<CharacterInfoManager>();

    if (tileSelectionManager == null)
        tileSelectionManager = FindObjectOfType<TileSelectionManager>();
}

    private void BindButtons() // 버튼 이벤트 연결
    {
        if (openInventoryPanelButton != null)
        {
            openInventoryPanelButton.onClick.RemoveListener(OpenPanel);
            openInventoryPanelButton.onClick.AddListener(OpenPanel);
        }

        if (closeInventoryPanelButton != null)
        {
            closeInventoryPanelButton.onClick.RemoveListener(ClosePanel);
            closeInventoryPanelButton.onClick.AddListener(ClosePanel);
        }

        if (previousPageButton != null)
        {
            previousPageButton.onClick.RemoveListener(GoToPreviousPage);
            previousPageButton.onClick.AddListener(GoToPreviousPage);
        }

        if (nextPageButton != null)
        {
            nextPageButton.onClick.RemoveListener(GoToNextPage);
            nextPageButton.onClick.AddListener(GoToNextPage);
        }
    }

private void InitializePanelState() // 패널 초기 상태 설정
{
    if (distributionInventoryPanel != null)
        distributionInventoryPanel.SetActive(false);

    SetWorldInputLocked(false);
    RefreshPanelButtons();
}

    private void CreateInventorySlots() // 고정 개수 아이템 슬롯 생성
    {
        ClearChildren(inventorySlotParent);
        inventorySlots.Clear();

        if (distributionInventorySlotPrefab == null || inventorySlotParent == null)
            return;

        for (int i = 0; i < slotCountPerPage; i++)
        {
            DistributionInventorySlot newSlot = Instantiate(distributionInventorySlotPrefab, inventorySlotParent);
            newSlot.ClearSlot();
            inventorySlots.Add(newSlot);
        }
    }

    private void CreateCharacterInventoriesFromSave() // 세이브 데이터 기준 캐릭터 인벤토리 생성
    {
        ClearChildren(characterInventoryPrefabParent);
        characterInventories.Clear();

        if (saveStorage == null || characterInventoryPrefab == null || characterInventoryPrefabParent == null)
            return;

        List<SaveStorage.OwnedCharacterInventorySaveData> saveInventoryList = saveStorage.GetCurrentOwnedCharacterInventoryList();

        for (int i = 0; i < saveInventoryList.Count; i++)
        {
            SaveStorage.OwnedCharacterInventorySaveData saveInventoryData = saveInventoryList[i];

            CharacterInventory newInventory = Instantiate(characterInventoryPrefab, characterInventoryPrefabParent);
            newInventory.SetCharacterID(
                saveInventoryData.firstRowID,
                saveInventoryData.secondRowID,
                saveInventoryData.individualID
            );

            List<CharacterInventoryItemData> convertedItems = ConvertSaveItemsToInventoryItems(saveInventoryData.items);
            newInventory.SetItems(convertedItems);

            characterInventories.Add(newInventory);
        }
    }

    private List<CharacterInventoryItemData> ConvertSaveItemsToInventoryItems(List<SaveStorage.OwnedCharacterInventoryItemSaveData> saveItems) // 저장 아이템을 런타임 아이템으로 변환
    {
        List<CharacterInventoryItemData> result = new List<CharacterInventoryItemData>();

        if (saveItems == null || itemDefinitionList == null)
            return result;

        for (int i = 0; i < saveItems.Count; i++)
        {
            SaveStorage.OwnedCharacterInventoryItemSaveData saveItem = saveItems[i];
            GlobalItemDefinition itemDefinition = itemDefinitionList.GetItemDefinition(saveItem.itemAID, saveItem.itemBID);

            if (itemDefinition == null)
                continue;

            CharacterInventoryItemData itemData = new CharacterInventoryItemData
            {
                itemDefinition = itemDefinition,
                totalCount = saveItem.count,
                totalWeightKg = itemDefinition.weightPerItemKg * saveItem.count
            };

            result.Add(itemData);
        }

        return result;
    }

private void CreateCharacterSlots() // 캐릭터 인벤토리 슬롯 UI 생성
{
    ClearChildren(characterSlotParent);
    characterSlots.Clear();

    if (characterSlotParent == null || characterInfoManager == null)
        return;

    List<CharacterInventory> sortedInventories = characterInventories
        .OrderBy(inventory => GetCharacterDisplayPriority(inventory))
        .ToList();

    for (int i = 0; i < sortedInventories.Count; i++)
    {
        CharacterInventory inventory = sortedInventories[i];

        GlobalCharacterDefinition characterDefinition = characterInfoManager.FindDefinitionByID(
            inventory.firstRowID,
            inventory.secondRowID
        );

        if (characterDefinition == null || characterDefinition.CharacterInventorySlotPrefab == null)
            continue;

        CharacterInventorySlot newSlot = Instantiate(characterDefinition.CharacterInventorySlotPrefab, characterSlotParent);
        int priority = GetCharacterDisplayPriority(inventory);

        newSlot.Initialize(inventory, this, priority);
        characterSlots.Add(newSlot);
    }
}

private int GetCharacterDisplayPriority(CharacterInventory inventory) // 캐릭터 나열 우선순위 가져오기
{
    if (inventory == null || characterInfoManager == null)
        return int.MaxValue;

    GlobalCharacterDefinition characterDefinition = characterInfoManager.FindDefinitionByID(
        inventory.firstRowID,
        inventory.secondRowID
    );

    if (characterDefinition == null || characterDefinition.CharacterInventorySlotPrefab == null)
        return int.MaxValue;

    return characterDefinition.CharacterInventorySlotPrefab.DisplayPriority;
}

    private void SelectFirstCharacterInventory() // 첫 번째 캐릭터 인벤토리 선택
    {
        if (characterInventories.Count <= 0)
        {
            ClearInventorySlots();
            RefreshPageUI();
            return;
        }

        SelectCharacterInventory(characterInventories[0]);
    }

    public void SelectCharacterInventory(CharacterInventory characterInventory) // 캐릭터 인벤토리 선택
    {
        selectedCharacterInventory = characterInventory;
        currentPageIndex = 0;

        RefreshCurrentSortedItems();
        RefreshInventorySlots();
    }

private void RefreshCurrentSortedItems() // 현재 선택 캐릭터 아이템을 슬롯 단위로 분할 정렬
{
    currentDisplaySlotItems.Clear();

    if (selectedCharacterInventory == null)
        return;

    List<CharacterInventoryItemData> sortedItems = selectedCharacterInventory.storedItems
        .Where(item => item != null && item.itemDefinition != null && item.totalCount > 0)
        .OrderBy(item => item.itemDefinition.displayPriority)
        .ToList();

    for (int i = 0; i < sortedItems.Count; i++)
    {
        CharacterInventoryItemData itemData = sortedItems[i];

        int remainingCount = itemData.totalCount; // 아직 슬롯에 배치하지 않은 개수
        int maxStack = Mathf.Max(1, itemData.itemDefinition.maxStackPerSlot); // 한 슬롯당 최대 저장 수

        while (remainingCount > 0)
        {
            int slotCount = Mathf.Min(remainingCount, maxStack); // 이번 슬롯에 표시할 개수

            InventoryDisplaySlotData displaySlotData = new InventoryDisplaySlotData
            {
                itemDefinition = itemData.itemDefinition,
                count = slotCount
            };

            currentDisplaySlotItems.Add(displaySlotData);
            remainingCount -= slotCount;
        }
    }

    totalPageCount = Mathf.CeilToInt((float)currentDisplaySlotItems.Count / slotCountPerPage);

    if (totalPageCount <= 0)
        totalPageCount = 1;

    currentPageIndex = Mathf.Clamp(currentPageIndex, 0, totalPageCount - 1);
}

private void RefreshInventorySlots() // 현재 페이지 기준 아이템 슬롯 표시 갱신
{
    ClearInventorySlots();

    if (currentDisplaySlotItems == null || currentDisplaySlotItems.Count <= 0)
    {
        RefreshPageUI();
        return;
    }

    int startIndex = currentPageIndex * slotCountPerPage;

    for (int i = 0; i < inventorySlots.Count; i++)
    {
        int itemIndex = startIndex + i;

        if (itemIndex >= currentDisplaySlotItems.Count)
            break;

        InventoryDisplaySlotData slotData = currentDisplaySlotItems[itemIndex];
        inventorySlots[i].SetSlot(slotData.itemDefinition, slotData.count);
    }

    RefreshPageUI();
}

    private void ClearInventorySlots() // 모든 인벤토리 슬롯 비우기
    {
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (inventorySlots[i] != null)
                inventorySlots[i].ClearSlot();
        }
    }

    private void GoToPreviousPage() // 이전 페이지로 이동
    {
        if (currentPageIndex <= 0)
            return;

        currentPageIndex--;
        RefreshInventorySlots();
    }

    private void GoToNextPage() // 다음 페이지로 이동
    {
        if (currentPageIndex >= totalPageCount - 1)
            return;

        currentPageIndex++;
        RefreshInventorySlots();
    }

    private void RefreshPageUI() // 페이지 UI 갱신
    {
        if (pageIndexText != null)
            pageIndexText.text = $"{currentPageIndex + 1} / {totalPageCount}";

        if (previousPageButton != null)
            previousPageButton.gameObject.SetActive(totalPageCount > 1 && currentPageIndex > 0);

        if (nextPageButton != null)
            nextPageButton.gameObject.SetActive(totalPageCount > 1 && currentPageIndex < totalPageCount - 1);
    }

private void OpenPanel() // 패널 열기
{
    if (distributionInventoryPanel != null)
        distributionInventoryPanel.SetActive(true);

    SetWorldInputLocked(true);
    RefreshPanelButtons();
}

private void ClosePanel() // 패널 닫기
{
    if (distributionInventoryPanel != null)
        distributionInventoryPanel.SetActive(false);

    SetWorldInputLocked(false);
    RefreshPanelButtons();
}

    private void RefreshPanelButtons() // 패널 상태에 따른 버튼 표시 갱신
    {
        bool isPanelActive = distributionInventoryPanel != null && distributionInventoryPanel.activeSelf;

        if (openInventoryPanelButton != null)
            openInventoryPanelButton.gameObject.SetActive(!isPanelActive);

        if (closeInventoryPanelButton != null)
            closeInventoryPanelButton.gameObject.SetActive(isPanelActive);
    }

    private void ClearChildren(Transform parent) // 부모 아래 자식 오브젝트 제거
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }
private class InventoryDisplaySlotData
{
    public GlobalItemDefinition itemDefinition; // 슬롯에 표시할 아이템 정의
    public int count; // 슬롯에 표시할 아이템 개수
}

private void SetWorldInputLocked(bool isLocked) // 인벤토리창 상태에 따른 월드 입력 잠금 적용
{
    if (tileSelectionManager == null)
        return;

    tileSelectionManager.SetCharacterManagementUIOpen(isLocked);
}
}