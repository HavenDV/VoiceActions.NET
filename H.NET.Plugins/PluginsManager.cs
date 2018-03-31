﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using H.NET.Plugins.Extensions;
using H.NET.Plugins.Utilities;

namespace H.NET.Plugins
{
    public class PluginsManager<T> : AssembliesManager, IDisposable where T : class
    {
        #region Properties

        public const string InstancesSubFolder = "Instances";
        public const string TempSubFolder = "Temp";
        public const string SettingsExtension = ".txt";
        public const string InstancesFileName = "instances.txt";

        public Action<T, string> LoadAction { get; }
        public Func<T, string> SaveFunc { get; }

        public string SettingsFolder { get; }
        public string TempFolder { get; }
        public string InstancesFilePath { get; }

        private string GetSettingsFilePath(string name) => Path.Combine(SettingsFolder, $"{name}{SettingsExtension}");
        private string GetTempDirectory() => DirectoryUtilities.CombineAndCreateDirectory(TempFolder, $"{new Random().Next()}");

        private string GetFileCopyFromTempWithOtherFiles(string path)
        {
            var filename = Path.GetFileName(path);
            var folder = Path.GetDirectoryName(path);
            var temp = GetTempDirectory();
            DirectoryUtilities.Copy(folder, temp);

            return Path.Combine(temp, filename);
        }

        private void TryClean()
        {
            Directory.EnumerateFiles(TempFolder, "*.*", SearchOption.AllDirectories).AsParallel().ForAll(path =>
            {
                try
                {
                    File.Delete(path);
                }
                catch (Exception)
                {
                    //ignored
                }
            });
        }

        public List<Type> AvailableTypes { get; private set; } = new List<Type>();
        private Type GetTypeByFullName(string name) => AvailableTypes
            .FirstOrDefault(i => string.Equals(i.FullName, name, StringComparison.OrdinalIgnoreCase));

        public Instances<T> Instances { get; set; }

        #endregion

        #region Constructors

        public PluginsManager(string companyName, Action<T, string> loadAction, Func<T, string> saveFunc) : base(companyName)
        {
            LoadAction = loadAction;
            SaveFunc = saveFunc;

            SettingsFolder = DirectoryUtilities.CombineAndCreateDirectory(BaseFolder, InstancesSubFolder);
            TempFolder = DirectoryUtilities.CombineAndCreateDirectory(BaseFolder, TempSubFolder);
            InstancesFilePath = Path.Combine(SettingsFolder, InstancesFileName);
        }

        #endregion

        #region Public methods

        #region Load/Save

        public override void Load()
        {
            try
            {
                base.Load();

                Dispose();

                LoadPlugins();
            }
            catch (Exception exception)
            {
                Log($"Load Plugins: {exception}");
            }

            TryClean();
        }

        public override void Save()
        {
            try
            {
                base.Save();

                foreach (var pair in Instances.Objects)
                {
                    SavePluginSettings(pair.Key, pair.Value.Value);
                }
            }
            catch (Exception exception)
            {
                Log($"Save Plugins: {exception}");
            }

            TryClean();
        }

        #endregion

        public List<KeyValuePair<string, T1>> GetPluginsOfSubtype<T1>() where T1 : T
        {
            return Instances.Objects
                .Where(i => i.Value.IsEnabled && i.Value.Value is T1)
                .Select(i => new KeyValuePair<string, T1>(i.Key, (T1)i.Value.Value))
                .ToList();
        }

        private void AddInstance(string name, string typeName)
        {
            Instances.Add(name, typeName);

            var path = GetSettingsFilePath(name);
            if (!File.Exists(path))
            {
                File.WriteAllText(path, string.Empty);
            }

            SetInstanceIsEnabled(name, true);
        }

        public void AddInstance(string name, Type type) => AddInstance(name, type.FullName);

        public void AddInstancesFromAssembly(string path, Type interfaceType, Predicate<Type> filter = null)
        {
            path = GetFileCopyFromTempWithOtherFiles(path);

            var assembly = InstallAndGet(path);
            var types = assembly.GetTypesOfInterface(interfaceType);
            foreach (var type in types)
            {
                if (TypeIsAvailable(type) &&
                    filter?.Invoke(type) != false)
                {
                    AddInstance(type.Name, type);
                }
            }
        }

        public void DeleteInstance(string name)
        {
            SetInstanceIsEnabled(name, false);

            Instances.Delete(name);
        } 

        public void SetInstanceIsEnabled(string name, bool value)
        {
            if (!Instances.Contains(name))
            {
                throw new InstanceNotFoundException(name);
            }

            var instanceObject = Instances.GetObject(name);
            var instanceInfo = Instances.GetInfo(name);
            if (value)
            {
                try
                {
                    var typeName = instanceInfo.TypeName;
                    var type = GetTypeByFullName(typeName);
                    if (type == null)
                    {
                        //Log($"Load Plugins: Type \"{typeName}\" is not found in current assemblies");

                        instanceObject.Exception = new Exception($"Type \"{typeName}\" is not found in current assemblies");
                        return;
                    }

                    var obj = CreateInstance<T>(type);

                    LoadPluginSettings(name, obj);

                    instanceObject.Value = obj;
                }
                catch (Exception exception)
                {
                    instanceObject.Exception = exception;
                }
            }
            else
            {
                instanceObject.Dispose();
            }

            instanceInfo.IsEnabled = instanceObject.IsEnabled;
            Instances.Save();
        }

        #endregion

        #region Private methods

        #region Load/Save Settings

        private void LoadPluginSettings(string name, T plugin)
        {
            var path = GetSettingsFilePath(name);
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return;
            }

            var text = File.ReadAllText(path);
            LoadAction?.Invoke(plugin, text);
        }

        private void SavePluginSettings(string name, T plugin)
        {
            var text = SaveFunc?.Invoke(plugin);
            if (text == null)
            {
                return;
            }

            File.WriteAllText(GetSettingsFilePath(name), text);
        }

        #endregion

        private List<Type> GetAvailableTypes() => ActiveAssemblies
            .SelectMany(i => i.GetTypesOfInterface<T>())
            .Where(TypeIsAvailable)
            .ToList();

        private bool TypeIsAvailable(Type type) => type.GetConstructors().Any(c => c.IsPublic && c.GetParameters().Length == 0);

        private void LoadPlugins()
        {
            Instances = new Instances<T>(InstancesFilePath);
            AvailableTypes = GetAvailableTypes();
            foreach (var pair in Instances.Informations)
            {
                var instance = pair.Value;
                var name = instance.Name;
                var typeName = instance.TypeName;
                if (string.IsNullOrWhiteSpace(name) ||
                    string.IsNullOrWhiteSpace(typeName))
                {
                    //Log($"Load Plugins: Invalid file name: {fileName}");

                    continue;
                }

                SetInstanceIsEnabled(name, instance.IsEnabled);
            }
        }

        #endregion

        #region IDisposable

        public virtual void Dispose()
        {
            Instances?.Dispose();
            Instances = null;
        }

        #endregion
    }
}
