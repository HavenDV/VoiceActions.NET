﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using H.Core;
using H.Core.Extensions;
using H.Core.Notifiers;
using H.Core.Recognizers;
using H.Core.Recorders;
using H.Core.Runners;
using H.Core.Searchers;
using H.Core.Synthesizers;
using H.Utilities;
using HomeCenter.NET.Extensions;
using HomeCenter.NET.Services;
using HomeCenter.NET.Utilities;
using HomeCenter.NET.ViewModels.Modules;
using HomeCenter.NET.ViewModels.Utilities;

// ReSharper disable UnusedMember.Global

namespace HomeCenter.NET.ViewModels.Settings
{
    public class SettingsViewModel : SaveCancelViewModel
    {
        #region Properties

        public Properties.Settings Settings { get; }
        //public ModuleService ModuleService { get; }

        public bool IsStartup { get; set; }

        public BindableCollection<ItemViewModel>? IgnoredApplications { get; }
        public BindableCollection<ItemViewModel>? Assemblies { get; set; }
        public BindableCollection<ItemViewModel>? AvailableTypes { get; set; }

        public BindableCollection<InstanceViewModel>? Modules { get; set; }
        public BindableCollection<InstanceViewModel>? Recorders { get; set; }
        public BindableCollection<InstanceViewModel>? Converters { get; set; }
        public BindableCollection<InstanceViewModel>? Synthesizers { get; set; }
        public BindableCollection<InstanceViewModel>? Searchers { get; set; }
        public BindableCollection<InstanceViewModel>? Runners { get; set; }
        public BindableCollection<InstanceViewModel>? Notifiers { get; set; }

        public BindableCollection<string>? RecorderElements { get; set; }
        public BindableCollection<string>? ConverterElements { get; set; }
        public BindableCollection<string>? SynthesizerElements { get; set; }
        public BindableCollection<string>? SearcherElements { get; set; }

        private string? _selectedRecorderElement;
        public string? SelectedRecorderElement {
            get => _selectedRecorderElement;
            set {
                _selectedRecorderElement = value;
                NotifyOfPropertyChange(nameof(SelectedRecorderElement));
            }
        }

        private string? _selectedConverterElement;
        public string? SelectedConverterElement {
            get => _selectedConverterElement;
            set {
                _selectedConverterElement = value;
                NotifyOfPropertyChange(nameof(SelectedConverterElement));
            }
        }

        private string? _selectedSynthesizerElement;
        public string? SelectedSynthesizerElement {
            get => _selectedSynthesizerElement;
            set {
                _selectedSynthesizerElement = value;
                NotifyOfPropertyChange(nameof(SelectedSynthesizerElement));
            }
        }

        private string? _selectedSearcherElement;
        public string? SelectedSearcherElement {
            get => _selectedSearcherElement;
            set {
                _selectedSearcherElement = value;
                NotifyOfPropertyChange(nameof(SelectedSearcherElement));
            }
        }

        #endregion

        #region Constructors

        public SettingsViewModel(Properties.Settings settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            //ModuleService = moduleService ?? throw new ArgumentNullException(nameof(moduleService));

            IgnoredApplications = new BindableCollection<ItemViewModel>();
                //HookService.HookIgnoredApps.Select(i => new IgnoredApplicationViewModel(i)));

            // TODO: To Container?
            UpdateAssemblies(false);
            UpdateAvailableTypes(false);
            UpdateModules(false);

            IsStartup = Startup.IsStartup(Options.FilePath);
            SaveAction = () =>
            {
                //ModuleService.Save();

                //HookService.KeyboardHook.SetEnabled(Settings.EnableKeyboardHook);
                //HookService.MouseHook.SetEnabled(Settings.EnableMouseHook);

                Settings.Recorder = SelectedRecorderElement;
                Settings.Converter = SelectedConverterElement;
                Settings.Synthesizer = SelectedSynthesizerElement;
                Settings.Searcher = SelectedSearcherElement;
                Settings.Save();
                Startup.Set(Options.FilePath, IsStartup);

                //ModuleService.UpdateActiveModules();
            };
        }

        #endregion

        #region Public methods

        public void UpdateAssemblies(bool notify = true)
        {
            Assemblies = new BindableCollection<ItemViewModel>();
                //ModuleService.ActiveAssemblies.Select(i => new AssemblyViewModel(i.Key, ModuleService.UpdatingIsNeed(i.Key))));

            if (notify)
            {
                NotifyOfPropertyChange(nameof(Assemblies));
            }
        }

        public void UpdateAvailableTypes(bool notify = true)
        {
            AvailableTypes = new BindableCollection<ItemViewModel>();
                //ModuleService.AvailableTypes.Select(i => new AvailableTypeViewModel(i)));

            if (notify)
            {
                NotifyOfPropertyChange(nameof(AvailableTypes));
            }
        }

        private BindableCollection<InstanceViewModel> CreateModuleCollection<T>() where T : class, IModule
        {
            return new BindableCollection<InstanceViewModel>();
            //ModuleService.GetBasePlugins<T>().Select(i => new InstanceViewModel(i.Key, i.Value)));
        }

        private void SetComboBox<T>(string value, Action<BindableCollection<string>> setElementsAction, Action<string> setSelectedAction)
            where T : class, IModule
        {
            //var plugins = ModuleService.GetEnabledPlugins<T>().Select(i => i.Key).ToList();
            //if (!string.IsNullOrWhiteSpace(value) && !plugins.Contains(value))
            {
                //plugins.Add(value);

            }

            //setElementsAction?.Invoke(new BindableCollection<string>(plugins));
            //setSelectedAction?.Invoke(value);
        }

