using System.Collections.Generic; // HashSet 사용
using UnityEngine; // Unity 기본 네임스페이스
using UnityEngine.Tilemaps; // Tilemap 사용
using System.IO; // JSON 파일 저장/로드용
using UnityEngine.UI; // UI 버튼 참조용

/// <summary>
/// 육각형 타일맵의 특정 원점 셀 기준 n칸 범위 내 실제 타일 위치에 프리팹을 배치하는 매니저
/// - XY 평면 배치 기준
/// - 게임 시작 시 자동 배치
/// - Hex Tilemap은 배치 틀로만 사용
/// </summary>
public class HexTilePlacementManager : MonoBehaviour
{
    public enum HexOffsetType
    {
        OddRow,    // Point Top 계열에서 주로 사용되는 홀수 행 오프셋
        OddColumn  // Flat Top 계열에서 주로 사용되는 홀수 열 오프셋
    }

[Header("타일맵 참조")]
[SerializeField] private Tilemap targetTilemap; // 배치 기준이 되는 육각형 타일맵
[SerializeField] private Transform placementParent; // 생성된 프리팹들을 정리해서 넣을 부모

public enum TileSpreadShape
{
    Basic, // 주변으로 균등하게 퍼짐
    Long   // 한 방향으로 치우쳐지며 퍼짐
}

[System.Serializable]
private class PlacementPrefabEntry
{
    public GameObject tilePrefab; // 이 그룹에서 사용할 타일 프리팹
    public int centerCount = 1; // 생성될 중심지 개수
    public int spreadTileCount = 10; // 퍼져서 배치될 타일 개수
    public TileSpreadShape spreadShape = TileSpreadShape.Basic; // 퍼짐 형태
}

[System.Serializable]
private class SpreadFrontierData
{
    public Vector3Int cellPosition; // 현재 확장 기준 셀
    public Vector3Int centerCellPosition; // 이 확장의 중심 셀
    public Vector2 preferredDirection; // 길쭉형일 때 선호 방향

    public SpreadFrontierData(Vector3Int cellPosition, Vector3Int centerCellPosition, Vector2 preferredDirection) // 확장 정보 생성자
    {
        this.cellPosition = cellPosition; // 현재 셀 저장
        this.centerCellPosition = centerCellPosition; // 중심 셀 저장
        this.preferredDirection = preferredDirection; // 선호 방향 저장
    }
}

[Header("타일 배치 그룹 설정")]
[SerializeField] private List<PlacementPrefabEntry> placementPrefabList = new List<PlacementPrefabEntry>(); // 여러 타일 프리팹 그룹 목록
[SerializeField] private GameObject emptyTilePrefab; // 남는 칸에 배치할 빈칸용 타일 프리팹

    [Header("원점 셀 좌표")]
    [SerializeField] private Vector2Int originCell = Vector2Int.zero; // 육각형 칸 범위의 기준이 되는 셀 좌표

    [Header("배치 범위 설정")]
    [SerializeField] private int placementRange = 0; // 원점 기준 몇 칸 범위까지 배치할지
    [SerializeField] private HexOffsetType hexOffsetType = HexOffsetType.OddRow; // 현재 타일맵이 어떤 오프셋 규칙을 쓰는지

    [Header("설치 개수 설정")]
    [SerializeField] private bool useFixedPlacementCount = true; // 고정 설치 개수 모드 사용 여부
    [SerializeField] private int placementCount = 10; // 실제로 설치할 목표 타일 개수

    

[Header("배치 옵션")]
[SerializeField] private bool placeOnStart = true; // 게임 시작 시 자동 배치 여부
[SerializeField] private bool clearPreviousChildrenOnStart = false; // 시작 시 placementParent의 자식 오브젝트 정리 여부
[SerializeField] private bool skipIfAlreadyPlacedAtCell = true; // 같은 셀에 중복 배치를 막을지 여부
[SerializeField] private bool requirePaintedTile = false; // true면 실제 칠해진 타일이 있는 셀만 배치, false면 칠해진 타일 없이도 배치
[SerializeField] private Vector3 worldOffset = Vector3.zero; // 셀 중심 위치에 추가로 더할 월드 오프셋
[SerializeField] private bool alwaysPlaceOriginCell = true; // 시작 중심 타일을 항상 먼저 생성할지 여부

public enum MapShapeType
{
    NormalIsland,   // 일반적인 섬 느낌
    LongIsland,     // 길쭉한 섬 느낌
    CurvedIsland    // 곡선 기반 섬 느낌
}

[Header("맵 형태 설정")]
[SerializeField] private MapShapeType mapShapeType = MapShapeType.NormalIsland; // 생성할 맵 형태 타입
[SerializeField] private int randomSeed = 0; // 노이즈 계산용 시드값
[SerializeField] private float noiseScale = 0.15f; // 노이즈 확대/축소 값
[SerializeField] private float shapeThreshold = 0.45f; // 섬으로 인정할 최소 기준값
[SerializeField] private float edgeFalloffStrength = 1.2f; // 바깥으로 갈수록 줄어드는 강도

[Header("길쭉한 섬 설정")]
[SerializeField] private Vector2 longIslandDirection = new Vector2(1f, 0f); // 길게 늘어질 방향
[SerializeField] private float longIslandLengthWeight = 1.25f; // 길이 방향 가중치
[SerializeField] private float longIslandWidthWeight = 0.75f; // 너비 방향 가중치

[Header("곡선 섬 설정")]
[SerializeField] private float curvedWaveFrequency = 2f; // 곡선 휘어짐 빈도
[SerializeField] private float curvedWaveAmplitude = 2f; // 곡선 휘어짐 크기
[SerializeField] private float curvedIslandWidth = 1.75f; // 곡선 중심선 기준 섬 두께


[Header("저장/복원 설정")]
[SerializeField] private int saveSlotNumber = 0; // 저장본 번호
[SerializeField] private string saveFileName = "HexTilePlacementSave.json"; // 저장 파일 이름

[Header("버튼 참조")]
[SerializeField] private Button saveButton; // 저장 실행 버튼
[SerializeField] private Button loadButton; // 복원 실행 버튼
[SerializeField] private Button clearSaveButton; // 저장본 초기화 버튼

[Header("시작점 타일 설정")]
[SerializeField] private GameObject startPointTilePrefab; // 외곽 1칸에 배치할 시작점 타일 프리팹

private int nextTileNumber = 0; // 다음에 부여할 타일 번호

[System.Serializable]
private class PlacementTileSaveData
{
    public int tileNumber; // 각 배치 타일 번호
    public int tilePrefabNumber; // 어떤 타일 프리팹 종류인지 구분하는 번호
    public float localPosX; // 로컬 위치 X
    public float localPosY; // 로컬 위치 Y
    public float localPosZ; // 로컬 위치 Z
}

[System.Serializable]
private class PlacementSaveData
{
    public int saveSlotNumber; // 저장본 번호
    public List<PlacementTileSaveData> tiles = new List<PlacementTileSaveData>(); // 저장된 타일 목록
}

