namespace KeyBridge;

public static class FileHydration
{
    public static async Task RequestHydrationAsync(string path, CancellationToken cancellationToken)
    {
        using var stream = new FileStream(
            path,
            new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.ReadWrite,
                Options = FileOptions.Asynchronous,
            }
        );

        await stream.CopyToAsync(Stream.Null, cancellationToken);
    }
}
