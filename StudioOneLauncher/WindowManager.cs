using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace StudioOneLauncher;

/// <summary>
/// 用于管理窗口操作的工具类
/// </summary>
public static class WindowManager
{
    // 导入 Windows API 函数
    
    // 用于枚举所有顶级窗口
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    // 获取指定窗口的标题
    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    // 获取指定窗口的类名
    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    // 获取指定窗口的进程ID
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

    // 检查窗口句柄是否有效
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool IsWindow(IntPtr hWnd);

    // 检查窗口是否无响应
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool IsHungAppWindow(IntPtr hWnd);

    // 发送消息到指定窗口
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    // 以异步方式发送消息到指定窗口
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    // 定义枚举窗口的回调函数
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    // 常量定义
    private const uint WmClose = 0x0010; // WM_CLOSE 消息
    private const uint WmSysCommand = 0x0112; // WM_SYSCOMMAND 消息
    private const int ScClose = 0xF060; // SC_CLOSE 子命令，表示关闭窗口

    /// <summary>
    /// 根据窗口标题、进程ID和类名查找窗口句柄
    /// </summary>
    /// <param name="title">窗口标题的部分内容</param>
    /// <param name="pid">进程ID</param>
    /// <param name="className">窗口类名</param>
    /// <returns>找到的窗口句柄，未找到时返回 IntPtr.Zero</returns>
    public static IntPtr FindWindowByTitlePidClass(string title, int pid, string className)
    {
        IntPtr foundWindow = IntPtr.Zero;

        // 枚举所有顶级窗口，查找符合条件的窗口
        EnumWindows((hWnd, _) =>
        {
            // 获取窗口标题
            StringBuilder windowTitle = new StringBuilder(256);
            GetWindowText(hWnd, windowTitle, windowTitle.Capacity);

            // 获取窗口类名
            StringBuilder windowClass = new StringBuilder(256);
            GetClassName(hWnd, windowClass, windowClass.Capacity);

            // 获取窗口进程ID
            GetWindowThreadProcessId(hWnd, out int windowPid);

            // 判断窗口是否符合指定的标题、进程ID和类名
            if (windowTitle.ToString().Contains(title) &&
                windowPid == pid &&
                windowClass.ToString() == className)
            {
                foundWindow = hWnd; // 找到目标窗口
                return false; // 停止枚举
            }

            return true; // 继续枚举其他窗口
        }, IntPtr.Zero);

        return foundWindow; // 返回找到的窗口句柄
    }

    /// <summary>
    /// 强制关闭指定的窗口
    /// </summary>
    /// <param name="hWnd">要关闭的窗口句柄</param>
    public static void CloseWindowByHandle(IntPtr hWnd)
    {
        if (hWnd != IntPtr.Zero)
        {
            Console.WriteLine($"正在关闭窗口，hWnd: {hWnd}");
            // 发送 WM_CLOSE 消息以强制关闭窗口
            SendMessage(hWnd, WmClose, IntPtr.Zero, IntPtr.Zero);
        }
        else
        {
            Console.WriteLine("无效的窗口句柄，无法关闭窗口");
        }
    }

    /// <summary>
    /// 安全关闭指定的窗口，带有等待时间和卡死窗口处理
    /// </summary>
    /// <param name="hWnd">要关闭的窗口句柄</param>
    /// <param name="waitTime">等待窗口关闭的时间（毫秒）</param>
    /// <param name="killIfHung">如果窗口无响应，是否强制结束</param>
    public static void CloseWindowSafely(IntPtr hWnd, int waitTime = 5000, bool killIfHung = false)
    {
        if (hWnd != IntPtr.Zero && IsWindow(hWnd))
        {
            Console.WriteLine($"正在安全关闭窗口，HWND: {hWnd}");

            // 检查窗口是否无响应
            if (IsHungAppWindow(hWnd))
            {
                Console.WriteLine("窗口无响应");
                if (killIfHung)
                {
                    // 如果窗口卡死并且允许强制结束
                    Console.WriteLine("强制结束进程");
                    CloseProcessByHWnd(hWnd);
                    return;
                }
            }

            // 发送 WM_SYSCOMMAND + SC_CLOSE 消息以模拟用户关闭
            PostMessage(hWnd, WmSysCommand, new IntPtr(ScClose), IntPtr.Zero);

            // 等待窗口关闭
            Stopwatch sw = Stopwatch.StartNew();
            while (IsWindow(hWnd) && sw.ElapsedMilliseconds < waitTime)
            {
                Thread.Sleep(100); // 每隔100毫秒检查一次窗口状态
            }

            // 检查窗口是否成功关闭
            if (IsWindow(hWnd))
            {
                Console.WriteLine("窗口未关闭，检查是否无响应");
                if (killIfHung)
                {
                    CloseProcessByHWnd(hWnd);
                }
            }
            else
            {
                Console.WriteLine("窗口已成功关闭");
            }
        }
        else
        {
            Console.WriteLine("无效的窗口句柄");
        }
    }

    /// <summary>
    /// 强制结束与窗口关联的进程
    /// </summary>
    /// <param name="hWnd">窗口句柄</param>
    private static void CloseProcessByHWnd(IntPtr hWnd)
    {
        // 获取窗口对应的进程ID
        GetWindowThreadProcessId(hWnd, out int pid);
        // 获取进程对象并强制结束
        Process process = Process.GetProcessById(pid);
        process.Kill();
        Console.WriteLine($"进程 {pid} 已被强制结束");
    }
}
