using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel;
using SemanticKernelPlayground.IOAdapters;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace SemanticKernelPlayground.Services;

public class ChatBot(IIoAdapter iIoAdapter, Kernel kernel)
{
    public async Task RunAsync()
    {
        var openAiPromptExecutionSettings = new AzureOpenAIPromptExecutionSettings
        {
            // Functions management
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),        // Auto-invocation of functions

            // System prompt for the assistant
            ChatSystemPrompt =
                """
                You are ReleaseNotesGPT, a concise yet precise technical writer.
                • Always respond in GitHub-flavoured markdown.
                • Section order: Changelog → Features → Fixes → Dependency Updates → Build & CI.
                • If no commits map to a section - omit the section.
                • Never fabricate commits. Base everything on {{commits}} parameter.
                """,

            // Prompt settings
            MaxTokens = 1024,
            Temperature = 0.2, // More deterministic output
            TopP = 0.9,         
            PresencePenalty = 0.1, // Less likely to repeat the same phrases
            FrequencyPenalty = 0.1
        };

        var history = new ChatHistory();

        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        await iIoAdapter.WriteAsync("Hello! I am your git assistant. How can I help you today?");

        string? userInput;
        while (!string.IsNullOrWhiteSpace(userInput = await iIoAdapter.ReadAsync()))
        {
            history.AddUserMessage(userInput);

            var streamingResponse =
                chatCompletionService.GetStreamingChatMessageContentsAsync(
                    history,
                    openAiPromptExecutionSettings,
                    kernel);

            var fullResponse = await iIoAdapter.WriteAsync(streamingResponse);
            history.AddAssistantMessage(fullResponse);
        }
    }
}