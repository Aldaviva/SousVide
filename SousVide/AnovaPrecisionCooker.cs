using InTheHand.Bluetooth;
using KoKo.Events;
using KoKo.Property;
using SousVide.Exceptions;
using SousVide.Unfucked.Bluetooth;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Timers;
using UnitsNet;
using UnitsNet.Units;
using BluetoothDevice = SousVide.Unfucked.Bluetooth.BluetoothDevice;
using GattCharacteristicValueChangedEventArgs = SousVide.Unfucked.Bluetooth.GattCharacteristicValueChangedEventArgs;
using Timer = System.Timers.Timer;

namespace SousVide;

/// <summary>
/// <para>An Anova Precision Cooker Bluetooth sous vide. Instantiate and connect using <see cref="Create"/>.</para>
/// <inheritdoc cref="ISousVide" path="/summary" />
/// </summary>
public class AnovaPrecisionCooker: ISousVide {

    private const string DeviceName             = "Anova";
    private const string ResponseInvalidCommand = "Invalid Command";

    private static readonly Encoding                       Encoding                      = Encoding.ASCII;
    private static readonly BluetoothUuid                  ServiceId                     = BluetoothUuid.FromShortId(0xffe0);
    private static readonly BluetoothUuid                  CharacteristicId              = BluetoothUuid.FromShortId(0xffe1);
    private static readonly TimeSpan                       DefaultPropertyUpdateInterval = new(0, 0, 1);
    private static readonly IEqualityComparer<Temperature> TemperatureComparer           = new TolerantTemperatureComparer(Temperature.FromDegreesFahrenheit(0.05));

    private readonly SemaphoreSlim     serializeRequestsMutex = new(1);
    private readonly IBluetoothDevice  device;
    private readonly IRemoteGattServer server;
    private readonly Timer             timer = new(DefaultPropertyUpdateInterval.TotalMilliseconds) { AutoReset = true, Enabled = false };

    private TaskCompletionSource<string>? currentResponseHandler;
    private IGattService?                 service;
    private IGattCharacteristic?          characteristic;
    private TemperatureUnit               temperatureUnit = TemperatureUnit.DegreeFahrenheit;

    private volatile bool isConnected;

    /// <summary>
    /// <inheritdoc path="/summary" />
    /// <para>To reconnect to this same exact device instead of another device with the same name, store this value and pass it to <see cref="Create"/>.</para>
    /// </summary>
    public string DeviceId { get; }

    /// <inheritdoc />
    public TimeSpan PropertyUpdateInterval {
        get => TimeSpan.FromMilliseconds(timer.Interval);
        set => timer.Interval = value.TotalMilliseconds;
    }

    private readonly StoredProperty<Temperature> actualTemperature  = new() { EqualityComparer = TemperatureComparer };
    private readonly StoredProperty<Temperature> desiredTemperature = new() { EqualityComparer = TemperatureComparer };
    private readonly StoredProperty<bool>        isRunning          = new();

    /// <inheritdoc />
    public Property<Temperature> ActualTemperature { get; }

    /// <inheritdoc />
    public Property<Temperature> DesiredTemperature { get; }

    /// <inheritdoc />
    public Property<bool> IsRunning { get; }

    /// <summary>
    /// Instantiate based on a given Bluetooth device without automatically connecting. Call <see cref="Connect"/> on this new instance before using it.
    /// </summary>
    /// <param name="device">Bluetooth device (possibly from <see cref="Bluetooth.GetPairedDevicesAsync"/>)</param>
    protected internal AnovaPrecisionCooker(IBluetoothDevice device) {
        this.device = device;
        DeviceId    = device.Id;
        server      = device.Gatt;

        ActualTemperature  = actualTemperature;
        DesiredTemperature = desiredTemperature;
        IsRunning          = isRunning;

        timer.Elapsed                 += UpdatePropertiesVoid;
        device.GattServerDisconnected += onDisconnection;
    }

