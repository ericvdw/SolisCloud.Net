// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.CommandLine;

namespace SolisCloud.Cli;

/// <summary>
/// The <c>solis</c> entry point.
/// </summary>
internal static class Program
{
    public static Task<int> Main(string[] args)
    {
        RootCommand root = CliCommands.Create(CliContext.CreateDefault());
        return root.Parse(args).InvokeAsync();
    }
}
