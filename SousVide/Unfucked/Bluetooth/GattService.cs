using InTheHand.Bluetooth;

namespace SousVide.Unfucked.Bluetooth;

/// <inheritdoc cref="InTheHand.Bluetooth.GattService" />
public interface IGattService {

    /// <inheritdoc cref="InTheHand.Bluetooth.GattService.GetCharacteristicAsync" />
    Task<IGattCharacteristic?> GetCharacteristicAsync(BluetoothUuid characteristic);

}

/// <inheritdoc />
public class GattService(InTheHand.Bluetooth.GattService service): IGattService {

    /// <inheritdoc />
    public async Task<IGattCharacteristic?> GetCharacteristicAsync(BluetoothUuid characteristic) =>
        await service.GetCharacteristicAsync(characteristic).ConfigureAwait(false) is { } c ? new GattCharacteristic(c) : null;

}