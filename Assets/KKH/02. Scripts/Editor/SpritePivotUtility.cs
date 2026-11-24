using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.Sprites;

public class SpritePivotUtility : EditorWindow
{
    [MenuItem("Tools/Pivot Utility/Set Selected Sprite Pivots to Center (0.5, 0.3)")]
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
                // Multiple 모드: 시트 내 모든 슬라이스 피벗 변경 (Sprite Editor 데이터 API)
                var factory = new SpriteDataProviderFactories();
                factory.Init();
                ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
                dataProvider.InitSpriteEditorDataProvider();
                SpriteRect[] rects = dataProvider.GetSpriteRects();
                foreach (SpriteRect rect in rects)
                {
                    rect.alignment = SpriteAlignment.Custom;
                    rect.pivot = new Vector2(0.5f, 0.4f);
                }
                dataProvider.SetSpriteRects(rects);
                dataProvider.Apply();
            }
            else
            {
                // Single 모드: 단일 스프라이트 피벗을 (0.5, 0.3) 변경
                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2(0.5f, 0.4f);
                importer.SetTextureSettings(settings);
            }

            // 변경사항 저장 및 다시 로드
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            count++;
        }

        AssetDatabase.Refresh();
        Debug.Log($"[SpritePivotUtility] 총 {count}개의 스프라이트 피벗을 (0.5, 0.3)로 변경했습니다.");
    }
}
