using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 네비 이동 시스템
/// - 순수 이동 처리 전담
/// - NavMesh로 경로 계산
/// - Rigidbody2D로 실제 이동
/// </summary>
public class NavigationMovementSystem : MonoBehaviour
{
    [Header("필수 참조")]
    [SerializeField] private Rigidbody2D targetRigidbody2D; // 실제 이동에 사용할 Rigidbody2D

[Header("이동 설정")]
[SerializeField] private float moveSpeed = 5f; // 기본 이동 속도
[SerializeField] private float stoppingDistance = 0.05f; // 코너 도착 판정 거리
[SerializeField] private float destinationSampleDistance = 1.5f; // NavMesh 위치 보정 탐색 반경
[SerializeField] private float slowDownDistance = 1.2f; // 마지막 코너 접근 시 감속 시작 거리
[SerializeField] private float minimumMoveSpeed = 1.5f; // 감속 시 최소 이동 속도
[SerializeField] private float finalSnapDistance = 0.08f; // 마지막 위치 고정 거리

[Header("임시 이동속도 배율")]
[SerializeField] private float temporaryMoveSpeedMultiplier = 1f; // 일시 적용 중인 이동속도 배율
[SerializeField] private bool useTemporaryMoveSpeedMultiplier; // 일시 이동속도 배율 사용 여부

    [Header("현재 이동 상태")]
    [SerializeField] private bool isMoving; // 현재 이동 중 여부

    [Header("우회 재탐색 설정")]
[SerializeField] private float repathInterval = 0.2f; // 이동 중 경로를 다시 계산할 간격
[SerializeField] private float blockedCheckDistance = 0.2f; // 현재 코너에 충분히 가까워지지 못했다고 판단할 거리 기준
[SerializeField] private float blockedCheckTime = 0.35f; // 막힘 여부를 판단하기 위한 시간 기준

[Header("외부 힘 처리 설정")]
[SerializeField] private float resumeMoveVelocityThreshold = 0.15f; // 외부 힘 종료 판단용 최소 속도 기준
[SerializeField] private float externalForceDeceleration = 12f; // 외부 힘 감속량
[SerializeField] private float externalForceStopAngularVelocityThreshold = 5f; // 회전 정지 판정 기준값

public enum FacingXDirectionType
{
    XPositive, // X+ 방향 상태
    XNegative  // X- 방향 상태
}

[Header("방향 상태")]
[SerializeField] private FacingXDirectionType currentFacingXDirection = FacingXDirectionType.XPositive; // 현재 X방향 상태
[SerializeField] private float forcedFacingDirectionThreshold = 0.001f; // 목표 위치 기준 방향 갱신 시 X축 판정 최소 기준값
[SerializeField] private float facingDirectionUpdateThreshold = 0.01f; // 방향 갱신 최소 기준값

[Header("강제 방향 고정 상태")]
[SerializeField] private bool useForcedFacingDirection; // 강제 방향 고정 사용 여부
[SerializeField] private FacingXDirectionType forcedFacingDirection = FacingXDirectionType.XPositive; // 강제로 유지할 방향값

public FacingXDirectionType CurrentFacingXDirection => currentFacingXDirection; // 현재 X방향 상태 반환

[Header("외부 힘 종료 신호 상태")]
[SerializeField] private bool didExternalForceEndThisFrame; // 이번 프레임에 외부 힘 종료가 발생했는지 여부



private bool isUnderExternalForce; // 현재 외부 힘 적용 중 여부
private bool shouldResumeMoveAfterForce; // 외부 힘 종료 후 이동 재개 여부
private Vector2 resumeDestinationAfterForce; // 외부 힘 종료 후 다시 이동할 목적지 저장

private Vector2 currentDestination; // 현재 이동 목표 지점 저장
private bool hasDestination; // 현재 유효한 목적지 보유 여부
private float repathTimer; // 주기적 재탐색용 타이머
private float blockedTimer; // 막힘 상태 누적 시간
private float lastDistanceToCorner = float.MaxValue; // 직전 프레임의 코너까지 거리 저장

public bool HasDestination => hasDestination; // 현재 목적지 보유 여부 반환
public Vector2 CurrentDestination => currentDestination; // 현재 목적지 반환

