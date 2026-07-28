using System.ClientModel;
using ConversationSuggestionService.Configuration;
using OpenAI;
using OpenAI.Chat;
using Tutorial01B.Models;

namespace Tutorial01B.DataAccess.Models
{
    public class AgentServiceMapper : IAgentServiceMapper
    {
        public AgentServiceModel MppFrom(AgentServiceDefinition agentServiceDefinition)
        {
            return new AgentServiceModel
            {
                Version = agentServiceDefinition.Version,
                Service = new ServiceModel
                {
                    Name = agentServiceDefinition.Service.Name,
                    DefaultLocale = agentServiceDefinition.Service.DefaultLocale,
                    DefaultTimeoutSeconds = agentServiceDefinition.Service.DefaultTimeoutSeconds,
                },
                Providers =
                [
                    .. agentServiceDefinition.Providers.Select(p => new ProviderModel
                    {
                        Id = p.Id,
                        Type = p.Type,
                        Authentication = new AuthenticationModel()
                        {
                            ApiKeyEnvVar = p.Authentication.ApiKeyEnvVar,
                            Type = p.Authentication.Type,
                        },
                        Defaults = new ProviderDefaultsModel
                        {
                            Temperature = p.Defaults.Temperature,
                            MaxOutputTokens = p.Defaults.MaxOutputTokens,
                        },
                        Endpoint = p.Endpoint,
                        Logging = p.Logging,
                    })
                ],
                Callbacks =
                [
                    .. agentServiceDefinition.Callbacks.Select(c => new CallbackModel
                    {
                        Id = c.Id,
                        Type = c.Type,
                        Url = c.Url,
                        IncludeAgentMetadata = c.IncludeAgentMetadata,
                        IncludeConversation = c.IncludeConversation,
                    })
                ],
                Execution = new ExecutionModel
                {
                    Mode = agentServiceDefinition.Execution.Mode,
                    MaxDegreeOfParallelism = agentServiceDefinition.Execution.MaxDegreeOfParallelism,
                    ReturnMode = agentServiceDefinition.Execution.ReturnMode,
                },
                Agents =
                [
                    .. agentServiceDefinition.Agents.Select(a => new AgentGroupModel()
                    {
                        Id = a.Id,
                        Source = a.Source,
                        PlainText = a.PlainText,
                        AgentGroup =
                        [
                            .. a.AgentGroup.Select(ag => new AgentModel
                            {
                                Id = ag.Id,
                                Name = ag.Name,
                                Enabled = ag.Enabled,
                                Type = ag.Type,
                                ProviderRef = ag.ProviderRef,
                                Deployment = ag.Deployment,
                                CallbackRef = ag.CallbackRef,
                                Priority = ag.Priority,
                                TimeoutSeconds = ag.TimeoutSeconds,
                                Prompt = new PromptModel
                                {
                                    System = ag.Prompt.System,
                                },
                                ChatClient = CreateChatClient(ag.Deployment, ag.ProviderRef, agentServiceDefinition.Providers, out ApiKeyCredential credential),
                                Credential = credential
                            })
                        ]

                    })
                ],
            };
        }

        private ChatClient CreateChatClient(string deploymentName, string providerRef, List<ProviderDefinition> providerDefinitions, out ApiKeyCredential credential)
        {
            ProviderDefinition providerDefinition = providerDefinitions.First(def => def.Id == providerRef);
            return CreateChatClient(deploymentName, providerDefinition, out credential);
        }

        private ChatClient CreateChatClient(string deploymentName, ProviderDefinition providerDefinition, out ApiKeyCredential credential)
        {
            credential = GetApiKeyCredential(providerDefinition);
            return new(
                credential: credential,
                model: deploymentName,
                options: new OpenAIClientOptions()
                {
                    Endpoint = new Uri(providerDefinition.Endpoint),
                });
        }

        private string GetApiKey(ProviderDefinition providerDefinition)
        {
            string apiKey = Environment.GetEnvironmentVariable(providerDefinition.Authentication.ApiKeyEnvVar);
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException($"API key for provider '{providerDefinition.Id}' is not set.");
            }

            return apiKey;
        }

        private ApiKeyCredential GetApiKeyCredential(ProviderDefinition providerDefinition)
        {
            string apiKey = GetApiKey(providerDefinition);
            return new ApiKeyCredential(apiKey);
        }
    }
}
