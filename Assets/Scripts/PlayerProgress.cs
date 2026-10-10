using System;

using UnityEngine;

// 물고기 도감과 조각 모양을 저장하고 최대 강화 기준의 기본 능력치를 제공한다.
public class PlayerProgress : MonoBehaviour
{
    private const string FISH_KEY_PREFIX = "SkipStoneV2.Fish.";
    private const string WALL_MESH_KEY = "SkipStoneV2.WallMesh";
    private const string ICE_MESH_KEY = "SkipStoneV2.IceMesh";

    public float PowerMultiplier => 1.4f;
    public float SpinMultiplier => 2f;
    public event Action<FishType> OnFishRegistered;

    /// <summary>
    /// 물고기가 도감에 등록되었는지 확인한다.
    /// fish를 사용하며, 한 번이라도 잡았으면 true를 반환한다.
    /// </summary>
    public bool IsFishRegistered(FishType fish)
    {
        return PlayerPrefs.GetInt(FISH_KEY_PREFIX + fish.Id, 0) > 0;
    }

    /// <summary>
    /// fish를 도감에 영구 등록하고 최초 획득일 때만 등록 이벤트를 보낸다.
    /// 이미 보유한 종류는 변경하지 않으며 신규 종류의 PlayerPrefs를 저장한다.
    /// </summary>
    public void AddFish(FishType fish)
    {
        if (IsFishRegistered(fish)) return;
        PlayerPrefs.SetInt(FISH_KEY_PREFIX + fish.Id, 1);
        PlayerPrefs.Save();
        OnFishRegistered?.Invoke(fish);
    }

    /// <summary>
    /// mesh와 scale을 사용해 마지막으로 접촉한 Wall 조각의 모양을 영구 저장한다.
    /// 저장된 크기 보정 데이터를 새 메시로 반환하며 다음 선택과 재실행에서 같은 모양을 사용한다.
    /// </summary>
    public Mesh SaveWallMesh(Mesh mesh, Vector3 scale)
    {
        WallFishMeshData data = new WallFishMeshData(mesh, scale);
        PlayerPrefs.SetString(WALL_MESH_KEY, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
        return data.ToMesh();
    }

    /// <summary>
    /// 저장된 Wall 메시 데이터를 읽어 독립된 메시로 반환한다.
    /// 입력값은 없으며 아직 접촉한 조각이 없으면 null을 반환한다.
    /// </summary>
    public Mesh LoadWallMesh()
    {
        if (!PlayerPrefs.HasKey(WALL_MESH_KEY)) return null;
        return JsonUtility.FromJson<WallFishMeshData>(PlayerPrefs.GetString(WALL_MESH_KEY)).ToMesh();
    }

    /// <summary>
    /// mesh와 scale을 사용해 마지막으로 먹은 유빙 조각의 투척용 모양을 저장한다.
    /// 벽과 독립된 저장 키를 갱신하고 크기를 보정한 새 메시를 반환한다.
    /// </summary>
    public Mesh SaveIceMesh(Mesh mesh, Vector3 scale)
    {
        WallFishMeshData data = new WallFishMeshData(mesh, scale);
        PlayerPrefs.SetString(ICE_MESH_KEY, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
        return data.ToMesh();
    }

    /// <summary>
    /// 입력값 없이 저장된 유빙 조각 데이터를 읽어 독립된 투척용 메시를 반환한다.
    /// 아직 먹은 유빙 조각이 없으면 null을 반환하며 벽 저장 데이터는 변경하지 않는다.
    /// </summary>
    public Mesh LoadIceMesh()
    {
        if (!PlayerPrefs.HasKey(ICE_MESH_KEY)) return null;
        return JsonUtility.FromJson<WallFishMeshData>(PlayerPrefs.GetString(ICE_MESH_KEY)).ToMesh();
    }

}
