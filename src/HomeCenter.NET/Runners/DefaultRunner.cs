﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using H.Core.Runners;
using H.Core.Utilities;
using HomeCenter.NET.Properties;

namespace HomeCenter.NET.Runners
{
    public class DefaultRunner : Runner
    {
        #region Properties

        private string? UserName { get; set; }
        
        #endregion

        #region Constructors

        public DefaultRunner(Action<string> printAction, Action<string> warningAction, Func<string, Task> sayFunc, Func<string, Task<List<string>>> searchFunc)
        {
            AddAsyncAction("say", sayFunc, "text");
            AddInternalAction("print", printAction, "text");
            AddInternalAction("warning", warningAction, "text");
            AddInternalAction("notify", Notify);
            AddInternalAction("run", Run, "other_command_key");
            AddAction("search", async key => printAction(string.Join(Environment.NewLine, await searchFunc(key))), "key");

            AddAsyncAction("sleep", SleepCommand, "integer");
            AddAction("sync-sleep", command => Thread.Sleep(int.TryParse(command, out var result) ? result : 1000), "integer");

            AddAction("start", StartCommand, "program.exe arguments");
            AddAsyncAction("start-async", StartCommandAsync, "program.exe arguments");

            AddAction("say-my-name", async command =>
            {
                if (string.IsNullOrWhiteSpace(UserName))
                {
                    await SayAsync("Я еще не знаю вашего имени. Пожалуйста, представьтесь");

                    var name = await WaitNextCommand(8000);
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        return;
                    }

                    // First char to upper case
                    name = name[0].ToString().ToUpper() + name.Substring(1);

                    Settings.Set("username", name);
                    SaveSettings();
                }

                Say($"Привет {UserName}");
            });
            AddSetting("username", o => UserName = o, NoEmpty, string.Empty);

            AddVariable("$username$", () => UserName);
        }

        #endregion

        #region Private methods

        private static async Task SleepCommand(string command)
        {
            var delay = int.TryParse(command, out var result) ? result : 1000;
            await Task.Delay(delay);
        }

        private static Process? StartCommandInternal(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return null;
            }

            var values = command.SplitOnlyFirstIgnoreQuote(' ');
            var path = values[0].Trim('\"', '\\').Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("\\", "/");

            return Process.Start(new ProcessStartInfo(path, values[1])
            {
                UseShellExecute = true,
            });
        }

        private static void StartCommand(string command) => StartCommandInternal(command);

        private static async Task StartCommandAsync(string command)
        {
            var process = StartCommandInternal(command);
            try
            {
                await Task.Delay(1000000);
                await Task.Run(() => process?.WaitForExit());
            }
            finally
            {
                process?.Close();
            }
        }

        private static void Notify(string command)
        {
            using var player = new System.Media.SoundPlayer(Resources.beep);

            player.Play();
        }

        #endregion
    }
}