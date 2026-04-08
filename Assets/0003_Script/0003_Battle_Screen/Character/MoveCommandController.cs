using UnityEngine;

/// <summary>
/// 이동 명령 컨트롤러
/// - 선택 판정 관리
/// - 선택 표시 관리
/// - 이동 명령 전달 관리
/// </summary>
public class MoveCommandController : MonoBehaviour
{
    [Header("필수 참조")]
    [SerializeField] private NavigationMovementSystem navigationMovementSystem; // 실제 이동을 담당하는 네비 이동 시스템
    [SerializeField] private Collider2D selectionCollider2D; // 선택 판정에 사용할 2D 콜라이더

    public enum FacingXDirectionType
{
    XPositive, // X+ 방향 상태
    XNegative  // X- 방향 상태
}

[Header("방향 상태")]
[SerializeField] private FacingXDirectionType currentFacingXDirection = FacingXDirectionType.XPositive; // 현재 캐릭터의 X방향 상태

[Header("방향 연동 회전 설정")]
[SerializeField] private GameObject directionLinkedObject; // 방향값에 따라 회전을 적용할 오브젝트
[SerializeField] private Vector3 xPositiveRotationEuler; // X+ 방향일 때 적용할 회전값
[SerializeField] private Vector3 xNegativeRotationEuler; // X- 방향일 때 적용할 회전값

public FacingXDirectionType CurrentFacingXDirection => currentFacingXDirection; // 현재 캐릭터 방향 상태 반환

    [Header("현재 상태")]
    [SerializeField] private bool isSelected; // 현재 선택 여부
    [SerializeField] private bool isMoving; // 현재 이동 중 여부

    [SerializeField] private bool isMoveCommandLocked; // 이동 명령 잠금 여부

    [SerializeField] private Vector2 lastMoveDestination; // 마지막으로 전달한 이동 목적지 저장
    [SerializeField] private bool hasMoveDestination; // 현재 이동 목적지 보유 여부
    [SerializeField] private CharacterDuelAI characterDuelAI; // 공격 명령 전달용 참조

    public CharacterDuelAI CurrentAttackTarget => characterDuelAI != null ? characterDuelAI.CurrentTarget : null; // 현재 공격 대상으로 잡은 대상 반환
    public bool IsChasingAttackTarget => characterDuelAI != null && characterDuelAI.IsAutoChasingCurrentTarget; // 현재 공격 대상 추적 이동 중 여부 반환

    public bool HasMoveDestination => hasMoveDestination; // 이동 목적지 보유 여부 반환
    public Vector2 LastMoveDestination => lastMoveDestination; // 마지막 이동 목적지 반환

    public bool IsMoveCommandLocked => isMoveCommandLocked; // 이동 명령 잠금 여부 반환
    public NavigationMovementSystem NavigationMovementSystem => navigationMovementSystem; // 네비 이동 시스템 반환

    /// <summary>
    /// 외부에서 선택용 콜라이더 접근 시 사용
    /// </summary>
    public Collider2D SelectionCollider2D => selectionCollider2D; // 선택용 콜라이더 반환

    /// <summary>
    /// 외부에서 현재 선택 여부 확인 시 사용
    /// </summary>
    public bool IsSelected => isSelected; // 현재 선택 여부 반환

    /// <summary>
    /// 외부에서 현재 이동 여부 확인 시 사용
    /// </summary>
    public bool IsMoving => isMoving; // 현재 이동 여부 반환

private void Awake() // 초기 참조 설정
{
    if (navigationMovementSystem == null) // 네비 이동 시스템 참조 확인
    {
        navigationMovementSystem = GetComponent<NavigationMovementSystem>(); // 동일 오브젝트에서 자동 참조
    }

    if (selectionCollider2D == null) // 선택 콜라이더 참조 확인
    {
        selectionCollider2D = GetComponent<Collider2D>(); // 동일 오브젝트에서 자동 참조
    }

    if (characterDuelAI == null) // CharacterDuelAI 참조 확인
    {
        characterDuelAI = GetComponent<CharacterDuelAI>(); // 동일 오브젝트에서 자동 참조
    }

    UpdateMovingState(); // 시작 시 이동 상태 갱신
}

private void Update() // 상태 동기화 처리
{
    UpdateMovingState(); // 네비 이동 시스템의 이동 상태를 현재 상태에 반영
    UpdateFacingDirectionState(); // 현재 방향 상태 갱신
    ApplyDirectionLinkedObjectRotation(); // 방향 상태에 맞는 회전값 즉시 적용
}

private void UpdateFacingDirectionState() // NavigationMovementSystem 기준으로 현재 방향 상태 갱신
{
    if (navigationMovementSystem == null)
    {
        return; // 네비 이동 시스템이 없으면 종료
    }

    if (characterDuelAI != null && characterDuelAI.IsFacingDirectionLockedAfterDuel)
    {
        if (characterDuelAI.LockedFacingDirectionAfterDuel == NavigationMovementSystem.FacingXDirectionType.XPositive)
        {
            currentFacingXDirection = FacingXDirectionType.XPositive; // 결투 후 고정 방향이 X+면 그대로 유지
        }
        else
        {
            currentFacingXDirection = FacingXDirectionType.XNegative; // 결투 후 고정 방향이 X-면 그대로 유지
        }

        return; // 고정 중이면 일반 방향 동기화 중단
    }

    if (navigationMovementSystem.CurrentFacingXDirection == NavigationMovementSystem.FacingXDirectionType.XPositive)
    {
        currentFacingXDirection = FacingXDirectionType.XPositive; // X+ 방향 상태 반영
    }
    else
    {
        currentFacingXDirection = FacingXDirectionType.XNegative; // X- 방향 상태 반영
    }
}

private void ApplyDirectionLinkedObjectRotation() // 현재 방향 상태에 따라 연결 오브젝트 회전 즉시 적용
{
    if (directionLinkedObject == null)
    {
        return; // 연결 오브젝트가 없으면 종료
    }

    if (characterDuelAI != null && characterDuelAI.IsFacingDirectionLockedAfterDuel)
    {
        if (currentFacingXDirection == FacingXDirectionType.XPositive)
        {
            directionLinkedObject.transform.localRotation = Quaternion.Euler(xPositiveRotationEuler); // 방향 고정 중 X+ 회전 유지
        }
        else
        {
            directionLinkedObject.transform.localRotation = Quaternion.Euler(xNegativeRotationEuler); // 방향 고정 중 X- 회전 유지
        }

        return; // 방향 고정 중이면 이 회전값 유지
    }

    if (characterDuelAI != null && characterDuelAI.IsDirectionLinkedRotationLocked)
    {
        return; // 결투 돌진 시작 ~ 넉백 종료까지는 회전 적용 차단
    }

    if (currentFacingXDirection == FacingXDirectionType.XPositive)
    {
        directionLinkedObject.transform.localRotation = Quaternion.Euler(xPositiveRotationEuler); // X+ 방향 회전값 적용
    }
    else
    {
        directionLinkedObject.transform.localRotation = Quaternion.Euler(xNegativeRotationEuler); // X- 방향 회전값 적용
    }
}
    public void SetPriorityAttackTarget(CharacterDuelAI target)
{
    if (characterDuelAI == null)
    {
        return;
    }

    characterDuelAI.SetPriorityTarget(target);
}

