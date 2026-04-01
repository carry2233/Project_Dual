using System.Collections; // 코루틴 사용
using UnityEngine; // Unity 기본 네임스페이스
using UnityEngine.UI; // 버튼 참조용
using UnityEngine.EventSystems; // UI 클릭 차단용
using UnityEngine.InputSystem; // 새 입력 시스템 사용

/// <summary>
/// 타일 선택 + 선택 해제 + 카메라 연출 통합 매니저
/// - 좌클릭으로 타일 선택
/// - 선택 해제 버튼 처리
/// - 카메라 루트/가상카메라 위치 연출 처리
/// - 연출 중 / 선택 중 월드맵 이동 잠금
/// </summary>
public class TileSelectionManager : MonoBehaviour
{
    public enum SelectionState
    {
        None, // 아무것도 선택되지 않은 상태
        Selecting, // 선택 연출 진행 중
        Selected, // 선택 완료 상태
        Deselecting // 선택 해제 연출 진행 중
    }

    [Header("입력 설정")]
    [SerializeField] private Camera inputCamera; // 화면 클릭을 Raycast로 변환할 카메라
    [SerializeField] private LayerMask tileLayerMask = ~0; // 타일 검출용 레이어 마스크
    [SerializeField] private float rayDistance = 500f; // Raycast 최대 거리
    [SerializeField] private bool blockClickWhenPointerOverUI = true; // UI 위 클릭일 때 타일 선택 차단 여부

    [Header("카메라 루트 참조")]
    [SerializeField] private Transform cameraRigRoot; // 빈 오브젝트(부모) 기준 루트
    [SerializeField] private Transform cinemachineCameraTransform; // 시네머신 카메라 오브젝트 Transform

    [Header("선택 연출 위치 설정")]
    [SerializeField] private Vector3 selectedRigWorldOffset = Vector3.zero; // 선택 시 루트가 타일 위치에 더할 월드 오프셋
    [SerializeField] private Vector3 selectedCameraLocalPosition = new Vector3(0f, 6f, -6f); // 선택 시 카메라 로컬 위치
    [SerializeField] private Vector3 selectedCameraLocalEuler = new Vector3(35f, 0f, 0f); // 선택 시 카메라 로컬 회전값

    [Header("연출 시간 설정")]
    [SerializeField] private float selectDuration = 0.5f; // 선택 연출 시간
    [SerializeField] private float deselectDuration = 0.5f; // 선택 해제 연출 시간

    [Header("연출 커브")]
    [SerializeField] private AnimationCurve rigMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // 루트 이동 커브
    [SerializeField] private AnimationCurve cameraMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // 카메라 로컬 위치 커브
    [SerializeField] private AnimationCurve cameraRotateCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // 카메라 로컬 회전 커브

    [Header("외부 참조")]
    [SerializeField] private WorldMapCameraController worldMapCameraController; // 이동 잠금을 걸 월드맵 카메라 이동 스크립트
    [SerializeField] private GameObject deselectButtonObject; // 선택 중 활성화할 선택 해제 버튼 오브젝트
    [SerializeField] private Button deselectButton; // 선택 해제 버튼 컴포넌트

    private SelectionState currentState = SelectionState.None; // 현재 선택 상태
    private TilePrefab currentSelectedTile; // 현재 선택된 타일
    private Coroutine selectionCoroutine; // 현재 실행 중인 연출 코루틴

    private Vector3 savedRigWorldPosition; // 선택 직전에 저장한 루트 월드 위치
    private Quaternion savedRigWorldRotation; // 선택 직전에 저장한 루트 월드 회전
    private Vector3 savedCameraLocalPosition; // 선택 직전에 저장한 카메라 로컬 위치
    private Quaternion savedCameraLocalRotation; // 선택 직전에 저장한 카메라 로컬 회전
    private bool hasSavedCameraState = false; // 선택 직전 카메라 상태 저장 여부

    public SelectionState CurrentState => currentState; // 현재 상태 외부 확인용
    public TilePrefab CurrentSelectedTile => currentSelectedTile; // 현재 선택 타일 외부 확인용

private void Awake() // 시작 전 기본 참조와 초기값 저장
{
    if (inputCamera == null)
    {
        inputCamera = Camera.main; // 비어 있으면 메인 카메라 자동 사용
    }

    if (deselectButton != null)
    {
        deselectButton.onClick.AddListener(ClearSelection); // 버튼 클릭 시 선택 해제 연결
    }

    SetDeselectButtonVisible(false); // 시작 시 버튼 비활성화
    UpdateMovementLock(); // 시작 시 이동 잠금 상태 반영
}

