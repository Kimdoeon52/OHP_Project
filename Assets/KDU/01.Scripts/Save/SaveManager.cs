using System.Collections.Generic;
using System.IO;
using PrototypeRT;
using UnityEngine;

// 단일 런 세이브. 층 진입 시점에 1번 write. persistentDataPath/run.json.
public static class SaveManager
{
    private const string FileName = "run.json";

    private static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    // 반지처럼 슬롯당 칸이 여럿인 장비를 위한 (슬롯, 칸수) 정의
    private static readonly (EquipmentSlot slot, int count)[] EquipSlots =
    {
        (EquipmentSlot.Weapon, 1),
        (EquipmentSlot.Armor, 1),
        (EquipmentSlot.Ring, 2),
        (EquipmentSlot.Artifact, 1),
        (EquipmentSlot.Rune, 1),
    };

    public static bool HasSave()
    {
        return File.Exists(SavePath);
    }

    public static void Delete()
    {
        if (File.Exists(SavePath))
            File.Delete(SavePath);
    }

    // 현재 상태 -> 스냅샷 -> json write. 층 진입 시점 호출.
    public static void Save()
    {
        RunSnapshot snap = BuildSnapshot();
        if (snap == null)
            return;

        string json = JsonUtility.ToJson(snap, true);
        File.WriteAllText(SavePath, json);
    }

    // json read -> 스냅샷. 없거나 깨지면 null.
    public static RunSnapshot Load()
    {
        if (!File.Exists(SavePath))
            return null;

        try
        {
            string json = File.ReadAllText(SavePath);
            return JsonUtility.FromJson<RunSnapshot>(json);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SaveManager] 로드 실패: {ex.Message}");
            return null;
        }
    }

    // 현재 씬 상태에서 스냅샷 구성 (읽기 전용)
    private static RunSnapshot BuildSnapshot()
    {
        var snap = new RunSnapshot();

        RunState run = RunState.Instance;
        if (run == null)
        {
            Debug.LogError("[SaveManager] RunState 없음 — 저장 취소");
            return null;
        }

        snap.seed = run.RunSeed;
        snap.depth = run.CurrentDepth;
        snap.challenges = BuildChallenges();

        snap.player = BuildPlayerSnapshot();
        snap.inventory = BuildInventory();
        snap.equipped = BuildEquipped();

        return snap;
    }

    private static List<int> BuildChallenges()
    {
        var list = new List<int>();

        ChallengeState cs = ChallengeState.Instance;
        if (cs == null)
            return list;

        foreach (ChallengeType t in cs.Active)
            list.Add((int)t);

        return list;
    }

    private static PlayerSnapshot BuildPlayerSnapshot()
    {
        var p = new PlayerSnapshot();

        NYHPlayerStats stats = NYHPlayerStats.Instance;
        if (stats != null)
        {
            p.maxHealth = stats.MaxHealth;
            p.strength = stats.Strength;
            p.moveSpeed = stats.MoveSpeed;
            p.attackSpeed = stats.AttackSpeed;
            p.accuracy = stats.Accuracy;
            p.defense = stats.Defense;
            p.evasion = stats.Evasion;
            p.hunger = stats.Hunger;
        }

        GridPlayerHealth health = GridPlayerHealth.Instance;
        if (health != null)
        {
            p.currentHp = health.CurrentHp;
            p.potionCount = health.PotionCount;
        }

        PlayerLevelSystem lvl = PlayerLevelSystem.Instance;
        if (lvl != null)
        {
            p.level = lvl.Level;
            p.exp = lvl.CurrentExp;
        }

        return p;
    }

    private static List<ItemDTO> BuildInventory()
    {
        var list = new List<ItemDTO>();

        InventoryController inv = Object.FindFirstObjectByType<InventoryController>();
        if (inv == null)
            return list;

        foreach (InventoryItem view in inv.Items)
        {
            if (view == null || view.ItemInstance == null)
                continue;

            list.Add(SaveMapper.ToDTO(view.ItemInstance, view.Count));
        }

        return list;
    }

    private static List<EquipDTO> BuildEquipped()
    {
        var list = new List<EquipDTO>();

        NYHEquipmentController equip = Object.FindFirstObjectByType<NYHEquipmentController>();
        if (equip == null)
            return list;

        foreach (var (slot, count) in EquipSlots)
        {
            for (int i = 0; i < count; i++)
            {
                if (!equip.TryGetEquipped(slot, i, out NYHEquipmentItemInstance item) || item == null)
                    continue;

                // SourceInstance가 강화/저주 상태를 들고 있음. 없으면 Data로 기본 인스턴스.
                ItemInstance src = item.SourceInstance ?? (item.Data != null ? item.Data.CreateInstance() : null);
                if (src == null)
                    continue;

                list.Add(new EquipDTO
                {
                    slot = (int)slot,
                    slotIndex = i,
                    item = SaveMapper.ToDTO(src, 1)
                });
            }
        }

        return list;
    }
}
