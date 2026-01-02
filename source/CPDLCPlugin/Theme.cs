using System.Windows;
using System.Windows.Media;
using CPDLCPlugin.Extensions;

namespace CPDLCPlugin;

public static class Theme
{
    public static float Alpha = 0.4f;
    public static Brush LightBrush = new SolidColorBrush(Color.FromScRgb(Alpha, 255, 255, 255));
    public static Brush DarkBrush = new SolidColorBrush(Color.FromScRgb(Alpha, 0, 0, 0));

    public static SolidColorBrush BackgroundColor { get; set; } = CreateColor(255, 160, 170, 170);
    public static SolidColorBrush GenericTextColor { get; set; } = CreateColor(255, 96, 0, 0);
    public static SolidColorBrush InteractiveTextColor { get; set; } = CreateColor(255, 0, 0, 96);
    public static SolidColorBrush NonInteractiveTextColor { get; set; } = CreateColor(255, 90, 90, 90);
    public static SolidColorBrush SelectedButtonColor { get; set; } = CreateColor(255, 0, 0, 96);

    public static SolidColorBrush CPDLCBackgroundColor { get; set; } = CreateColor(255, 230, 210, 190);
    public static SolidColorBrush CPDLCUplinkColor { get; set; } = CreateColor(255, 000, 000, 096);
    public static SolidColorBrush CPDLCDownlinkColor { get; set; } = CreateColor(255, 000,105,000);
    public static SolidColorBrush CPDLCSelectedDownlinkBackgroundColor { get; set; } = CreateColor(255, 220, 220, 220);
    public static SolidColorBrush CPDLCClosedColor { get; set; } = CreateColor(255, 000, 000, 000);
    public static SolidColorBrush CPDLCControllerLateColor { get; set; } = CreateColor(255, 170, 255, 170);
    public static SolidColorBrush CPDLCPilotLateColor { get; set; } = CreateColor(255, 000, 255 ,255);
    public static SolidColorBrush CPDLCUrgentColor { get; set; } = CreateColor(255, 209, 046, 046);
    public static SolidColorBrush CPDLCFailedColor { get; set; } = CreateColor(255, 255, 255, 0);
    public static SolidColorBrush CPDLCHotButtonBackgroundColor { get; set; } = CreateColor(255, 101,101,255);
    public static SolidColorBrush CPDLCSendBackgroundColor { get; set; } = CreateColor(255, 101,101,255);

    public static SolidColorBrush CPDLCUnableDownlinkColor { get; set; } = CreateColor(255, 230, 127, 127);
    public static SolidColorBrush CPDLCSuspendedColor { get; set; } = CreateColor(255, 255, 255, 255);

    public static SolidColorBrush SelectedUtilityColor { get; set; } = CreateColor(255, 0, 255, 255);

    // TODO: Support live updating font sizes
    public static FontFamily FontFamily { get; set; } = new("Terminus (TTF)");
    public static double FontSize { get; set; } = 16.0;
    public static FontWeight FontWeight { get; set; } = FontWeights.Bold;

    public static Thickness BeveledBorderThickness = new(2);
    public static double BeveledLineWidth = 4;

    static SolidColorBrush CreateColor(int alpha, int red, int green, int blue)
    {
        return new SolidColorBrush(System.Drawing.Color.FromArgb(alpha, red, green, blue).ToWindowsColor());
    }
}
