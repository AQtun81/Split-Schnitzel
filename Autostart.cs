using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;

namespace Split_Schnitzel;

public partial class MainWindow
{
    private async void StartAndCaptureApplication(string pathWithArguments, FrameworkElement targetElement)
    {
        // path cannot be empty
        if (pathWithArguments == string.Empty) return;
        
        // quotation marks with arguments
        string exePath = pathWithArguments.StartsWith('"') ?
            Regex.Matches(pathWithArguments, "\"([^\"]*)\"")[0].Groups[1].Value :
            pathWithArguments;
        
        // executable has to exist
        if (!File.Exists(exePath)) return;
        
        // get arguments
        int argumentsStart = pathWithArguments.LastIndexOf('"');
        string arguments = argumentsStart > 0 && argumentsStart < pathWithArguments.Length - 1 ?
            pathWithArguments.Substring(argumentsStart + 2, pathWithArguments.Length - argumentsStart - 2) : "";
        
        // get working directory
        string workingDirectory = Path.GetDirectoryName(exePath);
        
        Console.WriteLine($"Executable path: {exePath}");
        Console.WriteLine($"Start in: {workingDirectory}");
        Console.WriteLine($"Arguments: {arguments}");
        
        // launch and capture the application
        await LaunchApplicationAsync();

        async Task LaunchApplicationAsync()
        {
            using Process process = new();
            // create process
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.FileName = exePath;
            process.StartInfo.WorkingDirectory = workingDirectory;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.Arguments = arguments;
            process.Start();
            
            await Task.Run(() => process.WaitForInputIdle());
            // wait additional 1 second for better compatibility with certain apps
            await Task.Delay(1000);

            if (!Extern.IsWindow(process.MainWindowHandle)) return;

            // get available window slot
            int windowSlot = 0;
            for (int i = 0; i < managedWindows.Length; i++)
            {
                if (managedWindows[i] is not null) continue;
                windowSlot = i;
            }
            
            BindWindowToTargetElement(windowSlot, process.MainWindowHandle, targetElement);
        }
    }
}