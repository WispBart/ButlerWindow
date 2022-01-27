using System;
using System.IO;
using ButlerWindow.InterOp;
using Debug = UnityEngine.Debug;

namespace ButlerWindow
{
    public class ButlerMacOS : Butler
    {
        protected override string ButlerURI => "https://broth.itch.ovh/butler/darwin-amd64/LATEST/archive/default";
        protected override string ButlerExecutable => "butler";
        protected override string ButlerInstallPath => Path.Combine(Environment.GetEnvironmentVariable("HOME"), "Library/Application Support/butler") + "/";
        protected override string ButlerZipPath => Path.Combine(Environment.GetEnvironmentVariable("TMPDIR"), "butler_download.zip");


        protected override bool TrySetPermissions()
        {
            try
            {
                return Chmod.MakeExecutable(ButlerInstallPath + ButlerExecutable);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }
    }
}