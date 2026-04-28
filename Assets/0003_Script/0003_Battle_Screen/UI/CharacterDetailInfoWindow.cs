using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 캐릭터 상세 정보창 내부의 요소 Scroll View 크기 조정과 UI 토글 버튼들을 관리하는 스크립트입니다.
/// </summary>
public class CharacterDetailInfoWindow : MonoBehaviour
{
    [Header("캐릭터 요소 Scroll View 설정")]
    [SerializeField] private RectTransform characterElementContent; // 캐릭터 요소들이 배치되는 Content RectTransform
    [SerializeField] private GridLayoutGroup characterElementGridLayoutGroup; // 캐릭터 요소 Content에 적용된 GridLayoutGroup

    [Header("기본 활성화 설정")]
    [SerializeField] private int defaultActiveToggleIndex = 0; // 생성 시 기본으로 활성화할 토글 리스트 인덱스

    [Header("토글 UI 목록")]
    [SerializeField] private List<ToggleUIElement> toggleUIElements = new List<ToggleUIElement>(); // 토글 버튼/대상/스크롤바 목록

    [Header("레벨 표시 UI")]
    [SerializeField] private TMP_Text levelText; // 레벨 값을 표시할 텍스트

    private CharacterStatSystem targetStatSystem; // 상세 UI가 참조할 캐릭터 스탯 시스템

    /// <summary>
    /// 토글 버튼, 토글 대상 UI, 초기화할 Scrollbar를 묶어서 관리하는 데이터입니다.
    /// </summary>
    [System.Serializable]
    public class ToggleUIElement
    {
        public Button toggleButton; // 활성화/비활성화 토글용 버튼
        public GameObject targetUIObject; // 토글 대상 UI 오브젝트
        public Scrollbar targetScrollbar; // 비활성화 시 값이 0으로 초기화될 Scrollbar
    }

    private void Awake()
    {
        InitializeContentWidth();
        InitializeToggleButtons();
        ApplyDefaultToggleState();
    }

    /// <summary>
    /// GridLayoutGroup의 X축 셀 크기와 Content 자식 수를 곱해 Content 너비를 설정합니다.
    /// </summary>
    private void InitializeContentWidth()
    {
        if (characterElementContent == null || characterElementGridLayoutGroup == null)
        {
            return;
        }

        float cellWidth = characterElementGridLayoutGroup.cellSize.x;
        int childCount = characterElementContent.childCount;
        float targetWidth = cellWidth * childCount;

        characterElementContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
    }

    /// <summary>
    /// 토글 리스트에 등록된 버튼들에 클릭 이벤트를 연결합니다.
    /// </summary>
    private void InitializeToggleButtons()
    {
        for (int i = 0; i < toggleUIElements.Count; i++)
        {
            int index = i;

            if (toggleUIElements[index] == null || toggleUIElements[index].toggleButton == null)
            {
                continue;
            }

            toggleUIElements[index].toggleButton.onClick.RemoveListener(() => ToggleTargetUI(index));
            toggleUIElements[index].toggleButton.onClick.AddListener(() => ToggleTargetUI(index));
        }
    }

    /// <summary>
    /// 생성 시 지정된 인덱스의 UI만 활성화하고 나머지는 비활성화합니다.
    /// </summary>
    private void ApplyDefaultToggleState()
    {
        for (int i = 0; i < toggleUIElements.Count; i++)
        {
            bool isDefaultActive = i == defaultActiveToggleIndex;

            SetTargetUIActive(i, isDefaultActive);
        }
    }


private void ToggleTargetUI(int index) // 선택한 토글 UI를 활성화하고, 이미 활성화된 경우 아무 동작도 하지 않습니다.
{
    if (IsInvalidIndex(index))
    {
        return;
    }

    GameObject targetObject = toggleUIElements[index].targetUIObject;

    if (targetObject == null)
    {
        return;
    }

    // 이미 활성화 상태면 아무것도 하지 않음
    if (targetObject.activeSelf)
    {
        return;
    }

    // 비활성 상태일 때만 선택된 UI 활성화 + 나머지 비활성화
    SetOnlyTargetUIActive(index);
}

    /// <summary>
    /// 지정된 인덱스의 UI 활성 상태를 설정하고, 비활성화 시 Scrollbar 값을 0으로 초기화합니다.
    /// </summary>
    private void SetTargetUIActive(int index, bool isActive)
    {
        if (IsInvalidIndex(index))
        {
            return;
        }

        ToggleUIElement toggleElement = toggleUIElements[index];

        if (toggleElement.targetUIObject != null)
        {
            toggleElement.targetUIObject.SetActive(isActive);
        }

        if (isActive == false && toggleElement.targetScrollbar != null)
        {
            toggleElement.targetScrollbar.value = 0f;
        }
    }

    /// <summary>
    /// 리스트 인덱스가 유효한지 검사합니다.
    /// </summary>
    private bool IsInvalidIndex(int index)
    {
        return index < 0 || index >= toggleUIElements.Count || toggleUIElements[index] == null;
    }

    private void SetOnlyTargetUIActive(int activeIndex) // 선택한 인덱스의 UI만 활성화하고 나머지 UI는 모두 비활성화합니다.
{
    for (int i = 0; i < toggleUIElements.Count; i++)
    {
        bool isActiveTarget = i == activeIndex;

        SetTargetUIActive(i, isActiveTarget);
    }
}

public void Initialize(CharacterStatSystem newTargetStatSystem) // 상세 UI에 표시할 스탯 시스템을 연결합니다.
{
    if (targetStatSystem != null)
    {
        targetStatSystem.OnStatusValueChanged -= RefreshLevelText; // 기존 대상 이벤트 연결 해제
    }

    targetStatSystem = newTargetStatSystem; // 새 스탯 시스템 저장

    if (targetStatSystem != null)
    {
        targetStatSystem.OnStatusValueChanged += RefreshLevelText; // 스탯 변경 이벤트 연결
    }

    RefreshLevelText(targetStatSystem); // 최초 레벨 표시 갱신
}

private void OnDestroy() // 상세 UI 삭제 시 이벤트 연결을 해제합니다.
{
    if (targetStatSystem != null)
    {
        targetStatSystem.OnStatusValueChanged -= RefreshLevelText; // 이벤트 중복 참조 방지
    }
}

private void RefreshLevelText(CharacterStatSystem statSystem) // 현재 레벨 값을 텍스트에 표시합니다.
{
    if (levelText == null)
    {
        return; // 텍스트가 없으면 종료
    }

    if (statSystem == null)
    {
        levelText.text = "LV.-"; // 대상이 없을 때 표시
        return;
    }

    levelText.text = "LV." + statSystem.LevelStats; // 레벨 표시
}
}