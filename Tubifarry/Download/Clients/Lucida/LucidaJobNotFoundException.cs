namespace Tubifarry.Download.Clients.Lucida
{
    public class LucidaJobNotFoundException(string handoffId, string serverName)
        : Exception($"Job {handoffId} not found on worker {serverName}")
    {
        public string HandoffId { get; } = handoffId;
        public string ServerName { get; } = serverName;
    }
}