    private readonly Dictionary<Vector3Int, GameObject> placedObjectMap = new Dictionary<Vector3Int, GameObject>(); // 셀 좌표별 생성 오브젝트 기록


    private void Awake() // 버튼 이벤트 연결
{
    if (saveButton != null)
    {
        saveButton.onClick.AddListener(SavePlacementToJson); // 저장 버튼 연결
    }

    if (loadButton != null)
    {
        loadButton.onClick.AddListener(LoadPlacementFromJson); // 복원 버튼 연결
    }

    if (clearSaveButton != null)
    {
        clearSaveButton.onClick.AddListener(ClearPlacementSaveFile); // 저장본 초기화 버튼 연결
    }
}

private void OnDestroy() // 버튼 이벤트 해제
{
    if (saveButton != null)
    {
        saveButton.onClick.RemoveListener(SavePlacementToJson); // 저장 버튼 해제
    }

    if (loadButton != null)
    {
        loadButton.onClick.RemoveListener(LoadPlacementFromJson); // 복원 버튼 해제
    }

    if (clearSaveButton != null)
    {
        clearSaveButton.onClick.RemoveListener(ClearPlacementSaveFile); // 저장본 초기화 버튼 해제
    }
}
    private void Start() // 게임 시작 시 자동 배치 처리
    {
        if (placeOnStart == false)
        {
            return; // 자동 배치가 꺼져 있으면 종료
        }

        PlacePrefabsInRange(); // 설정된 범위대로 프리팹 배치
    }

public void PlacePrefabsInRange() // 현재 설정값 기준으로 범위 내 타일 생성 실행
{
    if (targetTilemap == null)
    {
        Debug.LogWarning("[HexTilePlacementManager] 타일맵 참조가 비어 있습니다.", this); // 타일맵 누락 경고
        return;
    }

    if (placementPrefabList == null || placementPrefabList.Count == 0)
    {
        Debug.LogWarning("[HexTilePlacementManager] 타일 프리팹 리스트가 비어 있습니다.", this); // 리스트 비어있음 경고
        return;
    }

    if (emptyTilePrefab == null)
    {
        Debug.LogWarning("[HexTilePlacementManager] 빈칸 배치용 타일 프리팹이 비어 있습니다.", this); // 빈칸 프리팹 누락 경고
        return;
    }

    if (placementRange < 0)
    {
        Debug.LogWarning("[HexTilePlacementManager] 배치 범위는 0 이상이어야 합니다.", this); // 음수 범위 방지
        return;
    }

    if (placementCount < 0)
    {
        Debug.LogWarning("[HexTilePlacementManager] 배치 개수는 0 이상이어야 합니다.", this); // 음수 개수 방지
        return;
    }

    if (clearPreviousChildrenOnStart == true)
    {
        ClearPlacedObjects(); // 기존 생성 오브젝트 정리
    }

    nextTileNumber = 0; // 새 생성 시작 전 타일 번호 초기화

    Vector3Int originCellPosition = new Vector3Int(originCell.x, originCell.y, 0); // 원점 셀 좌표 구성
    Vector3Int originCube = OffsetToCube(originCellPosition); // 원점 셀 cube 좌표 계산
    int targetSpawnCount = useFixedPlacementCount == true ? placementCount : int.MaxValue; // 최종 생성 셀 수 제한값

    HashSet<Vector3Int> generatedCells = new HashSet<Vector3Int>(); // 최종 생성 대상 셀 목록
    List<Vector3Int> frontierCells = new List<Vector3Int>(); // 생성 확장용 프론티어 셀 목록

    if (CanUseCellForGeneration(originCellPosition, originCellPosition, originCube, out int originHexDistance) == false)
    {
        Debug.LogWarning("[HexTilePlacementManager] 원점 셀이 현재 생성 조건을 만족하지 않습니다.", this); // 원점 불가 경고
        return;
    }

    generatedCells.Add(originCellPosition); // 원점 셀 기록
    frontierCells.Add(originCellPosition); // 프론티어 시작점 등록

    System.Random random = new System.Random(randomSeed); // 시드 기반 랜덤 생성기

    while (frontierCells.Count > 0 && generatedCells.Count < targetSpawnCount) // 목표 개수까지 셀 생성
    {
        int frontierIndex = random.Next(0, frontierCells.Count); // 프론티어 중 랜덤 기준 셀 선택
        Vector3Int baseCell = frontierCells[frontierIndex]; // 이번 확장의 기준 셀
        List<Vector3Int> expandableNeighbors = GetValidExpandableNeighbors(baseCell, originCellPosition, originCube, generatedCells); // 생성 가능한 인접 셀 수집

        if (expandableNeighbors.Count == 0)
        {
            frontierCells.RemoveAt(frontierIndex); // 더 확장 불가면 프론티어 제거
            continue;
        }

        int neighborIndex = random.Next(0, expandableNeighbors.Count); // 생성할 이웃 셀 랜덤 선택
        Vector3Int selectedCell = expandableNeighbors[neighborIndex]; // 최종 생성할 셀

        generatedCells.Add(selectedCell); // 셀 생성 기록 추가
        frontierCells.Add(selectedCell); // 새 셀도 프론티어에 추가
    }

    FillClosedSingleCellHoles(generatedCells, targetSpawnCount, originCellPosition, originCube); // 내부 1칸 구멍 메우기

    Dictionary<Vector3Int, GameObject> assignedPrefabMap = new Dictionary<Vector3Int, GameObject>(); // 셀별 배치 프리팹 기록
    AssignGroupedPrefabs(generatedCells, assignedPrefabMap, random); // 그룹 타일 배정
    AssignEmptyPrefabToRemainingCells(generatedCells, assignedPrefabMap); // 남은 셀은 빈칸 타일 배정
    PlaceStartPointOnOuterCell(generatedCells, assignedPrefabMap, random); // 외곽 셀 하나를 시작점으로 교체
    SpawnAssignedPrefabs(assignedPrefabMap); // 최종 프리팹 실제 생성
}

public void ClearPlacedObjects() // 현재 매니저가 생성한 프리팹들을 모두 정리
{
    foreach (KeyValuePair<Vector3Int, GameObject> pair in placedObjectMap)
    {
        if (pair.Value != null)
        {
            Destroy(pair.Value); // 생성했던 오브젝트 삭제
        }
    }

    placedObjectMap.Clear(); // 셀 기록 초기화
    nextTileNumber = 0; // 타일 번호 초기화

    if (placementParent == null)
    {
        return; // 부모가 없으면 여기서 종료
    }

    for (int i = placementParent.childCount - 1; i >= 0; i--)
    {
        Destroy(placementParent.GetChild(i).gameObject); // 부모 밑 자식 오브젝트 정리
    }
}
private void SpawnPrefabAtCell(Vector3Int cellPosition, GameObject prefabToSpawn) // 지정한 셀 중심 위치에 원하는 프리팹 생성
{
    if (prefabToSpawn == null)
    {
        return; // 생성할 프리팹이 없으면 종료
    }

    Vector3 spawnPosition = targetTilemap.GetCellCenterWorld(cellPosition) + worldOffset; // 셀 중심 월드 위치 계산
    Quaternion spawnRotation = prefabToSpawn.transform.rotation; // 해당 프리팹 기본 회전값 사용

    Transform parentToUse = placementParent != null ? placementParent : transform; // 부모가 지정되지 않았으면 현재 오브젝트를 부모로 사용
    GameObject spawnedObject = Instantiate(prefabToSpawn, spawnPosition, spawnRotation, parentToUse); // 프리팹 생성

    TilePrefab tilePrefab = spawnedObject.GetComponent<TilePrefab>(); // 배치된 타일 프리팹 컴포넌트 가져오기
    if (tilePrefab != null)
    {
        tilePrefab.SetTileNumber(nextTileNumber); // 생성 시 타일 번호 지정
        nextTileNumber++; // 다음 타일 번호 증가
    }

    placedObjectMap[cellPosition] = spawnedObject; // 생성 오브젝트를 셀 기준으로 기록
}

