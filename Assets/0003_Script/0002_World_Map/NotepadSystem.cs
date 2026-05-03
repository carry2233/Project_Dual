using System.Collections.Generic; // 리스트 사용
using TMPro; // TMP 입력필드/텍스트 사용
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // 버튼 사용

/// <summary>
/// 메모장 UI 열기/닫기, 앞/뒤 페이지 이동, 페이지 텍스트 표시 및 SaveStorage 저장 갱신을 담당한다.
/// </summary>
public class NotepadSystem : MonoBehaviour
{
    [Header("메모장 UI 참조")]
    [SerializeField] private GameObject notepadObject; // 메모장 전체 UI 오브젝트

    [Header("메모장 활성화 버튼 목록")]
    [SerializeField] private List<Button> openButtonList = new List<Button>(); // 메모장 열기 버튼 목록

    [Header("메모장 비활성화 버튼 목록")]
    [SerializeField] private List<Button> closeButtonList = new List<Button>(); // 메모장 닫기 버튼 목록

    [Header("페이지 이동 버튼")]
    [SerializeField] private Button previousPageButton; // 이전 페이지 묶음 이동 버튼
    [SerializeField] private Button nextPageButton; // 다음 페이지 묶음 이동 버튼

    [Header("페이지 입력필드")]
    [SerializeField] private TMP_InputField frontPageInputField; // 앞페이지 텍스트 입력필드
    [SerializeField] private TMP_InputField backPageInputField; // 뒤페이지 텍스트 입력필드

    [Header("페이지 표시 텍스트")]
    [SerializeField] private TMP_Text frontPageIndexText; // 앞페이지 번호 표시 텍스트
    [SerializeField] private TMP_Text backPageIndexText; // 뒤페이지 번호 표시 텍스트

    [Header("페이지 설정")]
    [SerializeField] private int startFrontPageIndex = 1; // 시작 앞페이지 인덱스

    private SaveStorage saveStorage; // 저장 데이터 관리 스크립트
    private int currentFrontPageIndex = 1; // 현재 앞페이지 인덱스

    private void Awake() // 시작 전 참조 초기화
    {
        saveStorage = SaveStorage.Instance != null ? SaveStorage.Instance : FindFirstObjectByType<SaveStorage>(); // SaveStorage 자동 참조
        currentFrontPageIndex = Mathf.Max(1, startFrontPageIndex); // 시작 페이지 보정
        ApplyNotepadState(false); // 시작 시 메모장 닫힘 적용
    }

    private void OnEnable() // 활성화 시 버튼 이벤트 등록
    {
        AddButtonEvents(); // 버튼 이벤트 연결
    }

    private void OnDisable() // 비활성화 시 버튼 이벤트 해제
    {
        SaveCurrentPageTexts(); // 비활성화 직전 현재 입력 내용 저장
        RemoveButtonEvents(); // 버튼 이벤트 해제
    }

    private void AddButtonEvents() // 버튼 이벤트 등록
    {
        for (int i = 0; i < openButtonList.Count; i++)
        {
            if (openButtonList[i] != null)
            {
                openButtonList[i].onClick.AddListener(OpenNotepad); // 열기 버튼 연결
            }
        }

        for (int i = 0; i < closeButtonList.Count; i++)
        {
            if (closeButtonList[i] != null)
            {
                closeButtonList[i].onClick.AddListener(CloseNotepad); // 닫기 버튼 연결
            }
        }

        if (previousPageButton != null)
        {
            previousPageButton.onClick.AddListener(MovePreviousPages); // 이전 페이지 버튼 연결
        }

        if (nextPageButton != null)
        {
            nextPageButton.onClick.AddListener(MoveNextPages); // 다음 페이지 버튼 연결
        }
    }

    private void RemoveButtonEvents() // 버튼 이벤트 해제
    {
        for (int i = 0; i < openButtonList.Count; i++)
        {
            if (openButtonList[i] != null)
            {
                openButtonList[i].onClick.RemoveListener(OpenNotepad); // 열기 버튼 해제
            }
        }

        for (int i = 0; i < closeButtonList.Count; i++)
        {
            if (closeButtonList[i] != null)
            {
                closeButtonList[i].onClick.RemoveListener(CloseNotepad); // 닫기 버튼 해제
            }
        }

        if (previousPageButton != null)
        {
            previousPageButton.onClick.RemoveListener(MovePreviousPages); // 이전 페이지 버튼 해제
        }

        if (nextPageButton != null)
        {
            nextPageButton.onClick.RemoveListener(MoveNextPages); // 다음 페이지 버튼 해제
        }
    }

    public void OpenNotepad() // 메모장 열기
    {
        ApplyNotepadState(true); // 메모장 열림 상태 적용
        LoadCurrentPageTexts(); // 현재 페이지 텍스트 불러오기
    }

