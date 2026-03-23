using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AudioSource 풀링 관리자
/// - AudioSource 오브젝트를 미리 생성 후 재사용
/// </summary>
public class AudioSourcePool : MonoBehaviour
{
    public static AudioSourcePool Instance { get; private set; } // 싱글톤 인스턴스

    [Header("풀 설정")]
    [SerializeField] private int initialPoolSize = 16; // 초기 생성 개수
    [SerializeField] private bool expandable = true; // 부족할 때 추가 생성 여부

    [Header("풀 루트")]
    [SerializeField] private Transform poolRoot; // 풀 오브젝트들을 담을 루트

    private readonly Queue<AudioSource> availableQueue = new Queue<AudioSource>(); // 사용 가능한 AudioSource 큐
    private readonly List<AudioSource> allSources = new List<AudioSource>(); // 전체 AudioSource 목록

    private void Awake() // 초기 풀 생성
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // 중복 인스턴스 제거
            return;
        }

        Instance = this; // 싱글톤 인스턴스 저장

        if (poolRoot == null)
        {
            poolRoot = transform; // 루트가 비어 있으면 자기 자신 사용
        }

        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewAudioSource(); // 초기 AudioSource 생성
        }
    }

    public AudioSource GetAudioSource() // 사용할 AudioSource 반환
    {
        if (availableQueue.Count > 0)
        {
            AudioSource source = availableQueue.Dequeue(); // 큐에서 하나 꺼냄
            source.gameObject.SetActive(true); // 오브젝트 활성화
            return source; // AudioSource 반환
        }

        if (!expandable)
        {
            return null; // 확장 불가이면 null 반환
        }

        return CreateNewAudioSource(); // 새 AudioSource 생성 후 반환
    }

    public void ReturnAudioSource(AudioSource source) // 사용이 끝난 AudioSource 반환
    {
        if (source == null)
        {
            return; // 대상이 없으면 종료
        }

        source.Stop(); // 재생 정지
        source.clip = null; // 클립 초기화
        source.volume = 1f; // 볼륨 초기화
        source.loop = false; // 루프 초기화
        source.transform.SetParent(poolRoot, false); // 풀 루트로 복귀
        source.transform.localPosition = Vector3.zero; // 위치 초기화
        source.gameObject.SetActive(false); // 오브젝트 비활성화

        if (!availableQueue.Contains(source))
        {
            availableQueue.Enqueue(source); // 큐에 반환
        }
    }

    private AudioSource CreateNewAudioSource() // 새 AudioSource 오브젝트 생성
    {
        GameObject audioObject = new GameObject($"PooledAudioSource_{allSources.Count}"); // 오브젝트 생성
        audioObject.transform.SetParent(poolRoot, false); // 풀 루트 하위로 배치

        AudioSource source = audioObject.AddComponent<AudioSource>(); // AudioSource 추가
        source.playOnAwake = false; // 자동 재생 비활성화
        source.spatialBlend = 0f; // 2D 사운드로 사용
        source.loop = false; // 기본 루프 비활성화

        audioObject.SetActive(false); // 기본 비활성화 상태로 시작

        allSources.Add(source); // 전체 목록 등록
        availableQueue.Enqueue(source); // 사용 가능 큐 등록

        return source; // 생성된 AudioSource 반환
    }
}