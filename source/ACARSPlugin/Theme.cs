using System.Windows;
using System.Windows.Media;

namespace ACARSPlugin;

public static class Theme
{
    public static float Alpha = 0.4f;
    public static Brush LightBrush = new SolidColorBrush(Color.FromScRgb(Alpha, 255, 255, 255));
    public static Brush DarkBrush = new SolidColorBrush(Color.FromScRgb(Alpha, 0, 0, 0));

    public static SolidColorBrush BackgroundColor { get; set; } = new(System.Drawing.Color.FromArgb(255, 160, 170, 170).ToWindowsColor());
    public static SolidColorBrush GenericTextColor { get; set; } = new(System.Drawing.Color.FromArgb(255, 96, 0, 0).ToWindowsColor());
    public static SolidColorBrush InteractiveTextColor { get; set; } = new(System.Drawing.Color.FromArgb(255, 0, 0, 96).ToWindowsColor());
    public static SolidColorBrush NonInteractiveTextColor { get; set; } = new(System.Drawing.Color.FromArgb(255, 90, 90, 90).ToWindowsColor());
    public static SolidColorBrush SelectedButtonColor { get; set; } = new(System.Drawing.Color.FromArgb(255, 0, 0, 96).ToWindowsColor());

    public static SolidColorBrush CPDLCUplinkBackgroundColor { get; set; } = new(System.Drawing.Color.FromArgb(255, 000, 000, 096).ToWindowsColor());
    public static SolidColorBrush CPDLCDownlinkColor { get; set; } = new(System.Drawing.Color.FromArgb(255, 000,105,000).ToWindowsColor());
    public static SolidColorBrush CPDLCSelectedDownlinkBackgroundColor { get; set; } = new(System.Drawing.Color.FromArgb(255, 255, 255, 230).ToWindowsColor());
    public static SolidColorBrush CPDLCClosedColor { get; set; } = new(System.Drawing.Color.FromArgb(255, 000, 000, 000).ToWindowsColor());
    public static SolidColorBrush CPDLCControllerLateColor { get; set; } = new(System.Drawing.Color.FromArgb(255, 000,000,255).ToWindowsColor());
    public static SolidColorBrush CPDLCPilotLateColor { get; set; } = new(System.Drawing.Color.FromArgb(255, 255 ,000, 105).ToWindowsColor());
    public static SolidColorBrush CPDLCBackgroundColor { get; set; } = new(System.Drawing.Color.FromArgb(255, 230, 210, 190).ToWindowsColor());
    public static SolidColorBrush CPDLCUrgentColor { get; set; } = new(System.Drawing.Color.FromArgb(255, 255,000, 000).ToWindowsColor());
    public static SolidColorBrush CPDLCFailedColor { get; set; } = new(System.Drawing.Color.FromArgb(255, 255,000, 255).ToWindowsColor());
    public static SolidColorBrush CPDLCHotButtonBackgroundColor { get; set; } = new(System.Drawing.Color.FromArgb(255, 101,101,255).ToWindowsColor());
    public static SolidColorBrush CPDLCSendBackgroundColor { get; set; } = new(System.Drawing.Color.FromArgb(255, 101,101,255).ToWindowsColor());
    
    public static SolidColorBrush CPDLCUnableDownlinkColor { get; set; } = new(System.Drawing.Color.FromArgb(255, 230, 127, 127).ToWindowsColor());
    public static SolidColorBrush CPDLCSuspendedColor { get; set; } = new(System.Drawing.Color.FromArgb(255, 255, 255, 255).ToWindowsColor());

    public static SolidColorBrush SelectedUtilityColor { get; set; } = new(System.Drawing.Color.FromArgb(255, 0, 255, 255).ToWindowsColor());
    
    // TODO: Support live updating font sizes
    public static FontFamily FontFamily { get; set; } = new("Terminus (TTF)");
    public static double FontSize { get; set; } = 16.0;
    public static FontWeight FontWeight { get; set; } = FontWeights.Bold;

    public static Thickness BeveledBorderThickness = new(2);
    public static double BeveledLineWidth = 4;
}
