using System;
using System.Collections;
using System.IO;

using UnityEngine;

/// <summary>
/// 블랙홀의 첫 착수 성공 화면을 흡입하고 저장된 16:9 이미지를 SCREEN 투척물로 제공한다.
/// </summary>
public class ScreenFishCapture : MonoBehaviour
{
    private const float ABSORB_DURATION = 0.5f;
    private const int IMAGE_WIDTH = 1280;
    private const int IMAGE_HEIGHT = 720;

    [Header("캡처 상태")]
    private PlayerProgress _progress;
    private Fish _template;
    private Mesh _mesh;
    private Material _material;
    private Texture2D _frame;
    private Transform _hole;
    private Vector2 _holePosition;
    private float _absorbProgress;
    private string _imagePath;
    public Texture2D Image { get; private set; }
    public bool IsCapturing { get; private set; }
    public bool HasFrame => _frame != null;
    public event Action OnUnlocked;

    /// <summary>
    /// progress와 저장 PNG를 사용해 비활성 SCREEN 템플릿을 만든다.
    /// 16:9 패널 메시와 이미지 머티리얼을 준비하고 선택 목록에 넣을 Fish를 반환한다.
    /// </summary>
    public Fish Initialize(PlayerProgress progress)
    {
        _progress = progress;
        _imagePath = Path.Combine(Application.persistentDataPath, "screen-fish.png");
        GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = "SCREEN Fish Catalog";
        panel.SetActive(false);
        panel.transform.SetParent(transform, false);
        Destroy(panel.GetComponent<Collider>());
        _mesh = Instantiate(panel.GetComponent<MeshFilter>().sharedMesh);
        Vector3[] vertices = _mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
            vertices[i] = Vector3.Scale(vertices[i], new Vector3(3.2f, 0.08f, 1.8f));
        _mesh.vertices = vertices;
        Vector2[] uvs = _mesh.uv;
        Vector3[] normals = _mesh.normals;
        for (int i = 0; i < vertices.Length; i++)
        {
            if (Mathf.Abs(normals[i].y) > 0.5f)
                uvs[i] = new Vector2(vertices[i].x / 3.2f + 0.5f, vertices[i].z / 1.8f + 0.5f);
        }
        _mesh.uv = uvs;
        _mesh.RecalculateBounds();
        panel.GetComponent<MeshFilter>().sharedMesh = _mesh;
        _material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        panel.GetComponent<MeshRenderer>().sharedMaterial = _material;
        panel.AddComponent<Rigidbody>().isKinematic = true;
        _template = panel.AddComponent<Fish>();
        _template.InitializeScreen();
        if (File.Exists(_imagePath))
        {
            Image = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (Image.LoadImage(File.ReadAllBytes(_imagePath))) _material.mainTexture = Image;
            else
            {
                Destroy(Image);
                Image = null;
                PlayerPrefs.DeleteKey("SkipStoneV2.Fish.screen");
            }
        }
        else PlayerPrefs.DeleteKey("SkipStoneV2.Fish.screen");
        return _template;
    }

    /// <summary>
    /// hole의 화면 위치를 목표로 전체 게임 화면 캡처와 흡입을 시작한다.
    /// 게임 시간을 멈추며 캡처 중 상태를 변경한다.
    /// </summary>
    public void Capture(Transform hole)
    {
        IsCapturing = true;
        Time.timeScale = 0f;
        _hole = hole;
        _absorbProgress = 0f;
        StartCoroutine(AbsorbScreen());
    }

    /// <summary>
    /// 완성된 게임 프레임을 캡처해 실제 시간 0.5초 동안 블랙홀로 흡입한다.
    /// 캡처 이미지를 저장하고 SCREEN을 해금한 뒤 게임 시간을 1로 복원한다.
    /// </summary>
    private IEnumerator AbsorbScreen()
    {
        try
        {
            yield return new WaitForEndOfFrame();
            _frame = UnityEngine.ScreenCapture.CaptureScreenshotAsTexture();
            Vector3 viewport = Camera.main.WorldToViewportPoint(_hole.position);
            _holePosition = new Vector2(viewport.x, 1f - viewport.y);
            float start = Time.realtimeSinceStartup;
            PrepareImage();
            while (Time.realtimeSinceStartup - start < ABSORB_DURATION)
            {
                _absorbProgress = (Time.realtimeSinceStartup - start) / ABSORB_DURATION;
                yield return null;
            }
            _progress.AddFish(_template.Type);
            OnUnlocked?.Invoke();
        }
        finally
        {
            Time.timeScale = 1f;
            IsCapturing = false;
            if (_frame != null) Destroy(_frame);
            _frame = null;
        }
        yield return null;
        File.WriteAllBytes(_imagePath, Image.EncodeToPNG());
    }

    /// <summary>
    /// 전체 캡처 프레임을 비율 유지해 1280x720 패널 이미지로 만든다.
    /// _frame을 사용하며 Image와 SCREEN 머티리얼을 변경한다.
    /// </summary>
    private void PrepareImage()
    {
        RenderTexture target = RenderTexture.GetTemporary(IMAGE_WIDTH, IMAGE_HEIGHT, 0);
        RenderTexture previous = RenderTexture.active;
        try
        {
            RenderTexture.active = target;
            GL.Clear(true, true, Color.black);
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, IMAGE_WIDTH, IMAGE_HEIGHT, 0);
            float scale = Mathf.Min((float)IMAGE_WIDTH / _frame.width, (float)IMAGE_HEIGHT / _frame.height);
            float width = _frame.width * scale;
            float height = _frame.height * scale;
            Graphics.DrawTexture(new Rect((IMAGE_WIDTH - width) * 0.5f, (IMAGE_HEIGHT - height) * 0.5f, width, height), _frame);
            GL.PopMatrix();
            Texture2D image = new Texture2D(IMAGE_WIDTH, IMAGE_HEIGHT, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, IMAGE_WIDTH, IMAGE_HEIGHT), 0, 0);
            image.Apply();
            if (Image != null) Destroy(Image);
            Image = image;
            _material.mainTexture = Image;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
        }
    }

    void OnGUI()
    {
        if (!IsCapturing || _frame == null) return;
        int depth = GUI.depth;
        Matrix4x4 matrix = GUI.matrix;
        GUI.depth = -1000;
        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 hole = new Vector2(_holePosition.x * Screen.width, _holePosition.y * Screen.height);
        float t = _absorbProgress * _absorbProgress;
        Vector2 position = Vector2.Lerp(center, hole, t);
        GUIUtility.RotateAroundPivot(180f * t, position);
        GUIUtility.ScaleAroundPivot(Vector2.one * Mathf.Max(0.001f, 1f - t), position);
        GUI.DrawTexture(new Rect(position.x - center.x, position.y - center.y, Screen.width, Screen.height), _frame);
        GUI.matrix = matrix;
        GUI.depth = depth;
    }

    void OnDisable()
    {
        if (!IsCapturing) return;
        StopAllCoroutines();
        Time.timeScale = 1f;
        IsCapturing = false;
        if (_frame != null) Destroy(_frame);
        _frame = null;
    }

    void OnDestroy()
    {
        if (Image != null) Destroy(Image);
        if (_material != null) Destroy(_material);
        if (_mesh != null) Destroy(_mesh);
    }
}
