using UnityEngine;
using UnityEditor;

public class SpritePivotUtility : EditorWindow
{
    [MenuItem("Tools/Pivot Utility/Set Selected Sprite Pivots to Center (0.5, 0.5)")]
    public static void SetSelectedPivotsToCenter()
    {
        // 프로젝트 창에서 선택된 Texture2D 에셋들을 가져옵니다.
        Object[] selectedObjects = Selection.GetFiltered<Texture2D>(SelectionMode.Assets);
        
        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("선택된 스프라이트(Texture2D) 에셋이 없습니다. 프로젝트 창에서 파일을 선택해주세요.");
            return;
        }

        int count = 0;
        
        foreach (Object obj in selectedObjects)
        {
            string assetPath = AssetDatabase.GetAssetPath(obj);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            
            if (importer == null) continue;

            // Texture Type을 Sprite로 설정
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
            }

            // Sprite Import Mode에 맞춰 처리
            if (importer.spriteImportMode == SpriteImportMode.Multiple)
            {
                // Multiple 모드: 시트 내 모든 개별 슬라이스의 피벗을 Center로 변경
                var spritesheet = importer.spritesheet;
                for (int i = 0; i < spritesheet.Length; i++)
                {
                    spritesheet[i].alignment = (int)SpriteAlignment.Center;
                }
                importer.spritesheet = spritesheet;
            }
            else
            {
                // Single 모드: 단일 스프라이트 피벗을 Center로 변경
                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                importer.SetTextureSettings(settings);
            }

            // 변경사항 저장 및 다시 로드
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            count++;
        }

        AssetDatabase.Refresh();
        Debug.Log($"[SpritePivotUtility] 총 {count}개의 스프라이트 피벗을 Center(0.5, 0.5)로 변경했습니다.");
    }
}
