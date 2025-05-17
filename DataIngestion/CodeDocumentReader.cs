using SemanticKernelPlayground.Models;

namespace SemanticKernelPlayground.DataIngestion;

public static class CodeDocumentReader
{
    public static IEnumerable<TextChunk> ReadCodeBase(string rootDirectory, int linesPerChunk = 50)
    {
        var csFiles = Directory.GetFiles(rootDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(f =>
                !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) &&
                !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
            .ToArray();

        foreach (var file in csFiles)
        {
            var allLines = File.ReadAllLines(file);
            var docName = Path.GetRelativePath(rootDirectory, file);
            int chunkId = 0;

            for (int i = 0; i < allLines.Length; i += linesPerChunk)
            {
                var slice = allLines.Skip(i).Take(linesPerChunk);
                var text = string.Join(Environment.NewLine, slice).Trim();
                if (string.IsNullOrEmpty(text)) continue;

                yield return new TextChunk
                {
                    Key = $"{docName.Replace(Path.DirectorySeparatorChar, '_')}_chunk{chunkId}",
                    DocumentName = docName,
                    ParagraphId = ++chunkId,
                    Text = text,
                    TextEmbedding = ReadOnlyMemory<float>.Empty
                };
            }
        }
    }
}