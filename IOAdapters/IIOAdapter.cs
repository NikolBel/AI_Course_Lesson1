using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelPlayground.IOAdapters;

public interface IIoAdapter
{
    Task<string> ReadAsync();
    Task WriteAsync(string text);
    Task<string> WriteAsync(IAsyncEnumerable<StreamingChatMessageContent> streamingResponse);
}