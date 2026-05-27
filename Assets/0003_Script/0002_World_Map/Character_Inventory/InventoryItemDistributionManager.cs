using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // UI 클릭 판정용
using UnityEngine.InputSystem; // New Input System 마우스 입력용

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

    [Header("드래그용 슬롯 UI")]
public GameObject dragSlotObject; // 드래그 중 표시할 슬롯 오브젝트
public RectTransform dragSlotRectTransform; // 드래그 슬롯 위치 제어용 RectTransform
public Image dragItemImage; // 드래그 아이템 이미지
public TMP_Text dragCountText; // 드래그 아이템 개수 텍스트

[Header("아군 정보 참조")]
public FriendlyCharacterInfoManager friendlyCharacterInfoManager; // 아군 정보 매니저 참조

[Header("무게 표시 UI")]
public TMP_Text weightInfoText; // 적정 무게 / 총 무게 표시 텍스트
public Color properWeightTextColor = Color.white; // 적정 무게 이하일 때 텍스트 색
public Color overweightTextColor = Color.red; // 적정 무게 초과일 때 텍스트 색
public int weightPerOverStack = 10; // 초과 중첩 1개당 필요한 무게값


[Header("____________________________________________________________________________")]


[Header("아이템 상호작용 패널")]
public GameObject itemInteractionPanelObject; // 우클릭 시 표시될 상호작용 패널 오브젝트
public Transform itemInteractionButtonListObject; // 상호작용 버튼들이 생성될 부모 오브젝트
public GameObject discardButtonPrefab; // 버리기 버튼 프리팹
public GameObject consumeItemButtonPrefab; // 기본 소모 버튼 프리팹

private DistributionInventorySlot selectedInteractionSlot; // 우클릭한 아이템 슬롯
private GlobalItemDefinition selectedInteractionItemDefinition; // 우클릭한 아이템 정의

private DistributionInventorySlot draggingSourceSlot; // 드래그를 시작한 원본 슬롯
private GlobalItemDefinition draggingItemDefinition; // 드래그 중인 아이템 정의
private int draggingItemCount; // 드래그 중인 아이템 개수
private bool isDraggingItem; // 현재 아이템 드래그 중인지 여부
private bool isDragDropped; // 정상 드롭 완료 여부

    private readonly List<CharacterInventory> characterInventories = new List<CharacterInventory>(); // 생성된 캐릭터 인벤토리 목록
    private readonly List<CharacterInventorySlot> characterSlots = new List<CharacterInventorySlot>(); // 생성된 캐릭터 슬롯 목록
    private readonly List<DistributionInventorySlot> inventorySlots = new List<DistributionInventorySlot>(); // 생성된 아이템 슬롯 목록
    private readonly List<RaycastResult> interactionClickRaycastResults = new List<RaycastResult>(); // 상호작용 패널 외부 클릭 판정 결과

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

private void Update() // 매 프레임 입력 확인
{
    CheckCloseItemInteractionPanelByOutsideClick(); // 상호작용 패널 외부 클릭 시 닫기
}

private void CheckCloseItemInteractionPanelByOutsideClick() // 생성된 버튼 외 영역 클릭 시 상호작용 패널 닫기
{
    if (itemInteractionPanelObject == null || itemInteractionPanelObject.activeSelf == false)
    {
        return; // 패널이 꺼져 있으면 종료
    }

    Mouse mouse = Mouse.current; // 현재 마우스 장치 참조

    if (mouse == null)
    {
        return; // 마우스 장치가 없으면 종료
    }

    bool isLeftClicked = mouse.leftButton.wasPressedThisFrame; // 이번 프레임 좌클릭 여부
    bool isRightClicked = mouse.rightButton.wasPressedThisFrame; // 이번 프레임 우클릭 여부

    if (isLeftClicked == false && isRightClicked == false)
    {
        return; // 좌클릭/우클릭이 아니면 종료
    }

    if (IsPointerOnInteractionButton() == true)
    {
        return; // 생성된 버튼 위 클릭이면 유지
    }

    CloseItemInteractionPanel(); // 버튼 외 영역 클릭 시 패널 닫기
}

