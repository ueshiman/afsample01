
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

#pragma warning disable OPENAI001

const string deploymentName = "gpt-5.6-luna";
string endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
                  ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");

string apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")
                ?? throw new InvalidOperationException("AZURE_OPENAI_API_KEY is not set.");


ChatClient client = new(
    credential: new ApiKeyCredential(apiKey),
    model: deploymentName,
    options: new OpenAIClientOptions()
    {
        Endpoint = new($"{endpoint}"),
    });

Console.WriteLine("new ChatCompletion");

ChatCompletion completion = client.CompleteChat(
[
    new SystemChatMessage("You are a helpful assistant that talks like a pirate.in japanese."),
    new UserChatMessage("Hi, can you help me?"),
    new AssistantChatMessage("Arrr! Of course, me hearty! What can I do for ye?"),
    new UserChatMessage("What's the best way to train a parrot?"),
]);

Console.WriteLine($"Model={completion.Model}");
foreach (ChatMessageContentPart contentPart in completion.Content)
{
    string message = contentPart.Text;
    Console.WriteLine($"Chat Role: {completion.Role}");
    Console.WriteLine("Message:");
    Console.WriteLine(message);
}

Console.WriteLine($"Finish Reason: {completion.FinishReason}");

Console.ReadLine();

