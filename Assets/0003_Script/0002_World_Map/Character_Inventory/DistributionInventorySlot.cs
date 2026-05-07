using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 분포 인벤토리 안에서 아이템 이미지와 개수를 표시하는 슬롯 스크립트입니다.
/// </summary>
public class DistributionInventorySlot : MonoBehaviour
{
    [Header("표시 UI")]
    public Image itemImage; // 아이템 이미지 UI
    public TMP_Text countText; // 아이템 개수 텍스트

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
}