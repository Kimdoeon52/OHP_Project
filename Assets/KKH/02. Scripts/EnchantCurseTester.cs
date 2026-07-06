using UnityEngine;
using System.Collections.Generic;
using PrototypeRT;

/// <summary>
/// 인챈트(Magic)와 저주(Curse) 시스템을 실시간으로 테스트하기 위한 개발자용 테스트 스크립트입니다.
/// - 'I' 키: 플레이어 주변 빈 격자 셀에 Enchanter(GridEnchanter)를 스폰합니다.
/// - 'Up/Down' 방향키: 장착 중인 무기(Weapon Slot)에 적용할 저주를 리스트 내에서 순환 갱신합니다.
/// - 'Left/Right' 방향키: 장착 중인 무기(Weapon Slot)에 적용할 인챈트(Magic)를 리스트 내에서 순환 갱신합니다.
/// - IMGUI를 통해 현재 선택한 및 착용한 저주/인챈트 정보를 화면 좌측 상단에 띄워줍니다.
/// </summary>
public class EnchantCurseTester : MonoBehaviour
{
    [Header("Enchanter 스폰 설정")]
    [Tooltip("스폰할 Enchanter 프리팹 (GridEnchanter.prefab)")]
    [SerializeField] private GameObject enchanterPrefab;

    [Header("저주 및 인챈트 목록")]
    [Tooltip("테스트할 저주 데이터 에셋 목록")]
    [SerializeField] private List<CurseData> curseList = new List<CurseData>();
    [Tooltip("테스트할 인챈트 데이터 에셋 목록")]
    [SerializeField] private List<MagicData> magicList = new List<MagicData>();

    [Header("대상 설정")]
    [Tooltip("저주와 인챈트를 적용할 장비 슬롯 (기본값: Weapon)")]
    [SerializeField] private EquipmentSlot targetSlot = EquipmentSlot.Weapon;

    private int _currentCurseIndex = -1; // -1: 저주 없음
    private int _currentMagicIndex = -1; // -1: 인챈트 없음

    private void Update()
    {
        // 1. 'I' 키 입력 시 Enchanter 스폰
        if (Input.GetKeyDown(KeyCode.I))
        {
            SpawnEnchanter();
        }

        // 2. 방향키 입력 시 저주/인챈트 변경
        HandleArrowInput();
    }

    private void SpawnEnchanter()
    {
        var player = FindFirstObjectByType<GridPlayer>();
        if (player == null)
        {
            Debug.LogWarning("[EnchantCurseTester] 씬에서 Player (GridPlayer)를 찾을 수 없습니다.");
            return;
        }

        if (enchanterPrefab == null)
        {
            Debug.LogWarning("[EnchantCurseTester] Enchanter 프리팹이 할당되지 않았습니다. 인스펙터를 확인하세요.");
            return;
        }

        Vector2Int playerPos = player.GridPosition;
        Vector2Int spawnPos = FindSpawnPosition(playerPos);

        GameObject go = Instantiate(enchanterPrefab);
        EnchantAltar altar = go.GetComponent<EnchantAltar>();
        if (altar != null)
        {
            int depth = MapGenerator.Instance != null ? MapGenerator.Instance.CurrentDepth : 1;
            altar.Initialize(spawnPos, depth);
            Debug.Log($"[EnchantCurseTester] Enchanter spawned and initialized at {spawnPos} (Depth: {depth})");
        }
        else
        {
            // EnchantAltar 컴포넌트가 없는 경우 단순히 월드 좌표 설정
            Vector3 worldPos = GridSystem.Instance != null 
                ? GridSystem.Instance.GridToWorld(spawnPos) 
                : new Vector3(spawnPos.x, spawnPos.y, 0);
            go.transform.position = worldPos;
            Debug.Log($"[EnchantCurseTester] Enchanter spawned without EnchantAltar component at {spawnPos}");
        }
    }

