namespace NCcsds.Core.Identifiers;

/// <summary>
/// Represents a Master Channel Identifier, combining TFVN and SCID.
/// </summary>
public readonly record struct MasterChannelId : IComparable<MasterChannelId>
{
    /// <summary>
    /// The transfer frame version number.
    /// </summary>
    public TransferFrameVersionNumber Tfvn { get; }

    /// <summary>
    /// The spacecraft identifier.
    /// </summary>
    public SpacecraftId Scid { get; }

    /// <summary>
    /// Creates a new master channel ID.
    /// </summary>
    /// <param name="tfvn">The transfer frame version number.</param>
    /// <param name="scid">The spacecraft identifier.</param>
    public MasterChannelId(TransferFrameVersionNumber tfvn, SpacecraftId scid)
    {
        Tfvn = tfvn;
        Scid = scid;
    }

    /// <summary>
    /// Creates a new master channel ID.
    /// </summary>
    /// <param name="tfvn">The transfer frame version number (0-3).</param>
    /// <param name="scid">The spacecraft identifier (0-1023).</param>
    public MasterChannelId(byte tfvn, ushort scid)
        : this(new TransferFrameVersionNumber(tfvn), new SpacecraftId(scid))
    {
    }

    public int CompareTo(MasterChannelId other)
    {
        var tfvnCompare = Tfvn.CompareTo(other.Tfvn);
        return tfvnCompare != 0 ? tfvnCompare : Scid.CompareTo(other.Scid);
    }

    public override string ToString() => $"MCID:{Tfvn.Value}.{Scid.Value}";
}
