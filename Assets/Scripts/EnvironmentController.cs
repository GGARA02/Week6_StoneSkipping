using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

// 날씨, 스카이박스, 후처리 볼륨처럼 화면 분위기를 바꾸는 환경을 한곳에서 관리한다.
// 던지기 전마다 비와 밤을 확률로 정하고 조명, 하늘, 구름, 주변광, 안개, 비, 후처리에 반영한다.
// 낮 값은 시작할 때 씬 설정을 저장해 쓴다. 재시작 때는 바로 바꾸고, 고른 물고기 때문에 바뀌는 날씨는 서서히 바꾼다.
// 스카이박스는 기본 하늘 위에 밤, 비처럼 요청받은 스카이박스를 비중만큼 먼저 요청한 순서대로 섞는다.
// 후처리 볼륨은 프로필마다 비중을 요청하며, 처음 요청한 프로필은 자식 전역 볼륨을 만들어 쓴다.
public class EnvironmentController : MonoBehaviour
{
    private class VolumeEntry
    {
        public Volume Volume;
        public float Target;
        public float Speed;
    }

    private class SkyboxLayer
    {
        public Material Material;
        public float Weight;
        public float Target;
        public float Speed;
    }

    private const int SH_COEFFICIENT_COUNT = 9;
    // 셰이더가 다른 스카이박스는 값을 섞을 수 없어 비중이 이 값을 넘을 때 바꾼다.
    private const float SKYBOX_SWITCH_WEIGHT = 0.5f;

    private static readonly int RAIN_INTENSITY_ID = Shader.PropertyToID("_RainIntensity");
    private static readonly int CLOUD_COVERAGE_ID = Shader.PropertyToID("_Coverage");

    [Header("참조")]
    [SerializeField]
    private Light _sun;
    [SerializeField]
    private Transform _cameraTransform;
    [Tooltip("비가 따라다니는 대상. 던진 물고기를 중심으로 매 프레임 비 위치를 다시 잡는다")]
    [SerializeField]
    private Transform _player;
    [SerializeField]
    private ParticleSystem _rain;
    [Tooltip("멀리까지 넓게 뿌리는 성긴 비. 가까운 비를 따라다닌다")]
    [SerializeField]
    private ParticleSystem _farRain;
    [Tooltip("하늘만 찍는 반사 프로브. 하늘이 바뀔 때마다 다시 찍어 물과 물체의 반사를 맞춘다")]
    [SerializeField]
    private ReflectionProbe _skyReflectionProbe;
    [Tooltip("카메라를 따라다니는 구름층 구. 비가 올 때 하늘을 덮는다")]
    [SerializeField]
    private Renderer _cloudRenderer;

    [Header("확률")]
    [Range(0f, 1f)]
    [SerializeField]
    private float _rainChance = 0.1f;
    [Range(0f, 1f)]
    [SerializeField]
    private float _nightChance = 0.1f;

    [Header("전환")]
    [Tooltip("물고기를 골라 날씨가 바뀔 때 걸리는 시간(초)")]
    [SerializeField]
    private float _fadeDuration = 1f;

    [Header("하늘")]
    [Tooltip("기본 하늘. 다른 스카이박스는 이 위에 비중만큼 섞는다")]
    [SerializeField]
    private Material _daySkybox;
    private readonly List<SkyboxLayer> _skyboxLayers = new List<SkyboxLayer>();
    private Material _skybox;
    private Material _skyboxScratch;

    // 밤 해 값은 GameLab6 SkyManager의 해 뜨기 전 시작값을 따른다. 밤 하늘 값은 밤 스카이박스 머티리얼에 있다.
    [Header("밤")]
    [SerializeField]
    private Material _nightSkybox;
    [Tooltip("밤의 해 X 회전. 0이면 해가 지평선에 걸린다. Y 회전은 낮 값을 그대로 쓴다")]
    [SerializeField]
    private float _nightSunXRotation = 0f;
    [SerializeField]
    private float _nightSunIntensity = 0.3f;
    [Tooltip("낮 주변광에 곱하는 색")]
    [SerializeField]
    private Color _nightAmbient = new Color(0.12f, 0.15f, 0.28f);
    [SerializeField]
    private VolumeProfile _nightProfile;
    private Quaternion _nightSunRotation;