    private NavMeshPath navMeshPath; // 계산된 네비 경로
    private int currentCornerIndex; // 현재 따라가는 경로 코너 인덱스
    private bool hasValidPath; // 유효한 경로 보유 여부

    public bool IsUnderExternalForce => isUnderExternalForce; // 외부 힘 적용 중 여부 반환

    /// <summary>
    /// 현재 이동 중 여부 반환
    /// </summary>
    public bool IsMoving => isMoving; // 현재 이동 상태 반환

    private void Awake() // 초기 참조 및 경로 객체 준비
    {
        if (targetRigidbody2D == null)
        {
            targetRigidbody2D = GetComponent<Rigidbody2D>(); // Rigidbody2D 자동 참조
        }

        navMeshPath = new NavMeshPath(); // NavMesh 경로 객체 생성
    }

private void FixedUpdate() // 물리 프레임마다 실제 이동 처리
{
    HandleExternalForceState(); // 외부 힘 상태 해제 여부 처리
    HandleRepath(); // 이동 중 우회 성능 향상을 위한 재탐색 처리
    HandlePathMovement(); // 경로를 따라 실제 이동 처리
}

private void UpdateFacingDirectionState() // 현재 이동 방향 기준으로 X방향 상태 갱신
{
    if (useForcedFacingDirection)
    {
        currentFacingXDirection = forcedFacingDirection; // 강제 방향 고정 중이면 저장된 방향 유지
        return; // 일반 방향 계산 중단
    }

    if (targetRigidbody2D == null)
    {
        return; // Rigidbody2D가 없으면 종료
    }

    Vector2 referenceDirection = targetRigidbody2D.linearVelocity; // 우선 현재 실제 속도를 기준 방향으로 사용

    if (referenceDirection.sqrMagnitude <= facingDirectionUpdateThreshold * facingDirectionUpdateThreshold)
    {
        if (hasValidPath && navMeshPath != null && navMeshPath.corners != null && currentCornerIndex < navMeshPath.corners.Length)
        {
            Vector3 currentCorner3D = navMeshPath.corners[currentCornerIndex]; // 현재 목표 코너 3D 좌표
            Vector2 currentCorner2D = new Vector2(currentCorner3D.x, currentCorner3D.y); // 현재 목표 코너 2D 좌표
            referenceDirection = currentCorner2D - targetRigidbody2D.position; // 속도가 거의 없으면 다음 이동 목표 방향 사용
        }
    }

    if (Mathf.Abs(referenceDirection.x) <= facingDirectionUpdateThreshold)
    {
        return; // X방향 변화가 너무 작으면 기존 방향 유지
    }

    if (referenceDirection.x > 0f)
    {
        currentFacingXDirection = FacingXDirectionType.XPositive; // X+ 방향 상태로 갱신
    }
    else
    {
        currentFacingXDirection = FacingXDirectionType.XNegative; // X- 방향 상태로 갱신
    }
}
    /// <summary>
    /// 외부에서 목적지를 전달받아 경로를 계산
    /// </summary>
public void SetMoveDestination(Vector2 destination) // 새 목적지 설정
{
    UpdateFacingDirectionByTargetPosition(destination); // 이동 목적지 기준으로 방향 상태 먼저 갱신

    currentDestination = destination; // 현재 목적지 저장
    hasDestination = true; // 목적지 보유 상태 설정
    repathTimer = 0f; // 재탐색 타이머 초기화
    blockedTimer = 0f; // 막힘 판정 타이머 초기화
    lastDistanceToCorner = float.MaxValue; // 직전 거리 초기화

    TryCalculatePathToCurrentDestination(); // 현재 저장된 목적지 기준으로 경로 계산 시도
}

    /// <summary>
    /// 이동을 즉시 정지
    /// </summary>
public void StopMove() // 이동 정지
{
    hasDestination = false; // 현재 목적지 보유 상태 해제
    hasValidPath = false; // 경로 제거
    isMoving = false; // 이동 상태 해제
    currentCornerIndex = 0; // 코너 인덱스 초기화
    repathTimer = 0f; // 재탐색 타이머 초기화
    blockedTimer = 0f; // 막힘 판정 타이머 초기화
    lastDistanceToCorner = float.MaxValue; // 직전 거리 초기화

    if (targetRigidbody2D != null && !isUnderExternalForce)
    {
        targetRigidbody2D.linearVelocity = Vector2.zero; // 외부 힘 적용 중이 아닐 때만 현재 속도 정지
    }
}

