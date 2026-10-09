using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Serialization;

// 자동차의 특수 동작. SPACE 판정 시(성공/실패 무관) 차체 창문에 배치된 스포너를 통해 도둑들을 밖으로 튀어나오게 소환한다.
public class Car : FishAbility
{
    [Header("스포너")]
    [FormerlySerializedAs("doduk")]
    [SerializeField]
    private GameObject _doduk;

    [FormerlySerializedAs("spawnPos")]
    [SerializeField]
    private List<DodukSpawner> _spawnPos = new List<DodukSpawner>();

    /// <summary>
    /// 현재 오브젝트와 하위 오브젝트의 모든 DodukSpawner를 수집한다.
    /// 비활성 오브젝트를 포함해 _spawnPos 리스트를 갱신한다.
    /// </summary>
    [ContextMenu("Collect Child Spawners")]
    private void CollectChildRenderers()
    {
        _spawnPos.Clear();
        GetComponentsInChildren<DodukSpawner>(true, _spawnPos);
    }

    /// <summary>
    /// 던진 상태에서 SPACE 판정에 성공했을 때 도둑들을 소환한다.
    /// context와 judge를 사용하며, SpawnDodukFromSpawner를 호출한다.
    /// </summary>
    public override void OnJudgeSuccess(ThrowContext context, SkipJudge judge)
    {
        SpawnDodukFromSpawner();
    }

    /// <summary>
    /// 등록된 스포너 중 무작위 위치를 선택하여 도둑 소환을 요청한다.
    /// _spawnPos를 사용하며, 각 DodukSpawner의 SpawnDoDuk을 호출한다.
    /// </summary>
    private void SpawnDodukFromSpawner()
    {
        if (_spawnPos == null || _spawnPos.Count == 0) return;

        // 소환할 도둑 수를 4~5마리 범위에서 랜덤으로 정한다.
        int count = Random.Range(4, 6);
        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, _spawnPos.Count);
            _spawnPos[index].SpawnDoDuk();
        }
    }
}