    [Header("비")]
    [SerializeField]
    private Material _rainSkybox;
    [Tooltip("비가 올 때 해 또는 달 세기에 곱하는 값")]
    [SerializeField]
    private float _rainLightScale = 0.7f;
    [Tooltip("낮 주변광에 곱하는 색")]
    [SerializeField]
    private Color _rainAmbient = new Color(0.85f, 0.87f, 0.9f);
    [SerializeField]
    private VolumeProfile _rainProfile;
    [SerializeField]
    private Color _rainFogColor = new Color(0.62f, 0.65f, 0.7f);
    [SerializeField]
    private Color _rainNightFogColor = new Color(0.05f, 0.06f, 0.09f);
    [SerializeField]
    private float _rainFogDensity = 0.0018f;
    [Tooltip("비가 가장 셀 때 구름 덮임 정도")]
    [Range(0f, 1f)]
    [SerializeField]
    private float _rainCloudCoverage = 0.85f;
    [Tooltip("비가 가장 셀 때 초당 빗방울 수")]
    [SerializeField]
    private float _rainEmissionRate = 8000f;
    [Tooltip("비가 가장 셀 때 먼 비의 초당 빗방울 수")]
    [SerializeField]
    private float _farRainEmissionRate = 15000f;
    [Tooltip("플레이어 기준 빗방울 생성 위치(월드 축)")]
    [SerializeField]
    private Vector3 _rainOffset = new Vector3(0f, 14f, 0f);
    [Tooltip("플레이어가 이 시간(초) 뒤에 있을 자리에 빗방울을 만든다. 빠르게 날아가도 비가 뒤처지지 않게 한다")]
    [SerializeField]
    private float _rainLeadTime = 0.6f;
    [Tooltip("이보다 빠르게 움직이면 재시작 같은 순간 이동으로 보고 속도로 치지 않는다")]
    [SerializeField]
    private float _maxTrackedPlayerSpeed = 200f;

    [Header("후처리")]
    [Tooltip("새로 만드는 볼륨의 우선순위. 기본 Global Volume보다 높아야 덮어쓴다")]
    [SerializeField]
    private float _volumePriority = 1f;
    private readonly Dictionary<VolumeProfile, VolumeEntry> _volumes = new Dictionary<VolumeProfile, VolumeEntry>();

    [Header("낮 값")]
    private Material _originalSkybox;
    private Material _cloudMaterial;
    private float _daySunIntensity;
    private Quaternion _daySunRotation;
    private SphericalHarmonicsL2 _dayAmbientProbe;
    private bool _dayFog;
    private Color _dayFogColor;
    private float _dayFogDensity;

    [Header("상태")]
    private bool _baseRain;
    private bool _baseNight;
    private bool _targetRain;
    private bool _targetNight;
    private float _rainWeight;
    private float _nightWeight;
    private Vector3 _lastPlayerPosition;
    private Vector3 _playerVelocity;
    // 서서히 바뀌는 중이어도 정해진 날씨를 기준으로 한다.
    public bool IsRaining => _targetRain;
    public bool IsNight => _targetNight;

    void Awake()
    {
        // 공유 스카이박스 에셋을 바꾸면 플레이를 멈춰도 값이 남아서, 섞은 결과는 복사본에 담는다.
        _originalSkybox = RenderSettings.skybox;
        _skybox = new Material(_daySkybox);
        _skyboxScratch = new Material(_daySkybox);
        RenderSettings.skybox = _skybox;
        _cloudMaterial = _cloudRenderer.material;
        _lastPlayerPosition = _player.position;

        _daySunIntensity = _sun.intensity;
        _daySunRotation = _sun.transform.rotation;
        Vector3 daySunAngles = _daySunRotation.eulerAngles;
        _nightSunRotation = Quaternion.Euler(_nightSunXRotation, daySunAngles.y, daySunAngles.z);
        _dayAmbientProbe = RenderSettings.ambientProbe;
        _dayFog = RenderSettings.fog;
        _dayFogColor = RenderSettings.fogColor;
        _dayFogDensity = RenderSettings.fogDensity;
    }

