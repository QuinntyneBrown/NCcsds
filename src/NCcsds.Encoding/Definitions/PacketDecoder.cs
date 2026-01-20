using NCcsds.Core.Buffers;
using NCcsds.Encoding.Primitives;

namespace NCcsds.Encoding.Definitions;

/// <summary>
/// Decodes packets based on a packet definition.
/// </summary>
public class PacketDecoder
{
    private readonly PacketDefinition _definition;

    /// <summary>
    /// Creates a new packet decoder for the given definition.
    /// </summary>
    public PacketDecoder(PacketDefinition definition)
    {
        _definition = definition;
    }

    /// <summary>
    /// Decodes a packet and returns parameter values.
    /// </summary>
    public Dictionary<string, object> Decode(ReadOnlySpan<byte> data)
    {
        var result = new Dictionary<string, object>();
        var reader = new BitReader(data);

        foreach (var param in _definition.Parameters)
        {
            object value = DecodeParameter(ref reader, param);
            result[param.Name] = value;
        }

        return result;
    }

    private object DecodeParameter(ref BitReader reader, ParameterDefinition param)
    {
        return param.Type switch
        {
            ParameterType.UnsignedInteger => IntegerEncoder.DecodeUnsigned(ref reader, param.BitSize),
            ParameterType.SignedInteger => IntegerEncoder.DecodeSigned(ref reader, param.BitSize),
            ParameterType.Float => RealEncoder.DecodeSingle(ref reader),
            ParameterType.Double => RealEncoder.DecodeDouble(ref reader),
            ParameterType.Boolean => reader.ReadBit(),
            ParameterType.Enumeration => DecodeEnumeration(ref reader, param),
            _ => throw new NotSupportedException($"Parameter type {param.Type} not supported for bit-level decoding.")
        };
    }

    private object DecodeEnumeration(ref BitReader reader, ParameterDefinition param)
    {
        var numericValue = IntegerEncoder.DecodeUnsigned(ref reader, param.BitSize);

        if (param.EnumerationValues != null)
        {
            foreach (var kvp in param.EnumerationValues)
            {
                if ((ulong)kvp.Value == numericValue)
                    return kvp.Key;
            }
        }

        return numericValue;
    }
}

/// <summary>
/// Encodes packets based on a packet definition.
/// </summary>
public class PacketEncoder
{
    private readonly PacketDefinition _definition;

    /// <summary>
    /// Creates a new packet encoder for the given definition.
    /// </summary>
    public PacketEncoder(PacketDefinition definition)
    {
        _definition = definition;
    }

    /// <summary>
    /// Encodes parameter values to a packet.
    /// </summary>
    public byte[] Encode(Dictionary<string, object> values)
    {
        var buffer = new byte[_definition.TotalByteSize];
        var writer = new BitWriter(buffer);

        foreach (var param in _definition.Parameters)
        {
            if (!values.TryGetValue(param.Name, out var value))
            {
                // Use default or skip
                if (param.DefaultValue != null)
                    value = ParseDefaultValue(param);
                else
                    throw new ArgumentException($"Missing value for parameter {param.Name}");
            }

            EncodeParameter(ref writer, param, value);
        }

        return buffer;
    }

    private void EncodeParameter(ref BitWriter writer, ParameterDefinition param, object value)
    {
        switch (param.Type)
        {
            case ParameterType.UnsignedInteger:
                IntegerEncoder.EncodeUnsigned(ref writer, Convert.ToUInt64(value), param.BitSize);
                break;

            case ParameterType.SignedInteger:
                IntegerEncoder.EncodeSigned(ref writer, Convert.ToInt64(value), param.BitSize);
                break;

            case ParameterType.Float:
                RealEncoder.EncodeSingle(ref writer, Convert.ToSingle(value));
                break;

            case ParameterType.Double:
                RealEncoder.EncodeDouble(ref writer, Convert.ToDouble(value));
                break;

            case ParameterType.Boolean:
                writer.WriteBit(Convert.ToBoolean(value));
                break;

            case ParameterType.Enumeration:
                EncodeEnumeration(ref writer, param, value);
                break;

            default:
                throw new NotSupportedException($"Parameter type {param.Type} not supported for bit-level encoding.");
        }
    }

    private void EncodeEnumeration(ref BitWriter writer, ParameterDefinition param, object value)
    {
        long numericValue;

        if (value is string strValue && param.EnumerationValues != null)
        {
            if (!param.EnumerationValues.TryGetValue(strValue, out numericValue))
                throw new ArgumentException($"Unknown enumeration value: {strValue}");
        }
        else
        {
            numericValue = Convert.ToInt64(value);
        }

        IntegerEncoder.EncodeUnsigned(ref writer, (ulong)numericValue, param.BitSize);
    }

    private object ParseDefaultValue(ParameterDefinition param)
    {
        return param.Type switch
        {
            ParameterType.UnsignedInteger => ulong.Parse(param.DefaultValue!),
            ParameterType.SignedInteger => long.Parse(param.DefaultValue!),
            ParameterType.Float => float.Parse(param.DefaultValue!),
            ParameterType.Double => double.Parse(param.DefaultValue!),
            ParameterType.Boolean => bool.Parse(param.DefaultValue!),
            _ => param.DefaultValue!
        };
    }
}
