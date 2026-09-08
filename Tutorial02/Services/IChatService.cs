namespace Tutorial02.Services;

public interface IChatService
{
    Task RunSampleAsync(CancellationToken cancellationToken = default);
}