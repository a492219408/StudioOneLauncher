// 主程序入口

using System.Diagnostics;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.RepresentationModel; // 低级 API

namespace StudioOneLauncher;

public static class Program
{
    public static void Main(string[] args)
    {
        // 读取 YAML 文件
        var yaml = new StreamReader("config.yaml");

        // 这里保留了反序列化代码，作为未来开发参考，但不实际使用它
        // 反序列化配置文件，使用驼峰命名的约定
        // var yaml = File.ReadAllText("config.yaml");
        // var deserializer = new DeserializerBuilder()
        //     .WithNamingConvention(CamelCaseNamingConvention.Instance) // 使用驼峰命名
        //     .Build();
        // var settings = deserializer.Deserialize<ProgramSettings>(yaml);

        // 使用低级 API 来加载 YAML 数据，而非反序列化
        var yamlStream = new YamlStream();
        yamlStream.Load(yaml);

        // 访问 YAML 文件的根节点
        var root = (YamlMappingNode)yamlStream.Documents[0].RootNode;

        // 创建 ProgramSettings 并手动从 YAML 文件中提取值
        var settings = new ProgramSettings
        {
            ProgramPath = root.Children[new YamlScalarNode("programPath")].ToString(),
            ProjectFolderPath = root.Children[new YamlScalarNode("projectFolderPath")].ToString(),
            UsedAutoSave = bool.Parse(root.Children[new YamlScalarNode("usedAutoSave")].ToString())
        };

        // 遍历 ProjectFolderPath 下的所有 .song 文件
        if (Directory.Exists(settings.ProjectFolderPath))
        {
            var songFiles = Directory.GetFiles(settings.ProjectFolderPath, "*.song");
            if (songFiles.Length > 0)
            {
                // 获取第一个 .song 文件并将其路径存储到 SongFilePath
                settings.SongFilePath = songFiles[0];
                Console.WriteLine($"Found song file: {settings.SongFilePath}");
            }
            else
            {
                Console.WriteLine("No .song files found in the specified folder.");
            }

            // 如果启用了自动保存，且相关路径不为空，尝试移动自动保存文件
            if (settings.UsedAutoSave && !string.IsNullOrEmpty(settings.SongAutoSaveFilePath) &&
                !string.IsNullOrEmpty(settings.SongFilePath))
            {
                if (Directory.Exists(settings.SongAutoSaveFilePath) &&
                    Directory.Exists(Path.GetDirectoryName(settings.SongFilePath)))
                {
                    // 移动自动保存文件到正式的 .song 文件路径
                    Directory.Move(settings.SongAutoSaveFilePath, settings.SongFilePath);
                }
                else
                {
                    Console.WriteLine("自动保存文件路径或目标路径不存在，无法移动文件。");
                }
            }
        }

        // 运行程序并获取它的进程ID
        var pid = RunProgramAsMin(settings);
        int ccLDialogClassWaitTime = 3000; // 最多等待3秒

        // 循环检查指定的窗口是否出现，直到超时
        while (ccLDialogClassWaitTime > 0)
        {
            var hWnd = WindowManager.FindWindowByTitlePidClass("Studio One 安全", pid, "CCLDialogClass");
            if (hWnd != IntPtr.Zero)
            {
                Console.WriteLine("发现\"Studio One 安全\"窗口");

                // 安全地关闭该窗口
                WindowManager.CloseWindowSafely(hWnd);

                // 确保程序重新启动
                RunProgramAsMin(settings);
                break;
            }

            // 每100毫秒检查一次窗口
            Thread.Sleep(100);
            ccLDialogClassWaitTime -= 100;
        }
    }

    /// <summary>
    /// 以最小化模式运行指定的程序，并返回其进程ID
    /// </summary>
    /// <param name="settings">包含程序路径和其他配置信息的设置对象</param>
    /// <returns>启动的程序的进程ID</returns>
    private static int RunProgramAsMin(ProgramSettings settings)
    {
        // 检查是否有同名的进程正在运行，如果有，则结束它们
        while (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(settings.ProgramFileName)).Length > 0)
        {
            Console.WriteLine($"{settings.ProgramFileName} 进程存在，正在结束...");
            foreach (var process in Process.GetProcessesByName(
                         Path.GetFileNameWithoutExtension(settings.ProgramFileName)))
            {
                process.Kill();
                Console.WriteLine($"{process.ProcessName} 进程已结束");
            }

            Thread.Sleep(1000); // 每次检查后等待1秒
        }

        // 启动指定的程序
        Console.WriteLine($"正在运行 {settings.ProgramPath} ...");
        Process processToRun = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = settings.ProgramPath,
                WindowStyle = ProcessWindowStyle.Minimized // 启动时窗口最小化
            }
        };
        processToRun.Start();
        int pid = processToRun.Id;
        Console.WriteLine($"程序已启动，PID: {pid}");

        // 等待程序的主窗口出现
        while (true)
        {
            try
            {
                Process startedProcess = Process.GetProcessById(pid); // 通过PID获取进程对象
                if (startedProcess.MainWindowHandle != IntPtr.Zero) // 如果有主窗口句柄，表示窗口已出现
                {
                    Console.WriteLine("窗口已出现");
                    break; // 窗口出现，退出循环
                }
            }
            catch (ArgumentException)
            {
                // 如果进程不存在或无法获取进程，继续等待
                Console.WriteLine("进程不存在，继续等待...");
            }

            Thread.Sleep(1000); // 每隔1秒检查一次
        }

        return pid; // 返回进程ID
    }
}