using Microsoft.Win32;
using ReTime_Testing.Models;
using System;
using System.IO;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// 自启动服务实现
    /// </summary>
    public class AutoStartService : IAutoStartService
    {
        private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "ReTime-Testing";

        public bool IsEnabled { get; private set; }
        public string Method { get; private set; } = "registry";

        public void InitializeFromConfig(AutoStartConfig config)
        {
            if (config == null) return;

            Method = config.Method;
            IsEnabled = config.Enabled;

            if (IsEnabled)
                Enable(Method);
            else
                Disable();
        }

        public void Enable(string method)
        {
            Method = method.ToLower();
            IsEnabled = true;

            try
            {
                var exePath = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exePath)) return;

                switch (Method)
                {
                    case "registry":
                        EnableViaRegistry(exePath);
                        RemoveStartupFolderShortcut();
                        break;
                    case "startupfolder":
                        CreateStartupFolderShortcut(exePath);
                        RemoveRegistryValue();
                        break;
                }

                Logger.Info(nameof(AutoStartService), $"自启动已启用，方式: {Method}");
            }
            catch (Exception ex)
            {
                Logger.Error(nameof(AutoStartService), $"启用自启动失败: {ex.Message}", ex);
                IsEnabled = false;
            }
        }

        public void Disable()
        {
            IsEnabled = false;

            try
            {
                RemoveRegistryValue();
                RemoveStartupFolderShortcut();

                Logger.Info(nameof(AutoStartService), "自启动已禁用");
            }
            catch (Exception ex)
            {
                Logger.Error(nameof(AutoStartService), $"禁用自启动失败: {ex.Message}", ex);
            }
        }

        private void EnableViaRegistry(string exePath)
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            key?.SetValue(AppName, $"\"{exePath}\"");
        }

        private void RemoveRegistryValue()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            key?.DeleteValue(AppName, false);
        }

        private void CreateStartupFolderShortcut(string exePath)
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return;

            dynamic? shell = Activator.CreateInstance(shellType);
            if (shell == null) return;

            try
            {
                var startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                var shortcutPath = Path.Combine(startupFolder, $"{AppName}.lnk");
                var shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = exePath;
                shortcut.WorkingDirectory = Path.GetDirectoryName(exePath);
                shortcut.Save();
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(shell);
            }
        }

        private void RemoveStartupFolderShortcut()
        {
            var startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            var shortcutPath = Path.Combine(startupFolder, $"{AppName}.lnk");
            if (File.Exists(shortcutPath))
                File.Delete(shortcutPath);
        }
    }
}
