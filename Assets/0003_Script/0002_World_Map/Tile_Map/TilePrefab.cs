using UnityEngine; // Unity 기본 네임스페이스

/// <summary>
/// 타일 프리팹 정보 보관용 스크립트
/// - 프리팹 종류 번호 보관
/// - 배치된 타일의 고유 타일 번호 보관
/// </summary>
public class TilePrefab : MonoBehaviour
{
    [Header("타일 프리팹 설정")]
    [SerializeField] private int tilePrefabNumber = 0; // 이 프리팹의 종류 번호

    [Header("배치된 타일 정보")]
    [SerializeField] private int tileNumber = -1; // 실제 배치 시 부여되는 타일 번호

    public int TilePrefabNumber => tilePrefabNumber; // 프리팹 종류 번호 반환
    public int TileNumber => tileNumber; // 현재 타일 번호 반환

    public void SetTileNumber(int newTileNumber) // 배치 시 타일 번호 지정
    {
        tileNumber = newTileNumber; // 타일 번호 저장
    }
}