using System.Collections; // 코루틴 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.UI; // Button, Image 사용
using System.Collections.Generic; // List, HashSet 사용
using UnityEngine.EventSystems; // UI 위 마우스 감지
using UnityEngine.InputSystem; // 새 입력 시스템 마우스 입력 사용

/// <summary>
/// 타일 행동 UI를 관리하는 스크립트입니다.
/// - 플레이어 현재 타일 위치에 월드 공간 UI 배치
/// - 행동 표시 UI와 행동 버튼 UI 토글
/// - 탐색 UI 열기/닫기
/// - 탐색 타이머와 탐색 충족도 게이지 처리
/// - 탐색 충족도 완료 시 이동 버튼 활성화
/// </summary>
public class TileActionManager : MonoBehaviour
{

    [Header("외부 입력 잠금 참조")]
    [SerializeField] private TileSelectionManager tileSelectionManager; // 타일 선택 및 월드맵 이동 잠금 제어 스크립트

    [Header("플레이어 타일 정보 참조")]
    [SerializeField] private PlayerTileMembership playerTileMembership; // 플레이어 현재 타일 정보 참조

    [Header("타일 정보 관리자 참조")]
    [SerializeField] private TileInfoManager tileInfoManager; // 타일 정보 관리자 참조

    [Header("월드 공간 UI 위치 설정")]
    [SerializeField] private Transform parentPanel; // 월드 공간 캔버스 또는 부모 패널 Transform
    [SerializeField] private Vector3 panelWorldOffset = Vector3.zero; // 타일 위치 기준 UI 위치 보정값

    [Header("행동 표시 UI")]
    [SerializeField] private GameObject actionDisplayObject; // 행동 표시 오브젝트
    [SerializeField] private Button actionDisplayButton; // 행동 표시 버튼
    [SerializeField] private Button actionHideButton; // 행동 표시 해제 버튼

    [Header("행동 버튼 UI")]
    [SerializeField] private GameObject actionButtonParentObject; // 행동 버튼 부모 오브젝트
    [SerializeField] private Button searchUIOpenButton; // 탐색 UI 열기 버튼
    [SerializeField] private Button scoutButton; // 정찰 버튼
    [SerializeField] private Button moveExecuteButton; // 이동 실행 버튼

    [Header("탐색 UI")]
    [SerializeField] private GameObject searchUIObject; // 탐색 UI 오브젝트
    [SerializeField] private Button searchUICloseButton; // 탐색 UI 닫기 버튼
    [SerializeField] private Button searchExecuteButton; // 탐색 실행 버튼

    [Header("탐색 수치")]
    [SerializeField] private float searchDuration = 3f; // 탐색 실행 시간
    [SerializeField] private int currentSearchValue = 0; // 현재 탐색 충족도
    [SerializeField] private int searchIncreaseValue = 1; // 탐색 1회당 충족도 증가값

    [Header("탐색 게이지 UI")]
    [SerializeField] private GameObject searchTimerObject; // 탐색 중에만 표시할 타이머 오브젝트
    [SerializeField] private Image searchTimerImage; // 탐색 타이머 이미지
    [SerializeField] private Image currentSearchImage; // 현재 탐색 충족도 이미지


    [Header("=======================================================")]


