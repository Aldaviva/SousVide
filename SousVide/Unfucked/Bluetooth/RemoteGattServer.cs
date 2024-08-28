using InTheHand.Bluetooth;

namespace SousVide.Unfucked.Bluetooth;

/// <inheritdoc cref="InTheHand.Bluetooth.RemoteGattServer" />
public interface IRemoteGattServer {

    /// <inheritdoc cref="InTheHand.Bluetooth.RemoteGattServer.ConnectAsync" />
    Task ConnectAsync();

    /// <inheritdoc cref="InTheHand.Bluetooth.RemoteGattServer.GetPrimaryServicesAsync" />
    Task<IEnumerable<IGattService>> GetPrimaryServicesAsync(BluetoothUuid? service = null);

    /// <inheritdoc cref="InTheHand.Bluetooth.RemoteGattServer.Disconnect" />
    void Disconnect();

    /// <inheritdoc cref="InTheHand.Bluetooth.RemoteGattServer.IsConnected" />
    bool IsConnected { get; }

}

/// <inheritdoc />
public class RemoteGattServer(InTheHand.Bluetooth.RemoteGattServer server): IRemoteGattServer {

    /// <inheritdoc />
    public Task ConnectAsync() => server.ConnectAsync();

    /// <inheritdoc />
    public async Task<IEnumerable<IGattService>> GetPrimaryServicesAsync(BluetoothUuid? service = null) =>
        (await server.GetPrimaryServicesAsync(service).ConfigureAwait(false)).Select(gattService => new GattService(gattService));

    /// <inheritdoc />
    public void Disconnect() => server.Disconnect();

    /// <inheritdoc />
    public bool IsConnected => server.IsConnected;

}