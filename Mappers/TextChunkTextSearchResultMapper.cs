using Microsoft.SemanticKernel.Data;
using SemanticKernelPlayground.Models;

namespace SemanticKernelPlayground.Mappers;

public class TextChunkTextSearchResultMapper : ITextSearchResultMapper
{
    public TextSearchResult MapFromResultToTextSearchResult(object result)
    {
        if (result is TextChunk tc)
        {
            return new TextSearchResult(value: tc.Text)
            {
                Name = tc.Key,
                Link = tc.DocumentName
            };
        }
        throw new ArgumentException("Unexpected result type");
    }
}