using TMPro;
using UnityEngine;
using UnityEngine.EventSystems; // UI 드래그 이벤트 사용
using UnityEngine.UI;

/// <summary>
/// 분포 인벤토리 안에서 아이템 이미지와 개수를 표시하는 슬롯 스크립트입니다.
/// </summary>
public class DistributionInventorySlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("표시 UI")]
    public Image itemImage; // 아이템 이미지 UI
    public TMP_Text countText; // 아이템 개수 텍스트

    private InventoryItemDistributionManager inventoryManager; // 인벤토리 분배 매니저 참조

    private GlobalItemDefinition currentItemDefinition; // 현재 표시 중인 아이템 정의
    private int currentCount; // 현재 표시 중인 아이템 개수

    public void SetSlot(GlobalItemDefinition itemDefinition, int count) // 슬롯 표시 정보 설정
    {
        currentItemDefinition = itemDefinition;
        currentCount = count;

        if (itemImage != null)
        {
            itemImage.sprite = itemDefinition != null ? itemDefinition.itemImage : null;
            itemImage.enabled = itemDefinition != null && itemDefinition.itemImage != null;
        }

        if (countText != null)
        {
            countText.text = count > 1 ? count.ToString() : "";
        }
    }

    public void ClearSlot() // 슬롯 표시 정보 비우기
    {
        currentItemDefinition = null;
        currentCount = 0;

        if (itemImage != null)
        {
            itemImage.sprite = null;
            itemImage.enabled = false;
        }

        if (countText != null)
        {
            countText.text = "";
        }
    }

    public void Initialize(InventoryItemDistributionManager newInventoryManager) // 슬롯 초기화
{
    inventoryManager = newInventoryManager;
}

public GlobalItemDefinition GetCurrentItemDefinition() // 현재 아이템 정의 반환
{
    return currentItemDefinition;
}

public int GetCurrentCount() // 현재 아이템 개수 반환
{
    return currentCount;
}

public void HideItemVisualOnly() // 드래그 중 원본 슬롯 표시만 숨김
{
    if (itemImage != null)
    {
        itemImage.sprite = null;
        itemImage.enabled = false;
    }

    if (countText != null)
    {
        countText.text = "";
    }
}

public void RestoreItemVisualOnly() // 드래그 취소 시 원본 슬롯 표시 복구
{
    if (itemImage != null)
    {
        itemImage.sprite = currentItemDefinition != null ? currentItemDefinition.itemImage : null;
        itemImage.enabled = currentItemDefinition != null && currentItemDefinition.itemImage != null;
    }

    if (countText != null)
    {
        countText.text = currentCount > 1 ? currentCount.ToString() : "";
    }
}

public void OnBeginDrag(PointerEventData eventData) // 드래그 시작
{
    if (inventoryManager == null || currentItemDefinition == null || currentCount <= 0)
        return;

    inventoryManager.BeginItemDrag(this, currentItemDefinition, currentCount);
}

public void OnDrag(PointerEventData eventData) // 드래그 중
{
    if (inventoryManager == null)
        return;

    inventoryManager.UpdateDragSlotPosition(eventData.position);
}

public void OnEndDrag(PointerEventData eventData) // 드래그 종료
{
    if (inventoryManager == null)
        return;

    inventoryManager.CancelDragIfNotDropped();
}
}