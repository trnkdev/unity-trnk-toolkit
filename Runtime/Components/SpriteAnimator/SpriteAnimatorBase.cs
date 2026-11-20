using System;
using TRnK.Extensions;
using TRnK.Logger;
using UnityEngine;
using UnityEngine.Events;

namespace TRnK.Components
{
    public enum SpriteAnimatorLoopMode { Once, Loop, PingPong }

    public abstract class SpriteAnimatorBase : MonoBehaviour
    {
        [Serializable]
        protected sealed class FrameEvent
        {
            [SerializeField] private int _frameIndex;
            [SerializeField, Space(6)] private UnityEvent _onFrame;

            public int FrameIndex => _frameIndex;
            public UnityEvent OnFrame => _onFrame;

            public FrameEvent(int frameIndex)
            {
                _frameIndex = frameIndex;
                _onFrame = new UnityEvent();
            }
        }

        private const int MaxCatchUpStepsPerUpdate = 8;
        private const int NoFrame = -1;

        [Tooltip("The sprites to animate through.")]
        [SerializeField] protected Sprite[] _sprites;

        [Tooltip("The frame rate of the animation. 0 pauses playback.")]
        [SerializeField, Min(0f)] protected float _frameRate = 12f;

        [Tooltip("The speed multiplier for the animation. 0 pauses playback.")]
        [SerializeField, Min(0f)] protected float _speedMultiplier = 1f;

        [Tooltip("The loop mode of the animation.")]
        [SerializeField] protected SpriteAnimatorLoopMode _loopMode = SpriteAnimatorLoopMode.Loop;

        [Tooltip("Should the animation play on awake?")]
        [SerializeField] protected bool _playOnAwake = true;

        [Tooltip("Should the animation use unscaled time?")]
        [SerializeField] protected bool _useUnscaledTime = false;

        [Tooltip("Should the animation start at a random frame?")]
        [SerializeField] protected bool _startAtRandomFrame = false;

        [SerializeField] protected FrameEvent[] _frameEvents;

        [SerializeField] protected UnityEvent _onCycleComplete;

        public UnityEvent OnCycleComplete => _onCycleComplete;

        protected int _currentFrame = 0;
        protected bool _isPlaying = false;
        protected bool _isReversed = false;

        protected float _frameTimer = 0f;
        protected int _spriteCount;
        private int _eventsFiredFrame = NoFrame;
        private bool _cycleFinished;
        protected float FrameDuration
        {
            get
            {
                float framesPerSecond = _frameRate * _speedMultiplier;
                return framesPerSecond > 0f ? 1f / framesPerSecond : float.PositiveInfinity;
            }
        }

        public bool IsPlaying => _isPlaying;
        public int CurrentFrame => _currentFrame;
        public int FrameCount => _spriteCount;
        public SpriteAnimatorLoopMode LoopMode => _loopMode;

        protected virtual void Awake()
        {
            CacheSpritesInfo();
            SetInitialSprite();
        }

        protected virtual void Start()
        {
            if (_playOnAwake)
                Play(_loopMode);
        }

        protected virtual void Update()
        {
            if (!_isPlaying || _spriteCount == 0)
                return;

            if (!ShouldAnimate())
                return;

            // Treat 0 as paused (eg. designer sets Frame Rate to 0 in the inspector)
            if (_frameRate <= 0f || _speedMultiplier <= 0f)
                return;

            _frameTimer += _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            float frameDuration = FrameDuration;

            if (_frameTimer < frameDuration)
            {
                return;
            }

            int catchUpSteps = Mathf.Min((int)(_frameTimer / frameDuration), MaxCatchUpStepsPerUpdate);
            _frameTimer -= catchUpSteps * frameDuration;
            // After a hitch, drop what the cap skipped instead of fast-forwarding through it over the next updates
            if (_frameTimer >= frameDuration) _frameTimer %= frameDuration;

            for (int i = 0; i < catchUpSteps; i++)
            {
                if (!_isPlaying)
                {
                    break;
                }

                NextFrame();
            }
        }

        protected abstract bool ShouldAnimate();
        protected abstract void SetInitialSprite();
        protected abstract void UpdateSprite();

        protected void CacheSpritesInfo()
        {
            _spriteCount = (_sprites != null) ? _sprites.Length : 0;
        }

        protected void NextFrame()
        {
            _currentFrame += _isReversed ? -1 : 1;
            if (_currentFrame >= _spriteCount || _currentFrame < 0)
            {
                // Once stops on the frame it already showed, so that frame's events must not fire again
                bool stopsHere = _loopMode == SpriteAnimatorLoopMode.Once;
                HandleEndReached();
                if (stopsHere) return;
            }

            UpdateSprite();
            CheckFrameEvents();
        }

