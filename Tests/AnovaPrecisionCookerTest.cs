using FakeItEasy.Configuration;
using InTheHand.Bluetooth;
using SousVide;
using SousVide.Unfucked.Bluetooth;
using System.Text;
using UnitsNet;
using GattCharacteristicValueChangedEventArgs = SousVide.Unfucked.Bluetooth.GattCharacteristicValueChangedEventArgs;

namespace Tests;

public class AnovaPrecisionCookerTest: IDisposable {

    private readonly AnovaPrecisionCooker sousVide;
    private readonly IBluetoothDevice     device         = A.Fake<IBluetoothDevice>();
    private readonly IRemoteGattServer    server         = A.Fake<IRemoteGattServer>();
    private readonly IGattService         service        = A.Fake<IGattService>();
    private readonly IGattCharacteristic  characteristic = A.Fake<IGattCharacteristic>();

    public AnovaPrecisionCookerTest() {
        A.CallTo(() => device.Id).Returns("1");
        A.CallTo(() => device.Gatt).Returns(server);
        A.CallTo(() => server.GetPrimaryServicesAsync(A<BluetoothUuid>._)).Returns([service]);
        A.CallTo(() => service.GetCharacteristicAsync(A<BluetoothUuid>._)).Returns(characteristic);
        sousVide = new AnovaPrecisionCooker(device);

        RegisterResponse("read unit", "f");
        RegisterResponse("read temp", "73.4");
        RegisterResponse("read set temp", "135.0");
        RegisterResponse("status", "stopped");
    }

    [Fact]
    public void DeviceId() {
        sousVide.DeviceId.Should().Be("1");
    }

    [Fact]
    public async Task Start() {
        await sousVide.Connect();
        sousVide.IsRunning.Value.Should().BeFalse();

        RegisterResponse("start", "start");
        RegisterResponse("status", "started"); //TODO I don't actually know what this value is yet

        await sousVide.Start();

        sousVide.IsRunning.Value.Should().BeTrue();
        GetRequest("start").MustHaveHappened();
    }

    [Fact]
    public async Task Stop() {
        RegisterResponse("status", "started");
        await sousVide.Connect();
        sousVide.IsRunning.Value.Should().BeTrue();

        RegisterResponse("stop", "stop");
        RegisterResponse("status", "stopped");

        await sousVide.Stop();

        sousVide.IsRunning.Value.Should().BeFalse();
        GetRequest("stop").MustHaveHappened();
    }

    [Fact]
    public async Task SetDesiredTemperature() {
        await sousVide.Connect();

        RegisterResponse("set temp 145.0", "145.0");

        await sousVide.SetDesiredTemperature(Temperature.FromDegreesFahrenheit(145));

        sousVide.DesiredTemperature.Value.Should().Be(Temperature.FromDegreesFahrenheit(145));
        GetRequest("set temp 145.0").MustHaveHappened();
    }

    [Fact]
    public async Task IgnoreUnparsableValues() {
        await sousVide.Connect();

        RegisterResponse("start", "start");
        RegisterResponse("read temp", "abc");
        RegisterResponse("read set temp", "def");
        RegisterResponse("status", "started");

        await sousVide.Start();

        sousVide.ActualTemperature.Value.Should().Be(Temperature.FromDegreesFahrenheit(73.4));
        sousVide.DesiredTemperature.Value.Should().Be(Temperature.FromDegreesFahrenheit(135));
        sousVide.IsRunning.Value.Should().BeTrue();
    }

    [Fact]
    public async Task StartTimer() {
        RegisterResponse("stop time", "stop time");
        RegisterResponse("set timer 1", "1");
        await sousVide.Connect();

        await sousVide.StartTimer(TimeSpan.FromMinutes(1));
        GetRequest("set timer 1").MustHaveHappened();
    }

    [Fact]
    public async Task TimerMustBeAtLeastOneMinute() {
        await sousVide.Connect();

        Func<Task> thrower = async () => await sousVide.StartTimer(TimeSpan.FromSeconds(59));
        await thrower.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task StopTimer() {
        RegisterResponse("stop time", "stop time");
        await sousVide.Connect();

        await sousVide.StopTimer();
        GetRequest("stop time").MustHaveHappened();
    }

    [Fact]
    public void SetPollingInterval() {
        TimeSpan newInterval = TimeSpan.FromSeconds(2);
        sousVide.PropertyUpdateInterval.Should().NotBe(newInterval);
        sousVide.PropertyUpdateInterval = newInterval;
        sousVide.PropertyUpdateInterval.Should().Be(newInterval);
    }

    [Fact]
    public async Task UpdateProperties() {
        sousVide.PropertyUpdateInterval = TimeSpan.FromMilliseconds(100);
        await sousVide.Connect();

        TaskCompletionSource polled = new();
        RegisterResponse("status", "stopped").Invokes(() => polled.TrySetResult());

        await polled.Task.WaitAsync(TimeSpan.FromSeconds(10));
        GetRequest("status").MustHaveHappenedTwiceOrMore(); // once in the initial Connect(), and at least once in UpdateProperties()
    }

    [Fact]
    public async Task Reconnect() {
        await sousVide.Connect();
        device.GattServerDisconnected += Raise.WithEmpty();

        await sousVide.Connect();

        A.CallTo(() => characteristic.StopNotificationsAsync()).MustHaveHappened();
    }

    [Fact]
    public async Task TestDisposeAsync() {
        await sousVide.Connect();
        await sousVide.DisposeAsync();

        A.CallTo(() => server.Disconnect()).MustHaveHappened();
    }

    [Fact]
    public void TestDispose() {
        sousVide.Dispose();

        A.CallTo(() => server.Disconnect()).MustHaveHappened();
    }

    public void Dispose() {
        sousVide.Dispose();
        GC.SuppressFinalize(this);
    }

    private IReturnValueConfiguration<Task> RegisterResponse(string request, string response) => GetRequest(request).Invokes(() => characteristic.CharacteristicValueChanged +=
        Raise.With(characteristic, new GattCharacteristicValueChangedEventArgs(Encoding.ASCII.GetBytes(response + '\r'))));

    private IReturnValueArgumentValidationConfiguration<Task> GetRequest(string request) =>
        A.CallTo(() => characteristic.WriteValueWithoutResponseAsync(A<byte[]>.That.IsSameSequenceAs(Encoding.ASCII.GetBytes(request + '\r'))));

}