using ReTime_Testing.Models;

namespace ReTime_Testing.Tests.Models;

/// <summary>
/// StyleChangeData 类的单元测试
/// 测试样式变更数据的基本功能
/// </summary>
public class StyleChangeDataTests
{
    [Fact]
    public void Constructor_默认构造函数_应该创建空对象()
    {
        // Arrange & Act
        var styleChange = new StyleChangeData();

        // Assert
        styleChange.ForegroundColor.Should().BeNull();
        styleChange.BackgroundColor.Should().BeNull();
        styleChange.Opacity.Should().BeNull();
    }

    [Fact]
    public void Constructor_设置ForegroundColor_应该正确保存()
    {
        // Arrange & Act
        var styleChange = new StyleChangeData
        {
            ForegroundColor = "#00FF00"
        };

        // Assert
        styleChange.ForegroundColor.Should().Be("#00FF00");
    }

    [Fact]
    public void Constructor_设置BackgroundColor_应该正确保存()
    {
        // Arrange & Act
        var styleChange = new StyleChangeData
        {
            BackgroundColor = "#FF0000"
        };

        // Assert
        styleChange.BackgroundColor.Should().Be("#FF0000");
    }

    [Fact]
    public void Constructor_设置Opacity_应该正确保存()
    {
        // Arrange & Act
        var styleChange = new StyleChangeData
        {
            Opacity = 0.5
        };

        // Assert
        styleChange.Opacity.Should().Be(0.5);
    }

    [Fact]
    public void Constructor_设置所有属性_应该正确保存()
    {
        // Arrange & Act
        var styleChange = new StyleChangeData
        {
            ForegroundColor = "#00FF00",
            BackgroundColor = "#FF0000",
            Opacity = 0.8
        };

        // Assert
        styleChange.ForegroundColor.Should().Be("#00FF00");
        styleChange.BackgroundColor.Should().Be("#FF0000");
        styleChange.Opacity.Should().Be(0.8);
    }

    [Fact]
    public void ForegroundColor_设置为null_应该正确保存()
    {
        // Arrange & Act
        var styleChange = new StyleChangeData
        {
            ForegroundColor = null
        };

        // Assert
        styleChange.ForegroundColor.Should().BeNull();
    }

    [Fact]
    public void ForegroundColor_设置为空字符串_应该正确保存()
    {
        // Arrange & Act
        var styleChange = new StyleChangeData
        {
            ForegroundColor = ""
        };

        // Assert
        styleChange.ForegroundColor.Should().Be("");
    }

    [Fact]
    public void ForegroundColor_设置为十六进制格式_应该正确保存()
    {
        // Arrange & Act
        var styleChange = new StyleChangeData
        {
            ForegroundColor = "#RRGGBB"
        };

        // Assert
        styleChange.ForegroundColor.Should().Be("#RRGGBB");
    }

    [Fact]
    public void BackgroundColor_设置为null_应该正确保存()
    {
        // Arrange & Act
        var styleChange = new StyleChangeData
        {
            BackgroundColor = null
        };

        // Assert
        styleChange.BackgroundColor.Should().BeNull();
    }

    [Fact]
    public void BackgroundColor_设置为空字符串_应该正确保存()
    {
        // Arrange & Act
        var styleChange = new StyleChangeData
        {
            BackgroundColor = ""
        };

        // Assert
        styleChange.BackgroundColor.Should().Be("");
    }

    [Fact]
    public void Opacity_设置为null_应该正确保存()
    {
        // Arrange & Act
        var styleChange = new StyleChangeData
        {
            Opacity = null
        };

        // Assert
        styleChange.Opacity.Should().BeNull();
    }

    [Fact]
    public void Opacity_设置为0_应该正确保存()
    {
        // Arrange & Act
        var styleChange = new StyleChangeData
        {
            Opacity = 0
        };

        // Assert
        styleChange.Opacity.Should().Be(0);
    }

    [Fact]
    public void Opacity_设置为1_应该正确保存()
    {
        // Arrange & Act
        var styleChange = new StyleChangeData
        {
            Opacity = 1
        };

        // Assert
        styleChange.Opacity.Should().Be(1);
    }

    [Fact]
    public void Opacity_设置为负数_应该正确保存()
    {
        // Arrange & Act
        var styleChange = new StyleChangeData
        {
            Opacity = -0.5
        };

        // Assert
        styleChange.Opacity.Should().Be(-0.5);
    }

