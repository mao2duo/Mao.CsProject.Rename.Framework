using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mao.CsProject.Rename.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            App app = new App();
            app.Execute();
        }
    }

    public class App
    {
        string projectPath = @"D:\Workspace\Template";
        string originalName = "MyProject1";
        string targetName = "MyProject2";

        /// <summary>
        /// 主程序
        /// </summary>
        public void Execute()
        {
            if (!Directory.Exists(projectPath))
            {
                return;
            }

            #region 刪除不需要的目錄
            List<string> deleteDirectories = new List<string>();
            deleteDirectories.Add($"{projectPath}\\.vs");
            // 先找到所有 .csproj
            var csprojFiles = Directory.GetFiles(projectPath, "*.csproj", SearchOption.AllDirectories);
            foreach (var filePath in csprojFiles)
            {
                string directoryPath = projectPath;
                int index = filePath.LastIndexOf('\\');
                if (index >= 0)
                {
                    directoryPath = filePath.Substring(0, index);
                }
                // 假設所有編譯目的都是預設的，找同一層的 bin / obj 目錄
                deleteDirectories.Add(Path.Combine(directoryPath, "bin"));
                deleteDirectories.Add(Path.Combine(directoryPath, "obj"));
            }
            foreach (var directoryPath in deleteDirectories)
            {
                if (Directory.Exists(directoryPath))
                {
                    try
                    {
                        Directory.Delete(directoryPath, true);
                    }
                    catch
                    {
                        // 刪除不掉就算了
                    }
                }
            }
            #endregion
            #region 刪除不需要的檔案
            var deleteFiles = Directory.GetFiles(projectPath, "*.user", SearchOption.AllDirectories);
            foreach (var filePath in deleteFiles)
            {
                try
                {
                    File.Delete(filePath);
                }
                catch
                {
                    // 刪除不掉就算了
                }
            }
            #endregion

            int pathIndex = projectPath.Length + 1;

            var directories = Directory.EnumerateDirectories(projectPath, "*", SearchOption.AllDirectories)
                .Where(x => x[pathIndex] != '.')
                .Where(x => x.IndexOf("packages\\", pathIndex) != pathIndex)
                .Where(x => !x.Contains("\\bin\\") && !x.Contains("\\obj\\") && !x.Contains("\\Log\\"))
                .OrderBy(x => x)
                .ToArray();

            #region 重新命名目錄
            for (int i = 0; i < directories.Length; i++)
            {
                var directoryPath = directories[i];
                int index1 = directoryPath.IndexOf(originalName, pathIndex);
                if (index1 >= 0)
                {
                    string directorySource = directoryPath;
                    int index2 = directoryPath.IndexOf("\\", index1 + 1);
                    if (index2 >= 0)
                    {
                        directorySource = directoryPath.Substring(0, index2);
                    }
                    string directoryDestination = directorySource.Replace(originalName, targetName);
                    // 重新命名目錄
                    Directory.Move(directorySource, directoryDestination);
                    // 把新路徑寫回陣列
                    if (directoryPath.Length == directorySource.Length)
                    {
                        directories[i] = directoryDestination;
                    }
                    else
                    {
                        directories[i] = $"{directoryDestination}{directoryPath.Substring(directorySource.Length)}";
                    }
                    // 把接下來要處理的子目錄路徑修正為重新命名後的路徑
                    int directorySourceLength = directorySource.Length;
                    for (int j = i + 1; j < directories.Length; j++)
                    {
                        if (directories[j].StartsWith($"{directorySource}\\"))
                        {
                            directories[j] = $"{directoryDestination}{directories[j].Substring(directorySourceLength)}";
                        }
                        else
                        {
                            // 因為路徑有排序過，找到第一個不是子目錄的路徑，就代表後面都不是了
                            break;
                        }
                    }
                }
            }
            #endregion

            var files = Directory.EnumerateFiles(projectPath, "*", SearchOption.AllDirectories)
                .Where(x => x[pathIndex] != '.')
                .Where(x => x.IndexOf("packages\\", pathIndex) != pathIndex)
                .Where(x => !x.EndsWith(".dll") && !x.EndsWith(".user") && !x.EndsWith(".log"))
                .Where(x => !x.Contains("\\bin\\") && !x.Contains("\\obj\\") && !x.Contains("\\Log\\"))
                .ToArray();

            #region 重新命名檔案
            for (int i = 0; i < files.Length; i++)
            {
                string filePath = files[i];
                // 因為前面有做過重新命名目錄，所以如果整個路徑還有就專案名稱就是在檔案名稱
                if (filePath.Contains(originalName))
                {
                    string fileSource = filePath;
                    string fileDestination = fileSource.Replace(originalName, targetName);
                    // 重新命名檔案
                    File.Move(fileSource, fileDestination);
                    // 把新路徑寫回陣列
                    files[i] = fileDestination;
                }
            }
            #endregion
            #region 取代檔案內容
            foreach (var filePath in files)
            {
                string text = File.ReadAllText(filePath, Encoding.UTF8);
                if (text.IndexOf(originalName) >= 0)
                {
                    string newText = text.Replace(originalName, targetName);
                    File.WriteAllText(filePath, newText, Encoding.UTF8);
                }
            }
            #endregion
        }
    }
}
