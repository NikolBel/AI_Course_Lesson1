using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Embeddings;
using SemanticKernelPlayground.Models;

namespace SemanticKernelPlayground.DataIngestion;

public class DataUploader
{
    private readonly IVectorStore _vectorStore;
    private readonly ITextEmbeddingGenerationService _embedGen;

    public DataUploader(
        IVectorStore vectorStore,
        ITextEmbeddingGenerationService embedGen)
    {
        _vectorStore = vectorStore;
        _embedGen = embedGen;
    }

    public async Task UploadToVectorStoreAsync(
        string collectionName,
        IEnumerable<TextChunk> chunks)
    {
        var coll = _vectorStore.GetCollection<string, TextChunk>(collectionName);
        await coll.CreateCollectionIfNotExistsAsync();

        foreach (var chunk in chunks)
        {
            Console.WriteLine($"Embedding chunk {chunk.Key}...");
            chunk.TextEmbedding = await _embedGen.GenerateEmbeddingAsync(chunk.Text);
            Console.WriteLine($"Upserting chunk {chunk.Key}...");
            await coll.UpsertAsync(chunk);
        }
    }
}