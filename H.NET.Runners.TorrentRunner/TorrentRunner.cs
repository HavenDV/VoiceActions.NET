﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Google.Apis.Customsearch.v1;
using Google.Apis.Services;
using H.NET.Core.Runners;
using H.NET.Core.Settings;
using HtmlAgilityPack;
using MonoTorrent.Common;

namespace H.NET.Runners.TorrentRunner
{
    public class TorrentRunner : Runner
    {
        #region Properties

        private string SaveTo { get; set; }
        private string QBitTorrentPath { get; set; }
        private string GoogleSearchApiKey { get; set; }
        private string GoogleCx { get; set; }
        private string GooglePattern { get; set; }
        private int Delay { get; set; }
        private double MinSizeGb { get; set; }
        private double MaxSizeGb { get; set; }
        private string Extension { get; set; }
        private int MaxResults { get; set; }

        private string TorrentsFolder => Path.Combine(SaveTo, "Torrents");
        private string DownloadsFolder => Path.Combine(SaveTo, "Downloads");

        #endregion

        #region Constructors

        public TorrentRunner()
        {
            AddSetting(nameof(SaveTo), o => SaveTo = o, NoEmpty, string.Empty, SettingType.Folder);
            AddSetting(nameof(QBitTorrentPath), o => QBitTorrentPath = o, FileExists, string.Empty, SettingType.Path);
            AddSetting(nameof(Delay), o => Delay = o, null, 10000);
            AddSetting(nameof(GoogleSearchApiKey), o => GoogleSearchApiKey = o, NoEmpty, string.Empty);
            AddSetting(nameof(GoogleCx), o => GoogleCx = o, NoEmpty, string.Empty);
            AddSetting(nameof(GooglePattern), o => GooglePattern = o, NoEmpty, "download torrent *");
            AddSetting(nameof(MinSizeGb), o => MinSizeGb = o, null, 1.0);
            AddSetting(nameof(MaxSizeGb), o => MaxSizeGb = o, null, 4.0);
            AddSetting(nameof(Extension), o => Extension = o, NoEmpty, ".mkv");
            AddSetting(nameof(MaxResults), o => MaxResults = o, null, 30);

            AddAction("torrent", TorrentCommand, "text");

            Settings.PropertyChanged += (sender, args) =>
            {
                if (string.IsNullOrWhiteSpace(SaveTo))
                {
                    return;
                }

                Directory.CreateDirectory(TorrentsFolder);
                Directory.CreateDirectory(DownloadsFolder);
            };
        }

        private static bool FileExists(string key) => NoEmpty(key) && File.Exists(key);

        #endregion

        #region Private methods

