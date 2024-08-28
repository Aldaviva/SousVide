using SousVide.Exceptions;

namespace Tests;

public class ExceptionsTest {

    [Fact]
    public void UnsupportedDevice() {
        UnsupportedDevice actual = new("abc", "no service found");
        actual.DeviceId.Should().Be("abc");
        actual.Message.Should().Be("no service found");
        actual.InnerException.Should().BeNull();
    }

}