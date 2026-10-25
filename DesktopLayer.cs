
using System;
using System.Runtime.InteropServices;

namespace DeskCritters;

public static class DesktopLayer
{
    private const uint SpawnWorkerMessage = 0x052C;
    private const uint TimeoutFlag = 0x0002;

    private const int GWL_STYLE = -16;
    private const int WS_CHILD = 0x40000000;
    private const int WS_POPUP = unchecked((int)0x80000000);

    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_FRAMECHANGED = 0x0020;
    private const uint SWP_SHOWWINDOW = 0x0040;

    private static IntPtr desktopHost = IntPtr.Zero;
    private static IntPtr attachedWindow = IntPtr.Zero;
    private static IntPtr previousStyle = IntPtr.Zero;
    private static IntPtr previousParent = IntPtr.Zero;

    public static bool IsAttached =>
        attachedWindow != IntPtr.Zero;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(
        string? className,
        string? windowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindowEx(
        IntPtr parent,
        IntPtr childAfter,
        string? className,
        string? windowName);

    private delegate bool EnumWindowsCallback(
        IntPtr window,
        IntPtr parameter);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(
        EnumWindowsCallback callback,
        IntPtr parameter);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetParent(
        IntPtr child,
        IntPtr newParent);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(
        IntPtr window,
        int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(
        IntPtr window,
        int index,
        IntPtr value);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr window,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(
        IntPtr window,
        out RECT rect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr window,
        uint message,
        IntPtr wParam,
        IntPtr lParam,
        uint flags,
        uint timeout,
        out IntPtr result);

    [DllImport("user32.dll")]
    private static extern IntPtr GetParent(IntPtr window);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public static bool TryAttach(IntPtr window)
    {
        if (window == IntPtr.Zero)
            return false;

        if (IsAttached)
            return true;

        IntPtr progman = FindWindow("Progman", null);

        if (progman == IntPtr.Zero)
            return false;

        // Ask Explorer to provide a wallpaper host.
        // This is an undocumented shell message.
        SendMessageTimeout(
            progman,
            SpawnWorkerMessage,
            IntPtr.Zero,
            IntPtr.Zero,
            TimeoutFlag,
            1000,
            out _);

        IntPtr worker = IntPtr.Zero;

        // Find the window holding desktop icons,
        // then locate its WorkerW sibling.
        EnumWindows((topWindow, parameter) =>
        {
            IntPtr icons = FindWindowEx(
                topWindow,
                IntPtr.Zero,
                "SHELLDLL_DefView",
                null);

            if (icons == IntPtr.Zero)
                return true;

            worker = FindWindowEx(
                IntPtr.Zero,
                topWindow,
                "WorkerW",
                null);

            return false;
        }, IntPtr.Zero);

        // Fallback for alternative Explorer layouts.
        if (worker == IntPtr.Zero)
        {
            worker = FindWindowEx(
                progman,
                IntPtr.Zero,
                "WorkerW",
                null);
        }

        if (worker == IntPtr.Zero)
            return false;

        previousStyle = GetWindowLongPtr(
            window,
            GWL_STYLE);

        previousParent = GetParent(window);

        long currentStyle = previousStyle.ToInt64();

        long childStyle =
            (currentStyle | WS_CHILD) & ~((long)WS_POPUP);

        SetWindowLongPtr(
            window,
            GWL_STYLE,
            new IntPtr(childStyle));

        SetParent(window, worker);

        // Restore the style if parenting fails.
        if (GetParent(window) != worker)
        {
            SetWindowLongPtr(
                window,
                GWL_STYLE,
                new IntPtr(currentStyle));

            return false;
        }

        desktopHost = worker;
        attachedWindow = window;

        SetWindowPos(
            window,
            IntPtr.Zero,
            100,
            100,
            180,
            180,
            SWP_NOZORDER |
            SWP_NOACTIVATE |
            SWP_FRAMECHANGED |
            SWP_SHOWWINDOW);

        return true;
    }

    public static (int width, int height) GetDesktopSize()
    {
        if (desktopHost == IntPtr.Zero)
            return (0, 0);

        if (!GetClientRect(desktopHost, out RECT rect))
            return (0, 0);

        return (
            rect.Right - rect.Left,
            rect.Bottom - rect.Top
        );
    }

    public static void Move(
        int x,
        int y,
        int width,
        int height)
    {
        if (!IsAttached)
            return;

        SetWindowPos(
            attachedWindow,
            IntPtr.Zero,
            x,
            y,
            width,
            height,
            SWP_NOZORDER |
            SWP_NOACTIVATE |
            SWP_SHOWWINDOW);
    }

    public static void Detach()
    {
        if (!IsAttached)
            return;

        IntPtr window = attachedWindow;

        // Restore the original parent and style.
        SetParent(window, previousParent);

        SetWindowLongPtr(
            window,
            GWL_STYLE,
            previousStyle);

        SetWindowPos(
            window,
            IntPtr.Zero,
            100,
            100,
            180,
            180,
            SWP_NOZORDER |
            SWP_NOACTIVATE |
            SWP_FRAMECHANGED |
            SWP_SHOWWINDOW);

        attachedWindow = IntPtr.Zero;
        desktopHost = IntPtr.Zero;
    }
}
