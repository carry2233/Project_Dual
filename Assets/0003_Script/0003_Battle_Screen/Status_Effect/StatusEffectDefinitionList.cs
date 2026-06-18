using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상태효과 정의 리스트
/// - StatusEffectDefinitionSO들을 모아두고 ID로 찾는 관리자.
/// - DontDestroyOnLoad 싱글톤으로 유지된다.
/// </summary>
public class StatusEffectDefinitionList : MonoBehaviour
{
    public static StatusEffectDefinitionList Instance { get; private set; }

    [Header("상태효과 정의 리스트")]
    [SerializeField] private List<StatusEffectDefinitionSO> statusEffectDefinitionList = new List<StatusEffectDefinitionSO>();

    public IReadOnlyList<StatusEffectDefinitionSO> StatusEffectDefinitions => statusEffectDefinitionList;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public StatusEffectDefinitionSO GetStatusEffectDefinitionByID(int statusEffectID)
    {
        for (int i = 0; i < statusEffectDefinitionList.Count; i++)
        {
            StatusEffectDefinitionSO definition = statusEffectDefinitionList[i];

            if (definition == null)
            {
                continue;
            }

            if (definition.StatusEffectID == statusEffectID)
            {
                return definition;
            }
        }

        return null;
    }

    public bool HasStatusEffectDefinition(int statusEffectID)
    {
        return GetStatusEffectDefinitionByID(statusEffectID) != null;
    }

    public string GetStatusEffectNameByID(int statusEffectID) // 상태효과 ID로 이름 반환
{
    StatusEffectDefinitionSO definition = GetStatusEffectDefinitionByID(statusEffectID); // 상태효과 정의 검색
    return definition != null ? definition.StatusEffectName : string.Empty; // 이름 반환
}

public Sprite GetStatusEffectIconByID(int statusEffectID) // 상태효과 ID로 아이콘 반환
{
    StatusEffectDefinitionSO definition = GetStatusEffectDefinitionByID(statusEffectID); // 상태효과 정의 검색
    return definition != null ? definition.StatusEffectIcon : null; // 아이콘 반환
}

public string GetStatusEffectShortDescriptionByID(int statusEffectID) // 상태효과 ID로 짧은 설명 반환
{
    StatusEffectDefinitionSO definition = GetStatusEffectDefinitionByID(statusEffectID); // 상태효과 정의 검색
    return definition != null ? definition.StatusEffectShortDescription : string.Empty; // 짧은 설명 반환
}

public string GetStatusEffectDescriptionByID(int statusEffectID) // 상태효과 ID로 상세 설명 반환
{
    StatusEffectDefinitionSO definition = GetStatusEffectDefinitionByID(statusEffectID); // 상태효과 정의 검색
    return definition != null ? definition.StatusEffectDescription : string.Empty; // 상세 설명 반환
}

public int GetStatusEffectSortPriorityByID(int statusEffectID) // 상태효과 ID로 슬롯 정렬 우선순위 반환
{
    StatusEffectDefinitionSO definition = GetStatusEffectDefinitionByID(statusEffectID); // 상태효과 정의 검색
    return definition != null ? definition.StatusEffectSlotSortPriority : 9999; // 우선순위 반환
}











}