private bool IsPointerOnInteractionButton() // 현재 포인터가 생성된 상호작용 버튼 위에 있는지 확인
{
    if (EventSystem.current == null || itemInteractionButtonListObject == null)
    {
        return false; // 이벤트 시스템 또는 버튼 부모가 없으면 버튼 위가 아님
    }

    Mouse mouse = Mouse.current; // 현재 마우스 장치 참조

    if (mouse == null)
    {
        return false; // 마우스 장치가 없으면 버튼 위가 아님
    }

    PointerEventData pointerEventData = new PointerEventData(EventSystem.current); // 포인터 이벤트 데이터 생성
    pointerEventData.position = mouse.position.ReadValue(); // New Input System 기준 마우스 위치 설정

    interactionClickRaycastResults.Clear(); // 이전 판정 결과 초기화
    EventSystem.current.RaycastAll(pointerEventData, interactionClickRaycastResults); // UI 레이캐스트 실행

    for (int i = 0; i < interactionClickRaycastResults.Count; i++)
    {
        GameObject hitObject = interactionClickRaycastResults[i].gameObject; // 감지된 UI 오브젝트

        if (hitObject == null)
        {
            continue; // 비어 있으면 건너뜀
        }

        if (hitObject.transform.IsChildOf(itemInteractionButtonListObject) == true)
        {
            return true; // 생성된 버튼 영역이면 true
        }
    }

    return false; // 버튼 영역이 아님
}

private void FindReferences() // 필요한 매니저 참조 찾기
{
    if (saveStorage == null)
        saveStorage = FindObjectOfType<SaveStorage>();

    if (itemDefinitionList == null)
        itemDefinitionList = FindObjectOfType<ItemDefinitionList>();

    if (characterInfoManager == null)
        characterInfoManager = FindObjectOfType<CharacterInfoManager>();

    if (friendlyCharacterInfoManager == null)
        friendlyCharacterInfoManager = FindObjectOfType<FriendlyCharacterInfoManager>();

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

    if (dragSlotObject != null) // 드래그 슬롯 시작 시 비활성화
        dragSlotObject.SetActive(false);

        if (itemInteractionPanelObject != null)
        {
            itemInteractionPanelObject.SetActive(false); // 시작 시 상호작용 패널 비활성화
        }

            ClearItemInteractionButtons(); // 시작 시 상호작용 버튼 정리

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
            newSlot.Initialize(this); // 드래그 처리를 위한 매니저 연결
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
        RefreshCharacterWeightState(newInventory);

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
    if (inventory == null || friendlyCharacterInfoManager == null)
        return int.MaxValue;

    FriendlyCharacterDefinition friendlyDefinition = friendlyCharacterInfoManager.FindDefinitionByID(
        inventory.firstRowID,
        inventory.secondRowID
    );

    if (friendlyDefinition == null)
        return int.MaxValue;

    return friendlyDefinition.slotDisplayPriority;
}

private void SelectFirstCharacterInventory() // 우선순위가 가장 낮은 캐릭터 인벤토리 선택
{
    if (characterInventories.Count <= 0)
    {
        ClearInventorySlots();
        RefreshPageUI();
        RefreshSelectedCharacterWeightInfo();
        return;
    }

    CharacterInventory firstPriorityInventory = characterInventories
        .OrderBy(inventory => GetCharacterDisplayPriority(inventory))
        .FirstOrDefault();

    SelectCharacterInventory(firstPriorityInventory);
}

