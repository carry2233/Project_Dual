using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// 클릭 이동 시스템 매니저
/// - 개체 선택 입력 처리
/// - 이동 목적지 입력 처리
/// - 현재 선택된 이동 명령 컨트롤러 관리
/// </summary>
public class ClickMoveSystemManager : MonoBehaviour
{
    public enum InputTriggerType
    {
        MouseButton, // 마우스 버튼으로 입력
        KeyboardKey  // 키보드 키로 입력
    }

    public enum MouseButtonType
    {
        LeftButton,   // 마우스 좌클릭
        RightButton,  // 마우스 우클릭
        MiddleButton  // 마우스 휠 클릭
    }

    [Header("필수 참조")]
    [SerializeField] private Camera targetCamera; // 마우스 좌표를 월드 좌표로 변환할 카메라

    [Header("아군 캐릭터 매니저 참조")]
    [SerializeField] private FriendlyCharacterManager friendlyCharacterManager; // 아군 캐릭터 관리 매니저 참조

    public MoveCommandController CurrentSelectedUnit => currentSelectedUnit; // 현재 선택된 유닛 반환

    [Header("선택 입력 설정")]
    [SerializeField] private InputTriggerType selectInputTriggerType = InputTriggerType.MouseButton; // 선택 입력 방식
    [SerializeField] private MouseButtonType selectMouseButton = MouseButtonType.LeftButton; // 선택용 마우스 버튼
    [SerializeField] private Key selectKeyboardKey = Key.Q; // 선택용 키보드 키

    [Header("목적지 입력 설정")]
    [SerializeField] private InputTriggerType moveInputTriggerType = InputTriggerType.MouseButton; // 목적지 입력 방식
    [SerializeField] private MouseButtonType moveMouseButton = MouseButtonType.RightButton; // 목적지 지정용 마우스 버튼
    [SerializeField] private Key moveKeyboardKey = Key.Space; // 목적지 지정용 키보드 키

    [Header("선택 대상 설정")]
    [SerializeField] private LayerMask selectableLayerMask; // 선택 가능한 개체 레이어 마스크

    [Header("현재 선택 상태")]
    [SerializeField] private MoveCommandController currentSelectedUnit; // 현재 선택된 이동 명령 컨트롤러


    [Header("선택 표시 참조")]
[SerializeField] private GameObject selectedUnitVisualObject; // 선택된 아군 위치를 따라갈 오브젝트1
[SerializeField] private GameObject destinationVisualObject; // 목적지 위치에 표시할 오브젝트2
[SerializeField] private LineRenderer selectionLineRenderer; // 오브젝트1과 오브젝트2를 연결할 라인렌더러

[Header("선택 표시 색상 설정")]
[SerializeField] private Color moveVisualColor = Color.green; // 일반 이동 상태 표시 색상
[SerializeField] private Color attackVisualColor = Color.red; // 공격 대상 추적 상태 표시 색상



private float selectedUnitVisualFixedZ; // 선택 표시 오브젝트1의 시작 Z값 저장
private float destinationVisualFixedZ; // 목적지 표시 오브젝트2의 시작 Z값 저장

private PerObjectTextureOverride selectedUnitVisualTextureOverride; // 선택 표시 오브젝트1 색 적용용 참조
private PerObjectTextureOverride destinationVisualTextureOverride; // 목적지 표시 오브젝트2 색 적용용 참조

private void Awake() // 시작 시 카메라 자동 참조 및 표시 오브젝트 초기값 준비
{
    if (targetCamera == null)
    {
        targetCamera = Camera.main; // 메인 카메라 자동 참조
    }

    if (friendlyCharacterManager == null)
    {
        friendlyCharacterManager = FindFirstObjectByType<FriendlyCharacterManager>(); // 아군 캐릭터 매니저 자동 탐색
    }

    if (selectedUnitVisualObject != null)
    {
        selectedUnitVisualFixedZ = selectedUnitVisualObject.transform.position.z; // 선택 표시 오브젝트의 시작 Z값 저장
        selectedUnitVisualTextureOverride = selectedUnitVisualObject.GetComponent<PerObjectTextureOverride>(); // 색 적용 스크립트 자동 참조
    }

    if (destinationVisualObject != null)
    {
        destinationVisualFixedZ = destinationVisualObject.transform.position.z; // 목적지 표시 오브젝트의 시작 Z값 저장
        destinationVisualTextureOverride = destinationVisualObject.GetComponent<PerObjectTextureOverride>(); // 색 적용 스크립트 자동 참조
    }

    InitializeSelectionVisualState(); // 시작 시 선택 표시 요소 초기화
}

private void Update() // 매 프레임 입력 처리 및 선택 표시 갱신
{
    HandleSelectionInput(); // 개체 선택 입력 처리
    HandleMoveInput(); // 이동 목적지 입력 처리
    UpdateSelectionVisualState(); // 현재 상태 기준으로 표시 오브젝트 활성/비활성 및 색 갱신
    UpdateSelectionVisualFollow(); // 현재 선택된 유닛 기준으로 표시 오브젝트와 라인 위치 갱신
}

