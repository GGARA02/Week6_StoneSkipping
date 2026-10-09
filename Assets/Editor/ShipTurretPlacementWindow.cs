using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

using UnityEditor;
using UnityEditor.SceneManagement;

public class ShipTurretPlacementWindow : EditorWindow
{
    private const string PREFAB_DIRECTORY = "Assets/Resources/Prefabs/Ship/IcebergImprove/";
    private const string UNDO_NAME = "Randomize ShipDynamic Turrets";

    [Header("배치 대상")]
    private Transform _shipDynamic;
    private Transform _shipNormal;

    [Header("배치 설정")]
    private bool _randomizeDirection = true;
    private bool _addFlameEffects = true;

    [Header("결과")]
    private string _status;
    private MessageType _statusType;

    /// <summary>
    /// ShipDynamic 터렛 무작위 배치 창을 연다.
    /// 입력값은 없으며, 에디터 창의 제목과 최소 크기를 설정한다.
    /// </summary>
    [MenuItem("Tools/Ship/Randomize ShipDynamic Turrets")]
    public static void OpenWindow()
    {
        ShipTurretPlacementWindow window = GetWindow<ShipTurretPlacementWindow>("Ship Turrets");
        window.minSize = new Vector2(420f, 250f);
        window.Show();
    }

    /// <summary>
    /// 창이 열리면 현재 로드된 씬의 배치 대상과 기준 배를 찾는다.
    /// 입력값은 없으며, 두 배의 참조를 갱신한다.
    /// </summary>
    void OnEnable()
    {
        FindShips();
    }

