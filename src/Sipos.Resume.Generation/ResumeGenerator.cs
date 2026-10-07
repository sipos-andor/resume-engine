using System.Globalization;
using Microsoft.Extensions.Logging;
using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Core.Content;
using Sipos.Resume.Core.Site;
using Sipos.Resume.Core.Validation;
using Sipos.Resume.Generation.Building;
using Sipos.Resume.Generation.Content;
using Sipos.Resume.Generation.Documents.Docx;
using Sipos.Resume.Generation.Documents.Json;
using Sipos.Resume.Generation.Documents.Markdown;
using Sipos.Resume.Generation.Documents.PlainText;
using Sipos.Resume.Generation.Output;
using Sipos.Resume.Generation.Verification;

namespace Sipos.Resume.Generation;

/// <summary>
/// The engine's entry point for a CV's build program: reads the command line and the environment, checks the content,
/// builds the site and verifies it.
/// </summary>
/// <example>
/// <code>
/// return await ResumeGenerator.Create(args)
///     .UseTheme(new OperandorTheme())
///     .UseWriter(new PdfDocumentWriter(new PdfWriterOptions(LicenseType.Community)))
///     .UseShareImages(new PdfShareImageWriter())
///     .RunAsync();
/// </code>
/// </example>
/// <remarks>
/// <para>
/// Decision: the e-mail address and the analytics token come from environment variables only
/// (<see cref="ContactEmailVariable"/>, <see cref="AnalyticsTokenVariable"/>), never from the command line or the
/// content, and are never written to the log.
/// Why: a command line is visible to every process and is echoed by CI logs; the content is public. The CI passes its
/// secrets as environment variables, which its log masks.
/// </para>
/// <para>
/// Decision: the Markdown, plain text, JSON Resume and DOCX writers are always registered; the PDF writer is added by
/// the program.
/// Why: the page's <c>index.md</c>, <c>resume.json</c> and the llms.txt files depend on the first three, and none of
/// them needs a licence beyond the engine's; QuestPDF's licence is the program's choice.
/// </para>
/// </remarks>
public sealed partial class ResumeGenerator
{
    /// <summary>The environment variable with the e-mail address the PDFs show as an image.</summary>
    public const string ContactEmailVariable = "RESUME_CONTACT_EMAIL";

    /// <summary>The environment variable with the Cloudflare Web Analytics token.</summary>
    public const string AnalyticsTokenVariable = "RESUME_ANALYTICS_TOKEN";

    /// <summary>The environment variable with a Unix time that stands for the present, as in reproducible builds.</summary>
    public const string SourceDateEpochVariable = "SOURCE_DATE_EPOCH";

    private const string Usage = """
        Usage: [--content <folder>] [--output <folder>] [--assets <folder>] [--today yyyy-MM-dd]
               [--clean] [--require-email] [--validate-only]

          --content        The folder with site.json and resume.{language}.json files (default: content).
          --output         The folder the site is written to (default: dist).
          --assets         The published wwwroot to copy static assets from (default: wwwroot next to the program).
          --today          The date that stands for the present (default: SOURCE_DATE_EPOCH, else today in UTC).
          --clean          Empty the output folder first.
          --require-email  Fail when RESUME_CONTACT_EMAIL is not set, as a deployment should.
          --validate-only  Check the content and stop.

        Environment: RESUME_CONTACT_EMAIL (shown only in the PDFs, as an image), RESUME_ANALYTICS_TOKEN.
        """;

    private readonly string[] _args;
    private readonly List<IDocumentWriter> _writers = [new MarkdownDocumentWriter(), new PlainTextDocumentWriter(), new JsonResumeDocumentWriter(), new DocxDocumentWriter()];
    private IResumeTheme? _theme;
    private IShareImageWriter? _shareImages;
    private ILoggerFactory? _loggerFactory;
    private Func<string, string?> _environment = Environment.GetEnvironmentVariable;

    private ResumeGenerator(string[] args) => _args = args;

    /// <summary>Starts a generator for a program's command line.</summary>
    /// <param name="args">The program's arguments.</param>
    public static ResumeGenerator Create(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        return new ResumeGenerator(args);
    }

    /// <summary>Sets the theme of the pages and the designed documents.</summary>
    /// <param name="theme">The theme.</param>
    public ResumeGenerator UseTheme(IResumeTheme theme)
    {
        _theme = theme ?? throw new ArgumentNullException(nameof(theme));
        return this;
    }

    /// <summary>Adds a document writer; a later writer of the same format and layout replaces an earlier one.</summary>
    /// <param name="writer">The writer, such as the PDF or DOCX writer.</param>
    public ResumeGenerator UseWriter(IDocumentWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        _writers.Insert(0, writer);
        return this;
    }

    /// <summary>Sets the writer of the pages' share images.</summary>
    /// <param name="writer">The writer.</param>
    public ResumeGenerator UseShareImages(IShareImageWriter writer)
    {
        _shareImages = writer ?? throw new ArgumentNullException(nameof(writer));
        return this;
    }

