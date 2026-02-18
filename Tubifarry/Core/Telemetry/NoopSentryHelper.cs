using System;
using Sentry;

namespace Tubifarry.Core.Telemetry
{
    public class NoopSentryHelper : ISentryHelper
    {
        public bool IsEnabled => false;

        public ISpan? StartSpan(string operation, string? description = null) => null;

        public void FinishSpan(ISpan? span, SpanStatus status = SpanStatus.Ok) { }

        public void FinishSpan(ISpan? span, Exception ex) { }

        public void SetSpanData(ISpan? span, string key, object? value) { }

        public void SetSpanTag(ISpan? span, string key, string value) { }

        public void AddBreadcrumb(string? message, string? category = null) { }

        public void CaptureException(Exception ex, string? message = null) { }
    }
}
