using System;
using System.Collections;
using System.ComponentModel;
using UnityEngine;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using ButlerWindow;
using Unity.EditorCoroutines.Editor;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;


public class ButlerWin64 : ScriptableObject
{
    public delegate void ButlerCommandResult(bool successful, string errorMessage);

    private const string ButlerForWindows64URI = "https://broth.itch.ovh/butler/windows-amd64/LATEST/archive/default";
    private string ButlerInstallPath => Environment.GetEnvironmentVariable("LocalAppData") + "/Butler/";
    private const string ButlerExe = "butler.exe";
    private const string VersionArgs = "-V"; 
    private const string UpdateArgs = "upgrade --assume-yes";
    private const string LoginArgs = "login --assume-yes";
    private const string LogoutArgs = "logout --assume-yes";
    private const string BUTLER_NOT_INSTALLED = "Butler is not installed.";
    private string ButlerZipPath => Environment.GetEnvironmentVariable("LocalAppData") + "/Butler/butler_download.zip";
    
    public bool IsInstalled { get; private set; }
    public bool IsBuilding { get; private set; }
    public bool IsUploading { get; private set; }
    public string Version { get; private set; }
    public ConsoleWindow Console;

    public string ButlerCommand(string args, bool showWindow, out bool error)
    {
        Process process = new Process()
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = ButlerInstallPath + ButlerExe,
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
                FileName = ButlerInstallPath + ButlerExe,
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
            if (Console != null) Console.SetContents(e.Message);
        }
        
        Console.Clear();
        while (!process.HasExited)
        {
            var consoleStream = process.StandardOutput;
            Console.AppendContents(consoleStream.ReadToEnd());
            yield return null;
        }

        if (process.ExitCode == 0)
        {
            onComplete?.Invoke(true, "");
        }
        else
        {
            string errorOut = process.StandardError.ReadToEnd();
            onComplete.Invoke(false, errorOut);
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
        IsInstalled = Directory.Exists(ButlerInstallPath) && File.Exists(ButlerInstallPath + ButlerExe);
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
            if (success) Console.AppendContents("Upload Complete.");
            else Console.AppendContents(error);
        }), this);
        
    }

    public void DownloadButler(Action<float> progress, Action onComplete)
    {
        EditorCoroutineUtility.StartCoroutine(DownloadAndInstallButlerRoutine(progress, onComplete), this);
    }

    IEnumerator DownloadAndInstallButlerRoutine(Action<float> progress, Action onComplete)
    {
        var webRequest = new UnityWebRequest()
        {
            url = ButlerForWindows64URI,
            method = "GET",
            downloadHandler = new DownloadHandlerBuffer(),
        };
        webRequest.SendWebRequest();
        while (!webRequest.isDone)
        {
            yield return new EditorWaitForSeconds(0.2f);
            progress.Invoke(webRequest.downloadProgress);
        }
        string status = $"Done? {webRequest.isDone}. Response code: {webRequest.isDone}";
        Directory.CreateDirectory(ButlerInstallPath);
        Debug.Log(status);
        File.WriteAllBytes(ButlerZipPath, webRequest.downloadHandler.data);
        ZipFile.ExtractToDirectory(ButlerZipPath, ButlerInstallPath, true);
        onComplete();
        CheckIsInstalled();
    }
}
