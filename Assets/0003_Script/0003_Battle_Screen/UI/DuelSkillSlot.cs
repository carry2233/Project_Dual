using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DuelSkillSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI 참조")]
    [SerializeField] private TextMeshProUGUI skillNameText; // 결투기술 이름 표시 텍스트
    [SerializeField] private Image skillIconImage; // 결투기술 아이콘 표시 이미지

    [Header("현재 슬롯 정보")]
    [SerializeField] private FriendlyCharacterManager friendlyCharacterManager; // 결투기술 목록을 관리하는 매니저
    [SerializeField] private DuelSkillDefinitionSO duelSkillDefinition; // 이 슬롯이 나타내는 결투기술
    [SerializeField] private int skillIndex; // 결투기술 목록 인덱스

    public DuelSkillDefinitionSO DuelSkillDefinition => duelSkillDefinition; // 결투기술 반환
    public int SkillIndex => skillIndex; // 결투기술 인덱스 반환

    public void Initialize(FriendlyCharacterManager targetManager, DuelSkillDefinitionSO targetSkill, int targetIndex) // 슬롯 초기화
    {
        friendlyCharacterManager = targetManager; // 매니저 저장
        duelSkillDefinition = targetSkill; // 결투기술 저장
        skillIndex = targetIndex; // 인덱스 저장

        RefreshVisual(); // 표시 정보 갱신
    }

    private void RefreshVisual() // 슬롯 표시 갱신
    {
        if (skillNameText != null)
        {
            skillNameText.text = duelSkillDefinition != null ? duelSkillDefinition.SkillName : string.Empty; // 기술 이름 표시
        }
    }

    public void OnPointerEnter(PointerEventData eventData) // 마우스가 슬롯 위에 올라왔을 때
    {
        if (friendlyCharacterManager == null)
        {
            return; // 매니저가 없으면 종료
        }

        friendlyCharacterManager.SetHoveredDuelSkillSlot(this); // 현재 호버 슬롯 등록
    }

    public void OnPointerExit(PointerEventData eventData) // 마우스가 슬롯 밖으로 나갔을 때
    {
        if (friendlyCharacterManager == null)
        {
            return; // 매니저가 없으면 종료
        }

        friendlyCharacterManager.ClearHoveredDuelSkillSlot(this); // 현재 호버 슬롯 해제
    }

    public bool IsScreenPointInsideSlot(Vector2 screenPoint) // 화면 좌표가 이 슬롯 안에 있는지 확인
{
    RectTransform rectTransform = transform as RectTransform; // 슬롯 RectTransform 참조

    if (rectTransform == null)
    {
        return false; // RectTransform이 아니면 false
    }

    return RectTransformUtility.RectangleContainsScreenPoint(
        rectTransform, // 검사할 슬롯 영역
        screenPoint, // 마우스 화면 좌표
        GetUICamera()); // Canvas 방식에 맞는 UI 카메라
}

private Camera GetUICamera() // UI 판정에 사용할 카메라 반환
{
    Canvas targetCanvas = GetComponentInParent<Canvas>(); // 부모 Canvas 탐색

    if (targetCanvas == null)
    {
        return null; // Canvas가 없으면 Overlay 기준 처리
    }

    if (targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
    {
        return null; // Overlay Canvas는 카메라 없이 판정
    }

    return targetCanvas.worldCamera; // Camera Canvas는 Canvas 카메라 사용
}
}