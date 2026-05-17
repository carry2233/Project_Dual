using UnityEngine;

/// <summary>
/// 전역 아이템 정보를 ScriptableObject로 정의하는 스크립트입니다.
/// </summary>
[CreateAssetMenu(fileName = "GlobalItemDefinition", menuName = "Inventory/Global Item Definition")]
public class GlobalItemDefinition : ScriptableObject
{
    [Header("아이템 ID")]
    public int itemAID; // 아이템 A ID
    public int itemBID; // 아이템 B ID

[Header("아이템 표시 정보")]
public string itemName; // 아이템 이름
public Sprite itemImage; // 아이템 이미지
public GameObject rewardItemDisplaySlotPrefab; // 획득 아이템 표시 슬롯 프리팹

[Header("아이템 수치 정보")]
public int maxDurability; // 최대 내구도
public int maxStackPerSlot; // 한 칸당 최대 저장 수
public float weightPerItemKg; // 한 개당 무게
public int displayPriority; // 나열 우선순위값
public int itemValue; // 아이템 가치값
}