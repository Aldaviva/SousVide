namespace SousVide.Unfucked.Bluetooth;

/// <inheritdoc cref="InTheHand.Bluetooth.GattCharacteristic" />
public interface IGattCharacteristic {

    /// <inheritdoc cref="InTheHand.Bluetooth.GattCharacteristic.CharacteristicValueChanged" />
    event EventHandler<GattCharacteristicValueChangedEventArgs>? CharacteristicValueChanged;

    /// <inheritdoc cref="InTheHand.Bluetooth.GattCharacteristic.WriteValueWithoutResponseAsync" />
    Task WriteValueWithoutResponseAsync(byte[] value);

    /// <inheritdoc cref="InTheHand.Bluetooth.GattCharacteristic.StopNotificationsAsync" />
    Task StopNotificationsAsync();

}

/// <inheritdoc />
public class GattCharacteristic(InTheHand.Bluetooth.GattCharacteristic characteristic): IGattCharacteristic {

    private event EventHandler<GattCharacteristicValueChangedEventArgs>? ValueChanged;

    private int listeners;

    /// <inheritdoc />
    public event EventHandler<GattCharacteristicValueChangedEventArgs>? CharacteristicValueChanged {
        add {
            ValueChanged += value;
            if (Interlocked.Increment(ref listeners) == 1) {
                characteristic.CharacteristicValueChanged += OnValueChange;
            }
        }
        remove {
            ValueChanged -= value;
            if (Interlocked.Decrement(ref listeners) == 0) {
                characteristic.CharacteristicValueChanged -= OnValueChange;
            }
        }
    }

    private void OnValueChange(object? sender, InTheHand.Bluetooth.GattCharacteristicValueChangedEventArgs e) => OnValueChange(sender, new GattCharacteristicValueChangedEventArgs(e));

    /// <summary>
    /// Trigger <see cref="CharacteristicValueChanged"/>
    /// </summary>
    protected void OnValueChange(object? sender, GattCharacteristicValueChangedEventArgs e) => ValueChanged?.Invoke(sender, e);

    /// <inheritdoc />
    public Task WriteValueWithoutResponseAsync(byte[] value) => characteristic.WriteValueWithoutResponseAsync(value);

    /// <inheritdoc />
    public Task StopNotificationsAsync() => characteristic.StopNotificationsAsync();

}