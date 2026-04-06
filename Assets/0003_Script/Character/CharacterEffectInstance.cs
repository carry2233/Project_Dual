using UnityEngine;

public class CharacterEffectInstance : MonoBehaviour
{
    [Header("현재 이펙트 재생 상태")]
    [SerializeField] private float currentElapsedTime; // 현재 생성 후 경과 시간
    [SerializeField] private float currentLifetime = 1f; // 현재 유지 시간
    [SerializeField] private float currentStartTimelineTime; // 생성 직후 시작할 타임라인 시간
    [SerializeField] private float currentPlaySpeedMultiplier = 1f; // 현재 재생속도 배율
    [SerializeField] private bool isInitialized; // 초기화 완료 여부

    private ParticleSystem[] cachedParticleSystemArray; // 이펙트 오브젝트 하위의 파티클 시스템 캐싱

    private void Awake() // 시작 시 파티클 시스템 참조 캐싱
    {
        CacheParticleSystems(); // 현재 오브젝트와 자식의 파티클 시스템 캐싱
    }

    private void Update() // 유지 시간 경과 후 이펙트 오브젝트 삭제
    {
        if (!isInitialized)
        {
            return; // 초기화 전이면 종료
        }

        currentElapsedTime += Time.deltaTime; // 경과 시간 누적

        if (currentElapsedTime < currentLifetime)
        {
            return; // 아직 유지 시간이 지나지 않았으면 종료
        }

        Destroy(gameObject); // 유지 시간이 끝났으면 오브젝트 삭제
    }

    public void InitializeEffectInstance(
        float startTimelineTime, // 생성 직후 이 시점까지 재생된 상태로 시작할 시간
        float lifetime, // 유지 시간
        float playSpeedMultiplier) // 파티클 재생속도 배율
    {
        currentElapsedTime = 0f; // 경과 시간 초기화
        currentLifetime = Mathf.Max(0f, lifetime); // 유지 시간 보정 후 저장
        currentStartTimelineTime = Mathf.Max(0f, startTimelineTime); // 시작 타임라인 시간 보정 후 저장
        currentPlaySpeedMultiplier = Mathf.Max(0f, playSpeedMultiplier); // 재생속도 배율 보정 후 저장
        isInitialized = true; // 초기화 완료 상태 저장

        if (cachedParticleSystemArray == null || cachedParticleSystemArray.Length == 0)
        {
            CacheParticleSystems(); // 아직 캐싱되지 않았으면 다시 캐싱
        }

        ApplyParticlePlaybackState(); // 파티클 재생 상태 적용
    }

    private void CacheParticleSystems() // 현재 오브젝트와 자식의 파티클 시스템 캐싱
    {
        cachedParticleSystemArray = GetComponentsInChildren<ParticleSystem>(true); // 비활성 포함 전체 파티클 시스템 수집
    }

    private void ApplyParticlePlaybackState() // 파티클 시스템들에 타임라인과 속도 배율 적용
    {
        if (cachedParticleSystemArray == null || cachedParticleSystemArray.Length == 0)
        {
            return; // 파티클 시스템이 없으면 종료
        }

        for (int i = 0; i < cachedParticleSystemArray.Length; i++)
        {
            ParticleSystem targetParticleSystem = cachedParticleSystemArray[i]; // 현재 파티클 시스템 참조

            if (targetParticleSystem == null)
            {
                continue; // 비어 있으면 건너뜀
            }

            ParticleSystem.MainModule mainModule = targetParticleSystem.main; // Main 모듈 참조
            mainModule.simulationSpeed = currentPlaySpeedMultiplier; // 재생속도 배율 적용

            targetParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // 기존 재생 상태 정지 및 초기화
            targetParticleSystem.Simulate(currentStartTimelineTime, true, true, true); // 지정 시간만큼 재생된 상태로 시뮬레이션
            targetParticleSystem.Play(true); // 현재 상태부터 실제 재생 시작
        }
    }
}