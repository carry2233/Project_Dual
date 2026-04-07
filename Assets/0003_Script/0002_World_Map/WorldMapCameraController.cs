using UnityEngine;
using UnityEngine.InputSystem; // 새 입력 시스템 사용

public class WorldMapCameraController : MonoBehaviour
{
[Header("이동 대상 오브젝트")]
[SerializeField] private Transform movementTarget; // 실제로 이동시킬 대상 오브젝트

[Header("리지드바디 미끄러짐 제거")]
[SerializeField] private Rigidbody movementTargetRigidbody; // 이동 대상이 가진 리지드바디 참조

[Header("이동 키 설정")]
[SerializeField] private Key moveUpKey = Key.W; // 위로 이동 키 (Z+)
[SerializeField] private Key moveDownKey = Key.S; // 아래로 이동 키 (Z-)
[SerializeField] private Key moveLeftKey = Key.A; // 왼쪽 이동 키 (X-)
[SerializeField] private Key moveRightKey = Key.D; // 오른쪽 이동 키 (X+)

[Header("이동 속도 설정")]
[SerializeField] private float baseMoveSpeed = 5f; // 기본 이동 속도
[SerializeField] private int finalMoveSpeedPercent = 100; // 최종 적용 이동 속도 퍼센트값

[Header("이동 잠금 설정")]
[SerializeField] private bool isMovementLocked = false; // 외부 연출 상태에 따라 이동을 잠글지 여부

    private void Reset() // 컴포넌트 추가 시 기본 참조 자동 설정
    {
        if (movementTarget == null)
        {
            movementTarget = transform; // 기본적으로 자기 자신을 이동 대상으로 사용
        }
    }

private void Awake() // 실행 시작 시 참조 보정
{
    if (movementTarget == null)
    {
        movementTarget = transform; // 참조가 비어 있으면 자기 자신을 이동 대상으로 사용
    }

    if (movementTargetRigidbody == null && movementTarget != null)
    {
        movementTargetRigidbody = movementTarget.GetComponent<Rigidbody>(); // 이동 대상에서 리지드바디 자동 탐색
    }
}

    private void Update() // 매 프레임 입력 감지 및 이동 처리
    {
        HandleMovement();
    }

    private void FixedUpdate() // 물리 프레임마다 리지드바디 미끄러짐 제거 처리
{
    ApplyHardStopToRigidbody();
}

private void HandleMovement() // 새 입력 시스템 기준으로 X/Z 평면 이동 처리
{
    if (movementTarget == null)
    {
        return; // 이동 대상이 없으면 종료
    }

    if (isMovementLocked == true)
    {
        return; // 이동 잠금 상태면 입력 이동 차단
    }

    if (Keyboard.current == null)
    {
        return; // 키보드 입력 장치가 없으면 종료
    }

    Vector3 moveDirection = Vector3.zero; // 누적 이동 방향

    if (Keyboard.current[moveUpKey].isPressed)
    {
        moveDirection += Vector3.forward; // 위 = Z+
    }

    if (Keyboard.current[moveDownKey].isPressed)
    {
        moveDirection += Vector3.back; // 아래 = Z-
    }

    if (Keyboard.current[moveLeftKey].isPressed)
    {
        moveDirection += Vector3.left; // 좌 = X-
    }

    if (Keyboard.current[moveRightKey].isPressed)
    {
        moveDirection += Vector3.right; // 우 = X+
    }

    if (moveDirection.sqrMagnitude > 1f)
    {
        moveDirection.Normalize(); // 대각선 이동 속도 보정
    }

    float finalMoveSpeed = GetFinalMoveSpeed(); // 최종 적용 이동 속도 계산
    Vector3 moveAmount = moveDirection * finalMoveSpeed * Time.deltaTime; // 프레임 이동량 계산

    movementTarget.position += moveAmount; // 대상 오브젝트 이동 적용
}

    private float GetFinalMoveSpeed() // 퍼센트가 반영된 최종 이동 속도 반환
    {
        return baseMoveSpeed * (finalMoveSpeedPercent / 100f); // 기본 속도에 퍼센트 적용
    }

    public void SetFinalMoveSpeedPercent(int newPercent) // 외부에서 최종 속도 퍼센트 변경
    {
        finalMoveSpeedPercent = newPercent; // 새 퍼센트값 저장
    }

    public void SetBaseMoveSpeed(float newSpeed) // 외부에서 기본 이동 속도 변경
    {
        baseMoveSpeed = newSpeed; // 새 기본 속도 저장
    }

    public float GetCurrentFinalMoveSpeed() // 현재 최종 적용 속도 확인용
    {
        return GetFinalMoveSpeed(); // 계산된 최종 속도 반환
    }

private void ApplyHardStopToRigidbody() // 입력이 없을 때 리지드바디를 완전 정지시키는 처리
{
    if (movementTargetRigidbody == null)
    {
        return; // 리지드바디가 없으면 종료
    }

    if (isMovementLocked == true)
    {
        movementTargetRigidbody.linearVelocity = Vector3.zero; // 이동 잠금 상태면 이동 속도 정지
        movementTargetRigidbody.angularVelocity = Vector3.zero; // 이동 잠금 상태면 회전 속도 정지
        return;
    }

    if (Keyboard.current == null)
    {
        movementTargetRigidbody.linearVelocity = Vector3.zero; // 키보드 장치가 없으면 완전 정지
        movementTargetRigidbody.angularVelocity = Vector3.zero; // 회전도 함께 정지
        return;
    }

    bool isAnyMoveKeyPressed =
        Keyboard.current[moveUpKey].isPressed || // 위 키 입력 여부
        Keyboard.current[moveDownKey].isPressed || // 아래 키 입력 여부
        Keyboard.current[moveLeftKey].isPressed || // 왼쪽 키 입력 여부
        Keyboard.current[moveRightKey].isPressed; // 오른쪽 키 입력 여부

    if (isAnyMoveKeyPressed)
    {
        return; // 이동 입력 중이면 강제 정지하지 않음
    }

    movementTargetRigidbody.linearVelocity = Vector3.zero; // 입력이 없으면 이동 속도 완전 정지
    movementTargetRigidbody.angularVelocity = Vector3.zero; // 입력이 없으면 회전 속도도 완전 정지
}

public void SetMovementLock(bool shouldLock) // 외부에서 이동 잠금 여부 설정
{
    isMovementLocked = shouldLock; // 이동 잠금 상태 저장

    if (isMovementLocked == true && movementTargetRigidbody != null)
    {
        movementTargetRigidbody.linearVelocity = Vector3.zero; // 잠금 즉시 이동 속도 정지
        movementTargetRigidbody.angularVelocity = Vector3.zero; // 잠금 즉시 회전 속도 정지
    }
}

public bool IsMovementLocked() // 현재 이동 잠금 상태 반환
{
    return isMovementLocked; // 현재 잠금 상태 반환
}

}