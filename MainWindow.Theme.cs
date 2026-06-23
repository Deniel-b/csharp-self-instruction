using System.Windows;
using System.Windows.Media;

namespace kursach;

public partial class MainWindow
{
    private void ThemeToggle_Checked(object sender, RoutedEventArgs e)
    {
        ThemeToggle.Content = "Светлая тема";
        ApplyTheme(isDark: true);
    }

    private void ThemeToggle_Unchecked(object sender, RoutedEventArgs e)
    {
        ThemeToggle.Content = "Темная тема";
        ApplyTheme(isDark: false);
    }

    private void ApplyTheme(bool isDark)
    {
        if (isDark)
        {
            SetGradientBrushColors("AppBackground", Color.FromRgb(0x0B, 0x14, 0x22), Color.FromRgb(0x08, 0x11, 0x1D));
            SetGradientBrushColors("HeroBrush", Color.FromRgb(0x16, 0x25, 0x3A), Color.FromRgb(0x1E, 0x31, 0x4A));

            SetBrushColor("SidebarBackground", Color.FromRgb(0x0E, 0x17, 0x26));
            SetBrushColor("SidebarSurface", Color.FromRgb(0x16, 0x23, 0x34));
            SetBrushColor("SidebarBorder", Color.FromRgb(0x22, 0x36, 0x4E));
            SetBrushColor("SidebarText", Color.FromRgb(0xE7, 0xEE, 0xF8));
            SetBrushColor("SidebarMuted", Color.FromRgb(0x9D, 0xB0, 0xC6));

            SetBrushColor("SurfaceCard", Color.FromRgb(0x0F, 0x1A, 0x2B));
            SetBrushColor("SurfaceMuted", Color.FromRgb(0x18, 0x26, 0x3B));
            SetBrushColor("SurfaceBorder", Color.FromRgb(0x2A, 0x3C, 0x55));
            SetBrushColor("TextPrimary", Color.FromRgb(0xE8, 0xF0, 0xFA));
            SetBrushColor("TextSecondary", Color.FromRgb(0xA9, 0xBB, 0xD0));

            SetBrushColor("InputBackground", Color.FromRgb(0x11, 0x1E, 0x30));
            SetBrushColor("InputForeground", Color.FromRgb(0xE8, 0xF0, 0xFA));
            SetBrushColor("InputBorder", Color.FromRgb(0x2A, 0x3D, 0x57));
            SetBrushColor("AccentMuted", Color.FromRgb(0x4A, 0x2F, 0x23));
            SetBrushColor("ControlBorder", Color.FromRgb(0x2A, 0x3D, 0x57));
            SetBrushColor("ControlHoverBackground", Color.FromRgb(0x1D, 0x2F, 0x47));
            SetBrushColor("ControlHoverBorder", Color.FromRgb(0x39, 0x53, 0x6F));
            SetBrushColor("ControlPressedBackground", Color.FromRgb(0x24, 0x38, 0x54));
            SetBrushColor("HeroCaption", Color.FromRgb(0xC8, 0xD8, 0xEB));
            SetBrushColor("HeroTitle", Colors.White);
            SetBrushColor("ContextPanelBackground", Color.FromRgb(0x1A, 0x27, 0x3A));
            SetBrushColor("ContextInnerBackground", Color.FromRgb(0x23, 0x34, 0x4B));
            SetBrushColor("TaskCardBackground", Color.FromRgb(0x1B, 0x2C, 0x42));
            SetBrushColor("TaskCodeBackground", Color.FromRgb(0x12, 0x1F, 0x33));
            SetBrushColor("TaskCardBorder", Color.FromRgb(0x32, 0x4A, 0x68));
            SetBrushColor("TaskHintText", Color.FromRgb(0xA6, 0xB7, 0xCB));
            SetBrushColor("StatusInfo", Color.FromRgb(0xA3, 0xB5, 0xCB));
            SetBrushColor("StatusWarning", Color.FromRgb(0xF0, 0xB3, 0x5C));
            SetBrushColor("StatusSuccess", Color.FromRgb(0x57, 0xC7, 0x88));
            SetBrushColor("StatusError", Color.FromRgb(0xFF, 0x80, 0x80));

            _pageButtonForeground = new SolidColorBrush(Color.FromRgb(0xD6, 0xE0, 0xEC));
            _pageButtonSelectedForeground = new SolidColorBrush(Colors.White);
            _pageButtonSelectedBackground = new SolidColorBrush(Color.FromRgb(0xFF, 0x7A, 0x3D));
            _pageButtonHoverBackground = new SolidColorBrush(Color.FromRgb(0x1D, 0x2F, 0x47));
            _pageButtonDefaultBackground = Brushes.Transparent;

            _stepperDefaultBackground = new SolidColorBrush(Color.FromRgb(0x1D, 0x2F, 0x47));
            _stepperHoverBackground = new SolidColorBrush(Color.FromRgb(0x29, 0x40, 0x5E));
            _stepperSelectedBackground = new SolidColorBrush(Color.FromRgb(0xFF, 0x7A, 0x3D));
            _stepperForeground = new SolidColorBrush(Colors.White);
            _stepperInactiveForeground = new SolidColorBrush(Color.FromRgb(0xA8, 0xB8, 0xCD));
        }
        else
        {
            SetGradientBrushColors("AppBackground", Color.FromRgb(0xF5, 0xF7, 0xFB), Color.FromRgb(0xE9, 0xEF, 0xF8));
            SetGradientBrushColors("HeroBrush", Color.FromRgb(0x1F, 0x32, 0x47), Color.FromRgb(0x2D, 0x47, 0x61));

            SetBrushColor("SidebarBackground", Color.FromRgb(0xEE, 0xF3, 0xFA));
            SetBrushColor("SidebarSurface", Color.FromRgb(0xDD, 0xE7, 0xF4));
            SetBrushColor("SidebarBorder", Color.FromRgb(0xC6, 0xD3, 0xE4));
            SetBrushColor("SidebarText", Color.FromRgb(0x1E, 0x2A, 0x3A));
            SetBrushColor("SidebarMuted", Color.FromRgb(0x5F, 0x70, 0x84));

            SetBrushColor("SurfaceCard", Colors.White);
            SetBrushColor("SurfaceMuted", Color.FromRgb(0xF6, 0xF8, 0xFC));
            SetBrushColor("SurfaceBorder", Color.FromRgb(0xDF, 0xE7, 0xF2));
            SetBrushColor("TextPrimary", Color.FromRgb(0x1C, 0x21, 0x2B));
            SetBrushColor("TextSecondary", Color.FromRgb(0x5F, 0x70, 0x84));

            SetBrushColor("InputBackground", Colors.White);
            SetBrushColor("InputForeground", Color.FromRgb(0x1C, 0x21, 0x2B));
            SetBrushColor("InputBorder", Color.FromRgb(0xC7, 0xD4, 0xE4));
            SetBrushColor("AccentMuted", Color.FromRgb(0xFF, 0xE2, 0xD0));
            SetBrushColor("ControlBorder", Color.FromRgb(0xCF, 0xDA, 0xE8));
            SetBrushColor("ControlHoverBackground", Color.FromRgb(0xEE, 0xF4, 0xFF));
            SetBrushColor("ControlHoverBorder", Color.FromRgb(0xBF, 0xD0, 0xEA));
            SetBrushColor("ControlPressedBackground", Color.FromRgb(0xE3, 0xEC, 0xFA));
            SetBrushColor("HeroCaption", Color.FromRgb(0xC8, 0xD8, 0xEB));
            SetBrushColor("HeroTitle", Colors.White);
            SetBrushColor("ContextPanelBackground", Color.FromRgb(0xF2, 0xF5, 0xF9));
            SetBrushColor("ContextInnerBackground", Colors.White);
            SetBrushColor("TaskCardBackground", Colors.White);
            SetBrushColor("TaskCodeBackground", Color.FromRgb(0xF7, 0xFA, 0xFF));
            SetBrushColor("TaskCardBorder", Color.FromRgb(0xD8, 0xE1, 0xEE));
            SetBrushColor("TaskHintText", Color.FromRgb(0x6C, 0x77, 0x87));
            SetBrushColor("StatusInfo", Color.FromRgb(0x6B, 0x7C, 0x93));
            SetBrushColor("StatusWarning", Color.FromRgb(0xB7, 0x79, 0x2C));
            SetBrushColor("StatusSuccess", Color.FromRgb(0x2D, 0x8A, 0x53));
            SetBrushColor("StatusError", Color.FromRgb(0xBF, 0x47, 0x47));

            _pageButtonForeground = new SolidColorBrush(Color.FromRgb(0x1C, 0x21, 0x2B));
            _pageButtonSelectedForeground = new SolidColorBrush(Colors.White);
            _pageButtonSelectedBackground = new SolidColorBrush(Color.FromRgb(0xFF, 0x7A, 0x3D));
            _pageButtonHoverBackground = new SolidColorBrush(Color.FromRgb(0xDE, 0xE6, 0xF1));
            _pageButtonDefaultBackground = Brushes.Transparent;

            _stepperDefaultBackground = new SolidColorBrush(Color.FromRgb(0xE2, 0xEB, 0xF7));
            _stepperHoverBackground = new SolidColorBrush(Color.FromRgb(0xD4, 0xE0, 0xEF));
            _stepperSelectedBackground = new SolidColorBrush(Color.FromRgb(0xFF, 0x7A, 0x3D));
            _stepperForeground = new SolidColorBrush(Color.FromRgb(0x1C, 0x21, 0x2B));
            _stepperInactiveForeground = new SolidColorBrush(Color.FromRgb(0x5E, 0x6F, 0x84));
        }

        ApplyFixedElementTheme();
        RenderNavigation();
        ApplyReadingDocumentTheme();
        if (IsLoaded && _viewModel.Navigator is not null)
        {
            LoadPage();
        }

        UpdateNavigationHighlight();
    }

    private void SetBrushColor(string key, Color color)
    {
        if (FindResource(key) is SolidColorBrush brush)
        {
            if (brush.IsFrozen)
            {
                Resources[key] = new SolidColorBrush(color);
                return;
            }

            brush.Color = color;
        }
    }

    private void SetGradientBrushColors(string key, Color first, Color second)
    {
        if (FindResource(key) is not LinearGradientBrush gradient || gradient.GradientStops.Count < 2)
        {
            return;
        }

        if (gradient.IsFrozen)
        {
            var clone = gradient.CloneCurrentValue();
            clone.GradientStops[0].Color = first;
            clone.GradientStops[1].Color = second;
            Resources[key] = clone;
            return;
        }

        gradient.GradientStops[0].Color = first;
        gradient.GradientStops[1].Color = second;
    }

    private void ApplyFixedElementTheme()
    {
        ContentCard.Background = ResolveBrush("SurfaceCard", Brushes.White);
        ContextPanel.Background = ResolveBrush("ContextPanelBackground", Brushes.LightGray);
        AdminPanel.Background = ResolveBrush("SurfaceMuted", Brushes.WhiteSmoke);
        ChaptersPanel.Background = Brushes.Transparent;
    }
}
