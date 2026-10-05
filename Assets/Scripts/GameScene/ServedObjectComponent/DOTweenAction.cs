using System;
using DG.Tweening;
using UnityEngine;
using Sequence = DG.Tweening.Sequence;

namespace GameScene.ServedObjectComponent
{
    public static class DOTweenAction
    {
        // DOTween defaults to 200 tweeners / 50 sequences. A field full of mobs already holds a
        // few hundred idle tweens, and every death spends ~28 more, so the pool used to run out
        // mid-frame and DOTween corrupted its active-tween list while growing it.
        // Recycling stays off on purpose: several effects hold tween references past Kill and
        // would end up killing a recycled instance belonging to someone else.
        private const int TweenerCapacity = 2000;
        private const int SequenceCapacity = 500;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void SetTweenCapacity()
        {
            DOTween.Init().SetCapacity(TweenerCapacity, SequenceCapacity);
        }

        public struct WaddleParameters
        {
            public float tiltAngle;
            public float duration;
        }
    
        public struct HopParameters
        {
            public float jumpHeight;
            public float duration;
        }
    
        public struct BounceParameters
        {
            public Vector3 originScale;
            public float squashScale;
            public float bounceScale;
            public float duration;
        }
        public struct SwingParameters
        {
            public float angle;
            public float duration;
        }

        public struct HoverParameters
        {
            public float zDistance;
            public float duration;
        }
    
        private static readonly WaddleParameters _bigMobWaddleParam = new WaddleParameters
        {
            tiltAngle = 5f,
            duration  = 3f
        };
    
        private static readonly HopParameters _mobHopParam = new HopParameters
        {
            jumpHeight = 0.2f,
            duration   = 0.5f
        };
    
        private static readonly BounceParameters _mobBounceParam = new BounceParameters
        {
            originScale = Vector3.one,
            squashScale = 0.8f,
            bounceScale = 1.2f,
            duration    = 0.2f
        };
        private static readonly BounceParameters _mobCrawlParam = new BounceParameters
        {
            originScale = Vector3.one,
            squashScale = 0.7f,
            bounceScale = 1.3f,
            duration    = 0.5f
        };
    
        private static readonly SwingParameters _mobAttackParam = new ()
        {
            angle = 30f,
            duration  = 0.4f
        };
        private static readonly SwingParameters _stormIdleParam = new ()
        {
            angle = 15f,
            duration  = 2f
        };

        private static readonly HoverParameters _mobHoverParam = new()
        {
            zDistance = 0.5f,
            duration = 1f
        };

        public static void FallForward(Transform tr, float duration)
        {
            Sequence seq = DOTween.Sequence();
            seq.SetLink(tr.gameObject);
            seq.AppendInterval(0.1f);
            seq.Append(tr.DOLocalRotate(new Vector3(0, 0, 90), duration).SetEase(Ease.InQuad));
            seq.Join(tr.DOLocalMoveZ(-0.5f, duration).SetRelative(true).SetEase(Ease.InQuad));
            seq.Append(tr.DOScale(new Vector3(1.3f, 0.7f, 1f), 0.2f).SetEase(Ease.OutQuad));
            seq.Append(tr.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutElastic));
        }