    [Fact]
    public void Opacity_设置为大于1的数_应该正确保存()
    {
        // Arrange & Act
        var styleChange = new StyleChangeData
        {
            Opacity = 1.5
        };

        // Assert
        styleChange.Opacity.Should().Be(1.5);
    }

    [Fact]
    public void Clone_应该创建独立副本()
    {
        // Arrange
        var original = new StyleChangeData
        {
            ForegroundColor = "#00FF00",
            BackgroundColor = "#FF0000",
            Opacity = 0.8
        };

        // Act
        var cloned = new StyleChangeData
        {
            ForegroundColor = original.ForegroundColor,
            BackgroundColor = original.BackgroundColor,
            Opacity = original.Opacity
        };

        // Assert
        cloned.Should().NotBeSameAs(original);
        cloned.ForegroundColor.Should().Be(original.ForegroundColor);
        cloned.BackgroundColor.Should().Be(original.BackgroundColor);
        cloned.Opacity.Should().Be(original.Opacity);
    }

    [Fact]
    public void Clone_修改副本不应影响原对象()
    {
        // Arrange
        var original = new StyleChangeData
        {
            ForegroundColor = "#00FF00",
            BackgroundColor = "#FF0000",
            Opacity = 0.8
        };
        var cloned = new StyleChangeData
        {
            ForegroundColor = original.ForegroundColor,
            BackgroundColor = original.BackgroundColor,
            Opacity = original.Opacity
        };

        // Act
        cloned.ForegroundColor = "#0000FF";

        // Assert
        original.ForegroundColor.Should().Be("#00FF00");
    }

    [Fact]
    public void HasAnyProperty_所有属性为null_应该返回false()
    {
        // Arrange
        var styleChange = new StyleChangeData();

        // Act
        var hasAnyProperty = !string.IsNullOrEmpty(styleChange.ForegroundColor) ||
                            !string.IsNullOrEmpty(styleChange.BackgroundColor) ||
                            styleChange.Opacity.HasValue;

        // Assert
        hasAnyProperty.Should().BeFalse();
    }

    [Fact]
    public void HasAnyProperty_只有ForegroundColor_应该返回true()
    {
        // Arrange
        var styleChange = new StyleChangeData
        {
            ForegroundColor = "#00FF00"
        };

        // Act
        var hasAnyProperty = !string.IsNullOrEmpty(styleChange.ForegroundColor) ||
                            !string.IsNullOrEmpty(styleChange.BackgroundColor) ||
                            styleChange.Opacity.HasValue;

        // Assert
        hasAnyProperty.Should().BeTrue();
    }

    [Fact]
    public void HasAnyProperty_只有BackgroundColor_应该返回true()
    {
        // Arrange
        var styleChange = new StyleChangeData
        {
            BackgroundColor = "#FF0000"
        };

        // Act
        var hasAnyProperty = !string.IsNullOrEmpty(styleChange.ForegroundColor) ||
                            !string.IsNullOrEmpty(styleChange.BackgroundColor) ||
                            styleChange.Opacity.HasValue;

        // Assert
        hasAnyProperty.Should().BeTrue();
    }

    [Fact]
    public void HasAnyProperty_只有Opacity_应该返回true()
    {
        // Arrange
        var styleChange = new StyleChangeData
        {
            Opacity = 0.5
        };

        // Act
        var hasAnyProperty = !string.IsNullOrEmpty(styleChange.ForegroundColor) ||
                            !string.IsNullOrEmpty(styleChange.BackgroundColor) ||
                            styleChange.Opacity.HasValue;

        // Assert
        hasAnyProperty.Should().BeTrue();
    }

    [Fact]
    public void HasAnyProperty_所有属性都有值_应该返回true()
    {
        // Arrange
        var styleChange = new StyleChangeData
        {
            ForegroundColor = "#00FF00",
            BackgroundColor = "#FF0000",
            Opacity = 0.8
        };

        // Act
        var hasAnyProperty = !string.IsNullOrEmpty(styleChange.ForegroundColor) ||
                            !string.IsNullOrEmpty(styleChange.BackgroundColor) ||
                            styleChange.Opacity.HasValue;

        // Assert
        hasAnyProperty.Should().BeTrue();
    }
}