    /// <summary>
    /// <para>Connect to an Anova Precision Cooker sous vide over Bluetooth. It must already be paired to this computer before calling this method.</para>
    /// </summary>
    /// <param name="deviceId">A unique identifier for the exact device instance you want to reconnect to, based on persisting <see cref="DeviceId"/> from a previous instance; or <c>null</c> to connect to any paired Anova sous vide</param>
    /// <returns>Instance that can monitor and control the connected sous vide device, or <c>null</c> if no paired Anova Precision Cookers were found.</returns>
    [ExcludeFromCodeCoverage]
    public static async Task<AnovaPrecisionCooker?> Create(string? deviceId = null) {
        IReadOnlyCollection<InTheHand.Bluetooth.BluetoothDevice> allDevices = await Bluetooth.GetPairedDevicesAsync().ConfigureAwait(false);
        try {
            if (allDevices.FirstOrDefault(dev => dev.Name == DeviceName && (deviceId == null || deviceId == dev.Id)) is { } device) {
                AnovaPrecisionCooker sousVide = new(new BluetoothDevice(device));
                await sousVide.Connect().ConfigureAwait(false);
                return sousVide;
            }
        } catch (UnsupportedDevice) { } /* return null below */
        return null;
    }

    /// <summary>Connect to this instance over Bluetooth and initialize the state of this client.</summary>
    /// <exception cref="UnsupportedDevice">the Bluetooth device has the name <c>Anova</c> but does not expose the required GATT service</exception>
    protected internal virtual async Task Connect() {
        if (!isConnected) {
            timer.Enabled = false;
            service       = null;
            if (characteristic != null) {
                characteristic.CharacteristicValueChanged -= OnResponseReceived;
                await characteristic.StopNotificationsAsync().ConfigureAwait(false);
                characteristic = null;
            }

            await server.ConnectAsync().ConfigureAwait(false);
            isConnected = true;

            service = (await server.GetPrimaryServicesAsync(ServiceId).ConfigureAwait(false)).FirstOrDefault()
                ?? throw new UnsupportedDevice(DeviceId, $"Could not find service {ServiceId} in device {device.Name}");
            characteristic = await service.GetCharacteristicAsync(CharacteristicId).ConfigureAwait(false);

            characteristic!.CharacteristicValueChanged += OnResponseReceived;

            temperatureUnit = await SendRequestAndReceiveResponse("read unit").ConfigureAwait(false) == "f" ? TemperatureUnit.DegreeFahrenheit : TemperatureUnit.DegreeCelsius;
            await UpdateProperties().ConfigureAwait(false);
            timer.Enabled = true;
        }
    }

    private async void UpdatePropertiesVoid(object? sender, ElapsedEventArgs e) => await UpdateProperties().ConfigureAwait(false);

    /// <summary>
    /// <para>Poll for updates to <see cref="ActualTemperature"/>, <see cref="DesiredTemperature"/>, and <see cref="IsRunning"/>.</para>
    /// <para>This is automatically called periodically when the client is connected, and <see cref="KoKoNotifyPropertyChanged{T}"/> events will be fired if a property value changes.</para>
    /// <para>To change the frequency of this poll, set <see cref="PropertyUpdateInterval"/>.</para>
    /// </summary>
    protected virtual async Task UpdateProperties() {
        try {
            actualTemperature.Value = Temperature.From(double.Parse(await SendRequestAndReceiveResponse("read temp").ConfigureAwait(false)), temperatureUnit);
        } catch (FormatException) { }
#if NETFRAMEWORK
        catch (OverflowException) { }
#endif

        try {
            desiredTemperature.Value = Temperature.From(double.Parse(await SendRequestAndReceiveResponse("read set temp").ConfigureAwait(false)), temperatureUnit);
        } catch (FormatException) { }
#if NETFRAMEWORK
        catch (OverflowException) { }
#endif

        isRunning.Value = await SendRequestAndReceiveResponse("status").ConfigureAwait(false) != "stopped";
    }

    /// <summary>
    /// <para>Send a request to the sous vide without waiting for a response.</para>
    /// <para>Must only be used for requests that do not send responses, otherwise, call <see cref="SendRequestAndReceiveResponse"/>, even if you ignore the response, to prevent a subsequent request from receiving the wrong response.</para>
    /// </summary>
    /// <param name="command">Text command to send. A trailing <c>\r</c> will be automatically appended before being sent.</param>
    protected virtual async Task SendRequestThatReturnsNoResponse(string command) {
        await serializeRequestsMutex.WaitAsync().ConfigureAwait(false);
        try {
            await SendCommandInternal(command).ConfigureAwait(false);
        } finally {
            serializeRequestsMutex.Release();
        }
    }

