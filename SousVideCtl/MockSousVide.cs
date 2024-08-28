using KoKo.Property;
using SousVide;
using System.Globalization;
using System.Timers;
using UnitsNet;
using Timer = System.Timers.Timer;

namespace SousVideCtl;

public class MockSousVide: ISousVide {

    private static readonly CultureInfo CurrentCulture  = CultureInfo.CurrentCulture;
    private static readonly Temperature RoomTemperature = Temperature.FromDegreesFahrenheit(73).ToUnit(CurrentCulture.TemperatureUnit());

    private readonly StoredProperty<Temperature> actualTemperature;
    private readonly StoredProperty<Temperature> desiredTemperature = new(Temperature.FromDegreesFahrenheit(135).ToUnit(CurrentCulture.TemperatureUnit()));
    private readonly StoredProperty<bool>        isRunning          = new();
    private readonly Timer                       timer              = new(1000) { AutoReset = true };

    public string DeviceId { get; } = "1";
    public Property<bool> IsRunning { get; }
    public Property<Temperature> ActualTemperature { get; }
    public Property<Temperature> DesiredTemperature { get; }

    public TimeSpan PropertyUpdateInterval {
        get => TimeSpan.FromMilliseconds(timer.Interval);
        set => timer.Interval = value.TotalMilliseconds;
    }

    public MockSousVide(Temperature? actualTemperature = default) {
        this.actualTemperature = new StoredProperty<Temperature>(actualTemperature ?? RoomTemperature);

        DesiredTemperature = desiredTemperature;
        ActualTemperature  = this.actualTemperature;
        IsRunning          = isRunning;

        timer.Elapsed += OnTimerTick;
        timer.Enabled =  true;
    }

    private void OnTimerTick(object? sender, ElapsedEventArgs e) {
        Temperature ambientTemperature = IsRunning.Value ? DesiredTemperature.Value : RoomTemperature;
        actualTemperature.Value = ((ambientTemperature - ActualTemperature.Value) * 0.01 + actualTemperature.Value).ToUnit(actualTemperature.Value.Unit);
    }

    public Task SetDesiredTemperature(Temperature desiredTemperature) {
        this.desiredTemperature.Value = desiredTemperature;
        return Task.CompletedTask;
    }

    public Task Start() {
        isRunning.Value = true;
        return Task.CompletedTask;
    }

    public Task Stop() {
        isRunning.Value = false;
        return Task.CompletedTask;
    }

    public Task StartTimer(TimeSpan duration) {
        throw new NotImplementedException();
    }

    public Task StopTimer() {
        throw new NotImplementedException();
    }

    public void Dispose() {
        timer.Dispose();
        GC.SuppressFinalize(this);
    }

    public ValueTask DisposeAsync() {
        Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

}