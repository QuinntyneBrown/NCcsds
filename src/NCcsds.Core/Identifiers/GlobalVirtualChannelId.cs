namespace NCcsds.Core.Identifiers;

/// <summary>
/// Represents a Global Virtual Channel Identifier (GVCID), combining MCID and VCID.
/// </summary>
public readonly record struct GlobalVirtualChannelId : IComparable<GlobalVirtualChannelId>
{
    /// <summary>
    /// The master channel identifier.
    /// </summary>
    public MasterChannelId Mcid { get; }

    /// <summary>
    /// The virtual channel identifier.
    /// </summary>
    public VirtualChannelId Vcid { get; }

    /// <summary>
    /// Gets the transfer frame version number.
    /// </summary>
    public TransferFrameVersionNumber Tfvn => Mcid.Tfvn;

    /// <summary>
    /// Gets the spacecraft identifier.
    /// </summary>
    public SpacecraftId Scid => Mcid.Scid;

    /// <summary>
    /// Creates a new global virtual channel ID.
    /// </summary>
    /// <param name="mcid">The master channel identifier.</param>
    /// <param name="vcid">The virtual channel identifier.</param>
    public GlobalVirtualChannelId(MasterChannelId mcid, VirtualChannelId vcid)
    {
        Mcid = mcid;
        Vcid = vcid;
    }

    /// <summary>
    /// Creates a new global virtual channel ID.
    /// </summary>
    /// <param name="tfvn">The transfer frame version number.</param>
    /// <param name="scid">The spacecraft identifier.</param>
    /// <param name="vcid">The virtual channel identifier.</param>
    public GlobalVirtualChannelId(byte tfvn, ushort scid, byte vcid)
        : this(new MasterChannelId(tfvn, scid), new VirtualChannelId(vcid))
    {
    }

    public int CompareTo(GlobalVirtualChannelId other)
    {
        var mcidCompare = Mcid.CompareTo(other.Mcid);
        return mcidCompare != 0 ? mcidCompare : Vcid.CompareTo(other.Vcid);
    }

    public override string ToString() => $"GVCID:{Tfvn.Value}.{Scid.Value}.{Vcid.Value}";
}
