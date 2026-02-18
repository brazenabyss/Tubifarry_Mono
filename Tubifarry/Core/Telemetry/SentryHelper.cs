#if !MASTER_BRANCH
using System;
using Sentry;

namespace Tubifarry.Core.Telemetry
{
    public class SentryHelper : ISentryHelper
    {
        public bool IsEnabled => TubifarrySentry.IsEnabled;

        public ISpan? StartSpan(string operation, string? description = null)
        {
            var parent = SentrySdk.GetSpan();
            if (parent == null)
                return null;

            var span = parent.StartChild(operation, description ?? operation);
            span.SetTag("plugin", "tubifarry");
            span.SetTag("branch", PluginInfo.Branch);
            return span;
        }

        public void FinishSpan(ISpan? span, SpanStatus status = SpanStatus.Ok)
            => span?.Finish(status);

        public void FinishSpan(ISpan? span, Exception ex)
        {
            if (span == null)
                return;

            var status = ex switch
            {
                TimeoutException => SpanStatus.DeadlineExceeded,
                OperationCanceledException => SpanStatus.Cancelled,
                UnauthorizedAccessException => SpanStatus.PermissionDenied,
                ArgumentException => SpanStatus.InvalidArgument,
                _ => SpanStatus.InternalError
            };

            span.Finish(ex, status);
        }

        public void SetSpanData(ISpan? span, string key, object? value)
        {
            if (span != null && value != null)
                span.SetExtra(key, value);
        }

        public void SetSpanTag(ISpan? span, string key, string value)
            => span?.SetTag(key, value);

        public void AddBreadcrumb(string? message, string? category = null)
        {
            if (!string.IsNullOrEmpty(message))
                SentrySdk.AddBreadcrumb(message, category);
        }

        public void CaptureException(Exception ex, string? message = null)
        {
            if (!string.IsNullOrEmpty(message))
                SentrySdk.CaptureException(ex, scope => scope.SetExtra("message", message));
            else
                SentrySdk.CaptureException(ex);
        }
    }
}
#endif