public void SelectCharacterInventory(CharacterInventory characterInventory) // 캐릭터 인벤토리 선택
{
    CloseItemInteractionPanel(); // 다른 캐릭터 선택으로 아이템 목록이 바뀌면 상호작용 패널 닫기

    selectedCharacterInventory = characterInventory; // 선택 캐릭터 인벤토리 갱신
    currentPageIndex = 0; // 페이지를 첫 페이지로 초기화

    RefreshCurrentSortedItems(); // 표시 아이템 목록 갱신
    RefreshInventorySlots(); // 슬롯 UI 갱신
    RefreshSelectedCharacterWeightInfo(); // 무게 UI 갱신
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
    CloseItemInteractionPanel(); // 인벤토리창 닫기 시 상호작용 패널도 닫기

    if (distributionInventoryPanel != null)
    {
        distributionInventoryPanel.SetActive(false); // 인벤토리 패널 비활성화
    }

    SetWorldInputLocked(false); // 월드 입력 잠금 해제
    RefreshPanelButtons(); // 패널 버튼 상태 갱신
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

public bool IsDraggingItem() // 아이템 드래그 중인지 반환
{
    return isDraggingItem;
}

public void BeginItemDrag(DistributionInventorySlot sourceSlot, GlobalItemDefinition itemDefinition, int itemCount) // 아이템 드래그 시작
{
    if (sourceSlot == null || itemDefinition == null || itemCount <= 0)
        return;

    draggingSourceSlot = sourceSlot;
    draggingItemDefinition = itemDefinition;
    draggingItemCount = itemCount;
    isDraggingItem = true;
    isDragDropped = false;

    sourceSlot.HideItemVisualOnly();

    if (dragSlotObject != null)
        dragSlotObject.SetActive(true);

    if (dragItemImage != null)
    {
        dragItemImage.sprite = itemDefinition.itemImage;
        dragItemImage.enabled = itemDefinition.itemImage != null;
    }

    if (dragCountText != null)
        dragCountText.text = itemCount > 1 ? itemCount.ToString() : "";
}

public void UpdateDragSlotPosition(Vector2 screenPosition) // 드래그 슬롯 위치 갱신
{
    if (!isDraggingItem || dragSlotRectTransform == null)
        return;

    dragSlotRectTransform.position = screenPosition;
}

public void DropDraggedItemToCharacterSlot(CharacterInventorySlot targetSlot) // 캐릭터 슬롯에 아이템 드롭
{
    if (!isDraggingItem || targetSlot == null)
        return;

    CharacterInventory targetInventory = targetSlot.TargetInventory;

    if (selectedCharacterInventory == null || targetInventory == null)
    {
        CancelDrag();
        return;
    }

    if (selectedCharacterInventory == targetInventory)
    {
        CancelDrag();
        return;
    }

    bool removed = selectedCharacterInventory.RemoveItem(draggingItemDefinition, draggingItemCount);

    if (!removed)
    {
        CancelDrag();
        return;
    }

    targetInventory.AddOrMergeItem(draggingItemDefinition, draggingItemCount);

    RefreshCharacterWeightState(selectedCharacterInventory);
    RefreshCharacterWeightState(targetInventory);

    SaveCharacterInventory(selectedCharacterInventory);
    SaveCharacterInventory(targetInventory);

    isDragDropped = true;

    RefreshCurrentSortedItems();
    RefreshInventorySlots();
    RefreshSelectedCharacterWeightInfo();
    EndDragState();
}

public void CancelDragIfNotDropped() // 캐릭터 슬롯이 아닌 곳에 놓았을 때 드래그 취소
{
    if (!isDraggingItem)
        return;

    if (isDragDropped)
        return;

    CancelDrag();
}

private void CancelDrag() // 드래그 취소 및 원본 슬롯 복구
{
    if (draggingSourceSlot != null)
        draggingSourceSlot.RestoreItemVisualOnly();

    EndDragState();
}

private void EndDragState() // 드래그 상태 초기화
{
    if (dragSlotObject != null)
        dragSlotObject.SetActive(false);

    if (dragItemImage != null)
    {
        dragItemImage.sprite = null;
        dragItemImage.enabled = false;
    }

    if (dragCountText != null)
        dragCountText.text = "";

    draggingSourceSlot = null;
    draggingItemDefinition = null;
    draggingItemCount = 0;
    isDraggingItem = false;
    isDragDropped = false;
}

private void SaveCharacterInventory(CharacterInventory characterInventory) // 캐릭터 인벤토리 세이브 갱신
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

        saveData.items.Add(itemSaveData);
    }

    saveStorage.SetCurrentOwnedCharacterInventoryData(saveData);
}

