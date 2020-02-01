﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using H.NET.Core.Managers;
using H.NET.Core.Recorders;
using HomeCenter.NET.Extensions;
using HomeCenter.NET.Services;
using HomeCenter.NET.ViewModels.Commands;
using HomeCenter.NET.ViewModels.Settings;
using HomeCenter.NET.ViewModels.Utilities;
// ReSharper disable UnusedMember.Global

namespace HomeCenter.NET.ViewModels
{
    public class MainViewModel : Screen
    {
        #region Properties

        public IWindowManager WindowManager { get; }
        public RunnerService RunnerService { get; }
        public HookService HookService { get; }
        public Properties.Settings Settings { get; }
        public PopupViewModel PopupViewModel { get; }
        public BaseManager Manager { get; }

        private string _text = string.Empty;
        public string Text {
            get => _text;
            set {
                _text = value;
                NotifyOfPropertyChange(nameof(Text));
            }
        }

        private string _input = string.Empty;
        public string Input {
            get => _input;
            set {
                _input = value;
                NotifyOfPropertyChange(nameof(Input));
            }
        }

        private bool _isVisible;
        public bool IsVisible {
            get => _isVisible;
            set {
                _isVisible = value;
                NotifyOfPropertyChange(nameof(IsVisible));
            }
        }

        private bool _isRecord;
        public bool IsRecord {
            get => _isRecord;
            set {
                _isRecord = value;
                NotifyOfPropertyChange(nameof(IsRecord));
            }
        }

        private bool UserCanClose { get; set; }

        #endregion

        #region Constructors

        public MainViewModel(IWindowManager windowManager, Properties.Settings settings, PopupViewModel popupViewModel, RunnerService runnerService, HookService hookService, BaseManager manager)
        {
            WindowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            PopupViewModel = popupViewModel ?? throw new ArgumentNullException(nameof(popupViewModel));
            RunnerService = runnerService ?? throw new ArgumentNullException(nameof(runnerService));
            HookService = hookService ?? throw new ArgumentNullException(nameof(hookService));
            Manager = manager ?? throw new ArgumentNullException(nameof(manager));

            RunnerService.NewOutput += (sender, text) => Print(text);
            Manager.Started += (sender, args) =>
            {
                IsRecord = true;
                RunnerService.HiddenRun("deskband start");
            };
            Manager.Stopped += (sender, args) =>
            {
                IsRecord = false;
                RunnerService.HiddenRun("deskband stop");
            };
        }

        #endregion

        #region Public methods

        public void ShowMessage(string text, bool isWarning)
        {
            Text += $"{DateTime.Now:T}: {text}{Environment.NewLine}";

            if (Settings.EnablePopUpMessages)
            {
                PopupViewModel.Show(text, 5000, isWarning);
            }
        }

        public void Print(string text)
        {
            ShowMessage(text, false);
        }

        public void Warning(string text)
        {
            RunnerService.Run($"say {text}");

            ShowMessage(text, true);
        }

        public async Task RecordAsync(CancellationToken cancellationToken = default)
        {
            await Manager.ChangeWithTimeoutAsync(TimeSpan.FromSeconds(5), cancellationToken);
        }

        public async Task ShowCommandsAsync()
        {
            await this.ShowWindowAsync<CommandsViewModel>();
        }

        public async Task ShowSettingsAsync()
        {
            await this.ShowWindowAsync<SettingsViewModel>();
        }

        public async Task BeforeExitAsync()
        {
            await PopupViewModel.TryCloseAsync();
        }

        public void PreviousCommand()
        {
            if (!RunnerService.History.Any())
            {
                return;
            }

            Input = RunnerService.History.LastOrDefault() ?? "";
            RunnerService.History.RemoveAt(RunnerService.History.Count - 1);
        }

        public void RunInput()
        {
            if (string.IsNullOrEmpty(Input))
            {
                return;
            }

            RunnerService.Run(Input);
            Input = string.Empty;
        }

        public void AddNewLine()
        {
            Input += Environment.NewLine;
            // TODO: Need to implement
            //InputTextBox.CaretIndex = InputTextBox.Text.Length - 1;
        }

        public void Restart()
        {
            RunnerService.Run("restart");
        }

        public async Task OnClosing(CancelEventArgs e)
        {
            if (UserCanClose)
            {
                await BeforeExitAsync();
                return;
            }

            IsVisible = false;
            e.Cancel = true;
        }

        public async Task Close()
        {
            UserCanClose = true;
            await TryCloseAsync();

            if (!IsVisible)
            {
                await BeforeExitAsync();
            }
        }

        #endregion
    }
}
