using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Windows;
using App = Autodesk.AutoCAD.ApplicationServices.Application;

namespace AutoCAD2024McpAddin;

public static class PaletteController
{
    private static PaletteSet? _paletteSet;

    public static void TogglePalette()
    {
        if (_paletteSet == null)
        {
            _paletteSet = new PaletteSet("AI CAD / MCP Bridge", new Guid("7A9F2184-752B-4CD7-9A74-12C85C01372E"))
            {
                Style = PaletteSetStyles.ShowAutoHideButton |
                        PaletteSetStyles.ShowCloseButton |
                        PaletteSetStyles.ShowPropertiesMenu,
                MinimumSize = new System.Drawing.Size(260, 420),
                Dock = DockSides.Right
            };

            var panel = CreateWpfControl();
            _paletteSet.AddVisual("Controls", panel);
        }

        _paletteSet.Visible = !_paletteSet.Visible;
    }

    private static UserControl CreateWpfControl()
    {
        var uc = new UserControl
        {
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30))
        };

        var sp = new StackPanel { Margin = new Thickness(12) };

        // Header
        var title = new TextBlock
        {
            Text = "CIVIL 3D / CAD MCP",
            FontWeight = FontWeights.Bold,
            FontSize = 14,
            Foreground = System.Windows.Media.Brushes.White,
            Margin = new Thickness(0, 0, 0, 4)
        };
        var subtitle = new TextBlock
        {
            Text = "AutoCAD 2024 Edition",
            FontSize = 11,
            Foreground = System.Windows.Media.Brushes.LightGray,
            Margin = new Thickness(0, 0, 0, 16)
        };
        sp.Children.Add(title);
        sp.Children.Add(subtitle);

        // Section 1: Server Controls
        sp.Children.Add(CreateHeaderLabel("MCP SERVER BRIDGE"));
        sp.Children.Add(CreateActionButton("> Start Server", System.Windows.Media.Color.FromRgb(46, 139, 87), "C3DMCPSTART "));
        sp.Children.Add(CreateActionButton("* Refresh / Restart", System.Windows.Media.Color.FromRgb(30, 144, 255), "C3DMCPREFRESH "));
        sp.Children.Add(CreateActionButton("X Stop Server", System.Windows.Media.Color.FromRgb(205, 92, 92), "C3DMCPSTOP "));
        sp.Children.Add(CreateActionButton("i Server Status", System.Windows.Media.Color.FromRgb(105, 105, 105), "C3DMCPSTATUS "));

        // Section 2: Title Block Automation
        sp.Children.Add(CreateHeaderLabel("TITLE BLOCK TOOLS"));
        sp.Children.Add(CreateActionButton("TB Dump Title Block", System.Windows.Media.Color.FromRgb(184, 134, 11), "C3DTBDUMP "));
        sp.Children.Add(CreateActionButton("REV Update Revision", System.Windows.Media.Color.FromRgb(138, 43, 226), "C3DTBREV "));
        sp.Children.Add(CreateActionButton("BAT Run Batch Process", System.Windows.Media.Color.FromRgb(47, 79, 79), "C3DTBBATCH "));

        uc.Content = new ScrollViewer { Content = sp };
        return uc;
    }

    private static TextBlock CreateHeaderLabel(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(160, 160, 160)),
            Margin = new Thickness(0, 8, 0, 6)
        };
    }

    private static System.Windows.Controls.Button CreateActionButton(string text, System.Windows.Media.Color bgColor, string command)
    {
        var btn = new System.Windows.Controls.Button
        {
            Content = text,
            Height = 34,
            Margin = new Thickness(0, 0, 0, 8),
            Foreground = System.Windows.Media.Brushes.White,
            Background = new SolidColorBrush(bgColor),
            FontWeight = FontWeights.Medium,
            Cursor = System.Windows.Input.Cursors.Hand
        };
        btn.Click += (s, e) =>
        {
            var doc = App.DocumentManager.MdiActiveDocument;
            doc?.SendStringToExecute(command, true, false, false);
        };
        return btn;
    }
}
