using System.IO;
using UnityEditor;
using UnityEngine;

namespace KKH.Editor
{
    /// <summary>
    /// 적 프리팹의 태그를 "Enemy"로 설정하고, 
    /// SpriteRenderer의 크기에 맞춰 BoxCollider2D를 자동으로 추가/조절해주는 에디터 유틸리티 창.
    /// </summary>
    public class EnemyPrefabUtility : EditorWindow
    {
        private string prefabFolderPath = "Assets/KKH/03. Prefab";
        private bool includeSubfolders = true;

        [MenuItem("Tools/60s Dungeon/Enemy Prefab Utility")]
        public static void ShowWindow()
        {
            GetWindow<EnemyPrefabUtility>("Enemy Prefab Utility");
        }

        private void OnGUI()
        {
            GUILayout.Label("적 프리팹 일괄 수정 도구", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            GUILayout.BeginVertical("box");
            prefabFolderPath = EditorGUILayout.TextField("프리팹 폴더 경로", prefabFolderPath);
            includeSubfolders = EditorGUILayout.Toggle("하위 폴더 포함", includeSubfolders);
            GUILayout.EndVertical();

            EditorGUILayout.Space();

            if (GUILayout.Button("프리팹 태그 및 콜라이더 일괄 변경", GUILayout.Height(40)))
            {
                ProcessPrefabs();
            }
        }

        private void ProcessPrefabs()
        {
            if (!Directory.Exists(prefabFolderPath))
            {
                EditorUtility.DisplayDialog("오류", $"지정한 경로가 존재하지 않습니다: {prefabFolderPath}", "확인");
                return;
            }

            // 1. "Enemy" 태그 프로젝트에 등록 확인 및 없으면 추가
            RegisterTag("Enemy");

            // 2. 프리팹 검색
            string searchOption = includeSubfolders ? "*.prefab" : "";
            string[] guids;


            if (includeSubfolders)
            {
                guids = AssetDatabase.FindAssets("t:Prefab", new string[] { prefabFolderPath });
            }
            else
            {
                // 현재 폴더에 있는 파일만 필터
                var files = Directory.GetFiles(prefabFolderPath, "*.prefab", SearchOption.TopDirectoryOnly);
                guids = new string[files.Length];
                for (int i = 0; i < files.Length; i++)
                {
                    string assetPath = files[i].Replace("\\", "/");
                    guids[i] = AssetDatabase.AssetPathToGUID(assetPath);
                }
            }

            int processedCount = 0;
            int totalCount = guids.Length;

            for (int i = 0; i < totalCount; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path)) continue;

                // 진행도 표시
                EditorUtility.DisplayProgressBar("프리팹 처리 중", $"{Path.GetFileName(path)} 수정 중... ({i + 1}/{totalCount})", (float)i / totalCount);

                // 프리팹 에디팅용 로드
                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
                if (prefabRoot != null)
                {
                    bool isModified = false;

                    // 1) 태그를 "Enemy"로 지정
                    if (prefabRoot.tag != "Enemy")
                    {
                        prefabRoot.tag = "Enemy";
                        isModified = true;
                    }

                    // 2) SpriteRenderer를 찾아서 BoxCollider2D 크기 맞춤
                    SpriteRenderer sr = prefabRoot.GetComponentInChildren<SpriteRenderer>();
                    if (sr != null && sr.sprite != null)
                    {
                        BoxCollider2D collider = prefabRoot.GetComponent<BoxCollider2D>();
                        if (collider == null)
                        {
                            collider = prefabRoot.AddComponent<BoxCollider2D>();
                        }

                        // Sprite의 로컬 바운드 크기 획득
                        Bounds spriteBounds = sr.sprite.bounds;
                        Vector2 spriteSize = spriteBounds.size;
                        Vector2 spriteCenter = spriteBounds.center;

                        // 콜라이더 사이즈와 오프셋 업데이트
                        if (collider.size != spriteSize || collider.offset != spriteCenter)
                        {
                            collider.size = spriteSize;
                            collider.offset = spriteCenter;
                            isModified = true;
                        }
                    }

                    // 3) EnemyBase를 찾아서 fragmentsOfLifeItem 필드 자동 연결
                    EnemyBase enemyComponent = prefabRoot.GetComponent<EnemyBase>();
                    if (enemyComponent != null)
                    {
                        ItemBase lifeShard = AssetDatabase.LoadAssetAtPath<ItemBase>("Assets/YJW/03.ItemDatas/etc/FragmentsOfLife.asset");
                        if (lifeShard != null)
                        {
                            SerializedObject serializedEnemy = new SerializedObject(enemyComponent);
                            SerializedProperty prop = serializedEnemy.FindProperty("fragmentsOfLifeItem");
                            if (prop != null && prop.objectReferenceValue != lifeShard)
                            {
                                prop.objectReferenceValue = lifeShard;
                                serializedEnemy.ApplyModifiedProperties();
                                isModified = true;
                            }
                        }
                    }

                    if (isModified)
                    {
                        // 변경 내용 저장
                        PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                        processedCount++;
                    }

                    // 메모리 해제 및 언로드
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("완료", $"총 {totalCount}개의 프리팹 중 {processedCount}개의 적 프리팹이 성공적으로 수정 및 저장되었습니다.", "확인");
        }

        /// <summary>
        /// 프로젝트의 TagManager를 조회해 특정 태그가 없으면 자동으로 생성함.
        /// </summary>
        private static void RegisterTag(string tagName)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return;

            SerializedObject tagManager = new SerializedObject(assets[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            // 이미 등록되어 있는지 확인
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tagName)
                {
                    return; // 이미 존재하므로 그냥 리턴
                }
            }

            // 없으면 맨 앞에 추가
            tagsProp.InsertArrayElementAtIndex(0);
            tagsProp.GetArrayElementAtIndex(0).stringValue = tagName;
            tagManager.ApplyModifiedProperties();
            tagManager.Update();
        }
    }
}
