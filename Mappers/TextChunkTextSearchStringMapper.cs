using Microsoft.SemanticKernel.Data;
using SemanticKernelPlayground.Models;

namespace SemanticKernelPlayground.Mappers;

public class TextChunkTextSearchStringMapper : ITextSearchStringMapper
{
    public string MapFromResultToString(object result)
    {
        return result is TextChunk tc
            ? tc.Text
            : throw new ArgumentException("Unexpected result type");
    }
}