    /// <summary>
    /// 선택 상태 설정
    /// </summary>
public void SetSelected(bool selected) // 선택 상태 설정
{
    isSelected = selected; // 선택 여부 저장
}

    /// <summary>
    /// 외부에서 이동 목적지 명령 전달
    /// </summary>
public void SetMoveDestination(Vector2 destination) // 이동 목적지 전달
{
    if (navigationMovementSystem == null)
    {
        return; // 네비 이동 시스템이 없으면 종료
    }

    if (isMoveCommandLocked)
    {
        return; // 이동 명령 잠금 상태면 목적지 전달 차단
    }

    if (characterDuelAI != null)
    {
        if (characterDuelAI.IsMoveCommandBlockedByDuelState)
        {
            return; // 결투 넉백/후딜 보호 상태면 새 이동 명령 차단
        }

        CharacterStatSystem statSystem = characterDuelAI.GetCharacterStatSystem(); // 현재 캐릭터 스탯 참조 가져오기

        if (statSystem != null && statSystem.IsActionLocked)
        {
            return; // 행동 잠금 상태면 새 이동 명령 차단
        }

        characterDuelAI.ClearPriorityTarget(); // 수동 이동 명령이 들어오면 직접 지정 공격 대상 해제
    }

    lastMoveDestination = destination; // 마지막 이동 목적지 저장
    hasMoveDestination = true; // 이동 목적지 보유 상태 설정

    navigationMovementSystem.SetMoveDestination(destination); // 실제 이동 스크립트에 목적지 전달
    ApplyDirectionLinkedRotationImmediatelyFromCurrentFacing(); // 갱신된 방향 상태 기준으로 회전 즉시 1회 적용
    UpdateMovingState(); // 이동 상태 갱신
}



    /// <summary>
    /// 외부에서 이동 중지 명령 전달
    /// </summary>
public void StopMove() // 이동 중지 전달
{
    if (navigationMovementSystem == null)
    {
        return; // 네비 이동 시스템이 없으면 종료
    }

    hasMoveDestination = false; // 이동 목적지 보유 상태 해제
    navigationMovementSystem.StopMove(); // 실제 이동 정지 전달
    UpdateMovingState(); // 이동 상태 갱신
}


    /// <summary>
    /// 실제 이동 스크립트의 이동 상태를 현재 변수에 반영
    /// </summary>
private void UpdateMovingState() // 이동 상태 동기화
{
    if (navigationMovementSystem == null)
    {
        isMoving = false; // 네비 이동 시스템이 없으면 이동 안 함 처리
        return;
    }

    isMoving = navigationMovementSystem.IsMoving; // 실제 이동 상태를 현재 변수에 반영

    if (hasMoveDestination && !navigationMovementSystem.HasDestination)
    {
        hasMoveDestination = false; // 수동 이동 목적지 도착/해제 시 목적지 보유 상태 해제
    }
}

private void OnDisable() // 비활성화 시 상태 정리
{
    StopMove(); // 비활성화될 때 이동 정지
    SetSelected(false); // 비활성화될 때 선택 해제
}

    public void SetMoveCommandLocked(bool locked) // 이동 명령 잠금 상태 설정
{
    isMoveCommandLocked = locked; // 이동 명령 잠금 여부 저장

    if (isMoveCommandLocked)
    {
        StopMove(); // 잠금 시 현재 이동 정지
    }
}

public void ApplyDirectionLinkedRotationImmediatelyFromCurrentFacing() // 현재 방향 상태 기준 회전을 즉시 1회 적용
{
    UpdateFacingDirectionState(); // NavigationMovementSystem의 최신 방향 상태를 즉시 반영
    ApplyDirectionLinkedObjectRotation(); // 현재 방향 상태에 맞는 회전값을 즉시 적용
}

}