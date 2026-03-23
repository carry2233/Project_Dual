using UnityEngine;

/// <summary>
/// AI 조작형 캐릭터 명령 컨트롤러
/// - 자기 팀 번호와 다른 캐릭터를 탐색
/// - 가장 가까운 적을 추적
/// - 실제 결투 판정은 CharacterDuelAI가 수행
/// </summary>
public class EnemyAIController : MonoBehaviour
{
    [Header("필수 참조")]
    [SerializeField] private CharacterDuelAI characterDuelAI; // 결투 AI 참조
    [SerializeField] private CharacterStatSystem characterStatSystem; // 캐릭터 스탯 참조
    [SerializeField] private NavigationMovementSystem navigationMovementSystem; // 이동 시스템 참조

    [Header("추적 설정")]
    [SerializeField] private float chaseStopDistance = 1.5f; // 일반 추적 시 이동 종료 거리

    private void Awake() // 초기 참조 자동 연결
    {
        if (characterDuelAI == null)
        {
            characterDuelAI = GetComponent<CharacterDuelAI>(); // 결투 AI 자동 참조
        }

        if (characterStatSystem == null)
        {
            characterStatSystem = GetComponent<CharacterStatSystem>(); // 스탯 자동 참조
        }

        if (navigationMovementSystem == null)
        {
            navigationMovementSystem = GetComponent<NavigationMovementSystem>(); // 이동 시스템 자동 참조
        }
    }

    private void Update() // 매 프레임 AI 명령 처리
    {
        if (characterDuelAI == null || characterStatSystem == null || navigationMovementSystem == null)
        {
            return; // 필수 참조가 없으면 종료
        }

        if (characterDuelAI.CurrentControlMode != CharacterDuelAI.ControlMode.EnemyAIControlled)
        {
            return; // AI 조작형이 아니면 종료
        }

        if (characterStatSystem.IsActionLocked)
        {
            return; // 행동 불가 상태면 종료
        }

        if (navigationMovementSystem.IsUnderExternalForce)
        {
            return; // 외부 힘 적용 중이면 종료
        }

        if (characterDuelAI.IsDashingToDuel)
        {
            return; // 결투 돌진 중이면 일반 AI 이동 중단
        }

        CharacterDuelAI target = characterDuelAI.FindNearestEnemy(); // 가장 가까운 적 탐색
        characterDuelAI.SetCurrentTarget(target); // 현재 타겟 설정
        UpdateChaseTarget(target); // 일반 추적 처리
    }

    private void UpdateChaseTarget(CharacterDuelAI target) // 현재 타겟 추적 처리
    {
        if (target == null)
        {
            navigationMovementSystem.StopMove(); // 타겟이 없으면 이동 정지
            return;
        }

        float distanceToTarget = Vector2.Distance(transform.position, target.transform.position); // 타겟까지 거리 계산

        if (distanceToTarget <= chaseStopDistance)
        {
            navigationMovementSystem.StopMove(); // 충분히 가까우면 이동 정지
            return;
        }

        navigationMovementSystem.SetMoveDestination(target.transform.position); // 타겟 위치로 이동
    }
}