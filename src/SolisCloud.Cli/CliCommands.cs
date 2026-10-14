// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.CommandLine;
using System.Globalization;
using SolisCloud.Cli.Commands;

namespace SolisCloud.Cli;

/// <summary>
/// Defines the command line. Parsing, help text and Ctrl+C handling come from System.CommandLine; the work itself
/// is in the classes under <c>Commands/</c>, which don't depend on it and are tested directly.
/// </summary>
internal static class CliCommands
{
    public static RootCommand Create(CliContext context)
    {
        RootCommand root = new(
            "Unofficial command-line tool for the SolisCloud platform API.\n"
            + "Credentials are read from the " + Credentials.ApiIdVariable + " and " + Credentials.ApiSecretVariable
            + " environment variables; " + Credentials.BaseAddressVariable + " optionally overrides the server.");

        root.Subcommands.Add(CreateWhoAmI(context));
        root.Subcommands.Add(CreateRaw(context));
        root.Subcommands.Add(CreateCapture(context));
        return root;
    }

    private static Command CreateWhoAmI(CliContext context)
    {
        Command command = new("whoami", "Check the credentials and the clock, and list the stations the account can see.");
        command.SetAction((_, cancellationToken) => WhoAmICommand.RunAsync(context, cancellationToken));
        return command;
    }

    private static Command CreateRaw(CliContext context)
    {
        Argument<string> operation = new("operation")
        {
            Description = "The operation's name or path, e.g. inverterDetail or /v1/api/inverterDetail.",
        };
        Option<string> body = new("--body", "-b")
        {
            Description = "The JSON request body.",
            DefaultValueFactory = _ => "{}",
        };
        Option<bool> envelope = new("--envelope")
        {
            Description = "Print the whole response (success, code, msg, data) instead of just data.",
        };
        Option<bool> allowWrite = new("--allow-write")
        {
            Description = "Allow operations that change account data (station administration).",
        };

        Command command = new("raw", "Send any operation with a hand-written body and print the response.")
        {
            operation, body, envelope, allowWrite,
        };
        command.SetAction((parseResult, cancellationToken) => RawCommand.RunAsync(
            context,
            parseResult.GetValue(operation) ?? string.Empty,
            parseResult.GetValue(body) ?? "{}",
            parseResult.GetValue(envelope),
            parseResult.GetValue(allowWrite),
            cancellationToken));
        return command;
    }

    private static Command CreateCapture(CliContext context)
    {
        Option<string> output = new("--output", "-o")
        {
            Description = "The folder to write to; each capture goes in a new timestamped subfolder.",
            DefaultValueFactory = _ => "captures",
        };
        Option<string> date = new("--date")
        {
            Description = "The day to request daily data for (yyyy-MM-dd). Defaults to today in each station's time zone.",
        };
        Option<bool> noSanitize = new("--no-sanitize")
        {
            Description = "Keep real IDs, serial numbers, names and location. For your own use only; never share the result.",
        };

        Command command = new("capture", "Save every read operation's request and response for this account, sanitised for sharing.")
        {
            output, date, noSanitize,
        };
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            string? dateText = parseResult.GetValue(date);
            DateOnly? day = null;
            if (dateText is not null)
            {
                if (!DateOnly.TryParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsed))
                {
                    await context.Error.WriteLineAsync("--date must be in the form yyyy-MM-dd, e.g. 2026-10-14.");
                    return ExitCodes.UsageError;
                }

                day = parsed;
            }

            return await CaptureCommand.RunAsync(
                context,
                parseResult.GetValue(output) ?? "captures",
                day,
                sanitize: !parseResult.GetValue(noSanitize),
                cancellationToken);
        });
        return command;
    }
}