    private Vector2Int FindSpawnPosition(Vector2Int center)
    {
        // 플레이어 주변 8방향 타일 검사하여 비어있는 통행 가능 타일 탐색
        Vector2Int[] directions = new Vector2Int[]
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1)
        };

        if (GridSystem.Instance == null)
            return center + Vector2Int.up;

        foreach (var dir in directions)
        {
            Vector2Int candidate = center + dir;
            if (GridSystem.Instance.IsWalkable(candidate) && !GridSystem.Instance.IsCellOccupied(candidate))
            {
                return candidate;
            }
        }

        // 주변 타일 중 적당한 빈 공간이 없는 경우 플레이어 바로 위 타일 반환
        return center + Vector2Int.up;
    }

    private void HandleArrowInput()
    {
        bool stateChanged = false;

        // Up/Down: 저주 변경
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            CycleCurse(1);
            stateChanged = true;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            CycleCurse(-1);
            stateChanged = true;
        }

        // Left/Right: 인챈트 변경
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            CycleMagic(1);
            stateChanged = true;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            CycleMagic(-1);
            stateChanged = true;
        }

        if (stateChanged)
        {
            ApplyToEquippedItem();
        }
    }

    private void CycleCurse(int direction)
    {
        if (curseList == null || curseList.Count == 0)
        {
            Debug.LogWarning("[EnchantCurseTester] 저주 목록(curseList)이 비어 있어 순환할 수 없습니다.");
            return;
        }

        // 범위: -1 (없음) ~ curseList.Count - 1
        _currentCurseIndex += direction;
        if (_currentCurseIndex < -1)
        {
            _currentCurseIndex = curseList.Count - 1;
        }
        else if (_currentCurseIndex >= curseList.Count)
        {
            _currentCurseIndex = -1;
        }

        string curseName = _currentCurseIndex == -1 ? "None (Purified)" : curseList[_currentCurseIndex].name;
        Debug.Log($"[EnchantCurseTester] Selected Curse for testing: {curseName} (Index: {_currentCurseIndex})");
    }

    private void CycleMagic(int direction)
    {
        if (magicList == null || magicList.Count == 0)
        {
            Debug.LogWarning("[EnchantCurseTester] 인챈트 목록(magicList)이 비어 있어 순환할 수 없습니다.");
            return;
        }

        // 범위: -1 (없음) ~ magicList.Count - 1
        _currentMagicIndex += direction;
        if (_currentMagicIndex < -1)
        {
            _currentMagicIndex = magicList.Count - 1;
        }
        else if (_currentMagicIndex >= magicList.Count)
        {
            _currentMagicIndex = -1;
        }

        string magicName = _currentMagicIndex == -1 ? "None" : magicList[_currentMagicIndex].name;
        Debug.Log($"[EnchantCurseTester] Selected Magic for testing: {magicName} (Index: {_currentMagicIndex})");
    }

    private void ApplyToEquippedItem()
    {
        var equipmentController = FindFirstObjectByType<NYHEquipmentController>();
        if (equipmentController == null)
        {
            Debug.LogWarning("[EnchantCurseTester] NYHEquipmentController를 씬에서 찾을 수 없습니다.");
            return;
        }

        if (equipmentController.TryGetEquipped(targetSlot, out NYHEquipmentItemInstance equippedItem))
        {
            if (equippedItem.SourceInstance is EquipmentInstance equipInstance)
            {
                // 1. 기존 저주 해제 (스탯 복구)
                if (equipInstance.curse != null)
                {
                    equipInstance.curse.OnUnequip();
                    Debug.Log($"[EnchantCurseTester] Unequipped existing curse: {equipInstance.curse.CurseName}");
                }

                // 2. 새 저주 및 인챈트 가져오기
                CurseData newCurse = (_currentCurseIndex >= 0 && _currentCurseIndex < curseList.Count) 
                    ? curseList[_currentCurseIndex] 
                    : null;
                MagicData newMagic = (_currentMagicIndex >= 0 && _currentMagicIndex < magicList.Count) 
                    ? magicList[_currentMagicIndex] 
                    : null;

                // 3. 저주 설정 및 OnEquip 호출
                equipInstance.curse = newCurse;
                if (newCurse != null)
                {
                    newCurse.OnEquip();
                }

                // 4. 인챈트(마법) 설정
                equipInstance.magic = newMagic;

                // 5. 저주 상태 플래그 갱신
                equippedItem.SetCursed(newCurse != null);

                // 6. UI 및 플레이어 최종 스탯 갱신 통지
                if (equipmentController.TryGetEquippedInventoryItem(targetSlot, 0, out InventoryItem inventoryItem))
                {
                    equipmentController.NotifyEquippedInventoryItemStateChanged(inventoryItem);
                }

                string curseLog = newCurse != null ? newCurse.CurseName : "None";
                string magicLog = newMagic != null ? newMagic.MagicName : "None";
                Debug.Log($"[EnchantCurseTester] Applied to {targetSlot}: Curse={curseLog}, Magic={magicLog}");
            }
            else
            {
                Debug.LogWarning($"[EnchantCurseTester] 장착 중인 아이템 {equippedItem.Data.Name}이 EquipmentInstance가 아닙니다.");
            }
        }
        else
        {
            Debug.LogWarning($"[EnchantCurseTester] {targetSlot} 슬롯에 장착된 장비가 없습니다. 저주/인챈트를 적용할 수 없습니다.");
        }
    }

    private void OnGUI()
    {
        // 화면에 보여줄 개발자용 IMGUI 디버그 박스
        GUI.color = Color.white;
        Rect rect = new Rect(20, 20, 360, 240);
        GUI.Box(rect, "Enchant & Curse Tester (Dev Mode)");

        GUILayout.BeginArea(new Rect(30, 45, 340, 205));
        
        GUILayout.Label("<b>Controls:</b>");
        GUILayout.Label(" - [ <b>I</b> Key ] : Spawn Enchanter Altar");
        GUILayout.Label(" - [ <b>Up/Down Arrow</b> ] : Cycle Curse in list");
        GUILayout.Label(" - [ <b>Left/Right Arrow</b> ] : Cycle Enchant in list");
        
        GUILayout.Space(8);

        // 현재 테스터 상에서 선택된 항목
        string selectedCurseName = (_currentCurseIndex >= 0 && _currentCurseIndex < curseList.Count) ? curseList[_currentCurseIndex].name : "None (Purified)";
        string selectedMagicName = (_currentMagicIndex >= 0 && _currentMagicIndex < magicList.Count) ? magicList[_currentMagicIndex].name : "None";

        GUILayout.Label($"Selected Curse: <color=yellow>{selectedCurseName}</color> (Idx: {_currentCurseIndex})");
        GUILayout.Label($"Selected Magic: <color=cyan>{selectedMagicName}</color> (Idx: {_currentMagicIndex})");

        GUILayout.Space(8);

        // 현재 실제 장착된 장비의 저주/인챈트 정보
        var equipmentController = FindFirstObjectByType<NYHEquipmentController>();
        if (equipmentController != null && equipmentController.TryGetEquipped(targetSlot, out NYHEquipmentItemInstance equippedItem))
        {
            if (equippedItem.SourceInstance is EquipmentInstance equipInstance)
            {
                string itemCurse = equipInstance.curse != null ? equipInstance.curse.CurseName : "None";
                string itemMagic = equipInstance.magic != null ? equipInstance.magic.MagicName : "None";
                GUILayout.Label($"Equipped Item: <b>{equippedItem.Data.Name}</b> (+{equipInstance.enhanceLevel})");
                GUILayout.Label($" - Current Curse: <color=orange>{itemCurse}</color>");
                GUILayout.Label($" - Current Magic: <color=#00ffff>{itemMagic}</color>");
            }
            else
            {
                GUILayout.Label("Equipped item does not support Curses/Enchants.");
            }
        }
        else
        {
            GUILayout.Label("<color=red>No item equipped in Weapon slot.</color>");
        }

        GUILayout.EndArea();
    }

