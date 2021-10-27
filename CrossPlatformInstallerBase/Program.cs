using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;
using System;
using System.IO;
using System.Reflection;

namespace CrossPlatformInstallerBase
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args) 
        {
            foreach(string s in args)
            {
                if (s == "-updatemode")
                    App.UpdateMode = true;
            }

            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                var exePath = Path.GetDirectoryName(entryAssembly.Location);
                if (exePath != null)
                    Directory.SetCurrentDirectory(exePath); // Not really required on windows, but required on mac for some reason or else it tries to use 
                                                            // the home directory for storage which breaks things.
            }

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        } 

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI();
    }
}
