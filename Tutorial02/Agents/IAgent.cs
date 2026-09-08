namespace Tutorial02.Agents;

public interface IAgent
{
    string Name { get; }

    Task<string> ReplyAsync(string input, CancellationToken cancellationToken = default);
}
