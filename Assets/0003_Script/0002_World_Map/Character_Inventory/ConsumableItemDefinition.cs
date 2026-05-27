using UnityEngine;

/// <summary>
/// 소모 아이템의 적용 효과와 버튼 프리팹을 정의하는 ScriptableObject입니다.
/// </summary>
[CreateAssetMenu(fileName = "ConsumableItemDefinition", menuName = "Game Data/Consumable Item Definition")]
public class ConsumableItemDefinition : ScriptableObject
{
    [Header("대상 아이템")]
    public GlobalItemDefinition targetItemDefinition; // 해당될 아이템 정의 에셋

    [Header("소모 적용값")]
    public int applyHealthValue; // 소모 시 적용될 체력값
    public int applyHungerValue; // 소모 시 적용될 허기값

    [Header("흑체 적용")]
    public bool useNigrumValue; // 흑체 수용값 적용 여부
    public int applyNigrumCapacityValue; // 소모 시 적용될 흑체 수용값

    [Header("버튼 프리팹")]
    public GameObject consumeButtonPrefab; // 버튼 목록 오브젝트에 생성될 소모 버튼 프리팹
}