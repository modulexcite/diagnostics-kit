﻿using LowLevelDesign.Diagnostics.Commons.Models;
using Nancy;
using System;
using System.Collections.Generic;

namespace LowLevelDesign.Diagnostics.Castle.Modules
{
    public sealed class SyncModule : NancyModule
    {
        public SyncModule() {
            Get["/ping"] = _ => {
                var syncModel = new SyncModel {
                    Host = Request.Url.HostName,
                    Port = Request.Url.Port ?? 80,
                    Path = Request.Url.BasePath
                };
                var owinenv = Context.Items["OWIN_REQUEST_ENVIRONMENT"] as IDictionary<String, Object>;
                if (owinenv != null) {
                    if (owinenv.ContainsKey("server.LocalIpAddress")) {
                        syncModel.IpAddr = (String)owinenv["server.LocalIpAddress"];
                        // HACK: for some reason WebRequest does not accept ::1 as uri
                        if (String.Equals(syncModel.IpAddr, "::1", StringComparison.Ordinal)) {
                            syncModel.IpAddr = "127.0.0.1";
                        }
                    }
                    int port;
                    if (owinenv.ContainsKey("server.LocalPort") && Int32.TryParse((String)owinenv["server.LocalPort"], out port)) {
                        syncModel.Port = port;
                    }
                }
                return Response.AsJson(syncModel);
            };
        }
    }
}