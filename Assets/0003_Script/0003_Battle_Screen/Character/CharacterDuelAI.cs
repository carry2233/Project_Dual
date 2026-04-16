using UnityEngine;
using System.Collections.Generic;
using System.Text; // 상세 결투 로그 문자열 조립용

/// <summary>
/// 공통 캐릭터 결투 AI
/// - 팀 번호가 다른 캐릭터와만 결투 가능
/// - 플레이어 조작형 / 적 AI 조작형 설정 가능
/// - 결투 시작, 돌진, 판정, 종료를 공통 처리
/// </summary>
public class CharacterDuelAI : MonoBehaviour
{
    public enum ControlMode
    {
        PlayerControlled,   // 플레이어 조작형
        EnemyAIControlled   // 적 AI 조작형
    }

    [Header("필수 참조")]
    [SerializeField] private CharacterStatSystem characterStatSystem; // 캐릭터 스탯 참조
    [SerializeField] private NavigationMovementSystem navigationMovementSystem; // 이동 시스템 참조
    [SerializeField] private MoveCommandController moveCommandController; // 플레이어 이동 명령 컨트롤러 참조
    [SerializeField] private EnemyAIController enemyAIController; // 적 AI 명령 컨트롤러 참조

    [Header("물리 접촉 무시 설정")]
[SerializeField] private Collider2D physicalContactCollider; // 본인 물리접촉 전용 콜라이더
[SerializeField] private float duelTargetCollisionIgnoreDuration = 0.2f; // 돌진 시작 후 상대와 충돌 무시할 시간

[Header("현재 물리 접촉 무시 상태")]
[SerializeField] private bool isIgnoringDuelTargetCollision; // 현재 결투 상대와 충돌 무시 중인지 여부
[SerializeField] private float currentDuelTargetCollisionIgnoreTimer; // 현재 충돌 무시 남은 시간
[SerializeField] private Collider2D currentIgnoredTargetPhysicalContactCollider; // 현재 충돌 무시 중인 상대 물리접촉 콜라이더

public Collider2D PhysicalContactCollider => physicalContactCollider; // 본인 물리접촉 전용 콜라이더 반환

    [Header("샌드백 대상 참조")]
    [SerializeField] private DuelSandbagTarget duelSandbagTarget; // 자신이 샌드백 대상인지 확인할 참조

    [Header("소속 / 조작 설정")]
    [SerializeField] private int teamNumber = 0; // 현재 캐릭터의 팀 번호
    [SerializeField] private ControlMode controlMode = ControlMode.PlayerControlled; // 현재 캐릭터의 조작 방식

    [Header("캐릭터 식별 정보")]
[SerializeField] private int firstRowID = 0; // 캐릭터 1열 ID
[SerializeField] private int secondRowID = 0; // 캐릭터 2열 ID
[SerializeField] private int individualID = 0; // 캐릭터 개체별 ID (0 이하면 미할당으로 간주)

[Header("결투 거리 설정")]
[SerializeField] private float searchRange = 20f; // 탐색 거리
[SerializeField] private float dashStartDistance = 4f; // 결투 돌진 시작 거리
[SerializeField] private float clashDistance = 1.2f; // 결투 판정 거리
[SerializeField] private float dashMoveSpeed = 3f; // 돌진 중 현재 이동속도에 곱할 배율

private enum PlayerControlledActionType // 플레이어 조작형 행동 분류
{
    None,           // 선택된 행동 없음
    MoveCommand,    // 이동 명령 수행
    AttackTarget    // 공격 대상 추적
}

[Header("플레이어 조작형 행동 우선도")]

[SerializeField] private int priorityTargetPriority = 1; // 직접 지정 공격 대상 우선도
[SerializeField] private int moveCommandPriority = 2; // 이동 목적지 지정 우선도
[SerializeField] private int attackingMeTargetPriority = 3; // 나를 공격 대상으로 삼은 적 우선도
[SerializeField] private int nearestEnemyPriority = 4; // 가장 가까운 적 우선도

[Header("현재 결투 상태")]
[SerializeField] private CharacterDuelAI currentTarget; // 현재 공격 대상으로 잡은 상대
[SerializeField] private CharacterDuelAI currentDuelTarget; // 현재 결투 대상으로 확정된 상대
[SerializeField] private bool isDashingToDuel; // 현재 결투 돌진 중 여부
[SerializeField] private Vector2 lastDashWorldDirection; // 마지막으로 저장된 월드 기준 돌진 방향
[SerializeField] private bool hasLastDashWorldDirection; // 유효한 돌진 방향 저장 여부

    [Header("결투 규칙 참조")]
    [SerializeField] private GlobalGameRuleManager globalGameRuleManager; // 인스펙터에서 직접 연결할 결투 규칙 매니저

    [Header("공격 대상 관리")]
    [SerializeField] private CharacterDuelAI priorityTarget; // 클릭으로 지정된 우선 공격 대상

    [SerializeField] private List<CharacterDuelAI> attackingMeList = new List<CharacterDuelAI>(); // 나를 공격 대상으로 삼은 AI 리스트

    [Header("행동 사운드 참조")]
    [SerializeField] private CharacterActionSound characterActionSound; // 캐릭터 사운드 출력기 참조

    [Header("캐릭터 애니메이션 재생 참조")]
    [SerializeField] private CharacterAnimationPlayer characterAnimationPlayer; // 캐릭터 애니메이션 재생기 참조

    [Header("결투 기술 목록")]
    [SerializeField] private List<DuelSkillDefinitionSO> duelSkillList = new List<DuelSkillDefinitionSO>(); // 이 캐릭터가 보유한 결투 기술 목록

    [Header("현재선택한 결투기술")]
    [SerializeField] private int currentSelectedDuelSkillIndex; // 현재 선택한 결투 기술 리스트 순서값

    [Header("결투 후 정지 설정")]
[SerializeField] private float postDuelStopDuration = 0f; // 결투 판정 직후부터 넉백 중에도 같이 흐를 결투 후 정지 시간

[Header("현재 결투 후 정지 상태")]
[SerializeField] private bool isCountingPostDuelStop; // 결투 후 정지 시간 카운트 진행 여부
[SerializeField] private float currentPostDuelStopTimer; // 현재 남아 있는 결투 후 정지 시간

[Header("즉시 결투 결과 애니 보호 상태")]
[SerializeField] private bool isImmediateResolveAnimationProtectionActive; // 후딜 초기화 직후 최신 결투 결과 애니를 즉시 허용하기 위한 임시 보호 상태

    [Header("결투 후 후딜 설정")]
[SerializeField] private float postDuelRecoveryDuration = 0.3f; // 넉백 종료 후 행동 불가 및 이미지 고정 시간

[Header("결투 후 방향 고정 상태")]
[SerializeField] private bool isFacingDirectionLockedAfterDuel; // 결투 판정 이후 방향 고정 중인지 여부
[SerializeField] private NavigationMovementSystem.FacingXDirectionType lockedFacingDirectionAfterDuel
    = NavigationMovementSystem.FacingXDirectionType.XPositive; // 결투 후 고정할 방향값

[Header("현재 결투 후 후딜 상태")]
[SerializeField] private bool isWaitingForPostDuelRecoveryStart; // 결투 넉백 종료를 기다리는 중인지 여부
[SerializeField] private bool isInPostDuelRecovery; // 현재 결투 후 후딜 진행 중인지 여부
[SerializeField] private float currentPostDuelRecoveryTimer; // 현재 결투 후 후딜 남은 시간

[Header("결투 판정 실행 잠금 상태")]
[SerializeField] private bool isDuelResolutionProcessing; // 현재 이 캐릭터가 결투 판정 실행/수신 처리 중인지 여부

[Header("아군 선택 순서 설정")]
[SerializeField] private int friendlySelectionPriority = 0; // 아군 선택 정렬 기준 우선순위값
[SerializeField] private int assignedFriendlySelectionOrder = 0; // 런타임에 부여된 실제 선택 순서값

[Header("결투 접근 제한 설정")]
[SerializeField] private float approachStopDistanceWhenTargetInExternalForce = 3f; // 상대가 넉백 상태일 때 이 거리까지만 접근

[Header("후딜 중 새 결투 인터럽트 허용 상태")]
[SerializeField] private bool isInterruptibleByNewDuelDuringRecovery = true; // 후딜 중 새 결투 인터럽트 허용 여부

public bool IsInterruptibleByNewDuelDuringRecovery => // 후딜 중 새 결투 인터럽트 가능 상태 반환
    isInterruptibleByNewDuelDuringRecovery && (isWaitingForPostDuelRecoveryStart || isInPostDuelRecovery);

public float ApproachStopDistanceWhenTargetInExternalForce => approachStopDistanceWhenTargetInExternalForce; // 넉백 대상 접근 제한 거리 반환

public int FriendlySelectionPriority => friendlySelectionPriority; // 아군 선택 정렬 기준 우선순위값 반환
public int AssignedFriendlySelectionOrder => assignedFriendlySelectionOrder; // 실제 선택 순서값 반환

    private bool isAutoChasingCurrentTarget; // 현재 공격 대상을 자동 추적 이동 중인지 여부

    public bool IsAutoChasingCurrentTarget => isAutoChasingCurrentTarget; // 현재 공격 대상 자동 추적 여부 반환

    public int TeamNumber => teamNumber; // 팀 번호 반환
    public ControlMode CurrentControlMode => controlMode; // 현재 조작 방식 반환
    public CharacterDuelAI CurrentTarget => currentTarget; // 현재 공격 대상 반환
    public CharacterDuelAI CurrentDuelTarget => currentDuelTarget; // 현재 결투 대상 반환
    public bool IsDashingToDuel => isDashingToDuel; // 현재 결투 돌진 여부 반환
    public float SearchRange => searchRange; // 탐색 거리 반환