    [Header("타일 배치 관리자 참조")]
[SerializeField] private HexTilePlacementManager hexTilePlacementManager; // 주변 1칸 타일 탐색용 배치 관리자

[Header("이동 선택 카메라 위치 설정")]
[SerializeField] private Vector3 selectedRigWorldOffset = Vector3.zero; // 이동 선택 시 카메라 루트 위치 보정값
[SerializeField] private Vector3 selectedCameraLocalPosition = new Vector3(0f, 6f, -6f); // 이동 선택 시 카메라 로컬 위치
[SerializeField] private Vector3 selectedCameraLocalEuler = new Vector3(35f, 0f, 0f); // 이동 선택 시 카메라 로컬 회전값

[Header("이동 타일 레이캐스트 설정")]
[SerializeField] private Camera inputCamera; // 마우스 위치를 Ray로 변환할 카메라
[SerializeField] private LayerMask tileLayerMask = ~0; // 타일 감지 레이어
[SerializeField] private float rayDistance = 500f; // Raycast 거리
[SerializeField] private bool blockMoveClickWhenPointerOverUI = true; // UI 위 클릭 시 이동 타일 선택 차단

[Header("타일 이동 UI")]
[SerializeField] private Transform worldCanvas1; // 이동 가능 타일 위에 표시할 월드 캔버스
[SerializeField] private Vector3 worldCanvas1Offset = Vector3.zero; // 월드 캔버스 위치 보정값
[SerializeField] private GameObject tileMoveDisplayUI; // 타일 이동 표시 UI
[SerializeField] private GameObject tileMoveExecuteUI; // 타일 이동 실행 UI
[SerializeField] private Button tileMoveButton; // 최종 타일 이동 버튼


[Header("=======================================================")]


[Header("이벤트 패널 참조")]
[SerializeField] private BaseEventPanel baseEventPanel; // 탐색 종료 후 이벤트 발생 처리를 담당할 이벤트 패널


[Header("=======================================================")]

[Header("기습 전투 설정")]
[Range(0f, 100f)]
[SerializeField] private float surpriseBattleChancePercent = 0f; // 타일 이동 시 기습 전투 발생 확률
[SerializeField] private List<BattleOccurrenceEvent> surpriseBattleEventList = new List<BattleOccurrenceEvent>(); // 기습 전투 후보 목록
[SerializeField] private EventInfoManager eventInfoManager; // 이벤트 정보 관리자 참조

[SerializeField] private float surpriseBattleDelayAfterCameraReturn = 1f; // 카메라 위치 복귀 실행 후 기습 전투까지 대기 시간

private Coroutine surpriseBattleDelayCoroutine; // 기습 전투 지연 실행 코루틴


private bool isMoveSelectionMode = false; // 현재 이동 타일 선택 모드 여부
private TilePrefab currentMoveBaseTile; // 이동 기준이 되는 현재 소속 타일
private TilePrefab hoveredMoveTile; // 현재 마우스가 올려진 이동 가능 타일
private TilePrefab selectedMoveTargetTile; // 이동 대상으로 확정된 타일
private readonly HashSet<TilePrefab> movableTileSet = new HashSet<TilePrefab>(); // 이동 가능한 주변 타일 목록

    private int maxSearchValue = 0; // 현재 타일의 최대 탐색 충족도
    private int cachedTilePrefabNumber = -1; // 마지막으로 반영한 타일 종류 ID
    private Coroutine searchCoroutine; // 현재 실행 중인 탐색 코루틴

