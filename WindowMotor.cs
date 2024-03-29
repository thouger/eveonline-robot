﻿using static BotEngine;
namespace Sanderling.Motor;
public class WindowMotor : IMotor
{
    readonly public IntPtr WindowHandle;

    public const int MouseEventTimeDistanceMilliDefault = 120;

    public int MouseEventTimeDistanceMilli = MouseEventTimeDistanceMilliDefault;

    public int KeyboardEventTimeDistanceMilli = 40;

    /// <summary>
    /// For some reason, the mouse positions seem to be offset when moving the mouse in the window client area.
    /// </summary>
    static public Vektor2DInt MouseOffsetStatic = new Vektor2DInt(2, 2);

    public WindowMotor(IntPtr windowHandle)
    {
        this.WindowHandle = windowHandle;
    }

    static public void EnsureWindowIsForeground(
        IntPtr windowHandle)
    {
        var PreviousForegroundWindowHandle = WinApi.GetForegroundWindow();

        if (PreviousForegroundWindowHandle == windowHandle)
        {
            return;
        }

        WinApi.SetForegroundWindow(windowHandle);
    }

    void EnsureWindowIsForeground() => EnsureWindowIsForeground(WindowHandle);

    static readonly public IDictionary<KeyValuePair<MouseButtonIdEnum, bool>, Action<WindowsInput.IMouseSimulator>> mouseActionFromButtonIdAndState =
        new KeyValuePair<KeyValuePair<MouseButtonIdEnum, bool>, Action<WindowsInput.IMouseSimulator>>[]
    {
            new KeyValuePair<KeyValuePair<MouseButtonIdEnum, bool>, Action<WindowsInput.IMouseSimulator>>(
                new KeyValuePair<MouseButtonIdEnum, bool>(MouseButtonIdEnum.Left, false), mouse => mouse.LeftButtonUp()),

            new KeyValuePair<KeyValuePair<MouseButtonIdEnum, bool>, Action<WindowsInput.IMouseSimulator>>(
                new KeyValuePair<MouseButtonIdEnum, bool>(MouseButtonIdEnum.Left, true), mouse => mouse.LeftButtonDown()),

            new KeyValuePair<KeyValuePair<MouseButtonIdEnum, bool>, Action<WindowsInput.IMouseSimulator>>(
                new KeyValuePair<MouseButtonIdEnum, bool>(MouseButtonIdEnum.Right, false), mouse => mouse.RightButtonUp()),

            new KeyValuePair<KeyValuePair<MouseButtonIdEnum, bool>, Action<WindowsInput.IMouseSimulator>>(
                new KeyValuePair<MouseButtonIdEnum, bool>(MouseButtonIdEnum.Right, true), mouse => mouse.RightButtonDown()),
    }.ToDictionary();

    public void ActSequenceMotion(IEnumerable<BotEngine.Motion> seqMotion)
    {
        try
        {
            if (null == seqMotion)
                return;

            var InputSimulator = new WindowsInput.InputSimulator();

            //foreach (var Motion in seqMotion.WhereNotDefault())
            foreach (var Motion in seqMotion)
            {
                var MotionMousePosition = Motion?.MousePosition;
                var MotionTextEntry = Motion?.TextEntry;
                var mouseLocationOnScreen = MotionMousePosition.HasValue ? WindowHandle.ClientToScreen(MotionMousePosition.Value) + MouseOffsetStatic : null;

                if (mouseLocationOnScreen.HasValue || (Motion.WindowToForeground ?? false))
                    EnsureWindowIsForeground();

                if (mouseLocationOnScreen.HasValue)
                {
                    WinApi.SetCursorPos((int)mouseLocationOnScreen.Value.A, (int)mouseLocationOnScreen.Value.B);

                    Thread.Sleep(MouseEventTimeDistanceMilli);
                }

                KeyValuePair<MouseButtonIdEnum, bool>[] mouseSetAction = new[]
                {
                    Motion?.MouseButtonDown?.Select(button => new KeyValuePair<MouseButtonIdEnum, bool>(button, true)),
                    Motion?.MouseButtonUp?.Select(button => new KeyValuePair<MouseButtonIdEnum, bool>(button, false)),
                }.ConcatNullable2().ToArray();

                if (0 < mouseSetAction?.Length)
                {
                    foreach (var mouseAction in mouseSetAction)
                        mouseActionFromButtonIdAndState?.TryGetValueOrDefault(mouseAction)?.Invoke(InputSimulator.Mouse);

                    Thread.Sleep(MouseEventTimeDistanceMilli);
                }

                Motion?.KeyDown?.ForEach(keyDown =>
                {
                    EnsureWindowIsForeground();
                    InputSimulator.Keyboard.KeyDown(keyDown);
                    Thread.Sleep(KeyboardEventTimeDistanceMilli);
                });

                Motion?.KeyUp?.ForEach(keyUp =>
                {
                    EnsureWindowIsForeground();
                    InputSimulator.Keyboard.KeyUp(keyUp);
                    Thread.Sleep(KeyboardEventTimeDistanceMilli);
                });

                if (0 < MotionTextEntry?.Length)
                {
                    EnsureWindowIsForeground();
                    InputSimulator.Keyboard.TextEntry(MotionTextEntry);
                }
            }

        }
        catch (Exception)
        {
        }
    }
}
