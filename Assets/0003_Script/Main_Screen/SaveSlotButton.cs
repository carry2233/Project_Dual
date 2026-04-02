using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveSlotButton : MonoBehaviour
{
    [Header("버튼 참조")]
    [SerializeField] private Button slotMainButton; // 저장본 본체 버튼
    [SerializeField] private Button deleteSaveButton; // 저장본 삭제 버튼

    [Header("텍스트 참조")]
    [SerializeField] private TMP_Text saveNameText; // 저장본 이름 표시 텍스트
    [SerializeField] private TMP_Text saveNumberText; // 저장본 번호 표시 텍스트

    [Header("저장본 정보")]
    [SerializeField] private string saveName; // 저장본 이름
    [SerializeField] private int saveNumber; // 저장본 번호

    private SaveSelection ownerSaveSelection; // 상위 저장 선택 스크립트 참조

    public string SaveName => saveName; // 저장본 이름 반환
    public int SaveNumber => saveNumber; // 저장본 번호 반환

    public void Initialize(SaveSelection targetSelection, string targetSaveName, int targetSaveNumber) // 저장본 버튼 초기화
    {
        ownerSaveSelection = targetSelection; // 상위 저장 선택 스크립트 저장
        saveName = targetSaveName; // 저장본 이름 저장
        saveNumber = targetSaveNumber; // 저장본 번호 저장

        RefreshVisual(); // 텍스트 갱신
        RebindButtonEvents(); // 버튼 이벤트 다시 연결
    }

    private void OnDestroy() // 삭제 시 버튼 이벤트 정리
    {
        RemoveButtonEvents(); // 버튼 이벤트 제거
    }

    private void RefreshVisual() // 표시 텍스트 갱신
    {
        if (saveNameText != null)
        {
            saveNameText.text = saveName; // 저장본 이름 텍스트 반영
        }

        if (saveNumberText != null)
        {
            saveNumberText.text = saveNumber.ToString(); // 저장본 번호 텍스트 반영
        }
    }

    private void RebindButtonEvents() // 버튼 이벤트 재연결
    {
        RemoveButtonEvents(); // 기존 이벤트 제거
        AddButtonEvents(); // 새 이벤트 등록
    }

    private void AddButtonEvents() // 버튼 이벤트 등록
    {
        if (slotMainButton != null)
        {
            slotMainButton.onClick.AddListener(OnClickSlotMainButton); // 저장본 본체 버튼 클릭 이벤트 등록
        }

        if (deleteSaveButton != null)
        {
            deleteSaveButton.onClick.AddListener(OnClickDeleteSaveButton); // 삭제 버튼 클릭 이벤트 등록
        }
    }

    private void RemoveButtonEvents() // 버튼 이벤트 해제
    {
        if (slotMainButton != null)
        {
            slotMainButton.onClick.RemoveListener(OnClickSlotMainButton); // 저장본 본체 버튼 클릭 이벤트 해제
        }

        if (deleteSaveButton != null)
        {
            deleteSaveButton.onClick.RemoveListener(OnClickDeleteSaveButton); // 삭제 버튼 클릭 이벤트 해제
        }
    }

    private void OnClickSlotMainButton() // 저장본 본체 버튼 클릭 처리
    {
        Debug.Log($"[SaveSlotButton] 저장본 선택 기능 미구현 - 이름: {saveName}, 번호: {saveNumber}"); // 디버그 로그 출력

        if (ownerSaveSelection != null)
        {
            ownerSaveSelection.NotifySaveSlotClicked(this); // 상위 선택 스크립트에 클릭 알림 전달
        }
    }

    private void OnClickDeleteSaveButton() // 저장본 삭제 버튼 클릭 처리
    {
        if (ownerSaveSelection == null) return; // 상위 선택 스크립트가 없으면 종료
        ownerSaveSelection.OpenDeleteConfirmUI(this); // 삭제 확인 UI 열기 요청
    }

    public void SetInteractable(bool isInteractable) // 저장본 버튼 클릭 가능 여부 설정
{
    if (slotMainButton != null)
    {
        slotMainButton.interactable = isInteractable; // 저장본 본체 버튼 클릭 가능 여부 적용
    }

    if (deleteSaveButton != null)
    {
        deleteSaveButton.interactable = isInteractable; // 저장본 삭제 버튼 클릭 가능 여부 적용
    }
}
}