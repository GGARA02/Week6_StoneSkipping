using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캡처 화면을 하나의 촘촘한 UI 메시로 표시하며 GPU가 천의 각 지점을 독립적으로 당긴다.
/// </summary>
public class ScreenSuctionGraphic : MaskableGraphic
{
    private const int COLUMNS = 128;
    private const int ROWS = 72;

    private Texture _texture;
    public override Texture mainTexture => _texture;

    /// <summary>
    /// texture를 화면 메시의 원본으로 지정하고 Canvas 텍스처 바인딩을 갱신한다.
    /// </summary>
    public void SetTexture(Texture texture)
    {
        _texture = texture;
        SetMaterialDirty();
    }

    /// <summary>
    /// vh에 RectTransform 전체를 덮는 단일 격자를 생성한다.
    /// 고정된 원본 UV를 기록하여 셰이더 변형 중에도 이미지의 각 조각을 유지한다.
    /// </summary>
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = rectTransform.rect;
        for (int y = 0; y <= ROWS; y++)
        {
            for (int x = 0; x <= COLUMNS; x++)
            {
                Vector2 uv = new Vector2((float)x / COLUMNS, (float)y / ROWS);
                vh.AddVert(new Vector3(rect.xMin + uv.x * rect.width, rect.yMin + uv.y * rect.height, 0f), color, uv);
            }
        }
        for (int y = 0; y < ROWS; y++)
        {
            for (int x = 0; x < COLUMNS; x++)
            {
                int i = y * (COLUMNS + 1) + x;
                vh.AddTriangle(i, i + COLUMNS + 1, i + 1);
                vh.AddTriangle(i + 1, i + COLUMNS + 1, i + COLUMNS + 2);
            }
        }
    }
}
