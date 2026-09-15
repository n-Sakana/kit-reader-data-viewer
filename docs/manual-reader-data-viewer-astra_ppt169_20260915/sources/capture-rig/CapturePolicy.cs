using System;
using System.Windows;

public static class ManualCapturePolicy
{
    // Host-only placement policy. The product sources remain byte-for-byte unchanged.
    public static void Install(Type[] windowTypes)
    {
        foreach (Type type in windowTypes)
        {
            Window.LeftProperty.OverrideMetadata(type, new FrameworkPropertyMetadata(-32000.0, null, Offscreen));
            Window.TopProperty.OverrideMetadata(type, new FrameworkPropertyMetadata(-32000.0, null, Offscreen));
            Window.ShowActivatedProperty.OverrideMetadata(type, new FrameworkPropertyMetadata(false, null, Disabled));
            Window.ShowInTaskbarProperty.OverrideMetadata(type, new FrameworkPropertyMetadata(false, null, Disabled));
        }
    }
    private static object Offscreen(DependencyObject obj, object value) { return -32000.0; }
    private static object Disabled(DependencyObject obj, object value) { return false; }
}
