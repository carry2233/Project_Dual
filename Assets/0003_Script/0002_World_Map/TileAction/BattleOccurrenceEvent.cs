using System.Collections.Generic; // 리스트 사용
using UnityEngine; // Unity 기본 네임스페이스

/// <summary>
/// 전투 발생 이벤트 데이터 보관 스크립트
/// - 이벤트 실행 시 사용할 전투 데이터 저장
/// - BaseEventPanel에서 참조하여 사용
/// </summary>
public class BattleOccurrenceEvent : MonoBehaviour
{
    [Header("이벤트 설정")]

    public int eventID; // 이벤트 ID

    [Header("적 생성 수 설정")]

    public int minEnemySpawnCount; // 최소 적 생성 수
    public int maxEnemySpawnCount; // 최대 적 생성 수

    [Header("적 레벨 설정")]

    public int enemyLevelCorrectionValue; // 적 레벨 보정값

    [Header("적 생성 딜레이 설정")]

    public float minEnemySpawnDelay; // 최소 적 생성 딜레이
    public float maxEnemySpawnDelay; // 최대 적 생성 딜레이

    [Header("생성 가능한 적 목록")]

    public List<GlobalCharacterDefinition> spawnableEnemyList =
        new List<GlobalCharacterDefinition>(); // 생성 가능한 적 리스트

        [Header("전투 후 아이템 획득 설정")]
[SerializeField] private bool executeItemRewardAfterBattle; // 전투 후 아이템 획득 이벤트 실행 여부
[SerializeField] private ItemRewardEvent postBattleItemRewardEvent; // 전투 후 실행할 아이템 획득 이벤트

public bool ExecuteItemRewardAfterBattle => executeItemRewardAfterBattle; // 전투 후 아이템 획득 실행 여부 반환
public ItemRewardEvent PostBattleItemRewardEvent => postBattleItemRewardEvent; // 전투 후 아이템 획득 이벤트 반환

    [Header("이벤트 슬롯 프리팹")]

    public EventSlot eventSlotPrefab; // BaseEventPanel에 생성할 슬롯 프리팹

    [Header("전투 이후 이벤트 비주얼 설정")]
[SerializeField] private GameObject afterBattleEventVisualPrefab; // 전투 이후 탐색 UI에 생성할 이벤트 비주얼 프리팹

public GameObject AfterBattleEventVisualPrefab => afterBattleEventVisualPrefab; // 전투 이후 이벤트 비주얼 프리팹 반환


    public int EventID => eventID; // 이벤트 ID 반환
public int MinEnemySpawnCount => minEnemySpawnCount; // 최소 적 생성 수 반환
public int MaxEnemySpawnCount => maxEnemySpawnCount; // 최대 적 생성 수 반환
public int EnemyLevelCorrectionValue => enemyLevelCorrectionValue; // 적 레벨 보정값 반환
public float MinEnemySpawnDelay => minEnemySpawnDelay; // 최소 생성 딜레이 반환
public float MaxEnemySpawnDelay => maxEnemySpawnDelay; // 최대 생성 딜레이 반환
public IReadOnlyList<GlobalCharacterDefinition> SpawnableEnemyList => spawnableEnemyList; // 생성 가능한 적 목록 반환
public EventSlot EventSlotPrefab => eventSlotPrefab; // 이벤트 슬롯 프리팹 반환

public bool CanHandleEvent(int targetEventID) // 해당 이벤트 처리 가능 여부
{
    return eventID == targetEventID; // 이벤트 ID가 같으면 처리 가능
}
}