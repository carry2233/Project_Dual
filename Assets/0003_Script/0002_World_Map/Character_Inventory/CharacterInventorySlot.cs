using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 인벤토리 선택용 UI 슬롯을 담당하는 스크립트입니다.
/// </summary>
public class CharacterInventorySlot : MonoBehaviour
{
    [Header("버튼")]
    public Button selectButton; // 캐릭터 인벤토리 선택 버튼

    [Header("나열 정보")]
    public int displayPriority; // 낮을수록 먼저 나열되는 우선순위값

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

        inventoryManager.SelectCharacterInventory(targetInventory);
    }
}