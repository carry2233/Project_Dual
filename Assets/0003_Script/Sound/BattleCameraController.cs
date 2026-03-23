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
    }

    private void Update() // 매 프레임 입력 처리
    {
        UpdateMoveInput(); // 이동 입력 처리
        UpdateZoomInput(); // 줌 입력 처리
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

        targetCamera.transform.position += moveDirection * moveSpeed * Time.deltaTime; // 카메라 이동 적용
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

        targetCamera.orthographicSize = Mathf.Clamp(targetSize, minOrthographicSize, maxOrthographicSize); // 줌 범위 제한 후 적용
    }
}