    /// <summary>
    /// 경로를 따라 실제 이동 처리
    /// </summary>
private void HandlePathMovement() // 경로 이동 처리
{
    if (targetRigidbody2D == null)
    {
        return; // Rigidbody2D가 없으면 종료
    }

    if (isUnderExternalForce)
    {
        return; // 외부 힘 적용 중에는 네비 이동 중단
    }

    if (!hasValidPath || !isMoving || navMeshPath == null || navMeshPath.corners == null || navMeshPath.corners.Length == 0)
    {
        targetRigidbody2D.linearVelocity = Vector2.zero; // 이동 조건이 아니면 속도 정지
        return;
    }

    if (currentCornerIndex >= navMeshPath.corners.Length)
    {
        StopMove(); // 모든 코너에 도달했으면 정지
        return;
    }

    Vector3 currentCorner3D = navMeshPath.corners[currentCornerIndex]; // 현재 목표 코너 3D 좌표
    Vector2 currentCorner2D = new Vector2(currentCorner3D.x, currentCorner3D.y); // 현재 목표 코너 2D 좌표
    Vector2 currentPosition2D = targetRigidbody2D.position; // 현재 Rigidbody2D 위치
    float distanceToCorner = Vector2.Distance(currentPosition2D, currentCorner2D); // 현재 코너까지 거리
    bool isLastCorner = currentCornerIndex >= navMeshPath.corners.Length - 1; // 마지막 코너 여부

    if (distanceToCorner <= stoppingDistance)
    {
        if (isLastCorner)
        {
            SnapToPositionAndStop(currentCorner2D); // 마지막 코너면 위치 보정 후 정지
            return;
        }

        currentCornerIndex++; // 다음 코너로 이동
        blockedTimer = 0f; // 코너를 정상적으로 넘겼으므로 막힘 판정 초기화
        lastDistanceToCorner = float.MaxValue; // 다음 코너 판단을 위해 직전 거리 초기화

        if (currentCornerIndex >= navMeshPath.corners.Length)
        {
            StopMove(); // 다음 코너가 없으면 정지
            return;
        }

        currentCorner3D = navMeshPath.corners[currentCornerIndex]; // 다음 목표 코너 갱신
        currentCorner2D = new Vector2(currentCorner3D.x, currentCorner3D.y); // 다음 목표 코너 2D 변환
        currentPosition2D = targetRigidbody2D.position; // 현재 위치 갱신
        distanceToCorner = Vector2.Distance(currentPosition2D, currentCorner2D); // 다음 코너까지 거리 재계산
        isLastCorner = currentCornerIndex >= navMeshPath.corners.Length - 1; // 마지막 코너 여부 재계산
    }

    if (isLastCorner && distanceToCorner <= finalSnapDistance)
    {
        SnapToPositionAndStop(currentCorner2D); // 마지막 코너에 매우 가까우면 위치 고정 후 정지
        return;
    }

    UpdateBlockedState(distanceToCorner); // 현재 코너 접근 진행 여부를 검사해 막힘 상태 갱신

    Vector2 moveDirection = (currentCorner2D - currentPosition2D).normalized; // 이동 방향 계산
    float appliedMoveSpeed = GetAppliedMoveSpeed(distanceToCorner, isLastCorner); // 실제 적용 이동 속도 계산
    targetRigidbody2D.linearVelocity = moveDirection * appliedMoveSpeed; // 실제 이동 속도 적용
}

    /// <summary>
    /// 마지막 코너 접근 시 적용할 실제 이동 속도 계산
    /// </summary>
private float GetAppliedMoveSpeed(float distanceToCorner, bool isLastCorner) // 감속 적용 속도 계산
{
    float currentMoveSpeed = GetCurrentMoveSpeed(); // 현재 실제 이동속도 계산

    if (!isLastCorner)
    {
        return currentMoveSpeed; // 마지막 코너가 아니면 현재 속도 유지
    }

    if (distanceToCorner >= slowDownDistance)
    {
        return currentMoveSpeed; // 감속 시작 거리보다 멀면 현재 속도 유지
    }

    float slowDownRatio = Mathf.Clamp01(distanceToCorner / slowDownDistance); // 감속 비율 계산
    float slowedSpeed = Mathf.Lerp(minimumMoveSpeed, currentMoveSpeed, slowDownRatio); // 거리 비율에 따라 속도 보간

    return Mathf.Max(minimumMoveSpeed, slowedSpeed); // 최소 이동 속도 보정
}