private void RefreshCharacterWeightState(CharacterInventory characterInventory) // 캐릭터 인벤토리의 초과 무게 상태 갱신
{
    if (characterInventory == null)
        return;

    FriendlyCharacterDefinition friendlyDefinition = GetFriendlyDefinition(characterInventory);

    if (friendlyDefinition == null)
    {
        characterInventory.RefreshTotalWeight();
        characterInventory.SetOverweightState(characterInventory.totalWeightKg, weightPerOverStack);
        return;
    }

    characterInventory.SetOverweightState(friendlyDefinition.properWeightKg, weightPerOverStack);
}

private void RefreshSelectedCharacterWeightInfo() // 선택된 캐릭터의 무게 표시 갱신
{
    if (weightInfoText == null)
        return;

    if (selectedCharacterInventory == null)
    {
        weightInfoText.text = "";
        return;
    }

    FriendlyCharacterDefinition friendlyDefinition = GetFriendlyDefinition(selectedCharacterInventory);

    if (friendlyDefinition == null)
    {
        selectedCharacterInventory.RefreshTotalWeight();
        weightInfoText.text = $"0 / {selectedCharacterInventory.totalWeightKg:0.##}";
        weightInfoText.color = overweightTextColor;
        return;
    }

    selectedCharacterInventory.SetOverweightState(friendlyDefinition.properWeightKg, weightPerOverStack);

    float properWeight = friendlyDefinition.properWeightKg;
    float totalWeight = selectedCharacterInventory.totalWeightKg;

    weightInfoText.text = $"{properWeight:0.##} / {totalWeight:0.##}";

    if (properWeight >= totalWeight)
        weightInfoText.color = properWeightTextColor;
    else
        weightInfoText.color = overweightTextColor;
}

private FriendlyCharacterDefinition GetFriendlyDefinition(CharacterInventory characterInventory) // 캐릭터 인벤토리 기준 아군 정의 찾기
{
    if (characterInventory == null || friendlyCharacterInfoManager == null)
        return null;

    return friendlyCharacterInfoManager.FindDefinitionByID(
        characterInventory.firstRowID,
        characterInventory.secondRowID
    );
}

public void OpenItemInteractionPanel(DistributionInventorySlot targetSlot, GlobalItemDefinition itemDefinition) // 아이템 상호작용 패널 열기
{
    if (IsDraggingItem())
    {
        return; // 아이템 이동 중에는 우클릭 상호작용 무시
    }

    if (targetSlot == null || itemDefinition == null)
    {
        return; // 대상 슬롯이나 아이템이 없으면 종료
    }

    selectedInteractionSlot = targetSlot; // 우클릭 슬롯 저장
    selectedInteractionItemDefinition = itemDefinition; // 우클릭 아이템 저장

    ClearItemInteractionButtons(); // 기존 버튼 제거

    if (itemInteractionPanelObject != null)
    {
        itemInteractionPanelObject.transform.position = targetSlot.transform.position; // 패널 위치를 슬롯 위치로 이동
        itemInteractionPanelObject.SetActive(true); // 패널 활성화
    }

    CreateDiscardButton(); // 버리기 버튼 생성
    CreateConsumeButtonIfNeeded(itemDefinition); // 소모 아이템이면 소모 버튼 생성
}

