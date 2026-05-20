using UnityEngine; // Unity 기본 기능 사용

/// <summary>
/// 타일 정보를 정의하는 ScriptableObject입니다.
/// - 타일 종류 ID
/// - 담당 타일 프리팹
/// - 탐색 충족도
/// </summary>
[CreateAssetMenu(fileName = "NewTileInfoDefinition", menuName = "Project Dual/타일 정보 정의")]
public class TileInfoDefinition : ScriptableObject
{
    [Header("타일 식별 정보")]
    [SerializeField] private int tileId = 0; // 타일 종류 ID

    [Header("담당 타일 프리팹")]
    [SerializeField] private TilePrefab targetTilePrefab; // 이 정보가 담당하는 TilePrefab

    [Header("탐색 정보")]
    [SerializeField] private int requiredSearchValue = 1; // 이동 가능까지 필요한 탐색 충족도

    [Header("탐색 소요 시간 설정")]
    [SerializeField] private int minSearchRequiredMinute = 10; // 최소 탐색 소요 시간
    [SerializeField] private int maxSearchRequiredMinute = 30; // 최대 탐색 소요 시간

    public int MinSearchRequiredMinute => minSearchRequiredMinute; // 최소 탐색 소요 시간 반환
    public int MaxSearchRequiredMinute => maxSearchRequiredMinute; // 최대 탐색 소요 시간 반환  
    public int TileId => tileId; // 타일 종류 ID 반환
    public TilePrefab TargetTilePrefab => targetTilePrefab; // 담당 타일 프리팹 반환
    public int RequiredSearchValue => requiredSearchValue; // 필요 탐색 충족도 반환

    public bool IsMatch(int targetTileId) // 타일 ID 일치 여부 확인
    {
        return tileId == targetTileId; // ID가 같으면 true
    }
}