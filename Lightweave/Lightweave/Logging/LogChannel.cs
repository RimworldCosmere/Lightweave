namespace Cosmere.Lightweave.Logging;

internal readonly struct LogChannel {
    public readonly string Id;
    public readonly string Name;
    public readonly int Count;
    public readonly int Depth;
    public readonly bool HasError;

    public LogChannel(string id, string name, int count, int depth, bool hasError) {
        Id = id;
        Name = name;
        Count = count;
        Depth = depth;
        HasError = hasError;
    }
}
