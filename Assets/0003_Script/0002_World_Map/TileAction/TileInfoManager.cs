using System.Collections.Generic; // List 사용
using UnityEngine; // Unity 기본 기능 사용

/// <summary>
/// 타일 정보 정의 목록을 관리하는 매니저입니다.
/// - TileInfoDefinition 리스트 관리
/// - DontDestroyOnLoad 적용
/// - 타일 ID 기준 정보 검색
/// </summary>
public class TileInfoManager : MonoBehaviour
{
    public static TileInfoManager Instance { get; private set; } // 싱글톤 인스턴스

    [Header("타일 정보 정의 목록")]
    [SerializeField] private List<TileInfoDefinition> tileInfoDefinitionList = new List<TileInfoDefinition>(); // 타일 정보 정의 목록

    private void Awake() // 시작 전 싱글톤 초기화
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // 중복 매니저 제거
            return;
        }

        Instance = this; // 인스턴스 등록
        DontDestroyOnLoad(gameObject); // 씬 전환 시 유지
    }

    public TileInfoDefinition GetTileInfoDefinition(int tileId) // 타일 ID로 정보 정의 검색
    {
        for (int i = 0; i < tileInfoDefinitionList.Count; i++)
        {
            TileInfoDefinition definition = tileInfoDefinitionList[i]; // 현재 타일 정보 정의

            if (definition == null)
                continue;

            if (definition.IsMatch(tileId))
                return definition;
        }

        return null;
    }

    public int GetRequiredSearchValue(int tileId) // 타일 ID 기준 필요 탐색 충족도 반환
    {
        TileInfoDefinition definition = GetTileInfoDefinition(tileId); // 타일 정보 검색

        if (definition == null)
            return 0;

        return definition.RequiredSearchValue;
    }
}