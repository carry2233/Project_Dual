using System.Collections; // 코루틴 사용
using System.Collections.Generic; // 리스트 사용
using UnityEngine; // Unity 기본 네임스페이스

/// <summary>
/// 적 생성 관리자
/// - SaveStorage에서 전달받은 데이터를 기반으로 적 생성
/// - 생성마다 랜덤 딜레이 적용
/// - 생성된 적을 GlobalCharacterManager에 등록
/// </summary>
public class EnemySpawnManager : MonoBehaviour
{
    [Header("적 생성 위치 목록")]

    public List<Transform> enemySpawnPointList =
        new List<Transform>(); // 적 생성 위치 리스트

    [Header("현재 전투 데이터")]

    public int currentEventID; // 현재 이벤트 ID

    public int currentMinEnemySpawnCount; // 최소 적 생성 수
    public int currentMaxEnemySpawnCount; // 최대 적 생성 수

    public int currentEnemyLevelCorrection; // 적 레벨 보정값

    public float currentMinSpawnDelay; // 최소 생성 딜레이
    public float currentMaxSpawnDelay; // 최대 생성 딜레이

    public List<GlobalCharacterDefinition> currentSpawnableEnemyList =
        new List<GlobalCharacterDefinition>(); // 생성 가능한 적 리스트

        [Header("계산된 적 레벨")]
public int currentCalculatedEnemyLevel = 1; // 이번 전투에서 적에게 적용할 최종 레벨
public bool isEnemyLevelCalculated; // 적 적용 레벨 계산 완료 여부

    [Header("생성 상태")]

    public bool isSpawning; // 생성 진행 여부

    public int currentSpawnedCount; // 현재 생성된 수
    public int targetSpawnCount; // 목표 생성 수

    [Header("외부 참조")]

    public FriendlyCharacterManager friendlyCharacterManager; // 아군 관리자 참조

    public GlobalCharacterManager globalCharacterManager; // 글로벌 캐릭터 관리자 참조

private void Start() // 시작 시 전투 데이터 수신 후 적 생성 준비
{
    StartCoroutine(InitializeAndStartEnemySpawnCoroutine()); // 초기화 완료 후 적 생성 시작
}

    /// <summary>
    /// 적 생성 시작
    /// </summary>
    public void StartEnemySpawn()
    {
        if (isSpawning)
        {
            return;
        }

        if (currentSpawnableEnemyList.Count <= 0)
        {
            Debug.LogWarning("생성 가능한 적 리스트가 비어있음");
            return;
        }

        if (enemySpawnPointList.Count <= 0)
        {
            Debug.LogWarning("적 생성 위치 리스트가 비어있음");
            return;
        }

        targetSpawnCount =
            Random.Range(
                currentMinEnemySpawnCount,
                currentMaxEnemySpawnCount + 1);

        currentSpawnedCount = 0;

        isSpawning = true;

        StartCoroutine(SpawnEnemyCoroutine());
    }

    /// <summary>
    /// 적 생성 코루틴
    /// </summary>
    private IEnumerator SpawnEnemyCoroutine()
    {
        while (currentSpawnedCount < targetSpawnCount)
        {
            SpawnSingleEnemy();

            currentSpawnedCount++;

            if (currentSpawnedCount >= targetSpawnCount)
            {
                break;
            }

            float randomDelay =
                Random.Range(
                    currentMinSpawnDelay,
                    currentMaxSpawnDelay);

            yield return new WaitForSeconds(randomDelay);
        }

        isSpawning = false;
    }

    /// <summary>
    /// 적 1명 생성
    /// </summary>
private void SpawnSingleEnemy() // 적 1명 생성
{
    GlobalCharacterDefinition randomEnemyDefinition =
        currentSpawnableEnemyList[
            Random.Range(0, currentSpawnableEnemyList.Count)]; // 랜덤 적 정의 선택

    if (randomEnemyDefinition == null)
    {
        return; // 정의가 없으면 종료
    }

    if (randomEnemyDefinition.InGameCharacterPrefab == null)
    {
        Debug.LogWarning("인게임 캐릭터 프리팹이 비어있음"); // 프리팹 누락 경고
        return;
    }

    Transform randomSpawnPoint =
        enemySpawnPointList[
            Random.Range(0, enemySpawnPointList.Count)]; // 랜덤 생성 위치 선택

    GameObject spawnedEnemy =
        Instantiate(
            randomEnemyDefinition.InGameCharacterPrefab,
            randomSpawnPoint.position,
            Quaternion.identity); // 적 생성

    ApplyEnemyLevel(spawnedEnemy); // 적 레벨 적용

    RegisterSpawnedEnemy(spawnedEnemy); // 글로벌 캐릭터 매니저 등록
}

    /// <summary>
    /// 적 레벨 적용
    /// </summary>
    /// <param name="enemyObject">생성된 적 오브젝트</param>
private void ApplyEnemyLevel(GameObject enemyObject) // 적 레벨 적용
{
    if (enemyObject == null)
    {
        return; // 대상이 없으면 종료
    }

    if (!isEnemyLevelCalculated)
    {
        CalculateCurrentEnemyLevel(); // 혹시 계산 전이면 즉시 계산
    }

    CharacterStatSystem statSystem = enemyObject.GetComponent<CharacterStatSystem>(); // 스탯 시스템 참조

    if (statSystem == null)
    {
        return; // 스탯 시스템이 없으면 종료
    }

    statSystem.SetLevelStats(currentCalculatedEnemyLevel); // 계산 완료된 적 레벨 적용
}

