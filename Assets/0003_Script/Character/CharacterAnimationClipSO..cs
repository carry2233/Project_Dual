using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterAnimationClip", menuName = "Project Dual/캐릭터 애니메이션 클립")]
public class CharacterAnimationClipSO : ScriptableObject
{
    [System.Serializable]
    public class AnimationFrameData
    {
        [Header("프레임 이미지")]
        [SerializeField] private Sprite frameSprite; // 이 프레임에서 출력할 스프라이트

        [Header("주기값 덮어쓰기 설정")]
        [SerializeField] private bool useOverrideImageChangeInterval; // 이 프레임에서 주기값을 덮어쓸지 여부
        [SerializeField] private float overrideImageChangeInterval = 0.1f; // 덮어쓸 이미지 변경 주기값

        public Sprite FrameSprite => frameSprite; // 프레임 이미지 반환
        public bool UseOverrideImageChangeInterval => useOverrideImageChangeInterval; // 주기값 덮어쓰기 여부 반환
        public float OverrideImageChangeInterval => overrideImageChangeInterval; // 덮어쓸 주기값 반환
    }

    [Header("프레임 목록")]
    [SerializeField] private List<AnimationFrameData> frameList = new List<AnimationFrameData>(); // 애니메이션 프레임 목록

    public IReadOnlyList<AnimationFrameData> FrameList => frameList; // 프레임 목록 반환
    public int FrameCount => frameList != null ? frameList.Count : 0; // 프레임 개수 반환

    public AnimationFrameData GetFrame(int index) // 특정 프레임 데이터 반환
    {
        if (frameList == null || frameList.Count == 0)
        {
            return null; // 프레임이 없으면 null 반환
        }

        if (index < 0 || index >= frameList.Count)
        {
            return null; // 범위를 벗어나면 null 반환
        }

        return frameList[index]; // 해당 프레임 반환
    }
}