    private void Awake() // 시작 전 버튼 이벤트 연결
    {
        if (actionDisplayButton != null)
            actionDisplayButton.onClick.AddListener(ShowActionButtons); // 행동 표시 버튼 이벤트 연결

        if (actionHideButton != null)
            actionHideButton.onClick.AddListener(HideActionButtons); // 행동 표시 해제 버튼 이벤트 연결

        if (searchUIOpenButton != null)
            searchUIOpenButton.onClick.AddListener(OpenSearchUI); // 탐색 UI 열기 버튼 이벤트 연결

        if (searchUICloseButton != null)
            searchUICloseButton.onClick.AddListener(CloseSearchUI); // 탐색 UI 닫기 버튼 이벤트 연결

        if (searchExecuteButton != null)
            searchExecuteButton.onClick.AddListener(StartSearch); // 탐색 실행 버튼 이벤트 연결
            
        if (moveExecuteButton != null)
            moveExecuteButton.onClick.AddListener(StartMoveSelectionMode); // 이동 선택 모드 시작 버튼 이벤트 연결

        if (tileMoveButton != null)
            tileMoveButton.onClick.AddListener(ExecuteTileMove); // 최종 타일 이동 버튼 이벤트 연결
    }

private void Start() // 씬 시작 시 초기화
{
    if (tileInfoManager == null)
        tileInfoManager = TileInfoManager.Instance; // 씬에 존재하는 타일 정보 관리자 참조

    if (baseEventPanel == null)
        baseEventPanel = FindFirstObjectByType<BaseEventPanel>(); // 이벤트 패널 자동 참조

    SetInitialUIState(); // 초기 UI 상태 설정
    RefreshPanelPosition(); // 플레이어 현재 타일 위치로 UI 이동
    RefreshTileSearchInfo(true); // 현재 타일 탐색 정보 반영
    UpdateSearchGauge(); // 탐색 게이지 갱신
    UpdateMoveButtonState(); // 이동 버튼 상태 갱신

    if (worldCanvas1 != null)
    {
        worldCanvas1.gameObject.SetActive(false); // 월드 캔버스 비활성화
    }

    if (tileMoveDisplayUI != null)
    {
        tileMoveDisplayUI.SetActive(true); // 이동 표시 UI 활성화
    }

    if (tileMoveExecuteUI != null)
    {
        tileMoveExecuteUI.SetActive(false); // 이동 실행 UI 비활성화
    }

    if (eventInfoManager == null)
    {
    eventInfoManager = EventInfoManager.Instance; // 씬 시작 시 이벤트 정보 관리자 참조
    }
}

    private void OnDestroy() // 오브젝트 제거 시 버튼 이벤트 해제
    {
        if (actionDisplayButton != null)
            actionDisplayButton.onClick.RemoveListener(ShowActionButtons); // 행동 표시 버튼 이벤트 해제

        if (actionHideButton != null)
            actionHideButton.onClick.RemoveListener(HideActionButtons); // 행동 표시 해제 버튼 이벤트 해제

        if (searchUIOpenButton != null)
            searchUIOpenButton.onClick.RemoveListener(OpenSearchUI); // 탐색 UI 열기 버튼 이벤트 해제

        if (searchUICloseButton != null)
            searchUICloseButton.onClick.RemoveListener(CloseSearchUI); // 탐색 UI 닫기 버튼 이벤트 해제

        if (searchExecuteButton != null)
            searchExecuteButton.onClick.RemoveListener(StartSearch); // 탐색 실행 버튼 이벤트 해제
        if (moveExecuteButton != null)
            moveExecuteButton.onClick.RemoveListener(StartMoveSelectionMode); // 이동 선택 모드 시작 버튼 이벤트 해제

        if (tileMoveButton != null)
            tileMoveButton.onClick.RemoveListener(ExecuteTileMove); // 최종 타일 이동 버튼 이벤트 해제
    }

private void LateUpdate() // 매 프레임 UI 위치 갱신
{
    if (isMoveSelectionMode == true)
    {
        UpdateMoveTileHoverAndClick(); // 이동 선택 중에는 마우스 타일 감지 처리
    }
    else
    {
        RefreshPanelPosition(); // 플레이어 현재 타일 위치에 UI 유지
    }

    RefreshTileSearchInfo(false); // 타일 ID 변경 여부 확인
}

private void SetInitialUIState() // 씬 시작 시 UI 초기 상태 설정
{
    //if (parentPanel != null)
    //    parentPanel.gameObject.SetActive(false); // 부모 패널 비활성화

    if (actionDisplayObject != null)
        actionDisplayObject.SetActive(true); // 행동 표시 오브젝트 활성화

    if (actionHideButton != null)
        actionHideButton.gameObject.SetActive(false); // 행동 표시 해제 버튼 비활성화

    if (actionButtonParentObject != null)
        actionButtonParentObject.SetActive(false); // 행동 버튼 부모 오브젝트 비활성화

    if (searchUIObject != null)
        searchUIObject.SetActive(false); // 탐색 UI 비활성화

    if (searchUICloseButton != null)
        searchUICloseButton.interactable = true; // 탐색 UI 닫기 버튼 활성화

    if (searchTimerObject != null)
        searchTimerObject.SetActive(false); // 탐색 타이머 오브젝트 비활성화

    if (searchTimerImage != null)
        searchTimerImage.fillAmount = 0f; // 탐색 타이머 이미지 초기화

    ApplySearchUILock(false); // 시작 시 외부 입력 잠금 해제
}

