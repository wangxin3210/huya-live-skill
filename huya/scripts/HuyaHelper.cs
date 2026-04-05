using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;

public class HuyaHelper {
    [DllImport("user32.dll")]
    static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll")]
    static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll")]
    static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    [DllImport("user32.dll")]
    static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);
    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")]
    static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
    [DllImport("user32.dll")]
    static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")]
    static extern bool SetCursorPos(int X, int Y);
    [DllImport("user32.dll")]
    static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [StructLayout(LayoutKind.Sequential)]
    struct RECT { public int Left, Top, Right, Bottom; }
    delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    const byte VK_F11 = 0x7A;
    const uint KEYEVENTF_KEYDOWN = 0;
    const uint KEYEVENTF_KEYUP = 2;
    const int SW_RESTORE = 9;
    const uint MOUSEEVENTF_LEFTDOWN = 2;
    const uint MOUSEEVENTF_LEFTUP = 4;

    // 多显示器配置：修改这里以适应你的屏幕布局
    const int SCREEN2_X = 3840;  // 屏幕2的起始X坐标
    const int SCREEN2_Y = 0;     // 屏幕2的起始Y坐标
    const int SCREEN_WIDTH = 1920;
    const int SCREEN_HEIGHT = 1080;

    static IntPtr FindEdgeWindow() {
        IntPtr found = IntPtr.Zero;
        EnumWindows(delegate(IntPtr hWnd, IntPtr lParam) {
            if (!IsWindowVisible(hWnd)) return true;
            var sb = new StringBuilder(256);
            GetWindowText(hWnd, sb, 256);
            string title = sb.ToString();
            if (string.IsNullOrEmpty(title)) return true;
            uint pid;
            GetWindowThreadProcessId(hWnd, out pid);
            try {
                var proc = Process.GetProcessById((int)pid);
                if (proc.ProcessName.ToLower().Equals("msedge") && title.Contains("Edge")) {
                    found = hWnd;
                    return false;
                }
            } catch {}
            return true;
        }, IntPtr.Zero);
        return found;
    }

    /// <summary>
    /// F11 - Toggle F11 fullscreen on Edge.
    ///   Moves window to screen2, brings to foreground, sends F11 via keybd_event.
    /// </summary>
    public static void F11() {
        IntPtr hwnd = FindEdgeWindow();
        if (hwnd == IntPtr.Zero) { Console.WriteLine("Edge not found"); return; }

        RECT rect;
        GetWindowRect(hwnd, out rect);

        // Restore window to normal state first (exit any existing fullscreen)
        ShowWindow(hwnd, SW_RESTORE);
        System.Threading.Thread.Sleep(300);

        // Move to screen2
        MoveWindow(hwnd, SCREEN2_X, SCREEN2_Y, SCREEN_WIDTH, SCREEN_HEIGHT, true);
        System.Threading.Thread.Sleep(500);

        // Attach threads + set foreground for reliable F11 delivery
        uint fgPid;
        uint fgTid = GetWindowThreadProcessId(GetForegroundWindow(), out fgPid);
        uint edPid;
        uint edTid = GetWindowThreadProcessId(hwnd, out edPid);
        if (fgTid != edTid) AttachThreadInput(fgTid, edTid, true);

        SetForegroundWindow(hwnd);
        System.Threading.Thread.Sleep(800);

        if (fgTid != edTid) AttachThreadInput(fgTid, edTid, false);

        keybd_event(VK_F11, 0, KEYEVENTF_KEYDOWN, 0);
        System.Threading.Thread.Sleep(50);
        keybd_event(VK_F11, 0, KEYEVENTF_KEYUP, 0);

        Console.WriteLine("F11 sent");
    }

    /// <summary>
    /// click <screenX> <screenY> - Physical mouse click at absolute screen coordinates.
    /// </summary>
    public static void Click(int screenX, int screenY) {
        SetCursorPos(screenX, screenY);
        System.Threading.Thread.Sleep(200);
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
        System.Threading.Thread.Sleep(50);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        Console.WriteLine("clicked " + screenX + "," + screenY);
    }

    public static void Main(string[] args) {
        if (args.Length == 0) { F11(); return; }
        switch (args[0]) {
            case "f11": F11(); break;
            case "click": Click(int.Parse(args[1]), int.Parse(args[2])); break;
            default: Console.WriteLine("Usage: HuyaHelper [f11|click <x> <y>]"); break;
        }
    }
}
