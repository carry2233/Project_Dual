using TMPro; // TMP 텍스트 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.UI; // Image 사용

/// <summary>
/// 이벤트 보상 아이템을 표시하는 슬롯입니다.
/// </summary>
public class EventSlot : MonoBehaviour
{
    [Header("표시 컴포넌트")]
    [SerializeField] private TextMeshProUGUI itemText; // 아이템 이름과 획득 수 표시 텍스트
    [SerializeField] private Image itemImage; // 아이템 이미지 표시 컴포넌트

public void SetSlot(GlobalItemDefinition itemDefinition, int itemCount) // 슬롯 정보 설정
{
    if (itemDefinition == null)
    {
        ClearSlot(); // 아이템이 없으면 슬롯 초기화
        return;
    }

    if (itemImage != null)
    {
        itemImage.sprite = itemDefinition.itemImage; // 아이템 이미지 적용
        itemImage.enabled = itemDefinition.itemImage != null; // 이미지가 있을 때만 표시
    }

    if (itemText != null)
    {
        itemText.text = $"{itemDefinition.itemName} +{itemCount}"; // 설정된 아이템 이름과 획득 수 표시
    }
}

    public void ClearSlot() // 슬롯 표시 초기화
    {
        if (itemImage != null)
        {
            itemImage.sprite = null; // 이미지 제거
            itemImage.enabled = false; // 이미지 비활성화
        }

        if (itemText != null)
        {
            itemText.text = ""; // 텍스트 초기화
        }
    }
}