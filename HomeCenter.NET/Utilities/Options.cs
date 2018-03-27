﻿using System.Reflection;

namespace HomeCenter.NET.Utilities
{
    public static class Options
    {
        public static string FileName => Assembly.GetExecutingAssembly().Location;
        public const string CompanyName = "HomeCenter.NET";
        public const int IpcPortToHomeCenter = 19445;
        public const int IpcPortToDeskBand = 19446;
    }
}
