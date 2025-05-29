using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Controls.ApplicationLifetimes;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using Avalonia;
using ReactiveUI;

namespace ProcessManagerApp.Views
{

public partial class MainWindow : Window
{
    public ObservableCollection<ProcessInfo> Processes { get; set; } = new();

    public MainWindow()
    {
        InitializeComponent();
        RefreshProcesses();
        ProcessGrid.Items = Processes;
    }

    private void RefreshProcesses()
    {
        Processes.Clear();
        foreach (var proc in Process.GetProcesses())
        {
            try
            {
                Processes.Add(new ProcessInfo
                {
                    PID = proc.Id,
                    Name = proc.ProcessName,
                    Status = proc.Responding ? "Running" : "Not Responding"
                });
            }
            catch { continue; }
        }
    }

    private async void OnExportClick(object? sender, RoutedEventArgs e)
    {
        var sfd = new SaveFileDialog
        {
            Filters = { new FileDialogFilter() { Name = "Text Files", Extensions = { "txt" } } },
            DefaultExtension = "txt"
        };

        var path = await sfd.ShowAsync(this);
        if (!string.IsNullOrWhiteSpace(path))
        {
            try
            {
                File.WriteAllLines(path, Processes.Select(p => $"{p.PID} {p.Name} {p.Status}"));
                await MessageBox("Експорт завершено", $"Файл збережено в {path}");
            }
            catch (Exception ex)
            {
                await MessageBox("Помилка", ex.Message);
            }
        }
    }

    private void OnRefreshClick(object? sender, RoutedEventArgs e) => RefreshProcesses();

    private async void OnContextMenuOpening(object? sender, ContextMenuEventArgs e)
    {
        if (ProcessGrid.SelectedItem is not ProcessInfo selected)
            return;

        var menu = new ContextMenu
        {
            Items = new[]
            {
                new MenuItem { Header = "Інформація про процес", Command = ReactiveCommand.CreateFromTask(() => ShowProcessInfo(selected)) },
                new MenuItem { Header = "Завершити процес", Command = ReactiveCommand.Create(() => KillProcess(selected)) },
                new MenuItem { Header = "Потоки і модулі", Command = ReactiveCommand.CreateFromTask(() => ShowThreadsAndModules(selected)) },
            }
        };
        menu.Open(ProcessGrid);
    }

    private async Task ShowProcessInfo(ProcessInfo procInfo)
    {
        try
        {
            var proc = Process.GetProcessById(procInfo.PID);
            var info = $"PID: {proc.Id}\nName: {proc.ProcessName}\nMemory: {proc.PrivateMemorySize64 / 1024 / 1024} MB\n" +
                       $"Threads: {proc.Threads.Count}\nStart Time: {proc.StartTime}";
            await MessageBox("Інформація", info);
        }
        catch (Exception ex)
        {
            await MessageBox("Помилка", ex.Message);
        }
    }

    private void KillProcess(ProcessInfo procInfo)
    {
        try
        {
            var proc = Process.GetProcessById(procInfo.PID);
            proc.Kill();
            RefreshProcesses();
        }
        catch { }
    }

    private async Task ShowThreadsAndModules(ProcessInfo procInfo)
    {
        try
        {
            var proc = Process.GetProcessById(procInfo.PID);
            var threads = string.Join("\n", proc.Threads.Cast<ProcessThread>().Select(t => $"ID: {t.Id}, State: {t.ThreadState}"));
            var modules = proc.Modules.Count > 0
                ? string.Join("\n", proc.Modules.Cast<ProcessModule>().Take(10).Select(m => m.ModuleName))
                : "Немає доступу або немає модулів.";

            await MessageBox("Потоки і модулі", $"Потоки:\n{threads}\n\nМодулі:\n{modules}");
        }
        catch (Exception ex)
        {
            await MessageBox("Помилка", ex.Message);
        }
    }

    private Task MessageBox(string title, string content)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 300,
            Content = new TextBlock { Text = content, TextWrapping = Avalonia.Media.TextWrapping.Wrap, Margin = new Thickness(10) }
        };

        return dialog.ShowDialog((Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow);
    }

    public class ProcessInfo
    {
        public int PID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
}
