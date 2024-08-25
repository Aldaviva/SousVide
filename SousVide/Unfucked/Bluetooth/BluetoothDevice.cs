namespace SousVide.Unfucked.Bluetooth;

/// <inheritdoc cref="InTheHand.Bluetooth.BluetoothDevice" />
public interface IBluetoothDevice {

    /// <inheritdoc cref="InTheHand.Bluetooth.BluetoothDevice.Id" />
    string Id { get; }

    /// <inheritdoc cref="InTheHand.Bluetooth.BluetoothDevice.Name" />
    string Name { get; }

    /// <inheritdoc cref="InTheHand.Bluetooth.BluetoothDevice.Gatt" />
    IRemoteGattServer Gatt { get; }

}

/// <inheritdoc />
public class BluetoothDevice(InTheHand.Bluetooth.BluetoothDevice device): IBluetoothDevice {

    private readonly Lazy<IRemoteGattServer> gattServer = new(() => new RemoteGattServer(device.Gatt), LazyThreadSafetyMode.PublicationOnly);

    /// <inheritdoc />
    public string Id => device.Id;

    /// <inheritdoc />
    public string Name => device.Name;

    /// <inheritdoc />
    public IRemoteGattServer Gatt => gattServer.Value;

}