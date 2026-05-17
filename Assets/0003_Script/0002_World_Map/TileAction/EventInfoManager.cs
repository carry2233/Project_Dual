using System.Collections.Generic; // List 사용
using UnityEngine; // Unity 기본 기능 사용

/// <summary>
/// 이벤트 정의 목록을 전역으로 보관하는 관리자입니다.
/// </summary>
public class EventInfoManager : MonoBehaviour
{
    public static EventInfoManager Instance { get; private set; } // 전역 접근 인스턴스

    [Header("이벤트 정의 목록")]
    public List<EventInfoDefinition> eventInfoDefinitionList = new List<EventInfoDefinition>(); // 이벤트 정의 리스트

    private void Awake() // 싱글톤 초기화
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // 중복 매니저 제거
            return;
        }

        Instance = this; // 인스턴스 등록
        DontDestroyOnLoad(gameObject); // 씬 전환 시 유지
    }
}