    /// <summary>
    /// 마지막 위치를 정확히 맞춘 뒤 이동 정지
    /// </summary>
    private void SnapToPositionAndStop(Vector2 targetPosition) // 위치 보정 후 정지
    {
        if (targetRigidbody2D == null)
        {
            return; // Rigidbody2D가 없으면 종료
        }

        targetRigidbody2D.linearVelocity = Vector2.zero; // 잔여 속도 제거
        targetRigidbody2D.MovePosition(targetPosition); // 목표 위치로 이동
        StopMove(); // 이동 종료
    }

private void OnDisable() // 비활성화 시 이동 정리
{
    ClearTemporaryMoveSpeedMultiplier(); // 비활성화 시 임시 이동속도 배율 해제
    StopMove(); // 비활성화될 때 이동 정지
}

    private void TryCalculatePathToCurrentDestination() // 현재 저장된 목적지 기준으로 경로 계산
{
    if (!hasDestination)
    {
        StopMove(); // 목적지가 없으면 이동 중지
        return;
    }

    Vector3 rawStartPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z); // 현재 위치 3D 변환
    Vector3 rawTargetPosition = new Vector3(currentDestination.x, currentDestination.y, transform.position.z); // 목적지 위치 3D 변환

    Vector3 finalStartPosition = rawStartPosition; // 실제 경로 계산용 시작 위치
    Vector3 finalTargetPosition = rawTargetPosition; // 실제 경로 계산용 목적지 위치

    NavMeshHit sampledStartHit; // 시작 위치 보정용 히트 정보
    if (NavMesh.SamplePosition(rawStartPosition, out sampledStartHit, destinationSampleDistance, NavMesh.AllAreas))
    {
        finalStartPosition = sampledStartHit.position; // 시작 위치 NavMesh 보정
    }

    NavMeshHit sampledTargetHit; // 목적지 보정용 히트 정보
    if (NavMesh.SamplePosition(rawTargetPosition, out sampledTargetHit, destinationSampleDistance, NavMesh.AllAreas))
    {
        finalTargetPosition = sampledTargetHit.position; // 목적지 위치 NavMesh 보정
    }

    bool pathCalculated = NavMesh.CalculatePath(finalStartPosition, finalTargetPosition, NavMesh.AllAreas, navMeshPath); // 경로 계산 시도

    if (!pathCalculated || navMeshPath == null || navMeshPath.corners == null || navMeshPath.corners.Length == 0 || navMeshPath.status != NavMeshPathStatus.PathComplete)
    {
        hasValidPath = false; // 유효 경로 해제
        isMoving = false; // 이동 상태 해제

        if (targetRigidbody2D != null)
        {
            targetRigidbody2D.linearVelocity = Vector2.zero; // 속도 정지
        }

        return; // 경로가 없으면 종료
    }

    hasValidPath = true; // 유효 경로 설정
    isMoving = true; // 이동 상태 설정
    currentCornerIndex = navMeshPath.corners.Length > 1 ? 1 : 0; // 첫 코너는 현재 위치일 수 있으므로 다음 코너부터 시작
    blockedTimer = 0f; // 막힘 판정 초기화
    lastDistanceToCorner = float.MaxValue; // 직전 거리 초기화
}

private void HandleRepath() // 이동 중 주기적 재탐색 처리
{
    if (!isMoving || !hasDestination)
    {
        return; // 이동 중이 아니거나 목적지가 없으면 종료
    }

    repathTimer += Time.fixedDeltaTime; // 재탐색 타이머 누적

    if (repathTimer < repathInterval)
    {
        return; // 아직 재탐색 간격이 지나지 않았으면 종료
    }

    repathTimer = 0f; // 재탐색 타이머 초기화
    TryCalculatePathToCurrentDestination(); // 현재 목적지 기준으로 경로 재계산
}

