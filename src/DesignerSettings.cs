namespace QRStickers;

/// <summary>
/// Configuration settings for the sticker designer
/// </summary>
public class DesignerSettings
{
    public MarginSettings DefaultMargins { get; set; } = new();
}

/// <summary>
/// Canvas margin settings (in pixels)
/// </summary>
public class MarginSettings
{
    /// <summary>
    /// Top margin in pixels (default: 100)
    /// </summary>
    public int Top { get; set; } = 100;

    /// <summary>
    /// Left margin in pixels (default: 100)
    /// </summary>
    public int Left { get; set; } = 100;

    /// <summary>
    /// Bottom margin in pixels (default: 500)
    /// </summary>
    public int Bottom { get; set; } = 500;

    /// <summary>
    /// Right margin in pixels (default: 500)
    /// </summary>
    public int Right { get; set; } = 500;
}
