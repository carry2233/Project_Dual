using UnityEngine; // Unity 기본 기능

/// <summary>
/// 캐릭터 관리창 슬롯 정보 스크립트
/// - 관리창 캐릭터 슬롯 프리팹에 붙임
/// - 슬롯 나열 우선순위값을 제공
/// - CharacterManagementManager가 이 값을 기준으로 작은 순서대로 배치
/// </summary>
public class CharacterManagementSlot : MonoBehaviour
{
    [Header("슬롯 정렬 설정")]
    [SerializeField] private int slotSortPriority = 0; // 슬롯 나열 우선순위값

    public int SlotSortPriority => slotSortPriority; // 슬롯 나열 우선순위값 반환
}