using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Steam 제외 빌드 (GGC 등) — NO_STEAM 심볼로 빌드 후 Steam 파일 제거
public static class NoSteamBuild
{
    private const string Define = "NO_STEAM";
    private const string ToggleMenu = "Tools/KDU/에디터 NO_STEAM 미리보기";

    // 빌드 결과에서 지울 Steam 파일
    private static readonly string[] SteamFiles = { "steam_api*.dll", "steam_appid.txt", "Steamworks.NET.txt" };

    [MenuItem("Tools/KDU/Steam 제외 빌드 (GGC)")]
    private static void Build()
    {
        string path = EditorUtility.SaveFilePanel("Steam 제외 빌드", "", PlayerSettings.productName, "exe");
        if (string.IsNullOrEmpty(path))
            return;

        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = path,
            target = BuildTarget.StandaloneWindows64,
            extraScriptingDefines = new[] { Define },
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"[NoSteamBuild] 빌드 실패: {report.summary.result}");
            return;
        }

        string root = Path.GetDirectoryName(path);
        string plugins = Path.Combine(root, Path.GetFileNameWithoutExtension(path) + "_Data", "Plugins");
        RemoveSteamFiles(root, SearchOption.TopDirectoryOnly);
        RemoveSteamFiles(plugins, SearchOption.AllDirectories);

        Debug.Log($"[NoSteamBuild] 완료: {path}");
        EditorUtility.RevealInFinder(path);
    }

    // Steam 네이티브 DLL·부가 파일 삭제 (exe 옆, _Data/Plugins 안만)
    private static void RemoveSteamFiles(string dir, SearchOption option)
    {
        if (!Directory.Exists(dir))
            return;

        foreach (string pattern in SteamFiles)
        {
            foreach (string file in Directory.GetFiles(dir, pattern, option))
                File.Delete(file);
        }
    }

    // ProjectSettings 디파인에 NO_STEAM 켜기/끄기
    [MenuItem(ToggleMenu)]
    private static void ToggleEditorDefine()
    {
        List<string> defines = GetDefines();
        if (!defines.Remove(Define))
            defines.Add(Define);

        PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, string.Join(";", defines));
    }

    [MenuItem(ToggleMenu, true)]
    private static bool ToggleEditorDefineValidate()
    {
        Menu.SetChecked(ToggleMenu, GetDefines().Contains(Define));
        return true;
    }

    private static List<string> GetDefines()
    {
        return PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone).Split(';').Where(d => d.Length > 0).ToList();
    }
}
