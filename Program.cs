using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Data;
using SemanticKernelPlayground.Services;
using SemanticKernelPlayground.IOAdapters;
using SemanticKernelPlayground.Plugins.GitPlugin;
using SemanticKernelPlayground.DataIngestion;
using SemanticKernelPlayground.Mappers;
using SemanticKernelPlayground.Models;

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true)
    .Build();

var modelName = configuration["ModelName"] ?? throw new ApplicationException("ModelName not found");
string embedModelName = configuration["EmbeddingModel"] ?? throw new ApplicationException("EmbeddingModel not found");
var endpoint = configuration["Endpoint"] ?? throw new ApplicationException("Endpoint not found");
var apiKey = configuration["ApiKey"] ?? throw new ApplicationException("ApiKey not found");

var builder = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(modelName, endpoint, apiKey)
    .AddAzureOpenAITextEmbeddingGeneration(embedModelName, endpoint, apiKey)
    .AddInMemoryVectorStore();

builder.Services.AddLogging(configure => configure.AddConsole());
builder.Services.AddLogging(configure => configure.SetMinimumLevel(LogLevel.Information));

// Registering the chat plugins
string pluginDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Plugins");
builder.Plugins.AddFromType<GitPlugin>();
builder.Plugins.AddFromPromptDirectory(pluginDirectory);

var kernel = builder.Build();

// Ingestion codebase to vector store
var vectorStore = kernel.GetRequiredService<IVectorStore>();
var textEmbeddingGenerator = kernel.GetRequiredService<ITextEmbeddingGenerationService>();

var assemblyDir = Path.GetDirectoryName(typeof(Program).Assembly.Location)!;
var projectRoot = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", ".."));
Console.WriteLine($"[Info] Project root: {projectRoot}");

var codeChunks = CodeDocumentReader.ReadCodeBase(
    projectRoot,
    linesPerChunk: 50);

var uploader = new DataUploader(vectorStore, textEmbeddingGenerator);
await uploader.UploadToVectorStoreAsync("codebase", codeChunks);

// Registering the CodeSearchPlugin
var collection = vectorStore.GetCollection<string, TextChunk>("codebase");
await collection.CreateCollectionIfNotExistsAsync();

var stringMapper = new TextChunkTextSearchStringMapper();
var resultMapper = new TextChunkTextSearchResultMapper();

var textSearch = new VectorStoreTextSearch<TextChunk>(
    collection,
    textEmbeddingGenerator,
    stringMapper,
    resultMapper);

var codeSearchPlugin = textSearch.CreateWithGetSearchResults("CodeSearchPlugin");
kernel.Plugins.Add(codeSearchPlugin);

Console.WriteLine("[Info] Loaded SK plugins:");
foreach (var plugin in kernel.Plugins)
{
    Console.WriteLine($" – {plugin.Name}");
}

// Running the chatbot
var services = new ServiceCollection();
services.AddSingleton(kernel);
services.AddSingleton<IIoAdapter, ConsoleAdapter>();
services.AddSingleton<ChatBot>();

var serviceProvider = services.BuildServiceProvider();

var chatBot = serviceProvider.GetRequiredService<ChatBot>();

await chatBot.RunAsync();