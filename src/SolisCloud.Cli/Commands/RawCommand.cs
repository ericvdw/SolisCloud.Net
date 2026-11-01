// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Text;
using System.Text.Json;

namespace SolisCloud.Cli.Commands;

/// <summary>
/// <c>solis raw &lt;operation&gt;</c>: sends any operation with a hand-written body and prints the response.
/// </summary>
internal static class RawCommand
{
    public static async Task<int> RunAsync(
        CliContext context,
        string operation,
        string body,
        bool envelope,
        bool allowWrite,
        CancellationToken cancellationToken)
    {
        SolisEndpoint? endpoint = SolisEndpoints.Find(operation);
        if (endpoint is null)
        {
            await context.Error.WriteLineAsync("Unknown operation '" + operation + "'. Known operations:");
            foreach (SolisEndpoint known in SolisEndpoints.All)
            {
                await context.Error.WriteLineAsync("  " + known.Name + (known.ModifiesData ? "  (changes data)" : string.Empty));
            }

            return ExitCodes.UsageError;
        }

        if (!IsJsonObject(body))
        {
            await context.Error.WriteLineAsync("--body must be a JSON object, e.g. {\"sn\":\"ABC123\"}.");
            await context.Error.WriteLineAsync(
                "Windows PowerShell drops the inner double quotes: write them as \\\" ('{\\\"sn\\\":\\\"ABC123\\\"}'), or put the JSON in a file and use --body-file.");
            return ExitCodes.UsageError;
        }

        if (endpoint.ModifiesData && !allowWrite)
        {
            await context.Error.WriteLineAsync(endpoint.Name + " changes account data. Add --allow-write to send it.");
            return ExitCodes.UsageError;
        }

        if (!Credentials.TryLoad(context.GetEnvironmentVariable, out SolisCloudClientOptions? options, out string? error))
        {
            await context.Error.WriteLineAsync(error);
            return ExitCodes.UsageError;
        }

        options.AllowWriteOperations = allowWrite;
        using SolisCloudClient client = context.CreateClient(options);
        try
        {
            SolisCloudResponse response = await client.SendAsync(endpoint, Encoding.UTF8.GetBytes(body), cancellationToken);
            string text = envelope ? JsonText.Format(response.Json)
                : response.Data.ValueKind == JsonValueKind.Undefined ? "null"
                : JsonText.Format(response.Data);
            await context.Output.WriteLineAsync(text);
            return ExitCodes.Success;
        }
        catch (Exception ex) when (Failures.IsRequestFailure(ex, cancellationToken))
        {
            await context.Error.WriteLineAsync(Failures.Describe(ex));
            return ExitCodes.ApiError;
        }
    }

    /// <summary>
    /// Works out the request body from <c>--body</c> or <c>--body-file</c>, reporting a problem to the user.
    /// </summary>
    /// <returns>The body, <c>{}</c> if neither was given, or <see langword="null"/> after writing an error.</returns>
    public static async Task<string?> ReadBodyAsync(CliContext context, string? body, string? bodyFile, CancellationToken cancellationToken)
    {
        if (bodyFile is null)
        {
            return body ?? "{}";
        }

        if (body is not null)
        {
            await context.Error.WriteLineAsync("Use --body or --body-file, not both.");
            return null;
        }

        try
        {
            return await File.ReadAllTextAsync(bodyFile, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await context.Error.WriteLineAsync("Couldn't read --body-file: " + ex.Message);
            return null;
        }
    }

    private static bool IsJsonObject(string text)
    {
        try
        {
            using var document = JsonDocument.Parse(text);
            return document.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
