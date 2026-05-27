using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전역 아이템 정의 목록을 보관하고 ID 기준으로 아이템 정보를 찾는 스크립트입니다.
/// </summary>
public class ItemDefinitionList : MonoBehaviour
{
    public static ItemDefinitionList Instance; // 전역 접근 인스턴스

    [Header("아이템 정의 목록")]
    public List<GlobalItemDefinition> itemDefinitions = new List<GlobalItemDefinition>(); // 전역 아이템 정의 리스트

    [Header("소모 아이템 정의 목록")]
public List<ConsumableItemDefinition> consumableItemDefinitions = new List<ConsumableItemDefinition>(); // 소모 아이템 정의 리스트

    private void Awake() // 오브젝트 초기화
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public GlobalItemDefinition GetItemDefinition(int itemAID, int itemBID) // 아이템 ID로 아이템 정의 찾기
    {
        for (int i = 0; i < itemDefinitions.Count; i++)
        {
            GlobalItemDefinition itemDefinition = itemDefinitions[i];

            if (itemDefinition == null)
                continue;

            if (itemDefinition.itemAID == itemAID && itemDefinition.itemBID == itemBID)
                return itemDefinition;
        }

        return null;
    }

    public ConsumableItemDefinition GetConsumableItemDefinition(GlobalItemDefinition itemDefinition) // 아이템 정의로 소모 아이템 정의 찾기
{
    if (itemDefinition == null)
    {
        return null; // 대상 아이템이 없으면 null 반환
    }

    for (int i = 0; i < consumableItemDefinitions.Count; i++)
    {
        ConsumableItemDefinition consumableDefinition = consumableItemDefinitions[i]; // 현재 소모 아이템 정의 참조

        if (consumableDefinition == null || consumableDefinition.targetItemDefinition == null)
        {
            continue; // 비어 있으면 건너뜀
        }

        bool isSameItem =
            consumableDefinition.targetItemDefinition.itemAID == itemDefinition.itemAID &&
            consumableDefinition.targetItemDefinition.itemBID == itemDefinition.itemBID; // 같은 아이템인지 확인

        if (isSameItem)
        {
            return consumableDefinition; // 소모 아이템 정의 반환
        }
    }

    return null; // 찾지 못하면 null 반환
}







}