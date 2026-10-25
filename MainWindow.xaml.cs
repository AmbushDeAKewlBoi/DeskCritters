
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace DeskCritters;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer movementTimer;
    private readonly Critter critter;
    private bool isPaused;
    private bool desktopMode;
    private int desktopX = 100;
    private int desktopY = 100;
    private double fractionalMovement;

    private const int ToggleHotkey = 1;
    private const int ExitHotkey = 2;
    private const int WM_HOTKEY = 0x0312;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;

    private HwndSource? windowSource;
    private IntPtr windowHandle;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(
        IntPtr window, int id, uint modifiers, uint key);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(
        IntPtr window, int id);

    public MainWindow()
    {
        InitializeComponent();

        critter = new Critter("Buddy", CritterSpecies.Slime);

        Left = SystemParameters.WorkArea.Right - Width - 50;
        Top = SystemParameters.WorkArea.Bottom - Height - 50;

        movementTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(30)
        };

        movementTimer.Tick += MoveCritter;

        SourceInitialized += Window_SourceInitialized;
        Closed += Window_Closed;

        UpdateAppearance();
        movementTimer.Start();

        // Desktop mode is opt-in until tested.
    }

    private void Window_SourceInitialized(
        object? sender, EventArgs e)
    {
        windowHandle = new WindowInteropHelper(this).Handle;
        windowSource = HwndSource.FromHwnd(windowHandle);
        windowSource?.AddHook(WindowProcedure);

        bool toggleOk = RegisterHotKey(
            windowHandle,
            ToggleHotkey,
            MOD_CONTROL | MOD_ALT,
            0x44);

        bool exitOk = RegisterHotKey(
            windowHandle,
            ExitHotkey,
            MOD_CONTROL | MOD_ALT,
            0x51);

        if (!toggleOk || !exitOk)
        {
            MessageBox.Show(
                "Some global shortcuts are already in use. " +
                "You can still control DeskCritters from " +
                "its right-click menu while in floating mode.",
                "DeskCritters shortcuts",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private IntPtr WindowProcedure(
        IntPtr hwnd,
        int message,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handled)
    {
        if (message == WM_HOTKEY)
        {
            int id = wParam.ToInt32();

            if (id == ToggleHotkey)
            {
                ToggleDesktopMode();
                handled = true;
            }
            else if (id == ExitHotkey)
            {
                Close();
                handled = true;
            }
        }

        return IntPtr.Zero;
    }

    private void MoveCritter(
        object? sender, EventArgs e)
    {
        if (isPaused)
            return;

        critter.Update();

        if (desktopMode)
            MoveInDesktopMode();
        else
            MoveInFloatingMode();

        UpdateLabel();
    }

    private void MoveInFloatingMode()
    {
        if (critter.IsWalking())
            Left += critter.CurrentSpeed * critter.Direction;

        double minX = SystemParameters.WorkArea.Left;
        double maxX = Math.Max(
            minX,
            SystemParameters.WorkArea.Right - Width);

        if (Left >= maxX)
        {
            Left = maxX;
            critter.Direction = -1;
        }
        else if (Left <= minX)
        {
            Left = minX;
            critter.Direction = 1;
        }
    }

    private void MoveInDesktopMode()
    {
        if (!DesktopLayer.IsAttached)
        {
            desktopMode = false;
            UpdateLabel();
            return;
        }

        var (width, height) = DesktopLayer.GetDesktopSize();

        if (width <= 0 || height <= 0)
            return;

        if (critter.IsWalking())
        {
            fractionalMovement +=
                critter.CurrentSpeed * critter.Direction;

            int wholePixels =
                (int)Math.Truncate(fractionalMovement);

            desktopX += wholePixels;
            fractionalMovement -= wholePixels;
        }

        int maxX = Math.Max(
            0,
            width - (int)Math.Ceiling(Width));

        int maxY = Math.Max(
            0,
            height - (int)Math.Ceiling(Height));

        if (desktopX >= maxX)
        {
            desktopX = maxX;
            critter.Direction = -1;
        }
        else if (desktopX <= 0)
        {
            desktopX = 0;
            critter.Direction = 1;
        }

        desktopY = Math.Clamp(desktopY, 0, maxY);

        DesktopLayer.Move(
            desktopX,
            desktopY,
            (int)Width,
            (int)Height);
    }

    private void EnableDesktopMode()
    {
        if (desktopMode || windowHandle == IntPtr.Zero)
            return;

        movementTimer.Stop();

        try
        {
            if (!DesktopLayer.TryAttach(windowHandle))
            {
                MessageBox.Show(
                    "Windows Explorer did not expose a " +
                    "compatible desktop host. Floating " +
                    "mode is still available.",
                    "Desktop mode unavailable",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            desktopMode = true;
            ShowInTaskbar = false;

            var (width, height) =
                DesktopLayer.GetDesktopSize();

            desktopX = Math.Max(
                0, width - (int)Width - 60);

            desktopY = Math.Max(
                0, height - (int)Height - 80);

            DesktopLayer.Move(
                desktopX,
                desktopY,
                (int)Width,
                (int)Height);
        }
        catch (Exception ex)
        {
            DesktopLayer.Detach();
            desktopMode = false;
            ShowInTaskbar = true;

            MessageBox.Show(
                ex.Message,
                "Desktop mode error");
        }
        finally
        {
            if (!isPaused)
                movementTimer.Start();

            UpdateLabel();
        }
    }

    private void DisableDesktopMode()
    {
        if (!desktopMode)
            return;

        movementTimer.Stop();

        try
        {
            DesktopLayer.Detach();
            desktopMode = false;
            ShowInTaskbar = true;

            Left = SystemParameters.WorkArea.Right
                - Width - 50;

            Top = SystemParameters.WorkArea.Bottom
                - Height - 50;
        }
        finally
        {
            if (!isPaused)
                movementTimer.Start();

            UpdateLabel();
        }
    }

    private void ToggleDesktopMode()
    {
        if (desktopMode)
            DisableDesktopMode();
        else
            EnableDesktopMode();
    }

    private void UpdateAppearance()
    {
        DragonWings.Visibility = Visibility.Collapsed;
        DragonWingRight.Visibility = Visibility.Collapsed;

        DogEarLeft.Visibility = Visibility.Collapsed;
        DogEarRight.Visibility = Visibility.Collapsed;

        CatEarLeft.Visibility = Visibility.Collapsed;
        CatEarRight.Visibility = Visibility.Collapsed;

        ElfEarLeft.Visibility = Visibility.Collapsed;
        ElfEarRight.Visibility = Visibility.Collapsed;

        Color color = critter.Species switch
        {
            CritterSpecies.Slime =>
                Color.FromRgb(100, 220, 150),

            CritterSpecies.Cat =>
                Color.FromRgb(255, 180, 100),

            CritterSpecies.Dog =>
                Color.FromRgb(190, 140, 100),

            CritterSpecies.Dragon =>
                Color.FromRgb(170, 100, 230),

            CritterSpecies.Elf =>
                Color.FromRgb(120, 200, 255),

            _ => Colors.Orange
        };

        CritterBody.Fill = new SolidColorBrush(color);

        switch (critter.Species)
        {
            case CritterSpecies.Cat:
                CatEarLeft.Visibility = Visibility.Visible;
                CatEarRight.Visibility = Visibility.Visible;
                break;

            case CritterSpecies.Dog:
                DogEarLeft.Visibility = Visibility.Visible;
                DogEarRight.Visibility = Visibility.Visible;
                break;

            case CritterSpecies.Dragon:
                DragonWings.Visibility = Visibility.Visible;
                DragonWingRight.Visibility = Visibility.Visible;
                break;

            case CritterSpecies.Elf:
                ElfEarLeft.Visibility = Visibility.Visible;
                ElfEarRight.Visibility = Visibility.Visible;
                break;
        }

        UpdateLabel();
    }

    private void UpdateLabel()
    {
        string state = isPaused
            ? "Paused"
            : critter.IsWalking()
                ? (critter.Sprinting ? "Sprinting" : "Walking")
                : "Resting";

        CritterLabel.Text =
            $"{critter.Name} | {critter.Species}\n" +
            $"{critter.Personality} | {state} | " +
            (desktopMode ? "Desktop" : "Floating");
    }

    private void Critter_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed
            || desktopMode)
            return;

        movementTimer.Stop();

        try
        {
            DragMove();
        }
        finally
        {
            if (!isPaused)
                movementTimer.Start();
        }
    }

    private void Species_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is MenuItem item &&
            Enum.TryParse(
                item.Tag?.ToString(),
                out CritterSpecies species))
        {
            critter.ChangeSpecies(species);
            UpdateAppearance();
        }
    }

    private void Pause_Click(
        object sender,
        RoutedEventArgs e)
    {
        isPaused = !isPaused;

        if (isPaused)
            movementTimer.Stop();
        else
            movementTimer.Start();

        PauseMenuItem.Header = isPaused
            ? "Resume Movement"
            : "Pause Movement";

        UpdateLabel();
    }

    private void DesktopMode_Click(
        object sender,
        RoutedEventArgs e)
    {
        ToggleDesktopMode();
    }

    private void Exit_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }

    private void Window_Closed(
        object? sender,
        EventArgs e)
    {
        movementTimer.Stop();

        if (desktopMode)
            DesktopLayer.Detach();

        if (windowHandle != IntPtr.Zero)
        {
            UnregisterHotKey(windowHandle, ToggleHotkey);
            UnregisterHotKey(windowHandle, ExitHotkey);
        }

        windowSource?.RemoveHook(WindowProcedure);
    }
}
