using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveSelection : MonoBehaviour
{
    [Header("저장 참조")]
    [SerializeField] private SaveStorage saveStorage; // 저장 데이터 관리 스크립트
    [SerializeField] private SaveSlotButton saveSlotButtonPrefab; // 저장본 버튼 프리팹
    [SerializeField] private RectTransform contentRectTransform; // Scroll View Content
    [SerializeField] private GridLayoutGroup targetGridLayoutGroup; // Content에 적용된 Grid Layout Group

    [Header("저장본 생성 UI")]
    [SerializeField] private Button createSaveButton; // 세이브 생성 버튼
    [SerializeField] private GameObject saveNameInputUI; // 이름 작성 UI창
    [SerializeField] private TMP_InputField saveNameInputField; // 저장본 이름 입력 필드
    [SerializeField] private Button createConfirmButton; // 생성 실행 버튼
    [SerializeField] private Button createCancelButton; // 생성 취소 버튼

    [Header("빈 입력 경고 UI")]
    [SerializeField] private GameObject emptyNameWarningUI; // 빈 이름 입력 시 보여줄 UI
    [SerializeField] private float emptyNameWarningDuration = 1.5f; // 경고 UI 활성화 시간

    [Header("저장본 삭제 UI")]
    [SerializeField] private GameObject deleteConfirmUI; // 저장본 삭제 확인 UI
    [SerializeField] private Button deleteCancelButton; // 삭제 취소 버튼
    [SerializeField] private Button deleteExecuteButton; // 삭제 실행 버튼

    [Header("모달 UI 중 클릭 차단할 버튼들")]
    [SerializeField] private List<Button> blockedButtonList = new List<Button>(); // 모달 UI가 열려 있을 때 클릭 차단할 버튼들

    private readonly List<SaveSlotButton> createdSlotButtonList = new List<SaveSlotButton>(); // 생성된 저장본 버튼 목록
    private Coroutine emptyNameWarningCoroutine; // 빈 입력 경고 UI 코루틴
    private SaveSlotButton pendingDeleteSlotButton; // 삭제 대기 중인 저장본 버튼

    private void Awake() // 시작 전 기본 UI 상태 초기화
    {
        SetInitialUIState(); // 기본 UI 상태 적용
    }

private void Start() // 시작 시 저장본 목록 구성
{
    RefreshSaveSlotList(); // 저장본 버튼 목록 새로 생성
    UpdateCreateSaveButtonState(); // 생성 버튼 상태 갱신
    UpdateBlockedButtonsState(); // 모달 UI 상태에 따른 버튼 클릭 가능 여부 갱신
}

    private void OnEnable() // 활성화 시 버튼 이벤트 연결
    {
        AddButtonEvents(); // 버튼 리스너 등록
    }

    private void OnDisable() // 비활성화 시 버튼 이벤트 해제
    {
        RemoveButtonEvents(); // 버튼 리스너 제거
    }

    private void SetInitialUIState() // 시작 기본 UI 설정
    {
        if (saveNameInputUI != null)
        {
            saveNameInputUI.SetActive(false); // 이름 입력 UI 비활성화
        }

        if (deleteConfirmUI != null)
        {
            deleteConfirmUI.SetActive(false); // 삭제 확인 UI 비활성화
        }

        if (emptyNameWarningUI != null)
        {
            emptyNameWarningUI.SetActive(false); // 빈 입력 경고 UI 비활성화
        }
    }

private void AddButtonEvents() // 버튼 이벤트 등록
{
    if (createSaveButton != null)
    {
        createSaveButton.onClick.AddListener(OpenSaveNameInputUI); // 생성 버튼 클릭 시 이름 입력 UI 열기
    }

    if (createConfirmButton != null)
    {
        createConfirmButton.onClick.AddListener(TryCreateSave); // 생성 실행 버튼 클릭 시 저장본 생성 시도
    }

    if (createCancelButton != null)
    {
        createCancelButton.onClick.AddListener(CloseSaveNameInputUI); // 생성 취소 버튼 클릭 시 이름 입력 UI 닫기
    }

    if (deleteCancelButton != null)
    {
        deleteCancelButton.onClick.AddListener(CloseDeleteConfirmUI); // 삭제 취소 버튼 클릭 시 삭제 UI 닫기
    }

    if (deleteExecuteButton != null)
    {
        deleteExecuteButton.onClick.AddListener(ExecuteDeletePendingSave); // 삭제 실행 버튼 클릭 시 저장본 삭제
    }
}

private void RemoveButtonEvents() // 버튼 이벤트 해제
{
    if (createSaveButton != null)
    {
        createSaveButton.onClick.RemoveListener(OpenSaveNameInputUI); // 생성 버튼 이벤트 해제
    }

    if (createConfirmButton != null)
    {
        createConfirmButton.onClick.RemoveListener(TryCreateSave); // 생성 실행 버튼 이벤트 해제
    }

    if (createCancelButton != null)
    {
        createCancelButton.onClick.RemoveListener(CloseSaveNameInputUI); // 생성 취소 버튼 이벤트 해제
    }

    if (deleteCancelButton != null)
    {
        deleteCancelButton.onClick.RemoveListener(CloseDeleteConfirmUI); // 삭제 취소 버튼 이벤트 해제
    }

    if (deleteExecuteButton != null)
    {
        deleteExecuteButton.onClick.RemoveListener(ExecuteDeletePendingSave); // 삭제 실행 버튼 이벤트 해제
    }
}

public void RefreshSaveSlotList() // 저장본 버튼 목록 다시 생성
{
    ClearCreatedSlotButtons(); // 기존 버튼 제거

    if (saveStorage == null || saveSlotButtonPrefab == null || contentRectTransform == null)
    {
        UpdateContentHeight(0); // 참조 부족 시 Content 높이 최소 보정
        return; // 생성 종료
    }

    List<SaveStorage.SaveEntry> saveList = saveStorage.GetSaveList(); // 저장본 목록 가져오기

    for (int i = 0; i < saveList.Count; i++)
    {
        SaveStorage.SaveEntry entry = saveList[i]; // 현재 저장본 데이터 참조

        SaveSlotButton newSlotButton = Instantiate(saveSlotButtonPrefab, contentRectTransform); // 저장본 버튼 생성
        newSlotButton.Initialize(this, entry.saveName, entry.saveNumber); // 저장본 버튼 초기화

        createdSlotButtonList.Add(newSlotButton); // 생성 버튼 목록에 추가
    }

    UpdateContentHeight(saveList.Count); // 저장본 개수에 맞게 Content 높이 조절
    UpdateCreateSaveButtonState(); // 생성 버튼 상태 갱신
    UpdateBlockedButtonsState(); // 현재 모달 UI 상태를 새로 생성된 버튼들에도 반영
}
    private void ClearCreatedSlotButtons() // 기존 저장본 버튼 제거
    {
        for (int i = 0; i < createdSlotButtonList.Count; i++)
        {
            if (createdSlotButtonList[i] == null) continue; // 비어 있으면 건너뜀

            Destroy(createdSlotButtonList[i].gameObject); // 버튼 오브젝트 삭제
        }

        createdSlotButtonList.Clear(); // 목록 비우기
    }

    private void UpdateContentHeight(int itemCount) // Grid Layout Group 기준 Content 높이 조절
    {
        if (contentRectTransform == null || targetGridLayoutGroup == null) return; // 참조 없으면 종료

        int columnCount = GetColumnCount(); // 현재 열 개수 계산
        int rowCount = Mathf.CeilToInt(itemCount / (float)columnCount); // 총 행 개수 계산

        float cellHeight = targetGridLayoutGroup.cellSize.y; // 셀 높이값
        float spacingY = targetGridLayoutGroup.spacing.y; // 세로 간격값
        float paddingTop = targetGridLayoutGroup.padding.top; // 상단 패딩값
        float paddingBottom = targetGridLayoutGroup.padding.bottom; // 하단 패딩값

        float contentHeight = paddingTop + paddingBottom; // 기본 패딩 높이 합산

        if (rowCount > 0)
        {
            contentHeight += (cellHeight * rowCount); // 행 수만큼 셀 높이 추가
            contentHeight += (spacingY * (rowCount - 1)); // 행 사이 간격 추가
        }

        Vector2 currentSize = contentRectTransform.sizeDelta; // 현재 Content 크기 가져오기
        currentSize.y = contentHeight; // 계산된 높이 적용
        contentRectTransform.sizeDelta = currentSize; // Content 크기 반영
    }

    private int GetColumnCount() // Grid Layout Group 기준 열 개수 계산
    {
        if (targetGridLayoutGroup == null) return 1; // 참조 없으면 1열 처리

        if (targetGridLayoutGroup.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
        {
            return Mathf.Max(1, targetGridLayoutGroup.constraintCount); // 고정 열 수 반환
        }

        if (targetGridLayoutGroup.constraint == GridLayoutGroup.Constraint.FixedRowCount)
        {
            float contentWidth = contentRectTransform.rect.width; // Content 너비 가져오기
            float cellWidth = targetGridLayoutGroup.cellSize.x; // 셀 너비값
            float spacingX = targetGridLayoutGroup.spacing.x; // 가로 간격값
            float paddingLeft = targetGridLayoutGroup.padding.left; // 좌측 패딩값
            float paddingRight = targetGridLayoutGroup.padding.right; // 우측 패딩값

            float usableWidth = contentWidth - paddingLeft - paddingRight + spacingX; // 사용 가능 너비 계산
            float perCellWidth = cellWidth + spacingX; // 셀 1개가 차지하는 총 너비

            if (perCellWidth <= 0f) return 1; // 잘못된 값 방지
            return Mathf.Max(1, Mathf.FloorToInt(usableWidth / perCellWidth)); // 계산된 열 수 반환
        }

        return 1; // 자유 배치일 때 기본 1열 처리
    }

    private void UpdateCreateSaveButtonState() // 생성 버튼 클릭 가능 상태 갱신
    {
        if (createSaveButton == null || saveStorage == null) return; // 참조 없으면 종료

        createSaveButton.interactable = saveStorage.CanCreateNewSave(); // 최대 저장본 여부에 따라 클릭 가능 설정
    }

private void OpenSaveNameInputUI() // 이름 입력 UI 열기
{
    if (saveStorage != null && !saveStorage.CanCreateNewSave()) return; // 최대 저장본이면 열지 않음
    if (saveNameInputUI == null) return; // 참조 없으면 종료

    saveNameInputUI.SetActive(true); // 이름 입력 UI 활성화
    UpdateBlockedButtonsState(); // 모달 UI 열림 상태 반영

    if (saveNameInputField != null)
    {
        saveNameInputField.ActivateInputField(); // 입력 포커스 활성화
    }
}

private void TryCreateSave() // 저장본 생성 시도
{
    if (saveStorage == null) return; // 저장 참조 없으면 종료
    if (!saveStorage.CanCreateNewSave()) return; // 최대 저장본이면 종료

    string inputName = saveNameInputField == null ? string.Empty : saveNameInputField.text; // 입력 이름 가져오기

    if (string.IsNullOrWhiteSpace(inputName))
    {
        ShowEmptyNameWarningUI(); // 빈 이름이면 경고 UI 표시
        return; // 생성 중단
    }

    bool createResult = saveStorage.CreateSave(inputName); // 저장본 생성 시도

    if (!createResult) return; // 생성 실패 시 종료

    if (saveNameInputUI != null)
    {
        saveNameInputUI.SetActive(false); // 생성 후 이름 입력 UI 비활성화
    }

    RefreshSaveSlotList(); // 저장본 버튼 목록 갱신
    UpdateBlockedButtonsState(); // 모달 UI 닫힘 상태 반영
}

    private void ShowEmptyNameWarningUI() // 빈 입력 경고 UI 표시
    {
        if (emptyNameWarningUI == null) return; // 참조 없으면 종료

        if (emptyNameWarningCoroutine != null)
        {
            StopCoroutine(emptyNameWarningCoroutine); // 이전 경고 UI 코루틴 중지
        }

        emptyNameWarningCoroutine = StartCoroutine(EmptyNameWarningRoutine()); // 새 경고 UI 코루틴 시작
    }

    private IEnumerator EmptyNameWarningRoutine() // 빈 입력 경고 UI 유지 후 비활성화
    {
        emptyNameWarningUI.SetActive(true); // 경고 UI 활성화
        yield return new WaitForSeconds(emptyNameWarningDuration); // 설정 시간 대기
        emptyNameWarningUI.SetActive(false); // 경고 UI 비활성화
        emptyNameWarningCoroutine = null; // 코루틴 참조 초기화
    }

public void OpenDeleteConfirmUI(SaveSlotButton targetSlotButton) // 삭제 확인 UI 열기
{
    if (targetSlotButton == null) return; // 대상이 없으면 종료
    if (deleteConfirmUI == null) return; // 삭제 UI 참조 없으면 종료

    pendingDeleteSlotButton = targetSlotButton; // 삭제 대기 대상 저장
    deleteConfirmUI.SetActive(true); // 삭제 확인 UI 활성화
    UpdateBlockedButtonsState(); // 모달 UI 열림 상태 반영
}
private void CloseDeleteConfirmUI() // 삭제 확인 UI 닫기
{
    if (deleteConfirmUI != null)
    {
        deleteConfirmUI.SetActive(false); // 삭제 확인 UI 비활성화
    }

    pendingDeleteSlotButton = null; // 삭제 대기 대상 초기화
    UpdateBlockedButtonsState(); // 모달 UI 닫힘 상태 반영
}
    private void ExecuteDeletePendingSave() // 삭제 대기 저장본 삭제 실행
    {
        if (pendingDeleteSlotButton == null)
        {
            CloseDeleteConfirmUI(); // 대상 없으면 UI 닫기
            return; // 삭제 종료
        }

        if (saveStorage == null)
        {
            CloseDeleteConfirmUI(); // 저장 참조 없으면 UI 닫기
            return; // 삭제 종료
        }

        bool deleteResult = saveStorage.DeleteSaveByNumber(pendingDeleteSlotButton.SaveNumber); // 번호 기준 저장본 삭제

        CloseDeleteConfirmUI(); // 삭제 확인 UI 닫기

        if (!deleteResult) return; // 삭제 실패 시 종료

        RefreshSaveSlotList(); // 저장본 버튼 목록 다시 생성
    }

    public void NotifySaveSlotClicked(SaveSlotButton clickedSlotButton) // 저장본 본체 클릭 알림 처리
    {
        if (clickedSlotButton == null) return; // 대상이 없으면 종료

        Debug.Log($"[SaveSelection] 저장본 클릭 미구현 - 이름: {clickedSlotButton.SaveName}, 번호: {clickedSlotButton.SaveNumber}"); // 디버그 로그 출력
    }

    private void CloseSaveNameInputUI() // 이름 입력 UI 닫기
{
    if (saveNameInputUI != null)
    {
        saveNameInputUI.SetActive(false); // 이름 입력 UI 비활성화
    }

    UpdateBlockedButtonsState(); // 모달 UI 닫힘 상태 반영
}

private bool IsAnyModalUIOpen() // 생성 UI 또는 삭제 UI 활성화 여부 확인
{
    bool isSaveNameInputUIOpen = saveNameInputUI != null && saveNameInputUI.activeSelf; // 생성 UI 활성화 여부
    bool isDeleteConfirmUIOpen = deleteConfirmUI != null && deleteConfirmUI.activeSelf; // 삭제 UI 활성화 여부

    return isSaveNameInputUIOpen || isDeleteConfirmUIOpen; // 둘 중 하나라도 열려 있으면 true 반환
}

private void UpdateBlockedButtonsState() // 모달 UI 상태에 따라 버튼 클릭 가능 여부 갱신
{
    bool canClick = !IsAnyModalUIOpen(); // 모달 UI가 열려 있지 않을 때만 클릭 허용

    for (int i = 0; i < blockedButtonList.Count; i++)
    {
        Button targetButton = blockedButtonList[i]; // 현재 차단 대상 버튼 참조
        if (targetButton == null) continue; // 비어 있으면 건너뜀

        targetButton.interactable = canClick; // 클릭 가능 여부 적용
    }

    for (int i = 0; i < createdSlotButtonList.Count; i++)
    {
        SaveSlotButton slotButton = createdSlotButtonList[i]; // 현재 저장본 버튼 참조
        if (slotButton == null) continue; // 비어 있으면 건너뜀

        slotButton.SetInteractable(canClick); // 저장본 버튼 클릭 가능 여부 적용
    }
}
}