private void CreateDiscardButton() // 버리기 버튼 생성
{
    if (discardButtonPrefab == null || itemInteractionButtonListObject == null)
    {
        return; // 프리팹이나 부모가 없으면 종료
    }

    GameObject buttonObject = Instantiate(discardButtonPrefab, itemInteractionButtonListObject); // 버리기 버튼 생성
    Button button = buttonObject.GetComponent<Button>(); // 버튼 컴포넌트 참조

    if (button != null)
    {
        button.onClick.RemoveAllListeners(); // 기존 이벤트 제거
        button.onClick.AddListener(DiscardSelectedInteractionItem); // 버리기 이벤트 연결
    }
}

private void CreateConsumeButtonIfNeeded(GlobalItemDefinition itemDefinition) // 소모 버튼 필요 시 생성
{
    if (itemDefinitionList == null)
    {
        return; // 아이템 정의 리스트가 없으면 종료
    }

    ConsumableItemDefinition consumableDefinition = itemDefinitionList.GetConsumableItemDefinition(itemDefinition); // 소모 아이템 정의 탐색

    if (consumableDefinition == null)
    {
        return; // 소모 아이템이 아니면 종료
    }

    GameObject buttonPrefab = consumableDefinition.consumeButtonPrefab != null
        ? consumableDefinition.consumeButtonPrefab
        : consumeItemButtonPrefab; // 소모 아이템 전용 버튼 우선 사용

    if (buttonPrefab == null || itemInteractionButtonListObject == null)
    {
        return; // 생성할 버튼이 없으면 종료
    }

    GameObject buttonObject = Instantiate(buttonPrefab, itemInteractionButtonListObject); // 소모 버튼 생성
    Button button = buttonObject.GetComponent<Button>(); // 버튼 컴포넌트 참조

    if (button != null)
    {
        bool canConsumeItem = CanConsumeItemByNigrumRule(consumableDefinition); // 흑체 규칙 기준 소모 가능 여부 확인

        button.interactable = canConsumeItem; // 소모 가능 여부에 따라 버튼 상호작용 설정
        button.onClick.RemoveAllListeners(); // 기존 이벤트 제거

        if (canConsumeItem == true)
        {
            button.onClick.AddListener(() => ConsumeSelectedInteractionItem(consumableDefinition)); // 소모 가능할 때만 소모 이벤트 연결
        }
    }
}

private void DiscardSelectedInteractionItem() // 선택한 아이템 버리기
{
    if (selectedCharacterInventory == null || selectedInteractionItemDefinition == null)
    {
        CloseItemInteractionPanel(); // 패널 닫기
        return;
    }

    selectedCharacterInventory.RemoveItem(selectedInteractionItemDefinition, 1); // 선택 아이템 1개 삭제
    RefreshCharacterWeightState(selectedCharacterInventory); // 무게 상태 갱신
    SaveCharacterInventory(selectedCharacterInventory); // 저장 인벤토리 갱신

    RefreshCurrentSortedItems(); // 표시 아이템 재정렬
    RefreshInventorySlots(); // 슬롯 UI 갱신
    RefreshSelectedCharacterWeightInfo(); // 무게 UI 갱신
    CloseItemInteractionPanel(); // 패널 닫기
}

