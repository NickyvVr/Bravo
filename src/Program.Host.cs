﻿using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;
using Microsoft.Extensions.Logging.EventLog;
using System;
using System.Net;

namespace Sqlbi.Bravo
{
    internal partial class Program
    {
        private static IHost CreateHost()
        {
            var hostBuilder = new HostBuilder();

            hostBuilder.UseEnvironment(Environments.Production);
            hostBuilder.UseContentRoot(Environment.CurrentDirectory);

            hostBuilder.ConfigureHostConfiguration((builder) =>
            {
                builder.SetBasePath(Environment.CurrentDirectory);
                //builder.AddJsonFile("hostsettings.json", optional: true);
                //builder.AddEnvironmentVariables(prefix: "CUSTOMPREFIX_");
                //builder.AddCommandLine(args);
            });

            hostBuilder.ConfigureAppConfiguration((HostBuilderContext hostingContext, IConfigurationBuilder config) =>
            {
                //var hostingEnvironment = hostingContext.HostingEnvironment;
                //var reloadConfigOnChange = hostingContext.Configuration.GetValue("hostBuilder:reloadConfigOnChange", defaultValue: true);

                // TODO: rename and move to user-settings file/folder
                config.AddJsonFile($"appsettings.json", optional: true, reloadOnChange: true);
                //config.AddJsonFile($"appsettings.{ hostingEnvironment.EnvironmentName }.json", optional: true, reloadConfigOnChange);

                //if (hostingEnvironment.IsDevelopment() && !string.IsNullOrEmpty(hostingEnvironment.ApplicationName))
                //{
                //    var assembly = Assembly.Load(new AssemblyName(hostingEnvironment.ApplicationName));
                //    if (assembly != null)
                //        config.AddUserSecrets(assembly, optional: true);
                //}

                //config.AddEnvironmentVariables();

                //if (args != null)
                //    config.AddCommandLine(args);
            });

            hostBuilder.ConfigureLogging((HostBuilderContext context, ILoggingBuilder logging) =>
            {
                logging.AddFilter<ApplicationInsightsLoggerProvider>((LogLevel level) => level >= LogLevel.Warning);
                logging.AddFilter<EventLogLoggerProvider>((LogLevel level) => level >= LogLevel.Warning);
                //logging.AddConfiguration(context.Configuration.GetSection("Logging"));
                logging.AddApplicationInsights();
                logging.AddEventSourceLogger();
                logging.AddEventLog();
#if DEBUG
                logging.AddConsole();
                logging.AddDebug();
#endif
                logging.Configure((LoggerFactoryOptions options) =>
                {
                    options.ActivityTrackingOptions = ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId | ActivityTrackingOptions.ParentId;
                });
            });

            hostBuilder.UseDefaultServiceProvider((HostBuilderContext context, ServiceProviderOptions options) =>
            {
                options.ValidateOnBuild = (options.ValidateScopes = context.HostingEnvironment.IsDevelopment());
            });

            hostBuilder.ConfigureWebHostDefaults((webBuilder) =>
            {
                //webBuilder.ConfigureLogging(builder =>
                //{
                //    builder.
                //});

                // Empty and ignore default URLs configured on the IWebHostBuilder - this remove the warning 'Microsoft.AspNetCore.Server.Kestrel: Warning: Overriding address(es) 'https://localhost:5001/, http://localhost:5000/'. Binding to endpoints defined in UseKestrel() instead.'
                webBuilder.UseUrls();

                webBuilder.UseKestrel((serverOptions) =>
                {
#if DEBUG
                    var listenEndpoint = new IPEndPoint(IPAddress.Loopback, port: 5000);
#else
                    var listenEndpoint = new IPEndPoint(Infrastructure.Helpers.NetworkHelper.GetLoopbackAddress(), port: 0);
#endif
                    // Allow sync IO - required by ImportVpax
                    serverOptions.AllowSynchronousIO = true;
                    serverOptions.Listen(listenEndpoint, (listenOptions) =>
                    {
#if DEBUG
                        listenOptions.UseConnectionLogging();
#endif
                        //listenOptions.UseHttps(); // TODO: do we need https ?
                    });
                });

                webBuilder.UseStartup<Startup>();
            });

            var host = hostBuilder.Build();
            return host;
        }
    }
}