    private Vector3Int OffsetToCube(Vector3Int offsetCell) // 현재 오프셋 좌표를 cube 좌표로 변환
    {
        int col = offsetCell.x; // 셀의 X를 열 값으로 사용
        int row = offsetCell.y; // 셀의 Y를 행 값으로 사용

        if (hexOffsetType == HexOffsetType.OddRow)
        {
            int cubeX = col - (row - (row & 1)) / 2; // odd-r 기준 cube x 계산
            int cubeZ = row; // odd-r 기준 cube z 계산
            int cubeY = -cubeX - cubeZ; // cube 좌표 합 0 규칙 맞춤

            return new Vector3Int(cubeX, cubeY, cubeZ); // 변환된 cube 좌표 반환
        }
        else
        {
            int cubeX = col; // odd-q 기준 cube x 계산
            int cubeZ = row - (col - (col & 1)) / 2; // odd-q 기준 cube z 계산
            int cubeY = -cubeX - cubeZ; // cube 좌표 합 0 규칙 맞춤

            return new Vector3Int(cubeX, cubeY, cubeZ); // 변환된 cube 좌표 반환
        }
    }

    private int GetCubeDistance(Vector3Int a, Vector3Int b) // 두 cube 좌표 사이의 육각형 칸 거리 계산
    {
        int deltaX = Mathf.Abs(a.x - b.x); // x축 차이 절댓값
        int deltaY = Mathf.Abs(a.y - b.y); // y축 차이 절댓값
        int deltaZ = Mathf.Abs(a.z - b.z); // z축 차이 절댓값

        return Mathf.Max(deltaX, Mathf.Max(deltaY, deltaZ)); // cube distance 반환
    }

