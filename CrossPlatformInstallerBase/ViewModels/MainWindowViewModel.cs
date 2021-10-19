using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CrossPlatformInstallerBase.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        public string LicenseText { get; set; } = "License";

        private string _nextText;
        public string NextText 
        {
            get => _nextText;

            set => this.RaiseAndSetIfChanged(ref _nextText, value);
        }

        private string _backText;
        public string BackText
        {
            get => _backText;

            set => this.RaiseAndSetIfChanged(ref _backText, value);
        }
        

        private string _finalScreenTitle = "Finishing Up";
        public string FinalScreenTitle
        {
            get => _finalScreenTitle;

            set => this.RaiseAndSetIfChanged(ref _finalScreenTitle, value);
        }

        private string _finalScreenMessage = "Occlusion has been successfully installed.";
        public string FinalScreenMessage
        {
            get => _finalScreenMessage;

            set => this.RaiseAndSetIfChanged(ref _finalScreenMessage, value);
        }

        private string _preReqInstallText = "Checking if .Net 5 is installed on your system";
        public string PreReqInstallText
        {
            get => _preReqInstallText;

            set => this.RaiseAndSetIfChanged(ref _preReqInstallText, value);
        }

        
        public string InstallDirectory { get; set; } = "";

        private string _installStatus = "Installing...";
        public string InstallStatus 
        {
            get => _installStatus;

            set => this.RaiseAndSetIfChanged(ref _installStatus, value);
        }

        private string _beingInstalled = "Please wait while Occlusion is being installed...";

        public string BeingInstalled
        {
            get => _beingInstalled;

            set => this.RaiseAndSetIfChanged(ref _beingInstalled, value);
        }


        private string _uninstallStatus = "Uninstalling...";
        public string UninstallStatus
        {
            get => _uninstallStatus;

            set => this.RaiseAndSetIfChanged(ref _uninstallStatus, value);
        }

        

        private string _moveStatus = "Moving install folder";
        public string MoveStatus
        {
            get => _moveStatus;

            set => this.RaiseAndSetIfChanged(ref _moveStatus, value);
        }


        public MainWindowViewModel()
        {
            LicenseText = ReadResource("installerlicense.txt");
            InstallDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\Occlusion Voice Chat\\";
        }

        public static string ReadResource(string name)
        {
            // Determine path
            var assembly = Assembly.GetExecutingAssembly();
            string resourcePath = name;
            // Format: "{Namespace}.{Folder}.{filename}.{Extension}"

            resourcePath = assembly.GetManifestResourceNames()
                .Single(str => str.EndsWith(name));
            

            using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
