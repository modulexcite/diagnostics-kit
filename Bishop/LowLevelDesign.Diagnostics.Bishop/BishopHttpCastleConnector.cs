﻿using LowLevelDesign.Diagnostics.Bishop.Config;
using LowLevelDesign.Diagnostics.Commons.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;

namespace LowLevelDesign.Diagnostics.Bishop
{
    public sealed class BishopHttpCastleConnector
    {
        private class CastleSettings
        {
            public bool IsAuthenticationEnabled { get; set; }

            public string Version { get; set; }
        }

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings {
            NullValueHandling = NullValueHandling.Ignore,
            /* HACK: for some reason the ISO format did not work with the collector. It converted
             * this value to local time, completely skipping timezone settings. */
            DateFormatHandling = DateFormatHandling.MicrosoftDateFormat
        };
        private readonly PluginSettings settings;
        private readonly CookieContainer cookies = new CookieContainer();
        private bool isAuthenticationRequired = true;

        public BishopHttpCastleConnector(PluginSettings settings) 
        {
            this.settings = settings;
        }

        public bool AreSettingsValid()
        {
            try {
                AuthenticateIfNecessary();
                return !isAuthenticationRequired || IsAuthenticationCookieSet();
            } catch {
                return false;
            }
        }

        public IEnumerable<ApplicationServerConfig> ReadApplicationConfigs()
        {
            if (!AreSettingsValid()) {
                throw new Exception("There was an error when trying to access Diagnostics Castle. " +
                    "Please make sure the settings are correct.");
            }
            return JsonConvert.DeserializeObject<ApplicationServerConfig[]>(MakeGetRequest(
                string.Format("{0}/conf/appsrvconfigs", settings.DiagnosticsUrl)), JsonSettings);
        }

        private string MakeGetRequest(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.CookieContainer = cookies;
            request.AllowAutoRedirect = false;

            using (var reader = new StreamReader(request.GetResponse().GetResponseStream())) {
                return reader.ReadToEnd();
            }
        }

        private string MakePostRequest(string url, string postData) {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.CookieContainer = cookies;

            request.Method = "POST";
            request.AllowAutoRedirect = false;
            request.ContentType = "application/x-www-form-urlencoded";
            using (var writer = new StreamWriter(request.GetRequestStream())) {
                writer.Write(postData);
            }
            using (var reader = new StreamReader(request.GetResponse().GetResponseStream())) {
                return reader.ReadToEnd();
            }
        }

        private void AuthenticateIfNecessary()
        {
            if (isAuthenticationRequired && !IsAuthenticationCookieSet())
            {
                var castleSettings = JsonConvert.DeserializeObject<CastleSettings>(MakeGetRequest(
                    string.Format("{0}/about.json", settings.DiagnosticsUrl)));
                isAuthenticationRequired = castleSettings.IsAuthenticationEnabled;
                if (isAuthenticationRequired) {
                    MakePostRequest(string.Format("{0}/auth/login", settings.DiagnosticsUrl),
                        string.Format("Login={0}&Password={1}", WebUtility.UrlEncode(settings.UserName),
                        WebUtility.UrlEncode(settings.GetPassword())));
                }
            }
        }

        private bool IsAuthenticationCookieSet()
        {
            return cookies.GetCookies(settings.DiagnosticsUrl)[".AspNet.ApplicationCookie"] != null;
        }
    }
}