    /// <summary>
    /// <para>Send a request to the sous vide and wait for a response.</para>
    /// <para>Must be used for all requests that send responses, even if you ignore the response, otherwise, call <see cref="SendRequestThatReturnsNoResponse"/>, to prevent a subsequent request from receiving the wrong response.</para>
    /// </summary>
    /// <param name="command"><inheritdoc cref="SendRequestThatReturnsNoResponse" path="/param[@name='command']"/></param>
    /// <returns>The output string that was returned in response to this request, with surrounding whitespace trimmed.</returns>
    protected virtual async Task<string> SendRequestAndReceiveResponse(string command) {
        await serializeRequestsMutex.WaitAsync().ConfigureAwait(false);
        try {
            currentResponseHandler = new TaskCompletionSource<string>();

            await SendCommandInternal(command).ConfigureAwait(false);

            return await currentResponseHandler.Task.ConfigureAwait(false);
        } finally {
            currentResponseHandler = null;
            serializeRequestsMutex.Release();
        }
    }

    private async Task SendCommandInternal(string command) {
        command = command.Trim();
        Trace.WriteLine(command, "ble-tx");
        await characteristic!.WriteValueWithoutResponseAsync(Encoding.GetBytes(command + '\r')).ConfigureAwait(false);
    }

    /// <summary>
    /// Callback to handle a response from the sous vide to requests sent by <see cref="SendRequestAndReceiveResponse"/>.
    /// </summary>
    /// <param name="sender">event emitter</param>
    /// <param name="e">response bytes</param>
    protected virtual void OnResponseReceived(object? sender, GattCharacteristicValueChangedEventArgs e) {
        if (e.Value is { } value) {
            string message = Encoding.GetString(value).Trim();
            Trace.WriteLine(message, "ble-rx");
            if (message != ResponseInvalidCommand && currentResponseHandler != null) {
                currentResponseHandler.TrySetResult(message);
            }
        }
    }

    /// <inheritdoc />
    public async Task Start() {
        await SendRequestAndReceiveResponse("start").ConfigureAwait(false);
        await UpdateProperties().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task Stop() {
        await SendRequestAndReceiveResponse("stop").ConfigureAwait(false); // TODO I don't know what this command is
        await UpdateProperties().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task StartTimer(TimeSpan duration) {
        if (duration < new TimeSpan(0, 1, 0)) {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "Timer must be at least 1 minute");
        }
        await StopTimer().ConfigureAwait(false);
        await SendRequestAndReceiveResponse($"set timer {duration.TotalMinutes:F0}").ConfigureAwait(false);
        await SendRequestThatReturnsNoResponse("start time").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task StopTimer() {
        await SendRequestAndReceiveResponse("stop time").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SetDesiredTemperature(Temperature temperature) {
        await SendRequestAndReceiveResponse($"set temp {temperature.As(temperatureUnit):F1}").ConfigureAwait(false);
        desiredTemperature.Value = temperature;
    }

    private void onDisconnection(object? sender, EventArgs e) {
        isConnected = false;
    }

    /// <inheritdoc cref="Dispose()" />
    protected virtual void Dispose(bool disposing) {
        if (disposing) {
            timer.Dispose();
            if (characteristic != null) {
                characteristic.CharacteristicValueChanged -= OnResponseReceived;
                characteristic                            =  null;
            }
            service                       =  null;
            isConnected                   =  false;
            device.GattServerDisconnected -= onDisconnection;
            server.Disconnect();
            currentResponseHandler = null;
            serializeRequestsMutex.Dispose();
        }
    }

    /// <inheritdoc />
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

#if NETCOREAPP
    /// <inheritdoc cref="DisposeAsync" />
    protected virtual async ValueTask DisposeAsyncCore() {
        if (characteristic != null) {
            try {
                await characteristic.StopNotificationsAsync().ConfigureAwait(false);
            } catch (Exception e) when (e is not OutOfMemoryException) { }
        }
        Dispose(true);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() {
        await DisposeAsyncCore().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
#endif

}