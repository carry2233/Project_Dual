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

    [Header("타일 레이캐스트 콜라이더")]
[SerializeField] private Collider tileRaycastCollider; // 마우스 레이캐스트 감지에 사용할 3D 콜라이더


[Header("타일 방문 상태")]
[SerializeField] private bool hasEnteredTile; // 한 세이브에서 소속한 적이 있는 타일인지 여부

[Header("타일 시야 처리 오브젝트")]
[SerializeField] private GameObject tileDarknessObject; // 미방문 타일 어둠처리 오브젝트
[SerializeField] private GameObject tileBodyObject; // 실제 타일 본체 오브젝트

public bool HasEnteredTile => hasEnteredTile;

public void SetTileEnteredState(bool entered, bool useUnvisitedDarkness) // 타일 방문 상태 적용
{
    hasEnteredTile = entered;

    if (!useUnvisitedDarkness)
    {
        if (tileDarknessObject != null)
            tileDarknessObject.SetActive(false);

        if (tileBodyObject != null)
            tileBodyObject.SetActive(true);

        return;
    }

    if (tileDarknessObject != null)
        tileDarknessObject.SetActive(!hasEnteredTile);

    if (tileBodyObject != null)
        tileBodyObject.SetActive(hasEnteredTile);
}

public void MarkTileEntered(bool useUnvisitedDarkness) // 현재 타일을 방문 처리
{
    SetTileEnteredState(true, useUnvisitedDarkness);
}

public Collider TileRaycastCollider => tileRaycastCollider; // 타일 레이캐스트 콜라이더 반환

    public int TilePrefabNumber => tilePrefabNumber; // 프리팹 종류 번호 반환
    public int TileNumber => tileNumber; // 현재 타일 번호 반환

    public void SetTileNumber(int newTileNumber) // 배치 시 타일 번호 지정
    {
        tileNumber = newTileNumber; // 타일 번호 저장
    }

    private void Awake() // 시작 전 콜라이더 자동 참조
{
    if (tileRaycastCollider == null)
    {
        tileRaycastCollider = GetComponent<Collider>(); // 같은 오브젝트의 3D 콜라이더 자동 참조
    }

    if (tileRaycastCollider == null)
    {
        tileRaycastCollider = GetComponentInChildren<Collider>(); // 자식 오브젝트의 3D 콜라이더 자동 참조
    }
}

public bool IsTileRaycastCollider(Collider targetCollider) // 지정 콜라이더가 이 타일의 감지 콜라이더인지 확인
{
    if (targetCollider == null)
    {
        return false; // 대상 콜라이더가 없으면 실패
    }

    if (tileRaycastCollider == null)
    {
        return targetCollider.GetComponentInParent<TilePrefab>() == this; // 참조가 없으면 부모 기준으로 검사
    }

    return targetCollider == tileRaycastCollider; // 등록된 콜라이더와 일치하는지 반환
}
}