        private static List<string> GetTorrentsFromUrl(string url)
        {
            try
            {
                var web = new HtmlWeb();
                var document = web.Load(url);

                var torrents = document.DocumentNode
                    .SelectNodes("//a[@href]")
                    .Select(i => i.Attributes["href"].Value)
                    .Where(i => i.EndsWith(".torrent"))
                    .ToList();

                var uri = new Uri(url);
                var baseUrl = $"{uri.Scheme}://{uri.Host}";
                return torrents
                    .Select(i => i.Contains("http") ? i : $"{baseUrl}{i}")
                    .ToList();
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }

        private string IsGoodTorrent(string url)
        {
            using (var client = new WebClient())
            {
                var temp = Path.GetTempFileName();

                client.DownloadFile(url, temp);
                var torrent = Torrent.Load(temp);
                var sizeGb = torrent.Size / 1000.0 / 1000.0 / 1000.0;
                var path = torrent.Files.FirstOrDefault()?.Path;
                var extension = Path.GetExtension(path);
                if (sizeGb > MinSizeGb &&
                    sizeGb < MaxSizeGb &&
                    string.Equals(extension, Extension, StringComparison.OrdinalIgnoreCase))
                {
                    Print($"Size: {sizeGb:F2} Gb");
                    Print($"Path: {path}");
                    Print($"Extension: {extension}");

                    return temp;
                }

                File.Delete(temp);
                return null;
            }
        }

        private string FindGoodTorrent(IEnumerable<string> urls)
        {
            foreach (var url in urls ?? new List<string>())
            {
                var torrents = GetTorrentsFromUrl(url);
                foreach (var torrent in torrents)
                {
                    var path = IsGoodTorrent(torrent);
                    if (path != null)
                    {
                        return path;
                    }
                }
            }

            return null;
        }

        private List<string> GoogleCommand(string query)
        {
            using (var service = new CustomsearchService(
                new BaseClientService.Initializer
                {
                    ApiKey = GoogleSearchApiKey
                }))
            {
                var requests = service.Cse.List(query);
                requests.Cx = GoogleCx;
                requests.Num = MaxResults;

                var results = requests.Execute().Items;
                if (results == null)
                {
                    return new List<string>();
                }

                return results.Select(i => i.Link).ToList();
            }
        }

        private void TorrentCommand(string text)
        {
            Say($"Ищу торрент {text}");

            var query = GooglePattern.Replace("*", text);
            var urls = GoogleCommand(query);
            if (!urls.Any())
            {
                Say("Поиск в гугле не дал результатов");
                return;
            }

            var path = FindGoodTorrent(urls);
            if (path == null)
            {
                Say("Не найден подходящий торрент");
                return;
            }

            Say("Нашла!");
            QTorrentCommand(path);
        }

        private async void QTorrentCommand(string torrentPath)
        {
            var path = GetFilePath(torrentPath);
            if (RunCommand(path, false))
            {
                return;
            }

            try
            {
                Process.Start(QBitTorrentPath, $"--sequential --skip-dialog=true --save-path=\"{DownloadsFolder}\" {torrentPath}");
                Say($@"Загружаю. До запуска {Delay / 1000} секунд");
            }
            catch (Exception e)
            {
                Say(@"Ошибка загрузки");
                Log(e.ToString());
                return;
            }

            await Task.Delay(Delay);

            RunCommand(path);

        }

        private bool RunCommand(string path, bool sayError = true, bool saySuccess = true)
        {
            if (!File.Exists(path))
            {
                if (sayError)
                {
                    Say(@"Файл не найден или еще не загружен");
                }

                return false;
            }

            Process.Start(path);
            if (saySuccess)
            {
                Say(@"Запускаю");
            }

            return true;
        }

        private string GetFilePath(string torrentPath)
        {
            var torrent = Torrent.Load(torrentPath);
            var subPath = torrent.Files.FirstOrDefault()?.FullPath;
            var path = Path.Combine(DownloadsFolder, subPath ?? string.Empty);

            return path;
        }

        /*

        private ClientEngine CreateEngine()
        {
            // Torrents will be downloaded here by default when they are registered with the engine
            // Tell the engine to listen at port 6969 for incoming connections
            // If both encrypted and unencrypted connections are supported, an encrypted connection will be attempted
            // first if this is true. Otherwise an unencrypted connection will be attempted first.
            return new ClientEngine(new EngineSettings(TorrentsFolder, 6969)
            {
                AllowedEncryption = EncryptionTypes.All,
                PreferEncryption = true
            });
        }

        private void TorrentCommand(string text)
        {
            var torrent = Torrent.Load(text);
            Print($"Files in torrent: {text}");
            foreach (var file in torrent.Files)
            {
                Print($"Path: {file.Path}");
                Print($"FullPath: {file.FullPath}");
            }

            Print($"Created by: {torrent.Files}");
            Print($"Creation date: {torrent.CreationDate}");
            Print($"Comment: {torrent.Comment}");
            Print($"Publish URL: {torrent.PublisherUrl}");
            Print($"Size: {torrent.Size}");

            /*
            Engine?.Dispose();
            Engine = CreateEngine();

            var torrent = Torrent.Load(text);
            Print($"Created by: {torrent.CreatedBy}");
            Print($"Creation date: {torrent.CreationDate}");
            Print($"Comment: {torrent.Comment}");
            Print($"Publish URL: {torrent.PublisherUrl}");
            Print($"Size: {torrent.Size}");

            var manager = new TorrentManager(torrent, DownloadsFolder, new TorrentSettings(10, 10), SaveTo);
            Engine.Register(manager);

            manager.TorrentStateChanged += (sender, args) => Print($"New state: {args.NewState:G}");

            //manager.Start();

            Managers.Add(manager);

            Engine.StartAll();

            /* Generate the paths to the folder we will save .torrent files to and where we download files to 
            main.basePath = SaveTo;						// This is the directory we are currently in
            main.torrentsPath = Path.Combine(main.basePath, "Torrents");				// This is the directory we will save .torrents to
            main.downloadsPath = Path.Combine(main.basePath, "Downloads");			// This is the directory we will save downloads to
            main.fastResumeFile = Path.Combine(main.torrentsPath, "fastresume.data");
            main.dhtNodeFile = Path.Combine(main.basePath, "DhtNodes");
            main.torrents = new List<TorrentManager>();							// This is where we will store the torrentmanagers
            main.listener = new Top10Listener(10);

            // We need to cleanup correctly when the user closes the window by using ctrl-c
            // or an unhandled exception happens
            //Console.CancelKeyPress += delegate { shutdown(); };
            //AppDomain.CurrentDomain.ProcessExit += delegate { shutdown(); };
            //AppDomain.CurrentDomain.UnhandledException += delegate (object sender, UnhandledExceptionEventArgs e) { Print(e.ExceptionObject.ToString()); shutdown(); };
            //Thread.GetDomain().UnhandledException += delegate (object sender, UnhandledExceptionEventArgs e) { Print(e.ExceptionObject.ToString()); shutdown(); };

            main.PrintAction = Log;
            main.StartEngine(31337);
            //main.Main(SaveTo, 6969, Log);
        }
        */
        #endregion
    }
}