        protected void HandleEndReached()
        {
            switch (_loopMode)
            {
                case SpriteAnimatorLoopMode.Once:
                    _currentFrame = _isReversed ? 0 : _spriteCount - 1;
                    _cycleFinished = true;
                    Stop();
                    _onCycleComplete?.Invoke();
                    break;

                case SpriteAnimatorLoopMode.Loop:
                    _currentFrame = _isReversed ? _spriteCount - 1 : 0;
                    _onCycleComplete?.Invoke();
                    break;

                case SpriteAnimatorLoopMode.PingPong:
                    _isReversed = !_isReversed;
                    _currentFrame = _isReversed ? _spriteCount - 2 : 1;
                    _currentFrame = Mathf.Clamp(_currentFrame, 0, _spriteCount - 1);
                    _onCycleComplete?.Invoke();
                    break;
            }
        }

        protected void CheckFrameEvents()
        {
            _eventsFiredFrame = _currentFrame;
            if (_frameEvents == null) return;

            for (int i = 0; i < _frameEvents.Length; i++)
            {
                var frameEvent = _frameEvents[i];

                if (frameEvent.FrameIndex < 0 || frameEvent.FrameIndex >= _spriteCount)
                    continue;

                if (frameEvent.FrameIndex == _currentFrame)
                {
                    frameEvent.OnFrame?.Invoke();
                }
            }
        }

        /// <summary>Play the animation with the loop mode set in the Inspector.</summary>
        public void Play() => Play(_loopMode);

        /// <summary>Play the animation with the given loop mode, which replaces the Inspector value.</summary>
        public virtual void Play(SpriteAnimatorLoopMode loopMode)
        {
            _loopMode = loopMode;

            if (_spriteCount == 0)
            {
                Log.Warn("No sprites assigned to " + GetType().Name + " on " + gameObject.name);
                return;
            }

            if (_spriteCount < 2 && _loopMode == SpriteAnimatorLoopMode.PingPong)
                Log.Warn($"[SpriteAnimator] PingPong mode requires at least 2 sprites on '{gameObject.name}'; OnCycleComplete will fire every frame.");

            _isReversed = false;
            bool starting = !_isPlaying;

            // A finished Once cycle starts over instead of ending again on its last frame
            if (starting && (_startAtRandomFrame || _cycleFinished))
            {
                _currentFrame = _startAtRandomFrame ? _sprites.RandIndex() : 0;
                UpdateSprite();
            }

            _cycleFinished = false;
            _isPlaying = true;
            _frameTimer = 0f;

            // The start frame is already showing, so fire its events once; resuming after Stop() doesn't fire them again
            if (starting && _eventsFiredFrame != _currentFrame) CheckFrameEvents();
        }

        /// <summary>Stop the animation.</summary>
        public virtual void Stop()
        {
            _isPlaying = false;
            _frameTimer = 0f;
        }

        /// <summary>Restart the animation from frame 0 using the current loop mode.</summary>
        public virtual void Restart()
        {
            _currentFrame = 0;
            _frameTimer = 0f;
            _isReversed = false;
            _cycleFinished = false;
            UpdateSprite();

            // Already marked playing, so Play() keeps frame 0 instead of a random start; frame 0's events fire here
            _isPlaying = _spriteCount > 0;
            Play(_loopMode);
            CheckFrameEvents();
        }

        /// <summary>Set the frame rate of the animation.</summary>
        public void SetFrameRate(float newFrameRate)
        {
            _frameRate = Mathf.Max(0f, newFrameRate);
        }

        /// <summary>Set the speed multiplier of the animation.</summary>
        public void SetSpeedMultiplier(float multiplier)
        {
            _speedMultiplier = Mathf.Max(0f, multiplier);
        }

        /// <summary>Go to a specific frame in the animation.</summary>
        public void GoToFrame(int frameIndex)
        {
            if (_spriteCount == 0)
            {
                Log.Warn($"[SpriteAnimator] GoToFrame({frameIndex}): no sprites assigned on '{gameObject.name}'.");
                return;
            }
            if (frameIndex < 0 || frameIndex >= _spriteCount)
            {
                Log.Warn($"[SpriteAnimator] GoToFrame({frameIndex}): index out of range [0, {_spriteCount - 1}] on '{gameObject.name}'.");
                return;
            }
            _currentFrame = frameIndex;
            _frameTimer = 0f;
            _cycleFinished = false;
            UpdateSprite();
        }

    }
}