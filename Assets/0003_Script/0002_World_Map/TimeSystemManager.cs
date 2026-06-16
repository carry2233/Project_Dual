using TMPro; // TMP 텍스트 사용
using UnityEngine; // Unity 기본 네임스페이스

/// <summary>
/// 현재 선택된 세이브의 시간/분/일차를 표시하고 갱신하는 시간 시스템 관리자
/// </summary>
public class TimeSystemManager : MonoBehaviour
{
    public enum TimeDisplayMode
    {
        TwentyFourHour, // 24시간제
        AmPm // 오전/오후 표시
    }

    [Header("저장 참조")]
    [SerializeField] private SaveStorage saveStorage; // 저장 데이터 관리 스크립트

    [Header("시간 표시 UI")]
    [SerializeField] private TMP_Text timeText; // 시간 표시 TMP 텍스트
    [SerializeField] private TMP_Text dayText; // 일차 표시 TMP 텍스트

    [Header("시간 표시 방식")]
    [SerializeField] private TimeDisplayMode timeDisplayMode = TimeDisplayMode.TwentyFourHour; // 시간 표시 방식

    private void Start() // 씬 시작 시 저장 참조 탐색 및 UI 갱신
    {
        FindSaveStorageIfNeeded(); // SaveStorage 자동 참조
        RefreshTimeUI(); // 시간 UI 갱신
    }

    private void FindSaveStorageIfNeeded() // SaveStorage 참조가 없으면 씬에서 탐색
    {
        if (saveStorage != null)
        {
            return; // 이미 참조가 있으면 종료
        }

        saveStorage = SaveStorage.Instance != null ? SaveStorage.Instance : FindFirstObjectByType<SaveStorage>(); // 인스턴스 우선 탐색
    }

    public void AddMinute(int addMinute) // 현재 선택 저장본 시간에 분 추가
    {
        if (saveStorage == null)
        {
            FindSaveStorageIfNeeded(); // 저장 참조 재탐색
        }

        if (saveStorage == null) return; // 저장 참조 없으면 종료

        bool result = saveStorage.AddMinutesToCurrentSelectedTime(addMinute); // 저장 시간 증가
        if (!result) return; // 실패 시 종료

    RefreshTimeUI();

    if (saveStorage != null)
    {
        saveStorage.CheckCurrentSaveAllFriendlyDeadAndMoveIfNeeded();
    }
    
    }

    public void RefreshTimeUI() // 현재 선택 저장본 기준 시간 UI 갱신
    {
        if (saveStorage == null)
        {
            FindSaveStorageIfNeeded(); // 저장 참조 재탐색
        }

        if (saveStorage == null) return; // 저장 참조 없으면 종료

        bool result = saveStorage.TryGetCurrentSelectedTime(out int day, out int hour, out int minute); // 현재 시간 가져오기
        if (!result) return; // 선택 저장본 없으면 종료

        if (timeText != null)
        {
            timeText.text = GetFormattedTimeText(hour, minute); // 시간 텍스트 적용
        }

        if (dayText != null)
        {
            dayText.text = $"{day}일차"; // 일차 텍스트 적용
        }
    }

    private string GetFormattedTimeText(int hour, int minute) // 설정된 방식에 맞는 시간 문자열 반환
    {
        if (timeDisplayMode == TimeDisplayMode.TwentyFourHour)
        {
            return $"{hour:00} : {minute:00} : 00"; // 24시간제 표시
        }

        string meridiemText = hour < 12 ? "오전" : "오후"; // 오전/오후 계산
        int displayHour = hour % 12; // 12시간제 시간 계산

        if (displayHour == 0)
        {
            displayHour = 12; // 0시는 12로 표시
        }

        return $"{meridiemText} {displayHour:00} : {minute:00} : 00"; // 오전/오후 표시
    }
}