    private void RefreshPanelPosition() // 플레이어 현재 타일 위치로 UI 배치
    {
        if (playerTileMembership == null || parentPanel == null)
            return;

        if (playerTileMembership.CurrentTileNumber < 0)
            return;

        parentPanel.position = playerTileMembership.CurrentTileWorldPosition + panelWorldOffset; // 타일 위치에 UI 배치
    }

    private void RefreshTileSearchInfo(bool forceRefresh) // 현재 타일 ID 기준 탐색 정보 갱신
    {
        if (playerTileMembership == null)
            return;

        int currentTilePrefabNumber = playerTileMembership.CurrentTilePrefabNumber; // 현재 소속 타일 종류 ID

        if (currentTilePrefabNumber < 0)
            return;

        if (forceRefresh == false && cachedTilePrefabNumber == currentTilePrefabNumber)
            return;

        cachedTilePrefabNumber = currentTilePrefabNumber; // 현재 타일 ID 캐싱

        if (tileInfoManager == null)
            tileInfoManager = TileInfoManager.Instance; // 매니저 참조 보정

        if (tileInfoManager == null)
        {
            maxSearchValue = 0; // 매니저가 없으면 최대값 초기화
            currentSearchValue = 0; // 현재값 초기화
            UpdateSearchGauge(); // 게이지 갱신
            UpdateMoveButtonState(); // 이동 버튼 상태 갱신
            return;
        }

        maxSearchValue = tileInfoManager.GetRequiredSearchValue(currentTilePrefabNumber); // 타일 ID에 맞는 탐색 충족도 가져오기
        currentSearchValue = 0; // 새 타일 정보 반영 시 현재 탐색값 초기화

        UpdateSearchGauge(); // 탐색 게이지 갱신
        UpdateMoveButtonState(); // 이동 버튼 상태 갱신
    }

    public void ShowParentPanel() // 부모 패널 활성화
    {
        if (parentPanel != null)
            parentPanel.gameObject.SetActive(true); // 부모 패널 켜기

        HideActionButtons(); // 기본 상태로 되돌림
        CloseSearchUI(); // 탐색 UI 닫기
        RefreshPanelPosition(); // 위치 갱신
        RefreshTileSearchInfo(true); // 현재 타일 탐색 정보 강제 갱신
    }

    public void HideParentPanel() // 부모 패널 비활성화
    {
        if (parentPanel != null)
            parentPanel.gameObject.SetActive(false); // 부모 패널 끄기
    }

    private void ShowActionButtons() // 행동 버튼 UI 표시
    {
        if (actionDisplayObject != null)
            actionDisplayObject.SetActive(false); // 행동 표시 오브젝트 비활성화

        if (actionButtonParentObject != null)
            actionButtonParentObject.SetActive(true); // 행동 버튼 부모 오브젝트 활성화

        if (actionHideButton != null)
            actionHideButton.gameObject.SetActive(true); // 행동 표시 해제 버튼 활성화
    }