    /// <summary>
    /// 배치 대상, 방향과 이펙트 설정 및 실행 결과를 표시한다.
    /// 에디터 입력을 사용하며, 설정과 무작위 배치 결과를 변경한다.
    /// </summary>
    void OnGUI()
    {
        _shipDynamic = (Transform)EditorGUILayout.ObjectField("배치 대상", _shipDynamic, typeof(Transform), true);
        _shipNormal = (Transform)EditorGUILayout.ObjectField("배치 기준", _shipNormal, typeof(Transform), true);
        if (GUILayout.Button("열린 씬에서 Ship 찾기")) FindShips();

        EditorGUILayout.Space();
        _randomizeDirection = EditorGUILayout.Toggle("방향 무작위 회전", _randomizeDirection);
        _addFlameEffects = EditorGUILayout.Toggle("Flame 화염 이펙트", _addFlameEffects);
        EditorGUILayout.HelpBox("Player가 있는 칸을 유지하고 나머지 칸의 터렛을 4종으로 고르게 섞어 교체합니다. Ctrl+Z로 되돌릴 수 있습니다.", MessageType.Info);

        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button("터렛 무작위 재배치")) RandomizeTurrets();
        }
        if (!string.IsNullOrEmpty(_status)) EditorGUILayout.HelpBox(_status, _statusType);
    }

    /// <summary>
    /// 로드된 씬에서 ShipDynamic과 같은 씬의 ShipNormal을 찾는다.
    /// 씬의 루트 오브젝트를 사용하며, 배 참조를 갱신한다.
    /// </summary>
    private void FindShips()
    {
        _shipDynamic = null;
        _shipNormal = null;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "ShipDynamic") _shipDynamic = root.transform;
            }
            if (_shipDynamic == null) continue;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "ShipNormal") _shipNormal = root.transform;
            }
            break;
        }
    }

    /// <summary>
    /// 대상과 에셋을 검증한 뒤 Player 칸을 제외한 기존 터렛을 Undo 가능한 무작위 배치로 교체한다.
    /// 두 배와 배치 설정을 사용하며, 대상 씬을 변경하고 결과를 표시한다.
    /// </summary>
    private void RandomizeTurrets()
    {
        int undoGroup = -1;
        try
        {
            if (_shipDynamic == null || _shipNormal == null)
                throw new InvalidOperationException("Ship 씬을 열고 배치 대상과 기준을 지정해 주세요.");
            if (_shipDynamic.name != "ShipDynamic" || _shipNormal.name != "ShipNormal"
                || EditorUtility.IsPersistent(_shipDynamic) || EditorUtility.IsPersistent(_shipNormal)
                || _shipDynamic.gameObject.scene != _shipNormal.gameObject.scene)
                throw new InvalidOperationException("같은 씬의 ShipDynamic과 ShipNormal을 지정해 주세요.");

            GameObject[] prefabs =
            {
                LoadPrefab("Turret_Flame"), LoadPrefab("Turret_Machinegun"),
                LoadPrefab("Turret_Missile"), LoadPrefab("Turret_Saw")
            };
            GameObject effectPrefab = _addFlameEffects ? LoadPrefab("FX_FlameThrower") : null;
            List<Transform> references = CollectReferenceTurrets();
            if (references.Count == 0)
                throw new InvalidOperationException("ShipNormal에 배치된 Turret_Flame이 없습니다.");

            List<Transform> cells = new List<Transform>();
            int playerCells = 0;
            foreach (Transform child in _shipDynamic.GetComponentsInChildren<Transform>(true))
            {
                if (!IsCell(child)) continue;
                if (HasPlayerTurret(child)) playerCells++;
                else cells.Add(child);
            }
            if (cells.Count == 0)
                throw new InvalidOperationException("재배치할 CellBody가 없습니다.");

            System.Random random = new System.Random();
            List<GameObject> choices = CreateChoices(prefabs, cells.Count, random);
            Undo.IncrementCurrentGroup();
            undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UNDO_NAME);
            int[] counts = new int[prefabs.Length];
            for (int i = 0; i < cells.Count; i++)
            {
                Transform cell = cells[i];
                Transform reference = FindClosestReference(cell, references);
                for (int j = cell.childCount - 1; j >= 0; j--)
                {
                    Transform child = cell.GetChild(j);
                    if (GetPrefabName(child).StartsWith("Turret_", StringComparison.Ordinal))
                        Undo.DestroyObjectImmediate(child.gameObject);
                }

                GameObject prefab = choices[i];
                Transform turret = CreateInstance(prefab, cell);
                SetLocalPose(turret, reference.localPosition, reference.localRotation, reference.localScale);
                if (_randomizeDirection)
                {
                    Transform barrel = FindPart(turret, "Barrel");
                    Transform rotatingPart = barrel != null ? barrel : turret;
                    Vector3 angles = rotatingPart.localEulerAngles;
                    angles.y = (float)(random.NextDouble() * 360.0);
                    Undo.RecordObject(rotatingPart, UNDO_NAME);
                    rotatingPart.localRotation = Quaternion.Euler(angles);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(rotatingPart);
                }
                if (prefab == prefabs[0] && _addFlameEffects)
                    AddFlameEffect(turret, reference, effectPrefab);
                counts[Array.IndexOf(prefabs, prefab)]++;
            }
            EditorSceneManager.MarkSceneDirty(_shipDynamic.gameObject.scene);
            Undo.CollapseUndoOperations(undoGroup);
            _status = $"{cells.Count}칸 배치 완료: Flame {counts[0]}, Machinegun {counts[1]}, Missile {counts[2]}, Saw {counts[3]}. Player {playerCells}칸 유지. 씬 저장은 Ctrl+S입니다.";
            _statusType = MessageType.Info;
        }
        catch (Exception exception)
        {
            if (undoGroup >= 0) Undo.RevertAllDownToGroup(undoGroup);
            _status = exception.Message;
            _statusType = MessageType.Error;
            Debug.LogException(exception);
        }
    }

    /// <summary>
    /// 지정한 이름의 터렛 또는 이펙트 프리팹을 불러온다.
    /// name과 공용 디렉터리를 사용하며, 프리팹을 반환하거나 누락 시 예외를 발생시킨다.
    /// </summary>
    private static GameObject LoadPrefab(string name)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_DIRECTORY + name + ".prefab");
        if (prefab == null) throw new InvalidOperationException(name + " 프리팹을 찾을 수 없습니다.");
        return prefab;
    }

    /// <summary>
    /// Transform이 배치 대상 CellBody인지 이름으로 확인한다.
    /// target의 이름을 사용하며, Top, Middle 또는 Bottom이면 true를 반환한다.
    /// </summary>
    private static bool IsCell(Transform target)
    {
        return target.name == "CellBodyTop" || target.name == "CellBodyMiddle" || target.name == "CellBodyBottom";
    }

    /// <summary>
    /// 프리팹 원본 이름 또는 일반 씬 오브젝트 이름을 구한다.
    /// target의 원본 에셋 경로를 사용하며, 프리팹 파일 이름 또는 오브젝트 이름을 반환한다.
    /// </summary>
    private static string GetPrefabName(Transform target)
    {
        string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target.gameObject);
        return string.IsNullOrEmpty(path) ? target.name : System.IO.Path.GetFileNameWithoutExtension(path);
    }

    /// <summary>
    /// CellBody 안에 Player 터렛이 있는지 확인한다.
    /// cell의 모든 자식을 사용하며, Player 프리팹 또는 이름이 있으면 true를 반환한다.
    /// </summary>
    private static bool HasPlayerTurret(Transform cell)
    {
        foreach (Transform child in cell.GetComponentsInChildren<Transform>(true))
        {
            if (GetPrefabName(child) == "Turret_Player"
                || child.name.StartsWith("Turret_Player", StringComparison.Ordinal)) return true;
        }
        return false;
    }

    /// <summary>
    /// ShipNormal에서 CellBody의 직계 자식으로 배치된 Flame 터렛을 수집한다.
    /// 기준 배의 계층을 사용하며, 위치와 배율을 복사할 참조 목록을 반환한다.
    /// </summary>
    private List<Transform> CollectReferenceTurrets()
    {
        List<Transform> references = new List<Transform>();
        foreach (Transform child in _shipNormal.GetComponentsInChildren<Transform>(true))
        {
            if (child.parent != null && IsCell(child.parent) && GetPrefabName(child) == "Turret_Flame")
                references.Add(child);
        }
        return references;
    }

    /// <summary>
    /// 두 배의 상대 위치를 비교해 대상 칸에 가장 가까운 Flame 배치를 찾는다.
    /// cell과 references를 사용하며, 로컬 위치와 배율을 복사할 터렛을 반환한다.
    /// </summary>
    private Transform FindClosestReference(Transform cell, List<Transform> references)
    {
        Vector3 position = _shipDynamic.InverseTransformPoint(cell.position);
        Transform closest = references[0];
        float distance = float.PositiveInfinity;
        foreach (Transform reference in references)
        {
            float candidate = (_shipNormal.InverseTransformPoint(reference.parent.position) - position).sqrMagnitude;
            if (candidate >= distance) continue;
            distance = candidate;
            closest = reference;
        }
        return closest;
    }

    /// <summary>
    /// 프리팹 종류별 수량 차이를 최대 한 개로 맞추고 순서를 무작위로 섞는다.
    /// prefabs, count와 random을 사용하며, 칸별로 배치할 프리팹 목록을 반환한다.
    /// </summary>
    private static List<GameObject> CreateChoices(GameObject[] prefabs, int count, System.Random random)
    {
        List<GameObject> choices = new List<GameObject>();
        int offset = random.Next(prefabs.Length);
        for (int i = 0; i < count; i++) choices.Add(prefabs[(i + offset) % prefabs.Length]);
        for (int i = choices.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            GameObject temporary = choices[i];
            choices[i] = choices[j];
            choices[j] = temporary;
        }
        return choices;
    }

    /// <summary>
    /// 프리팹 연결을 유지하며 자식 인스턴스를 만들고 Undo에 등록한다.
    /// prefab과 parent를 사용하며, 생성한 오브젝트의 Transform을 반환한다.
    /// </summary>
    private static Transform CreateInstance(GameObject prefab, Transform parent)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        Undo.RegisterCreatedObjectUndo(instance, UNDO_NAME);
        return instance.transform;
    }

    /// <summary>
    /// 인스턴스의 로컬 위치, 회전과 배율을 설정하고 프리팹 변경사항을 기록한다.
    /// target, position, rotation, scale을 사용하며, Transform과 Undo 기록을 변경한다.
    /// </summary>
    private static void SetLocalPose(Transform target, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        Undo.RecordObject(target, UNDO_NAME);
        target.localPosition = position;
        target.localRotation = rotation;
        target.localScale = scale;
        PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }

    /// <summary>
    /// Flame의 Cylinder (6)에 기준 배와 같은 화염 이펙트를 배치한다.
    /// turret, reference, effectPrefab을 사용하며, 포구에 Undo 가능한 이펙트 인스턴스를 추가한다.
    /// </summary>
    private static void AddFlameEffect(Transform turret, Transform reference, GameObject effectPrefab)
    {
        Transform cylinder = FindPart(turret, "Cylinder (6)");
        if (cylinder == null) throw new InvalidOperationException("Flame의 Cylinder (6)을 찾을 수 없습니다.");
        Transform referenceCylinder = FindPart(reference, "Cylinder (6)");
        Transform referenceEffect = referenceCylinder != null ? FindPart(referenceCylinder, "FX_FlameThrower") : null;
        Transform effect = CreateInstance(effectPrefab, cylinder);
        if (referenceEffect != null)
            SetLocalPose(effect, referenceEffect.localPosition, referenceEffect.localRotation, referenceEffect.localScale);
        else
            SetLocalPose(effect, new Vector3(0f, 1f, 0f), Quaternion.Euler(-90f, 0f, 0f), new Vector3(1f, 1f, 10f));
    }

    /// <summary>
    /// 비활성 오브젝트를 포함해 이름이 일치하는 파츠를 찾는다.
    /// root와 name을 사용하며, 해당 Transform 또는 없으면 null을 반환한다.
    /// </summary>
    private static Transform FindPart(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name) return child;
        }
        return null;
    }
}
