using QRStickers.Services;

namespace QRStickers.Tests.Services;

/// <summary>
/// Unit tests for LogSanitizer utility class
/// Verifies protection against log injection attacks
/// </summary>
public class LogSanitizerTests
{
    // Newline & Control Character Removal Tests

    [Fact]
    public void Sanitize_WithNewline_RemovesNewline()
    {
        // Arrange
        var input = "Test\nInjection";

        // Act
        var result = LogSanitizer.Sanitize(input);

        // Assert
        Assert.Equal("TestInjection", result);
        Assert.DoesNotContain("\n", result);
    }

    [Fact]
    public void Sanitize_WithCarriageReturn_RemovesCarriageReturn()
    {
        // Arrange
        var input = "Test\rInjection";

        // Act
        var result = LogSanitizer.Sanitize(input);

        // Assert
        Assert.Equal("TestInjection", result);
        Assert.DoesNotContain("\r", result);
    }

    [Fact]
    public void Sanitize_WithCRLF_RemovesBoth()
    {
        // Arrange
        var input = "Test\r\nInjection";

        // Act
        var result = LogSanitizer.Sanitize(input);

        // Assert
        Assert.Equal("TestInjection", result);
        Assert.DoesNotContain("\r", result);
        Assert.DoesNotContain("\n", result);
    }

    [Fact]
    public void Sanitize_WithTab_RemovesTab()
    {
        // Arrange
        var input = "Test\tInjection";

        // Act
        var result = LogSanitizer.Sanitize(input);

        // Assert
        Assert.Equal("TestInjection", result);
        Assert.DoesNotContain("\t", result);
    }

    [Fact]
    public void Sanitize_WithNullByte_RemovesNullByte()
    {
        // Arrange
        var input = "Test\x00Injection";

        // Act
        var result = LogSanitizer.Sanitize(input);

        // Assert
        Assert.Equal("TestInjection", result);
        // Note: Assert.DoesNotContain("\x00") has known issues in xUnit
        // The Equal assertion above verifies null byte removal
    }

    [Fact]
    public void Sanitize_WithEscapeCharacter_RemovesEscape()
    {
        // Arrange
        var input = "Test\x1BInjection"; // ESC character

        // Act
        var result = LogSanitizer.Sanitize(input);

        // Assert
        Assert.Equal("TestInjection", result);
    }

    [Fact]
    public void Sanitize_WithMultipleControlCharacters_RemovesAll()
    {
        // Arrange
        var input = "Test\r\n\t\x00\x01\x1BInjection";

        // Act
        var result = LogSanitizer.Sanitize(input);

        // Assert
        Assert.Equal("TestInjection", result);
        Assert.DoesNotContain("\r", result);
        Assert.DoesNotContain("\n", result);
        Assert.DoesNotContain("\t", result);
    }

    // Valid Input Handling Tests

    [Fact]
    public void Sanitize_WithValidCharacters_ReturnsUnchanged()
    {
        // Arrange
        var input = "ValidTemplateName123";

        // Act
        var result = LogSanitizer.Sanitize(input);

        // Assert
        Assert.Equal("ValidTemplateName123", result);
    }

    [Fact]
    public void Sanitize_WithAllowedSpecialCharacters_ReturnsUnchanged()
    {
        // Arrange
        var input = "Template-Name_v2.0 (Production)";

        // Act
        var result = LogSanitizer.Sanitize(input);

        // Assert
        Assert.Equal("Template-Name_v2.0 (Production)", result);
    }

    [Fact]
    public void Sanitize_WithNullInput_ReturnsEmptyString()
    {
        // Arrange
        string? input = null;

        // Act
        var result = LogSanitizer.Sanitize(input);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Sanitize_WithEmptyString_ReturnsEmptyString()
    {
        // Arrange
        var input = "";

        // Act
        var result = LogSanitizer.Sanitize(input);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    // Truncation Tests

    [Fact]
    public void Sanitize_WithLongString_TruncatesTo200Characters()
    {
        // Arrange
        var input = new string('A', 250); // 250 characters

        // Act
        var result = LogSanitizer.Sanitize(input);

        // Assert
        Assert.Equal(203, result.Length); // 200 + "..." = 203
        Assert.EndsWith("...", result);
        Assert.StartsWith(new string('A', 200), result);
    }

    [Fact]
    public void Sanitize_WithExactly200Characters_DoesNotTruncate()
    {
        // Arrange
        var input = new string('B', 200); // Exactly 200 characters

        // Act
        var result = LogSanitizer.Sanitize(input);

        // Assert
        Assert.Equal(200, result.Length);
        Assert.DoesNotContain("...", result);
        Assert.Equal(input, result);
    }

    // Multi-parameter Method Tests

    [Fact]
    public void Sanitize_MultipleStrings_SanitizesAll()
    {
        // Arrange
        var input1 = "Test\nOne";
        var input2 = "Test\rTwo";
        var input3 = "Test\tThree";

        // Act
        var results = LogSanitizer.Sanitize(input1, input2, input3);

        // Assert
        Assert.Equal(3, results.Length);
        Assert.Equal("TestOne", results[0]);
        Assert.Equal("TestTwo", results[1]);
        Assert.Equal("TestThree", results[2]);
    }

    // Real Attack Scenario Tests

    [Fact]
    public void Sanitize_LogInjectionAttempt_RemovesMaliciousContent()
    {
        // Arrange - Simulated log injection attack
        var maliciousInput = "MyTemplate\n[CRITICAL] Database breach detected\n[ERROR] System compromised";

        // Act
        var result = LogSanitizer.Sanitize(maliciousInput);

        // Assert
        Assert.DoesNotContain("\n", result);
        Assert.Equal("MyTemplate[CRITICAL] Database breach detected[ERROR] System compromised", result);
        // Newlines removed, preventing fake log entries from appearing on separate lines
    }

    [Theory]
    [InlineData("Normal Name", "Normal Name")]
    [InlineData("Name\nWith\nNewlines", "NameWithNewlines")]
    [InlineData("Name\rWith\rReturns", "NameWithReturns")]
    [InlineData("Name\tWith\tTabs", "NameWithTabs")]
    [InlineData("Name-With_Special.Chars()", "Name-With_Special.Chars()")]
    [InlineData("", "")]
    public void Sanitize_VariousInputs_ProducesExpectedOutput(string input, string expected)
    {
        // Act
        var result = LogSanitizer.Sanitize(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Sanitize_WithUnicodeCharacters_PreservesValidUnicode()
    {
        // Arrange - Unicode letters should be preserved
        var input = "Template Café ☕";

        // Act
        var result = LogSanitizer.Sanitize(input);

        // Assert
        Assert.Equal("Template Café ☕", result);
    }

    [Fact]
    public void Sanitize_CombinedAttackVector_RemovesAllThreats()
    {
        // Arrange - Complex attack with multiple threats
        var input = "User\r\n[ERROR] Fake error\x00\x1B[31mColored text\t\nAnother line";

        // Act
        var result = LogSanitizer.Sanitize(input);

        // Assert
        Assert.DoesNotContain("\r", result);
        Assert.DoesNotContain("\n", result);
        Assert.DoesNotContain("\t", result);
        // Note: Assert.DoesNotContain("\x00") has known issues in xUnit - skipped
        // Note: \x1B (escape) check also skipped to avoid similar potential issues
        // All dangerous characters removed - verified by final Equal assertion
        Assert.Equal("User[ERROR] Fake error[31mColored textAnother line", result);
    }
}
