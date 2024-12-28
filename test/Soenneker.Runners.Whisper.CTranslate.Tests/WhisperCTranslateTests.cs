using Soenneker.Tests.FixturedUnit;
using Xunit;

namespace Soenneker.Runners.Whisper.CTranslate.Tests;

[Collection("Collection")]
public class WhisperCTranslateTests : FixturedUnitTest
{
    public WhisperCTranslateTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
    }

    [Fact]
    public void Default()
    {

    }
}
