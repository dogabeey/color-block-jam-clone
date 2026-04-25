using DG.Tweening;
using UnityEngine;

namespace Game
{
    public class ExitGateController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform animatedPart;
        [SerializeField] private Renderer gateRenderer;

        [Header("Visual")]
        [SerializeField] private Color defaultGateColor = new Color(0.45f, 0.45f, 0.45f, 1f);

        [Header("Animation")]
        [SerializeField] private float loweredOffset = -0.2f;
        [SerializeField] private float lowerBlendPortion = 0.25f;
        [SerializeField] private float raiseBlendPortion = 0.25f;

        private float baseLocalY;
        private bool hasCachedPose;
        private Sequence transitSequence;

        private void Awake()
        {
            EnsureSetup();
        }

        public void Init(ElementData elementData)
        {
            EnsureSetup();

            if (gateRenderer != null)
            {
                gateRenderer.material.color = elementData != null ? elementData.color : defaultGateColor;
            }
        }

        public void PlayTransit(float duration)
        {
            EnsureSetup();

            if (animatedPart == null || !isActiveAndEnabled)
            {
                return;
            }

            if (transitSequence != null && transitSequence.IsActive())
            {
                transitSequence.Kill(false);
            }

            float totalDuration = Mathf.Max(0.05f, duration);
            float lowerDuration = Mathf.Max(0.01f, totalDuration * Mathf.Clamp01(lowerBlendPortion));
            float raiseDuration = Mathf.Max(0.01f, totalDuration * Mathf.Clamp01(raiseBlendPortion));
            float holdDuration = Mathf.Max(0f, totalDuration - lowerDuration - raiseDuration);
            float loweredY = baseLocalY + loweredOffset;

            transitSequence = DOTween.Sequence();
            transitSequence.Append(animatedPart.DOLocalMoveY(loweredY, lowerDuration).SetEase(Ease.InOutSine));
            if (holdDuration > 0f)
            {
                transitSequence.AppendInterval(holdDuration);
            }
            transitSequence.Append(animatedPart.DOLocalMoveY(baseLocalY, raiseDuration).SetEase(Ease.InOutSine));
            transitSequence.OnKill(() =>
            {
                transitSequence = null;
            });
            transitSequence.OnComplete(() =>
            {
                SetAnimatedLocalY(baseLocalY);
                transitSequence = null;
            });
        }

        private void EnsureSetup()
        {
            if (animatedPart == null)
            {
                animatedPart = transform;
            }

            if (gateRenderer == null)
            {
                gateRenderer = GetComponentInChildren<Renderer>();
            }

            if (!hasCachedPose && animatedPart != null)
            {
                baseLocalY = animatedPart.localPosition.y;
                hasCachedPose = true;
            }
        }

        private void SetAnimatedLocalY(float y)
        {
            Vector3 localPosition = animatedPart.localPosition;
            localPosition.y = y;
            animatedPart.localPosition = localPosition;
        }
    }
}
