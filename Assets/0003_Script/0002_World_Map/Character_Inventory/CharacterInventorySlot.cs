using UnityEngine;
using UnityEngine.EventSystems; // UI 포인터/드롭 이벤트 사용
using UnityEngine.UI;

/// <summary>
/// 캐릭터 인벤토리 선택용 UI 슬롯을 담당하는 스크립트입니다.
/// </summary>
public class CharacterInventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDropHandler
{
    [Header("버튼")]
    public Button selectButton; // 캐릭터 인벤토리 선택 버튼

    [Header("나열 정보")]
    public int displayPriority; // 낮을수록 먼저 나열되는 우선순위값

    [Header("드래그 강조 이미지")]
public Image highlightImage; // 드래그 중 마우스가 올라왔을 때 색이 바뀔 이미지
public Color normalColor = Color.white; // 기본 색
public Color dragHoverColor = Color.yellow; // 드래그 중 마우스가 올라왔을 때 색

public CharacterInventory TargetInventory => targetInventory; // 담당 캐릭터 인벤토리 반환

    private CharacterInventory targetInventory; // 이 슬롯이 담당하는 캐릭터 인벤토리
    private InventoryItemDistributionManager inventoryManager; // 인벤토리 분포 매니저 참조
    public int DisplayPriority => displayPriority; // 슬롯 나열 우선순위값 반환

    public void Initialize(CharacterInventory newTargetInventory, InventoryItemDistributionManager newInventoryManager, int newDisplayPriority) // 슬롯 초기화
    {
        targetInventory = newTargetInventory;
        inventoryManager = newInventoryManager;
        displayPriority = newDisplayPriority;

        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(OnClickSlot);
            selectButton.onClick.AddListener(OnClickSlot);
        }
    }

private void OnClickSlot() // 슬롯 클릭 처리
{
    if (inventoryManager == null || targetInventory == null)
        return;

    if (inventoryManager.IsDraggingItem()) // 드래그 중 클릭 선택 방지
        return;

    inventoryManager.SelectCharacterInventory(targetInventory);
}

public void OnPointerEnter(PointerEventData eventData) // 마우스 진입 처리
{
    if (inventoryManager == null || !inventoryManager.IsDraggingItem())
        return;

    SetHighlightColor(dragHoverColor);
}

public void OnPointerExit(PointerEventData eventData) // 마우스 이탈 처리
{
    SetHighlightColor(normalColor);
}

public void OnDrop(PointerEventData eventData) // 드래그 아이템 드롭 처리
{
    if (inventoryManager == null || targetInventory == null)
        return;

    inventoryManager.DropDraggedItemToCharacterSlot(this);
    SetHighlightColor(normalColor);
}

private void SetHighlightColor(Color targetColor) // 강조 이미지 색상 적용
{
    if (highlightImage != null)
        highlightImage.color = targetColor;
}
}