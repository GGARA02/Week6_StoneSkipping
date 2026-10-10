using System;
using System.Text;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class CreditScrollPanel : MonoBehaviour
{
    private const string DEFAULT_TITLE = "Special Thanks";

    [Header("Font")]
    [SerializeField] private TMP_FontAsset _fontAsset;

    [Header("Data")]
    [SerializeField] private ContributorList _contributor;

    [Header("UI Reference")]
    [Tooltip("스크롤될 CreditPanel (미할당 시 본 오브젝트의 RectTransform)")]
    [SerializeField] private RectTransform _creditPanel;
    [SerializeField] private TMP_Text _creditText;

    [Header("Display Mode (Text Size)")]
    [Tooltip("3D 월드 아이템(Fish) 모드 여부. 체크 시 3D 공간에서 잘 보이도록 폰트 크기를 확대합니다.")]
    [SerializeField] private bool _is3DMode;
    [Tooltip("2D UI 모드 폰트 크기")]
    private const float UIFONTSIZE = 32f;
    [Tooltip("3D 월드 모드 폰트 크기")]
    private const float WORLDFONTSIZE = 80f;

    [Header("Mask Settings")]
    [Tooltip("부모 패널에 RectMask2D를 자동 적용하여 보드 크기 밖으로 나가는 글자를 자를지 여부")]
    [SerializeField] private bool _autoApplyRectMask2D = true;

    [Header("Scroll Settings")]
    [Tooltip("위로 스크롤되는 속도 (px/초)")]
    private float _scrollSpeed = 120f;
    [Tooltip("바닥 시작 및 상단 종료 시 추가 여백(px)")]
    [SerializeField] private float _marginOffset = 120f;

    private bool _isInitialized;

    public event Action OnScrollFinished;

    void Awake()
    {
        if (_creditPanel == null)
        {
            _creditPanel = GetComponent<RectTransform>();
        }
        HidePanelBackground();
        EnsureRectMask();
        EnsureCreditText();
    }

    /// <summary>
    /// 기울어진 CreditPanel 자체의 배경 Image가 3D 공간에 노출되지 않도록 비활성화한다.
    /// _creditPanel의 Image 컴포넌트를 탐색하며, enabled를 false로 변경한다.
    /// </summary>
    private void HidePanelBackground()
    {
        // 본 스크립트가 부모(CreditBackground)에 붙어있고 _creditPanel이 자식인 경우, 자식의 Image만 끈다.
        if (_creditPanel != transform && _creditPanel.TryGetComponent<Image>(out var panelImage))
        {
            panelImage.enabled = false;
        }
    }

    /// <summary>
    /// 부모 패널에 RectMask2D가 없으면 자동으로 추가하여 보드 영역 바깥의 글자를 마스킹한다.
    /// _creditPanel의 부모를 탐색하며, 필요 시 RectMask2D 컴포넌트를 추가한다.
    /// </summary>
    private void EnsureRectMask()
    {
        if (!_autoApplyRectMask2D || _creditPanel == null || _creditPanel.parent == null) return;

        if (_creditPanel.parent.GetComponent<RectMask2D>() == null && _creditPanel.parent.GetComponent<Mask>() == null)
        {
            _creditPanel.parent.gameObject.AddComponent<RectMask2D>();
        }
    }

    void OnEnable()
    {
        SetupCreditContent();
        ResetPosition();
    }

    void Update()
    {
        UpdateScroll();
    }

    /// <summary>
    /// 크레딧 패널을 위로 스크롤하고 화면 상단을 벗어나면 바닥으로 리셋하여 무한 반복한다.
    /// _scrollSpeed와 Time.unscaledDeltaTime을 사용하며, _creditPanel의 anchoredPosition을 변경한다.
    /// </summary>
    private void UpdateScroll()
    {
        if (_creditPanel == null) return;

        float startY = GetStartY();
        float endY = GetEndY();

        Vector2 pos = _creditPanel.anchoredPosition;
        pos.y += _scrollSpeed * Time.unscaledDeltaTime;

        // 화면 위로 완전히 벗어나면 바닥 위치로 되돌려 무한 반복한다.
        if (pos.y >= endY)
        {
            pos.y = startY;
            OnScrollFinished?.Invoke();
        }

        _creditPanel.anchoredPosition = pos;
    }

    /// <summary>
    /// Special Thanks 제목과 기여자 목록을 조합하여 텍스트 상단부터 반영한다.
    /// _contributor의 목록을 사용하며, _creditText의 내용과 상단 중앙 정렬을 변경한다.
    /// </summary>
    private void SetupCreditContent()
    {
        if (_creditText == null) return;

        _creditText.fontSize = _is3DMode ? WORLDFONTSIZE : UIFONTSIZE;

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("<size=140%><b>" + DEFAULT_TITLE + "</b></size>");
        builder.AppendLine();
        builder.AppendLine();

        if (_contributor != null && _contributor.contributorList != null)
        {
            for (int i = 0; i < _contributor.contributorList.Count; i++)
            {
                string name = _contributor.contributorList[i];
                if (!string.IsNullOrWhiteSpace(name))
                {
                    builder.AppendLine(name.Trim());
                    builder.AppendLine();
                }
            }
        }

        _creditText.text = builder.ToString();
        _creditText.alignment = TextAlignmentOptions.Top;
    }

    /// <summary>
    /// 크레딧 패널의 크기를 맞추고 회전 없이 패널이 화면 바닥 아래에 오도록 초기화한다.
    /// GetStartY()를 사용하며, _creditPanel의 회전, 크기, 위치를 변경한다.
    /// </summary>
    public void ResetPosition()
    {
        if (_creditPanel == null) return;

        // 독립 보드 형태로 동작할 수 있도록 앵커와 피벗을 중앙으로 맞춘다.
        _creditPanel.anchorMin = new Vector2(0.5f, 0.5f);
        _creditPanel.anchorMax = new Vector2(0.5f, 0.5f);
        _creditPanel.pivot = new Vector2(0.5f, 0.5f);

        float contentHeight = GetContentHeight();
        float panelHeight = Mathf.Max(1200f, contentHeight + 400f);
        _creditPanel.sizeDelta = new Vector2(900f, panelHeight);

        _creditPanel.localRotation = Quaternion.identity;
        _creditPanel.localScale = Vector3.one;
        _creditPanel.anchoredPosition = new Vector2(0f, GetStartY());
    }

    /// <summary>
    /// 크레딧 텍스트 컴포넌트가 없으면 패널 자식에 상단 정렬 형태로 새로 생성한다.
    /// _creditPanel을 탐색하며, _creditText를 초기화한다.
    /// </summary>
    private void EnsureCreditText()
    {
        if (_isInitialized) return;
        _isInitialized = true;

        if (_creditText == null)
        {
            _creditText = _creditPanel.GetComponentInChildren<TMP_Text>();
        }

        if (_creditText == null)
        {
            GameObject textObj = new GameObject("CreditText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(_creditPanel, false);

            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, 0f);
            rect.anchorMax = new Vector2(0.95f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            _creditText = textObj.GetComponent<TextMeshProUGUI>();
            _creditText.font = _fontAsset;
            _creditText.fontSize = _is3DMode ? WORLDFONTSIZE : UIFONTSIZE;
            _creditText.color = Color.white;
            _creditText.alignment = TextAlignmentOptions.Top;
        }
        else
        {
            _creditText.font = _fontAsset;
            _creditText.fontSize = _is3DMode ? WORLDFONTSIZE : UIFONTSIZE;
            _creditText.alignment = TextAlignmentOptions.Top;
        }
    }

    /// <summary>
    /// 패널의 최상단이 화면 바닥 아래에 완전히 숨겨진 시작 Y 좌표를 계산한다.
    /// 패널 높이와 뷰 높이를 사용하며, 시작 Y 좌표(픽셀)를 반환한다.
    /// </summary>
    private float GetStartY()
    {
        float viewHeight = GetViewHeight();
        float panelHeight = _creditPanel != null ? _creditPanel.sizeDelta.y : 1200f;
        return -viewHeight * 0.5f - panelHeight * 0.5f - _marginOffset;
    }

    /// <summary>
    /// 패널의 최하단이 화면 상단 위로 완전히 벗어난 종료 Y 좌표를 계산한다.
    /// 패널 높이와 뷰 높이를 사용하며, 종료 Y 좌표(픽셀)를 반환한다.
    /// </summary>
    private float GetEndY()
    {
        float viewHeight = GetViewHeight();
        float panelHeight = _creditPanel != null ? _creditPanel.sizeDelta.y : 1200f;
        return viewHeight * 0.5f + panelHeight * 0.5f + _marginOffset;
    }

    /// <summary>
    /// 부모 RectTransform 또는 Screen의 높이를 반환한다.
    /// 부모 Transform을 사용하며, 화면 기준 높이를 반환한다.
    /// </summary>
    private float GetViewHeight()
    {
        if (_creditPanel != null && _creditPanel.parent is RectTransform parentRect && parentRect.rect.height > 0f)
        {
            return parentRect.rect.height;
        }
        return Screen.height > 0 ? Screen.height : 1080f;
    }

    /// <summary>
    /// 크레딧 텍스트의 전체 세로 길이를 계산한다.
    /// _creditText를 사용하며, 계산된 텍스트 높이(픽셀)를 반환한다.
    /// </summary>
    private float GetContentHeight()
    {
        if (_creditText != null)
        {
            return _creditText.preferredHeight;
        }
        return 1000f;
    }
}
