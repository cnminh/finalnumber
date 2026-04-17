using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FinalNumber.SumPuzzle
{
    /// <summary>
    /// Visual representation of a sum puzzle tile.
    /// Handles user interaction and visual states.
    /// </summary>
    public class SumPuzzleTile : MonoBehaviour
    {
        [Header("UI References")]
        public Button tileButton;
        public TextMeshProUGUI numberText;
        public Image backgroundImage;
        public Image highlightImage;
        public Image errorImage;

        [Header("Visual Settings")]
        public Color normalColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        public Color fixedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        public Color selectedColor = new Color(0.5f, 0.7f, 1f, 1f);
        public Color validColor = new Color(0.7f, 1f, 0.7f, 1f);
        public Color invalidColor = new Color(1f, 0.5f, 0.5f, 1f);
        public Color hintColor = new Color(1f, 1f, 0.5f, 1f);

        [Header("Animation")]
        public float appearDuration = 0.2f;
        public float scaleBounce = 1.1f;

        // State
        public int Row { get; private set; }
        public int Column { get; private set; }
        public int CurrentValue { get; private set; }
        public bool IsFixed { get; private set; }
        public bool IsSelected { get; private set; }
        public bool IsValid { get; private set; }

        // Events
        public System.Action<int, int> OnTileClicked;

        private RectTransform _rectTransform;
        private Vector3 _originalScale;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform != null)
                _originalScale = _rectTransform.localScale;

            if (tileButton != null)
            {
                tileButton.onClick.AddListener(HandleClick);
            }
        }

        private void OnDestroy()
        {
            if (tileButton != null)
            {
                tileButton.onClick.RemoveListener(HandleClick);
            }
        }

        /// <summary>
        /// Initialize the tile with grid position
        /// </summary>
        public void Initialize(int row, int column)
        {
            Row = row;
            Column = column;
            CurrentValue = 0;
            IsFixed = false;
            IsSelected = false;
            IsValid = true;

            UpdateVisuals();
        }

        /// <summary>
        /// Set the value displayed on this tile
        /// </summary>
        public void SetValue(int value, bool isFixed = false)
        {
            CurrentValue = value;
            IsFixed = isFixed;

            if (numberText != null)
            {
                numberText.text = value > 0 ? value.ToString() : "";
            }

            if (value > 0 && !isFixed)
            {
                PlayAppearAnimation();
            }

            UpdateVisuals();
        }

        /// <summary>
        /// Clear the tile value
        /// </summary>
        public void Clear()
        {
            CurrentValue = 0;
            IsFixed = false;

            if (numberText != null)
            {
                numberText.text = "";
            }

            UpdateVisuals();
        }

        /// <summary>
        /// Set the selected state
        /// </summary>
        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            UpdateVisuals();
        }

        /// <summary>
        /// Set the validation state
        /// </summary>
        public void SetValid(bool valid)
        {
            IsValid = valid;
            UpdateVisuals();
        }

        /// <summary>
        /// Show hint highlight
        /// </summary>
        public void ShowHint()
        {
            if (highlightImage != null)
            {
                highlightImage.color = hintColor;
                highlightImage.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Hide hint highlight
        /// </summary>
        public void HideHint()
        {
            UpdateVisuals();
        }

        /// <summary>
        /// Update visual state based on current properties
        /// </summary>
        private void UpdateVisuals()
        {
            if (backgroundImage == null)
                return;

            // Set background color based on state
            if (IsFixed)
            {
                backgroundImage.color = fixedColor;
            }
            else if (IsSelected)
            {
                backgroundImage.color = selectedColor;
            }
            else if (!IsValid && CurrentValue > 0)
            {
                backgroundImage.color = invalidColor;
            }
            else if (CurrentValue > 0 && IsRowColumnComplete())
            {
                backgroundImage.color = validColor;
            }
            else
            {
                backgroundImage.color = normalColor;
            }

            // Update error indicator
            if (errorImage != null)
            {
                errorImage.gameObject.SetActive(!IsValid && CurrentValue > 0);
            }

            // Update highlight
            if (highlightImage != null)
            {
                highlightImage.gameObject.SetActive(IsSelected);
                if (IsSelected)
                {
                    highlightImage.color = selectedColor;
                }
            }

            // Update interactability
            if (tileButton != null)
            {
                tileButton.interactable = !IsFixed;
            }

            // Set text color
            if (numberText != null)
            {
                numberText.color = IsFixed ? Color.black : Color.black;
            }
        }

        /// <summary>
        /// Check if the row and column this tile belongs to are complete
        /// </summary>
        private bool IsRowColumnComplete()
        {
            // This is a simplified check - the actual check is done by the board
            return IsValid;
        }

        /// <summary>
        /// Handle click event
        /// </summary>
        private void HandleClick()
        {
            if (!IsFixed)
            {
                OnTileClicked?.Invoke(Row, Column);
            }
        }

        /// <summary>
        /// Play appear animation
        /// </summary>
        private void PlayAppearAnimation()
        {
            if (_rectTransform == null)
                return;

            // Simple scale bounce
            _rectTransform.localScale = Vector3.one * 0.1f;

            // Animate to full scale (LeanTween removed - using instant scale)
            _rectTransform.localScale = _originalScale;
        }

        /// <summary>
        /// Play error shake animation
        /// </summary>
        public void PlayErrorAnimation()
        {
            if (_rectTransform == null)
                return;

            float shakeAmount = 10f;
            float shakeDuration = 0.3f;

            // Shake animation removed (LeanTween not available)
        }

        /// <summary>
        /// Set up the tile components if they don't exist (runtime creation)
        /// </summary>
        public void EnsureComponents()
        {
            // Ensure we have a RectTransform
            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
                if (_rectTransform == null)
                {
                    _rectTransform = gameObject.AddComponent<RectTransform>();
                }
            }

            // Ensure we have a Button
            if (tileButton == null)
            {
                tileButton = GetComponent<Button>();
                if (tileButton == null)
                {
                    tileButton = gameObject.AddComponent<Button>();
                    tileButton.onClick.AddListener(HandleClick);
                }
            }

            // Ensure we have an Image for background
            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
                if (backgroundImage == null)
                {
                    backgroundImage = gameObject.AddComponent<Image>();
                    backgroundImage.color = normalColor;
                }
            }

            // Create number text if needed
            if (numberText == null)
            {
                GameObject textObj = new GameObject("NumberText");
                textObj.transform.SetParent(transform, false);
                textObj.layer = gameObject.layer;

                RectTransform textRect = textObj.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;

                numberText = textObj.AddComponent<TextMeshProUGUI>();
                numberText.alignment = TextAlignmentOptions.Center;
                numberText.fontSize = 36;
                numberText.color = Color.black;
                numberText.raycastTarget = false;
            }
        }
    }
}
