using UnityEngine;

/// <summary>
/// 아군 캐릭터 전용 정보를 정의하는 ScriptableObject입니다.
/// </summary>
[CreateAssetMenu(fileName = "NewFriendlyCharacterDefinition", menuName = "Project Dual/아군 캐릭터 정의")]
public class FriendlyCharacterDefinition : ScriptableObject
{
    [Header("담당 캐릭터 전역 정의")]
    public GlobalCharacterDefinition globalCharacterDefinition; // 연결된 전역 캐릭터 정의

    [Header("인벤토리 슬롯 나열 정보")]
    public int slotDisplayPriority; // 낮을수록 먼저 나열되는 우선순위값

    [Header("적정무게 정보")]
    public float properWeightKg; // 해당 캐릭터의 적정 무게값

    public bool IsMatch(int targetFirstRowID, int targetSecondRowID) // 캐릭터 ID 일치 여부 확인
    {
        if (globalCharacterDefinition == null)
            return false;

        return globalCharacterDefinition.IsMatch(targetFirstRowID, targetSecondRowID);
    }
}