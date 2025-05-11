using System.ComponentModel;
using System.Text;
using Microsoft.SemanticKernel;
using LibGit2Sharp;
using Version = System.Version;

namespace SemanticKernelPlayground.Plugins.GitPlugin;

/// <summary>
/// Kernel plugin exposing Git helper functions.
/// </summary>
public sealed class GitPlugin
{
    private string? _repoPath;

    [KernelFunction, Description("Absolute path to the git repository.")]
    public string SetRepository(
        [Description("Absolute path to git repository.")] string repoPath)
    {
        if (!IsValidRepo(repoPath))
            return $"❌ {repoPath} is not a git repository.";

        _repoPath = repoPath;
        SaveLastRepo(repoPath);
        return $"✅ Repository set to “{repoPath}”.";
    }

    [KernelFunction, Description("Get the latest commits from the currently set git repository")]
    public string GetCommits(
        [Description("Number of commits to retrieve.")] int nOfCommits = 5)
    {
        var repoPath = ResolveRepoPath() ??
                       "⚠️ No repository defined. Please run **SetRepository** first.";
        if (repoPath.StartsWith('⚠')) return repoPath;

        using var repo = new Repository(repoPath);
        var commits = repo.Commits.Take(nOfCommits);

        var sb = new StringBuilder();
        foreach (var c in commits)
            sb.AppendLine($"- {c.Committer.When:yyyy-MM-dd}: {c.MessageShort} (#{c.Sha[..7]})");

        return sb.ToString();
    }

    [KernelFunction, Description("(Optional) Bump SemVer, tag repo and persist")]
    public string BumpVersion(
        [Description("major / minor / patch")] string level = "patch")
    {
        var repoPath = ResolveRepoPath() ??
                       "No repo. Use SetRepository first.";
        if (repoPath.StartsWith("No repo")) return repoPath;

        var state = LoadState();
        var current = Version.Parse(state.LatestVersion ?? "0.0.0");
        var next = level switch
        {
            "major" => new Version(current.Major + 1, 0, 0),
            "minor" => new Version(current.Major, current.Minor + 1, 0),
            _ => new Version(current.Major, current.Minor, current.Build + 1)
        };

        using var repo = new Repository(repoPath);
        var tagName = $"v{next}";

        if (repo.Tags[tagName] is not null)
            return $"⚠️ Tag {tagName} already exists.";

        var sig = new Signature("release-bot", "bot@example.com", DateTimeOffset.Now);
        repo.ApplyTag(tagName, sig, "Automated release tag");

        // persist
        state.LatestVersion = next.ToString();
        WriteState(state);

        return $"🎉 Tagged repository with {tagName}";
    }

    // ––––––––– helpers –––––––––
    private static readonly string StateFilePath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                     ".sk-release-bot", "state.json");

    private string? ResolveRepoPath()
    {
        // 1) локальная переменная (последний SetRepository)
        if (!string.IsNullOrWhiteSpace(_repoPath)) return _repoPath;

        // 2) state.json
        var last = LoadState().LastRepo;
        if (!string.IsNullOrWhiteSpace(last) && IsValidRepo(last))
        {
            _repoPath = last;         // кэшируем для будущих вызовов
            return last;
        }

        return null;
    }

    private static bool IsValidRepo(string path) =>
        Directory.Exists(path) && Repository.IsValid(path);

    /* ─── state helpers ─── */

    private static void SaveLastRepo(string path)
    {
        var s = LoadState();
        s.LastRepo = path;
        WriteState(s);
    }

    private static BotState LoadState()
    {
        if (!File.Exists(StateFilePath)) return new();

        try
        {
            var json = File.ReadAllText(StateFilePath);
            return System.Text.Json.JsonSerializer.Deserialize<BotState>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    private static void WriteState(BotState state)
    {
        var dir = Path.GetDirectoryName(StateFilePath)!;
        Directory.CreateDirectory(dir);

        var json = System.Text.Json.JsonSerializer.Serialize(
            state,
            new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

        File.WriteAllText(StateFilePath, json);
    }

    private sealed record BotState
    {
        public string? LastRepo { get; set; }
        public string? LatestVersion { get; set; }
    }
}