        public static void Waddle(Transform tr, float tiltAngle, float duration)
        {
            tr.localRotation = Quaternion.identity;
        
            tr.DOLocalRotate(new Vector3(0, 0, tiltAngle), duration * 0.5f)
                .From(new Vector3(0, 0, -tiltAngle)) 
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true)
                .SetLink(tr.gameObject);
        }

        public static void Hop(Transform tr, float jumpHeight, float duration)
        {
            tr.DOLocalMoveY(jumpHeight, duration)
                .SetRelative(true)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.OutQuad)
                .SetLink(tr.gameObject);
        }
    
        public static Sequence Bounce(Transform tr, Vector3 originScale, float squashScale, float bounceScale, float duration)
        {
            Sequence seq = DOTween.Sequence();
            seq.SetLink(tr.gameObject);
            seq.Append(tr.DOScale(new Vector3(originScale.x * squashScale, originScale.y * bounceScale, originScale.z),
                    duration / 2).SetEase(Ease.OutQuad))
                .Append(tr.DOScale(originScale, duration / 2).SetEase(Ease.InQuad));
            return seq;
        }

        public static void Crawl(Transform tr, Vector3 originScale, float stretchScale, float squashScale, float duration)
        {
            Sequence seq = DOTween.Sequence();
            seq.SetLink(tr.gameObject);
            seq.Append(tr.DOScale(new Vector3(originScale.x * stretchScale, originScale.y * squashScale, originScale.z),
                    duration / 2).SetEase(Ease.OutQuad))
                .Append(tr.DOScale(originScale, duration / 2).SetEase(Ease.InOutQuad));
            seq.SetLoops(-1);
        }

        //Has ZVisual Issue
        public static void Hover(Transform tr, float zDistance, float duration)
        {
            Sequence seq = DOTween.Sequence();
            seq.SetLink(tr.gameObject);
            seq.Append(tr.DOLocalMoveZ(zDistance, duration/2 ).SetEase(Ease.InOutSine))
                .Append(tr.DOLocalMoveZ(0, duration/2).SetEase(Ease.InOutSine))
                .SetLoops(-1, LoopType.Yoyo);
        }
    
        public static void Swing(Transform tr, float angle, float duration)
        {
            if (tr == null)
            {
                return;
            }

            Sequence seq = DOTween.Sequence();
            seq.SetLink(tr.gameObject);
            seq
                .Append(tr.DOLocalRotate(new Vector3(0, 0, +angle), duration * 0.3f, RotateMode.Fast)
                    .SetEase(Ease.OutBack))
                .AppendInterval(duration * 0.3f)
                .Append(tr.DOLocalRotate(new Vector3(0, 0, -angle), duration * 0.2f, RotateMode.Fast)
                    .SetEase(Ease.OutCubic))
                .Append(tr.DOLocalRotate(new Vector3(0, 0, 0), duration * 0.2f, RotateMode.Fast)
                    .SetEase(Ease.OutQuad));
        }
        public static void StumbleStorm(Transform tr, float angle, float duration)
        {
            Sequence seq = DOTween.Sequence();
            seq.SetLink(tr.gameObject);
            seq.Append(tr.DOLocalRotate(new Vector3(0, 0, +angle), duration * 0.25f, RotateMode.Fast)
                    .SetEase(Ease.OutSine))
                .Append(tr.DOLocalRotate(new Vector3(0, 0, 0), duration * 0.25f, RotateMode.Fast)
                    .SetEase(Ease.InSine))
                .Append(tr.DOLocalRotate(new Vector3(0, 0, -angle), duration * 0.25f, RotateMode.Fast)
                    .SetEase(Ease.OutSine))
                .Append(tr.DOLocalRotate(new Vector3(0, 0, 0), duration * 0.25f, RotateMode.Fast)
                    .SetEase(Ease.InSine));
            seq.SetLoops(-1);
        }
    
        public static void Rotate(Transform tr, float angle, float duration, RotateMode mode = RotateMode.Fast)
        {
            Sequence seq = DOTween.Sequence();
            seq.SetLink(tr.gameObject);
            seq
                .Append(tr.DOLocalRotate(new Vector3(0, 0, angle), duration, mode)
                    .SetEase(Ease.OutBack));
        }
    
        public static void Pop(Transform tr, float duration = 0.5f, Action onComplete = null)
        {
            tr.localScale = Vector3.zero;
    
            Sequence seq = DOTween.Sequence();
            seq.SetLink(tr.gameObject);

            seq.Append(tr.DOScale(Vector3.one, duration * 0.4f)
                    .SetEase(Ease.OutBack))
                .Append(tr.DOScale(Vector3.zero, duration * 0.6f)
                    .SetEase(Ease.InBack))
                .OnComplete(() => onComplete?.Invoke());
        }
    
        public static void PopIn(Transform tr, float duration = 0.5f, Action onComplete = null)
        {
            tr.localScale = Vector3.zero;
    
            Sequence seq = DOTween.Sequence();
            seq.SetLink(tr.gameObject);

            seq.Append(tr.DOScale(Vector3.one, duration)
                    .SetEase(Ease.OutBack))
                .OnComplete(() => onComplete?.Invoke());
        }
        public static void Shrink(Transform tr, float duration = 0.5f, Action onComplete = null)
        {
            Sequence seq = DOTween.Sequence();
            seq.SetLink(tr.gameObject);

            seq.Append(tr.DOScale(Vector3.zero, duration))
                .OnComplete(() => onComplete?.Invoke());
        }
    
        public static void WaddleBigMob(Transform tr)
        {
            Waddle(tr, _bigMobWaddleParam.tiltAngle, _bigMobWaddleParam.duration);
        }
    
        public static void HopMob(Transform tr, float duration)
        {
            Hop(tr, _mobHopParam.jumpHeight, duration);
        }
    
        public static void HopMob(Transform tr)
        {
            Hop(tr, _mobHopParam.jumpHeight, _mobHopParam.duration);
        }

        public static Sequence BounceMob(Transform tr)
        {
            return Bounce(tr, _mobBounceParam.originScale, _mobBounceParam.squashScale,_mobBounceParam.bounceScale, _mobBounceParam.duration);
        }

        public static void HoverMob(Transform tr)
        {
            Hover(tr, _mobHoverParam.zDistance, _mobHoverParam.duration);
        }

        public static void CrawlMob(Transform tr)
        {
            Crawl(tr, _mobCrawlParam.originScale, _mobCrawlParam.bounceScale, _mobCrawlParam.squashScale, _mobCrawlParam.duration);
        }
    
        public static void SwingMobAttack(Transform tr)
        {
            Swing(tr, _mobAttackParam.angle, _mobAttackParam.duration);
        }

        public static void SwingStormIdle(Transform tr)
        {
            StumbleStorm(tr, _stormIdleParam.angle, _stormIdleParam.duration);
        }
    
        public static void BouncePlayerUseCard(Transform tr)
        {
            Bounce(tr, _mobBounceParam.originScale, _mobBounceParam.squashScale,_mobBounceParam.bounceScale, _mobBounceParam.duration);
        }
    }
}
