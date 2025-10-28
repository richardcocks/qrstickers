namespace QRStickers.Models;

/// <summary>
/// Represents a single entry in the Vite build manifest
/// Used for resolving hashed filenames for cache-busting
/// </summary>
public class ViteManifestEntry
{
    /// <summary>
    /// The output file path (e.g., "assets/designer-B0Xn_wyy.js")
    /// </summary>
    public string File { get; set; } = string.Empty;

    /// <summary>
    /// The entry point name (e.g., "designer")
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The source file path (e.g., "src/pages/designer/designer.entry.ts")
    /// </summary>
    public string? Src { get; set; }

    /// <summary>
    /// Whether this is an entry point (vs a chunk)
    /// </summary>
    public bool IsEntry { get; set; }

    /// <summary>
    /// List of chunk imports (e.g., ["_units-DX2Z1J1D.js"])
    /// </summary>
    public List<string>? Imports { get; set; }
}
