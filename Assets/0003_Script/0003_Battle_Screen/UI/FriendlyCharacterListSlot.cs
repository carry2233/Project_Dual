using UnityEngine;
using UnityEngine.UI;

public class FriendlyCharacterListSlot : MonoBehaviour
{
    [Header("필수 참조")]
    [SerializeField] private FriendlyCharacterManager friendlyCharacterManager; // 아군 캐릭터 매니저 참조
    [SerializeField] private GlobalCharacterDefinition globalCharacterDefinition; // 캐릭터 전역 정의 참조
    [SerializeField] private CharacterDuelAI targetCharacter; // 이 슬롯이 나타내는 캐릭터
    [SerializeField] private int assignedSelectionOrder; // 이 슬롯에 연결된 실제 선택 순서값

    [Header("버튼 참조")]
    [SerializeField] private Button detailUIToggleButton; // 상세 UI 토글 버튼
    [SerializeField] private Button selectOrDeselectButton; // 선택 / 선택해제 버튼

    [Header("선택 표시 참조")]
    [SerializeField] private Image selectionStateImage; // 선택 상태 색상 표시 이미지

    [Header("선택 표시 색상")]
    [SerializeField] private Color selectedColor = Color.green; // 선택 중 색상
    [SerializeField] private Color normalColor = Color.white; // 기본 색상

    public CharacterDuelAI TargetCharacter => targetCharacter; // 연결된 캐릭터 반환
    public int AssignedSelectionOrder => assignedSelectionOrder; // 실제 선택 순서값 반환

    private void Awake() // 시작 시 버튼 이벤트 연결
    {
        if (detailUIToggleButton != null)
        {
            detailUIToggleButton.onClick.AddListener(OnClickDetailUIToggleButton); // 상세 UI 토글 버튼 이벤트 연결
        }

        if (selectOrDeselectButton != null)
        {
            selectOrDeselectButton.onClick.AddListener(OnClickSelectOrDeselectButton); // 선택/선택해제 버튼 이벤트 연결
        }
    }

    private void Update() // 매 프레임 선택 상태 색상 갱신
    {
        RefreshSelectionVisual(); // 선택 상태 색상 갱신
    }

    public void InitializeSlot(
        FriendlyCharacterManager targetManager, // 아군 캐릭터 매니저
        GlobalCharacterDefinition targetDefinition, // 캐릭터 정의
        CharacterDuelAI targetDuelAI, // 연결 캐릭터
        int targetAssignedSelectionOrder) // 실제 선택 순서값
    {
        friendlyCharacterManager = targetManager; // 매니저 저장
        globalCharacterDefinition = targetDefinition; // 정의 저장
        targetCharacter = targetDuelAI; // 연결 캐릭터 저장
        assignedSelectionOrder = targetAssignedSelectionOrder; // 실제 선택 순서값 저장

        RefreshSelectionVisual(); // 초기 색상 갱신
    }

    private void OnClickDetailUIToggleButton() // 상세 UI 토글 버튼 클릭 처리
    {
        if (friendlyCharacterManager == null)
        {
            return; // 매니저가 없으면 종료
        }

        if (targetCharacter == null)
        {
            return; // 대상 캐릭터가 없으면 종료
        }

        friendlyCharacterManager.ToggleDetailUIForCharacter(targetCharacter); // 해당 캐릭터 상세 UI 토글
    }

    private void OnClickSelectOrDeselectButton() // 선택 / 선택해제 버튼 클릭 처리
    {
        if (friendlyCharacterManager == null)
        {
            return; // 매니저가 없으면 종료
        }

        if (targetCharacter == null)
        {
            return; // 대상 캐릭터가 없으면 종료
        }

        if (friendlyCharacterManager.IsCharacterCurrentlySelected(targetCharacter))
        {
            friendlyCharacterManager.SetCurrentSelectedFriendlyCharacter(null); // 아군 매니저 선택 상태 해제
            FriendlyCharacterManager.FriendlyCharacterEntry targetEntry = friendlyCharacterManager.GetFriendlyCharacterEntryByOrder(assignedSelectionOrder); // 순서값으로 엔트리 탐색

            if (targetEntry != null && targetEntry.MoveCommandController != null)
            {
                ClickMoveSystemManager clickMoveSystemManager = FindFirstObjectByType<ClickMoveSystemManager>(); // 클릭 이동 시스템 탐색

                if (clickMoveSystemManager != null)
                {
                    clickMoveSystemManager.ClearSelectionExternally(); // 실제 선택 해제 전달
                }
            }

            return;
        }

        friendlyCharacterManager.SelectFriendlyCharacterByOrder(assignedSelectionOrder); // 해당 캐릭터 선택
    }

    public void RefreshSelectionVisual() // 현재 선택 여부에 따라 색상 갱신
    {
        if (selectionStateImage == null)
        {
            return; // 표시 이미지가 없으면 종료
        }

        bool isSelected = friendlyCharacterManager != null && friendlyCharacterManager.IsCharacterCurrentlySelected(targetCharacter); // 현재 선택 여부 확인
        selectionStateImage.color = isSelected ? selectedColor : normalColor; // 색상 적용
    }
}