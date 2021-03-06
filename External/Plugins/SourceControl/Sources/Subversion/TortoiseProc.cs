﻿using System;
using System.Diagnostics;
using System.IO;
using SourceControl.Actions;

namespace SourceControl.Sources.Subversion
{
    internal static class TortoiseProc
    {
        static string resolvedCmd;
        static string qualifiedCmd;

        public static void Execute(string command, string path)
        {
            var args = $"/command:{command} /path:\"{path}\"";
            var info = new ProcessStartInfo(GetTortoiseProc(), args);
            info.UseShellExecute = true;

            var proc = new Process
            {
                StartInfo = info,
                EnableRaisingEvents = true
            };
            proc.Exited += (sender, eventArgs) => ProjectWatcher.ForceRefresh();
            proc.Start();
        }

        public static void Execute(string command, string path1, string path2)
        {
            var args = $"/command:{command} /path:\"{path1}\" /path2:\"{path2}\"";
            var info = new ProcessStartInfo(GetTortoiseProc(), args);
            info.UseShellExecute = true;

            var proc = new Process
            {
                StartInfo = info,
                EnableRaisingEvents = true
            };
            proc.Exited += (sender, eventArgs) => ProjectWatcher.ForceRefresh();
            proc.Start();
        }

        static string GetTortoiseProc()
        {
            var cmd = PluginMain.SCSettings.TortoiseSVNProcPath;
            if (File.Exists(cmd)) return cmd;
            if (string.IsNullOrEmpty(cmd)) cmd = "TortoiseProc.exe";
            return ResolveTortoiseProcPath(cmd);
        }

        static string ResolveTortoiseProcPath(string cmd)
        {
            if (resolvedCmd == cmd || Path.IsPathRooted(cmd))
                return qualifiedCmd;

            resolvedCmd = cmd;
            qualifiedCmd = cmd;
            var cp = Environment.GetEnvironmentVariable("PATH");
            foreach (var path in cp.Split(';'))
            {
                if (path.IndexOf("svn", StringComparison.OrdinalIgnoreCase) > 0 && Directory.Exists(path))
                {
                    var test = Path.Combine(path, cmd);
                    if (File.Exists(test)) { qualifiedCmd = test; break; }
                }
            }
            return qualifiedCmd;
        }
    }
}