    private void HideActionButtons() // 행동 버튼 UI 숨김
    {
        if (actionButtonParentObject != null)
            actionButtonParentObject.SetActive(false); // 행동 버튼 부모 오브젝트 비활성화

        if (actionDisplayObject != null)
            actionDisplayObject.SetActive(true); // 행동 표시 오브젝트 활성화

        if (actionHideButton != null)
            actionHideButton.gameObject.SetActive(false); // 행동 표시 해제 버튼 비활성화
    }

private void OpenSearchUI() // 탐색 UI 열기
{
    if (searchUIObject != null)
        searchUIObject.SetActive(true); // 탐색 UI 활성화

    ApplySearchUILock(true); // 탐색 UI 열림 상태로 외부 입력 잠금

    UpdateSearchGauge(); // 탐색 게이지 갱신
    UpdateMoveButtonState(); // 이동 버튼 상태 갱신

    if (baseEventPanel != null)
    {
        baseEventPanel.TryShowReturnedBattleEventResult(); // 전투 복귀 결과 이벤트 UI 표시 시도
    }
}

private void CloseSearchUI() // 탐색 UI 닫기
{
    if (searchCoroutine != null)
        return; // 탐색 중에는 닫기 차단

    if (searchUIObject != null)
        searchUIObject.SetActive(false); // 탐색 UI 비활성화

    ApplySearchUILock(false); // 탐색 UI 닫힘 상태로 외부 입력 잠금 해제
}

private void StartSearch() // 탐색 시작
{
    if (searchCoroutine != null)
        return; // 이미 탐색 중이면 중복 실행 방지

    if (maxSearchValue <= 0)
        return; // 최대 탐색 충족도가 없으면 실행하지 않음

    if (baseEventPanel != null)
        baseEventPanel.HideEventUI(); // 탐색 시작 시 이전 이벤트 UI 비활성화

    searchCoroutine = StartCoroutine(SearchRoutine()); // 탐색 코루틴 시작
}

private IEnumerator SearchRoutine() // 탐색 진행 코루틴
{
    float elapsedTime = 0f; // 경과 시간

    if (searchUICloseButton != null)
        searchUICloseButton.interactable = false; // 탐색 중 닫기 버튼 비활성화

    if (searchTimerObject != null)
        searchTimerObject.SetActive(true); // 타이머 오브젝트 활성화

    if (searchTimerImage != null)
        searchTimerImage.fillAmount = 1f; // 타이머를 가득 찬 상태로 시작

    while (elapsedTime < searchDuration)
    {
        elapsedTime += Time.deltaTime; // 경과 시간 누적

        float normalizedTime = Mathf.Clamp01(elapsedTime / searchDuration); // 진행률 계산

        if (searchTimerImage != null)
            searchTimerImage.fillAmount = 1f - normalizedTime; // 타이머 이미지를 1에서 0으로 감소

        yield return null;
    }

    currentSearchValue += searchIncreaseValue; // 탐색 완료 시 현재 탐색 충족도 증가
    currentSearchValue = Mathf.Clamp(currentSearchValue, 0, maxSearchValue); // 최대값 초과 방지

    if (searchTimerImage != null)
        searchTimerImage.fillAmount = 0f; // 타이머 이미지 비우기

    if (searchTimerObject != null)
        searchTimerObject.SetActive(false); // 타이머 오브젝트 비활성화

    if (searchUICloseButton != null)
        searchUICloseButton.interactable = true; // 탐색 종료 후 닫기 버튼 활성화

        UpdateSearchGauge(); // 현재 탐색 게이지 갱신
        UpdateMoveButtonState(); // 이동 버튼 상태 갱신

        if (baseEventPanel != null)
            baseEventPanel.TryTriggerRandomEvent(); // 탐색 종료 시 이벤트 발생 시도

    searchCoroutine = null; // 탐색 코루틴 참조 초기화
}

    private void UpdateSearchGauge() // 현재 탐색 충족도 게이지 갱신
    {
        if (currentSearchImage == null)
            return;

        if (maxSearchValue <= 0)
        {
            currentSearchImage.fillAmount = 0f; // 최대값이 없으면 게이지 비우기
            return;
        }

        currentSearchImage.fillAmount = Mathf.Clamp01((float)currentSearchValue / maxSearchValue); // 현재/최대 비율 반영
    }

    private void UpdateMoveButtonState() // 이동 실행 버튼 상태 갱신
    {
        if (moveExecuteButton == null)
            return;

        moveExecuteButton.interactable = maxSearchValue > 0 && currentSearchValue >= maxSearchValue; // 탐색 완료 시 이동 가능
    }
    
