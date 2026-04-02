namespace Tutorial01B.Services;

public interface IChatService
{
    Task RunSampleAsync(CancellationToken cancellationToken = default);
}