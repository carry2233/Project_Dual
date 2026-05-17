using UnityEngine; // Unity 기본 기능 사용

/// <summary>
/// 이벤트 1개의 기본 정보를 정의하는 ScriptableObject입니다.
/// </summary>
[CreateAssetMenu(fileName = "NewEventInfoDefinition", menuName = "Project Dual/이벤트 정보 정의")]
public class EventInfoDefinition : ScriptableObject
{
    public enum EventType
    {
        Positive, // 긍정적 이벤트
        Negative, // 부정적 이벤트
        Mixed // 복합적 이벤트
    }

    [Header("이벤트 식별 정보")]
    public int eventID; // 이벤트 ID

    [Header("이벤트 타입")]
    public EventType eventType; // 이벤트 타입

    [Header("이벤트 등장 설정")]
    public int eventWeight = 1; // 이벤트 등장 가중치

    [Header("이벤트 비주얼")]
    public GameObject eventVisualPrefab; // 이벤트 발생 시 생성할 비주얼 프리팹
}