        public void UpdateModules(bool notify = true)
        {
            Modules = new BindableCollection<InstanceViewModel>();
                //ModuleService.Instances.Objects.Select(i => new InstanceViewModel(i.Key, i.Value)));
            Recorders = CreateModuleCollection<IRecorder>();
            Converters = CreateModuleCollection<IRecognizer>();
            Synthesizers = CreateModuleCollection<ISynthesizer>();
            Searchers = CreateModuleCollection<ISearcher>();
            Runners = CreateModuleCollection<IRunner>();
            Notifiers = CreateModuleCollection<INotifier>();

            SetComboBox<IRecorder>(Settings.Recorder, e => RecorderElements = e, s => SelectedRecorderElement = s);
            SetComboBox<IRecognizer>(Settings.Converter, e => ConverterElements = e, s => SelectedConverterElement = s);
            SetComboBox<ISynthesizer>(Settings.Synthesizer, e => SynthesizerElements = e, s => SelectedSynthesizerElement = s);
            SetComboBox<ISearcher>(Settings.Searcher, e => SearcherElements = e, s => SelectedSearcherElement = s);

            if (notify)
            {
                NotifyOfPropertyChange(nameof(Modules));
                NotifyOfPropertyChange(nameof(Recorders));
                NotifyOfPropertyChange(nameof(Converters));
                NotifyOfPropertyChange(nameof(Synthesizers));
                NotifyOfPropertyChange(nameof(Searchers));
                NotifyOfPropertyChange(nameof(Runners));
                NotifyOfPropertyChange(nameof(Notifiers));

                NotifyOfPropertyChange(nameof(RecorderElements));
                NotifyOfPropertyChange(nameof(SelectedRecorderElement));
                NotifyOfPropertyChange(nameof(ConverterElements));
                NotifyOfPropertyChange(nameof(SelectedConverterElement));
                NotifyOfPropertyChange(nameof(SynthesizerElements));
                NotifyOfPropertyChange(nameof(SelectedSynthesizerElement));
                NotifyOfPropertyChange(nameof(SearcherElements));
                NotifyOfPropertyChange(nameof(SelectedSearcherElement));
            }
        }

        public void Reload()
        {
            UpdateAssemblies();
            UpdateAvailableTypes();
            UpdateModules();
            //ModuleManager.Instance.Load();
            //NotifyOf ?
        }

        public void AddAssembly()
        {
            // TODO: Window manager
            var path = DialogUtilities.OpenFileDialog(filter: @"DLL files (*.dll) |*.dll");
            if (path == null || string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            SafeActions.Run(() =>
            {
                //ModuleService.AddInstancesFromAssembly(path, typeof(IModule),
                //    type => type.AutoCreateInstance());

                UpdateAssemblies();
            });
        }

        public void DeleteItem(ItemViewModel viewModel)
        {
            switch (viewModel)
            {
                case IgnoredApplicationViewModel _:
                    //HookService.HookIgnoredApps = HookService.HookIgnoredApps.Except(new[] { viewModel.Description }).ToList();
                    IgnoredApplications?.Remove(viewModel);
                    break;

                case AssemblyViewModel _:
                    //ModuleService.Uninstall(viewModel.Description);
                    //ModuleService.UpdateActiveModules();
                    Assemblies?.Remove(viewModel);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        public void UpdateItem(ItemViewModel viewModel)
        {
            switch (viewModel)
            {
                case AssemblyViewModel _:
                    //ModuleService.Update(viewModel.Description);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        public void AddItem(ItemViewModel viewModel)
        {
            switch (viewModel)
            {
                case AvailableTypeViewModel availableTypeViewModel:
                    //ModuleService.AddInstance($"{viewModel.Name}_{new Random().Next()}", availableTypeViewModel.Type, false);
                    UpdateModules();
                    // TODO: Focus to Modules tab?
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        public void DeleteInstance(InstanceViewModel viewModel)
        {
            // TODO: replace
            var result = MessageBox.Show(
                "Are you sure that you want to delete this instance of module?", "Warning",
                MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            //ModuleService.DeleteInstance(viewModel.Name);
            UpdateModules();
        }

        public async Task RenameInstanceAsync(InstanceViewModel viewModel)
        {
            var oldName = viewModel.Name;
            var renameViewModel = new RenameViewModel(oldName, oldName);

            var result = await this.ShowDialogAsync(renameViewModel);
            if (result != true)
            {
                return;
            }

            var newName = renameViewModel.NewName;
            if (string.IsNullOrWhiteSpace(newName) ||
                string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            //ModuleService.RenameInstance(oldName, newName);
            viewModel.Name = newName;
        }

        public Task EditInstanceAsync(InstanceViewModel viewModel)
        {
            //var moduleSettingsView = new ModuleSettingsViewModel(viewModel.Instance.Value);
            //var result = await this.ShowDialogAsync(moduleSettingsView);
            //if (result != true)
            {
               return Task.CompletedTask;
            }

            //UpdateModules(); // TODO: update single item??
        }

        public void EnableInstance(InstanceViewModel viewModel)
        {
            viewModel.IsEnabled = !viewModel.IsEnabled;
            //ModuleService.SetInstanceIsEnabled(viewModel.Name, viewModel.IsEnabled);
            //ModuleService.RegisterHandlers(RunnerService);

            UpdateModules(); // TODO: update single item??
        }

        public void AddIgnoredApplication()
        {
            var path = DialogUtilities.OpenFileDialog(filter: @"EXE files (*.exe) |*.exe");
            if (path == null || string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            //HookService.HookIgnoredApps = HookService.HookIgnoredApps.Concat(new[] { path }).ToList();
            IgnoredApplications?.Add(new IgnoredApplicationViewModel(path));
        }

        #endregion

    }
}
