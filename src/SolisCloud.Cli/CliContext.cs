// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

namespace SolisCloud.Cli;

/// <summary>
/// Everything a command needs from the outside world, so tests can replace each piece.
/// </summary>
internal sealed class CliContext
{
    public CliContext(
        TextWriter output,
        TextWriter error,
        Func<string, string?> getEnvironmentVariable,
        TimeProvider timeProvider,
        Func<SolisCloudClientOptions, SolisCloudClient> createClient)
    {
        Output = output;
        Error = error;
        GetEnvironmentVariable = getEnvironmentVariable;
        TimeProvider = timeProvider;
        CreateClient = createClient;
    }

    /// <summary>Gets the writer for normal output (results).</summary>
    public TextWriter Output { get; }

    /// <summary>Gets the writer for diagnostics and errors.</summary>
    public TextWriter Error { get; }

    /// <summary>Gets a function that reads an environment variable.</summary>
    public Func<string, string?> GetEnvironmentVariable { get; }

    /// <summary>Gets the clock.</summary>
    public TimeProvider TimeProvider { get; }

    /// <summary>Gets a factory for API clients.</summary>
    public Func<SolisCloudClientOptions, SolisCloudClient> CreateClient { get; }

    /// <summary>
    /// Creates the context for a real run: the console, the process environment and the system clock.
    /// </summary>
    public static CliContext CreateDefault() =>
        new(
            Console.Out,
            Console.Error,
            Environment.GetEnvironmentVariable,
            TimeProvider.System,
            static options => new SolisCloudClient(options));
}