    /// <summary>
    /// 선택 입력을 감지하여 개체 선택 시도
    /// </summary>
    private void HandleSelectionInput() // 선택 입력 처리
    {
        if (!IsInputTriggered(selectInputTriggerType, selectMouseButton, selectKeyboardKey))
        {
            return; // 선택 입력이 없으면 종료
        }

        Vector2 mouseWorldPosition = GetMouseWorldPosition2D(); // 현재 마우스 위치를 월드 좌표로 변환
        Collider2D hitCollider = Physics2D.OverlapPoint(mouseWorldPosition, selectableLayerMask); // 선택 가능한 2D 콜라이더 검사

        if (hitCollider == null)
        {
            ClearCurrentSelection(); // 아무것도 클릭하지 않았으면 선택 해제
            return;
        }

        MoveCommandController targetUnit = hitCollider.GetComponent<MoveCommandController>(); // 클릭한 오브젝트의 명령 컨트롤러 참조

        if (targetUnit == null)
        {
            targetUnit = hitCollider.GetComponentInParent<MoveCommandController>(); // 부모 오브젝트에서도 명령 컨트롤러 탐색
        }

        if (targetUnit == null)
        {
            ClearCurrentSelection(); // 명령 컨트롤러가 없으면 선택 해제
            return;
        }

        SetCurrentSelection(targetUnit); // 해당 유닛 선택
    }

    /// <summary>
    /// 목적지 입력을 감지하여 현재 선택된 개체에 목적지를 전달
    /// </summary>
private void HandleMoveInput() // 목적지 입력 또는 공격 대상 입력 처리
{
    if (currentSelectedUnit == null)
    {
        return; // 현재 선택된 유닛이 없으면 종료
    }

    if (!IsInputTriggered(moveInputTriggerType, moveMouseButton, moveKeyboardKey))
    {
        return; // 목적지 입력이 없으면 종료
    }

    Vector2 mouseWorldPosition = GetMouseWorldPosition2D(); // 현재 마우스 월드 좌표 계산
    Collider2D hitCollider = Physics2D.OverlapPoint(mouseWorldPosition); // 클릭 위치의 콜라이더 탐색

    if (hitCollider != null)
    {
        CharacterDuelAI duelAI = hitCollider.GetComponent<CharacterDuelAI>(); // 클릭 대상의 CharacterDuelAI 탐색

        if (duelAI == null)
        {
            duelAI = hitCollider.GetComponentInParent<CharacterDuelAI>(); // 부모에서도 CharacterDuelAI 탐색
        }

        if (duelAI != null)
        {
            bool isFriendlyTarget = friendlyCharacterManager != null && friendlyCharacterManager.IsFriendlyCharacter(duelAI); // 아군 여부 판정

            if (!isFriendlyTarget)
            {
                currentSelectedUnit.SetPriorityAttackTarget(duelAI); // 적 클릭 시 우선 공격 대상 지정
                UpdateSelectionVisualState(); // 표시 상태 즉시 갱신
                return;
            }
        }
    }

    currentSelectedUnit.SetMoveDestination(mouseWorldPosition); // 빈 곳 클릭 시 이동 명령 전달
    UpdateSelectionVisualState(); // 표시 상태 즉시 갱신
}

