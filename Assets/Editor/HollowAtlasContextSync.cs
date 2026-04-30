// ============================================================
//  HollowAtlasContextSync.cs
//  Assets/Editor/HollowAtlasContextSync.cs  klasörüne koy.
//  Unity 6000.3.10f1 / Unity 6 uyumlu.
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace HollowAtlas.Editor
{
    public class CsFileInfo
    {
        public string RelativePath;
        public string FullPath;
        public DateTime Modified;
    }

    public class HollowAtlasContextSync : AssetPostprocessor
    {
        private const string PROJECT_OUTPUT_PATH = "Assets/Context/context.md";
        private const string DESKTOP_FILE_NAME   = "HollowAtlas_context.md";

        private static readonly string[] IGNORE_FOLDERS = new string[]
        {
            "PackageCache", "Packages", "Library", "Temp",
            "obj", ".git", "node_modules"
        };

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            bool anyCs = false;
            for (int i = 0; i < importedAssets.Length; i++)
            {
                if (importedAssets[i].EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                {
                    anyCs = true;
                    break;
                }
            }
            if (anyCs) SyncContext();
        }

        [MenuItem("Tools/Hollow Atlas/Sync Context Now %#s")]
        public static void SyncContextManual()
        {
            SyncContext();
            Debug.Log("[HollowAtlasContextSync] Manuel sync tamamlandi.");
        }

        [MenuItem("Tools/Hollow Atlas/Open context.md")]
        public static void OpenContextFile()
        {
            string fullPath = Path.GetFullPath(PROJECT_OUTPUT_PATH);
            if (File.Exists(fullPath))
            {
                System.Diagnostics.Process.Start(fullPath);
            }
            else
            {
                Debug.LogWarning("[HollowAtlasContextSync] context.md henuz olusturulmadi.");
            }
        }

        private static void SyncContext()
        {
            List<CsFileInfo> allCs = CollectCsFiles();
            string content = BuildMarkdown(allCs);
            WriteProjectFile(content);
            WriteDesktopFile(content);
            Debug.Log("[HollowAtlasContextSync] " + allCs.Count + " script senkronize edildi.");
        }

        private static List<CsFileInfo> CollectCsFiles()
        {
            List<CsFileInfo> result = new List<CsFileInfo>();
            string assetsRoot = Application.dataPath.Replace('\\', '/');
            string[] files = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);

            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i].Replace('\\', '/');
                if (Path.GetFileName(file) == "HollowAtlasContextSync.cs") continue;

                bool skip = false;
                for (int j = 0; j < IGNORE_FOLDERS.Length; j++)
                {
                    if (file.Contains(IGNORE_FOLDERS[j])) { skip = true; break; }
                }
                if (skip) continue;

                CsFileInfo info = new CsFileInfo();
                info.RelativePath = "Assets" + file.Substring(assetsRoot.Length);
                info.FullPath = file;
                info.Modified = File.GetLastWriteTime(file);
                result.Add(info);
            }

            result.Sort(delegate (CsFileInfo a, CsFileInfo b)
            {
                return string.Compare(a.RelativePath, b.RelativePath, StringComparison.OrdinalIgnoreCase);
            });
            return result;
        }

        private static string BuildMarkdown(List<CsFileInfo> files)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("# HOLLOW ATLAS - PROJE BAGLAM DOSYASI");
            sb.AppendLine("<!-- Otomatik uretilir. Elle duzenleme. -->");
            sb.AppendLine();
            sb.AppendLine("**Olusturulma:** " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("**Unity:** 6000.3.10f1 | **Render:** URP | **Platform:** PC / Steam");
            sb.AppendLine("**Toplam Script:** " + files.Count);
            sb.AppendLine();

            sb.AppendLine("## PROJE OZETI");
            sb.AppendLine("- **Tur:** 2.5D izometrik action roguelike/roguelite");
            sb.AppendLine("- **Gorsel hedef:** Hades 2 ilhamli, yuksek kontrastli karanlik mitolojik fantezi");
            sb.AppendLine("- **Oynanis:** Vampire Survivors benzeri auto-attack + wave + roguelite kart secimi");
            sb.AppendLine("- **Engine:** Unity 6 (URP, C#, Input System, uGUI)");
            sb.AppendLine();

            sb.AppendLine("## SCRIPT INDEKSI");
            sb.AppendLine();
            sb.AppendLine("| # | Dosya | Klasor | Son Degisiklik |");
            sb.AppendLine("|---|-------|--------|----------------|");
            for (int i = 0; i < files.Count; i++)
            {
                CsFileInfo f = files[i];
                string fileName = Path.GetFileName(f.RelativePath);
                string folder = Path.GetDirectoryName(f.RelativePath);
                if (folder != null) folder = folder.Replace('\\', '/');
                else folder = "";
                sb.AppendLine("| " + (i + 1) + " | `" + fileName + "` | `" + folder + "` | " + f.Modified.ToString("yyyy-MM-dd HH:mm") + " |");
            }
            sb.AppendLine();

            sb.AppendLine("---");
            sb.AppendLine("## SCRIPT ICERIKLERI");
            sb.AppendLine();

            for (int i = 0; i < files.Count; i++)
            {
                CsFileInfo f = files[i];
                string fileName = Path.GetFileName(f.RelativePath);

                sb.AppendLine("### [" + (i + 1) + "] " + fileName);
                sb.AppendLine("**Yol:** `" + f.RelativePath + "`  ");
                sb.AppendLine("**Son degisiklik:** " + f.Modified.ToString("yyyy-MM-dd HH:mm:ss"));
                sb.AppendLine();
                sb.AppendLine("```csharp");

                try
                {
                    string code = File.ReadAllText(f.FullPath);
                    sb.AppendLine(code);
                }
                catch (Exception ex)
                {
                    sb.AppendLine("// HATA: " + ex.Message);
                }

                sb.AppendLine("```");
                sb.AppendLine();
            }

            sb.AppendLine("---");
            sb.AppendLine("*[Hollow Atlas Context Sync]*");

            return sb.ToString();
        }

        private static void WriteProjectFile(string content)
        {
            try
            {
                string fullPath = Path.GetFullPath(PROJECT_OUTPUT_PATH);
                string dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllText(fullPath, content, Encoding.UTF8);
                AssetDatabase.ImportAsset(PROJECT_OUTPUT_PATH, ImportAssetOptions.ForceUpdate);
            }
            catch (Exception ex)
            {
                Debug.LogError("[HollowAtlasContextSync] Proje dosyasi yazilamadi: " + ex.Message);
            }
        }

        private static void WriteDesktopFile(string content)
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string filePath = Path.Combine(desktop, DESKTOP_FILE_NAME);
                File.WriteAllText(filePath, content, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.LogError("[HollowAtlasContextSync] Masaustu dosyasi yazilamadi: " + ex.Message);
            }
        }
    }
}