    private void ApplySearchUILock(bool isOpen) // 탐색 UI 열림 상태에 따른 외부 입력 잠금 적용
    {
        if (tileSelectionManager != null)
            tileSelectionManager.SetTileActionUIOpen(isOpen); // 타일 선택/월드맵 이동 잠금 상태 전달
    }

private void StartMoveSelectionMode() // 이동 선택 모드 시작
{
    if (maxSearchValue <= 0 || currentSearchValue < maxSearchValue)
    {
        return; // 탐색 충족도가 부족하면 이동 선택 불가
    }

    if (playerTileMembership == null || tileSelectionManager == null || hexTilePlacementManager == null)
    {
        Debug.LogWarning("[TileActionManager] 이동 선택에 필요한 참조가 비어 있습니다.", this); // 참조 누락 경고
        return;
    }

    currentMoveBaseTile = hexTilePlacementManager.GetTileByTileNumber(playerTileMembership.CurrentTileNumber); // 현재 소속 타일 찾기

    if (currentMoveBaseTile == null)
    {
        Debug.LogWarning("[TileActionManager] 현재 플레이어 소속 타일을 찾지 못했습니다.", this); // 현재 타일 없음 경고
        return;
    }

    if (parentPanel != null)
        parentPanel.gameObject.SetActive(false); // 이동 타일 선택 시작 시 기존 타일 행동 패널 비활성화

    if (searchUIObject != null)
        searchUIObject.SetActive(false); // 탐색 UI 비활성화

    ApplySearchUILock(false); // 탐색 UI 잠금 해제

    if (tileMoveDisplayUI != null)
        tileMoveDisplayUI.SetActive(true); // 이동 표시 UI 활성화

    if (tileMoveExecuteUI != null)
        tileMoveExecuteUI.SetActive(false); // 이동 실행 UI 비활성화

    if (worldCanvas1 != null)
        worldCanvas1.gameObject.SetActive(false); // 이동 가능 타일 표시 캔버스 비활성화

    movableTileSet.Clear(); // 기존 이동 가능 타일 목록 초기화
    List<TilePrefab> neighborTiles = hexTilePlacementManager.GetNeighborTilePrefabs(currentMoveBaseTile); // 주변 1칸 타일 가져오기

    for (int i = 0; i < neighborTiles.Count; i++)
    {
        if (neighborTiles[i] != null)
        {
            movableTileSet.Add(neighborTiles[i]); // 이동 가능 타일 등록
        }
    }

    hoveredMoveTile = null; // hover 타일 초기화
    selectedMoveTargetTile = null; // 선택 타일 초기화
    isMoveSelectionMode = true; // 이동 선택 모드 활성화

    tileSelectionManager.SelectTileWithCustomView(
        currentMoveBaseTile,
        selectedRigWorldOffset,
        selectedCameraLocalPosition,
        selectedCameraLocalEuler,
        false
    ); // 현재 소속 타일을 이동 선택용 위치값으로 선택하고 선택해제 버튼은 숨김
}

private void UpdateMoveTileHoverAndClick() // 이동 가능 타일 hover와 클릭 처리
{
    if (selectedMoveTargetTile != null)
    {
        return; // 이동 대상이 이미 확정되면 hover 갱신 중지
    }

    TilePrefab tileUnderMouse = GetMoveTileUnderMouse(); // 마우스 아래 타일 감지

    if (tileUnderMouse != null && movableTileSet.Contains(tileUnderMouse) == true)
    {
        hoveredMoveTile = tileUnderMouse; // 현재 hover 타일 저장

        if (worldCanvas1 != null)
        {
            worldCanvas1.gameObject.SetActive(true); // 월드 캔버스 활성화
            worldCanvas1.position = hoveredMoveTile.transform.position + worldCanvas1Offset; // hover 타일 위치로 이동
        }
    }
    else
    {
        hoveredMoveTile = null; // hover 타일 초기화

        if (worldCanvas1 != null)
            worldCanvas1.gameObject.SetActive(false); // 이동 가능 타일이 아니면 캔버스 숨김
    }

    if (Mouse.current == null)
    {
        return; // 마우스가 없으면 종료
    }

    if (Mouse.current.leftButton.wasPressedThisFrame == false)
    {
        return; // 좌클릭이 아니면 종료
    }

    if (blockMoveClickWhenPointerOverUI == true && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
    {
        return; // UI 위 클릭이면 이동 타일 선택 차단
    }

    if (hoveredMoveTile == null)
    {
        return; // 이동 가능 타일 위가 아니면 종료
    }

    SelectMoveTargetTile(hoveredMoveTile); // 이동 대상 타일 확정
}

private TilePrefab GetMoveTileUnderMouse() // 마우스 위치의 타일 감지
{
    if (inputCamera == null)
    {
        inputCamera = Camera.main; // 입력 카메라가 없으면 메인 카메라 사용
    }

    if (inputCamera == null || Mouse.current == null)
    {
        return null; // 카메라 또는 마우스가 없으면 실패
    }

    Ray ray = inputCamera.ScreenPointToRay(Mouse.current.position.ReadValue()); // 마우스 위치를 Ray로 변환

    if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, tileLayerMask) == false)
    {
        return null; // 아무것도 맞지 않으면 실패
    }

