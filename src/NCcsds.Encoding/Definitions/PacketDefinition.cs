namespace NCcsds.Encoding.Definitions;

/// <summary>
/// Defines a packet structure for encoding/decoding.
/// </summary>
public class PacketDefinition
{
    /// <summary>
    /// Packet definition name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Packet description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// APID for this packet type (null if not APID-specific).
    /// </summary>
    public ushort? Apid { get; set; }

    /// <summary>
    /// Service type (for PUS packets).
    /// </summary>
    public byte? ServiceType { get; set; }

    /// <summary>
    /// Service subtype (for PUS packets).
    /// </summary>
    public byte? ServiceSubtype { get; set; }

    /// <summary>
    /// Parameters in this packet.
    /// </summary>
    public List<ParameterDefinition> Parameters { get; set; } = new();

    /// <summary>
    /// Gets the total bit size of all parameters.
    /// </summary>
    public int TotalBitSize => Parameters.Sum(p => p.BitSize);

    /// <summary>
    /// Gets the total byte size of all parameters.
    /// </summary>
    public int TotalByteSize => (TotalBitSize + 7) / 8;

    /// <summary>
    /// Gets a parameter by name.
    /// </summary>
    public ParameterDefinition? GetParameter(string name) =>
        Parameters.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Adds a parameter to the definition.
    /// </summary>
    public PacketDefinition AddParameter(ParameterDefinition parameter)
    {
        parameter.BitOffset = TotalBitSize;
        Parameters.Add(parameter);
        return this;
    }

    /// <summary>
    /// Adds an unsigned integer parameter.
    /// </summary>
    public PacketDefinition AddUnsignedInteger(string name, int bitSize, string? description = null)
    {
        return AddParameter(new ParameterDefinition
        {
            Name = name,
            Description = description,
            Type = ParameterType.UnsignedInteger,
            BitSize = bitSize,
            IsSigned = false
        });
    }

    /// <summary>
    /// Adds a signed integer parameter.
    /// </summary>
    public PacketDefinition AddSignedInteger(string name, int bitSize, string? description = null)
    {
        return AddParameter(new ParameterDefinition
        {
            Name = name,
            Description = description,
            Type = ParameterType.SignedInteger,
            BitSize = bitSize,
            IsSigned = true
        });
    }

    /// <summary>
    /// Adds a float parameter.
    /// </summary>
    public PacketDefinition AddFloat(string name, string? description = null)
    {
        return AddParameter(new ParameterDefinition
        {
            Name = name,
            Description = description,
            Type = ParameterType.Float,
            BitSize = 32
        });
    }

    /// <summary>
    /// Adds a double parameter.
    /// </summary>
    public PacketDefinition AddDouble(string name, string? description = null)
    {
        return AddParameter(new ParameterDefinition
        {
            Name = name,
            Description = description,
            Type = ParameterType.Double,
            BitSize = 64
        });
    }

    /// <summary>
    /// Adds a boolean parameter.
    /// </summary>
    public PacketDefinition AddBoolean(string name, string? description = null)
    {
        return AddParameter(new ParameterDefinition
        {
            Name = name,
            Description = description,
            Type = ParameterType.Boolean,
            BitSize = 1
        });
    }

    /// <summary>
    /// Adds an enumeration parameter.
    /// </summary>
    public PacketDefinition AddEnumeration(string name, int bitSize, Dictionary<string, long> values, string? description = null)
    {
        return AddParameter(new ParameterDefinition
        {
            Name = name,
            Description = description,
            Type = ParameterType.Enumeration,
            BitSize = bitSize,
            EnumerationValues = values
        });
    }
}