private void UpdateBlockedState(float distanceToCorner) // 코너 접근 진행 여부를 바탕으로 막힘 상태 갱신
{
    if (lastDistanceToCorner == float.MaxValue)
    {
        lastDistanceToCorner = distanceToCorner; // 첫 비교용 거리 저장
        return;
    }

    float distanceDelta = lastDistanceToCorner - distanceToCorner; // 직전 프레임 대비 얼마나 가까워졌는지 계산

    if (distanceDelta <= blockedCheckDistance)
    {
        blockedTimer += Time.fixedDeltaTime; // 충분히 전진하지 못하면 막힘 시간 누적
    }
    else
    {
        blockedTimer = 0f; // 정상적으로 전진 중이면 막힘 시간 초기화
    }

    lastDistanceToCorner = distanceToCorner; // 현재 거리를 다음 프레임 비교용으로 저장

    if (blockedTimer >= blockedCheckTime)
    {
        blockedTimer = 0f; // 막힘 판정 타이머 초기화
        TryCalculatePathToCurrentDestination(); // 현재 위치 기준으로 즉시 경로 재계산
    }
}

public void ApplyExternalForce(Vector2 force, bool resumeMoveAfterForce) // 외부 힘 적용
{
    if (targetRigidbody2D == null)
    {
        return; // Rigidbody2D가 없으면 종료
    }

    didExternalForceEndThisFrame = false; // 새 외부 힘 시작 시 종료 신호 초기화

    if (hasDestination)
    {
        resumeDestinationAfterForce = currentDestination; // 외부 힘 종료 후 복귀할 목적지 저장
    }

    shouldResumeMoveAfterForce = resumeMoveAfterForce && hasDestination; // 외부 힘 종료 후 이동 재개 여부 저장
    isUnderExternalForce = true; // 외부 힘 적용 상태 설정

    hasValidPath = false; // 기존 경로 사용 중단
    isMoving = false; // 네비 이동 일시 정지
    currentCornerIndex = 0; // 코너 인덱스 초기화
    repathTimer = 0f; // 재탐색 타이머 초기화
    blockedTimer = 0f; // 막힘 판정 타이머 초기화
    lastDistanceToCorner = float.MaxValue; // 거리 비교값 초기화

    targetRigidbody2D.linearVelocity = Vector2.zero; // 기존 이동 속도 완전 제거
    targetRigidbody2D.angularVelocity = 0f; // 기존 회전 속도 제거

    targetRigidbody2D.AddForce(force, ForceMode2D.Impulse); // 외부 힘 즉시 적용
}

private void HandleExternalForceState() // 외부 힘 적용 상태 처리
{
    if (!isUnderExternalForce || targetRigidbody2D == null)
    {
        return; // 외부 힘 적용 중이 아니면 종료
    }

    Vector2 currentVelocity = targetRigidbody2D.linearVelocity; // 현재 선속도 저장
    float currentSpeed = currentVelocity.magnitude; // 현재 속도 크기 저장

    if (currentSpeed > 0f)
    {
        float nextSpeed = Mathf.MoveTowards(currentSpeed, 0f, externalForceDeceleration * Time.fixedDeltaTime); // 감속 적용 후 다음 속도 계산
        Vector2 nextVelocity = currentVelocity.normalized * nextSpeed; // 현재 방향을 유지한 채 감속 속도 계산

        if (currentSpeed <= resumeMoveVelocityThreshold)
        {
            nextVelocity = Vector2.zero; // 일정 속도 이하이면 완전 정지 처리
        }

        targetRigidbody2D.linearVelocity = nextVelocity; // 감속된 속도 적용
    }

    if (Mathf.Abs(targetRigidbody2D.angularVelocity) <= externalForceStopAngularVelocityThreshold)
    {
        targetRigidbody2D.angularVelocity = 0f; // 작은 회전값은 완전히 제거
    }

    bool isLinearStopped = targetRigidbody2D.linearVelocity.magnitude <= resumeMoveVelocityThreshold; // 선속도 정지 여부 판정
    bool isAngularStopped = Mathf.Abs(targetRigidbody2D.angularVelocity) <= 0f; // 회전속도 정지 여부 판정

    if (!isLinearStopped || !isAngularStopped)
    {
        return; // 아직 물리 작용이 남아 있으면 종료
    }

    targetRigidbody2D.linearVelocity = Vector2.zero; // 선속도 완전 제거
    targetRigidbody2D.angularVelocity = 0f; // 회전속도 완전 제거
    isUnderExternalForce = false; // 외부 힘 적용 상태 해제
    didExternalForceEndThisFrame = true; // 외부 힘 종료 신호 기록

    if (shouldResumeMoveAfterForce && hasDestination)
    {
        shouldResumeMoveAfterForce = false; // 이동 재개 예약 해제
        SetMoveDestination(resumeDestinationAfterForce); // 이전 목적지로 이동 재개
        return;
    }

    shouldResumeMoveAfterForce = false; // 이동 재개 예약 해제
}

