using System.ClientModel;
using ConversationSuggestionService.Configuration;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using Tutorial02.Models;

namespace Tutorial02.DataAccess.Models
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
                        Input = new AgentInputModel
                        {
                            Source = a.Input.Source,
                            Format = a.Input.Format,
                            MaxTurns = a.Input.MaxTurns,
                        },
                        AgentGroup =
                        [
                            .. a.AgentGroup.Select(ag => MapAgent(ag, agentServiceDefinition.Providers))
                        ],
                        Coordinators =
                        [
                            .. a.Coordinators.Select(c => MapAgent(c, agentServiceDefinition.Providers))
                        ],
                        SummaryGroup =
                        [
                            .. a.SummaryGroup.Select(s => MapAgent(s, agentServiceDefinition.Providers))
                        ]

                    })
                ],
            };
        }

        private AgentModel MapAgent(AgentDefinition definition, List<ProviderDefinition> providerDefinitions)
        {
            return new AgentModel
            {
                Id = definition.Id,
                Name = definition.Name,
                Enabled = definition.Enabled,
                Type = definition.Type,
                ProviderRef = definition.ProviderRef,
                Deployment = definition.Deployment,
                CallbackRef = definition.CallbackRef,
                Priority = definition.Priority,
                TimeoutSeconds = definition.TimeoutSeconds,
                Prompt = new PromptModel
                {
                    System = definition.Prompt.System,
                },
                Input = new InputModel
                {
                    Source = definition.Input.Source,
                    Format = definition.Input.Format,
                    MaxTurns = definition.Input.MaxTurns,
                },
                Settings = new AgentSettingsModel
                {
                    Temperature = definition.Settings.Temperature,
                    MaxOutputTokens = definition.Settings.MaxOutputTokens,
                },
                ChatClient = CreateChatClient(definition.Deployment, definition.ProviderRef, providerDefinitions, out ApiKeyCredential credential),
                Credential = credential
            };
        }

        private IChatClient CreateChatClient(string deploymentName, string providerRef, List<ProviderDefinition> providerDefinitions, out ApiKeyCredential credential)
        {
            ProviderDefinition providerDefinition = providerDefinitions.First(def => def.Id == providerRef);
            return CreateChatClient(deploymentName, providerDefinition, out credential);
        }

        private IChatClient CreateChatClient(string deploymentName, ProviderDefinition providerDefinition, out ApiKeyCredential credential)
        {
            credential = GetApiKeyCredential(providerDefinition);
            ChatClient chatClient = new(
                credential: credential,
                model: deploymentName,
                options: new OpenAIClientOptions()
                {
                    Endpoint = new Uri(providerDefinition.Endpoint),
                });

            return chatClient.AsIChatClient();
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
