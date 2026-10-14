// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

namespace SolisCloud.Cli.Tests.TestSupport;

/// <summary>
/// A fresh directory under the system temp folder, deleted with its contents on dispose.
/// </summary>
internal sealed class TemporaryDirectory : IDisposable
{
    public TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "solis-tests-" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
