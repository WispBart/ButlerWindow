using System;
using System.IO;
using ButlerWindow.InterOp;
using Debug = UnityEngine.Debug;

namespace ButlerWindow
{
    public class ButlerLinux : Butler
    {
        protected override string ButlerURI => "https://broth.itch.zone/butler/linux-amd64/LATEST/archive/default";
        protected override string ButlerExecutable => "butler";
        protected override string ButlerInstallPath => Path.Combine(Environment.GetEnvironmentVariable("HOME"), "bin") + "/";
        protected override string ButlerZipPath => Path.Combine("/tmp", "butler_download.zip");


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