    TilePrefab tilePrefab = hit.collider.GetComponentInParent<TilePrefab>(); // 맞은 콜라이더의 타일 탐색

    if (tilePrefab == null)
    {
        return null; // 타일이 아니면 실패
    }

    if (tilePrefab.IsTileRaycastCollider(hit.collider) == false)
    {
        return null; // TilePrefab에 등록된 콜라이더가 아니면 실패
    }

    return tilePrefab; // 감지된 타일 반환
}

private void SelectMoveTargetTile(TilePrefab targetTile) // 이동 대상 타일 선택
{
    if (targetTile == null)
    {
        return; // 대상 타일이 없으면 종료
    }

    selectedMoveTargetTile = targetTile; // 이동 대상 저장
    hoveredMoveTile = targetTile; // hover 타일도 선택 대상 기준으로 고정

    if (worldCanvas1 != null)
    {
        worldCanvas1.gameObject.SetActive(true); // 월드 캔버스 활성화 유지
        worldCanvas1.position = selectedMoveTargetTile.transform.position + worldCanvas1Offset; // 선택한 타일 위치에 고정
    }

    if (tileMoveDisplayUI != null)
        tileMoveDisplayUI.SetActive(false); // 이동 표시 UI 비활성화

    if (tileMoveExecuteUI != null)
        tileMoveExecuteUI.SetActive(true); // 이동 실행 UI 활성화
}

private void ExecuteTileMove() // 확정된 타일로 플레이어 이동 실행
{
    if (selectedMoveTargetTile == null || playerTileMembership == null)
    {
        return; // 이동 대상 또는 플레이어 소속 정보가 없으면 종료
    }

    playerTileMembership.SetCurrentTile(selectedMoveTargetTile, selectedMoveTargetTile.transform); // 현재 플레이어 소속 타일 갱신

    if (tileSelectionManager != null)
    {
        tileSelectionManager.ClearSelection(); // 이동 완료 후 현재 선택 해제 실행
    }

    ApplySearchUILock(false); // 이동 완료 후 탐색 UI 잠금 해제

    isMoveSelectionMode = false; // 이동 선택 모드 종료
    currentMoveBaseTile = null; // 기준 타일 초기화
    hoveredMoveTile = null; // hover 타일 초기화
    selectedMoveTargetTile = null; // 이동 대상 초기화
    movableTileSet.Clear(); // 이동 가능 타일 목록 초기화

    if (worldCanvas1 != null)
        worldCanvas1.gameObject.SetActive(false); // 이동 완료 후 월드 캔버스 비활성화

    if (tileMoveDisplayUI != null)
        tileMoveDisplayUI.SetActive(false); // 이동 표시 UI 비활성화

    if (tileMoveExecuteUI != null)
        tileMoveExecuteUI.SetActive(false); // 이동 실행 UI 비활성화

    if (parentPanel != null)
        parentPanel.gameObject.SetActive(true); // 이동 실행 후 기존 타일 행동 패널 다시 활성화

    RefreshPanelPosition(); // parentPanel 위치 갱신
    RefreshTileSearchInfo(true); // 새 타일 기준 탐색 정보 갱신
    UpdateSearchGauge(); // 탐색 게이지 갱신
    UpdateMoveButtonState(); // 이동 버튼 상태 갱신
    TryStartDelayedSurpriseBattle(); // 카메라 위치 복귀 실행 후 지연 기습 전투 시도
}

