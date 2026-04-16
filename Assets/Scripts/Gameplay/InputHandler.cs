using System;
using UnityEngine;

namespace FinalNumber.Gameplay
{
    /// <summary>
    /// Handles touch and swipe input for mobile devices.
    /// Detects swipe gestures and translates them into move directions.
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        [Header("Swipe Detection")]
        [Tooltip("Minimum swipe distance to register as a valid gesture (in pixels)")]
        public float minSwipeDistance = 50f;
        
        [Tooltip("Maximum time for a swipe to be considered valid (in seconds)")]
        public float maxSwipeTime = 0.5f;
        
        [Tooltip("Enable keyboard input for testing in editor")]
        public bool enableKeyboardInput = true;

        // Events
        public event EventHandler<MoveDirection> OnSwipeDetected;
        public event EventHandler OnTapDetected;

        // Input state
        private Vector2 _touchStartPosition;
        private float _touchStartTime;
        private bool _isTrackingTouch;
        private int _trackedTouchId = -1;

        // Touch platform detection
        private bool _isTouchDevice;

        private void Awake()
        {
            // Detect if we're on a touch device
            _isTouchDevice = Application.platform == RuntimePlatform.Android ||
                            Application.platform == RuntimePlatform.IPhonePlayer ||
                            Input.touchSupported;
        }

        private void Update()
        {
            if (_isTouchDevice)
            {
                ProcessTouchInput();
            }
            else
            {
                ProcessMouseInput();
            }

            // Keyboard input for editor testing
            if (enableKeyboardInput)
            {
                ProcessKeyboardInput();
            }
        }

        /// <summary>
        /// Process touch input for mobile devices
        /// </summary>
        private void ProcessTouchInput()
        {
            if (Input.touchCount == 0)
                return;

            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    if (!_isTrackingTouch)
                    {
                        StartTracking(touch.position, touch.fingerId);
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (_isTrackingTouch && touch.fingerId == _trackedTouchId)
                    {
                        EndTracking(touch.position);
                    }
                    break;
            }
        }

        /// <summary>
        /// Process mouse input for desktop testing
        /// </summary>
        private void ProcessMouseInput()
        {
            // Mouse down - start tracking
            if (Input.GetMouseButtonDown(0))
            {
                StartTracking(Input.mousePosition, 0);
            }

            // Mouse up - end tracking
            if (Input.GetMouseButtonUp(0))
            {
                EndTracking(Input.mousePosition);
            }
        }

        /// <summary>
        /// Process keyboard input for testing
        /// </summary>
        private void ProcessKeyboardInput()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                OnSwipeDetected?.Invoke(this, MoveDirection.Up);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                OnSwipeDetected?.Invoke(this, MoveDirection.Down);
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                OnSwipeDetected?.Invoke(this, MoveDirection.Left);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                OnSwipeDetected?.Invoke(this, MoveDirection.Right);
            }
        }

        /// <summary>
        /// Start tracking a touch/mouse gesture
        /// </summary>
        private void StartTracking(Vector2 position, int touchId)
        {
            _touchStartPosition = position;
            _touchStartTime = Time.time;
            _isTrackingTouch = true;
            _trackedTouchId = touchId;
        }

        /// <summary>
        /// End tracking and determine the swipe gesture
        /// </summary>
        private void EndTracking(Vector2 endPosition)
        {
            if (!_isTrackingTouch)
                return;

            _isTrackingTouch = false;
            _trackedTouchId = -1;

            // Calculate swipe properties
            Vector2 swipeDelta = endPosition - _touchStartPosition;
            float swipeTime = Time.time - _touchStartTime;
            float swipeDistance = swipeDelta.magnitude;

            // Check if it's a valid swipe
            if (swipeTime > maxSwipeTime)
            {
                // Too slow - treat as a tap
                if (swipeDistance < minSwipeDistance * 0.5f)
                {
                    OnTapDetected?.Invoke(this, EventArgs.Empty);
                }
                return;
            }

            if (swipeDistance < minSwipeDistance)
            {
                // Too short - treat as a tap
                OnTapDetected?.Invoke(this, EventArgs.Empty);
                return;
            }

            // Determine swipe direction
            MoveDirection direction = CalculateSwipeDirection(swipeDelta);
            
            if (direction != MoveDirection.None)
            {
                Debug.Log($"[InputHandler] Swipe detected: {direction} (delta: {swipeDelta}, distance: {swipeDistance:F1})");
                OnSwipeDetected?.Invoke(this, direction);
            }
        }

        /// <summary>
        /// Calculate the primary direction of a swipe
        /// </summary>
        private MoveDirection CalculateSwipeDirection(Vector2 delta)
        {
            // Check which axis has more movement
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                // Horizontal swipe
                return delta.x > 0 ? MoveDirection.Right : MoveDirection.Left;
            }
            else
            {
                // Vertical swipe
                return delta.y > 0 ? MoveDirection.Up : MoveDirection.Down;
            }
        }

        /// <summary>
        /// Check if input is currently being tracked
        /// </summary>
        public bool IsTrackingInput()
        {
            return _isTrackingTouch;
        }

        /// <summary>
        /// Cancel current tracking (useful when pausing game)
        /// </summary>
        public void CancelTracking()
        {
            _isTrackingTouch = false;
            _trackedTouchId = -1;
        }

        /// <summary>
        /// Enable or disable input processing
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            this.enabled = enabled;
            if (!enabled)
            {
                CancelTracking();
            }
        }
    }
}
