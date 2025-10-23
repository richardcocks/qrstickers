using System.ComponentModel.DataAnnotations;

namespace QRStickers;

/// <summary>
/// Represents a custom global variable for a connection.
/// Variables can be referenced in templates using syntax: {{global.variableName}}
/// </summary>
public class GlobalVariable
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to Connection
    /// </summary>
    [Required]
    public int ConnectionId { get; set; }

    /// <summary>
    /// Variable name (e.g., "supportUrl", "companyPhone", "customMessage")
    /// Used in template with syntax: {{global.supportUrl}}
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string VariableName { get; set; } = null!;

    /// <summary>
    /// Variable value (e.g., "https://support.company.com", "+1-555-0123")
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string VariableValue { get; set; } = null!;

    /// <summary>
    /// Optional description to help users understand the variable's purpose
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Variable creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last modification timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Connection Connection { get; set; } = null!;
}
