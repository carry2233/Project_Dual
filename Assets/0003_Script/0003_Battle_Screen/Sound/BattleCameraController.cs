using UnityEngine;
using UnityEngine.InputSystem; // 새 입력 시스템 사용

/// <summary>
/// 전투 카메라 이동 / 확대 / 축소 제어
/// - 상하좌우 이동 키 설정 가능
/// - 마우스 휠 확대 / 축소
/// - 현재 카메라 위치와 줌값 제공
/// </summary>
public class BattleCameraController : MonoBehaviour
{
    public static BattleCameraController Instance { get; private set; } // 싱글톤 인스턴스

    [Header("카메라 이동 키 설정")]
    [SerializeField] private Key moveUpKey = Key.W; // 위로 이동 키
    [SerializeField] private Key moveDownKey = Key.S; // 아래로 이동 키
    [SerializeField] private Key moveLeftKey = Key.A; // 왼쪽으로 이동 키
    [SerializeField] private Key moveRightKey = Key.D; // 오른쪽으로 이동 키

    [Header("카메라 이동 설정")]
    [SerializeField] private float moveSpeed = 8f; // 카메라 이동 속도

    [Header("카메라 줌 설정")]
    [SerializeField] private float zoomInSpeed = 8f; // 확대 속도
    [SerializeField] private float zoomOutSpeed = 8f; // 축소 속도
    [SerializeField] private float minOrthographicSize = 3f; // 최소 줌 크기
    [SerializeField] private float maxOrthographicSize = 15f; // 최대 줌 크기

    [Header("카메라 참조")]
    [SerializeField] private Camera targetCamera; // 제어할 카메라 참조

    [Header("카메라 물리 참조")]
[SerializeField] private BoxCollider2D cameraBoxCollider2D; // 카메라 투사 범위와 동기화할 박스 콜라이더
[SerializeField] private Rigidbody2D cameraRigidbody2D; // 카메라 물리 처리를 위한 리지드바디2D 참조

[Header("카메라 콜라이더 동기화 설정")]
[SerializeField] private bool syncColliderWithCameraView = true; // 카메라 투사 범위와 콜라이더 크기 동기화 여부
[SerializeField] private float colliderThicknessPadding = 0f; // 콜라이더 크기에 추가로 더할 여유값
[SerializeField] private Vector2 colliderOffset = Vector2.zero; // 카메라 중심 기준 콜라이더 오프셋

[Header("카메라 충돌 검사 설정")]
[SerializeField] private LayerMask cameraBlockingLayerMask; // 카메라 이동을 막을 벽 레이어 마스크
[SerializeField] private float collisionCheckSkinWidth = 0.01f; // 충돌 검사 시 살짝 줄여서 검사할 여유값

    public Vector3 CurrentCameraPosition => targetCamera != null ? targetCamera.transform.position : transform.position; // 현재 카메라 위치 반환
    public float CurrentOrthographicSize => targetCamera != null ? targetCamera.orthographicSize : 0f; // 현재 Orthographic Size 반환

    public float MinOrthographicSize => minOrthographicSize; // 최소 카메라 크기 반환
public float MaxOrthographicSize => maxOrthographicSize; // 최대 카메라 크기 반환

    public float ZoomNormalized01 // 현재 줌 상태를 0~1로 반환 (0 = 최대 확대, 1 = 최대 축소)
    {
        get
        {
            if (targetCamera == null)
            {
                return 0f; // 카메라가 없으면 기본값 반환
            }

            if (Mathf.Approximately(minOrthographicSize, maxOrthographicSize))
            {
                return 0f; // 최소/최대 값이 같으면 0 반환
            }

            return Mathf.InverseLerp(minOrthographicSize, maxOrthographicSize, targetCamera.orthographicSize); // 현재 줌 상태 정규화
        }
    }

private void Awake() // 초기 참조 설정
{
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject); // 중복 인스턴스 제거
        return;
    }

    Instance = this; // 싱글톤 인스턴스 저장

    if (targetCamera == null)
    {
        targetCamera = GetComponent<Camera>(); // 같은 오브젝트의 Camera 자동 참조
    }

    if (targetCamera == null)
    {
        targetCamera = Camera.main; // 메인 카메라 자동 참조
    }

    if (cameraBoxCollider2D == null)
    {
        cameraBoxCollider2D = GetComponent<BoxCollider2D>(); // 같은 오브젝트의 BoxCollider2D 자동 참조
    }

    if (cameraRigidbody2D == null)
    {
        cameraRigidbody2D = GetComponent<Rigidbody2D>(); // 같은 오브젝트의 Rigidbody2D 자동 참조
    }

    SyncCameraColliderToCurrentView(); // 시작 시 현재 카메라 투사 범위 기준으로 콜라이더 즉시 동기화
}