    private bool IsCellIncludedByShape(Vector3Int candidateCell, Vector3Int originCellPosition, int hexDistance) // 현재 선택된 형태 기준으로 셀 포함 여부 판정
{
    switch (mapShapeType)
    {
        case MapShapeType.LongIsland:
            return EvaluateLongIsland(candidateCell, originCellPosition, hexDistance); // 길쭉한 섬 판정

        case MapShapeType.CurvedIsland:
            return EvaluateCurvedIsland(candidateCell, originCellPosition, hexDistance); // 곡선 섬 판정

        default:
            return EvaluateNormalIsland(candidateCell, originCellPosition, hexDistance); // 일반 섬 판정
    }
}

private bool EvaluateNormalIsland(Vector3Int candidateCell, Vector3Int originCellPosition, int hexDistance) // 일반 섬 형태 판정
{
    float noiseValue = GetCellNoise(candidateCell); // 현재 셀의 노이즈값 계산
    float distance01 = placementRange == 0 ? 0f : (float)hexDistance / placementRange; // 중심 대비 거리 비율 계산
    float falloff = Mathf.Pow(distance01, edgeFalloffStrength); // 가장자리 감소량 계산
    float finalValue = noiseValue - falloff; // 노이즈에서 가장자리 감소량 반영

    return finalValue >= shapeThreshold; // 기준치 이상이면 섬에 포함
}

private bool EvaluateLongIsland(Vector3Int candidateCell, Vector3Int originCellPosition, int hexDistance) // 길쭉한 섬 형태 판정
{
    Vector2 dir = longIslandDirection.sqrMagnitude > 0.0001f ? longIslandDirection.normalized : Vector2.right; // 길이 방향 정규화
    Vector2 perpendicular = new Vector2(-dir.y, dir.x); // 길이 방향에 대한 수직 방향 계산
    Vector2 offset = new Vector2(candidateCell.x - originCellPosition.x, candidateCell.y - originCellPosition.y); // 원점 대비 현재 셀 오프셋

    float along = Vector2.Dot(offset, dir) * longIslandLengthWeight; // 길이 방향 영향도
    float across = Vector2.Dot(offset, perpendicular) * longIslandWidthWeight; // 너비 방향 영향도
    float ellipseDistance = Mathf.Sqrt(along * along + across * across); // 타원형 거리 계산

    float range01 = placementRange == 0 ? 0f : ellipseDistance / placementRange; // 길쭉한 섬 기준 거리 비율
    float noiseValue = GetCellNoise(candidateCell); // 현재 셀의 노이즈값 계산
    float falloff = Mathf.Pow(range01, edgeFalloffStrength); // 가장자리 감소량 계산
    float finalValue = noiseValue - falloff; // 최종 판정값 계산

    return finalValue >= shapeThreshold; // 기준치 이상이면 섬에 포함
}

private bool EvaluateCurvedIsland(Vector3Int candidateCell, Vector3Int originCellPosition, int hexDistance) // 곡선 기반 섬 형태 판정
{
    float localX = candidateCell.x - originCellPosition.x; // 원점 기준 현재 셀의 로컬 X
    float localY = candidateCell.y - originCellPosition.y; // 원점 기준 현재 셀의 로컬 Y

    float curveCenterY = Mathf.Sin((localX + randomSeed) * curvedWaveFrequency * 0.1f) * curvedWaveAmplitude; // 현재 X 위치에서 곡선 중심선 Y 계산
    float distanceToCurve = Mathf.Abs(localY - curveCenterY); // 곡선 중심선과의 거리 계산
    float curveWidth01 = curvedIslandWidth <= 0.0001f ? 999f : distanceToCurve / curvedIslandWidth; // 곡선 두께 기준 거리 비율 계산

    float radial01 = placementRange == 0 ? 0f : (float)hexDistance / placementRange; // 전체 반경 기준 거리 비율 계산
    float noiseValue = GetCellNoise(candidateCell); // 현재 셀의 노이즈값 계산
    float falloff = Mathf.Pow(radial01, edgeFalloffStrength) + curveWidth01; // 반경 감소 + 곡선 이탈 패널티 계산
    float finalValue = noiseValue - falloff; // 최종 판정값 계산

    return finalValue >= shapeThreshold; // 기준치 이상이면 섬에 포함
}

private float GetCellNoise(Vector3Int cellPosition) // 셀 위치 기반 노이즈값 계산
{
    float sampleX = (cellPosition.x + randomSeed * 0.137f) * noiseScale; // X축 노이즈 샘플 좌표 계산
    float sampleY = (cellPosition.y + randomSeed * 0.173f) * noiseScale; // Y축 노이즈 샘플 좌표 계산

    return Mathf.PerlinNoise(sampleX, sampleY); // 0~1 범위의 펄린 노이즈 반환
}


private bool CanUseCellForGeneration(Vector3Int candidateCell, Vector3Int originCellPosition, Vector3Int originCube, out int hexDistance) // 셀이 생성 조건을 만족하는지 검사
{
    Vector3Int candidateCube = OffsetToCube(candidateCell); // 후보 셀 cube 좌표 계산
    hexDistance = GetCubeDistance(originCube, candidateCube); // 원점과 후보 셀의 육각형 거리 계산

    if (hexDistance > placementRange) // 범위를 벗어나면 제외
    {
        return false;
    }

    if (requirePaintedTile == true && targetTilemap.HasTile(candidateCell) == false) // 실제 타일 필요 모드인데 타일이 없으면 제외
    {
        return false;
    }

    if (skipIfAlreadyPlacedAtCell == true && placedObjectMap.ContainsKey(candidateCell)) // 이미 배치된 셀이면 제외
    {
        return false;
    }

    if (IsCellIncludedByShape(candidateCell, originCellPosition, hexDistance) == false) // 형태 규칙에 맞지 않으면 제외
    {
        return false;
    }

    return true; // 모든 조건을 만족하면 생성 가능
}

private List<Vector3Int> GetValidExpandableNeighbors(Vector3Int baseCell, Vector3Int originCellPosition, Vector3Int originCube, HashSet<Vector3Int> generatedCells) // 기준 셀 주변의 생성 가능한 이웃 셀 목록 반환
{
    List<Vector3Int> validNeighbors = new List<Vector3Int>(); // 생성 가능한 이웃 셀 목록
    List<Vector3Int> neighbors = GetNeighborCells(baseCell); // 기준 셀의 6방향 이웃 셀 목록

    for (int i = 0; i < neighbors.Count; i++) // 이웃 셀 순회
    {
        Vector3Int neighborCell = neighbors[i]; // 현재 검사할 이웃 셀

        if (generatedCells.Contains(neighborCell) == true) // 이번 공정에서 이미 생성 확정된 셀이면 제외
        {
            continue;
        }

        if (CanUseCellForGeneration(neighborCell, originCellPosition, originCube, out int hexDistance) == false) // 생성 조건 미충족 시 제외
        {
            continue;
        }

        validNeighbors.Add(neighborCell); // 조건 통과 이웃 셀 추가
    }

    return validNeighbors; // 최종 이웃 셀 목록 반환
}

private List<Vector3Int> GetNeighborCells(Vector3Int centerCell) // 지정 셀의 육각형 인접 6칸 반환
{
    List<Vector3Int> neighbors = new List<Vector3Int>(); // 인접 셀 목록
    Vector3Int centerCube = OffsetToCube(centerCell); // 중심 셀 cube 좌표 계산

    Vector3Int[] cubeDirections = new Vector3Int[6] // cube 기준 6방향 정의
    {
        new Vector3Int(1, -1, 0), // 오른쪽 위 방향
        new Vector3Int(1, 0, -1), // 오른쪽 방향
        new Vector3Int(0, 1, -1), // 오른쪽 아래 방향
        new Vector3Int(-1, 1, 0), // 왼쪽 아래 방향
        new Vector3Int(-1, 0, 1), // 왼쪽 방향
        new Vector3Int(0, -1, 1) // 왼쪽 위 방향
    };

    for (int i = 0; i < cubeDirections.Length; i++) // 6방향 순회
    {
        Vector3Int neighborCube = centerCube + cubeDirections[i]; // 이웃 cube 좌표 계산
        Vector3Int neighborOffset = CubeToOffset(neighborCube); // offset 좌표로 복원

        neighbors.Add(neighborOffset); // 이웃 셀 추가
    }

    return neighbors; // 인접 셀 목록 반환
}

private Vector3Int CubeToOffset(Vector3Int cubeCell) // cube 좌표를 현재 오프셋 좌표로 변환
{
    int cubeX = cubeCell.x; // cube x 값
    int cubeZ = cubeCell.z; // cube z 값

    if (hexOffsetType == HexOffsetType.OddRow) // odd-r 오프셋 복원
    {
        int col = cubeX + (cubeZ - (cubeZ & 1)) / 2; // odd-r 기준 열 계산
        int row = cubeZ; // odd-r 기준 행 계산

        return new Vector3Int(col, row, 0); // 변환된 offset 좌표 반환
    }
    else // odd-q 오프셋 복원
    {
        int col = cubeX; // odd-q 기준 열 계산
        int row = cubeZ + (cubeX - (cubeX & 1)) / 2; // odd-q 기준 행 계산

        return new Vector3Int(col, row, 0); // 변환된 offset 좌표 반환
    }
}

private Transform GetPlacementRoot() // 실제 배치 부모 반환
{
    return placementParent != null ? placementParent : transform; // 부모가 없으면 현재 오브젝트 기준 사용
}

private string GetSaveFilePath() // 저장 파일 전체 경로 반환
{
    return Path.Combine(Application.persistentDataPath, saveFileName); // persistentDataPath 기준 파일 경로 생성
}

public void SavePlacementToJson() // 현재 배치 상태를 JSON으로 저장
{
    Transform root = GetPlacementRoot(); // 저장 기준 부모
    PlacementSaveData saveData = new PlacementSaveData(); // 저장 데이터 생성
    saveData.saveSlotNumber = saveSlotNumber; // 저장본 번호 기록

    for (int i = 0; i < root.childCount; i++) // 현재 부모 아래 자식 순회
    {
        Transform child = root.GetChild(i); // 현재 저장 대상 자식
        TilePrefab tilePrefab = child.GetComponent<TilePrefab>(); // 타일 프리팹 정보 컴포넌트 가져오기

        if (tilePrefab == null)
        {
            continue; // 타일 프리팹 정보가 없으면 저장 제외
        }

        PlacementTileSaveData tileData = new PlacementTileSaveData(); // 타일 저장 데이터 생성
        tileData.tileNumber = tilePrefab.TileNumber; // 타일 번호 저장
        tileData.tilePrefabNumber = tilePrefab.TilePrefabNumber; // 타일 프리팹 번호 저장
        tileData.localPosX = child.localPosition.x; // 로컬 위치 X 저장
        tileData.localPosY = child.localPosition.y; // 로컬 위치 Y 저장
        tileData.localPosZ = child.localPosition.z; // 로컬 위치 Z 저장

        saveData.tiles.Add(tileData); // 저장 목록에 추가
    }

    string json = JsonUtility.ToJson(saveData, true); // JSON 문자열 생성
    File.WriteAllText(GetSaveFilePath(), json); // 파일로 저장

    Debug.Log($"[HexTilePlacementManager] 배치 저장 완료 : {GetSaveFilePath()}", this); // 저장 완료 로그
}

public void LoadPlacementFromJson() // JSON 저장본을 기준으로 배치 복원
{
    string savePath = GetSaveFilePath(); // 저장 파일 경로

    if (File.Exists(savePath) == false)
    {
        Debug.LogWarning("[HexTilePlacementManager] 복원할 저장본이 없습니다.", this); // 저장본 없음 경고
        return;
    }

    string json = File.ReadAllText(savePath); // JSON 문자열 읽기
    PlacementSaveData saveData = JsonUtility.FromJson<PlacementSaveData>(json); // JSON 역직렬화

    if (saveData == null)
    {
        Debug.LogWarning("[HexTilePlacementManager] 저장본 데이터를 읽지 못했습니다.", this); // 파싱 실패 경고
        return;
    }

    ClearPlacedObjects(); // 기존 배치 제거
    nextTileNumber = 0; // 복원 시작 전 번호 초기화

    Transform root = GetPlacementRoot(); // 복원 기준 부모

    for (int i = 0; i < saveData.tiles.Count; i++) // 저장된 타일 목록 순회
    {
        PlacementTileSaveData tileData = saveData.tiles[i]; // 현재 복원할 타일 데이터
        GameObject prefabToSpawn = GetPrefabByTilePrefabNumber(tileData.tilePrefabNumber); // 번호에 맞는 프리팹 찾기

        if (prefabToSpawn == null)
        {
            Debug.LogWarning($"[HexTilePlacementManager] tilePrefabNumber {tileData.tilePrefabNumber} 에 맞는 프리팹을 찾지 못했습니다.", this); // 프리팹 매칭 실패 경고
            continue;
        }

        GameObject spawnedObject = Instantiate(prefabToSpawn, root); // 부모 아래 프리팹 생성

        Vector3 localPosition = new Vector3(tileData.localPosX, tileData.localPosY, tileData.localPosZ); // 저장된 로컬 위치 복원
        spawnedObject.transform.localPosition = localPosition; // 로컬 위치 적용
        spawnedObject.transform.localRotation = prefabToSpawn.transform.localRotation; // 프리팹 기본 로컬 회전 적용
        spawnedObject.transform.localScale = prefabToSpawn.transform.localScale; // 프리팹 기본 로컬 스케일 적용

        TilePrefab tilePrefab = spawnedObject.GetComponent<TilePrefab>(); // 생성된 오브젝트의 타일 프리팹 컴포넌트 가져오기
        if (tilePrefab != null)
        {
            tilePrefab.SetTileNumber(tileData.tileNumber); // 저장된 타일 번호 복원
        }

        nextTileNumber = Mathf.Max(nextTileNumber, tileData.tileNumber + 1); // 다음 생성용 번호 보정

        if (targetTilemap != null) // 타일맵이 있으면 셀 기록도 복원
        {
            Vector3 worldPosition = root.TransformPoint(localPosition); // 로컬 위치를 월드 위치로 변환
            Vector3Int cellPosition = targetTilemap.WorldToCell(worldPosition); // 월드 위치 기준 셀 계산
            placedObjectMap[cellPosition] = spawnedObject; // 셀 기준 생성 오브젝트 기록
        }
    }

    Debug.Log($"[HexTilePlacementManager] 배치 복원 완료 : 저장본 번호 {saveData.saveSlotNumber}", this); // 복원 완료 로그
}

public void ClearPlacementSaveFile() // 저장본 파일 초기화
{
    string savePath = GetSaveFilePath(); // 저장 파일 경로

    if (File.Exists(savePath) == true)
    {
        File.Delete(savePath); // 저장 파일 삭제
        Debug.Log("[HexTilePlacementManager] 저장본 초기화 완료", this); // 삭제 완료 로그
    }
    else
    {
        Debug.Log("[HexTilePlacementManager] 초기화할 저장본이 없습니다.", this); // 파일 없음 로그
    }
}

private void FillClosedSingleCellHoles(HashSet<Vector3Int> generatedCells, int maxCellCount, Vector3Int originCellPosition, Vector3Int originCube) // 주변 6칸이 모두 찬 빈칸을 자동으로 메우기
{
    bool addedHole = true; // 이번 반복에서 추가된 구멍이 있는지 여부

    while (addedHole == true && generatedCells.Count < maxCellCount) // 더 이상 메울 구멍이 없거나 개수 제한 도달 시 종료
    {
        addedHole = false; // 이번 루프 추가 여부 초기화
        List<Vector3Int> fillTargets = new List<Vector3Int>(); // 메울 대상 빈칸 목록
        HashSet<Vector3Int> candidateEmptyCells = new HashSet<Vector3Int>(); // 검사할 빈칸 후보 목록

        foreach (Vector3Int generatedCell in generatedCells) // 생성된 모든 셀 기준으로 주변 빈칸 수집
        {
            List<Vector3Int> neighbors = GetNeighborCells(generatedCell); // 현재 셀의 주변 6칸 가져오기

            for (int i = 0; i < neighbors.Count; i++) // 주변 셀 순회
            {
                Vector3Int neighborCell = neighbors[i]; // 현재 검사할 빈칸 후보

                if (generatedCells.Contains(neighborCell) == true) // 이미 생성된 셀이면 제외
                {
                    continue;
                }

                candidateEmptyCells.Add(neighborCell); // 빈칸 후보 등록
            }
        }

        foreach (Vector3Int emptyCell in candidateEmptyCells) // 빈칸 후보 순회
        {
            if (CanUseCellForGeneration(emptyCell, originCellPosition, originCube, out int hexDistance) == false) // 생성 불가능한 칸은 제외
            {
                continue;
            }

            List<Vector3Int> neighbors = GetNeighborCells(emptyCell); // 빈칸의 주변 6칸 가져오기
            int filledNeighborCount = 0; // 채워진 주변 타일 수

            for (int i = 0; i < neighbors.Count; i++) // 주변 6칸 검사
            {
                if (generatedCells.Contains(neighbors[i]) == true) // 생성된 셀이면 카운트 증가
                {
                    filledNeighborCount++; // 채워진 주변 타일 수 증가
                }
            }

            if (filledNeighborCount >= 6) // 주변 6칸이 전부 차 있으면 내부 빈칸으로 판단
            {
                fillTargets.Add(emptyCell); // 메울 대상에 추가
            }
        }

        for (int i = 0; i < fillTargets.Count; i++) // 최종 메우기 실행
        {
            if (generatedCells.Count >= maxCellCount) // 설치 개수 제한 도달 시 종료
            {
                break;
            }

            if (generatedCells.Add(fillTargets[i]) == true) // 새롭게 추가된 경우만 반영
            {
                addedHole = true; // 이번 루프에서 구멍 추가됨 기록
            }
        }
    }
}

private void PlaceStartPointOnOuterCell(HashSet<Vector3Int> generatedCells, System.Random random) // 외곽 셀 1칸에 시작점 타일 배치
{
    if (startPointTilePrefab == null)
    {
        return; // 시작점 프리팹이 없으면 종료
    }

    List<Vector3Int> outerCells = new List<Vector3Int>(); // 외곽 셀 목록

    foreach (Vector3Int cell in generatedCells) // 생성된 셀 전체 순회
    {
        List<Vector3Int> neighbors = GetNeighborCells(cell); // 현재 셀의 6방향 이웃 가져오기
        bool isOuterCell = false; // 외곽 여부

        for (int i = 0; i < neighbors.Count; i++) // 주변 6칸 검사
        {
            if (generatedCells.Contains(neighbors[i]) == false) // 비어 있는 이웃이 하나라도 있으면 외곽
            {
                isOuterCell = true; // 외곽 셀 판정
                break; // 더 볼 필요 없음
            }
        }

        if (isOuterCell == true)
        {
            outerCells.Add(cell); // 외곽 셀 목록에 추가
        }
    }

    if (outerCells.Count == 0)
    {
        return; // 외곽 셀이 없으면 종료
    }

    int selectedIndex = random.Next(0, outerCells.Count); // 외곽 셀 중 랜덤 선택
    Vector3Int selectedOuterCell = outerCells[selectedIndex]; // 시작점으로 사용할 셀

    if (placedObjectMap.TryGetValue(selectedOuterCell, out GameObject existingObject) == true) // 기존 일반 타일이 있으면
    {
        if (existingObject != null)
        {
            Destroy(existingObject); // 기존 일반 타일 제거
        }

        placedObjectMap.Remove(selectedOuterCell); // 기존 셀 기록 제거
    }

    SpawnPrefabAtCell(selectedOuterCell, startPointTilePrefab); // 시작점 타일 프리팹 배치
}

private GameObject GetPrefabByTilePrefabNumber(int tilePrefabNumber) // 저장된 타일 프리팹 번호에 맞는 프리팹 찾기
{
    for (int i = 0; i < placementPrefabList.Count; i++) // 그룹 리스트 순회
    {
        PlacementPrefabEntry entry = placementPrefabList[i]; // 현재 그룹 데이터

        if (entry == null || entry.tilePrefab == null) // 프리팹이 비어 있으면 제외
        {
            continue;
        }

        TilePrefab groupTilePrefab = entry.tilePrefab.GetComponent<TilePrefab>(); // 그룹 프리팹의 TilePrefab 컴포넌트 가져오기
        if (groupTilePrefab != null && groupTilePrefab.TilePrefabNumber == tilePrefabNumber)
        {
            return entry.tilePrefab; // 일치하는 그룹 프리팹 반환
        }
    }

    TilePrefab emptyPrefabInfo = emptyTilePrefab != null ? emptyTilePrefab.GetComponent<TilePrefab>() : null; // 빈칸 프리팹 정보 가져오기
    if (emptyPrefabInfo != null && emptyPrefabInfo.TilePrefabNumber == tilePrefabNumber)
    {
        return emptyTilePrefab; // 빈칸 프리팹 반환
    }

    TilePrefab startPrefabInfo = startPointTilePrefab != null ? startPointTilePrefab.GetComponent<TilePrefab>() : null; // 시작점 프리팹 정보 가져오기
    if (startPrefabInfo != null && startPrefabInfo.TilePrefabNumber == tilePrefabNumber)
    {
        return startPointTilePrefab; // 시작점 프리팹 반환
    }

    return null; // 일치하는 프리팹이 없으면 null 반환
}

private void AssignGroupedPrefabs(HashSet<Vector3Int> generatedCells, Dictionary<Vector3Int, GameObject> assignedPrefabMap, System.Random random) // 그룹 리스트 기준으로 셀에 프리팹 배정
{
    for (int i = 0; i < placementPrefabList.Count; i++) // 그룹 목록 순회
    {
        PlacementPrefabEntry entry = placementPrefabList[i]; // 현재 그룹 데이터

        if (entry == null || entry.tilePrefab == null) // 프리팹이 비어 있으면 제외
        {
            continue;
        }

        int remainingCellCount = generatedCells.Count - assignedPrefabMap.Count; // 아직 배정되지 않은 셀 수
        int targetGroupTileCount = Mathf.Min(entry.spreadTileCount, remainingCellCount); // 이 그룹이 실제로 배정할 타일 수

        if (targetGroupTileCount <= 0)
        {
            continue;
        }

        List<Vector3Int> availableCells = GetUnassignedCells(generatedCells, assignedPrefabMap); // 아직 비어 있는 셀 목록
        int actualCenterCount = Mathf.Clamp(entry.centerCount, 1, Mathf.Min(targetGroupTileCount, availableCells.Count)); // 실제 중심지 개수 보정

        if (actualCenterCount <= 0)
        {
            continue;
        }

        List<SpreadFrontierData> frontierList = new List<SpreadFrontierData>(); // 이 그룹 전용 확장 프론티어
        int placedGroupTileCount = 0; // 현재 그룹이 배정한 타일 수

        for (int centerIndex = 0; centerIndex < actualCenterCount; centerIndex++) // 중심지 먼저 배정
        {
            if (availableCells.Count == 0 || placedGroupTileCount >= targetGroupTileCount)
            {
                break;
            }

            int pickIndex = random.Next(0, availableCells.Count); // 중심지 셀 랜덤 선택
            Vector3Int centerCell = availableCells[pickIndex]; // 선택된 중심지 셀
            availableCells.RemoveAt(pickIndex); // 사용한 셀 제거

            assignedPrefabMap[centerCell] = entry.tilePrefab; // 중심지 셀에 그룹 프리팹 배정
            frontierList.Add(new SpreadFrontierData(centerCell, centerCell, GetRandomSpreadDirection(random))); // 확장 시작점 추가
            placedGroupTileCount++; // 그룹 배정 수 증가
        }

        while (frontierList.Count > 0 && placedGroupTileCount < targetGroupTileCount) // 중심지 기준으로 퍼져나가며 배정
        {
            int frontierIndex = random.Next(0, frontierList.Count); // 프론티어 랜덤 선택
            SpreadFrontierData frontierData = frontierList[frontierIndex]; // 현재 확장 기준 데이터
            List<Vector3Int> candidates = GetSpreadableNeighbors(frontierData.cellPosition, generatedCells, assignedPrefabMap); // 확장 가능한 이웃 셀 목록

            if (candidates.Count == 0)
            {
                frontierList.RemoveAt(frontierIndex); // 더 퍼질 수 없으면 프론티어 제거
                continue;
            }

            Vector3Int selectedCell = SelectSpreadCell(candidates, frontierData, entry.spreadShape, random); // 퍼짐 형태에 따라 셀 선택
            assignedPrefabMap[selectedCell] = entry.tilePrefab; // 선택된 셀에 그룹 프리팹 배정
            frontierList.Add(new SpreadFrontierData(selectedCell, frontierData.centerCellPosition, frontierData.preferredDirection)); // 새 셀도 확장 대상으로 추가
            placedGroupTileCount++; // 그룹 배정 수 증가
        }
    }
}

private List<Vector3Int> GetUnassignedCells(HashSet<Vector3Int> generatedCells, Dictionary<Vector3Int, GameObject> assignedPrefabMap) // 아직 프리팹이 배정되지 않은 셀 목록 반환
{
    List<Vector3Int> result = new List<Vector3Int>(); // 결과 셀 목록

    foreach (Vector3Int cell in generatedCells) // 생성 대상 셀 전체 순회
    {
        if (assignedPrefabMap.ContainsKey(cell) == true) // 이미 배정된 셀이면 제외
        {
            continue;
        }

        result.Add(cell); // 아직 배정되지 않은 셀 추가
    }

    return result; // 최종 결과 반환
}

private List<Vector3Int> GetSpreadableNeighbors(Vector3Int baseCell, HashSet<Vector3Int> generatedCells, Dictionary<Vector3Int, GameObject> assignedPrefabMap) // 그룹 확장 가능한 이웃 셀 목록 반환
{
    List<Vector3Int> result = new List<Vector3Int>(); // 결과 셀 목록
    List<Vector3Int> neighbors = GetNeighborCells(baseCell); // 현재 셀의 주변 6칸 가져오기

    for (int i = 0; i < neighbors.Count; i++) // 주변 셀 순회
    {
        Vector3Int neighbor = neighbors[i]; // 현재 검사할 셀

        if (generatedCells.Contains(neighbor) == false) // 생성 대상이 아닌 셀이면 제외
        {
            continue;
        }

        if (assignedPrefabMap.ContainsKey(neighbor) == true) // 이미 다른 프리팹이 배정된 셀이면 제외
        {
            continue;
        }

        result.Add(neighbor); // 확장 가능한 셀 추가
    }

    return result; // 최종 결과 반환
}

private Vector3Int SelectSpreadCell(List<Vector3Int> candidates, SpreadFrontierData frontierData, TileSpreadShape spreadShape, System.Random random) // 퍼짐 형태에 따라 다음 셀 선택
{
    if (spreadShape == TileSpreadShape.Basic || candidates.Count <= 1) // 기본형이면 균등 랜덤 선택
    {
        return candidates[random.Next(0, candidates.Count)]; // 랜덤 후보 반환
    }

    float bestScore = float.MinValue; // 현재 최고 점수
    List<Vector3Int> bestCandidates = new List<Vector3Int>(); // 최고 점수 후보들

    for (int i = 0; i < candidates.Count; i++) // 후보 셀 순회
    {
        Vector3Int candidate = candidates[i]; // 현재 후보 셀
        Vector2 direction = new Vector2(candidate.x - frontierData.centerCellPosition.x, candidate.y - frontierData.centerCellPosition.y); // 중심 기준 방향 계산

        if (direction.sqrMagnitude <= 0.0001f) // 방향 길이가 거의 0이면 제외
        {
            continue;
        }

        float score = Vector2.Dot(direction.normalized, frontierData.preferredDirection.normalized); // 선호 방향과의 정렬 정도 계산

        if (score > bestScore + 0.0001f) // 더 좋은 점수면 후보 갱신
        {
            bestScore = score; // 최고 점수 갱신
            bestCandidates.Clear(); // 이전 후보 비우기
            bestCandidates.Add(candidate); // 새 최고 후보 추가
        }
        else if (Mathf.Abs(score - bestScore) <= 0.0001f) // 같은 점수면 같이 보관
        {
            bestCandidates.Add(candidate); // 공동 최고 후보 추가
        }
    }

    if (bestCandidates.Count == 0) // 후보가 비면 안전하게 랜덤 선택
    {
        return candidates[random.Next(0, candidates.Count)]; // 전체 후보 중 랜덤 반환
    }

    return bestCandidates[random.Next(0, bestCandidates.Count)]; // 최고 점수 후보 중 랜덤 반환
}

private Vector2 GetRandomSpreadDirection(System.Random random) // 길쭉형 확장용 랜덤 방향 반환
{
    Vector2[] directions = new Vector2[6] // 육각형 6방향을 XY 평면 기준으로 근사 표현
    {
        new Vector2(1f, 0f), // 오른쪽
        new Vector2(0.5f, 0.866f), // 오른쪽 위
        new Vector2(-0.5f, 0.866f), // 왼쪽 위
        new Vector2(-1f, 0f), // 왼쪽
        new Vector2(-0.5f, -0.866f), // 왼쪽 아래
        new Vector2(0.5f, -0.866f) // 오른쪽 아래
    };

    return directions[random.Next(0, directions.Length)].normalized; // 랜덤 방향 반환
}

private void AssignEmptyPrefabToRemainingCells(HashSet<Vector3Int> generatedCells, Dictionary<Vector3Int, GameObject> assignedPrefabMap) // 남은 셀에 빈칸 프리팹 배정
{
    if (emptyTilePrefab == null)
    {
        return; // 빈칸 프리팹이 없으면 종료
    }

    foreach (Vector3Int cell in generatedCells) // 생성된 셀 전체 순회
    {
        if (assignedPrefabMap.ContainsKey(cell) == true) // 이미 배정된 셀이면 제외
        {
            continue;
        }

        assignedPrefabMap[cell] = emptyTilePrefab; // 남는 칸은 빈칸 프리팹 배정
    }
}

private void PlaceStartPointOnOuterCell(HashSet<Vector3Int> generatedCells, Dictionary<Vector3Int, GameObject> assignedPrefabMap, System.Random random) // 외곽 셀 1칸에 시작점 프리팹 배정
{
    if (startPointTilePrefab == null)
    {
        return; // 시작점 프리팹이 없으면 종료
    }

    List<Vector3Int> outerCells = new List<Vector3Int>(); // 외곽 셀 목록

    foreach (Vector3Int cell in generatedCells) // 생성된 셀 전체 순회
    {
        List<Vector3Int> neighbors = GetNeighborCells(cell); // 현재 셀의 주변 6칸 가져오기
        bool isOuterCell = false; // 외곽 여부

        for (int i = 0; i < neighbors.Count; i++) // 주변 셀 검사
        {
            if (generatedCells.Contains(neighbors[i]) == false) // 비어 있는 이웃이 하나라도 있으면 외곽
            {
                isOuterCell = true; // 외곽 판정
                break;
            }
        }

        if (isOuterCell == true)
        {
            outerCells.Add(cell); // 외곽 셀 등록
        }
    }

    if (outerCells.Count == 0)
    {
        return; // 외곽 셀이 없으면 종료
    }

    int selectedIndex = random.Next(0, outerCells.Count); // 외곽 셀 중 랜덤 선택
    Vector3Int selectedOuterCell = outerCells[selectedIndex]; // 선택된 시작점 셀

    assignedPrefabMap[selectedOuterCell] = startPointTilePrefab; // 기존 배정을 덮어쓰고 시작점 프리팹 지정
}

private void SpawnAssignedPrefabs(Dictionary<Vector3Int, GameObject> assignedPrefabMap) // 셀별 배정 결과를 실제 프리팹 생성으로 반영
{
    foreach (KeyValuePair<Vector3Int, GameObject> pair in assignedPrefabMap) // 배정된 셀 전체 순회
    {
        SpawnPrefabAtCell(pair.Key, pair.Value); // 해당 셀에 지정 프리팹 생성
    }
}





#if UNITY_EDITOR
    private void OnDrawGizmosSelected() // 선택 시 원점 셀 중심 위치를 기즈모로 표시
    {
        if (targetTilemap == null)
        {
            return; // 타일맵이 없으면 종료
        }

        Vector3Int originCellPosition = new Vector3Int(originCell.x, originCell.y, 0); // 원점 셀 좌표 구성
        Vector3 center = targetTilemap.GetCellCenterWorld(originCellPosition) + worldOffset; // 원점 셀 중심 월드 위치 계산

        Gizmos.color = Color.yellow; // 원점 표시 색상
        Gizmos.DrawWireSphere(center, 0.1f); // 원점 셀 중심 표시
    }
#endif
}