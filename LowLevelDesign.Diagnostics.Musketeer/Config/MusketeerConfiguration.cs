﻿/**
 *  Part of the Diagnostics Kit
 *
 *  Copyright (C) 2016  Sebastian Solnica
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.

 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 */

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;

namespace LowLevelDesign.Diagnostics.Musketeer.Config
{
    static class MusketeerConfiguration
    {
        private readonly static string iisConfigurationRefreshCron = ConfigurationManager.AppSettings["job:iis-config-refresh-cron"] ?? "0 0 0/2 * * ?";
        private readonly static string perfMonitorCron = ConfigurationManager.AppSettings["job:perf-monitor-cron"] ?? "0 0/3 * * * ?";
        private readonly static string iisLogsReadCron = ConfigurationManager.AppSettings["job:iis-logs-read-cron"] ?? "0 0/1 * * * ?";
        private readonly static string checkUpdateCron = ConfigurationManager.AppSettings["job:check-updates"] ?? "0 /10 * * * ?";
        private readonly static bool shouldSendSuccessHttpLogs;
        private readonly static List<Regex> excludedServices;
        private readonly static List<Regex> includedServices;

        private readonly static Uri diagnosticsUrl;

        static MusketeerConfiguration()
        {
            var v = ConfigurationManager.AppSettings["lowleveldesign.diagnostics.url"];
            if (v != null) {
                try {
                    diagnosticsUrl = new Uri(v);
                } catch (UriFormatException) {
                    diagnosticsUrl = null;
                }
            } else {
                diagnosticsUrl = null;
            }
            if (diagnosticsUrl == null) {
                throw new ConfigurationErrorsException("Missing URL to the Diagnostics Castle. Please add 'lowleveldesign.diagnostics.url' key to the " +
                    "appsettings section of the Musketeer configuration - the value should be an url to the Diagnostics Castle application.");
            }

            excludedServices = ParsePatternToRegexList(ConfigurationManager.AppSettings["exclude-services"]);
            includedServices = ParsePatternToRegexList(ConfigurationManager.AppSettings["include-services"]);
            shouldSendSuccessHttpLogs = Boolean.Parse(ConfigurationManager.AppSettings["include-http-success-logs"] ?? "false");
        }

        private static List<Regex> ParsePatternToRegexList(string pattern)
        {
            var res = new List<Regex>();
            if (pattern != null) {
                foreach (var str in pattern.Split(new [] { ';' }, StringSplitOptions.RemoveEmptyEntries)) {
                    res.Add(new Regex(str, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline));
                }
            }
            return res;
        }

        public static Uri DiagnosticsCastleUrl
        {
            get { return diagnosticsUrl; }
        }

        public static string PerformanceMonitorJobCron
        {
            get { return perfMonitorCron; }
        }

        public static string IISConfigurationRefreshCron
        {
            get { return iisConfigurationRefreshCron; }
        }

        public static string IISLogsReadCron
        {
            get { return iisLogsReadCron; }
        }

        public static string CheckUpdateCron
        {
            get { return checkUpdateCron; }
        }

        public static bool ShouldSendSuccessHttpLogs
        {
            get { return shouldSendSuccessHttpLogs; }
        }

        public static bool ShouldServiceBeIncluded(string serviceName)
        {
            return includedServices.Any(rgx => rgx.IsMatch(serviceName));
        }

        public static bool ShouldServiceBeExcluded(string serviceName)
        {
            return excludedServices.Any(rgx => rgx.IsMatch(serviceName));
        }
    }
}
