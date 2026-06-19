using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using EnemySystem;

namespace EditorSystem
{
    public static class SpawnConfigUtility
    {
        [MenuItem("Tools/60s Dungeon/Fix EnemySpawner SpawnConfigs in Scene")]
        public static void FixSpawnerConfigsInScene()
        {
            // 1. Find EnemySpawner in active scene
            EnemySpawner spawner = Object.FindFirstObjectByType<EnemySpawner>();
            if (spawner == null)
            {
                EditorUtility.DisplayDialog("Fix EnemySpawner", "Active scene에 EnemySpawner가 존재하지 않습니다.", "OK");
                return;
            }

            // 2. Load the Area spawn configs
            string[] paths = new string[]
            {
                "Assets/KKH/04. SOData/SpawnData/Area1SpawnConfig.asset",
                "Assets/KKH/04. SOData/SpawnData/Area2SpawnConfig.asset",
                "Assets/KKH/04. SOData/SpawnData/Area3SpawnConfig.asset"
            };

            List<FloorSpawnConfig> configs = new List<FloorSpawnConfig>();
            foreach (var path in paths)
            {
                FloorSpawnConfig config = AssetDatabase.LoadAssetAtPath<FloorSpawnConfig>(path);
                if (config == null)
                {
                    Debug.LogError($"SpawnConfigUtility: '{path}' 경로의 FloorSpawnConfig 로드 실패!");
                    EditorUtility.DisplayDialog("Fix EnemySpawner", $"오류: '{path}'를 로드할 수 없습니다.\n프로젝트 구조를 확인해주세요.", "OK");
                    return;
                }
                configs.Add(config);
            }

            // 3. Apply to EnemySpawner
            // We use SerializedObject to support undo and ensure persistent save
            SerializedObject serializedSpawner = new SerializedObject(spawner);
            SerializedProperty floorConfigsProp = serializedSpawner.FindProperty("floorConfigs");

            if (floorConfigsProp == null)
            {
                Debug.LogError("SpawnConfigUtility: EnemySpawner에서 'floorConfigs' 필드를 찾을 수 없습니다.");
                return;
            }

            floorConfigsProp.ClearArray();
            for (int i = 0; i < configs.Count; i++)
            {
                floorConfigsProp.InsertArrayElementAtIndex(i);
                floorConfigsProp.GetArrayElementAtIndex(i).objectReferenceValue = configs[i];
            }

            serializedSpawner.ApplyModifiedProperties();

            // 4. Mark dirty and save scene
            EditorUtility.SetDirty(spawner);
            EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
            
            Debug.Log($"SpawnConfigUtility: '{spawner.gameObject.scene.name}' 씬의 EnemySpawner 설정을 완료했습니다 (Area 1, 2, 3 설정됨).");
            EditorUtility.DisplayDialog("Fix EnemySpawner", $"'{spawner.gameObject.scene.name}' 씬의 EnemySpawner 설정이 정상적으로 완료되었습니다!\n(Area 1, 2, 3 Configs가 등록되었습니다)", "OK");
        }
    }
}