private void Update() // 매 프레임 입력 처리
{
    UpdateMoveInput(); // 이동 입력 처리
    UpdateZoomInput(); // 줌 입력 처리
    SyncCameraColliderToCurrentView(); // 현재 카메라 투사 범위 기준으로 콜라이더 동기화
}

private void UpdateMoveInput() // Input System 기준 카메라 이동 처리
{
    if (targetCamera == null)
    {
        return; // 카메라가 없으면 종료
    }

    Keyboard keyboard = Keyboard.current; // 현재 키보드 장치 참조

    if (keyboard == null)
    {
        return; // 키보드가 없으면 종료
    }

    float horizontal = 0f; // X축 입력값
    float vertical = 0f; // Y축 입력값

    if (keyboard[moveLeftKey].isPressed)
    {
        horizontal -= 1f; // 왼쪽 이동 입력
    }

    if (keyboard[moveRightKey].isPressed)
    {
        horizontal += 1f; // 오른쪽 이동 입력
    }

    if (keyboard[moveDownKey].isPressed)
    {
        vertical -= 1f; // 아래 이동 입력
    }

    if (keyboard[moveUpKey].isPressed)
    {
        vertical += 1f; // 위 이동 입력
    }

    Vector3 moveDirection = new Vector3(horizontal, vertical, 0f); // 이동 방향 계산

    if (moveDirection.sqrMagnitude > 1f)
    {
        moveDirection.Normalize(); // 대각선 이동 속도 보정
    }

    if (moveDirection.sqrMagnitude <= 0f)
    {
        return; // 이동 입력이 없으면 종료
    }

    Vector3 currentPosition = targetCamera.transform.position; // 현재 카메라 위치 저장
    Vector3 moveDelta = moveDirection * moveSpeed * Time.deltaTime; // 이번 프레임 이동량 계산

    Vector3 xOnlyTargetPosition = currentPosition + new Vector3(moveDelta.x, 0f, 0f); // X축만 적용한 목표 위치
    if (CanMoveCameraToPosition(xOnlyTargetPosition))
    {
        currentPosition = xOnlyTargetPosition; // X축 이동 가능하면 반영
    }

    Vector3 yOnlyTargetPosition = currentPosition + new Vector3(0f, moveDelta.y, 0f); // Y축만 적용한 목표 위치
    if (CanMoveCameraToPosition(yOnlyTargetPosition))
    {
        currentPosition = yOnlyTargetPosition; // Y축 이동 가능하면 반영
    }

    targetCamera.transform.position = currentPosition; // 최종 계산된 위치 적용
}

private void UpdateZoomInput() // Input System 기준 마우스 휠 줌 처리
{
    if (targetCamera == null || !targetCamera.orthographic)
    {
        return; // 카메라가 없거나 Orthographic이 아니면 종료
    }

    Mouse mouse = Mouse.current; // 현재 마우스 장치 참조

    if (mouse == null)
    {
        return; // 마우스가 없으면 종료
    }

    float wheelInput = mouse.scroll.ReadValue().y; // 마우스 휠 입력값 읽기

    if (Mathf.Approximately(wheelInput, 0f))
    {
        return; // 휠 입력이 없으면 종료
    }

    float targetSize = targetCamera.orthographicSize; // 목표 줌 크기

    if (wheelInput > 0f)
    {
        targetSize -= zoomInSpeed * Time.deltaTime; // 확대 처리
    }
    else if (wheelInput < 0f)
    {
        targetSize += zoomOutSpeed * Time.deltaTime; // 축소 처리
    }

    float clampedTargetSize = Mathf.Clamp(targetSize, minOrthographicSize, maxOrthographicSize); // 줌 범위 제한

    if (CanApplyOrthographicSize(clampedTargetSize))
    {
        targetCamera.orthographicSize = clampedTargetSize; // 충돌이 없을 때만 줌 적용
    }
}

    private void SyncCameraColliderToCurrentView() // 현재 카메라 투사 범위 기준으로 콜라이더 크기 동기화
{
    if (!syncColliderWithCameraView)
    {
        return; // 동기화 사용 안 하면 종료
    }

    if (targetCamera == null)
    {
        return; // 카메라가 없으면 종료
    }

    if (cameraBoxCollider2D == null)
    {
        return; // 박스 콜라이더가 없으면 종료
    }

    if (!targetCamera.orthographic)
    {
        return; // Orthographic 카메라가 아니면 종료
    }

    float cameraHalfHeight = targetCamera.orthographicSize; // 현재 카메라 반높이 계산
    float cameraHalfWidth = cameraHalfHeight * targetCamera.aspect; // 현재 카메라 반너비 계산

    float finalWidth = (cameraHalfWidth * 2f) + colliderThicknessPadding; // 최종 콜라이더 가로 크기 계산
    float finalHeight = (cameraHalfHeight * 2f) + colliderThicknessPadding; // 최종 콜라이더 세로 크기 계산

    cameraBoxCollider2D.size = new Vector2(finalWidth, finalHeight); // 카메라 투사 범위 기준 콜라이더 크기 적용
    cameraBoxCollider2D.offset = colliderOffset; // 설정된 콜라이더 오프셋 적용
}

