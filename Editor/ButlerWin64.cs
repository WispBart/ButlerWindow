using System;

namespace ButlerWindow
{
    public class ButlerWin64 : Butler
    {
        protected override string ButlerURI => "https://broth.itch.ovh/butler/windows-amd64/LATEST/archive/default";
        protected override string ButlerExecutable => "butler.exe";
        protected override string ButlerInstallPath => Environment.GetEnvironmentVariable("LocalAppData") + "/Butler/";
        protected override string ButlerZipPath => Environment.GetEnvironmentVariable("LocalAppData") + "/Butler/butler_download.zip";
    }
}