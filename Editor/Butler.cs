using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace ButlerWindow
{
    public abstract class Butler : ScriptableObject
    {
        public delegate void ButlerCommandResult(bool successful, string errorMessage);

        
        protected abstract string ButlerURI { get; }
        protected abstract string ButlerExecutable { get; }
        protected abstract string ButlerInstallPath { get; }
        protected abstract string ButlerZipPath { get; }

        private const string VersionArgs = "-V";
        private const string UpdateArgs = "upgrade --assume-yes";
        private const string LoginArgs = "login --assume-yes";
        private const string LogoutArgs = "logout --assume-yes";
        private const string BUTLER_NOT_INSTALLED = "Butler is not installed.";


        public bool IsInstalled { get; private set; }
        public bool IsBuilding { get; private set; }
        public bool IsUploading { get; private set; }
        public string Version { get; private set; }
        //public ConsoleWindow Console;

        public Action<string> SetConsoleMessage;
        public Action<string> AppendConsoleMessage;

        public string ButlerCommand(string args, bool showWindow, out bool error)
        {
            Process process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = ButlerInstallPath + ButlerExecutable,
                    Arguments = args,
                    WindowStyle = showWindow ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                },
            };
            try
            {
                process.Start();
                process.WaitForExit();
            }
            catch (Win32Exception e)
            {
                error = false;
                return e.Message;
            }

            var stdOut = process.StandardOutput.ReadToEnd();
            var stdError = process.StandardError.ReadToEnd();
            error = process.ExitCode != 0;
            return stdError + stdOut;
        }

        public IEnumerator ButlerCommandRoutine(string args, ButlerCommandResult onComplete = null)
        {
            Process process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = ButlerInstallPath + ButlerExecutable,
                    Arguments = args,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                },
            };
            try
            {
                process.Start();
            }
            catch (Win32Exception e)
            {
                SetConsoleMessage?.Invoke(e.Message);
            }

            Console.Clear();
            while (!process.HasExited)
            {
                var consoleStream = process.StandardOutput;
                var msg = consoleStream.ReadToEnd();
                if (msg.Contains("stdin is not a terminal")) // On Linux IsTerminal() returns false and login is not allowed.
                {
                    msg = "\nAuthenticate by typing 'butler login' in the terminal instead."; 
                }
                SetConsoleMessage?.Invoke(msg);
                yield return null;
            }

            if (process.ExitCode == 0)
            {
                onComplete?.Invoke(true, "");
            }
            else
            {
                string errorOut = process.StandardError.ReadToEnd();
                onComplete?.Invoke(false, errorOut);
            }
        }

        private void OnEnable()
        {
            CheckIsInstalled();
        }

        public void Login() => EditorCoroutineUtility.StartCoroutine(ButlerCommandRoutine(LoginArgs), this);
        public string Logout() => ButlerCommand(LogoutArgs, true, out bool error);

        public void CheckIsInstalled()
        {
            IsInstalled = Directory.Exists(ButlerInstallPath) && File.Exists(ButlerInstallPath + ButlerExecutable);
        }

        public string CheckVersion()
        {
            if (!IsInstalled) return "BUTLER_NOT_INSTALLED";
            var result = ButlerCommand(VersionArgs, false, out bool error);
            if (!error) IsInstalled = true;
            Version = result;
            return result;
        }

        public string CheckForUpdates()
        {
            var result = ButlerCommand(UpdateArgs, true, out bool error);
            if (error) Debug.LogError(result);
            return result;
        }

        public void UploadBuild(ButlerSettings settings)
        {
            IsUploading = true;
            EditorCoroutineUtility.StartCoroutine(ButlerCommandRoutine(settings.ToPushArgs(), (success, error) =>
            {
                IsUploading = false;
                Application.OpenURL(settings.GetURL());
                if (success) AppendConsoleMessage?.Invoke("Upload Complete.");
                else AppendConsoleMessage?.Invoke(error);
            }), this);
        }

        public void DownloadButler(Action<float> progress, Action onComplete)
        {
            EditorCoroutineUtility.StartCoroutine(DownloadAndInstallButlerRoutine(progress, onComplete), this);
        }

        protected virtual bool TrySetPermissions()
        {
            return true;
        }
        IEnumerator DownloadAndInstallButlerRoutine(Action<float> progress, Action onComplete)
        {
            using (var webRequest = new UnityWebRequest()
            {
                url = ButlerURI,
                method = "GET",
                downloadHandler = new DownloadHandlerBuffer(),
            }) 
            {
                webRequest.SendWebRequest();
                while (!webRequest.isDone)
                {
                    yield return new EditorWaitForSeconds(0.2f);
                    progress.Invoke(webRequest.downloadProgress);
                }

                if (webRequest.result == UnityWebRequest.Result.ConnectionError ||
                    webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Error occured: {webRequest.error}");
                    yield break;
                }

                Directory.CreateDirectory(ButlerInstallPath);
                File.WriteAllBytes(ButlerZipPath, webRequest.downloadHandler.data);
                ZipFile.ExtractToDirectory(ButlerZipPath, ButlerInstallPath);
            }

            if (!TrySetPermissions())
            {
                Debug.LogError("Failed to set permissions.");
                Directory.Delete(ButlerInstallPath, true);
                yield break;
            }
            
            onComplete();
            CheckIsInstalled();
        }
    }
}