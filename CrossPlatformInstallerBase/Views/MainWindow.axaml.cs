#nullable disable

using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using CrossPlatformInstallerBase.json;
using CrossPlatformInstallerBase.ViewModels;
using Ionic.Zip;
using IWshRuntimeLibrary;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CrossPlatformInstallerBase.Views
{
    public partial class MainWindow : Window
    {
        #region Controls
        public Button NextButton;
        public Button BackButton;
        public Grid Grid1;
        public Grid Grid2;
        public Grid Grid3;
        public Grid Grid4;
        public Grid Grid5;
        public Grid GridReinstall;
        public Grid GridMoveInstall;
        public Grid GridUninstall;
        public Grid GridUninstallProgress;
        public PageSlide PageSlider;
        public TextBox InstallLocationBox;
        public ProgressBar InstallProgressBar;
        public ToggleSwitch MakeStartMenuShortcut;
        public ToggleSwitch MakeDesktopShortcut;
        public ToggleSwitch RunProgramAfterClosing;
        public Button BrowseButton;
        public TextBlock InvalidPathText;
        public ProgressBar PreReqBar;
        public Button ReinstallButton;
        public Button MoveInstallButton;
        public Button UninstallButton;
        public ProgressBar MoveFolderBar;
        public ToggleSwitch RemoveStartShortcut; 
        public ToggleSwitch RemoveDesktopShortcut;
        public ProgressBar UninstallBar;
        #endregion

        public List<Page> Pages { get; set; } = new List<Page>();

        public int PageIndex { get; set; } = 0;

        public bool CanSwitchPages { get; set; } = true;

        public static string? EXEPath { get; set; } = null;

        public bool CanClose { get; set; } = true;

        public bool ProgramIsAlreadyInstalled { get; set; } = false;

        public bool Reinstalling { get; set; } = false;

        public string RegistryPath { get; set; } = string.Empty;

        public string ReinstallPath { get; set; } = string.Empty;

        public string MoveFolderPath { get; set; } = string.Empty;

        public const int ProgramVersion = 3; // This version number does not have anything to do with a format like 1.0.0. Any time the version number updates at all, even for just a patch
                                             // this number should increase by one.

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            uint attr = 19;
            int val = 1;
            int i = App.DwmSetWindowAttribute(PlatformImpl.Handle.Handle, attr, ref val, sizeof(int));
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            DataContext = new MainWindowViewModel();

            NextButton = this.FindControl<Button>("NextButton");
            BackButton = this.FindControl<Button>("BackButton");
            Grid1 = this.FindControl<Grid>("Grid1");
            Grid2 = this.FindControl<Grid>("Grid2");
            Grid3 = this.FindControl<Grid>("Grid3");
            Grid4 = this.FindControl<Grid>("Grid4");
            Grid5 = this.FindControl<Grid>("Grid5");
            GridReinstall = this.FindControl<Grid>("GridReinstall");
            GridMoveInstall = this.FindControl<Grid>("GridMoveInstall");
            GridUninstall = this.FindControl<Grid>("GridUninstall");
            GridUninstallProgress = this.FindControl<Grid>("GridUninstallProgress");
            PageSlider = this.FindResource("PageSlider") as PageSlide;
            InstallLocationBox = this.FindControl<TextBox>("InstallLocationBox");
            InstallProgressBar = this.FindControl<ProgressBar>("InstallProgressBar");
            MakeStartMenuShortcut = this.FindControl<ToggleSwitch>("MakeStartMenuShortcut");
            MakeDesktopShortcut = this.FindControl<ToggleSwitch>("MakeDesktopShortcut");
            RunProgramAfterClosing = this.FindControl<ToggleSwitch>("RunProgramAfterClosing");
            BrowseButton = this.FindControl<Button>("BrowseButton");
            InvalidPathText = this.FindControl<TextBlock>("InvalidPathText");
            PreReqBar = this.FindControl<ProgressBar>("PreReqBar");
            ReinstallButton = this.FindControl<Button>("ReinstallButton");
            MoveInstallButton = this.FindControl<Button>("MoveInstallButton");
            UninstallButton = this.FindControl<Button>("UninstallButton");
            MoveFolderBar = this.FindControl<ProgressBar>("MoveFolderBar");
            RemoveStartShortcut = this.FindControl<ToggleSwitch>("RemoveStartShortcut");
            RemoveDesktopShortcut = this.FindControl<ToggleSwitch>("RemoveDesktopShortcut");
            UninstallBar = this.FindControl<ProgressBar>("UninstallBar");

            Pages.Add(new Page(this, Grid1, true, false, "I have read and accept the terms above"));
            Pages.Add(new ConfigPage(this, Grid2, true, true, "Install"));
            Pages.Add(new PreReqPage(this, Grid3, false, false));
            Pages.Add(new InstallingPage(this, Grid4, true, false, "Next"));
            Pages.Add(new FinalPage(this, Grid5, true, false, "Finish"));
            Pages.Add(new MoveInstallPage(this, GridMoveInstall, false, false));
            Pages.Add(new UninstallPage(this, GridUninstall, true, false));
            Pages.Add(new UninstallProgressPage(this, GridUninstallProgress, false, false));

            NextButton.Click += NextButton_Click;
            BackButton.Click += BackButton_Click;

            BrowseButton.Click += BrowseButton_Click;

            Closing += MainWindow_Closing;

            ReinstallButton.Click += ReinstallButton_Click;
            MoveInstallButton.Click += MoveInstallButton_Click;
            UninstallButton.Click += UninstallButton_Click;

            Activated += (obj, e) =>
            {
                UpdateButtons();

                if (App.UpdateMode)
                    DisableButtons();
            };

#if WINDOWS
            // Check if the program has already been installed
            var key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\OcclusionVoiceChat", true);

            if (key != null)
            {
                object versionNum = key.GetValue("PROGRAM_VERSION");

                if (versionNum is int version)
                {
                    if (key.GetValue("PROGRAM_PATH") is string path && version >= ProgramVersion)
                    {
                        ProgramIsAlreadyInstalled = true;
                        Pages.Insert(1, new Page(this, GridReinstall, false, true));
                        ReinstallPath = path;
                        RegistryPath = path;

                        // Set up as updater.
                        if (App.UpdateMode)
                        {
                            Pages[PageIndex].UnderlyingGrid.IsVisible = false;
                            PageIndex = GetPageIndexByType<InstallingPage>();
                            InstallLocationBox.Text = Path.GetDirectoryName(path);
                            UpdateButtons();
                            Pages[PageIndex].OnSwitchedTo();
                            Pages[PageIndex].UnderlyingGrid.IsVisible = true;
                        }
                    }
                }
                
            }
#endif

#if LINUX
            FileInfo occlusionConfigFile = new FileInfo("etc/occlusion_settings.json");

            if (occlusionConfigFile.Exists)
            {
                JsonFile<OcclusionLinuxSettings> configFile = new JsonFile<OcclusionLinuxSettings>(occlusionConfigFile.FullName, new OcclusionLinuxSettings());

                if (configFile.Obj.VersionNumber >= ProgramVersion)
                {
                    ProgramIsAlreadyInstalled = true;
                    Pages.Insert(1, new Page(this, GridReinstall, false, true));
                    ReinstallPath = configFile.Obj.ProgramPath;
                    RegistryPath = configFile.Obj.ProgramPath;

                    // Set up as updater.
                    if (App.UpdateMode)
                    {
                        Pages[PageIndex].UnderlyingGrid.IsVisible = false;
                        PageIndex = GetPageIndexByType<InstallingPage>();
                        InstallLocationBox.Text = Path.GetDirectoryName(configFile.Obj.ProgramPath);
                        UpdateButtons();
                        Pages[PageIndex].OnSwitchedTo();
                        Pages[PageIndex].UnderlyingGrid.IsVisible = true;
                    }
                }
            }
#endif
            UpdateButtons();
        }

        private void UninstallButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            GoToPage(GetPageIndexByType<UninstallPage>());
        }

        public int GetPageIndexByType<T>()
        {
            for(int i = 0; i < Pages.Count; i++)
            {
                if (Pages[i] is T)
                    return i;
            }

            return -1;
        }

        private async void MoveInstallButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            OpenFolderDialog folderDialog = new OpenFolderDialog();
            var result = await folderDialog.ShowAsync(this);

            // This try catch ensure that the directory chosen by the folder dialog is valid.
            try
            {
                DirectoryInfo info = new DirectoryInfo(result);

                if (result != string.Empty)
                {
                    MoveFolderPath = result;
                    GoToPage(GetPageIndexByType<MoveInstallPage>());
                }
                    
            }
            catch(IOException ex)
            {
                AbortInstaller("Installer failed", "Invalid path given. Check the log file located in the directory of the installer for more details.");
                string logFile = $"{ex.Message}\n\nSTACK TRACE:\n{ex.StackTrace}";
                System.IO.File.WriteAllText($"installercrashlog-{string.Format("{0:yyyy-MM-dd_HH-mm-ss-fff}", DateTime.Now)}.txt", logFile);
            }
            catch(ArgumentException ex)
            {
                AbortInstaller("Installer failed", "Invalid path given. Check the log file located in the directory of the installer for more details.");
                string logFile = $"{ex.Message}\n\nSTACK TRACE:\n{ex.StackTrace}";
                System.IO.File.WriteAllText($"installercrashlog-{string.Format("{0:yyyy-MM-dd_HH-mm-ss-fff}", DateTime.Now)}.txt", logFile);
            }
        }

        private void ReinstallButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Reinstalling = true;

            InstallLocationBox.Text = ReinstallPath;
            InstallLocationBox.IsEnabled = false;
            BrowseButton.IsEnabled = false;

            GoToPage(GetPageIndexByType<PreReqPage>());
            (DataContext as MainWindowViewModel).NextText = "Reinstall";
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            //if (!CanClose)
            //    e.Cancel = true;
        }

        private async void BrowseButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            OpenFolderDialog fileDialog = new OpenFolderDialog();
            var path = await fileDialog.ShowAsync(this);

            if (path != string.Empty)
                InstallLocationBox.Text = path;
        }

        private void BackButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Page currentPage = Pages[PageIndex];
            currentPage.BackPage();
        }

        private void NextButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Page currentPage = Pages[PageIndex];
            currentPage.NextPage();
        }

        public void UpdateButtons()
        {
            (DataContext as MainWindowViewModel).NextText = Pages[PageIndex].NextText;
            (DataContext as MainWindowViewModel).BackText = Pages[PageIndex].BackText;

            BackButton.IsEnabled = Pages[PageIndex].CanGoBack;
            NextButton.IsEnabled = Pages[PageIndex].CanGoForward;
        }

        public void DisableButtons()
        {
            BackButton.IsEnabled = false;
            NextButton.IsEnabled = false;
        }

        public void AbortInstaller(string messageTitle, string message)
        {
            Page currentPage = Pages[PageIndex];

            RunProgramAfterClosing.IsVisible = false;
            PageIndex = GetPageIndexByType<FinalPage>();

            if (CanSwitchPages)
            {
                CanSwitchPages = false;

                var task = PageSlider.Start(currentPage.UnderlyingGrid, Pages[PageIndex].UnderlyingGrid, true);
                task.ContinueWith(t =>
                {
                    CanSwitchPages = true;
                });
            }
            else
            {
                currentPage.UnderlyingGrid.IsVisible = false;
                Pages[PageIndex].UnderlyingGrid.IsVisible = true;
            }

            UpdateButtons();

            

            (DataContext as MainWindowViewModel).FinalScreenTitle = messageTitle;
            (DataContext as MainWindowViewModel).FinalScreenMessage = message;
        }

        public void CleanupInstaller(string tempZipLocation)
        {
            if (System.IO.File.Exists(tempZipLocation))
            {
                System.IO.File.Delete(tempZipLocation);
            }
        }

        public void GoToPage(int number, bool forward = true)
        {
            CanSwitchPages = false;
            var task = PageSlider.Start(Pages[PageIndex].UnderlyingGrid, Pages[number].UnderlyingGrid, forward);

            PageIndex = number;

            task.ContinueWith(t =>
            {
                CanSwitchPages = true;
            });

            UpdateButtons();

            Pages[PageIndex].OnSwitchedTo();
        }
    }

    public class Page
    {
        public Grid UnderlyingGrid { get; set; }

        public virtual bool CanGoBack { get; set; } = true;

        public virtual bool CanGoForward { get; set; } = true;

        public string NextText { get; set; } = "Next";

        public string BackText { get; set; } = "Back";

        internal MainWindow window;

        public Page(MainWindow window, Grid grid, bool canGoForward, bool canGoBack, string nextText = "Next", string backText = "Back")
        {
            UnderlyingGrid = grid;
            CanGoBack = canGoBack;
            CanGoForward = canGoForward;
            BackText = backText;
            NextText = nextText;
            this.window = window;
        }

        public virtual void NextPage()
        {
            if (window.CanSwitchPages)
            {
                if (window.PageIndex + 1 < window.Pages.Count)
                {
                    window.PageIndex++;

                    Page newPage = window.Pages[window.PageIndex];

                    window.CanSwitchPages = false;
                    var task = window.PageSlider.Start(UnderlyingGrid, newPage.UnderlyingGrid, true);

                    task.ContinueWith(t =>
                    {
                        window.CanSwitchPages = true;
                    });
                }

                window.UpdateButtons();

                window.Pages[window.PageIndex].OnSwitchedTo();
            }
        }

        public virtual void BackPage()
        {
            if (window.CanSwitchPages)
            {

                if (window.PageIndex - 1 >= 0)
                {
                    window.PageIndex--;

                    Page newPage = window.Pages[window.PageIndex];

                    window.CanSwitchPages = false;
                    var task = window.PageSlider.Start(UnderlyingGrid, newPage.UnderlyingGrid, false);

                    task.ContinueWith(t =>
                    {
                        window.CanSwitchPages = true;
                    });

                    
                }

                window.UpdateButtons();

                window.Pages[window.PageIndex].OnSwitchedTo();
            }
        }

        public virtual void OnSwitchedTo()
        {

        }
    }

    public class ConfigPage : Page
    {
        public ConfigPage(MainWindow window, Grid grid, bool canGoForward, bool canGoBack, string nextText = "Next", string backText = "Back")
            : base(window, grid, canGoForward, canGoBack, nextText, backText)
        {

        }


        public override void BackPage()
        {
            base.BackPage();
        }

        public override void NextPage()
        {
            base.NextPage();
        }

        
    }

    public class FinalPage : Page
    {
        public FinalPage(MainWindow window, Grid grid, bool canGoForward, bool canGoBack, string nextText = "Next", string backText = "Back")
            : base(window, grid, canGoForward, canGoBack, nextText, backText)
        {

        }

        public override void NextPage()
        {
            if (MainWindow.EXEPath != null && window.RunProgramAfterClosing.IsChecked.Value && window.RunProgramAfterClosing.IsVisible)
            {
                Process.Start(MainWindow.EXEPath);

                string tempPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\occlusion_tempfiles.tmp.zip";

                window.CleanupInstaller(tempPath);
            }

            window.Close();
        }
    }

    public class InstallingPage : Page
    {
        public InstallingPage(MainWindow window, Grid grid, bool canGoForward, bool canGoBack, string nextText = "Next", string backText = "Back")
            : base(window, grid, canGoForward, canGoBack, nextText, backText)
        {

        }

        public override void OnSwitchedTo()
        {
            // Ensure path is valid
            try
            {
                string path = window.InstallLocationBox.Text;

                if (window.Reinstalling)
                {
                    FileInfo existingInstall = new FileInfo(window.ReinstallPath);
                    path = existingInstall.Directory.FullName;
                }

                var dirInfo = new DirectoryInfo(path);

                if(!dirInfo.Exists)
                    dirInfo.Create();

                // If the path is valid & created, we will have gotten here in the code.
                // Extract zip file with contents to temp location
                var resourcePath = Assembly.GetExecutingAssembly().GetManifestResourceNames()
                .Single(str => str.EndsWith("programfiles.zip"));

                string tempPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\occlusion_tempfiles.tmp.zip";

                // Write the resource zip file to the temp directory
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourcePath))
                {
                    using (FileStream bw = new FileStream(tempPath, FileMode.Create))
                    {
                        // Read until we reach the end of the file
                        while (stream.Position < stream.Length)
                        {
                            // Byte array to hold file bytes
                            byte[] bits = new byte[stream.Length];
                            // Read in the bytes
                            stream.Read(bits, 0, (int)stream.Length);
                            // Write out the bytes
                            bw.Write(bits, 0, (int)stream.Length);
                        }
                    }
                }


                // Now, extract the contents of the zip file to the install directory
                Task.Run(() =>
                {
                    using (ZipFile zip = ZipFile.Read(tempPath))
                    {
                        zip.ZipError += (sender, e) =>
                        {
                            if (e.Exception != null)
                                throw e.Exception;
                        };

                        int entryNum = 0;
                        window.CanClose = false;
                        foreach (ZipEntry entry in zip.Entries)
                        {
                            entry.Extract(path, ExtractExistingFileAction.OverwriteSilently);
                            entryNum++;

                            Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                (window.DataContext as MainWindowViewModel).InstallStatus = $"Installing {(int)(((float)entryNum / (float)zip.Entries.Count) * 100)}%";

                                if (App.UpdateMode)
                                {
                                    (window.DataContext as MainWindowViewModel).BeingInstalled = "Please wait while Occlusion is updated...";
                                    (window.DataContext as MainWindowViewModel).InstallStatus = $"Updating {(int)(((float)entryNum / (float)zip.Entries.Count) * 100)}%";
                                    (window.DataContext as MainWindowViewModel).FinalScreenMessage = "Occlusion has been successfully updated to the latest version.";
                                }
                                    

                                window.InstallProgressBar.Value = (float)entryNum / (float)zip.Entries.Count;

                                if (window.InstallProgressBar.Value >= 1)
                                {

                                    (window.DataContext as MainWindowViewModel).InstallStatus = "Finished! Please click next to finish the setup.";
                                    window.NextButton.IsEnabled = true;
                                    window.BackButton.IsEnabled = false;

                                    try
                                    {

                                        string pathToExe = $"{path}\\{MainWindowViewModel.ReadResource("exename.txt")}";
                                        MainWindow.EXEPath = pathToExe;
                                        // Create a start menu shortcut if the user enabled it
                                        if (window.MakeStartMenuShortcut.IsChecked.Value)
                                        {
                                            var exeInfo = new FileInfo(pathToExe);
                                            string commonStartMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu);
                                            string appStartMenuPath = Path.Combine(commonStartMenuPath, "Programs", "Occlusion Voice Chat");

                                            if (!Directory.Exists(appStartMenuPath))
                                                Directory.CreateDirectory(appStartMenuPath);

                                            string shortcutLocation = Path.Combine(appStartMenuPath, "Occlusion Voice Chat" + ".lnk");
                                            WshShell shell = new WshShell();
                                            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);

                                            shortcut.Description = "A directional VoIP client for Minecraft";
                                            shortcut.TargetPath = pathToExe;
                                            shortcut.Save();
                                        }

                                        // Create a desktop shortcut if the user enabled it
                                        if (window.MakeDesktopShortcut.IsChecked.Value)
                                        {

                                            var exeInfo = new FileInfo(pathToExe);
                                            string appDesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);


                                            if (!Directory.Exists(appDesktopPath))
                                                Directory.CreateDirectory(appDesktopPath);

                                            string shortcutLocation = Path.Combine(appDesktopPath, "Occlusion Voice Chat" + ".lnk");
                                            WshShell shell = new WshShell();
                                            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);
                                            
                                            shortcut.Description = "A directional VoIP client for minecraft";
                                            shortcut.TargetPath = pathToExe;
                                            shortcut.Save();

                                        }

                                        // Add program path to registry for the installer to know if the program is already installed.
                                        // Ensure the key doesn't exist. the installer should have already checked for this, and if it did it should have gone into re-install/uninstall mode.
                                        // We still need to make sure the key didn't get created somehow between then and now.
                                        if (System.IO.File.Exists(pathToExe))
                                        {
                                            var key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\OcclusionVoiceChat", true);

                                            if (key == null)
                                            {
                                                key = Registry.LocalMachine.CreateSubKey("SOFTWARE\\OcclusionVoiceChat", true);
                                            }

                                            key.SetValue("PROGRAM_PATH", pathToExe); 
                                            key.SetValue("PROGRAM_VERSION", MainWindow.ProgramVersion);
                                        }

                                        window.CanClose = true;
                                    }
                                    catch (Exception e)
                                    {
                                        if (e is PathTooLongException || e is IOException)
                                        {
                                            window.AbortInstaller("Installer Failed", "An error occured while attempting to install the program. A log file will be saved in the directory of the installer.");
                                            string logFile = $"{e.Message}\n\nSTACK TRACE:\n{e.StackTrace}";
                                            System.IO.File.WriteAllText($"installercrashlog-{string.Format("{0:yyyy-MM-dd_HH-mm-ss-fff}", DateTime.Now)}.txt", logFile);

                                        }
                                        else
                                        {
                                            throw;
                                        }
                                    }
                                }
                            });
                        }
                    }

                    
                });



                //base.NextPage();

                window.DisableButtons();
            }
            catch (Exception e)
            {
                if (e is PathTooLongException || e is IOException)
                {
                    window.InvalidPathText.IsVisible = true;
                }
                else
                {
                    throw;
                }
            }

            base.OnSwitchedTo();
        }
    }


    public class PreReqPage : Page
    {
        public PreReqPage(MainWindow window, Grid grid, bool canGoForward, bool canGoBack, string nextText = "Next", string backText = "Back")
            : base(window, grid, canGoForward, canGoBack, nextText, backText)
        {

        }

        public override void OnSwitchedTo()
        {
            bool net5IsInstalled = CheckForRequiredRuntime();

            if (net5IsInstalled)
            {
                (window.DataContext as MainWindowViewModel).PreReqInstallText = "Required prerequisites are already installed. Please click next to continue the installation.";
                window.NextButton.IsEnabled = true;
                window.BackButton.IsEnabled = false;
                window.PreReqBar.Value = 1.2;
            }
            else
            {
                (window.DataContext as MainWindowViewModel).PreReqInstallText = "Downloading .Net 6.0.4 from Microsoft";

                // Download .Net 5
                using (WebClient wc = new WebClient())
                {
                    var installerPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\occlusion_win_download.tmp.exe";

                    wc.DownloadFileAsync(
                        new Uri("https://download.visualstudio.microsoft.com/download/pr/2e97f1f0-f321-4baf-8d02-0be5f08afc4e/2a011c8f9b2792e17d363a21c0ed8fdc/dotnet-runtime-6.0.4-win-x64.exe"),
                        installerPath);
                    
                    // Progress bar
                    wc.DownloadProgressChanged += (sender, e) =>
                    {
                        Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            window.PreReqBar.Value = ((float)e.BytesReceived / e.TotalBytesToReceive);
                            (window.DataContext as MainWindowViewModel).PreReqInstallText = $"Downloading .Net 6.0.4 from Microsoft ({(int)(((float)e.BytesReceived / e.TotalBytesToReceive) * 100)}% | {e.BytesReceived} / {e.TotalBytesToReceive} bytes)";
                        });
                    };

                    wc.DownloadFileCompleted += (sender, e) =>
                    {
                        (window.DataContext as MainWindowViewModel).PreReqInstallText = "Waiting for user to install required runtime from external source";

                        // When the file is finished downloading, run it.
                        // We first run the installer process and block this thread until it's closed.
                        // We then perform the check to see if .net is installed again, and if it isn't we abort the installer.
                        Process installer = new Process();
                        installer.StartInfo.FileName = installerPath;

                        installer.Start();
                        installer.WaitForExit();
                        window.PreReqBar.Value = 1.2;

                        bool isNowInstalled = CheckForRequiredRuntime();
                        if (!isNowInstalled)
                        {
                            window.AbortInstaller("Installation Failed", "User failed to install downloaded required runtime.");
                        }
                        else
                        {
                            (window.DataContext as MainWindowViewModel).PreReqInstallText = "Required runtimes have been successfully installed. Please click next to continue.";
                            window.NextButton.IsEnabled = true;
                        }
                    };
                }
            }
            

            base.OnSwitchedTo();
        }

        public override void NextPage()
        {
            window.GoToPage(window.GetPageIndexByType<InstallingPage>());
        }

        public bool CheckForRequiredRuntime()
        {
            DirectoryInfo dir = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "/dotnet/host/fxr");

            if (dir.Exists)
            {
                Regex regex = new Regex("5\\.[0-9]+\\.[0-9].*?", RegexOptions.IgnoreCase);
                foreach (DirectoryInfo version in dir.GetDirectories())
                {
                    bool match = regex.IsMatch(version.Name);
                    if (match)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    public class MoveInstallPage : Page
    {
        public MoveInstallPage(MainWindow window, Grid grid, bool canGoForward, bool canGoBack, string nextText = "Next", string backText = "Back")
            : base(window, grid, canGoForward, canGoBack, nextText, backText)
        {

        }

        public override void OnSwitchedTo()
        {
            Task.Run(() =>
            {
                var path = Path.GetDirectoryName(window.RegistryPath);

                if (Directory.Exists(window.MoveFolderPath) && !Directory.Exists(window.MoveFolderPath + "\\Occlusion Voice Chat") && Path.GetDirectoryName(window.MoveFolderPath + "\\Occlusion Voice Chat") != Path.GetDirectoryName(window.RegistryPath))
                {

                    Directory.Move(path, window.MoveFolderPath + "\\Occlusion Voice Chat");

                    Dispatcher.UIThread.InvokeAsync(() => window.MoveFolderBar.Value = 0.8);
                    
                    // Update the path in the registry
                    var key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\OcclusionVoiceChat", true);

                    if (key != null)
                    {
                        key.SetValue("PROGRAM_PATH", $"{window.MoveFolderPath + "\\Occlusion Voice Chat"}\\{MainWindowViewModel.ReadResource("exename.txt")}");
                    }
                    Dispatcher.UIThread.InvokeAsync(() => window.MoveFolderBar.Value = 1);

                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        window.GoToPage(window.GetPageIndexByType<FinalPage>());
                        window.RunProgramAfterClosing.IsVisible = false;
                    });

                }
                else
                {
                    Debug.WriteLine(window.MoveFolderPath);
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        window.AbortInstaller("Installer failed", "Invalid new location given for program to move to.");
                    });

                }
            });


            base.OnSwitchedTo();
        }
    }

    public class UninstallPage : Page
    {
        public UninstallPage(MainWindow window, Grid grid, bool canGoForward, bool canGoBack, string nextText = "Next", string backText = "Back")
            : base(window, grid, canGoForward, canGoBack, nextText, backText)
        {

        }

        public override void NextPage()
        {
            window.GoToPage(window.GetPageIndexByType<UninstallProgressPage>());
        }
    }

    public class UninstallProgressPage : Page
    {
        public UninstallProgressPage(MainWindow window, Grid grid, bool canGoForward, bool canGoBack, string nextText = "Next", string backText = "Back")
            : base(window, grid, canGoForward, canGoBack, nextText, backText)
        {

        }

        public override void OnSwitchedTo()
        {
            Task.Run(() =>
            {
                try
                {
                    if (Directory.Exists(Path.GetDirectoryName(window.RegistryPath)))
                    {
                        DirectoryInfo dirInfo = new DirectoryInfo(Path.GetDirectoryName(window.RegistryPath));
                        var files = dirInfo.GetFiles();
                        var dirs = dirInfo.GetDirectories();

                        int totalEntries = files.Length + dirs.Length;
                        int currentEntry = 0;

                        foreach (FileInfo file in files)
                        {
                            file.Delete();
                            currentEntry++;
                            updateProgress();
                        }


                        foreach (DirectoryInfo dir in dirs)
                        {
                            dir.Delete(true);
                            currentEntry++;
                            updateProgress();
                        }

                        // We're finished removing the directory, now clean up the registry
                        var key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\OcclusionVoiceChat", true);

                        if (key != null)
                        {
                            Registry.LocalMachine.DeleteSubKey("SOFTWARE\\OcclusionVoiceChat");
                        }

                        // Delete start menu shortcut if enabled
                        if (window.RemoveStartShortcut.IsChecked.Value)
                        {
                            string commonStartMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu);
                            string appStartMenuPath = Path.Combine(commonStartMenuPath, "Programs", "Occlusion Voice Chat");

                            if (Directory.Exists(appStartMenuPath))
                            {
                                Directory.Delete(appStartMenuPath, true);
                            }
                        }

                        // Same with the desktop shortcut
                        if (window.RemoveDesktopShortcut.IsChecked.Value)
                        {
                            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                            if (System.IO.File.Exists(desktopPath + "\\Occlusion Voice Chat.lnk"))
                            {
                                System.IO.File.Delete(desktopPath + "\\Occlusion Voice Chat.lnk");
                            }
                        }

                        Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            (window.DataContext as MainWindowViewModel).UninstallStatus = "Program successfully uninstalled. Please click next to continue.";
                            window.GoToPage(window.GetPageIndexByType<FinalPage>());

                            window.RunProgramAfterClosing.IsVisible = false;
                            (window.DataContext as MainWindowViewModel).FinalScreenTitle = "Uninstall Finished";
                            (window.DataContext as MainWindowViewModel).FinalScreenMessage = "Occlusion has been fully uninstalled from your system. You may now close the installer.";
                        });

                        // Local functions
                        void updateProgress()
                        {
                            Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                double progress = (double)currentEntry / (double)totalEntries;
                                window.UninstallBar.Value = progress;
                                (window.DataContext as MainWindowViewModel).UninstallStatus = $"Uninstalling... {(int)(progress * 100d)}%";
                            });
                            
                        }
                    }
                }
                catch(IOException ex)
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    { 
                        window.AbortInstaller("Uninstall failed", "Could not delete one or more files. Please ensure the installer has access to the path occlusion is in, and it is running in administrator. Check the log file for more details.\n\n Error details:\n" + ex.Message);
                        string logFile = $"{ex.Message}\n\nSTACK TRACE:\n{ex.StackTrace}";
                        System.IO.File.WriteAllText($"installercrashlog-{string.Format("{0:yyyy-MM-dd_HH-mm-ss-fff}", DateTime.Now)}.txt", logFile);
                    });
                    
                }
            });

            base.OnSwitchedTo();
        }
    }
}