    /// <summary>
    /// 현재 선택된 유닛을 교체
    /// </summary>
private void SetCurrentSelection(MoveCommandController newUnit) // 현재 선택 유닛 설정
{
    if (currentSelectedUnit == newUnit)
    {
        return; // 이미 같은 유닛이 선택되어 있으면 종료
    }

    if (currentSelectedUnit != null)
    {
        currentSelectedUnit.SetSelected(false); // 기존 선택 유닛 선택 해제
    }

    currentSelectedUnit = newUnit; // 새 유닛 저장

    if (currentSelectedUnit != null)
    {
        currentSelectedUnit.SetSelected(true); // 새 유닛 선택 처리
    }

    SyncCurrentSelectedFriendlyCharacter(); // 현재 선택 아군 캐릭터 동기화
    UpdateSelectionVisualState(); // 선택 변경 후 표시 상태 갱신
}

    /// <summary>
    /// 현재 선택을 해제
    /// </summary>
private void ClearCurrentSelection() // 선택 해제
{
    if (currentSelectedUnit != null)
    {
        currentSelectedUnit.SetSelected(false); // 기존 선택 유닛 선택 해제
    }

    currentSelectedUnit = null; // 현재 선택 참조 제거
    SyncCurrentSelectedFriendlyCharacter(); // 현재 선택 아군 캐릭터 동기화
    InitializeSelectionVisualState(); // 선택 해제 시 표시 요소 초기화
}

    /// <summary>
    /// 입력 방식에 따라 실제 입력 발생 여부를 판정
    /// </summary>
    private bool IsInputTriggered(InputTriggerType triggerType, MouseButtonType mouseButtonType, Key keyboardKey) // 입력 발생 판정
    {
        switch (triggerType)
        {
            case InputTriggerType.MouseButton:
                return IsMouseButtonPressed(mouseButtonType); // 마우스 버튼 입력 판정

            case InputTriggerType.KeyboardKey:
                return IsKeyboardKeyPressed(keyboardKey); // 키보드 키 입력 판정
        }

        return false; // 어느 경우에도 해당하지 않으면 false
    }

    /// <summary>
    /// 설정된 마우스 버튼이 눌렸는지 판정
    /// </summary>
    private bool IsMouseButtonPressed(MouseButtonType mouseButtonType) // 마우스 버튼 입력 판정
    {
        if (Mouse.current == null)
        {
            return false; // 마우스 장치가 없으면 false
        }

        switch (mouseButtonType)
        {
            case MouseButtonType.LeftButton:
                return Mouse.current.leftButton.wasPressedThisFrame; // 좌클릭 입력 판정

            case MouseButtonType.RightButton:
                return Mouse.current.rightButton.wasPressedThisFrame; // 우클릭 입력 판정

            case MouseButtonType.MiddleButton:
                return Mouse.current.middleButton.wasPressedThisFrame; // 휠 클릭 입력 판정
        }

        return false; // 어느 버튼에도 해당하지 않으면 false
    }

    /// <summary>
    /// 설정된 키보드 키가 눌렸는지 판정
    /// </summary>
    private bool IsKeyboardKeyPressed(Key keyboardKey) // 키보드 입력 판정
    {
        if (Keyboard.current == null)
        {
            return false; // 키보드 장치가 없으면 false
        }

        return Keyboard.current[keyboardKey].wasPressedThisFrame; // 지정 키 입력 판정
    }