private float GetCurrentMoveSpeed() // 현재 실제 적용할 이동속도 계산
{
    if (!useTemporaryMoveSpeedMultiplier)
    {
        return moveSpeed; // 임시 배율을 사용하지 않으면 기본 속도 반환
    }

    return moveSpeed * Mathf.Max(0f, temporaryMoveSpeedMultiplier); // 기본 속도에 배율을 곱한 값 반환
}

public void SetTemporaryMoveSpeedMultiplier(float multiplier) // 임시 이동속도 배율 적용
{
    temporaryMoveSpeedMultiplier = multiplier; // 임시 배율 저장
    useTemporaryMoveSpeedMultiplier = true; // 임시 배율 사용 상태 설정
}

public void ClearTemporaryMoveSpeedMultiplier() // 임시 이동속도 배율 해제
{
    temporaryMoveSpeedMultiplier = 1f; // 임시 배율 기본값 복구
    useTemporaryMoveSpeedMultiplier = false; // 임시 배율 사용 상태 해제
}

public void SetFacingDirectionByTargetPosition(Vector2 selfPosition, Vector2 targetPosition) // 대상의 X축 위치 기준으로 방향 상태 강제 설정
{
    float deltaX = targetPosition.x - selfPosition.x; // 자신 대비 대상의 X축 차이값 계산

    if (Mathf.Abs(deltaX) <= forcedFacingDirectionThreshold)
    {
        return; // X축 차이가 너무 작으면 기존 방향 유지
    }

    if (deltaX > 0f)
    {
        currentFacingXDirection = FacingXDirectionType.XPositive; // 대상이 X+ 쪽에 있으면 X+ 방향으로 강제 설정
    }
    else
    {
        currentFacingXDirection = FacingXDirectionType.XNegative; // 대상이 X- 쪽에 있으면 X- 방향으로 강제 설정
    }
}

public void UpdateFacingDirectionByTargetPosition(Vector2 targetPosition) // 현재 목표 위치 기준으로 방향 상태 갱신
{
    float deltaX = targetPosition.x - transform.position.x; // 자신 대비 목표 위치의 X축 차이값 계산

    if (Mathf.Abs(deltaX) <= forcedFacingDirectionThreshold)
    {
        return; // X축 차이가 너무 작으면 기존 방향 유지
    }

    if (deltaX > 0f)
    {
        currentFacingXDirection = FacingXDirectionType.XPositive; // 목표가 X+ 쪽에 있으면 X+ 방향으로 갱신
    }
    else
    {
        currentFacingXDirection = FacingXDirectionType.XNegative; // 목표가 X- 쪽에 있으면 X- 방향으로 갱신
    }
}

public bool ConsumeExternalForceEndedSignal() // 외부 힘 종료 신호를 1회 소비
{
    bool result = didExternalForceEndThisFrame; // 현재 종료 신호 저장
    didExternalForceEndThisFrame = false; // 소비 후 즉시 초기화
    return result; // 저장된 종료 신호 반환
}

public void SetForcedFacingDirection(FacingXDirectionType directionType) // 강제 방향 고정 시작
{
    forcedFacingDirection = directionType; // 고정할 방향 저장
    useForcedFacingDirection = true; // 강제 방향 사용 시작
    currentFacingXDirection = forcedFacingDirection; // 내부 방향값 즉시 반영
}

public void ClearForcedFacingDirection() // 강제 방향 고정 해제
{
    useForcedFacingDirection = false; // 강제 방향 사용 해제
}
}