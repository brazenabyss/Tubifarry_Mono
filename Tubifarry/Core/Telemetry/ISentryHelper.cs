using System;
using Sentry;

namespace Tubifarry.Core.Telemetry
{
    public interface ISentryHelper
    {
        bool IsEnabled { get; }
        ISpan? StartSpan(string operation, string? description = null);
        void FinishSpan(ISpan? span, SpanStatus status = SpanStatus.Ok);
        void FinishSpan(ISpan? span, Exception ex);
        void SetSpanData(ISpan? span, string key, object? value);
        void SetSpanTag(ISpan? span, string key, string value);
        void AddBreadcrumb(string? message, string? category = null);
        void CaptureException(Exception ex, string? message = null);
    }
}
