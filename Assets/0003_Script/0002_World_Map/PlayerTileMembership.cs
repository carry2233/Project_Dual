using UnityEngine; // Unity 기본 네임스페이스

/// <summary>
/// 플레이어가 현재 소속된 타일 정보를 저장하는 스크립트
/// - 타일 번호 저장
/// - 타일 프리팹 번호 저장
/// - 타일 위치 저장
/// </summary>
public class PlayerTileMembership : MonoBehaviour
{
    [Header("현재 소속 타일 정보")]
    [SerializeField] private int currentTileNumber = -1; // 현재 소속 타일 번호
    [SerializeField] private int currentTilePrefabNumber = -1; // 현재 소속 타일 프리팹 번호
    [SerializeField] private Vector3 currentTileWorldPosition = Vector3.zero; // 현재 소속 타일 위치

    public int CurrentTileNumber => currentTileNumber; // 현재 타일 번호 반환
    public int CurrentTilePrefabNumber => currentTilePrefabNumber; // 현재 타일 프리팹 번호 반환
    public Vector3 CurrentTileWorldPosition => currentTileWorldPosition; // 현재 타일 위치 반환

    public void SetCurrentTile(TilePrefab tilePrefab, Transform tileTransform) // 타일 정보를 받아 현재 소속 타일로 저장
    {
        if (tilePrefab == null || tileTransform == null)
        {
            Debug.LogWarning("[PlayerTileMembership] 타일 정보가 비어 있어 현재 타일 정보를 저장할 수 없습니다.", this); // 참조 누락 경고
            return;
        }

        currentTileNumber = tilePrefab.TileNumber; // 타일 번호 저장
        currentTilePrefabNumber = tilePrefab.TilePrefabNumber; // 타일 프리팹 번호 저장
        currentTileWorldPosition = tileTransform.position; // 타일 위치 저장
    }

    public void ClearCurrentTile() // 소속 타일 정보 초기화
    {
        currentTileNumber = -1; // 타일 번호 초기화
        currentTilePrefabNumber = -1; // 타일 프리팹 번호 초기화
        currentTileWorldPosition = Vector3.zero; // 타일 위치 초기화
    }
}