private void ConsumeSelectedInteractionItem(ConsumableItemDefinition consumableDefinition) // 선택한 소모 아이템 사용
{
    if (selectedCharacterInventory == null || selectedInteractionItemDefinition == null || consumableDefinition == null)
    {
        CloseItemInteractionPanel(); // 패널 닫기
        return; // 사용 불가 상태면 종료
    }

    bool removed = selectedCharacterInventory.RemoveItem(
        selectedInteractionItemDefinition,
        1
    ); // 선택한 소모 아이템을 1개만 제거

    if (removed == false)
    {
        CloseItemInteractionPanel(); // 제거 실패 시 패널 닫기
        return; // 제거 실패 시 종료
    }

    if (saveStorage != null)
    {
        saveStorage.ApplyConsumableValueToCurrentOwnedCharacterStat(
            selectedCharacterInventory.firstRowID,
            selectedCharacterInventory.secondRowID,
            selectedCharacterInventory.individualID,
            consumableDefinition.applyHealthValue,
            consumableDefinition.applyHungerValue
        ); // 체력/허기 적용

        if (consumableDefinition.useNigrumValue == true)
        {
            FriendlyCharacterDefinition friendlyDefinition = GetFriendlyDefinition(selectedCharacterInventory); // 선택 캐릭터의 아군 정의 탐색

            FriendlyNigrumIntakeManager nigrumIntakeManager =
                FriendlyNigrumIntakeManager.Instance != null
                    ? FriendlyNigrumIntakeManager.Instance
                    : FindFirstObjectByType<FriendlyNigrumIntakeManager>(); // 흑체 복용 관리자 탐색

            int maxNigrumCapacity = nigrumIntakeManager != null
                ? nigrumIntakeManager.GetMaxNigrumCapacity(friendlyDefinition)
                : 0; // 해당 아군의 최대 흑체 수용값 가져오기

            saveStorage.ApplyFriendlyNigrumCapacityValue(
                friendlyDefinition,
                maxNigrumCapacity,
                consumableDefinition.applyNigrumCapacityValue
            ); // 최대값을 넘지 않게 흑체 수용값 적용
        }
    }

    RefreshCharacterWeightState(selectedCharacterInventory); // 무게 상태 갱신
    SaveCharacterInventory(selectedCharacterInventory); // 저장 인벤토리 갱신

    RefreshCurrentSortedItems(); // 표시 아이템 재정렬
    RefreshInventorySlots(); // 슬롯 UI 갱신
    RefreshSelectedCharacterWeightInfo(); // 무게 UI 갱신
    CloseItemInteractionPanel(); // 패널 닫기
}

private void CloseItemInteractionPanel() // 아이템 상호작용 패널 닫기
{
    if (itemInteractionPanelObject != null)
    {
        itemInteractionPanelObject.SetActive(false); // 패널 비활성화
    }

    ClearItemInteractionButtons(); // 생성된 버튼 제거

    selectedInteractionSlot = null; // 선택 슬롯 초기화
    selectedInteractionItemDefinition = null; // 선택 아이템 초기화
}

private void ClearItemInteractionButtons() // 상호작용 버튼 전체 제거
{
    if (itemInteractionButtonListObject == null)
    {
        return; // 버튼 부모가 없으면 종료
    }

    for (int i = itemInteractionButtonListObject.childCount - 1; i >= 0; i--)
    {
        Destroy(itemInteractionButtonListObject.GetChild(i).gameObject); // 생성된 버튼 제거
    }
}

private bool CanConsumeItemByNigrumRule(ConsumableItemDefinition consumableDefinition) // 흑체 규칙 기준 소모 아이템 사용 가능 여부 반환
{
    if (consumableDefinition == null)
    {
        return false; // 소모 정의가 없으면 사용 불가
    }

    if (consumableDefinition.useNigrumValue == false)
    {
        return true; // 흑체 수용값을 적용하지 않는 아이템은 사용 가능
    }

    FriendlyCharacterDefinition friendlyDefinition = GetFriendlyDefinition(selectedCharacterInventory); // 선택 캐릭터의 아군 정의 탐색

    FriendlyNigrumIntakeManager nigrumIntakeManager =
        FriendlyNigrumIntakeManager.Instance != null
            ? FriendlyNigrumIntakeManager.Instance
            : FindFirstObjectByType<FriendlyNigrumIntakeManager>(); // 흑체 복용 관리자 탐색

    if (nigrumIntakeManager == null)
    {
        return false; // 흑체 복용 관리자가 없으면 사용 불가
    }

    return nigrumIntakeManager.HasNigrumIntakeRule(friendlyDefinition); // 흑체 규칙 목록에 있는 아군만 사용 가능
}







}