    public void CloseNotepad() // 메모장 닫기
    {
        SaveCurrentPageTexts(); // 닫기 전 현재 페이지 텍스트 저장
        ApplyNotepadState(false); // 메모장 닫힘 상태 적용
    }

    private void MovePreviousPages() // 이전 페이지 묶음으로 이동
    {
        SaveCurrentPageTexts(); // 이동 전 현재 페이지 저장
        currentFrontPageIndex = Mathf.Max(1, currentFrontPageIndex - 2); // 앞페이지 기준 2 감소
        LoadCurrentPageTexts(); // 이동한 페이지 텍스트 불러오기
    }

    private void MoveNextPages() // 다음 페이지 묶음으로 이동
    {
        SaveCurrentPageTexts(); // 이동 전 현재 페이지 저장
        currentFrontPageIndex += 2; // 앞페이지 기준 2 증가
        LoadCurrentPageTexts(); // 이동한 페이지 텍스트 불러오기
    }

    private void LoadCurrentPageTexts() // 현재 페이지 인덱스 기준 텍스트 표시
    {
        if (saveStorage == null)
        {
            saveStorage = SaveStorage.Instance != null ? SaveStorage.Instance : FindFirstObjectByType<SaveStorage>(); // SaveStorage 재참조
        }

        int frontPageIndex = currentFrontPageIndex; // 앞페이지 인덱스
        int backPageIndex = currentFrontPageIndex + 1; // 뒤페이지 인덱스

        string frontPageText = saveStorage == null ? string.Empty : saveStorage.GetCurrentSelectedNotepadPageText(frontPageIndex); // 앞페이지 저장 텍스트
        string backPageText = saveStorage == null ? string.Empty : saveStorage.GetCurrentSelectedNotepadPageText(backPageIndex); // 뒤페이지 저장 텍스트

        if (frontPageInputField != null)
        {
            frontPageInputField.SetTextWithoutNotify(frontPageText); // 앞페이지 입력필드에 텍스트 표시
        }

        if (backPageInputField != null)
        {
            backPageInputField.SetTextWithoutNotify(backPageText); // 뒤페이지 입력필드에 텍스트 표시
        }

        RefreshPageIndexTexts(); // 페이지 번호 표시 갱신
    }

    private void SaveCurrentPageTexts() // 현재 입력필드 내용을 SaveStorage에 저장
    {
        if (saveStorage == null)
        {
            saveStorage = SaveStorage.Instance != null ? SaveStorage.Instance : FindFirstObjectByType<SaveStorage>(); // SaveStorage 재참조
        }

        if (saveStorage == null) return; // 저장 참조 없으면 종료

        int frontPageIndex = currentFrontPageIndex; // 앞페이지 인덱스
        int backPageIndex = currentFrontPageIndex + 1; // 뒤페이지 인덱스

        string frontText = frontPageInputField == null ? string.Empty : frontPageInputField.text; // 앞페이지 입력 텍스트
        string backText = backPageInputField == null ? string.Empty : backPageInputField.text; // 뒤페이지 입력 텍스트

        saveStorage.SetCurrentSelectedNotepadPageText(frontPageIndex, frontText); // 앞페이지 저장
        saveStorage.SetCurrentSelectedNotepadPageText(backPageIndex, backText); // 뒤페이지 저장
    }

    private void RefreshPageIndexTexts() // 페이지 표시 텍스트 갱신
    {
        int frontPageIndex = currentFrontPageIndex; // 앞페이지 인덱스
        int backPageIndex = currentFrontPageIndex + 1; // 뒤페이지 인덱스

        if (frontPageIndexText != null)
        {
            frontPageIndexText.text = $"-{frontPageIndex}-"; // 앞페이지 번호 표시
        }

        if (backPageIndexText != null)
        {
            backPageIndexText.text = $"-{backPageIndex}-"; // 뒤페이지 번호 표시
        }
    }

    private void ApplyNotepadState(bool isOpen) // 메모장 열림/닫힘 상태 적용
    {
        if (notepadObject != null)
        {
            notepadObject.SetActive(isOpen); // 메모장 UI 활성 상태 적용
        }

        for (int i = 0; i < openButtonList.Count; i++)
        {
            if (openButtonList[i] != null)
            {
                openButtonList[i].gameObject.SetActive(!isOpen); // 열기 버튼은 메모장 닫힘일 때 표시
            }
        }

        for (int i = 0; i < closeButtonList.Count; i++)
        {
            if (closeButtonList[i] != null)
            {
                closeButtonList[i].gameObject.SetActive(isOpen); // 닫기 버튼은 메모장 열림일 때 표시
            }
        }
    }
}