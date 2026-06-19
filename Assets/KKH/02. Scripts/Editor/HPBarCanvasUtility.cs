using UnityEditor;
using UnityEngine;

namespace KKH.Editor
{
    /// <summary>
    /// 프로젝트 뷰에서 프리팹을 선택하여 하위의 "HP_Bar_Canvas" 오브젝트의 Canvas 컴포넌트 
    /// Order in Layer (sortingOrder) 값을 8로 일괄 변경해주는 에디터 유틸리티 툴입니다.
    /// </summary>
    public static class HPBarCanvasUtility
    {
        [MenuItem("Tools/60s Dungeon/Set Selected HP Bar Canvas Order to 8")]
        public static void SetHPBarCanvasOrderTo8()
        {
            // 선택된 게임 오브젝트 목록 가져오기 (프로젝트 뷰의 프리팹 에셋 및 하이어라키 오브젝트 호환)
            GameObject[] selectedObjects = Selection.gameObjects;
            if (selectedObjects.Length == 0)
            {
                EditorUtility.DisplayDialog("경고", "수정할 프리팹이나 오브젝트를 먼저 선택해 주세요.", "확인");
                return;
            }

            int successCount = 0;

            foreach (GameObject obj in selectedObjects)
            {
                string assetPath = AssetDatabase.GetAssetPath(obj);

                // 1. 프로젝트 뷰에 있는 프리팹 파일(.prefab)일 경우
                if (!string.IsNullOrEmpty(assetPath) && assetPath.EndsWith(".prefab"))
                {
                    // 프리팹 내용 수정 모드로 로드
                    GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
                    if (prefabRoot == null) continue;

                    bool isModified = ModifyHPBarCanvasSortingOrder(prefabRoot, 8);
                    if (isModified)
                    {
                        // 변경점 저장 후 언로드
                        PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
                        successCount++;
                    }
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
                // 2. 현재 씬 하이어라키에 배치된 오브젝트일 경우
                else
                {
                    bool isModified = ModifyHPBarCanvasSortingOrder(obj, 8);
                    if (isModified)
                    {
                        // 언두(Undo) 등록
                        Undo.RecordObject(obj, "Set HP Bar Canvas Order to 8");
                        successCount++;
                    }
                }
            }

            // 애셋 리프레시 및 저장
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("완료", $"선택한 항목 중 {successCount}개의 오브젝트/프리팹 내부 'HP_Bar_Canvas'의 Order in Layer를 8로 변경 완료했습니다.", "확인");
        }

        /// <summary>
        /// 지정한 루트 오브젝트 하위에서 "HP_Bar_Canvas"라는 이름을 가진 캔버스를 찾아 Sorting Order를 변경합니다.
        /// </summary>
        private static bool ModifyHPBarCanvasSortingOrder(GameObject root, int order)
        {
            bool modified = false;

            // 하위 모든 트랜스폼 탐색 (비활성화 오브젝트 포함)
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.name == "HP_Bar_Canvas")
                {
                    Canvas canvas = child.GetComponent<Canvas>();
                    if (canvas != null)
                    {
                        if (canvas.sortingOrder != order)
                        {
                            canvas.sortingOrder = order;
                            EditorUtility.SetDirty(canvas); // 변경 플래그 표시
                            modified = true;
                        }
                    }
                }
            }

            return modified;
        }
    }
}
