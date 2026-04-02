using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

#pragma warning disable OPENAI001

const string deploymentName = "gpt-5.2";
string endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
                  ?? "https://agftutorial-resource.openai.azure.com/openai/v1/";
string apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")
                ?? throw new InvalidOperationException("AZURE_OPENAI_API_KEY is not set.");

ChatClient client = new(
    credential: new ApiKeyCredential(apiKey),
    model: deploymentName,
    options: new OpenAIClientOptions()
    {
        Endpoint = new Uri(endpoint),
    });

ChatCompletion completion = client.CompleteChat(
[
    new SystemChatMessage("You are a helpful assistant that talks like a pirate in Japanese."),
    new UserChatMessage("Hi, can you help me?"),
    new AssistantChatMessage("Arrr! もちろんでござる…じゃなくて海賊風に手伝うぜ！"),
    new UserChatMessage("What's the best way to train a parrot?")
]);

Console.WriteLine($"Model={completion.Model}");
Console.WriteLine($"Chat Role: {completion.Role}");

foreach (ChatMessageContentPart contentPart in completion.Content)
{
    if (!string.IsNullOrEmpty(contentPart.Text))
    {
        Console.WriteLine("Message:");
        Console.WriteLine(contentPart.Text);
    }
}

Console.WriteLine($"Finish Reason: {completion.FinishReason}");
