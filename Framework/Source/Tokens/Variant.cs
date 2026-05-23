namespace Cosmere.Lightweave.Tokens;

public readonly struct Variant : System.IEquatable<Variant> {
    public string Id { get; }

    public Variant(string id) {
        Id = id ?? throw new System.ArgumentNullException(nameof(id));
    }

    public static readonly Variant Primary = new Variant("primary");
    public static readonly Variant Secondary = new Variant("secondary");
    public static readonly Variant Ghost = new Variant("ghost");
    public static readonly Variant Danger = new Variant("danger");
    public static readonly Variant Frosted = new Variant("frosted");

    public bool Equals(Variant other) {
        return string.Equals(Id, other.Id, System.StringComparison.Ordinal);
    }

    public override bool Equals(object? obj) {
        return obj is Variant other && Equals(other);
    }

    public override int GetHashCode() {
        return Id?.GetHashCode() ?? 0;
    }

    public static bool operator ==(Variant a, Variant b) => a.Equals(b);
    public static bool operator !=(Variant a, Variant b) => !a.Equals(b);

    public override string ToString() => Id ?? string.Empty;
}