    public int FirstRowID => firstRowID; // 1열 ID 반환
public int SecondRowID => secondRowID; // 2열 ID 반환
public int IndividualID => individualID; // 개체별 ID 반환

public bool IsFacingDirectionLockedAfterDuel => isFacingDirectionLockedAfterDuel; // 결투 후 방향 고정 여부 반환
public NavigationMovementSystem.FacingXDirectionType LockedFacingDirectionAfterDuel => lockedFacingDirectionAfterDuel; // 결투 후 고정 방향 반환

public int CurrentSelectedDuelSkillIndex => currentSelectedDuelSkillIndex; // 현재 선택된 결투 기술 인덱스 반환
public DuelSkillDefinitionSO CurrentSelectedDuelSkill => GetCurrentSelectedDuelSkill(); // 현재 선택된 결투 기술 반환

public bool IsSandbagTarget // 현재 캐릭터가 샌드백 대상인지 여부 반환
{
    get
    {
        return duelSandbagTarget != null && duelSandbagTarget.IsSandbagTarget; // 샌드백 스크립트가 있고 활성 상태면 true
    }
}

public bool IsSandbagTargetCharacter() // 외부에서 샌드백 여부 확인용 메서드
{
    return IsSandbagTarget; // 현재 샌드백 여부 반환
}

public bool IsDuelAnimationProtectedState // 결투 애니 우선 보호 상태 여부 반환
{
    get
    {
        if (isImmediateResolveAnimationProtectionActive)
        {
            return true; // 최신 결투 결과 애니 즉시 재생을 위한 임시 보호 상태
        }

        if (isDashingToDuel)
        {
            return true; // 결투 돌진 중이면 보호 상태
        }

        if (isWaitingForPostDuelRecoveryStart)
        {
            return true; // 넉백 종료 대기 중이면 보호 상태
        }

        if (isInPostDuelRecovery)
        {
            return true; // 후딜 진행 중이면 보호 상태
        }

        if (navigationMovementSystem != null && navigationMovementSystem.IsUnderExternalForce)
        {
            return true; // 넉백(외부 힘) 적용 중이면 보호 상태
        }

        return false; // 그 외에는 보호 상태 아님
    }
}

public bool IsMoveCommandBlockedByDuelState // 결투 상태로 인한 이동 명령 차단 여부 반환
{
    get
    {
        return IsDuelAnimationProtectedState; // 결투 애니 보호 상태와 동일 기준 사용
    }
}


public bool IsPostDuelRecoveryStateWithoutExternalForce // 넉백이 끝난 후 후딜 상태인지 반환
{
    get
    {
        if (navigationMovementSystem != null && navigationMovementSystem.IsUnderExternalForce)
        {
            return false; // 아직 넉백 중이면 후딜 상태로 취급하지 않음
        }

        return isInPostDuelRecovery; // 넉백이 끝난 뒤 후딜 상태 여부 반환
    }
}

public bool CanBeDuelTriggeredByOther // 다른 대상이 본인에게 결투를 걸 수 있는 상태인지 반환
{
    get
    {
        if (characterStatSystem == null)
        {
            return false; // 스탯 참조가 없으면 결투 불가
        }

        if (navigationMovementSystem != null && navigationMovementSystem.IsUnderExternalForce)
        {
            return false; // 넉백 중이면 결투 대상으로 받을 수 없음
        }

        return true; // 넉백만 아니면 결투 대상으로 받을 수 있음
    }
}



private void Awake() // 초기 참조 자동 연결
{
    if (characterStatSystem == null)
    {
        characterStatSystem = GetComponent<CharacterStatSystem>(); // 스탯 자동 참조
    }

    if (navigationMovementSystem == null)
    {
        navigationMovementSystem = GetComponent<NavigationMovementSystem>(); // 이동 시스템 자동 참조
    }

    if (moveCommandController == null)
    {
        moveCommandController = GetComponent<MoveCommandController>(); // 플레이어 이동 명령 컨트롤러 자동 참조
    }

    if (enemyAIController == null)
    {
        enemyAIController = GetComponent<EnemyAIController>(); // 적 AI 명령 컨트롤러 자동 참조
    }

    if (characterActionSound == null)
    {
        characterActionSound = GetComponent<CharacterActionSound>(); // 행동 사운드 자동 참조
    }

    if (characterAnimationPlayer == null)
    {
        characterAnimationPlayer = GetComponent<CharacterAnimationPlayer>(); // 캐릭터 애니메이션 재생기 자동 참조
    }

    if (duelSandbagTarget == null)
    {
        duelSandbagTarget = GetComponent<DuelSandbagTarget>(); // 샌드백 대상 스크립트 자동 참조
    }

    if (physicalContactCollider == null)
    {
        physicalContactCollider = GetComponent<Collider2D>(); // 물리접촉 전용 콜라이더 자동 참조
    }
}

private void Update() // 매 프레임 결투 상태 처리
{
    UpdateDuelTargetCollisionIgnoreTimer(); // 결투 상대와의 물리 접촉 무시 타이머 갱신
    UpdatePostDuelStopTimer(); // 결투 후 정지 시간 카운트 갱신
    UpdatePostDuelRecoveryState(); // 결투 후 후딜 상태 갱신

    if (characterStatSystem == null || navigationMovementSystem == null)
    {
        return; // 필수 참조가 없으면 종료
    }

    if (navigationMovementSystem.IsUnderExternalForce)
    {
        return; // 외부 힘 적용 중이면 종료
    }

    if (isDashingToDuel)
    {
        UpdateDashToDuel(); // 결투 돌진 처리
        return;
    }

    if (IsSandbagTarget)
    {
        return; // 샌드백 대상이면 능동 행동 로직을 수행하지 않음
    }

    // ✅ 후딜 중이라도 새 결투 인터럽트 허용 상태면 결투 진입 로직은 계속 태움
    if (characterStatSystem.IsActionLocked && !IsInterruptibleByNewDuelDuringRecovery)
    {
        return; // 일반 행동 잠금 상태면 종료
    }

    // ✅ 후딜 중 인터럽트 허용 상태에서는 일반 행동 대신 결투 진입만 처리
    if (IsInterruptibleByNewDuelDuringRecovery)
    {
        UpdateRecoveryInterruptTarget(); // 후딜 중 새 결투 진입용 타겟 갱신
        TryStartDuel(); // 새 결투 시작 시도
        return; // 일반 행동 로직은 실행하지 않음
    }

    if (controlMode == ControlMode.PlayerControlled)
    {
        UpdatePlayerControlledAction(); // 플레이어 조작형 행동 판단 및 추적 처리
    }

    TryStartDuel(); // 현재 타겟 기준 결투 시작 시도
}

public int GetFirstRowID() // 1열 ID 반환
{
    return firstRowID; // 1열 ID 반환
}

public int GetSecondRowID() // 2열 ID 반환
{
    return secondRowID; // 2열 ID 반환
}

public int GetIndividualID() // 개체별 ID 반환
{
    return individualID; // 개체별 ID 반환
}

public void SetIndividualID(int newIndividualID) // 개체별 ID 설정
{
    individualID = newIndividualID; // 새 개체별 ID 저장
}

public bool IsDirectionLinkedRotationLocked
{
    get
    {
        if (isDashingToDuel)
        {
            return true; // 결투 돌진 시작 후에는 회전 적용 잠금
        }

        if (navigationMovementSystem != null && navigationMovementSystem.IsUnderExternalForce)
        {
            return true; // 결투 판정 후 넉백(외부 힘) 진행 중에도 회전 적용 잠금
        }

        return false; // 그 외에는 회전 적용 가능
    }
}

private void TryStartDuel() // 상호 타겟이 아니어도 한쪽이 돌진 시작 시 상대를 강제 결투 상태로 동기화
{
    if (currentTarget == null) // 현재 타겟이 없으면 종료
    {
        return;
    }

    if (!CanDuelWith(currentTarget)) // 결투 불가능 대상이면 타겟 제거 후 종료
    {
        SetCurrentTarget(null);
        return;
    }

    NavigationMovementSystem targetMovementSystem = currentTarget.GetNavigationMovementSystem(); // 상대 이동 시스템 참조

    if (targetMovementSystem != null && targetMovementSystem.IsUnderExternalForce) // 상대가 넉백 중이면 시작 금지
    {
        return;
    }

    bool isTargetSandbag = currentTarget.IsSandbagTargetCharacter(); // 상대가 샌드백 대상인지 확인

    if (!isTargetSandbag && !currentTarget.CanBeDuelTriggeredByOther) // 일반 대상은 결투를 받을 수 있는 상태여야 함
    {
        return;
    }

    if (!isTargetSandbag
        && currentTarget.CurrentDuelTarget != null
        && currentTarget.CurrentDuelTarget != this) // 상대가 이미 다른 대상과 결투 중이면 가로채지 않음
    {
        return;
    }

    float distanceToTarget = Vector2.Distance(transform.position, currentTarget.transform.position); // 현재 거리 계산

    if (distanceToTarget > dashStartDistance) // 돌진 시작 거리 밖이면 종료
    {
        return;
    }

    if (IsInterruptibleByNewDuelDuringRecovery) // 후딜 중 새 결투 인터럽트 허용 상태면 이전 상태 초기화
    {
        PrepareForImmediateResolvedDuel(); // 기존 후딜/잠금/이미지 고정 상태 초기화
        FinishImmediateResolvedDuelPreparation(); // 준비용 임시 보호 상태 종료
    }

    if (!isTargetSandbag) // 샌드백이 아니면 상대도 같은 결투 상태로 강제 진입
    {
        currentTarget.ForceStartDuelByIncomingChallenger(this); // 상대를 나와의 결투 상태로 즉시 동기화
    }

    StartDuelDashState(currentTarget, false); // 자신도 기존 결투 돌진 상태로 진입
}

public void SetPriorityTarget(CharacterDuelAI target) // 직접 지정 공격 대상 설정
{
    if (!CanDuelWith(target))
    {
        return; // 결투 불가능한 대상이면 종료
    }

    priorityTarget = target; // 직접 지정 공격 대상 저장

    if (controlMode == ControlMode.PlayerControlled)
    {
        bool hasMoveCommand = moveCommandController != null && moveCommandController.HasMoveDestination; // 현재 이동 명령 존재 여부

        if (!hasMoveCommand)
        {
            SetCurrentTarget(priorityTarget); // 이동 명령이 없으면 즉시 현재 타겟 반영
        }
    }
}

public void RegisterAttacker(CharacterDuelAI attacker)
{
    if (!attackingMeList.Contains(attacker))
    {
        attackingMeList.Add(attacker);
    }
}

public void UnregisterAttacker(CharacterDuelAI attacker)
{
    attackingMeList.Remove(attacker);
}

private void UpdatePlayerControlledTarget()
{
    if (moveCommandController != null && moveCommandController.HasMoveDestination)
    {
        return;
    }

    if (priorityTarget != null && CanDuelWith(priorityTarget))
    {
        currentTarget = priorityTarget;
        return;
    }

    CharacterDuelAI nearestThreat = FindNearestAttacker();

    if (nearestThreat != null)
    {
        currentTarget = nearestThreat;
        return;
    }

    currentTarget = FindNearestEnemy();
}

private CharacterDuelAI FindNearestAttacker()
{
    float nearestDistance = float.MaxValue;
    CharacterDuelAI nearest = null;

    for (int i = 0; i < attackingMeList.Count; i++)
    {
        CharacterDuelAI attacker = attackingMeList[i];

        if (attacker == null)
        {
            continue;
        }

        float distance = Vector2.Distance(transform.position, attacker.transform.position);

        if (distance < nearestDistance)
        {
            nearestDistance = distance;
            nearest = attacker;
        }
    }

    return nearest;
}

