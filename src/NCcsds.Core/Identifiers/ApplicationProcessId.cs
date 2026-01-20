namespace NCcsds.Core.Identifiers;

/// <summary>
/// Represents a CCSDS Application Process Identifier (APID).
/// Valid range is 0-2047 (11 bits).
/// </summary>
public readonly record struct ApplicationProcessId : IComparable<ApplicationProcessId>
{
    /// <summary>
    /// Maximum valid APID value (11 bits).
    /// </summary>
    public const ushort MaxValue = 0x7FF;

    /// <summary>
    /// Idle packet APID (all ones).
    /// </summary>
    public static readonly ApplicationProcessId Idle = new(MaxValue);

    /// <summary>
    /// The raw APID value.
    /// </summary>
    public ushort Value { get; }

    /// <summary>
    /// Creates a new application process ID.
    /// </summary>
    /// <param name="value">The APID value (0-2047).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value exceeds 2047.</exception>
    public ApplicationProcessId(ushort value)
    {
        if (value > MaxValue)
            throw new ArgumentOutOfRangeException(nameof(value), value, $"APID must be between 0 and {MaxValue}.");
        Value = value;
    }

    /// <summary>
    /// Gets whether this is an idle packet APID.
    /// </summary>
    public bool IsIdle => Value == MaxValue;

    /// <summary>
    /// Tries to create an APID from the given value.
    /// </summary>
    public static bool TryCreate(ushort value, out ApplicationProcessId apid)
    {
        if (value > MaxValue)
        {
            apid = default;
            return false;
        }
        apid = new ApplicationProcessId(value);
        return true;
    }

    public int CompareTo(ApplicationProcessId other) => Value.CompareTo(other.Value);

    public override string ToString() => IsIdle ? "APID:IDLE" : $"APID:{Value}";

    public static implicit operator ushort(ApplicationProcessId apid) => apid.Value;
    public static explicit operator ApplicationProcessId(ushort value) => new(value);
}
