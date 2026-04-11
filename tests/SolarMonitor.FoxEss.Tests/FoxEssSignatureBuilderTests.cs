using SolarMonitor.FoxEss;

namespace SolarMonitor.FoxEss.Tests;

public sealed class FoxEssSignatureBuilderTests
{
    [Fact]
    public void CreateSignature_UsesLiteralEscapedSeparators()
    {
        var signature = FoxEssSignatureBuilder.CreateSignature(
            "/op/v0/device/list",
            "321d4321-aaaa-bbbb-cccc-1234567852b5",
            "1775802121700");

        Assert.Equal("212b7a827fcb9d601b6ca0eee70eee78", signature);
    }
}
