using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class AttackSkillHitbox : MonoBehaviour
{
    [Header("벽 감지 상태")]
    [SerializeField] private bool isTouchingWall; // 현재 벽과 접촉 중인지 여부
    [SerializeField] private int touchingWallCount; // 접촉 중인 벽 콜라이더 개수

    [Header("벽 감지 레이어")]
    [SerializeField] private LayerMask wallLayerMask; // 벽으로 판정할 레이어

    private BoxCollider2D boxCollider2D; // 공격기술 판정용 박스 콜라이더

    public bool IsTouchingWall => isTouchingWall; // 벽 접촉 여부 반환
    public int TouchingWallCount => touchingWallCount; // 접촉 중인 벽 개수 반환
    public BoxCollider2D BoxCollider2D => boxCollider2D; // 박스 콜라이더 반환

    private void Awake() // 시작 시 컴포넌트 참조
    {
        boxCollider2D = GetComponent<BoxCollider2D>(); // 박스 콜라이더 참조

        if (boxCollider2D != null)
        {
            boxCollider2D.isTrigger = true; // 공격기술 벽 감지는 트리거로 고정
        }
    }

    public void Initialize(LayerMask targetWallLayerMask) // 히트박스 초기화
    {
        wallLayerMask = targetWallLayerMask; // 벽 레이어 저장
        touchingWallCount = 0; // 벽 접촉 수 초기화
        isTouchingWall = false; // 벽 접촉 상태 초기화
    }

    private void OnTriggerEnter2D(Collider2D other) // 다른 콜라이더와 접촉 시작
    {
        if (!IsWallLayer(other.gameObject.layer))
        {
            return; // 벽 레이어가 아니면 종료
        }

        touchingWallCount++; // 접촉 벽 개수 증가
        isTouchingWall = touchingWallCount > 0; // 벽 접촉 상태 갱신
    }

    private void OnTriggerExit2D(Collider2D other) // 다른 콜라이더와 접촉 해제
    {
        if (!IsWallLayer(other.gameObject.layer))
        {
            return; // 벽 레이어가 아니면 종료
        }

        touchingWallCount = Mathf.Max(0, touchingWallCount - 1); // 접촉 벽 개수 감소
        isTouchingWall = touchingWallCount > 0; // 벽 접촉 상태 갱신
    }

    public void ClearTouchState() // 접촉 상태 강제 초기화
    {
        touchingWallCount = 0; // 접촉 벽 개수 초기화
        isTouchingWall = false; // 벽 접촉 상태 초기화
    }

    private bool IsWallLayer(int targetLayer) // 대상 레이어가 벽 레이어인지 확인
    {
        return (wallLayerMask.value & (1 << targetLayer)) != 0; // 레이어마스크 포함 여부 반환
    }
}