#if UNITY_EDITOR
    private void Reset()
    {
        // Reset 함수는 컴포넌트 추가 또는 Reset 버튼 클릭 시 에디터 환경에서 호출됩니다.
        // 프로젝트 폴더에서 저주(CurseData)와 인챈트(MagicData) 에셋들을 찾아 자동으로 리스트를 채웁니다.
        curseList.Clear();
        string[] curseGuids = UnityEditor.AssetDatabase.FindAssets("t:CurseData");
        foreach (string guid in curseGuids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            CurseData curse = UnityEditor.AssetDatabase.LoadAssetAtPath<CurseData>(path);
            if (curse != null) curseList.Add(curse);
        }

        magicList.Clear();
        string[] magicGuids = UnityEditor.AssetDatabase.FindAssets("t:MagicData");
        foreach (string guid in magicGuids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            MagicData magic = UnityEditor.AssetDatabase.LoadAssetAtPath<MagicData>(path);
            if (magic != null) magicList.Add(magic);
        }

        // GridEnchanter 프리팹 자동 감지
        string[] enchanterGuids = UnityEditor.AssetDatabase.FindAssets("GridEnchanter");
        foreach (string guid in enchanterGuids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".prefab"))
            {
                enchanterPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                break;
            }
        }

        Debug.Log($"[EnchantCurseTester] Auto-populated {curseList.Count} curses, {magicList.Count} magics, and Enchanter Prefab!");
    }
#endif
}
