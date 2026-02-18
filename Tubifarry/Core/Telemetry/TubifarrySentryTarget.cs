#if !MASTER_BRANCH
using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NLog.Targets;
using Sentry;

namespace Tubifarry.Core.Telemetry
{
    [Target("TubifarrySentry")]
    public sealed class TubifarrySentryTarget : TargetWithLayout
    {
        private static readonly Dictionary<LogLevel, SentryLevel> LevelMap = new()
        {
            { LogLevel.Debug, SentryLevel.Debug },
            { LogLevel.Error, SentryLevel.Error },
            { LogLevel.Fatal, SentryLevel.Fatal },
            { LogLevel.Info, SentryLevel.Info },
            { LogLevel.Trace, SentryLevel.Debug },
            { LogLevel.Warn, SentryLevel.Warning }
        };

        private static readonly Dictionary<LogLevel, BreadcrumbLevel> BreadcrumbLevelMap = new()
        {
            { LogLevel.Debug, BreadcrumbLevel.Debug },
            { LogLevel.Error, BreadcrumbLevel.Error },
            { LogLevel.Fatal, BreadcrumbLevel.Critical },
            { LogLevel.Info, BreadcrumbLevel.Info },
            { LogLevel.Trace, BreadcrumbLevel.Debug },
            { LogLevel.Warn, BreadcrumbLevel.Warning }
        };

        public bool Enabled { get; set; } = true;
        public LogLevel MinimumEventLevel { get; set; } = LogLevel.Error;
        public LogLevel MinimumBreadcrumbLevel { get; set; } = LogLevel.Debug;

        protected override void Write(LogEventInfo logEvent)
        {
            if (!Enabled || !TubifarrySentry.IsEnabled)
                return;

            if (string.IsNullOrEmpty(logEvent.LoggerName) ||
                !logEvent.LoggerName.StartsWith("Tubifarry", StringComparison.Ordinal))
                return;

            try
            {
                if (logEvent.Level >= MinimumBreadcrumbLevel)
                {
                    var breadcrumbLevel = BreadcrumbLevelMap.GetValueOrDefault(logEvent.Level, BreadcrumbLevel.Info);
                    
                    var data = logEvent.Properties?
                        .Where(p => p.Key?.ToString() != "Sentry")
                        .ToDictionary(p => p.Key?.ToString() ?? "", p => p.Value?.ToString() ?? "");

                    SentrySdk.AddBreadcrumb(
                        logEvent.FormattedMessage,
                        logEvent.LoggerName,
                        level: breadcrumbLevel,
                        data: data?.Count > 0 ? data : null);
                }

                if (logEvent.Level >= MinimumEventLevel || logEvent.Exception != null)
                {
                    CaptureEvent(logEvent);
                }
            }
            catch
            {
            }
        }

        private void CaptureEvent(LogEventInfo logEvent)
        {
            var sentryEvent = new SentryEvent(logEvent.Exception)
            {
                Level = LevelMap.GetValueOrDefault(logEvent.Level, SentryLevel.Info),
                Logger = logEvent.LoggerName,
                Message = logEvent.FormattedMessage
            };

            sentryEvent.SetExtra("logger_name", logEvent.LoggerName);
            
            if (logEvent.CallerFilePath != null)
                sentryEvent.SetExtra("caller_file", logEvent.CallerFilePath);
            if (logEvent.CallerLineNumber > 0)
                sentryEvent.SetExtra("caller_line", logEvent.CallerLineNumber);
            if (logEvent.CallerMemberName != null)
                sentryEvent.SetExtra("caller_member", logEvent.CallerMemberName);

            if (logEvent.Properties != null)
            {
                foreach (var prop in logEvent.Properties)
                {
                    var key = prop.Key?.ToString();
                    if (key == "Sentry" && prop.Value is string[] fingerprint && fingerprint.Length > 0)
                    {
                        sentryEvent.SetFingerprint(fingerprint);
                    }
                    else if (key != null && !string.IsNullOrEmpty(key))
                    {
                        sentryEvent.SetExtra(key, prop.Value);
                    }
                }
            }

            SentrySdk.CaptureEvent(sentryEvent);
        }
    }
}
#endif
