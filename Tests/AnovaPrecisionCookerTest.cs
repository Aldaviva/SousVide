using FakeItEasy.Configuration;
using InTheHand.Bluetooth;
using SousVide;
using SousVide.Unfucked.Bluetooth;
using System.Text;
using UnitsNet;
using GattCharacteristicValueChangedEventArgs = SousVide.Unfucked.Bluetooth.GattCharacteristicValueChangedEventArgs;

namespace Tests;

public class AnovaPrecisionCookerTest {

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

        registerResponse("read unit", "f");
        registerResponse("read temp", "73.4");
        registerResponse("read set temp", "135.0");
        registerResponse("status", "stopped");
    }

    [Fact]
    public void DeviceId() {
        sousVide.DeviceId.Should().Be("1");
    }

    [Fact]
    public async Task Start() {
        await sousVide.Connect();
        sousVide.IsRunning.Value.Should().BeFalse();

        registerResponse("start", "start");
        registerResponse("status", "started"); //TODO I don't actually know what this value is yet

        await sousVide.Start();

        sousVide.IsRunning.Value.Should().BeTrue();
        getRequest("start").MustHaveHappened();
    }

    [Fact]
    public async Task Stop() {
        registerResponse("status", "started");
        await sousVide.Connect();
        sousVide.IsRunning.Value.Should().BeTrue();

        registerResponse("stop", "stop");
        registerResponse("status", "stopped");

        await sousVide.Stop();

        sousVide.IsRunning.Value.Should().BeFalse();
        getRequest("stop").MustHaveHappened();
    }

    [Fact]
    public async Task SetDesiredTemperature() {
        await sousVide.Connect();

        registerResponse("set temp 145.0", "145.0");

        await sousVide.SetDesiredTemperature(Temperature.FromDegreesFahrenheit(145));

        sousVide.DesiredTemperature.Value.Should().Be(Temperature.FromDegreesFahrenheit(145));
        getRequest("set temp 145.0").MustHaveHappened();
    }

    [Fact]
    public async Task IgnoreUnparsableValues() {
        await sousVide.Connect();

        registerResponse("start", "start");
        registerResponse("read temp", "abc");
        registerResponse("read set temp", "def");
        registerResponse("status", "started");

        await sousVide.Start();

        sousVide.ActualTemperature.Value.Should().Be(Temperature.FromDegreesFahrenheit(73.4));
        sousVide.DesiredTemperature.Value.Should().Be(Temperature.FromDegreesFahrenheit(135));
        sousVide.IsRunning.Value.Should().BeTrue();
    }

    [Fact]
    public async Task StartTimer() {
        registerResponse("stop time", "stop time");
        registerResponse("set timer 1", "1");
        await sousVide.Connect();

        await sousVide.StartTimer(TimeSpan.FromMinutes(1));
        getRequest("set timer 1").MustHaveHappened();
    }

    [Fact]
    public async Task TimerMustBeAtLeastOneMinute() {
        await sousVide.Connect();

        Func<Task> thrower = async () => await sousVide.StartTimer(TimeSpan.FromSeconds(59));
        thrower.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task StopTimer() {
        registerResponse("stop time", "stop time");
        await sousVide.Connect();

        await sousVide.StopTimer();
        getRequest("stop time").MustHaveHappened();
    }

    [Fact]
    public async Task DisposeAsync() {
        await sousVide.Connect();
        await sousVide.DisposeAsync();

        A.CallTo(() => server.Disconnect()).MustHaveHappened();
    }

    [Fact]
    public void Dispose() {
        sousVide.Dispose();

        A.CallTo(() => server.Disconnect()).MustHaveHappened();
    }

    private void registerResponse(string request, string response) {
        getRequest(request).Invokes(
            () => characteristic.CharacteristicValueChanged += Raise.With(characteristic, new GattCharacteristicValueChangedEventArgs(Encoding.ASCII.GetBytes(response + '\r'))));
    }

    private IReturnValueArgumentValidationConfiguration<Task> getRequest(string request) =>
        A.CallTo(() => characteristic.WriteValueWithoutResponseAsync(A<byte[]>.That.IsSameSequenceAs(Encoding.ASCII.GetBytes(request + '\r'))));

}