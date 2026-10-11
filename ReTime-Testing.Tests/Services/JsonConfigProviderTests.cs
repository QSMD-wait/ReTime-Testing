using System.IO;
using System.Text.Json.Nodes;

namespace ReTime_Testing.Tests.Services;

/// <summary>
/// JsonConfigProvider 单元测试（真实文件 I/O，临时目录）
/// 重点：原子写行为（临时文件替换、无残留）
/// </summary>
public class JsonConfigProviderTests : IDisposable
{
    private readonly string _tempDir;
    private readonly JsonConfigProvider _provider = new();

    public JsonConfigProviderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "RTT_JsonProviderTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
            // 临时目录清理失败忽略
        }
    }

    [Fact]
    public void Write_首次写入应创建目录并产生合法JSON文件()
    {
        // Arrange
        var path = Path.Combine(_tempDir, "sub", "test.json");

        // Act
        _provider.Write(path, new GlobalSetting());

        // Assert
        File.Exists(path).Should().BeTrue();
        var node = JsonNode.Parse(File.ReadAllText(path));
        node.Should().NotBeNull();
    }

    [Fact]
    public void Write_已存在文件应被完整覆盖()
    {
        // Arrange
        var path = Path.Combine(_tempDir, "test.json");
        _provider.Write(path, new GlobalSetting { Version = "1.0.0" });

        // Act
        _provider.Write(path, new GlobalSetting { Version = "2.0.0" });

        // Assert
        var node = JsonNode.Parse(File.ReadAllText(path));
        node!["version"]!.GetValue<string>().Should().Be("2.0.0");
    }

    [Fact]
    public void Write_完成后不应残留临时文件()
    {
        // Arrange
        var path = Path.Combine(_tempDir, "test.json");

        // Act
        _provider.Write(path, new GlobalSetting());
        _provider.Write(path, new GlobalSetting());

        // Assert：目录内只有正式配置文件
        Directory.GetFiles(_tempDir).Should().ContainSingle();
        Directory.GetFiles(_tempDir)[0].Should().Be(path);
    }
}