    /// <summary>
    /// 생성된 적 등록
    /// </summary>
    /// <param name="enemyObject">생성된 적 오브젝트</param>
private void RegisterSpawnedEnemy(GameObject enemyObject) // 생성된 적 등록
{
    if (enemyObject == null)
    {
        return; // 대상이 없으면 종료
    }

    if (globalCharacterManager == null)
    {
        globalCharacterManager = GlobalCharacterManager.Instance; // 글로벌 캐릭터 매니저 재참조
    }

    if (globalCharacterManager == null)
    {
        return; // 매니저가 없으면 종료
    }

    CharacterDuelAI duelAI = enemyObject.GetComponent<CharacterDuelAI>(); // 적 캐릭터 AI 참조

    if (duelAI == null)
    {
        return; // 캐릭터 AI가 없으면 종료
    }

    globalCharacterManager.RegisterCharacter(duelAI, GlobalCharacterManager.CharacterFactionType.Enemy); // 적군 타입으로 등록
}

    /// <summary>
    /// 전투 데이터 초기화
    /// </summary>
    public void ClearCurrentBattleData()
    {
        currentEventID = 0;

        currentMinEnemySpawnCount = 0;
        currentMaxEnemySpawnCount = 0;

        currentEnemyLevelCorrection = 0;

        currentMinSpawnDelay = 0f;
        currentMaxSpawnDelay = 0f;

        currentSpawnableEnemyList.Clear();
    }

    public void ReceiveBattleEventData(SaveStorage.BattleEventRuntimeData battleData) // SaveStorage에서 전투 데이터 수신
{
    if (battleData == null)
    {
        return; // 데이터가 없으면 종료
    }

    currentEventID = battleData.eventID; // 이벤트 ID 저장
    currentMinEnemySpawnCount = battleData.minEnemySpawnCount; // 최소 생성 수 저장
    currentMaxEnemySpawnCount = battleData.maxEnemySpawnCount; // 최대 생성 수 저장
    currentEnemyLevelCorrection = battleData.enemyLevelCorrectionValue; // 레벨 보정값 저장
    currentMinSpawnDelay = battleData.minEnemySpawnDelay; // 최소 딜레이 저장
    currentMaxSpawnDelay = battleData.maxEnemySpawnDelay; // 최대 딜레이 저장
    currentSpawnableEnemyList = new List<GlobalCharacterDefinition>(battleData.spawnableEnemyList); // 적 목록 복사
}

private IEnumerator InitializeAndStartEnemySpawnCoroutine() // 적 생성 전 필요한 데이터와 레벨 계산을 먼저 완료
{
    ResolveReferences(); // 외부 참조 보정

    if (SaveStorage.Instance != null)
    {
        SaveStorage.Instance.SendBattleEventRuntimeDataToEnemySpawnManager(this); // 저장소에서 전투 데이터 수신
    }

    yield return WaitUntilFriendlyBattleSetupCompleted(); // 아군 평균 레벨 계산 완료까지 대기

    CalculateCurrentEnemyLevel(); // 이번 전투 적 적용 레벨 계산

    StartEnemySpawn(); // 레벨 계산 완료 후 적 생성 시작
}

private void ResolveReferences() // 외부 참조 자동 보정
{
    if (friendlyCharacterManager == null)
    {
        friendlyCharacterManager = FindFirstObjectByType<FriendlyCharacterManager>(); // 아군 매니저 자동 탐색
    }

    if (globalCharacterManager == null)
    {
        globalCharacterManager = GlobalCharacterManager.Instance; // 글로벌 캐릭터 매니저 참조
    }
}

private IEnumerator WaitUntilFriendlyBattleSetupCompleted() // 아군 전투 준비 완료까지 대기
{
    while (friendlyCharacterManager == null)
    {
        friendlyCharacterManager = FindFirstObjectByType<FriendlyCharacterManager>(); // 아군 매니저 재탐색
        yield return null; // 다음 프레임까지 대기
    }

    while (!friendlyCharacterManager.IsFriendlyBattleSetupCompleted)
    {
        yield return null; // 아군 평균 레벨 계산 완료까지 대기
    }
}

private void CalculateCurrentEnemyLevel() // 이번 전투에서 사용할 최종 적 레벨 계산
{
    int friendlyAverageLevel = 1; // 기본 아군 평균 레벨

    if (friendlyCharacterManager != null)
    {
        friendlyCharacterManager.RefreshFriendlyAverageLevel(); // 현재 아군 기준 평균 레벨 재계산
        friendlyAverageLevel = friendlyCharacterManager.FriendlyAverageLevel; // 아군 평균 레벨 참조
    }

    currentCalculatedEnemyLevel = Mathf.Max(1, friendlyAverageLevel + currentEnemyLevelCorrection); // 최종 적 레벨 계산
    isEnemyLevelCalculated = true; // 적 레벨 계산 완료 표시
}













}