    private CharacterDuelAI FindEnemyTargetingMe() // 자신을 타겟팅 중인 적 탐색
    {
        CharacterDuelAI[] duelAIArray = FindObjectsOfType<CharacterDuelAI>(); // 씬의 모든 결투 AI 탐색
        CharacterDuelAI nearestTarget = null; // 가장 가까운 우선 타겟 저장 변수
        float nearestDistance = float.MaxValue; // 최소 거리 비교용 변수

        for (int i = 0; i < duelAIArray.Length; i++)
        {
            CharacterDuelAI otherAI = duelAIArray[i]; // 현재 검사 중인 결투 AI

            if (!CanDuelWith(otherAI))
            {
                continue; // 결투 가능한 대상이 아니면 건너뜀
            }

            if (!otherAI.IsTargetingMe(this))
            {
                continue; // 자신을 타겟팅 중이지 않으면 건너뜀
            }

            float distance = Vector2.Distance(transform.position, otherAI.transform.position); // 거리 계산

            if (distance > searchRange)
            {
                continue; // 탐색 거리 밖이면 건너뜀
            }

            if (distance < nearestDistance)
            {
                nearestDistance = distance; // 최소 거리 갱신
                nearestTarget = otherAI; // 가장 가까운 우선 타겟 갱신
            }
        }

        return nearestTarget; // 최종 탐색 결과 반환
    }

    public CharacterDuelAI FindNearestEnemy() // 가장 가까운 적 탐색
    {
        CharacterDuelAI[] duelAIArray = FindObjectsOfType<CharacterDuelAI>(); // 씬의 모든 결투 AI 탐색
        CharacterDuelAI nearestTarget = null; // 가장 가까운 적 저장 변수
        float nearestDistance = float.MaxValue; // 최소 거리 비교용 변수

        for (int i = 0; i < duelAIArray.Length; i++)
        {
            CharacterDuelAI otherAI = duelAIArray[i]; // 현재 검사 중인 결투 AI

            if (!CanDuelWith(otherAI))
            {
                continue; // 결투 가능한 대상이 아니면 건너뜀
            }

            float distance = Vector2.Distance(transform.position, otherAI.transform.position); // 거리 계산

            if (distance > searchRange)
            {
                continue; // 탐색 거리 밖이면 건너뜀
            }

            if (distance < nearestDistance)
            {
                nearestDistance = distance; // 최소 거리 갱신
                nearestTarget = otherAI; // 가장 가까운 적 갱신
            }
        }

        return nearestTarget; // 최종 탐색 결과 반환
    }

    public bool CanDuelWith(CharacterDuelAI otherAI) // 특정 대상과 결투 가능한지 판정
    {
        if (otherAI == null)
        {
            return false; // 대상이 없으면 불가
        }

        if (otherAI == this)
        {
            return false; // 자기 자신과는 불가
        }

        if (otherAI.TeamNumber == teamNumber)
        {
            return false; // 같은 팀 번호면 결투 불가
        }

        return true; // 결투 가능
    }

    public bool IsTargetingMe(CharacterDuelAI otherAI) // 특정 대상이 자신을 타겟으로 잡았는지 확인
    {
        return currentTarget == otherAI; // 현재 타겟과 같은지 반환
    }

