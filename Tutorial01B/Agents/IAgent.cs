namespace Tutorial01B.Agents;

public interface IAgent
{
    string Name { get; }

    Task<string> ReplyAsync(string input, CancellationToken cancellationToken = default);
}
