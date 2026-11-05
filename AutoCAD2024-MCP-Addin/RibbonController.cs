using System;
using System.Drawing;
using System.Windows.Input;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.Windows;
using App = Autodesk.AutoCAD.ApplicationServices.Application;

namespace AutoCAD2024McpAddin;

public static class RibbonController
{
    private const string TabId = "ID_TAB_CIVIL3D_MCP";
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized) return;

        if (ComponentManager.Ribbon == null)
        {
            ComponentManager.ItemInitialized += ComponentManager_ItemInitialized;
            return;
        }

        CreateRibbonUI();
        _initialized = true;
    }

    private static void ComponentManager_ItemInitialized(object sender, RibbonItemEventArgs e)
    {
        if (ComponentManager.Ribbon != null)
        {
            ComponentManager.ItemInitialized -= ComponentManager_ItemInitialized;
            CreateRibbonUI();
            _initialized = true;
        }
    }

    private static void CreateRibbonUI()
    {
        var ribbon = ComponentManager.Ribbon;
        if (ribbon == null) return;

        RibbonTab? tab = null;
        foreach (var t in ribbon.Tabs)
        {
            if (t.Id == TabId)
            {
                tab = t;
                break;
            }
        }

        if (tab == null)
        {
            tab = new RibbonTab
            {
                Title = "AI CAD MCP",
                Id = TabId
            };
            ribbon.Tabs.Add(tab);
        }

        tab.Panels.Clear();

        // Panel 1: MCP Server Controls
        var serverPanel = new RibbonPanel();
        var serverSource = new RibbonPanelSource { Title = "MCP Server Bridge" };

        var btnStart = new RibbonButton
        {
            Text = "Start\nServer",
            ShowText = true,
            ShowImage = true,
            Size = RibbonItemSize.Large,
            Orientation = System.Windows.Controls.Orientation.Vertical,
            LargeImage = IconHelper.CreateButtonIcon("\u25B6", Color.FromArgb(34, 139, 34), Color.White, 32),
            Image = IconHelper.CreateButtonIcon("\u25B6", Color.FromArgb(34, 139, 34), Color.White, 16),
            ToolTip = "Starts the MCP JSON-RPC TCP server (port 8080) and background Node bridge.",
            CommandHandler = new RelayCommand(_ => ExecuteCadCommand("C3DMCPSTART "))
        };

        var btnRefresh = new RibbonButton
        {
            Text = "Refresh\nServer",
            ShowText = true,
            ShowImage = true,
            Size = RibbonItemSize.Large,
            Orientation = System.Windows.Controls.Orientation.Vertical,
            LargeImage = IconHelper.CreateButtonIcon("\u21BB", Color.FromArgb(30, 144, 255), Color.White, 32),
            Image = IconHelper.CreateButtonIcon("\u21BB", Color.FromArgb(30, 144, 255), Color.White, 16),
            ToolTip = "Restarts the MCP server and resynchronizes active drawing context.",
            CommandHandler = new RelayCommand(_ => ExecuteCadCommand("C3DMCPREFRESH "))
        };

        var btnStop = new RibbonButton
        {
            Text = "Stop\nServer",
            ShowText = true,
            ShowImage = true,
            Size = RibbonItemSize.Large,
            Orientation = System.Windows.Controls.Orientation.Vertical,
            LargeImage = IconHelper.CreateButtonIcon("\u25A0", Color.FromArgb(220, 20, 60), Color.White, 32),
            Image = IconHelper.CreateButtonIcon("\u25A0", Color.FromArgb(220, 20, 60), Color.White, 16),
            ToolTip = "Stops the MCP listener and terminates background bridge processes.",
            CommandHandler = new RelayCommand(_ => ExecuteCadCommand("C3DMCPSTOP "))
        };

        var btnStatus = new RibbonButton
        {
            Text = "Status",
            ShowText = true,
            ShowImage = true,
            Size = RibbonItemSize.Standard,
            Image = IconHelper.CreateButtonIcon("i", Color.FromArgb(100, 100, 100), Color.White, 16),
            ToolTip = "Displays current server uptime, port, and pending task status.",
            CommandHandler = new RelayCommand(_ => ExecuteCadCommand("C3DMCPSTATUS "))
        };

        var btnPalette = new RibbonButton
        {
            Text = "Palette UI",
            ShowText = true,
            ShowImage = true,
            Size = RibbonItemSize.Standard,
            Image = IconHelper.CreateButtonIcon("P", Color.FromArgb(70, 130, 180), Color.White, 16),
            ToolTip = "Toggles floating/dockable MCP Control Palette.",
            CommandHandler = new RelayCommand(_ => ExecuteCadCommand("C3DMCPPALETTE "))
        };

        serverSource.Items.Add(btnStart);
        serverSource.Items.Add(btnRefresh);
        serverSource.Items.Add(btnStop);

        var statusRow = new RibbonRowPanel();
        statusRow.Items.Add(btnStatus);
        statusRow.Items.Add(btnPalette);
        serverSource.Items.Add(statusRow);

        serverPanel.Source = serverSource;
        tab.Panels.Add(serverPanel);

        // Panel 2: Title Block Tools
        var tbPanel = new RibbonPanel();
        var tbSource = new RibbonPanelSource { Title = "Title Block Automation" };

        var btnTbDump = new RibbonButton
        {
            Text = "Dump\nTitle Block",
            ShowText = true,
            ShowImage = true,
            Size = RibbonItemSize.Large,
            Orientation = System.Windows.Controls.Orientation.Vertical,
            LargeImage = IconHelper.CreateButtonIcon("TB", Color.FromArgb(218, 165, 32), Color.White, 32),
            Image = IconHelper.CreateButtonIcon("TB", Color.FromArgb(218, 165, 32), Color.White, 16),
            ToolTip = "Dumps all title block attributes and layout entities to tb_dump2.txt.",
            CommandHandler = new RelayCommand(_ => ExecuteCadCommand("C3DTBDUMP "))
        };

        var btnTbRev = new RibbonButton
        {
            Text = "Update\nRevision",
            ShowText = true,
            ShowImage = true,
            Size = RibbonItemSize.Large,
            Orientation = System.Windows.Controls.Orientation.Vertical,
            LargeImage = IconHelper.CreateButtonIcon("REV", Color.FromArgb(147, 112, 219), Color.White, 32),
            Image = IconHelper.CreateButtonIcon("REV", Color.FromArgb(147, 112, 219), Color.White, 16),
            ToolTip = "Updates revision letter and attributes in active layout title block.",
            CommandHandler = new RelayCommand(_ => ExecuteCadCommand("C3DTBREV "))
        };

        var btnTbBatch = new RibbonButton
        {
            Text = "Batch CAD\nRunner",
            ShowText = true,
            ShowImage = true,
            Size = RibbonItemSize.Large,
            Orientation = System.Windows.Controls.Orientation.Vertical,
            LargeImage = IconHelper.CreateButtonIcon("BAT", Color.FromArgb(47, 79, 79), Color.White, 32),
            Image = IconHelper.CreateButtonIcon("BAT", Color.FromArgb(47, 79, 79), Color.White, 16),
            ToolTip = "Executes headless batch Title Block processing across a folder of DWGs.",
            CommandHandler = new RelayCommand(_ => ExecuteCadCommand("C3DTBBATCH "))
        };

        tbSource.Items.Add(btnTbDump);
        tbSource.Items.Add(btnTbRev);
        tbSource.Items.Add(btnTbBatch);

        tbPanel.Source = tbSource;
        tab.Panels.Add(tbPanel);
    }

    private static void ExecuteCadCommand(string command)
    {
        var doc = App.DocumentManager.MdiActiveDocument;
        if (doc != null)
        {
            doc.SendStringToExecute(command, true, false, false);
        }
    }
}

public sealed class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _execute(parameter);
    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }
}
