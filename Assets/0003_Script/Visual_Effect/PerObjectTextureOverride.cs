using UnityEngine;

/// <summary>
/// 같은 머티리얼을 공유하더라도 오브젝트마다 다른 이미지를 보이게 하는 스크립트
/// - MaterialPropertyBlock을 사용하여 렌더러 단위로 텍스처 속성을 덮어씀
/// - 머티리얼 자체를 복제하지 않음
/// - Texture2D 또는 Sprite 중 하나를 선택해서 적용 가능
/// </summary>
[RequireComponent(typeof(Renderer))]
public class PerObjectTextureOverride : MonoBehaviour
{
    /// <summary>
    /// 사용할 이미지 소스 종류
    /// </summary>
    public enum ImageSourceType
    {
        Texture2D, // Texture2D 직접 사용
        Sprite     // Sprite의 texture 사용
    }

    [Header("이미지 설정")]
    [SerializeField] private ImageSourceType imageSourceType = ImageSourceType.Texture2D; // 사용할 이미지 소스 종류
    [SerializeField] private Texture2D overrideTexture; // 오브젝트에 개별 적용할 텍스처
    [SerializeField] private Sprite overrideSprite; // 오브젝트에 개별 적용할 스프라이트

    [Header("텍스처 슬롯 이름 설정")]
    [SerializeField] private string texturePropertyName = "_BaseMap"; // 셰이더의 텍스처 속성 이름(URP는 보통 _BaseMap)

    [Header("적용 설정")]
    [SerializeField] private bool applyOnStart = true; // 시작 시 자동 적용 여부
    [SerializeField] private bool applyEveryFrame = false; // 매 프레임 재적용 여부(실시간 교체가 필요할 때만 사용)

    [Header("색상 설정")]
    [SerializeField] private string colorPropertyName = "_BaseColor"; // 셰이더의 색상 속성 이름
    [SerializeField] private Color overrideColor = Color.white; // 오브젝트에 개별 적용할 색상

private int colorPropertyId; // 색상 속성 ID 캐싱

    private Renderer cachedRenderer; // Renderer 참조 캐싱
    private MaterialPropertyBlock propertyBlock; // 머티리얼 속성 블록
    private int texturePropertyId; // 텍스처 속성 ID 캐싱

private void Awake() // 초기 참조 설정
{
    cachedRenderer = GetComponent<Renderer>(); // Renderer 가져오기
    propertyBlock = new MaterialPropertyBlock(); // PropertyBlock 생성
    texturePropertyId = Shader.PropertyToID(texturePropertyName); // 텍스처 속성 이름을 ID로 캐싱
    colorPropertyId = Shader.PropertyToID(colorPropertyName); // 색상 속성 이름을 ID로 캐싱
}

    private void Start() // 시작 시 적용
    {
        if (applyOnStart) // 자동 적용이 켜져 있으면
        {
            ApplyTextureOverride(); // 텍스처 적용
        }
    }

    private void Update() // 필요 시 매 프레임 적용
    {
        if (applyEveryFrame) // 매 프레임 적용이 켜져 있으면
        {
            ApplyTextureOverride(); // 텍스처 적용
        }
    }

    /// <summary>
    /// 현재 설정된 Texture2D 또는 Sprite를 렌더러에 적용
    /// </summary>
public void ApplyTextureOverride() // 텍스처/색상 오버라이드 적용
{
    if (cachedRenderer == null) // 안전 체크
        return; // Renderer가 없으면 중단

    cachedRenderer.GetPropertyBlock(propertyBlock); // 기존 PropertyBlock 값 가져오기

    Texture targetTexture = GetTargetTexture(); // 현재 설정에 맞는 대상 텍스처 얻기
    if (targetTexture != null) // 적용할 텍스처가 있으면
    {
        propertyBlock.SetTexture(texturePropertyId, targetTexture); // 텍스처 슬롯에 대상 텍스처 설정
    }

    propertyBlock.SetColor(colorPropertyId, overrideColor); // 색상 슬롯에 대상 색상 설정
    cachedRenderer.SetPropertyBlock(propertyBlock); // Renderer에 적용
}

public void SetColor(Color newColor) // 색상 교체 후 즉시 적용
{
    overrideColor = newColor; // 새 색상 저장
    ApplyTextureOverride(); // 텍스처/색상 오버라이드 즉시 적용
}

    /// <summary>
    /// 현재 설정에 맞는 대상 텍스처 반환
    /// </summary>
    private Texture GetTargetTexture() // 실제 적용할 텍스처 선택
    {
        switch (imageSourceType) // 소스 종류에 따라 분기
        {
            case ImageSourceType.Texture2D:
                return overrideTexture; // Texture2D 반환

            case ImageSourceType.Sprite:
                return overrideSprite != null ? overrideSprite.texture : null; // Sprite의 texture 반환

            default:
                return null; // 예외 상황 대비
        }
    }

    /// <summary>
    /// Texture2D를 즉시 적용
    /// </summary>
    public void SetTexture(Texture2D newTexture) // Texture2D 교체 후 즉시 적용
    {
        overrideTexture = newTexture; // 새 텍스처 저장
        imageSourceType = ImageSourceType.Texture2D; // 소스 타입 변경
        ApplyTextureOverride(); // 즉시 적용
    }

    /// <summary>
    /// Sprite를 즉시 적용
    /// </summary>
    public void SetSprite(Sprite newSprite) // Sprite 교체 후 즉시 적용
    {
        overrideSprite = newSprite; // 새 스프라이트 저장
        imageSourceType = ImageSourceType.Sprite; // 소스 타입 변경
        ApplyTextureOverride(); // 즉시 적용
    }

    /// <summary>
    /// 현재 적용된 PropertyBlock을 제거하여 공유 머티리얼 기본 상태로 되돌림
    /// </summary>
    public void ClearTextureOverride() // 오버라이드 제거
    {
        if (cachedRenderer == null) // 안전 체크
            return; // Renderer가 없으면 중단

        propertyBlock.Clear(); // PropertyBlock 초기화
        cachedRenderer.SetPropertyBlock(propertyBlock); // 비운 상태 적용
    }
}