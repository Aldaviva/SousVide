using System.Globalization;
using UnitsNet;
using UnitsNet.Units;

namespace SousVideCtl;

public static class Extensions {

    public static Task Wait(this CancellationToken token) {
        if (token.IsCancellationRequested) {
            return Task.CompletedTask;
        } else {
            TaskCompletionSource           completion   = new();
            CancellationTokenRegistration? registration = null;
            registration = token.Register(_ => {
                registration!.Value.Dispose();
                completion.SetResult();
            }, false);
            return completion.Task;
        }
    }

    public static Temperature Plus(this Temperature temp, QuantityValue delta) => (temp + TemperatureDelta.From(delta, temp.Unit.ToDeltaUnit())).ToUnit(temp.Unit);

    public static TemperatureDeltaUnit ToDeltaUnit(this TemperatureUnit unit) => unit switch {
        UnitsNet.Units.TemperatureUnit.DegreeCelsius      => TemperatureDeltaUnit.DegreeCelsius,
        UnitsNet.Units.TemperatureUnit.DegreeDelisle      => TemperatureDeltaUnit.DegreeDelisle,
        UnitsNet.Units.TemperatureUnit.DegreeFahrenheit   => TemperatureDeltaUnit.DegreeFahrenheit,
        UnitsNet.Units.TemperatureUnit.DegreeNewton       => TemperatureDeltaUnit.DegreeNewton,
        UnitsNet.Units.TemperatureUnit.DegreeRankine      => TemperatureDeltaUnit.DegreeRankine,
        UnitsNet.Units.TemperatureUnit.DegreeReaumur      => TemperatureDeltaUnit.DegreeReaumur,
        UnitsNet.Units.TemperatureUnit.DegreeRoemer       => TemperatureDeltaUnit.DegreeRoemer,
        UnitsNet.Units.TemperatureUnit.Kelvin             => TemperatureDeltaUnit.Kelvin,
        UnitsNet.Units.TemperatureUnit.MillidegreeCelsius => TemperatureDeltaUnit.MillidegreeCelsius,
        _                                                 => throw new ArgumentOutOfRangeException(nameof(unit), unit, null)
    };

    public static TemperatureUnit TemperatureUnit(this CultureInfo culture) => culture.IetfLanguageTag
        is "en-AS" // American Samoa
        or "en-GU" // Guam
        or "en-KY" // British Cayman Islands
        or "en-LR" // Liberia
        or "en-MP" // Northern Mariana Islands
        or "en-PR" // Puerto Rico
        or "en-US" // United States
        or "en-VI" // US Virgin Islands
        ? UnitsNet.Units.TemperatureUnit.DegreeFahrenheit : UnitsNet.Units.TemperatureUnit.DegreeCelsius;

}