    void Update()
    {
        foreach (VolumeEntry entry in _volumes.Values)
        {
            if (Mathf.Approximately(entry.Volume.weight, entry.Target)) continue;
            entry.Volume.weight = Mathf.MoveTowards(entry.Volume.weight, entry.Target, entry.Speed * Time.unscaledDeltaTime);
        }

        bool skyboxChanged = false;
        foreach (SkyboxLayer layer in _skyboxLayers)
        {
            if (Mathf.Approximately(layer.Weight, layer.Target)) continue;
            layer.Weight = Mathf.MoveTowards(layer.Weight, layer.Target, layer.Speed * Time.unscaledDeltaTime);
            skyboxChanged = true;
        }
        if (skyboxChanged) RebuildSkybox();

        float targetRain = _targetRain ? 1f : 0f;
        float targetNight = _targetNight ? 1f : 0f;
        if (Mathf.Approximately(_rainWeight, targetRain) && Mathf.Approximately(_nightWeight, targetNight)) return;

        float step = Time.unscaledDeltaTime / Mathf.Max(_fadeDuration, 0.01f);
        _rainWeight = Mathf.MoveTowards(_rainWeight, targetRain, step);
        _nightWeight = Mathf.MoveTowards(_nightWeight, targetNight, step);
        ApplyWeights();
    }

    void LateUpdate()
    {
        Vector3 playerPosition = _player.position;
        if (Time.deltaTime > 0f)
        {
            // 재시작처럼 플레이어가 순간 이동한 프레임은 속도로 치지 않는다.
            Vector3 velocity = (playerPosition - _lastPlayerPosition) / Time.deltaTime;
            if (velocity.sqrMagnitude <= _maxTrackedPlayerSpeed * _maxTrackedPlayerSpeed)
            {
                _playerVelocity = velocity;
            }
        }
        _lastPlayerPosition = playerPosition;

        // 가까운 비와 먼 비 모두 플레이어를 중심으로 매 프레임 다시 놓는다. 먼 비는 가까운 비의 자식이다.
        Vector3 lead = new Vector3(_playerVelocity.x, 0f, _playerVelocity.z) * _rainLeadTime;
        _rain.transform.SetPositionAndRotation(playerPosition + _rainOffset + lead, Quaternion.identity);
        _cloudRenderer.transform.position = _cameraTransform.position;
    }

    void OnDestroy()
    {
        RenderSettings.skybox = _originalSkybox;
        Shader.SetGlobalFloat(RAIN_INTENSITY_ID, 0f);
        Destroy(_skybox);
        Destroy(_skyboxScratch);
        Destroy(_cloudMaterial);
    }

    /// <summary>
    /// 이번 판의 기본 날씨를 확률로 새로 정하고, 고른 물고기의 날씨까지 더해 바로 적용한다.
    /// fish(기본 물고기면 null)와 비, 밤 확률을 사용하며, 기본 날씨, 목표 날씨, 현재 날씨 비중을 변경한다.
    /// </summary>
    public void Roll(FishType fish)
    {
        _baseRain = RollChance(_rainChance);
        _baseNight = RollChance(_nightChance);
        SetTarget(fish);
        _rainWeight = _targetRain ? 1f : 0f;
        _nightWeight = _targetNight ? 1f : 0f;
        ApplyWeights();
    }

    /// <summary>
    /// 던질 거리를 바꿨을 때 그 물고기의 선택 시 날씨 설정으로 목표 날씨를 다시 정한다. 실제 변화는 Update에서 서서히 진행한다.
    /// fish(기본 물고기면 null)를 사용하며, 목표 날씨를 변경한다.
    /// </summary>
    public void ChangeFish(FishType fish)
    {
        SetTarget(fish);
    }

    /// <summary>
    /// 스카이박스의 비중을 바로 바꾼다. 처음 요청한 스카이박스는 섞는 순서의 맨 뒤에 붙는다.
    /// skybox와 weight(0~1)를 사용하며, 해당 스카이박스 비중과 섞은 하늘을 변경한다.
    /// </summary>
    public void SetSkyboxWeight(Material skybox, float weight)
    {
        SkyboxLayer layer = GetSkyboxLayer(skybox);
        layer.Target = weight;
        layer.Weight = weight;
        RebuildSkybox();
    }

