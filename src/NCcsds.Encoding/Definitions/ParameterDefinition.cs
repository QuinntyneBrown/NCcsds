namespace NCcsds.Encoding.Definitions;

/// <summary>
/// Defines a parameter within a packet structure.
/// </summary>
public class ParameterDefinition
{
    /// <summary>
    /// Parameter name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Parameter description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Parameter data type.
    /// </summary>
    public ParameterType Type { get; set; }

    /// <summary>
    /// Bit size for the parameter (for fixed-size types).
    /// </summary>
    public int BitSize { get; set; }

    /// <summary>
    /// Byte size (calculated from BitSize).
    /// </summary>
    public int ByteSize => (BitSize + 7) / 8;

    /// <summary>
    /// Bit offset within the containing structure.
    /// </summary>
    public int BitOffset { get; set; }

    /// <summary>
    /// Whether the parameter is signed (for integers).
    /// </summary>
    public bool IsSigned { get; set; }

    /// <summary>
    /// Enumeration values (for enumerated types).
    /// </summary>
    public Dictionary<string, long>? EnumerationValues { get; set; }

    /// <summary>
    /// Default value as a string.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Engineering unit.
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// Calibration/conversion to apply.
    /// </summary>
    public CalibrationDefinition? Calibration { get; set; }

    /// <summary>
    /// Validity condition (when this parameter is valid).
    /// </summary>
    public string? ValidityCondition { get; set; }
}

/// <summary>
/// Parameter data types.
/// </summary>
public enum ParameterType
{
    /// <summary>
    /// Unsigned integer.
    /// </summary>
    UnsignedInteger,

    /// <summary>
    /// Signed integer.
    /// </summary>
    SignedInteger,

    /// <summary>
    /// IEEE 754 single-precision float.
    /// </summary>
    Float,

    /// <summary>
    /// IEEE 754 double-precision float.
    /// </summary>
    Double,

    /// <summary>
    /// Enumerated value.
    /// </summary>
    Enumeration,

    /// <summary>
    /// Boolean (1 bit).
    /// </summary>
    Boolean,

    /// <summary>
    /// Fixed-length string.
    /// </summary>
    String,

    /// <summary>
    /// Variable-length string.
    /// </summary>
    VariableString,

    /// <summary>
    /// Fixed-length octet string.
    /// </summary>
    OctetString,

    /// <summary>
    /// Variable-length octet string.
    /// </summary>
    VariableOctetString,

    /// <summary>
    /// CUC time code.
    /// </summary>
    CucTime,

    /// <summary>
    /// CDS time code.
    /// </summary>
    CdsTime,

    /// <summary>
    /// Deduced parameter (calculated from others).
    /// </summary>
    Deduced
}

/// <summary>
/// Calibration/conversion definition.
/// </summary>
public class CalibrationDefinition
{
    /// <summary>
    /// Calibration type.
    /// </summary>
    public CalibrationType Type { get; set; }

    /// <summary>
    /// Polynomial coefficients (for polynomial calibration).
    /// </summary>
    public double[]? Coefficients { get; set; }

    /// <summary>
    /// Lookup table points (for interpolation).
    /// </summary>
    public List<(double Raw, double Calibrated)>? LookupTable { get; set; }
}

/// <summary>
/// Calibration types.
/// </summary>
public enum CalibrationType
{
    /// <summary>
    /// No calibration (identity).
    /// </summary>
    None,

    /// <summary>
    /// Polynomial calibration.
    /// </summary>
    Polynomial,

    /// <summary>
    /// Linear interpolation.
    /// </summary>
    Interpolation,

    /// <summary>
    /// Logarithmic.
    /// </summary>
    Logarithmic
}