    /// <summary>Sets the logging; by default the build logs to the console.</summary>
    /// <param name="loggerFactory">The logger factory.</param>
    public ResumeGenerator UseLogging(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        return this;
    }

    /// <summary>Reads environment variables through a function instead of the process's environment, as tests do.</summary>
    /// <param name="environment">Returns a variable's value, or <see langword="null"/>.</param>
    public ResumeGenerator UseEnvironment(Func<string, string?> environment)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        return this;
    }

    /// <summary>Runs the build.</summary>
    /// <param name="cancellationToken">Stops the build.</param>
    /// <returns>The exit code: 0 when the site was built and verified, 1 when the content or the site has issues, 2 for a usage error.</returns>
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var ownsLogging = _loggerFactory is null;
        var loggerFactory = _loggerFactory ?? LoggerFactory.Create(builder => builder.AddSimpleConsole(console => console.SingleLine = true));
        try
        {
            var logger = loggerFactory.CreateLogger<ResumeGenerator>();
            if (!TryParse(out var options, out var error))
            {
                Log.Usage(logger, error, Usage);
                return 2;
            }

            if (_theme is null && !options.ValidateOnly)
            {
                Log.Usage(logger, "No theme: call UseTheme before RunAsync.", Usage);
                return 2;
            }

            if (!Directory.Exists(options.ContentFolder))
            {
                Log.Usage(logger, $"The content folder {Path.GetFullPath(options.ContentFolder)} does not exist.", Usage);
                return 2;
            }

            if (!options.ValidateOnly && OutputConflict(options) is { } conflict)
            {
                Log.Usage(logger, conflict, Usage);
                return 2;
            }

            try
            {
                return await BuildAsync(options, logger, loggerFactory, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                // A disk error or an artifact written twice: the exit code scripts expect, with the reason.
                Log.Crashed(logger, EmailGuard.Redact(exception.Message));
                return 1;
            }
        }
        finally
        {
            if (ownsLogging)
            {
                loggerFactory.Dispose();
            }
        }
    }

    private async Task<int> BuildAsync(BuildOptions options, ILogger logger, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        var files = await new FolderSource(options.ContentFolder).ReadAsync(cancellationToken).ConfigureAwait(false);
        var loaded = ContentLoader.Load(files, options.AnalyticsToken);
        foreach (var issue in loaded.Issues)
        {
            // An issue may quote a value, such as a parity difference; an address in it must not reach the log.
            Log.ContentIssue(logger, EmailGuard.Redact(issue.ToString()));
        }

        if (loaded.Set is not { } set)
        {
            Log.Failed(logger, loaded.Issues.Count);
            return 1;
        }

        Log.Loaded(logger, string.Join(", ", set.Languages.Select(language => language.Tag)), options.Today);
        if (options.ValidateOnly)
        {
            return 0;
        }

        if (options.ContactEmail is null)
        {
            if (options.RequireContactEmail)
            {
                Log.MissingEmail(logger, ContactEmailVariable);
                return 1;
            }

            Log.NoEmail(logger, ContactEmailVariable);
        }

        if (!PrepareOutput(options, logger))
        {
            return 1;
        }

        var sink = new FolderSink(options.OutputFolder);
        var builder = new SiteBuilder(_theme!, _writers, _shareImages, options, loggerFactory);
        var pages = await builder.BuildAsync(set, sink, cancellationToken).ConfigureAwait(false);
        var problems = OutputVerifier.Verify(sink.Root, pages, _theme!.RequiredAssets, options.ContactEmail);
        foreach (var problem in problems)
        {
            Log.OutputIssue(logger, EmailGuard.Redact(problem.ToString()));
        }

        if (problems.Count > 0)
        {
            Log.Failed(logger, problems.Count);
            return 1;
        }

        Log.Built(logger, sink.Written.Count, sink.Root);
        return 0;
    }

    private bool TryParse(out BuildOptions options, out string error)
    {
        string content = "content", output = "dist";
        string? assets = null, today = null;
        bool clean = false, requireEmail = false, validateOnly = false;
        options = null!;
        for (var index = 0; index < _args.Length; index++)
        {
            var name = _args[index];
            string? Value() => index + 1 < _args.Length ? _args[++index] : null;
            switch (name)
            {
                case "--content":
                    content = Value() ?? "";
                    break;
                case "--output":
                    output = Value() ?? "";
                    break;
                case "--assets":
                    assets = Value() ?? "";
                    break;
                case "--today":
                    today = Value() ?? "";
                    break;
                case "--clean":
                    clean = true;
                    break;
                case "--require-email":
                    requireEmail = true;
                    break;
                case "--validate-only":
                    validateOnly = true;
                    break;
                default:
                    error = $"Unknown argument '{name}'.";
                    return false;
            }
        }

        if (content.Length == 0 || output.Length == 0 || assets is { Length: 0 })
        {
            error = "A folder argument needs a value.";
            return false;
        }

        if (!TryToday(today, out var date))
        {
            error = $"--today must be a date in the form yyyy-MM-dd, and {SourceDateEpochVariable} a Unix time.";
            return false;
        }

        var email = _environment(ContactEmailVariable);
        options = new BuildOptions
        {
            ContentFolder = content,
            OutputFolder = output,
            AssetsFolder = assets,
            Today = date,
            ContactEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            AnalyticsToken = _environment(AnalyticsTokenVariable),
            Clean = clean,
            RequireContactEmail = requireEmail,
            ValidateOnly = validateOnly,
        };
        error = "";
        return true;
    }

    private bool TryToday(string? argument, out DateOnly today)
    {
        if (argument is not null)
        {
            return DateOnly.TryParseExact(argument, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out today);
        }

        if (_environment(SourceDateEpochVariable) is { Length: > 0 } epoch)
        {
            today = default;
            if (!long.TryParse(epoch, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) || seconds > MaxUnixSeconds)
            {
                return false;
            }

            today = DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime);
            return true;
        }

        today = DateOnly.FromDateTime(DateTime.UtcNow);
        return true;
    }

    // The last second DateTimeOffset holds: 9999-12-31T23:59:59Z.
    private const long MaxUnixSeconds = 253_402_300_799;

    // Decision: the output may not be, or hold, the content, the assets, the program, the working folder or the home
    // folder, and may not be a drive's root.
    // Why: --clean empties the output; a mistyped --output such as "." or the content folder would delete the CV's
    // sources, the repository or the running program.
    private static string? OutputConflict(BuildOptions options)
    {
        var output = Full(options.OutputFolder);
        if (Path.GetPathRoot(output) is { } root && Same(Full(root), output))
        {
            return $"--output {output} is the root of a drive; name a folder for the site.";
        }

        (string Path, string Name)[] protectedFolders =
        [
            (Full(options.ContentFolder), "the content folder"),
            (Full(options.AssetsRoot), "the assets folder"),
            (Full(AppContext.BaseDirectory), "the program's folder"),
            (Full(Environment.CurrentDirectory), "the working folder"),
            (Full(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)), "the home folder"),
        ];
        foreach (var (path, name) in protectedFolders)
        {
            if (path.Length > 0 && Contains(output, path))
            {
                return $"--output {output} is or holds {name} ({path}); name a folder of its own for the site.";
            }
        }

        return null;

        static string Full(string path) => path.Length == 0 ? "" : Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        static bool Same(string a, string b) => string.Equals(a, b, PathComparison);
        static bool Contains(string outer, string inner) =>
            Same(outer, inner) || inner.StartsWith(Path.TrimEndingDirectorySeparator(outer) + Path.DirectorySeparatorChar, PathComparison);
    }

    // Windows and macOS file systems ignore case by default.
    private static StringComparison PathComparison =>
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    // Decision: a folder that holds files is emptied only with --clean.
    // Why: a mistyped --output must not delete someone's folder.
    private static bool PrepareOutput(BuildOptions options, ILogger logger)
    {
        var folder = Path.GetFullPath(options.OutputFolder);
        if (Directory.Exists(folder) && Directory.EnumerateFileSystemEntries(folder).Any())
        {
            if (!options.Clean)
            {
                Log.OutputNotEmpty(logger, folder);
                return false;
            }

            Directory.Delete(folder, recursive: true);
        }

        Directory.CreateDirectory(folder);
        return true;
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Error, Message = "{Error}\n{Usage}")]
        public static partial void Usage(ILogger logger, string error, string usage);

        [LoggerMessage(Level = LogLevel.Error, Message = "{Issue}")]
        public static partial void ContentIssue(ILogger logger, string issue);

        [LoggerMessage(Level = LogLevel.Error, Message = "{Issue}")]
        public static partial void OutputIssue(ILogger logger, string issue);

        [LoggerMessage(Level = LogLevel.Error, Message = "The build failed with {Count} issue(s).")]
        public static partial void Failed(ILogger logger, int count);

        [LoggerMessage(Level = LogLevel.Information, Message = "Read the CV in {Languages}; the present is {Today}.")]
        public static partial void Loaded(ILogger logger, string languages, DateOnly today);

        [LoggerMessage(Level = LogLevel.Error, Message = "{Variable} is not set, and --require-email asks for the e-mail image in the PDFs.")]
        public static partial void MissingEmail(ILogger logger, string variable);

        [LoggerMessage(Level = LogLevel.Warning, Message = "{Variable} is not set; the PDFs show no e-mail address.")]
        public static partial void NoEmail(ILogger logger, string variable);

        [LoggerMessage(Level = LogLevel.Error, Message = "The output folder {Folder} is not empty; pass --clean to empty it first.")]
        public static partial void OutputNotEmpty(ILogger logger, string folder);

        [LoggerMessage(Level = LogLevel.Error, Message = "The build stopped: {Reason}")]
        public static partial void Crashed(ILogger logger, string reason);

        [LoggerMessage(Level = LogLevel.Information, Message = "Built and verified {Count} files in {Folder}.")]
        public static partial void Built(ILogger logger, int count, string folder);
    }
}