    /// <summary>
    /// 스카이박스의 비중을 duration초 동안 서서히 바꾼다. 실제 변화는 Update에서 진행한다.
    /// skybox, weight(0~1), duration을 사용하며, 해당 스카이박스의 목표 비중과 변화 속도를 변경한다.
    /// </summary>
    public void FadeSkyboxWeight(Material skybox, float weight, float duration)
    {
        SkyboxLayer layer = GetSkyboxLayer(skybox);
        layer.Target = weight;
        layer.Speed = Mathf.Abs(weight - layer.Weight) / Mathf.Max(duration, 0.01f);
    }

    /// <summary>
    /// 프로필의 후처리 볼륨 비중을 바로 바꾼다. 진행 중이던 서서히 바꾸기는 멈춘다.
    /// profile과 weight(0~1)를 사용하며, 해당 볼륨의 비중과 목표 비중을 변경한다.
    /// </summary>
    public void SetVolumeWeight(VolumeProfile profile, float weight)
    {
        VolumeEntry entry = GetVolumeEntry(profile);
        entry.Target = weight;
        entry.Volume.weight = weight;
    }

    /// <summary>
    /// 프로필의 후처리 볼륨 비중을 duration초 동안 서서히 바꾼다. 실제 변화는 Update에서 진행한다.
    /// profile, weight(0~1), duration을 사용하며, 해당 볼륨의 목표 비중과 변화 속도를 변경한다.
    /// </summary>
    public void FadeVolumeWeight(VolumeProfile profile, float weight, float duration)
    {
        VolumeEntry entry = GetVolumeEntry(profile);
        entry.Target = weight;
        entry.Speed = Mathf.Abs(weight - entry.Volume.weight) / Mathf.Max(duration, 0.01f);
    }

    /// <summary>
    /// 스카이박스에 해당하는 섞기 층을 찾고, 없으면 비중 0으로 맨 뒤에 추가한다.
    /// skybox를 사용하며, 섞기 층을 반환하고 처음이면 _skyboxLayers에 추가한다.
    /// </summary>
    private SkyboxLayer GetSkyboxLayer(Material skybox)
    {
        foreach (SkyboxLayer existing in _skyboxLayers)
        {
            if (existing.Material == skybox) return existing;
        }
        SkyboxLayer layer = new SkyboxLayer { Material = skybox };
        _skyboxLayers.Add(layer);
        return layer;
    }

    /// <summary>
    /// 기본 하늘에서 시작해 각 스카이박스를 비중만큼 차례로 섞어 화면 하늘을 다시 만든다.
    /// _daySkybox와 _skyboxLayers를 사용하며, _skybox의 셰이더와 값, 반사 프로브를 변경한다.
    /// </summary>
    private void RebuildSkybox()
    {
        _skybox.shader = _daySkybox.shader;
        _skybox.CopyPropertiesFromMaterial(_daySkybox);
        foreach (SkyboxLayer layer in _skyboxLayers)
        {
            if (layer.Weight <= 0f) continue;
            if (layer.Material.shader != _skybox.shader)
            {
                if (layer.Weight < SKYBOX_SWITCH_WEIGHT) continue;
                _skybox.shader = layer.Material.shader;
                _skybox.CopyPropertiesFromMaterial(layer.Material);
                continue;
            }
            // 자기 자신을 시작값으로 섞지 않도록 지금까지 섞은 값을 따로 옮겨 둔다.
            _skyboxScratch.shader = _skybox.shader;
            _skyboxScratch.CopyPropertiesFromMaterial(_skybox);
            _skybox.Lerp(_skyboxScratch, layer.Material, layer.Weight);
        }
        _skyReflectionProbe.RenderProbe();
    }

    /// <summary>
    /// 프로필에 해당하는 볼륨을 찾고, 없으면 비중 0인 자식 전역 볼륨을 만든다.
    /// profile과 _volumePriority를 사용하며, 볼륨 묶음을 반환하고 처음이면 _volumes에 추가한다.
    /// </summary>
    private VolumeEntry GetVolumeEntry(VolumeProfile profile)
    {
        if (_volumes.TryGetValue(profile, out VolumeEntry entry)) return entry;

        GameObject volumeObject = new GameObject(profile.name);
        volumeObject.transform.SetParent(transform, false);
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = _volumePriority;
        volume.weight = 0f;
        volume.sharedProfile = profile;

        entry = new VolumeEntry { Volume = volume };
        _volumes.Add(profile, entry);
        return entry;
    }