private bool CanMoveCameraToPosition(Vector3 targetPosition) // 목표 위치로 카메라 이동 가능한지 충돌 검사
{
    if (cameraBoxCollider2D == null)
    {
        return true; // 콜라이더가 없으면 이동 허용
    }

    Vector2 checkCenter = new Vector2(
        targetPosition.x + cameraBoxCollider2D.offset.x, // 목표 위치 기준 검사 중심 X
        targetPosition.y + cameraBoxCollider2D.offset.y); // 목표 위치 기준 검사 중심 Y

    Vector2 checkSize = GetCurrentCollisionCheckBoxSize(); // 현재 카메라 기준 검사 박스 크기 계산

    Collider2D hitCollider = Physics2D.OverlapBox(
        checkCenter, // 검사 중심
        checkSize, // 검사 크기
        0f, // 회전 없음
        cameraBlockingLayerMask); // 카메라 이동 차단 레이어만 검사

    return hitCollider == null; // 겹치는 벽이 없을 때만 이동 허용
}

private bool CanApplyOrthographicSize(float targetOrthographicSize) // 목표 줌 크기 적용 가능한지 충돌 검사
{
    if (targetCamera == null)
    {
        return false; // 카메라가 없으면 적용 불가
    }

    if (cameraBoxCollider2D == null)
    {
        return true; // 콜라이더가 없으면 적용 허용
    }

    float cameraHalfHeight = targetOrthographicSize; // 목표 반높이 계산
    float cameraHalfWidth = cameraHalfHeight * targetCamera.aspect; // 목표 반너비 계산

    Vector2 targetSize = new Vector2(
        (cameraHalfWidth * 2f) + colliderThicknessPadding, // 목표 가로 크기 계산
        (cameraHalfHeight * 2f) + colliderThicknessPadding); // 목표 세로 크기 계산

    targetSize.x = Mathf.Max(0f, targetSize.x - collisionCheckSkinWidth); // 검사 여유만큼 가로 축소
    targetSize.y = Mathf.Max(0f, targetSize.y - collisionCheckSkinWidth); // 검사 여유만큼 세로 축소

    Vector3 currentPosition = targetCamera.transform.position; // 현재 카메라 위치 저장
    Vector2 checkCenter = new Vector2(
        currentPosition.x + cameraBoxCollider2D.offset.x, // 현재 위치 기준 검사 중심 X
        currentPosition.y + cameraBoxCollider2D.offset.y); // 현재 위치 기준 검사 중심 Y

    Collider2D hitCollider = Physics2D.OverlapBox(
        checkCenter, // 검사 중심
        targetSize, // 목표 줌 기준 검사 크기
        0f, // 회전 없음
        cameraBlockingLayerMask); // 카메라 이동 차단 레이어만 검사

    return hitCollider == null; // 겹치는 벽이 없을 때만 줌 허용
}

private Vector2 GetCurrentCollisionCheckBoxSize() // 현재 카메라 기준 충돌 검사 박스 크기 반환
{
    if (targetCamera == null)
    {
        return Vector2.zero; // 카메라가 없으면 0 반환
    }

    float cameraHalfHeight = targetCamera.orthographicSize; // 현재 반높이 계산
    float cameraHalfWidth = cameraHalfHeight * targetCamera.aspect; // 현재 반너비 계산

    float finalWidth = (cameraHalfWidth * 2f) + colliderThicknessPadding; // 현재 가로 크기 계산
    float finalHeight = (cameraHalfHeight * 2f) + colliderThicknessPadding; // 현재 세로 크기 계산

    finalWidth = Mathf.Max(0f, finalWidth - collisionCheckSkinWidth); // 검사 여유만큼 가로 축소
    finalHeight = Mathf.Max(0f, finalHeight - collisionCheckSkinWidth); // 검사 여유만큼 세로 축소

    return new Vector2(finalWidth, finalHeight); // 최종 검사 크기 반환
}
}