using NCcsds.Core.Interfaces;

namespace NCcsds.Core.Configuration;

/// <summary>
/// Validates TM frame configuration.
/// </summary>
public class TmFrameConfigurationValidator : IValidator<TmFrameConfiguration>
{
    /// <summary>
    /// Validates the TM frame configuration.
    /// </summary>
    public ValidationResult Validate(TmFrameConfiguration config)
    {
        var errors = new List<string>();

        if (config.SpacecraftId > 1023)
            errors.Add($"SpacecraftId must be between 0 and 1023, got {config.SpacecraftId}");

        if (config.FrameLength < 7)
            errors.Add($"FrameLength must be at least 7 bytes (header), got {config.FrameLength}");

        if (config.FrameLength > 2048)
            errors.Add($"FrameLength exceeds maximum of 2048 bytes, got {config.FrameLength}");

        if (config.SecondaryHeaderLength < 0)
            errors.Add("SecondaryHeaderLength cannot be negative");

        foreach (var vc in config.VirtualChannels)
        {
            if (vc.VirtualChannelId > 63)
                errors.Add($"VirtualChannelId must be between 0 and 63, got {vc.VirtualChannelId}");
        }

        var vcIds = config.VirtualChannels.Select(vc => vc.VirtualChannelId).ToList();
        if (vcIds.Count != vcIds.Distinct().Count())
            errors.Add("Duplicate VirtualChannelId found in configuration");

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}

/// <summary>
/// Validates TC frame configuration.
/// </summary>
public class TcFrameConfigurationValidator : IValidator<TcFrameConfiguration>
{
    /// <summary>
    /// Validates the TC frame configuration.
    /// </summary>
    public ValidationResult Validate(TcFrameConfiguration config)
    {
        var errors = new List<string>();

        if (config.SpacecraftId > 1023)
            errors.Add($"SpacecraftId must be between 0 and 1023, got {config.SpacecraftId}");

        if (config.MaxFrameLength < 5)
            errors.Add($"MaxFrameLength must be at least 5 bytes (header), got {config.MaxFrameLength}");

        if (config.MaxFrameLength > 1024)
            errors.Add($"MaxFrameLength exceeds maximum of 1024 bytes, got {config.MaxFrameLength}");

        foreach (var vc in config.VirtualChannels)
        {
            if (vc.VirtualChannelId > 7)
                errors.Add($"TC VirtualChannelId must be between 0 and 7, got {vc.VirtualChannelId}");
        }

        var vcIds = config.VirtualChannels.Select(vc => vc.VirtualChannelId).ToList();
        if (vcIds.Count != vcIds.Distinct().Count())
            errors.Add("Duplicate VirtualChannelId found in configuration");

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}

/// <summary>
/// Validates AOS frame configuration.
/// </summary>
public class AosFrameConfigurationValidator : IValidator<AosFrameConfiguration>
{
    /// <summary>
    /// Validates the AOS frame configuration.
    /// </summary>
    public ValidationResult Validate(AosFrameConfiguration config)
    {
        var errors = new List<string>();

        if (config.SpacecraftId > 255)
            errors.Add($"AOS SpacecraftId must be between 0 and 255, got {config.SpacecraftId}");

        if (config.FrameLength < 8)
            errors.Add($"FrameLength must be at least 8 bytes (header), got {config.FrameLength}");

        if (config.FrameLength > 2048)
            errors.Add($"FrameLength exceeds maximum of 2048 bytes, got {config.FrameLength}");

        if (config.InsertZoneLength < 0)
            errors.Add("InsertZoneLength cannot be negative");

        foreach (var vc in config.VirtualChannels)
        {
            if (vc.VirtualChannelId > 63)
                errors.Add($"VirtualChannelId must be between 0 and 63, got {vc.VirtualChannelId}");
        }

        var vcIds = config.VirtualChannels.Select(vc => vc.VirtualChannelId).ToList();
        if (vcIds.Count != vcIds.Distinct().Count())
            errors.Add("Duplicate VirtualChannelId found in configuration");

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
