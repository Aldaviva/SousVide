using SousVide;
using UnitsNet;

namespace Tests;

public class TolerantTemperatureComparerTest {

    [Fact]
    public void ExactlyEquals() {
        TolerantTemperatureComparer comparer = new(Temperature.FromDegreesFahrenheit(0));

        Temperature a = Temperature.FromDegreesFahrenheit(82.0);
        Temperature b = Temperature.FromDegreesFahrenheit(82.0);
        comparer.Equals(a, b).Should().BeTrue();
        comparer.GetHashCode(a).Should().Be(comparer.GetHashCode(b));

        Temperature c = Temperature.FromDegreesFahrenheit(82.1);
        comparer.Equals(a, c).Should().BeFalse();
        comparer.GetHashCode(a).Should().NotBe(comparer.GetHashCode(c));
    }

    [Theory]
    [InlineData(81.0)]
    [InlineData(81.9)]
    [InlineData(82.0)]
    [InlineData(82.1)]
    [InlineData(83.0)]
    public void WithinTolerance(QuantityValue temperature) {
        TolerantTemperatureComparer comparer = new(Temperature.FromDegreesFahrenheit(1));
        comparer.Equals(Temperature.FromDegreesFahrenheit(82.0), Temperature.FromDegreesFahrenheit(temperature)).Should().BeTrue();
    }

    [Theory]
    [InlineData(80.9)]
    [InlineData(83.1)]
    public void OutOfTolerance(QuantityValue temperature) {
        TolerantTemperatureComparer comparer = new(Temperature.FromDegreesFahrenheit(1));
        comparer.Equals(Temperature.FromDegreesFahrenheit(82.0), Temperature.FromDegreesFahrenheit(temperature)).Should().BeFalse();
    }

}