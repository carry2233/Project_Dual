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
    [SerializeField] private CharacterActionSound characterActionSound; // 캐릭터 행동 사운드 설정기 참조

    [Header("캐릭터 애니메이션 재생 참조")]
    [SerializeField] private CharacterAnimationPlayer characterAnimationPlayer; // 캐릭터 애니메이션 재생기 참조

    [Header("결투 기술 목록")]
    [SerializeField] private List<DuelSkillDefinitionSO> duelSkillList = new List<DuelSkillDefinitionSO>(); // 이 캐릭터가 보유한 결투 기술 목록
    [SerializeField] private int currentSelectedDuelSkillIndex; // 현재 선택한 결투 기술 리스트 순서값

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

public int CurrentSelectedDuelSkillIndex => currentSelectedDuelSkillIndex; // 현재 선택된 결투 기술 인덱스 반환
public DuelSkillDefinitionSO CurrentSelectedDuelSkill => GetCurrentSelectedDuelSkill(); // 현재 선택된 결투 기술 반환

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
}

private void Update() // 매 프레임 결투 상태 처리
{
    if (characterStatSystem == null || navigationMovementSystem == null)
    {
        return; // 필수 참조가 없으면 종료
    }

    if (characterStatSystem.IsActionLocked)
    {
        return; // 행동 불가 상태면 종료
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

private void TryStartDuel() // 상호 타겟 상태면 결투 시작
{
    if (currentTarget == null)
    {
        return; // 현재 타겟이 없으면 종료
    }

    if (!CanDuelWith(currentTarget))
    {
        currentTarget = null; // 결투 불가 대상이면 타겟 제거
        return;
    }

    if (!currentTarget.IsTargeting(this))
    {
        return; // 상대가 자신을 타겟팅하지 않으면 종료
    }

    float distanceToTarget = Vector2.Distance(transform.position, currentTarget.transform.position); // 현재 거리 계산

    if (distanceToTarget > dashStartDistance)
    {
        return; // 돌진 시작 거리 밖이면 종료
    }

    ApplyPreDuelFacingDirection(); // 돌진 시작 전에 결투 대상이 있는 X축 방향으로 방향값과 회전을 먼저 적용

    currentDuelTarget = currentTarget; // 현재 결투 대상 확정
    isDashingToDuel = true; // 결투 돌진 상태 설정
    lastDashWorldDirection = Vector2.zero; // 결투 시작 시 저장 방향 초기화
    hasLastDashWorldDirection = false; // 결투 시작 시 저장 방향 유효 여부 초기화

    if (controlMode == ControlMode.PlayerControlled && moveCommandController != null)
    {
        moveCommandController.SetMoveCommandLocked(true); // 플레이어 조작형이면 이동 명령 잠금
    }

    if (navigationMovementSystem != null)
    {
        navigationMovementSystem.StopMove(); // 일반 이동 정지
        navigationMovementSystem.SetTemporaryMoveSpeedMultiplier(dashMoveSpeed); // 돌진 중 이동속도 배율 적용
    }

    PlayCurrentDuelDashAnimation(); // 현재 결투 기술의 돌진 애니메이션 재생
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

    if (gameObject.GetInstanceID() > currentDuelTarget.gameObject.GetInstanceID())
    {
        return; // 한 쌍당 한 번만 판정되도록 인스턴스 아이디가 작은 쪽만 처리
    }

    if (globalGameRuleManager == null)
    {
        Debug.LogWarning($"{name} : GlobalGameRuleManager 참조가 비어 있습니다."); // 인스펙터 참조 누락 경고
        EndDuel(); // 자신 결투 종료
        return;
    }

    CharacterDuelAI resolvedTarget = currentDuelTarget; // 판정 시점의 상대 참조를 임시 저장
    CharacterStatSystem otherStat = resolvedTarget.GetCharacterStatSystem(); // 상대의 스탯 참조

    if (characterStatSystem == null || otherStat == null)
    {
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

    float selfDifferenceRate = globalGameRuleManager.CalculateDifferenceRate(selfDebugData.attackPowerValue, otherDebugData.attackPowerValue); // 자신의 차이율 계산
    float otherDifferenceRate = globalGameRuleManager.CalculateDifferenceRate(otherDebugData.attackPowerValue, selfDebugData.attackPowerValue); // 상대의 차이율 계산

    GlobalGameRuleManager.DuelResultType selfResult = globalGameRuleManager.EvaluateDuelResult(selfDifferenceRate); // 자신의 결투 결과 판정
    GlobalGameRuleManager.DuelResultType otherResult = globalGameRuleManager.EvaluateDuelResult(otherDifferenceRate); // 상대의 결투 결과 판정

    PlayCurrentDuelResolveAnimation(); // 자신의 결투 판정 애니메이션 재생
    resolvedTarget.PlayCurrentDuelResolveAnimation(); // 상대의 결투 판정 애니메이션 재생

    if (characterActionSound != null)
    {
        characterActionSound.PlayDuelResultSound(selfResult); // 자신의 결투 결과 사운드 재생
    }

    CharacterActionSound targetActionSound = resolvedTarget.GetCharacterActionSound(); // 상대 행동 사운드 참조 가져오기

    if (targetActionSound != null)
    {
        targetActionSound.PlayDuelResultSound(otherResult); // 상대의 결투 결과 사운드 재생
    }

    Debug.Log(BuildDuelDebugLog(
        resolvedTarget, // 판정 상대
        selfDebugData, // 자신의 계산 데이터
        otherDebugData, // 상대의 계산 데이터
        selfDifferenceRate, // 자신의 차이율
        otherDifferenceRate, // 상대의 차이율
        selfResult, // 자신의 결투 결과
        otherResult)); // 상대의 결투 결과

    ApplyRepelForce(resolvedTarget, globalGameRuleManager, selfResult, otherResult); // 밀려나는 힘 적용

    EndDuel(false); // 자신의 결투 종료, 판정 애니메이션은 유지
    resolvedTarget.ReceiveResolvedDuel(otherResult); // 저장된 상대 참조로 결과 전달
}


private void ApplyRepelForce(
    CharacterDuelAI resolvedTarget, // 판정 시점에 확정된 상대 참조
    GlobalGameRuleManager ruleManager, // 결투 규칙 매니저 참조
    GlobalGameRuleManager.DuelResultType selfResult, // 자신의 결투 결과
    GlobalGameRuleManager.DuelResultType otherResult) // 상대의 결투 결과
{
    if (navigationMovementSystem == null || resolvedTarget == null)
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

    float selfForceMagnitude = ruleManager.GetRepelForceByResult(selfResult); // 자신의 넉백 힘 크기 계산
    float otherForceMagnitude = ruleManager.GetRepelForceByResult(otherResult); // 상대의 넉백 힘 크기 계산

    if (selfPushDirection.sqrMagnitude > 0.0001f)
    {
        navigationMovementSystem.ApplyExternalForce(selfPushDirection * selfForceMagnitude, false); // 자신에게 저장된 반대 방향 넉백 적용
    }

    if (otherPushDirection.sqrMagnitude > 0.0001f)
    {
        otherMovementSystem.ApplyExternalForce(otherPushDirection * otherForceMagnitude, false); // 상대에게 저장된 반대 방향 넉백 적용
    }
}


public void EndDuelFromOtherSide() // 상대 쪽에서 결투 종료 요청
{
    EndDuel(); // 일반 종료 처리
}

public void ReceiveResolvedDuel(GlobalGameRuleManager.DuelResultType resultType) // 상대가 판정한 결투 결과 수신
{
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

    if (navigationMovementSystem != null)
    {
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

private void ApplyPreDuelFacingDirection() // 결투 돌진 시작 직전 대상의 X축 위치 기준으로 방향과 회전을 먼저 맞춤
{
    if (currentTarget == null)
    {
        return; // 현재 타겟이 없으면 종료
    }

    if (navigationMovementSystem != null)
    {
        navigationMovementSystem.UpdateFacingDirectionByTargetPosition(currentTarget.transform.position); // 결투 대상 위치 기준으로 방향 상태 갱신
    }

    if (moveCommandController != null)
    {
        moveCommandController.ApplyDirectionLinkedRotationImmediatelyFromCurrentFacing(); // 갱신된 방향 상태 기준으로 회전 즉시 1회 적용
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

    public CharacterActionSound GetCharacterActionSound() // 행동 사운드 설정기 반환
    {
        return characterActionSound; // 행동 사운드 참조 반환
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

private void PlayCurrentDuelResolveAnimation() // 현재 선택된 결투 기술의 결투 판정 애니메이션 재생
{
    if (characterAnimationPlayer == null)
    {
        return; // 재생기가 없으면 종료
    }

    DuelSkillDefinitionSO currentSkill = GetCurrentSelectedDuelSkill(); // 현재 선택 기술 가져오기

    if (currentSkill == null || currentSkill.DuelResolveAnimationClip == null)
    {
        return; // 기술 또는 애니메이션이 없으면 종료
    }

    if (currentSkill.UseDuelResolveAnimationLoop)
    {
        characterAnimationPlayer.PlayLoopAnimation(currentSkill.DuelResolveAnimationClip); // 판정 애니메이션 루프 재생
        return;
    }

    characterAnimationPlayer.PlayOneShotAnimation(currentSkill.DuelResolveAnimationClip); // 판정 애니메이션 1회 재생
}

public void EndDuel(bool stopManualAnimation = true) // 결투 종료 처리
{
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

}