private void TryStartDelayedSurpriseBattle() // 지연 기습 전투 발생 시도
{
    if (baseEventPanel == null)
        baseEventPanel = FindFirstObjectByType<BaseEventPanel>(); // 이벤트 패널 재참조

    if (baseEventPanel == null)
        return; // 이벤트 패널이 없으면 종료

    if (surpriseBattleEventList == null || surpriseBattleEventList.Count <= 0)
        return; // 기습 전투 후보가 없으면 종료

    float randomValue = Random.Range(0f, 100f); // 기습 전투 확률 판정값

    if (randomValue > surpriseBattleChancePercent)
        return; // 확률 실패 시 종료

    BattleOccurrenceEvent selectedBattleEvent = GetRandomSurpriseBattleEvent(); // 기습 전투 이벤트 랜덤 선택

    if (selectedBattleEvent == null)
        return; // 선택 실패 시 종료

    if (surpriseBattleDelayCoroutine != null)
        StopCoroutine(surpriseBattleDelayCoroutine); // 기존 지연 실행 중이면 중단

    surpriseBattleDelayCoroutine = StartCoroutine(
        DelayedSurpriseBattleRoutine(selectedBattleEvent)); // 지연 후 기습 전투 실행
}

private BattleOccurrenceEvent GetRandomSurpriseBattleEvent() // 기습 전투 후보 중 랜덤 선택
{
    List<BattleOccurrenceEvent> validBattleEvents = new List<BattleOccurrenceEvent>(); // 유효한 전투 이벤트 목록

    for (int i = 0; i < surpriseBattleEventList.Count; i++)
    {
        BattleOccurrenceEvent battleEvent = surpriseBattleEventList[i]; // 현재 후보 이벤트

        if (battleEvent == null)
            continue; // 비어 있으면 건너뜀

        validBattleEvents.Add(battleEvent); // 유효 후보 추가
    }

    if (validBattleEvents.Count <= 0)
        return null; // 유효 후보가 없으면 null 반환

    int randomIndex = Random.Range(0, validBattleEvents.Count); // 랜덤 인덱스 선택
    return validBattleEvents[randomIndex]; // 선택된 전투 이벤트 반환
}

private IEnumerator DelayedSurpriseBattleRoutine(BattleOccurrenceEvent selectedBattleEvent) // 설정 시간 후 기습 전투 실행
{
    if (surpriseBattleDelayAfterCameraReturn > 0f)
    {
        yield return new WaitForSeconds(surpriseBattleDelayAfterCameraReturn); // 카메라 복귀 실행 후 대기
    }

    surpriseBattleDelayCoroutine = null; // 코루틴 참조 초기화

    if (selectedBattleEvent == null)
        yield break; // 선택된 전투 이벤트가 없으면 종료

    if (baseEventPanel == null)
        baseEventPanel = FindFirstObjectByType<BaseEventPanel>(); // 이벤트 패널 재참조

    if (baseEventPanel == null)
        yield break; // 이벤트 패널이 없으면 종료

    baseEventPanel.StartBattleEventDirectly(selectedBattleEvent); // 선택된 전투 이벤트 실행
}








}