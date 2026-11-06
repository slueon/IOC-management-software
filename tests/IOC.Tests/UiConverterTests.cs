#nullable disable
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using IOC.App.Converters;
using Xunit;

namespace IOC.Tests;

public class UiConverterTests
{
    [Theory]
    [InlineData(true, "#ECFDF5")]
    [InlineData(false, "#FEE2E2")]
    public void AckStatusToBackgroundBrushConverter_ReturnsExpectedBrush(bool isAck, string expectedHex)
    {
        var converter = new AcknowledgementStatusToBackgroundBrushConverter();
        var result = converter.Convert(isAck, typeof(Brush), string.Empty, CultureInfo.InvariantCulture) as SolidColorBrush;

        Assert.NotNull(result);
        var expectedColor = (Color)ColorConverter.ConvertFromString(expectedHex);
        Assert.Equal(expectedColor, result.Color);
    }

    [Theory]
    [InlineData(true, "#10B981")]
    [InlineData(false, "#EF4444")]
    public void AckStatusToBorderBrushConverter_ReturnsExpectedBrush(bool isAck, string expectedHex)
    {
        var converter = new AcknowledgementStatusToBorderBrushConverter();
        var result = converter.Convert(isAck, typeof(Brush), string.Empty, CultureInfo.InvariantCulture) as SolidColorBrush;

        Assert.NotNull(result);
        var expectedColor = (Color)ColorConverter.ConvertFromString(expectedHex);
        Assert.Equal(expectedColor, result.Color);
    }

    [Theory]
    [InlineData(true, "#065F46")]
    [InlineData(false, "#991B1B")]
    public void AckStatusToTextBrushConverter_ReturnsExpectedBrush(bool isAck, string expectedHex)
    {
        var converter = new AcknowledgementStatusToTextBrushConverter();
        var result = converter.Convert(isAck, typeof(Brush), string.Empty, CultureInfo.InvariantCulture) as SolidColorBrush;

        Assert.NotNull(result);
        var expectedColor = (Color)ColorConverter.ConvertFromString(expectedHex);
        Assert.Equal(expectedColor, result.Color);
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(5, "5")]
    [InlineData(99, "99")]
    [InlineData(100, "99+")]
    [InlineData(250, "99+")]
    public void CountToBadgeStringConverter_LimitsTo99Plus(int count, string expected)
    {
        var converter = new CountToBadgeStringConverter();
        var result = converter.Convert(count, typeof(string), string.Empty, CultureInfo.InvariantCulture);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(false, 3, Visibility.Visible)]
    [InlineData(false, 0, Visibility.Collapsed)]
    [InlineData(true, 3, Visibility.Collapsed)]
    [InlineData(true, 0, Visibility.Collapsed)]
    public void CollapsedBadgeVisibilityConverter_ShowsOnlyWhenCollapsedAndPositive(bool isExpanded, int count, Visibility expected)
    {
        var converter = new CollapsedBadgeVisibilityConverter();
        var result = converter.Convert(new object[] { isExpanded, count }, typeof(Visibility), string.Empty, CultureInfo.InvariantCulture);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(true, 3, Visibility.Visible)]
    [InlineData(true, 0, Visibility.Collapsed)]
    [InlineData(false, 3, Visibility.Collapsed)]
    [InlineData(false, 0, Visibility.Collapsed)]
    public void ExpandedBadgeVisibilityConverter_ShowsOnlyWhenExpandedAndPositive(bool isExpanded, int count, Visibility expected)
    {
        var converter = new ExpandedBadgeVisibilityConverter();
        var result = converter.Convert(new object[] { isExpanded, count }, typeof(Visibility), string.Empty, CultureInfo.InvariantCulture);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(true, "Thoát Toàn Màn Hình (F11)")]
    [InlineData(false, "Toàn Màn Hình / Video Wall (F11)")]
    public void BooleanToKioskTooltipConverter_ReturnsExpectedTooltip(bool isKiosk, string expected)
    {
        var converter = new BooleanToKioskTooltipConverter();
        var result = converter.Convert(isKiosk, typeof(string), string.Empty, CultureInfo.InvariantCulture);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(true, "Bật Lại Còi Cảnh Báo (Đang Tắt)")]
    [InlineData(false, "Tắt Còi Cảnh Báo (Đang Bật)")]
    public void BooleanToMuteTooltipConverter_ReturnsExpectedTooltip(bool isMuted, string expected)
    {
        var converter = new BooleanToMuteTooltipConverter();
        var result = converter.Convert(isMuted, typeof(string), string.Empty, CultureInfo.InvariantCulture);

        Assert.Equal(expected, result);
    }
}