    /// <summary>
    /// 현재 마우스 스크린 좌표를 2D 월드 좌표로 변환
    /// </summary>
    private Vector2 GetMouseWorldPosition2D() // 마우스 월드 좌표 계산
    {
        if (targetCamera == null || Mouse.current == null)
        {
            return Vector2.zero; // 카메라 또는 마우스가 없으면 0 반환
        }

        Vector3 mouseScreenPosition = Mouse.current.position.ReadValue(); // 현재 마우스 스크린 좌표
        Vector3 mouseWorldPosition = targetCamera.ScreenToWorldPoint(mouseScreenPosition); // 월드 좌표 변환

        return new Vector2(mouseWorldPosition.x, mouseWorldPosition.y); // XY 평면 좌표 반환
    }

private void InitializeSelectionVisualState() // 시작 시 선택 표시 요소 초기화
{
    if (selectedUnitVisualObject != null)
    {
        selectedUnitVisualObject.SetActive(false); // 오브젝트1 비활성화
    }

    if (destinationVisualObject != null)
    {
        destinationVisualObject.SetActive(false); // 오브젝트2 비활성화
    }

    if (selectionLineRenderer != null)
    {
        selectionLineRenderer.positionCount = 2; // 라인 점 개수 설정
        selectionLineRenderer.enabled = false; // 라인렌더러 비활성화
    }

    ApplySelectionVisualColor(moveVisualColor); // 기본 색상 적용
}

private void UpdateSelectionVisualState() // 현재 선택 상태 기준으로 표시 상태 및 색상 갱신
{
    bool hasSelectedUnit = currentSelectedUnit != null; // 현재 선택된 유닛 존재 여부
    bool hasDestination = false; // 현재 표시할 목적지 존재 여부

    if (hasSelectedUnit)
    {
        hasDestination = TryGetCurrentVisualDestination(out _); // 현재 유닛이 실제로 가려는 위치 존재 여부 확인
    }

    if (selectedUnitVisualObject != null)
    {
        selectedUnitVisualObject.SetActive(hasSelectedUnit); // 선택된 유닛이 있으면 오브젝트1 활성화
    }

    if (destinationVisualObject != null)
    {
        destinationVisualObject.SetActive(hasSelectedUnit && hasDestination); // 선택 유닛과 목적지가 있을 때만 오브젝트2 활성화
    }

    if (selectionLineRenderer != null)
    {
        selectionLineRenderer.enabled = hasSelectedUnit && hasDestination; // 둘 다 있을 때만 라인 활성화
    }

    ApplyCurrentSelectionVisualColor(); // 현재 상태에 맞는 색상 적용
}

private void UpdateSelectionVisualFollow() // 선택된 유닛 위치 추적 및 라인 갱신
{
    if (currentSelectedUnit == null)
    {
        return; // 선택된 유닛이 없으면 종료
    }

    if (selectedUnitVisualObject != null)
    {
        Vector3 unitPosition = currentSelectedUnit.transform.position; // 현재 선택 유닛 월드 위치
        selectedUnitVisualObject.transform.position = new Vector3(
            unitPosition.x,
            unitPosition.y,
            selectedUnitVisualFixedZ); // X/Y만 따라가고 Z는 시작값 유지
    }

    if (TryGetCurrentVisualDestination(out Vector2 currentVisualDestination))
    {
        UpdateDestinationVisualPosition(currentVisualDestination); // 현재 실제 목적지 위치 반영
    }

    if (selectionLineRenderer != null && selectionLineRenderer.enabled)
    {
        Vector3 startPosition = currentSelectedUnit.transform.position; // 기본 시작점
        Vector3 endPosition = currentSelectedUnit.transform.position; // 기본 끝점

        if (selectedUnitVisualObject != null)
        {
            startPosition = selectedUnitVisualObject.transform.position; // 오브젝트1 위치를 시작점으로 사용
        }

        if (destinationVisualObject != null)
        {
            endPosition = destinationVisualObject.transform.position; // 오브젝트2 위치를 끝점으로 사용
        }

        selectionLineRenderer.SetPosition(0, startPosition); // 라인 시작점 설정
        selectionLineRenderer.SetPosition(1, endPosition); // 라인 끝점 설정
    }
}

private void UpdateDestinationVisualPosition(Vector2 destinationPosition) // 목적지 표시 오브젝트 위치 갱신
{
    if (destinationVisualObject == null)
    {
        return; // 오브젝트2 참조가 없으면 종료
    }

    destinationVisualObject.transform.position = new Vector3(
        destinationPosition.x,
        destinationPosition.y,
        destinationVisualFixedZ); // X/Y만 반영하고 Z는 시작값 유지
}
private bool TryGetCurrentVisualDestination(out Vector2 visualDestination) // 현재 선택 유닛이 실제로 가려는 위치 반환
{
    visualDestination = Vector2.zero; // 기본값 초기화

    if (currentSelectedUnit == null)
    {
        return false; // 선택 유닛이 없으면 실패
    }

    if (currentSelectedUnit.IsChasingAttackTarget)
    {
        CharacterDuelAI currentAttackTarget = currentSelectedUnit.CurrentAttackTarget; // 현재 공격 추적 대상 참조

        if (currentAttackTarget != null)
        {
            visualDestination = currentAttackTarget.transform.position; // 공격 대상 현재 위치를 목적지로 사용
            return true; // 공격 추적 목적지 반환 성공
        }
    }

    if (currentSelectedUnit.HasMoveDestination)
    {
        visualDestination = currentSelectedUnit.LastMoveDestination; // 일반 이동 목적지 반환
        return true; // 이동 목적지 반환 성공
    }

    return false; // 표시할 목적지가 없으면 실패
}

private void ApplyCurrentSelectionVisualColor() // 현재 상태에 맞는 선택 표시 색상 적용
{
    if (currentSelectedUnit == null)
    {
        ApplySelectionVisualColor(moveVisualColor); // 선택 대상이 없으면 기본 이동 색상 적용
        return;
    }

    if (currentSelectedUnit.IsChasingAttackTarget)
    {
        ApplySelectionVisualColor(attackVisualColor); // 공격 대상 추적 중이면 공격 색상 적용
        return;
    }

    ApplySelectionVisualColor(moveVisualColor); // 그 외에는 일반 이동 색상 적용
}

private void ApplySelectionVisualColor(Color targetColor) // 선택 표시 오브젝트들과 라인에 동일한 색상 적용
{
    if (selectedUnitVisualTextureOverride != null)
    {
        selectedUnitVisualTextureOverride.SetColor(targetColor); // 오브젝트1 색상 적용
    }

    if (destinationVisualTextureOverride != null)
    {
        destinationVisualTextureOverride.SetColor(targetColor); // 오브젝트2 색상 적용
    }

    if (selectionLineRenderer != null)
    {
        selectionLineRenderer.startColor = targetColor; // 라인 시작 색상 적용
        selectionLineRenderer.endColor = targetColor; // 라인 끝 색상 적용
    }
}

public void SelectUnitExternally(MoveCommandController targetUnit) // 외부에서 특정 유닛을 선택
{
    if (targetUnit == null)
    {
        return; // 대상이 없으면 종료
    }

    SetCurrentSelection(targetUnit); // 기존 선택 처리 흐름 사용
}

public void ClearSelectionExternally() // 외부에서 현재 선택 해제
{
    ClearCurrentSelection(); // 기존 선택 해제 처리 흐름 사용
}

private void SyncCurrentSelectedFriendlyCharacter() // 현재 선택된 아군 캐릭터를 아군 매니저와 동기화
{
    if (friendlyCharacterManager == null)
    {
        return; // 매니저가 없으면 종료
    }

    if (currentSelectedUnit == null)
    {
        friendlyCharacterManager.SetCurrentSelectedFriendlyCharacter(null); // 선택이 없으면 null 전달
        return;
    }

    CharacterDuelAI selectedDuelAI = currentSelectedUnit.GetComponent<CharacterDuelAI>(); // 현재 선택된 유닛의 CharacterDuelAI 참조

    if (selectedDuelAI == null)
    {
        friendlyCharacterManager.SetCurrentSelectedFriendlyCharacter(null); // 결투 AI가 없으면 null 전달
        return;
    }

    if (!friendlyCharacterManager.IsFriendlyCharacter(selectedDuelAI))
    {
        friendlyCharacterManager.SetCurrentSelectedFriendlyCharacter(null); // 아군이 아니면 null 전달
        return;
    }

    friendlyCharacterManager.SetCurrentSelectedFriendlyCharacter(selectedDuelAI); // 현재 선택된 아군 캐릭터 전달
}

}