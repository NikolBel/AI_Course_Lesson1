using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SemanticKernelPlayground.IOAdapters;
using SemanticKernelPlayground.Plugins.GitPlugin;
using SemanticKernelPlayground.Services;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true)
    .Build();

var modelName = configuration["ModelName"] ?? throw new ApplicationException("ModelName not found");
var endpoint = configuration["Endpoint"] ?? throw new ApplicationException("Endpoint not found");
var apiKey = configuration["ApiKey"] ?? throw new ApplicationException("ApiKey not found");

var builder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(modelName, endpoint, apiKey);

builder.Services.AddLogging(configure => configure.AddConsole());
builder.Services.AddLogging(configure => configure.SetMinimumLevel(LogLevel.Information));

string pluginDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Plugins");
builder.Plugins.AddFromType<GitPlugin>();
builder.Plugins.AddFromPromptDirectory(pluginDirectory);

var kernel = builder.Build();

var services = new ServiceCollection();
services.AddSingleton(kernel);
services.AddSingleton<IIoAdapter, ConsoleAdapter>();
services.AddSingleton<ChatBot>();

var serviceProvider = services.BuildServiceProvider();

var chatBot = serviceProvider.GetRequiredService<ChatBot>();

Task.WaitAll(chatBot.RunAsync());