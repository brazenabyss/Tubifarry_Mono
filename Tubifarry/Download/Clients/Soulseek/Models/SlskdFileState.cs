using NzbDrone.Core.Download;

namespace Tubifarry.Download.Clients.Soulseek.Models;

[Flags]
public enum TransferStates
{
    None = 0,
    Queued = 2,
    Initializing = 4,
    InProgress = 8,
    Completed = 16,
    Succeeded = 32,
    Cancelled = 64,
    TimedOut = 128,
    Errored = 256,
    Rejected = 512,
    Aborted = 1024,
    Locally = 2048,
    Remotely = 4096,
}

public class SlskdFileState(SlskdDownloadFile file)
{
    public SlskdDownloadFile File { get; private set; } = file;
    public int RetryCount { get; private set; }
    private bool _retried = false;
    public int MaxRetryCount { get; private set; } = 1;
    public string State => File.State;
    public string PreviousState { get; private set; } = "Requested";

    public DownloadItemStatus GetStatus()
    {
        DownloadItemStatus status = GetStatus(State);
        if ((status == DownloadItemStatus.Failed && RetryCount < MaxRetryCount) || _retried)
            return DownloadItemStatus.Warning;
        return status;
    }

    public static DownloadItemStatus GetStatus(string stateStr)
    {
        if (Enum.TryParse<TransferStates>(stateStr, ignoreCase: true, out TransferStates state))
            return GetStatus(state);
        return DownloadItemStatus.Queued;
    }

    public static DownloadItemStatus GetStatus(TransferStates state) => state switch
    {
        _ when state.HasFlag(TransferStates.Completed) && state.HasFlag(TransferStates.Succeeded) => DownloadItemStatus.Completed,
        _ when state.HasFlag(TransferStates.Completed) => DownloadItemStatus.Failed,
        _ when state.HasFlag(TransferStates.Rejected) => DownloadItemStatus.Failed,
        _ when state.HasFlag(TransferStates.TimedOut) => DownloadItemStatus.Failed,
        _ when state.HasFlag(TransferStates.Errored) => DownloadItemStatus.Failed,
        _ when state.HasFlag(TransferStates.Cancelled) => DownloadItemStatus.Failed,
        _ when state.HasFlag(TransferStates.Aborted) => DownloadItemStatus.Failed,
        _ when state.HasFlag(TransferStates.InProgress) => DownloadItemStatus.Downloading,
        _ when state.HasFlag(TransferStates.Initializing) => DownloadItemStatus.Queued,
        _ when state.HasFlag(TransferStates.Queued) => DownloadItemStatus.Queued,
        _ => DownloadItemStatus.Queued,
    };

    public void UpdateFile(SlskdDownloadFile file)
    {
        if (!_retried)
            PreviousState = State;
        else if (File != null && GetStatus(file.State) == DownloadItemStatus.Failed)
            PreviousState = "Requested";
        File = file;
        _retried = false;
    }

    public void UpdateMaxRetryCount(int maxRetryCount) => MaxRetryCount = maxRetryCount;

    public void IncrementAttempt()
    {
        _retried = true;
        RetryCount++;
    }
}
