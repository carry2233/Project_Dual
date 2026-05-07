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
}