    /// <summary>
    /// 기본 날씨에 고른 물고기가 부르는 비와 밤을 더하고, 고른 물고기가 막는 비와 밤은 뺀 목표 날씨를 정한다.
    /// fish와 기본 날씨를 사용하며, _targetRain과 _targetNight를 변경한다.
    /// </summary>
    private void SetTarget(FishType fish)
    {
        bool preventRain = fish != null && fish.SelectPreventsRain;
        bool preventNight = fish != null && fish.SelectPreventsNight;
        _targetRain = !preventRain && (_baseRain || (fish != null && fish.SelectCausesRain));
        _targetNight = !preventNight && (_baseNight || (fish != null && fish.SelectCausesNight));
    }

    /// <summary>
    /// 현재 비와 밤 비중을 조명, 하늘, 구름, 주변광, 안개, 후처리, 빗방울, 수면 빗물결에 반영한다.
    /// _rainWeight, _nightWeight와 저장한 낮 값을 사용하며, 씬 렌더 설정과 각 참조 대상을 변경한다.
    /// </summary>
    private void ApplyWeights()
    {
        float rain = _rainWeight;
        float night = _nightWeight;

        _sun.intensity = Mathf.Lerp(_daySunIntensity, _nightSunIntensity, night) * Mathf.Lerp(1f, _rainLightScale, rain);
        _sun.transform.rotation = Quaternion.Slerp(_daySunRotation, _nightSunRotation, night);

        // 비 하늘을 먼저 섞고 밤 하늘을 나중에 섞어, 비 오는 밤에는 밤 하늘이 이긴다.
        SetSkyboxWeight(_rainSkybox, rain);
        SetSkyboxWeight(_nightSkybox, night);
        _cloudRenderer.enabled = rain > 0f;
        _cloudMaterial.SetFloat(CLOUD_COVERAGE_ID, _rainCloudCoverage * rain);

        Color ambient = Color.Lerp(Color.white, _nightAmbient, night) * Color.Lerp(Color.white, _rainAmbient, rain);
        RenderSettings.ambientProbe = TintProbe(_dayAmbientProbe, ambient);

        RenderSettings.fog = _dayFog || rain > 0f;
        RenderSettings.fogColor = Color.Lerp(_dayFogColor, Color.Lerp(_rainFogColor, _rainNightFogColor, night), rain);
        RenderSettings.fogDensity = Mathf.Lerp(_dayFog ? _dayFogDensity : 0f, _rainFogDensity, rain);

        SetVolumeWeight(_nightProfile, night);
        SetVolumeWeight(_rainProfile, rain);

        ParticleSystem.EmissionModule emission = _rain.emission;
        emission.rateOverTimeMultiplier = _rainEmissionRate * rain;
        ParticleSystem.EmissionModule farEmission = _farRain.emission;
        farEmission.rateOverTimeMultiplier = _farRainEmissionRate * rain;
        Shader.SetGlobalFloat(RAIN_INTENSITY_ID, rain);
    }

    /// <summary>
    /// 주변광 구면 조화 계수에 채널별 색을 곱한다.
    /// probe와 tint를 사용하며, 색을 곱한 새 계수를 반환한다.
    /// </summary>
    private static SphericalHarmonicsL2 TintProbe(SphericalHarmonicsL2 probe, Color tint)
    {
        for (int i = 0; i < SH_COEFFICIENT_COUNT; i++)
        {
            probe[0, i] *= tint.r;
            probe[1, i] *= tint.g;
            probe[2, i] *= tint.b;
        }
        return probe;
    }

    /// <summary>
    /// 주어진 확률로 성공 여부를 뽑는다. 0이면 항상 실패하고 1이면 항상 성공한다.
    /// chance를 사용하며, 성공 여부를 반환한다.
    /// </summary>
    private static bool RollChance(float chance)
    {
        return chance > 0f && Random.value <= chance;
    }
}