    private void OnDestroy() // 종료 시 버튼 이벤트 해제
    {
        if (deselectButton != null)
        {
            deselectButton.onClick.RemoveListener(ClearSelection); // 버튼 이벤트 해제
        }
    }

private void Update() // 매 프레임 클릭 입력 처리
{
    if (Mouse.current == null)
    {
        return; // 마우스 장치가 없으면 종료
    }

    if (Mouse.current.leftButton.wasPressedThisFrame == false)
    {
        return; // 좌클릭 입력이 없으면 종료
    }

    if (blockClickWhenPointerOverUI == true && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
    {
        return; // UI 위 클릭이면 타일 선택 차단
    }

    if (currentState == SelectionState.Selecting || currentState == SelectionState.Deselecting)
    {
        return; // 연출 중에는 새 선택 입력 차단
    }

    if (currentState == SelectionState.Selected)
    {
        return; // 이미 선택 상태면 새 선택 입력 차단
    }

    TrySelectTileFromMousePosition(); // 마우스 위치로 타일 선택 시도
}

private void TrySelectTileFromMousePosition() // 마우스 위치에서 타일 선택 시도
{
    if (inputCamera == null)
    {
        return; // 입력 카메라가 없으면 종료
    }

    if (Mouse.current == null)
    {
        return; // 마우스 장치가 없으면 종료
    }

    Ray ray = inputCamera.ScreenPointToRay(Mouse.current.position.ReadValue()); // 현재 마우스 위치를 Ray로 변환

    if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, tileLayerMask) == false)
    {
        return; // 아무것도 맞지 않으면 종료
    }

    TilePrefab tile = hit.collider.GetComponentInParent<TilePrefab>(); // 맞은 오브젝트에서 TilePrefab 탐색

    if (tile == null)
    {
        return; // 타일이 아니면 종료
    }

    SelectTile(tile); // 타일 선택 실행
}

public void SelectTile(TilePrefab tile) // 외부 또는 내부에서 타일 선택 실행
{
    if (tile == null)
    {
        return; // 타일이 없으면 종료
    }

    if (cameraRigRoot == null || cinemachineCameraTransform == null)
    {
        Debug.LogWarning("[TileSelectionManager] 카메라 루트 또는 시네머신 카메라 Transform 참조가 비어 있습니다.", this); // 참조 누락 경고
        return;
    }

    SaveCurrentCameraStateBeforeSelection(); // 선택 직전 카메라 상태 저장
    currentSelectedTile = tile; // 현재 선택 타일 저장
    PlaySelectionAnimation(true); // 선택 연출 재생
}

    public void ClearSelection() // 현재 선택 해제 실행
    {
        if (currentState == SelectionState.None)
        {
            return; // 이미 해제 상태면 종료
        }

        if (cameraRigRoot == null || cinemachineCameraTransform == null)
        {
            Debug.LogWarning("[TileSelectionManager] 카메라 루트 또는 시네머신 카메라 Transform 참조가 비어 있습니다.", this); // 참조 누락 경고
            return;
        }

        PlaySelectionAnimation(false); // 해제 연출 재생
    }

    private void PlaySelectionAnimation(bool isSelecting) // 선택/해제 연출 시작
    {
        if (selectionCoroutine != null)
        {
            StopCoroutine(selectionCoroutine); // 기존 연출 중이면 중지
        }

        selectionCoroutine = StartCoroutine(AnimateSelectionRoutine(isSelecting)); // 새 연출 시작
    }

