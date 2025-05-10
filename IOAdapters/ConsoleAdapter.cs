using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelPlayground.IOAdapters;

public class ConsoleAdapter : IIoAdapter
{
    public async Task<string> ReadAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("Me > ");
        Console.ResetColor();

        return Console.ReadLine() ?? string.Empty;
    }

    public async Task WriteAsync(string? text)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("Assistant > ");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write(text);
        Console.ResetColor();

        Console.WriteLine();

        await Task.CompletedTask;
    }

    public async Task<string> WriteAsync(IAsyncEnumerable<StreamingChatMessageContent> streamingResponse)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("Assistant > ");
        Console.ResetColor();

        var fullResponse = "";
        await foreach (var chunk in streamingResponse)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(chunk.Content);
            Console.ResetColor();
            fullResponse += chunk.Content;
        }
        Console.WriteLine();

        return fullResponse;
    }
}