using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 캐릭터 한 명의 인벤토리 정보를 보관하고 총 무게를 계산하는 스크립트입니다.
/// </summary>
public class CharacterInventory : MonoBehaviour
{
    [Header("담당 캐릭터 ID")]
    public int firstRowID; // 캐릭터 첫 번째 행 ID
    public int secondRowID; // 캐릭터 두 번째 행 ID
    public int individualID; // 캐릭터 개체별 고유 ID

    [Header("아이템 저장 목록")]
    public List<CharacterInventoryItemData> storedItems = new List<CharacterInventoryItemData>(); // 저장된 아이템 목록

    [Header("총 무게")]
    public float totalWeightKg; // 모든 아이템의 총 무게

    [Header("무게 초과 정보")]
public float overweightKg; // 적정 무게를 초과한 무게값
public int overweightStackCount; // 초과 무게에 따른 중첩 수

private SaveStorage saveStorage; // 무게 디버프 저장용 SaveStorage 참조

    public void SetCharacterID(int newFirstRowID, int newSecondRowID, int newIndividualID) // 담당 캐릭터 ID 설정
    {
        firstRowID = newFirstRowID;
        secondRowID = newSecondRowID;
        individualID = newIndividualID;
    }

    public void SetItems(List<CharacterInventoryItemData> newItems) // 인벤토리 아이템 목록 설정
    {
        storedItems.Clear();

        if (newItems != null)
            storedItems.AddRange(newItems);

        RefreshTotalWeight();
    }

    public void RefreshTotalWeight() // 총 무게 다시 계산
    {
        totalWeightKg = 0f;

        for (int i = 0; i < storedItems.Count; i++)
        {
            CharacterInventoryItemData itemData = storedItems[i];

            if (itemData == null || itemData.itemDefinition == null)
                continue;

            itemData.totalWeightKg = itemData.itemDefinition.weightPerItemKg * itemData.totalCount;
            totalWeightKg += itemData.totalWeightKg;
        }
    }

    public bool IsSameCharacter(int targetFirstRowID, int targetSecondRowID, int targetIndividualID) // 같은 캐릭터인지 확인
    {
        return firstRowID == targetFirstRowID
            && secondRowID == targetSecondRowID
            && individualID == targetIndividualID;
    }

    public bool RemoveItem(GlobalItemDefinition itemDefinition, int removeCount) // 아이템 제거
{
    if (itemDefinition == null || removeCount <= 0)
        return false;

    for (int i = 0; i < storedItems.Count; i++)
    {
        CharacterInventoryItemData itemData = storedItems[i];

        if (itemData == null || itemData.itemDefinition == null)
            continue;

        bool isSameItem =
            itemData.itemDefinition.itemAID == itemDefinition.itemAID &&
            itemData.itemDefinition.itemBID == itemDefinition.itemBID;

        if (!isSameItem)
            continue;

        if (itemData.totalCount < removeCount)
            return false;

        itemData.totalCount -= removeCount;

        if (itemData.totalCount <= 0)
            storedItems.RemoveAt(i);

        RefreshTotalWeight();
        return true;
    }

    return false;
}

public void AddOrMergeItem(GlobalItemDefinition itemDefinition, int addCount) // 같은 ID 아이템에 합치거나 새로 추가
{
    if (itemDefinition == null || addCount <= 0)
        return;

    for (int i = 0; i < storedItems.Count; i++)
    {
        CharacterInventoryItemData itemData = storedItems[i];

        if (itemData == null || itemData.itemDefinition == null)
            continue;

        bool isSameItem =
            itemData.itemDefinition.itemAID == itemDefinition.itemAID &&
            itemData.itemDefinition.itemBID == itemDefinition.itemBID;

        if (!isSameItem)
            continue;

        itemData.totalCount += addCount;
        RefreshTotalWeight();
        return;
    }

    CharacterInventoryItemData newItemData = new CharacterInventoryItemData
    {
        itemDefinition = itemDefinition,
        totalCount = addCount,
        totalWeightKg = itemDefinition.weightPerItemKg * addCount
    };

    storedItems.Add(newItemData);
    RefreshTotalWeight();
}

public void SetOverweightState(float properWeightKg, int weightPerStack) // 초과 무게와 중첩 수 갱신
{
    RefreshTotalWeight(); // 총 무게 다시 계산

    overweightKg = Mathf.Max(0f, totalWeightKg - properWeightKg); // 초과 무게 계산

    if (weightPerStack <= 0)
    {
        overweightStackCount = 0; // 잘못된 중첩 기준이면 0 처리
        SyncOverweightStackCountToSaveStorage(); // 저장 데이터에 중첩값 반영
        return;
    }

    overweightStackCount = Mathf.FloorToInt(overweightKg / weightPerStack); // 초과 무게 중첩 계산
    SyncOverweightStackCountToSaveStorage(); // 저장 데이터에 중첩값 반영
}

private void SyncOverweightStackCountToSaveStorage() // 현재 무게 디버프 중첩값을 SaveStorage에 반영
{
    if (saveStorage == null)
    {
        saveStorage = SaveStorage.Instance != null ? SaveStorage.Instance : FindFirstObjectByType<SaveStorage>(); // 저장소 참조 보정
    }

    if (saveStorage == null)
    {
        return; // 저장소가 없으면 종료
    }

    saveStorage.SetCurrentOwnedCharacterOverweightStackCount(
        firstRowID,
        secondRowID,
        individualID,
        overweightStackCount); // 현재 캐릭터 저장 스탯에 무게 디버프 중첩값 저장
}














}

[Serializable]
public class CharacterInventoryItemData
{
    [Header("아이템 정의")]
    public GlobalItemDefinition itemDefinition; // 저장한 아이템 정의

    [Header("저장 정보")]
    public int totalCount; // 총 저장 개수
    public float totalWeightKg; // 총 무게
}