private IEnumerator AnimateSelectionRoutine(bool isSelecting) // 선택/해제 연출 코루틴
{
    currentState = isSelecting ? SelectionState.Selecting : SelectionState.Deselecting; // 현재 연출 상태 설정
    UpdateMovementLock(); // 연출 시작 시 이동 잠금 반영

    Vector3 startRigWorldPosition = cameraRigRoot.position; // 시작 루트 위치
    Quaternion startRigWorldRotation = cameraRigRoot.rotation; // 시작 루트 회전
    Vector3 startCameraLocalPosition = cinemachineCameraTransform.localPosition; // 시작 카메라 로컬 위치
    Quaternion startCameraLocalRotation = cinemachineCameraTransform.localRotation; // 시작 카메라 로컬 회전

    Vector3 targetRigWorldPosition = isSelecting && currentSelectedTile != null
        ? currentSelectedTile.transform.position + selectedRigWorldOffset
        : (hasSavedCameraState ? savedRigWorldPosition : cameraRigRoot.position); // 목표 루트 위치

    Quaternion targetRigWorldRotation = isSelecting
        ? cameraRigRoot.rotation
        : (hasSavedCameraState ? savedRigWorldRotation : cameraRigRoot.rotation); // 목표 루트 회전

    Vector3 targetCameraLocalPosition = isSelecting
        ? selectedCameraLocalPosition
        : (hasSavedCameraState ? savedCameraLocalPosition : cinemachineCameraTransform.localPosition); // 목표 카메라 로컬 위치

    Quaternion targetCameraLocalRotation = isSelecting
        ? Quaternion.Euler(selectedCameraLocalEuler)
        : (hasSavedCameraState ? savedCameraLocalRotation : cinemachineCameraTransform.localRotation); // 목표 카메라 로컬 회전

    float duration = isSelecting ? selectDuration : deselectDuration; // 현재 연출 시간

    if (duration <= 0f)
    {
        cameraRigRoot.position = targetRigWorldPosition; // 루트 위치 즉시 적용
        cameraRigRoot.rotation = targetRigWorldRotation; // 루트 회전 즉시 적용
        cinemachineCameraTransform.localPosition = targetCameraLocalPosition; // 카메라 로컬 위치 즉시 적용
        cinemachineCameraTransform.localRotation = targetCameraLocalRotation; // 카메라 로컬 회전 즉시 적용
    }
    else
    {
        float elapsed = 0f; // 경과 시간

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime; // 시간 누적
            float time01 = Mathf.Clamp01(elapsed / duration); // 0~1 진행률

            float rigT = rigMoveCurve != null ? rigMoveCurve.Evaluate(time01) : time01; // 루트 이동 커브 적용값
            float cameraMoveT = cameraMoveCurve != null ? cameraMoveCurve.Evaluate(time01) : time01; // 카메라 위치 커브 적용값
            float cameraRotateT = cameraRotateCurve != null ? cameraRotateCurve.Evaluate(time01) : time01; // 카메라 회전 커브 적용값

            cameraRigRoot.position = Vector3.Lerp(startRigWorldPosition, targetRigWorldPosition, rigT); // 루트 위치 보간
            cameraRigRoot.rotation = Quaternion.Slerp(startRigWorldRotation, targetRigWorldRotation, rigT); // 루트 회전 보간
            cinemachineCameraTransform.localPosition = Vector3.Lerp(startCameraLocalPosition, targetCameraLocalPosition, cameraMoveT); // 카메라 로컬 위치 보간
            cinemachineCameraTransform.localRotation = Quaternion.Slerp(startCameraLocalRotation, targetCameraLocalRotation, cameraRotateT); // 카메라 로컬 회전 보간

            yield return null; // 다음 프레임까지 대기
        }

        cameraRigRoot.position = targetRigWorldPosition; // 마지막 위치 보정
        cameraRigRoot.rotation = targetRigWorldRotation; // 마지막 회전 보정
        cinemachineCameraTransform.localPosition = targetCameraLocalPosition; // 마지막 로컬 위치 보정
        cinemachineCameraTransform.localRotation = targetCameraLocalRotation; // 마지막 로컬 회전 보정
    }

    if (isSelecting == true)
    {
        currentState = SelectionState.Selected; // 선택 완료 상태로 변경
        SetDeselectButtonVisible(true); // 버튼 활성화
    }
    else
    {
        currentState = SelectionState.None; // 해제 완료 상태로 변경
        currentSelectedTile = null; // 선택 타일 초기화
        SetDeselectButtonVisible(false); // 버튼 비활성화
    }

    UpdateMovementLock(); // 연출 종료 후 이동 잠금 반영
    selectionCoroutine = null; // 코루틴 참조 초기화
}

    private void SetDeselectButtonVisible(bool isVisible) // 선택 해제 버튼 표시 여부 적용
    {
        if (deselectButtonObject != null)
        {
            deselectButtonObject.SetActive(isVisible); // 버튼 오브젝트 활성/비활성 적용
        }
    }

    private void UpdateMovementLock() // 현재 상태에 따라 월드맵 이동 잠금 적용
    {
        if (worldMapCameraController == null)
        {
            return; // 참조가 없으면 종료
        }

        bool shouldLock =
            currentState == SelectionState.Selecting ||
            currentState == SelectionState.Selected ||
            currentState == SelectionState.Deselecting; // 선택 관련 상태면 잠금

        worldMapCameraController.SetMovementLock(shouldLock); // 이동 잠금 적용
    }

    private void SaveCurrentCameraStateBeforeSelection() // 선택 직전 카메라 상태 저장
{
    if (cameraRigRoot == null || cinemachineCameraTransform == null)
    {
        return; // 참조가 없으면 저장하지 않음
    }

    savedRigWorldPosition = cameraRigRoot.position; // 현재 루트 월드 위치 저장
    savedRigWorldRotation = cameraRigRoot.rotation; // 현재 루트 월드 회전 저장
    savedCameraLocalPosition = cinemachineCameraTransform.localPosition; // 현재 카메라 로컬 위치 저장
    savedCameraLocalRotation = cinemachineCameraTransform.localRotation; // 현재 카메라 로컬 회전 저장
    hasSavedCameraState = true; // 저장 완료 표시
}
}