    public bool IsTargeting(CharacterDuelAI otherAI) // 특정 대상을 현재 타겟으로 잡고 있는지 확인
    {
        return currentTarget == otherAI; // 현재 타겟과 같은지 반환
    }

public void SetCurrentTarget(CharacterDuelAI newTarget) // 현재 공격 대상 설정
{
    if (newTarget != null && !CanDuelWith(newTarget))
    {
        return; // 결투 불가능한 대상이면 설정 차단
    }

    if (currentTarget == newTarget)
    {
        return; // 기존 타겟과 같으면 종료
    }

    UnregisterFromCurrentTarget(); // 이전 타겟의 attackingMeList에서 자신 제거
    currentTarget = newTarget; // 현재 타겟 저장
    RegisterToCurrentTarget(); // 새 타겟의 attackingMeList에 자신 등록
}

private void RegisterToCurrentTarget() // 새 현재 타겟에게 자신을 공격자라고 등록
{
    if (currentTarget == null)
    {
        return; // 현재 타겟이 없으면 종료
    }

    currentTarget.RegisterAttacker(this); // 상대의 attackingMeList에 자신 등록
}

private void UnregisterFromCurrentTarget() // 기존 현재 타겟에게서 자신 공격자 등록 해제
{
    if (currentTarget == null)
    {
        return; // 현재 타겟이 없으면 종료
    }

    currentTarget.UnregisterAttacker(this); // 상대의 attackingMeList에서 자신 제거
}

private void UpdatePlayerControlledAction() // 플레이어 조작형 행동 우선도 판단 및 실행
{
    isAutoChasingCurrentTarget = false; // 매 프레임 자동 추적 상태 초기화

    if (priorityTarget != null && !CanDuelWith(priorityTarget))
    {
        priorityTarget = null; // 직접 지정 공격 대상이 더 이상 유효하지 않으면 제거
    }

    int selectedPriority = int.MaxValue; // 현재 선택된 가장 높은 우선도 값
    PlayerControlledActionType selectedActionType = PlayerControlledActionType.None; // 현재 선택된 행동 종류
    CharacterDuelAI selectedTarget = null; // 현재 선택된 공격 대상

    bool hasMoveCommand = moveCommandController != null && moveCommandController.HasMoveDestination; // 이동 목적지 지정 여부

    if (hasMoveCommand && moveCommandPriority < selectedPriority)
    {
        selectedPriority = moveCommandPriority; // 이동 명령 우선도 선택
        selectedActionType = PlayerControlledActionType.MoveCommand; // 이동 행동 선택
        selectedTarget = null; // 이동 행동은 공격 대상 없음
    }

    if (priorityTarget != null && priorityTargetPriority < selectedPriority)
    {
        selectedPriority = priorityTargetPriority; // 직접 지정 공격 대상 우선도 선택
        selectedActionType = PlayerControlledActionType.AttackTarget; // 공격 행동 선택
        selectedTarget = priorityTarget; // 직접 지정 공격 대상 선택
    }

    CharacterDuelAI nearestAttacker = FindNearestAttacker(); // 자신을 공격 대상으로 삼은 가장 가까운 적 탐색

    if (nearestAttacker != null && attackingMeTargetPriority < selectedPriority)
    {
        selectedPriority = attackingMeTargetPriority; // attackingMeList 대상 우선도 선택
        selectedActionType = PlayerControlledActionType.AttackTarget; // 공격 행동 선택
        selectedTarget = nearestAttacker; // 가장 가까운 공격자 선택
    }

    CharacterDuelAI nearestEnemy = FindNearestEnemy(); // 가장 가까운 적 탐색

    if (nearestEnemy != null && nearestEnemyPriority < selectedPriority)
    {
        selectedPriority = nearestEnemyPriority; // 가장 가까운 적 우선도 선택
        selectedActionType = PlayerControlledActionType.AttackTarget; // 공격 행동 선택
        selectedTarget = nearestEnemy; // 가장 가까운 적 선택
    }

    if (selectedActionType == PlayerControlledActionType.MoveCommand)
    {
        SetCurrentTarget(null); // 이동 명령이 최우선이면 공격 대상 해제
        return; // 이동 명령 수행 상태 유지
    }

    if (selectedActionType == PlayerControlledActionType.AttackTarget)
    {
        SetCurrentTarget(selectedTarget); // 선택된 공격 대상 반영
        UpdateAutoChaseToCurrentTarget(); // 현재 공격 대상으로 자동 추적
        return;
    }

    SetCurrentTarget(null); // 수행할 행동이 없으면 공격 대상 해제

    if (navigationMovementSystem != null)
    {
        navigationMovementSystem.StopMove(); // 자동 추적 이동 정지
    }
}

private void UpdateAutoChaseToCurrentTarget() // 현재 공격 대상으로 자동 추적 이동
{
    if (currentTarget == null || navigationMovementSystem == null)
    {
        isAutoChasingCurrentTarget = false; // 자동 추적 불가 상태 저장
        return; // 타겟 또는 이동 시스템이 없으면 종료
    }

    float distanceToTarget = Vector2.Distance(transform.position, currentTarget.transform.position); // 현재 타겟까지 거리 계산
    NavigationMovementSystem targetMovementSystem = currentTarget.GetNavigationMovementSystem(); // 현재 타겟 이동 시스템 참조
    bool isTargetUnderExternalForce = targetMovementSystem != null && targetMovementSystem.IsUnderExternalForce; // 현재 타겟 넉백 상태 여부

    if (isTargetUnderExternalForce)
    {
        if (distanceToTarget <= approachStopDistanceWhenTargetInExternalForce)
        {
            isAutoChasingCurrentTarget = false; // 넉백 대상 접근 제한 거리 이내면 자동 추적 해제
            navigationMovementSystem.StopMove(); // 설정 거리까지만 접근하고 정지
            return;
        }

        isAutoChasingCurrentTarget = true; // 접근 중 상태 저장
        navigationMovementSystem.UpdateFacingDirectionByTargetPosition(currentTarget.transform.position); // 타겟 방향 갱신

        if (moveCommandController != null)
        {
            moveCommandController.ApplyDirectionLinkedRotationImmediatelyFromCurrentFacing(); // 방향 회전 즉시 적용
        }

        navigationMovementSystem.SetMoveDestination(currentTarget.transform.position); // 설정 거리까지 접근
        return;
    }

    if (distanceToTarget <= dashStartDistance)
    {
        isAutoChasingCurrentTarget = false; // 결투 돌진 거리 이내면 일반 추적 아님
        navigationMovementSystem.StopMove(); // 결투 돌진 시작 거리 안이면 일반 추적 정지
        return;
    }

    isAutoChasingCurrentTarget = true; // 공격 대상 자동 추적 상태 저장
    navigationMovementSystem.UpdateFacingDirectionByTargetPosition(currentTarget.transform.position); // 현재 공격 대상 위치 기준으로 방향 상태 먼저 갱신

    if (moveCommandController != null)
    {
        moveCommandController.ApplyDirectionLinkedRotationImmediatelyFromCurrentFacing(); // 갱신된 방향 상태 기준으로 회전 즉시 1회 적용
    }

    navigationMovementSystem.SetMoveDestination(currentTarget.transform.position); // 현재 타겟 위치로 추적 이동
}

public void ClearPriorityTarget() // 직접 지정 공격 대상 해제
{
    priorityTarget = null; // 직접 지정 공격 대상 제거
}

private void UpdateDashToDuel() // 결투 대상에게 돌진 처리
{
    if (currentDuelTarget == null)
    {
        EndDuel(); // 결투 대상이 없으면 종료
        return;
    }

    NavigationMovementSystem targetMovementSystem = currentDuelTarget.GetNavigationMovementSystem(); // 상대 이동 시스템 참조

    if (targetMovementSystem != null && targetMovementSystem.IsUnderExternalForce)
    {
        EndDuel(); // 돌진 중 상대가 넉백 상태가 되면 돌진 강제 해제
        return;
    }

    Vector2 selfPosition = transform.position; // 자신의 현재 위치
    Vector2 targetPosition = currentDuelTarget.transform.position; // 결투 대상 현재 위치
    Vector2 dashDirection = targetPosition - selfPosition; // 현재 프레임의 돌진 방향 계산
    float distanceToTarget = dashDirection.magnitude; // 현재 거리 계산

    if (distanceToTarget > 0.0001f)
    {
        lastDashWorldDirection = dashDirection.normalized; // 마지막 유효 월드 돌진 방향 저장
        hasLastDashWorldDirection = true; // 유효한 돌진 방향 저장 상태 설정
    }

    if (distanceToTarget <= clashDistance)
    {
        TryResolveDuel(); // 결투 판정 시도
        return;
    }

    navigationMovementSystem.SetMoveDestination(targetPosition); // 돌진 중에는 상대의 현재 위치로 계속 이동
}

private void TryResolveDuel() // 결투 판정 처리
{
    if (currentDuelTarget == null)
    {
        return; // 결투 대상이 없으면 종료
    }

    CharacterDuelAI resolvedTarget = currentDuelTarget; // 판정 시점의 상대 참조를 임시 저장

    if (resolvedTarget == null)
    {
        return; // 상대가 없으면 종료
    }

    if (isDuelResolutionProcessing || resolvedTarget.isDuelResolutionProcessing)
    {
        return; // 이미 한쪽에서 판정 실행 중이면 중복 실행 금지
    }

    BeginDuelResolutionProcessing(resolvedTarget); // 자신과 상대의 판정 실행 잠금 시작

    if (globalGameRuleManager == null)
    {
        Debug.LogWarning($"{name} : GlobalGameRuleManager 참조가 비어 있습니다."); // 인스펙터 참조 누락 경고
        FinishDuelResolutionProcessing(resolvedTarget); // 실행 잠금 해제
        EndDuel(); // 자신 결투 종료
        resolvedTarget.EndDuelFromOtherSide(); // 상대 결투 종료
        return;
    }

    CharacterStatSystem otherStat = resolvedTarget.GetCharacterStatSystem(); // 상대의 스탯 참조

    if (characterStatSystem == null || otherStat == null)
    {
        FinishDuelResolutionProcessing(resolvedTarget); // 실행 잠금 해제
        EndDuel(); // 자신 결투 종료
        resolvedTarget.EndDuelFromOtherSide(); // 상대 결투 종료
        return;
    }

    int selfRolledSpeedRatePercent = globalGameRuleManager.RollBattleSpeedRatePercent(
        GetCurrentMinimumSpeedRatePercent(), // 현재 선택 기술 기준 최소 속도율
        GetCurrentMaximumSpeedRatePercent()); // 현재 선택 기술 기준 최대 속도율

    int otherRolledSpeedRatePercent = globalGameRuleManager.RollBattleSpeedRatePercent(
        resolvedTarget.GetCurrentMinimumSpeedRatePercent(), // 상대 현재 선택 기술 기준 최소 속도율
        resolvedTarget.GetCurrentMaximumSpeedRatePercent()); // 상대 현재 선택 기술 기준 최대 속도율

    int selfBattleSpeed = globalGameRuleManager.CalculateBattleSpeed(characterStatSystem, selfRolledSpeedRatePercent); // 자신의 전투속도 계산
    int otherBattleSpeed = globalGameRuleManager.CalculateBattleSpeed(otherStat, otherRolledSpeedRatePercent); // 상대의 전투속도 계산

    characterStatSystem.SetBattleSpeed(selfBattleSpeed); // 자신의 전투속도 저장
    otherStat.SetBattleSpeed(otherBattleSpeed); // 상대의 전투속도 저장

    GlobalGameRuleManager.DuelCombatDebugData selfDebugData =
        globalGameRuleManager.CreateDuelCombatDebugData(characterStatSystem, selfRolledSpeedRatePercent, selfBattleSpeed, otherBattleSpeed); // 자신의 디버그 계산 데이터 생성

    GlobalGameRuleManager.DuelCombatDebugData otherDebugData =
        globalGameRuleManager.CreateDuelCombatDebugData(otherStat, otherRolledSpeedRatePercent, otherBattleSpeed, selfBattleSpeed); // 상대의 디버그 계산 데이터 생성

    if (resolvedTarget.IsSandbagTargetCharacter())
    {
        GlobalGameRuleManager.DuelResultType selfResult = GlobalGameRuleManager.DuelResultType.Hit; // 샌드백을 공격한 쪽은 무조건 적중
        GlobalGameRuleManager.DuelResultType otherResult = GlobalGameRuleManager.DuelResultType.Damaged; // 샌드백 쪽은 무조건 피격

        ApplyResolvedDuelOutcome(selfResult, resolvedTarget); // 자신 결과 즉시 적용
        resolvedTarget.ApplyResolvedDuelOutcome(otherResult, this); // 상대 결과 즉시 적용

        ApplyResolvedNumericalDamage(selfResult, resolvedTarget, selfDebugData.attackPowerValue); // 자신의 수치 피해 적용
        resolvedTarget.ApplyResolvedNumericalDamage(otherResult, this, otherDebugData.attackPowerValue); // 상대의 수치 피해 적용

        ApplyRepelForce(resolvedTarget, globalGameRuleManager, selfResult, otherResult); // 넉백 즉시 적용

        FinishDuelResolutionProcessing(resolvedTarget); // 실행 잠금 해제
        EndDuel(false); // 자신 결투 종료, 판정 애니메이션은 유지
        resolvedTarget.ReceiveResolvedDuel(otherResult); // 상대 결투 종료 처리
        return; // 샌드백 전용 판정 후 종료
    }

    float selfDifferenceRate = globalGameRuleManager.CalculateDifferenceRate(selfDebugData.attackPowerValue, otherDebugData.attackPowerValue); // 자신의 차이율 계산
    float otherDifferenceRate = globalGameRuleManager.CalculateDifferenceRate(otherDebugData.attackPowerValue, selfDebugData.attackPowerValue); // 상대의 차이율 계산

    GlobalGameRuleManager.DuelResultType selfResultNormal = globalGameRuleManager.EvaluateDuelResult(selfDifferenceRate); // 자신의 결투 결과 판정
    GlobalGameRuleManager.DuelResultType otherResultNormal = globalGameRuleManager.EvaluateDuelResult(otherDifferenceRate); // 상대의 결투 결과 판정

    Debug.Log(BuildDuelDebugLog(
        resolvedTarget, // 판정 상대
        selfDebugData, // 자신의 계산 데이터
        otherDebugData, // 상대의 계산 데이터
        selfDifferenceRate, // 자신의 차이율
        otherDifferenceRate, // 상대의 차이율
        selfResultNormal, // 자신의 결투 결과
        otherResultNormal)); // 상대의 결투 결과

    ApplyResolvedDuelOutcome(selfResultNormal, resolvedTarget); // 자신 결과 즉시 적용
    resolvedTarget.ApplyResolvedDuelOutcome(otherResultNormal, this); // 상대 결과 즉시 적용

    ApplyResolvedNumericalDamage(selfResultNormal, resolvedTarget, selfDebugData.attackPowerValue); // 자신의 수치 피해 적용
    resolvedTarget.ApplyResolvedNumericalDamage(otherResultNormal, this, otherDebugData.attackPowerValue); // 상대의 수치 피해 적용

    ApplyRepelForce(resolvedTarget, globalGameRuleManager, selfResultNormal, otherResultNormal); // 양쪽 넉백 즉시 적용

    FinishDuelResolutionProcessing(resolvedTarget); // 실행 잠금 해제
    EndDuel(false); // 자신 결투 종료, 판정 애니메이션은 유지
    resolvedTarget.ReceiveResolvedDuel(otherResultNormal); // 상대 결투 종료 처리
}
// GlobalGameRuleManager.DuelResultType selfResult = GlobalGameRuleManager.DuelResultType.Hit; // 샌드백을 공격한 쪽은 무조건 적중
private void ApplyRepelForce(
    CharacterDuelAI resolvedTarget, // 판정 시점에 확정된 상대 참조
    GlobalGameRuleManager ruleManager, // 결투 규칙 매니저 참조
    GlobalGameRuleManager.DuelResultType selfResult, // 자신의 결투 결과
    GlobalGameRuleManager.DuelResultType otherResult) // 상대의 결투 결과
{
    if (navigationMovementSystem == null || resolvedTarget == null || ruleManager == null)
    {
        return; // 필수 참조가 없으면 종료
    }

    NavigationMovementSystem otherMovementSystem = resolvedTarget.GetNavigationMovementSystem(); // 저장한 상대의 이동 시스템 참조

    if (otherMovementSystem == null)
    {
        return; // 상대 이동 시스템이 없으면 종료
    }

    Vector2 selfPushDirection = Vector2.zero; // 자신의 넉백 방향 저장
    Vector2 otherPushDirection = Vector2.zero; // 상대의 넉백 방향 저장

    if (hasLastDashWorldDirection)
    {
        selfPushDirection = -lastDashWorldDirection; // 자신의 돌진 방향 반대 방향을 넉백 방향으로 사용
    }
    else
    {
        Vector2 fallbackDirection = (Vector2)(transform.position - resolvedTarget.transform.position); // 예비용 위치 기준 방향 계산

        if (fallbackDirection.sqrMagnitude > 0.0001f)
        {
            selfPushDirection = fallbackDirection.normalized; // 돌진 방향이 없을 때 위치 기준 반대 방향 사용
        }
    }

    if (resolvedTarget.hasLastDashWorldDirection)
    {
        otherPushDirection = -resolvedTarget.lastDashWorldDirection; // 상대도 자신의 돌진 방향 반대 방향을 넉백 방향으로 사용
    }
    else
    {
        Vector2 fallbackDirection = (Vector2)(resolvedTarget.transform.position - transform.position); // 상대 예비용 위치 기준 방향 계산

        if (fallbackDirection.sqrMagnitude > 0.0001f)
        {
            otherPushDirection = fallbackDirection.normalized; // 상대 돌진 방향이 없을 때 위치 기준 반대 방향 사용
        }
    }

    float selfForceMagnitude = ResolveRepelForceForSelf(resolvedTarget, ruleManager, selfResult, otherResult); // 자신에게 적용할 최종 넉백값 계산
    float otherForceMagnitude = ResolveRepelForceForTarget(resolvedTarget, ruleManager, selfResult, otherResult); // 상대에게 적용할 최종 넉백값 계산

    ApplySignedRepelForce(navigationMovementSystem, selfPushDirection, selfForceMagnitude); // 자신의 최종 넉백 적용
    ApplySignedRepelForce(otherMovementSystem, otherPushDirection, otherForceMagnitude); // 상대의 최종 넉백 적용
}

public void EndDuelFromOtherSide() // 상대 쪽에서 결투 종료 요청
{
    EndDuel(); // 일반 종료 처리
}

public void ReceiveResolvedDuel(GlobalGameRuleManager.DuelResultType resultType) // 상대가 판정한 결투 결과 수신
{
    isDuelResolutionProcessing = false; // 상대가 판정을 끝냈으므로 수신 측 잠금 해제
    EndDuel(false); // 결투 종료, 판정 애니메이션은 유지
}

public void ClearTargetReference(CharacterDuelAI otherAI) // 특정 상대에 대한 참조 정리
{
    if (priorityTarget == otherAI)
    {
        priorityTarget = null; // 직접 지정 공격 대상 참조 제거
    }

    if (currentTarget == otherAI)
    {
        SetCurrentTarget(null); // 현재 타겟 참조 제거 + attackingMeList 등록 해제
    }

    if (currentDuelTarget == otherAI)
    {
        isDuelResolutionProcessing = false; // 상대 참조 정리 시 판정 실행 잠금도 해제
        RestoreIgnoredDuelTargetCollision(); // 현재 결투 상대와의 충돌 무시 상태 복구
        currentDuelTarget = null; // 현재 결투 대상 제거
        isDashingToDuel = false; // 결투 돌진 상태 해제
        lastDashWorldDirection = Vector2.zero; // 저장된 돌진 방향 초기화
        hasLastDashWorldDirection = false; // 저장된 돌진 방향 유효 여부 초기화

        if (navigationMovementSystem != null)
        {
            navigationMovementSystem.ClearTemporaryMoveSpeedMultiplier(); // 돌진 속도 배율 해제
        }
    }

    if (controlMode == ControlMode.PlayerControlled && moveCommandController != null)
    {
        moveCommandController.SetMoveCommandLocked(false); // 플레이어 조작형이면 이동 명령 잠금 해제
    }

    if (characterAnimationPlayer != null)
    {
        characterAnimationPlayer.RefreshAnimationByCurrentState(); // 참조 정리 후 현재 상태 기준 애니메이션 즉시 갱신
    }
}

public CharacterStatSystem GetCharacterStatSystem() // 스탯 반환
    {
        return characterStatSystem; // 스탯 참조 반환
    }

public NavigationMovementSystem GetNavigationMovementSystem() // 이동 시스템 반환
    {
        return navigationMovementSystem; // 이동 시스템 참조 반환
    }

private void OnDisable() // 비활성화 시 참조 정리
{
    CharacterDuelAI previousTarget = currentTarget; // 기존 현재 타겟 임시 저장
    CharacterDuelAI previousDuelTarget = currentDuelTarget; // 기존 결투 대상 임시 저장

    isDuelResolutionProcessing = false; // 비활성화 시 판정 실행 잠금 해제
    RestoreIgnoredDuelTargetCollision(); // 비활성화 시 충돌 무시 상태 원복

    isCountingPostDuelStop = false; // 결투 후 정지 카운트 해제
    currentPostDuelStopTimer = 0f; // 결투 후 정지 시간 초기화

    isWaitingForPostDuelRecoveryStart = false; // 후딜 시작 대기 해제
    isInPostDuelRecovery = false; // 후딜 상태 해제
    currentPostDuelRecoveryTimer = 0f; // 후딜 타이머 초기화
    isFacingDirectionLockedAfterDuel = false; // 방향 고정 해제

    if (characterStatSystem != null)
    {
        characterStatSystem.SetActionLocked(false); // 비활성화 시 행동 잠금 해제
    }

    if (moveCommandController != null)
    {
        moveCommandController.SetMoveCommandLocked(false); // 비활성화 시 이동 명령 잠금 해제
    }

    if (navigationMovementSystem != null)
    {
        navigationMovementSystem.ClearForcedFacingDirection(); // 비활성화 시 강제 방향 해제
        navigationMovementSystem.StopMove(); // 비활성화 시 현재 이동 정지
        navigationMovementSystem.ClearTemporaryMoveSpeedMultiplier(); // 비활성화 시 돌진 속도 배율 해제
    }

    SetCurrentTarget(null); // 현재 타겟의 attackingMeList에서 자신 제거
    priorityTarget = null; // 직접 지정 공격 대상 초기화

    if (previousTarget != null)
    {
        previousTarget.ClearTargetReference(this); // 상대가 가지고 있는 자신 참조 정리
    }

    if (previousDuelTarget != null && previousDuelTarget != previousTarget)
    {
        previousDuelTarget.ClearTargetReference(this); // 결투 대상이 따로 있으면 참조 정리
    }
}

private void OnDrawGizmosSelected() // 선택 시 결투 거리 범위를 씬에 표시
{
    Gizmos.color = new Color(0f, 1f, 0f, 0.35f); // 탐색 거리 색상
    Gizmos.DrawWireSphere(transform.position, searchRange); // 탐색 거리 표시

    Gizmos.color = new Color(1f, 0.8f, 0f, 0.45f); // 돌진 시작 거리 색상
    Gizmos.DrawWireSphere(transform.position, dashStartDistance); // 돌진 시작 거리 표시

    Gizmos.color = new Color(1f, 0f, 0f, 0.55f); // 결투 판정 거리 색상
    Gizmos.DrawWireSphere(transform.position, clashDistance); // 결투 판정 거리 표시
}

private void ApplyPreDuelFacingDirection() // 기존 호출 유지용 래퍼
{
    ApplyPreDuelFacingDirection(currentTarget); // 현재 타겟 기준으로 방향/회전 반영
}

private void ApplyPreDuelFacingDirection(CharacterDuelAI targetAI) // 지정한 대상 기준으로 방향과 회전을 먼저 맞춤
{
    if (targetAI == null) // 대상이 없으면 종료
    {
        return;
    }

    if (navigationMovementSystem != null) // 대상 위치 기준으로 방향 상태 갱신
    {
        navigationMovementSystem.UpdateFacingDirectionByTargetPosition(targetAI.transform.position);
    }

    if (moveCommandController != null) // 갱신된 방향 상태 기준으로 회전 즉시 적용
    {
        moveCommandController.ApplyDirectionLinkedRotationImmediatelyFromCurrentFacing();
    }
}

private string BuildDuelDebugLog(
    CharacterDuelAI resolvedTarget, // 판정 시점의 상대 참조
    GlobalGameRuleManager.DuelCombatDebugData selfDebugData, // 자신의 디버그 계산 데이터
    GlobalGameRuleManager.DuelCombatDebugData otherDebugData, // 상대의 디버그 계산 데이터
    float selfDifferenceRate, // 자신의 차이율
    float otherDifferenceRate, // 상대의 차이율
    GlobalGameRuleManager.DuelResultType selfResult, // 자신의 결투 결과
    GlobalGameRuleManager.DuelResultType otherResult) // 상대의 결투 결과
{
    StringBuilder logBuilder = new StringBuilder(); // 상세 로그 문자열 조립용 객체 생성

    logBuilder.AppendLine($"[결투 판정] {name} vs {resolvedTarget.name}"); // 결투 제목 출력

    logBuilder.AppendLine("- 전투속도 계산"); // 전투속도 계산 구간 제목
    logBuilder.AppendLine(
        $"  · {name} : (속도율 {selfDebugData.rolledSpeedRatePercent} × 기본속도 {characterStatSystem.SpeedStat}) / 100 = 전투속도 {selfDebugData.battleSpeed}"); // 자신의 전투속도 계산식 출력
    logBuilder.AppendLine(
        $"  · {resolvedTarget.name} : (속도율 {otherDebugData.rolledSpeedRatePercent} × 기본속도 {resolvedTarget.GetCharacterStatSystem().SpeedStat}) / 100 = 전투속도 {otherDebugData.battleSpeed}"); // 상대의 전투속도 계산식 출력

    logBuilder.AppendLine("- 전투속도 차이 기반 위력률 보정"); // 위력률 보정 구간 제목
    logBuilder.AppendLine(
        $"  · {name} : 전투속도 차이 {selfDebugData.battleSpeedDifference} → 추가 위력률 +{selfDebugData.bonusPowerRatePercent}"); // 자신의 보정 출력
    logBuilder.AppendLine(
        $"  · {resolvedTarget.name} : 전투속도 차이 {otherDebugData.battleSpeedDifference} → 추가 위력률 +{otherDebugData.bonusPowerRatePercent}"); // 상대의 보정 출력

    logBuilder.AppendLine("- 최종 위력률"); // 최종 위력률 구간 제목
    logBuilder.AppendLine(
        $"  · {name} : 기본 위력률 {selfDebugData.basePowerRatePercent} + 추가 위력률 {selfDebugData.bonusPowerRatePercent} = {selfDebugData.finalPowerRatePercent}"); // 자신의 최종 위력률 출력
    logBuilder.AppendLine(
        $"  · {resolvedTarget.name} : 기본 위력률 {otherDebugData.basePowerRatePercent} + 추가 위력률 {otherDebugData.bonusPowerRatePercent} = {otherDebugData.finalPowerRatePercent}"); // 상대의 최종 위력률 출력

    logBuilder.AppendLine("- 공격위력값 계산"); // 공격위력 계산 구간 제목
    logBuilder.AppendLine(
        $"  · {name} : (최종 위력률 {selfDebugData.finalPowerRatePercent} × 공격력 {selfDebugData.attackPower}) / 100 = {selfDebugData.attackPowerValue}"); // 자신의 공격위력 계산식 출력
    logBuilder.AppendLine(
        $"  · {resolvedTarget.name} : (최종 위력률 {otherDebugData.finalPowerRatePercent} × 공격력 {otherDebugData.attackPower}) / 100 = {otherDebugData.attackPowerValue}"); // 상대의 공격위력 계산식 출력

    logBuilder.AppendLine("- 차이율"); // 차이율 구간 제목
    logBuilder.AppendLine(
        $"  · {name} : ({selfDebugData.attackPowerValue} - {otherDebugData.attackPowerValue}) / (({selfDebugData.attackPowerValue} + {otherDebugData.attackPowerValue}) / 2) × 100 = {selfDifferenceRate:F2}%"); // 자신의 차이율 계산식 출력
    logBuilder.AppendLine(
        $"  · {resolvedTarget.name} : ({otherDebugData.attackPowerValue} - {selfDebugData.attackPowerValue}) / (({otherDebugData.attackPowerValue} + {selfDebugData.attackPowerValue}) / 2) × 100 = {otherDifferenceRate:F2}%"); // 상대의 차이율 계산식 출력

    logBuilder.AppendLine("- 결투결과"); // 결투 결과 구간 제목
    logBuilder.AppendLine(
        $"  · {name} : {globalGameRuleManager.GetDuelResultKoreanText(selfResult)}"); // 자신의 한국어 결과 출력
    logBuilder.AppendLine(
        $"  · {resolvedTarget.name} : {globalGameRuleManager.GetDuelResultKoreanText(otherResult)}"); // 상대의 한국어 결과 출력

    return logBuilder.ToString(); // 최종 로그 문자열 반환
}

public void SetCurrentSelectedDuelSkillIndex(int newIndex) // 현재 선택된 결투 기술 인덱스 설정
{
    currentSelectedDuelSkillIndex = newIndex; // 새 인덱스 저장
}

public DuelSkillDefinitionSO GetCurrentSelectedDuelSkill() // 현재 선택된 결투 기술 반환
{
    if (duelSkillList == null || duelSkillList.Count == 0)
    {
        return null; // 기술 목록이 없으면 null 반환
    }

    int clampedIndex = Mathf.Clamp(currentSelectedDuelSkillIndex, 0, duelSkillList.Count - 1); // 범위 보정
    return duelSkillList[clampedIndex]; // 현재 선택 기술 반환
}

public int GetCurrentMinimumSpeedRatePercent() // 현재 선택된 결투 기술 기준 최소 속도율 반환
{
    DuelSkillDefinitionSO currentSkill = GetCurrentSelectedDuelSkill(); // 현재 선택 기술 가져오기

    if (currentSkill != null)
    {
        return currentSkill.MinimumSpeedRatePercent; // 기술 최소 속도율 반환
    }

    if (characterStatSystem != null)
    {
        return characterStatSystem.MinimumSpeedRatePercent; // 기술이 없으면 기본 스탯값 반환
    }

    return 100; // 참조가 없으면 기본값 반환
}

public int GetCurrentMaximumSpeedRatePercent() // 현재 선택된 결투 기술 기준 최대 속도율 반환
{
    DuelSkillDefinitionSO currentSkill = GetCurrentSelectedDuelSkill(); // 현재 선택 기술 가져오기

    if (currentSkill != null)
    {
        return currentSkill.MaximumSpeedRatePercent; // 기술 최대 속도율 반환
    }

    if (characterStatSystem != null)
    {
        return characterStatSystem.MaximumSpeedRatePercent; // 기술이 없으면 기본 스탯값 반환
    }

    return 100; // 참조가 없으면 기본값 반환
}


public void EndDuel(bool stopManualAnimation = true) // 결투 종료 처리
{
    isDuelResolutionProcessing = false; // 결투 판정 실행 잠금 해제
    isDashingToDuel = false; // 결투 돌진 상태 해제
    isAutoChasingCurrentTarget = false; // 공격 대상 자동 추적 상태 해제
    currentTarget = null; // 현재 공격 대상 제거
    currentDuelTarget = null; // 현재 결투 대상 제거
    lastDashWorldDirection = Vector2.zero; // 저장된 돌진 방향 초기화
    hasLastDashWorldDirection = false; // 저장된 돌진 방향 유효 여부 초기화

    if (navigationMovementSystem != null)
    {
        navigationMovementSystem.ClearTemporaryMoveSpeedMultiplier(); // 돌진 속도 배율 해제
    }

    if (controlMode == ControlMode.PlayerControlled && moveCommandController != null)
    {
        moveCommandController.SetMoveCommandLocked(false); // 플레이어 조작형이면 이동 명령 잠금 해제
    }

    if (stopManualAnimation && characterAnimationPlayer != null)
    {
        characterAnimationPlayer.StopManualAnimation(); // 필요 시 수동 애니메이션 종료
    }
    else if (characterAnimationPlayer != null)
    {
        characterAnimationPlayer.RefreshAnimationByCurrentState(); // 상태 변화 기준 애니메이션 즉시 갱신
    }
}


private float ResolveRepelForceForSelf(
    CharacterDuelAI resolvedTarget, // 판정 시점의 상대 참조
    GlobalGameRuleManager ruleManager, // 전역 규칙 참조
    GlobalGameRuleManager.DuelResultType selfResult, // 자신의 결과
    GlobalGameRuleManager.DuelResultType otherResult) // 상대의 결과
{
    DuelSkillDefinitionSO selfSkill = GetCurrentSelectedDuelSkill(); // 자신의 현재 선택 기술 참조
    DuelSkillDefinitionSO targetSkill = resolvedTarget != null ? resolvedTarget.GetCurrentSelectedDuelSkill() : null; // 상대의 현재 선택 기술 참조

    if (targetSkill != null && targetSkill.TryGetTargetRepelForceOverride(otherResult, out float targetSkillForceToMe))
    {
        return targetSkillForceToMe; // 상대 기술의 상대용 override가 있으면 자신에게 우선 적용
    }

    if (selfSkill != null && selfSkill.TryGetSelfRepelForceOverride(selfResult, out float selfSkillForceToMe))
    {
        return selfSkillForceToMe; // 자신의 자신용 override가 있으면 자신에게 적용
    }

    return ruleManager.GetRepelForceByResult(selfResult); // 둘 다 없으면 전역 기본 규칙 사용
}

private float ResolveRepelForceForTarget(
    CharacterDuelAI resolvedTarget, // 판정 시점의 상대 참조
    GlobalGameRuleManager ruleManager, // 전역 규칙 참조
    GlobalGameRuleManager.DuelResultType selfResult, // 자신의 결과
    GlobalGameRuleManager.DuelResultType otherResult) // 상대의 결과
{
    DuelSkillDefinitionSO selfSkill = GetCurrentSelectedDuelSkill(); // 자신의 현재 선택 기술 참조
    DuelSkillDefinitionSO targetSkill = resolvedTarget != null ? resolvedTarget.GetCurrentSelectedDuelSkill() : null; // 상대의 현재 선택 기술 참조

    if (selfSkill != null && selfSkill.TryGetTargetRepelForceOverride(selfResult, out float selfSkillForceToTarget))
    {
        return selfSkillForceToTarget; // 자신의 기술의 상대용 override가 있으면 상대에게 우선 적용
    }

    if (targetSkill != null && targetSkill.TryGetSelfRepelForceOverride(otherResult, out float targetSelfForce))
    {
        return targetSelfForce; // 상대 기술의 자신용 override가 있으면 상대에게 적용
    }

    return ruleManager.GetRepelForceByResult(otherResult); // 둘 다 없으면 전역 기본 규칙 사용
}

private void ApplySignedRepelForce(
    NavigationMovementSystem targetMovementSystem, // 힘을 적용할 이동 시스템
    Vector2 baseDirection, // 기본 넉백 방향
    float signedForceMagnitude) // 부호 포함 넉백값
{
    if (targetMovementSystem == null)
    {
        return; // 이동 시스템이 없으면 종료
    }

    if (baseDirection.sqrMagnitude <= 0.0001f)
    {
        return; // 방향이 유효하지 않으면 종료
    }

    if (Mathf.Approximately(signedForceMagnitude, 0f))
    {
        return; // 힘이 0이면 적용하지 않음
    }

    Vector2 finalDirection = baseDirection.normalized; // 기본 방향 정규화
    float finalForceMagnitude = Mathf.Abs(signedForceMagnitude); // 실제 힘 크기는 절댓값 사용

    if (signedForceMagnitude < 0f)
    {
        finalDirection = -finalDirection; // 음수면 방향 반전
    }

    targetMovementSystem.ApplyExternalForce(finalDirection * finalForceMagnitude, false); // 최종 방향과 크기로 외부 힘 적용
}

private void BeginIgnoreCollisionWithCurrentDuelTarget() // 현재 결투 상대와 물리 접촉 무시 시작
{
    RestoreIgnoredDuelTargetCollision(); // 이전에 무시 중이던 충돌이 있으면 먼저 복구

    if (physicalContactCollider == null)
    {
        return; // 본인 물리접촉용 콜라이더가 없으면 종료
    }

    if (currentDuelTarget == null)
    {
        return; // 현재 결투 상대가 없으면 종료
    }

    Collider2D targetCollider = currentDuelTarget.PhysicalContactCollider; // 현재 결투 상대의 물리접촉용 콜라이더 참조

    if (targetCollider == null)
    {
        return; // 상대 물리접촉용 콜라이더가 없으면 종료
    }

    Physics2D.IgnoreCollision(physicalContactCollider, targetCollider, true); // 현재 결투 상대와의 충돌 무시 시작
    currentIgnoredTargetPhysicalContactCollider = targetCollider; // 현재 무시 중인 상대 콜라이더 저장
    currentDuelTargetCollisionIgnoreTimer = Mathf.Max(0f, duelTargetCollisionIgnoreDuration); // 충돌 무시 시간 초기화
    isIgnoringDuelTargetCollision = true; // 충돌 무시 상태 저장
}

private void UpdateDuelTargetCollisionIgnoreTimer() // 결투 상대와의 물리 접촉 무시 시간 갱신
{
    if (!isIgnoringDuelTargetCollision)
    {
        return; // 현재 충돌 무시 중이 아니면 종료
    }

    currentDuelTargetCollisionIgnoreTimer -= Time.unscaledDeltaTime; // 현실시간 기준으로 남은 충돌 무시 시간 감소

    if (currentDuelTargetCollisionIgnoreTimer > 0f)
    {
        return; // 아직 시간이 남아 있으면 종료
    }

    RestoreIgnoredDuelTargetCollision(); // 시간이 끝났으면 충돌 무시 해제
}

private void RestoreIgnoredDuelTargetCollision() // 현재 무시 중인 상대와의 물리 접촉 복구
{
    if (physicalContactCollider != null && currentIgnoredTargetPhysicalContactCollider != null)
    {
        Physics2D.IgnoreCollision(physicalContactCollider, currentIgnoredTargetPhysicalContactCollider, false); // 무시했던 충돌 복구
    }

    currentIgnoredTargetPhysicalContactCollider = null; // 현재 무시 중인 상대 콜라이더 초기화
    currentDuelTargetCollisionIgnoreTimer = 0f; // 충돌 무시 시간 초기화
    isIgnoringDuelTargetCollision = false; // 충돌 무시 상태 해제
}

private void PreparePostDuelRecovery() // 결투 넉백 종료 후 후딜 시작을 예약
{
    isWaitingForPostDuelRecoveryStart = true; // 넉백 종료 대기 상태 시작
    isInPostDuelRecovery = false; // 현재 후딜 진행 상태 초기화
    currentPostDuelRecoveryTimer = 0f; // 후딜 타이머 초기화
}

private void UpdatePostDuelRecoveryState() // 결투 후 후딜 시작/진행 처리
{
    if (characterStatSystem == null || navigationMovementSystem == null)
    {
        return; // 필수 참조가 없으면 종료
    }

    if (isWaitingForPostDuelRecoveryStart)
    {
        if (!navigationMovementSystem.ConsumeExternalForceEndedSignal())
        {
            return; // 아직 넉백 종료 신호가 없으면 대기
        }

        isWaitingForPostDuelRecoveryStart = false; // 넉백 종료 대기 상태 해제
        isInPostDuelRecovery = true; // 후딜 시작

        float remainingPostDuelStopTime = isCountingPostDuelStop ? currentPostDuelStopTimer : 0f; // 넉백 종료 시점의 남은 결투 후 정지 시간 계산
        currentPostDuelRecoveryTimer = Mathf.Max(0f, postDuelRecoveryDuration + remainingPostDuelStopTime); // 기존 후딜에 남은 결투 후 정지 시간을 더해 최종 후딜 시간 설정

        isCountingPostDuelStop = false; // 후딜 시간에 합산했으므로 별도 카운트 종료
        currentPostDuelStopTimer = 0f; // 결투 후 정지 시간 초기화

        navigationMovementSystem.SetForcedFacingDirection(lockedFacingDirectionAfterDuel); // 후딜 시작 시 내부 방향값 다시 고정
        navigationMovementSystem.StopMove(); // 후딜 시작 시 현재 이동 즉시 정지

        if (moveCommandController != null)
        {
            moveCommandController.SetMoveCommandLocked(true); // 후딜 시작 시 플레이어 이동 명령 잠금
            moveCommandController.ApplyDirectionLinkedRotationImmediatelyFromCurrentFacing(); // 고정 방향 기준 회전 즉시 적용
        }

        characterStatSystem.SetActionLocked(true); // 행동 잠금
    }

    if (!isInPostDuelRecovery)
    {
        return; // 후딜 중이 아니면 종료
    }

    currentPostDuelRecoveryTimer -= Time.unscaledDeltaTime; // 현실시간 기준으로 후딜 시간 감소

    if (currentPostDuelRecoveryTimer > 0f)
    {
        navigationMovementSystem.SetForcedFacingDirection(lockedFacingDirectionAfterDuel); // 후딜 동안 내부 방향 유지

        if (moveCommandController != null)
        {
            moveCommandController.ApplyDirectionLinkedRotationImmediatelyFromCurrentFacing(); // 후딜 동안 외부 회전 유지
        }

        return; // 아직 시간이 남아 있으면 유지
    }

    isInPostDuelRecovery = false; // 후딜 종료
    currentPostDuelRecoveryTimer = 0f; // 타이머 초기화
    isFacingDirectionLockedAfterDuel = false; // 방향 고정 해제

    characterStatSystem.SetActionLocked(false); // 행동 잠금 해제

    if (moveCommandController != null)
    {
        moveCommandController.SetMoveCommandLocked(false); // 후딜 종료 시 플레이어 이동 명령 잠금 해제
    }

    characterAnimationPlayer?.RefreshAnimationByCurrentState(); // 현재 상태 기준 애니메이션 즉시 갱신
}

private void LockCurrentFacingDirectionAfterDuel() // 현재 방향을 결투 후 고정 방향으로 저장
{
    if (navigationMovementSystem == null)
    {
        return; // 이동 시스템이 없으면 종료
    }

    lockedFacingDirectionAfterDuel = navigationMovementSystem.CurrentFacingXDirection; // 현재 방향 저장
    isFacingDirectionLockedAfterDuel = true; // 방향 고정 상태 시작

    navigationMovementSystem.SetForcedFacingDirection(lockedFacingDirectionAfterDuel); // 내부 방향값 즉시 고정
}

private void StartPostDuelStopTimer() // 결투 판정 직후부터 흐를 결투 후 정지 시간 시작
{
    currentPostDuelStopTimer = Mathf.Max(0f, postDuelStopDuration); // 설정된 결투 후 정지 시간으로 초기화
    isCountingPostDuelStop = currentPostDuelStopTimer > 0f; // 0보다 클 때만 카운트 시작
}

private void UpdatePostDuelStopTimer() // 결투 후 정지 시간 감소 처리
{
    if (!isCountingPostDuelStop)
    {
        return; // 카운트 중이 아니면 종료
    }

    currentPostDuelStopTimer -= Time.unscaledDeltaTime; // 현실시간 기준으로 남은 시간 감소

    if (currentPostDuelStopTimer > 0f)
    {
        return; // 아직 남아 있으면 유지
    }

    currentPostDuelStopTimer = 0f; // 음수 보정
    isCountingPostDuelStop = false; // 카운트 종료
}

private void PlayCurrentDuelDashAnimation() // 현재 선택된 결투 기술의 돌진 애니메이션 재생
{
    if (characterAnimationPlayer == null)
    {
        return; // 재생기가 없으면 종료
    }

    DuelSkillDefinitionSO currentSkill = GetCurrentSelectedDuelSkill(); // 현재 선택 기술 가져오기

    if (currentSkill == null || currentSkill.DashAnimationClip == null)
    {
        return; // 기술 또는 애니메이션이 없으면 종료
    }

    if (currentSkill.UseDashAnimationLoop)
    {
        characterAnimationPlayer.PlayLoopAnimation(currentSkill.DashAnimationClip); // 돌진 애니메이션 루프 재생
        return;
    }

    characterAnimationPlayer.PlayOneShotAnimation(currentSkill.DashAnimationClip); // 돌진 애니메이션 1회 재생
}

private void PlayCurrentDuelDashSound() // 현재 선택된 결투 기술의 돌진 시작 사운드 재생
{
    if (characterActionSound == null)
    {
        return; // 사운드 출력기가 없으면 종료
    }

    DuelSkillDefinitionSO currentSkill = GetCurrentSelectedDuelSkill(); // 현재 선택 기술 가져오기

    if (currentSkill == null)
    {
        return; // 기술이 없으면 종료
    }

    if (!currentSkill.TryGetDashStartSoundData(
        out AudioClip audioClip,
        out float volume,
        out BattleAudioSettings.AudioGroupType audioGroupType))
    {
        return; // 설정된 사운드가 없으면 종료
    }

    characterActionSound.PlaySound(audioClip, volume, audioGroupType); // 돌진 시작 사운드 재생
}

private void PlayCurrentDuelResolveAnimation(GlobalGameRuleManager.DuelResultType duelResultType) // 현재 선택된 결투 기술의 결과별 판정 애니메이션 재생
{
    if (characterAnimationPlayer == null)
    {
        return; // 재생기가 없으면 종료
    }

    DuelSkillDefinitionSO currentSkill = GetCurrentSelectedDuelSkill(); // 현재 선택 기술 가져오기

    if (currentSkill == null)
    {
        return; // 기술이 없으면 종료
    }

    if (!currentSkill.TryGetResolveAnimationData(
        duelResultType,
        out CharacterAnimationClipSO animationClip,
        out bool useLoop))
    {
        return; // 설정된 애니메이션이 없으면 종료
    }

    if (useLoop)
    {
        characterAnimationPlayer.PlayLoopAnimation(animationClip); // 결과별 판정 애니메이션 루프 재생
        return;
    }

    characterAnimationPlayer.PlayOneShotAnimation(animationClip); // 결과별 판정 애니메이션 1회 재생
}

private void PlayCurrentDuelResolveSound(GlobalGameRuleManager.DuelResultType duelResultType) // 현재 선택된 결투 기술의 결과별 판정 사운드 재생
{
    if (characterActionSound == null)
    {
        return; // 사운드 출력기가 없으면 종료
    }

    DuelSkillDefinitionSO currentSkill = GetCurrentSelectedDuelSkill(); // 현재 선택 기술 가져오기

    if (currentSkill == null)
    {
        return; // 기술이 없으면 종료
    }

    if (!currentSkill.TryGetResolveSoundData(
        duelResultType,
        out AudioClip audioClip,
        out float volume,
        out BattleAudioSettings.AudioGroupType audioGroupType))
    {
        return; // 설정된 사운드가 없으면 종료
    }

    characterActionSound.PlaySound(audioClip, volume, audioGroupType); // 결과별 판정 사운드 재생
}

public void SetAssignedFriendlySelectionOrder(int newOrder) // 런타임 실제 선택 순서값 설정
{
    assignedFriendlySelectionOrder = Mathf.Max(0, newOrder); // 0 이상으로 보정 후 저장
}

private void PrepareForImmediateResolvedDuel(
    bool keepCurrentDuelTargetCollisionIgnore = false, // 현재 충돌 무시 유지 여부
    bool keepCurrentPostDuelStopTimer = false) // 현재 결투 후 정지 타이머 유지 여부
{
    isImmediateResolveAnimationProtectionActive = true; // 최신 결투 결과 애니를 즉시 허용하기 위한 임시 보호 상태 시작

    if (!keepCurrentPostDuelStopTimer)
    {
        isCountingPostDuelStop = false; // 이전 결투 후 정지 카운트 종료
        currentPostDuelStopTimer = 0f; // 이전 결투 후 정지 시간 초기화
    }

    isWaitingForPostDuelRecoveryStart = false; // 이전 결투 후딜 시작 대기 상태 해제
    isInPostDuelRecovery = false; // 이전 결투 후딜 상태 해제
    currentPostDuelRecoveryTimer = 0f; // 이전 결투 후딜 시간 초기화

    isFacingDirectionLockedAfterDuel = false; // 이전 결투 결과로 인한 방향 고정 해제

    if (characterStatSystem != null)
    {
        characterStatSystem.SetActionLocked(false); // 이전 후딜로 걸린 행동 잠금 해제
    }

    if (moveCommandController != null)
    {
        moveCommandController.SetMoveCommandLocked(false); // 이전 후딜로 걸린 이동 명령 잠금 해제
    }

    if (characterAnimationPlayer != null)
    {
        characterAnimationPlayer.RefreshAnimationByCurrentState(); // 현재 상태 기준 애니메이션 즉시 재동기화
    }

    if (navigationMovementSystem != null)
    {
        navigationMovementSystem.ClearForcedFacingDirection(); // 이전 결투 결과로 걸린 강제 방향 해제
        navigationMovementSystem.ClearTemporaryMoveSpeedMultiplier(); // 이전 돌진/후처리 속도 배율 해제
        navigationMovementSystem.StopMove(); // 기존 이동 상태 정리
    }

    if (!keepCurrentDuelTargetCollisionIgnore)
    {
        RestoreIgnoredDuelTargetCollision(); // 유지가 필요 없는 경우에만 이전 결투 상대와의 충돌 무시 상태 복구
    }
}

private void FinishImmediateResolvedDuelPreparation() // 최신 결투 결과 적용 후 임시 보호 상태 종료
{
    isImmediateResolveAnimationProtectionActive = false; // 임시 결투 결과 애니 보호 상태 해제
}


private void BeginDuelResolutionProcessing(CharacterDuelAI otherAI) // 자신과 상대의 결투 판정 실행 잠금 시작
{
    isDuelResolutionProcessing = true; // 자신 잠금 시작

    if (otherAI != null)
    {
        otherAI.isDuelResolutionProcessing = true; // 상대도 잠금 시작
    }
}

private void FinishDuelResolutionProcessing(CharacterDuelAI otherAI) // 자신과 상대의 결투 판정 실행 잠금 해제
{
    isDuelResolutionProcessing = false; // 자신 잠금 해제

    if (otherAI != null)
    {
        otherAI.isDuelResolutionProcessing = false; // 상대도 잠금 해제
    }
}

private void ApplyResolvedDuelOutcome(
    GlobalGameRuleManager.DuelResultType resultType, // 이번 결투 결과
    CharacterDuelAI resolvedTarget) // 이번 결투 상대
{
    PrepareForImmediateResolvedDuel(true, true); // 결과 적용 중에는 충돌 무시와 결투 후 정지 타이머를 유지
    PlayCurrentDuelResolveAnimation(resultType); // 이번 결투 결과 애니메이션 재생
    PlayCurrentDuelResolveSound(resultType); // 이번 결투 결과 사운드 재생
    PlayCurrentDuelResolveEffect(resultType, resolvedTarget); // 이번 결투 결과 이펙트 재생
    LockCurrentFacingDirectionAfterDuel(); // 이번 결투 결과 기준 방향 고정
    StartPostDuelStopTimer(); // 이번 결투 기준 정지 시간 시작
    PreparePostDuelRecovery(); // 이번 결투 기준 후딜 예약
    FinishImmediateResolvedDuelPreparation(); // 즉시 결과 애니 보호 상태 종료
}
private void UpdateRecoveryInterruptTarget() // 후딜 중 새 결투 인터럽트용 타겟 갱신
{
    CharacterDuelAI interruptTarget = null; // 이번 프레임 인터럽트 대상으로 사용할 캐릭터

    if (priorityTarget != null && CanDuelWith(priorityTarget))
    {
        interruptTarget = priorityTarget; // 직접 지정 공격 대상이 유효하면 우선 사용
    }

    if (interruptTarget == null)
    {
        interruptTarget = FindNearestAttacker(); // 나를 노리는 가장 가까운 적 우선 탐색
    }

    if (interruptTarget == null)
    {
        interruptTarget = FindNearestEnemy(); // 없으면 가장 가까운 적 탐색
    }

    SetCurrentTarget(interruptTarget); // 현재 타겟 갱신

    if (navigationMovementSystem != null)
    {
        navigationMovementSystem.StopMove(); // 후딜 인터럽트 대기 중 일반 이동은 정지 유지
    }
}

public void ForceStartDuelByIncomingChallenger(CharacterDuelAI challenger) // 상대의 선공 돌진에 의해 본인을 강제 결투 상태로 진입
{
    if (challenger == null) // 도전자 참조가 없으면 종료
    {
        return;
    }

    if (!CanDuelWith(challenger)) // 결투 불가능 대상이면 종료
    {
        return;
    }

    if (IsSandbagTargetCharacter()) // 샌드백은 능동 돌진 상태에 들어가지 않음
    {
        return;
    }

    if (!CanBeDuelTriggeredByOther) // 현재 결투를 받을 수 없는 상태면 종료
    {
        return;
    }

    if (currentDuelTarget != null && currentDuelTarget != challenger) // 이미 다른 상대와 결투 중이면 종료
    {
        return;
    }

    if (isDashingToDuel && currentDuelTarget == challenger) // 이미 같은 상대와 돌진 중이면 중복 실행 방지
    {
        return;
    }

    if (IsInterruptibleByNewDuelDuringRecovery) // 후딜 중이면 새 결투 진입 가능 상태로 초기화
    {
        PrepareForImmediateResolvedDuel(); // 기존 후딜/잠금/이미지 고정 상태 초기화
        FinishImmediateResolvedDuelPreparation(); // 준비용 임시 보호 상태 종료
    }

    StartDuelDashState(challenger, true); // 상대를 현재 결투 대상으로 동기화하고 돌진 상태 진입
}

private void StartDuelDashState(CharacterDuelAI duelTarget, bool syncCurrentTarget) // 결투 돌진 진입 공통 처리
{
    if (duelTarget == null) // 대상이 없으면 종료
    {
        return;
    }

    if (syncCurrentTarget) // 강제 결투 진입 시 현재 공격 대상도 함께 동기화
    {
        SetCurrentTarget(duelTarget); // 현재 타겟과 attackingMeList 관계까지 같이 정리
    }

    currentDuelTarget = duelTarget; // 현재 결투 대상 확정
    isDashingToDuel = true; // 결투 돌진 상태 설정
    lastDashWorldDirection = Vector2.zero; // 마지막 돌진 방향 초기화
    hasLastDashWorldDirection = false; // 마지막 돌진 방향 유효 여부 초기화

    ApplyPreDuelFacingDirection(duelTarget); // 돌진 시작 전에 대상 방향으로 방향/회전 즉시 반영
    BeginIgnoreCollisionWithCurrentDuelTarget(); // 현재 결투 상대와 물리 접촉 무시 시작

    if (controlMode == ControlMode.PlayerControlled && moveCommandController != null) // 플레이어 조작형이면 이동 명령 잠금
    {
        moveCommandController.SetMoveCommandLocked(true);
    }

    if (navigationMovementSystem != null) // 일반 이동 정지 후 돌진 속도 배율 적용
    {
        navigationMovementSystem.StopMove();
        navigationMovementSystem.SetTemporaryMoveSpeedMultiplier(dashMoveSpeed);
    }

    PlayCurrentDuelDashAnimation(); // 현재 선택 기술의 돌진 애니메이션 재생
    PlayCurrentDuelDashSound(); // 현재 선택 기술의 돌진 시작 사운드 재생
}

public CharacterAnimationPlayer GetCharacterAnimationPlayer() // 자신의 애니메이션 플레이어 반환
{
    return characterAnimationPlayer; // 캐릭터 애니메이션 플레이어 참조 반환
}

private void PlayCurrentDuelResolveEffect(
    GlobalGameRuleManager.DuelResultType duelResultType, // 현재 결투 결과
    CharacterDuelAI resolvedTarget) // 결투 상대
{
    if (characterAnimationPlayer == null) // 애니메이션 플레이어가 없으면 종료
    {
        return;
    }

    DuelSkillDefinitionSO currentSkill = GetCurrentSelectedDuelSkill(); // 현재 선택 기술 가져오기

    if (currentSkill == null) // 기술이 없으면 종료
    {
        return;
    }

    characterAnimationPlayer.PlayDuelResolveEffect(
        currentSkill, // 현재 결투 기술 정의 전달
        duelResultType, // 이번 결투 결과 전달
        resolvedTarget); // 결투 상대 전달
}

private void ApplyResolvedNumericalDamage(
    GlobalGameRuleManager.DuelResultType resultType, // 이번 결투 결과
    CharacterDuelAI resolvedTarget, // 실제 피해를 받을 상대
    int attackPowerValue) // 이번 판정에서 계산된 공격위력값
{
    if (resolvedTarget == null)
    {
        return; // 상대가 없으면 종료
    }

    CharacterStatSystem targetStatSystem = resolvedTarget.GetCharacterStatSystem(); // 상대 스탯 참조 가져오기

    if (targetStatSystem == null)
    {
        return; // 스탯 참조가 없으면 종료
    }

    CharacterAnimationPlayer targetAnimationPlayer = resolvedTarget.GetCharacterAnimationPlayer(); // 상대 애니메이션 플레이어 참조 가져오기

    if (resultType == GlobalGameRuleManager.DuelResultType.Hit)
    {
        int finalHealthDamage = targetStatSystem.ApplyHealthDamage(attackPowerValue); // 방어력 반영 체력피해 적용

        if (finalHealthDamage > 0 && targetAnimationPlayer != null)
        {
            targetAnimationPlayer.ShowHealthDamageFloater(finalHealthDamage); // 최종 체력피해 숫자 표시
        }
    }

    DuelSkillDefinitionSO currentSkill = GetCurrentSelectedDuelSkill(); // 현재 선택 기술 가져오기

    if (currentSkill == null)
    {
        return; // 기술이 없으면 종료
    }

    if (!currentSkill.TryGetStaggerDamage(resultType, out int rawStaggerDamage))
    {
        return; // 해당 결과의 와해피해 설정이 없으면 종료
    }

    int finalStaggerDamage = targetStatSystem.ApplyStaggerDamage(rawStaggerDamage); // 저지율 반영 와해피해 적용

    if (finalStaggerDamage > 0 && targetAnimationPlayer != null)
    {
        targetAnimationPlayer.ShowStaggerDamageFloater(finalStaggerDamage); // 최종 와해피해 숫자 표시
    }
}

}