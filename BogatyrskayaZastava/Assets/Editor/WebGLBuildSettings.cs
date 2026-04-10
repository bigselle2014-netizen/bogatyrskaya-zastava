using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// Отключает gzip-сжатие для WebGL на GitHub Pages.
/// GitHub Pages не отдаёт Content-Encoding: gzip, поэтому браузер не может распаковать файлы.
/// </summary>
public class WebGLBuildSettings : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report)
    {
        if (report.summary.platform == BuildTarget.WebGL)
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            Debug.Log("[WebGLBuildSettings] Compression disabled for GitHub Pages compatibility.");
        }
    }
}
