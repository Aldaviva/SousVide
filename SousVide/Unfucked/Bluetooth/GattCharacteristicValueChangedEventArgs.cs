namespace SousVide.Unfucked.Bluetooth;

/// <inheritdoc cref="InTheHand.Bluetooth.GattCharacteristicValueChangedEventArgs" />
public class GattCharacteristicValueChangedEventArgs(byte[]? value, Exception? error = null): EventArgs {

    /// <summary>
    /// Wrap non-constructable event args.
    /// </summary>
    /// <param name="eventArgs">event args from <c>InTheHand.BluetoothLE</c></param>
    public GattCharacteristicValueChangedEventArgs(InTheHand.Bluetooth.GattCharacteristicValueChangedEventArgs eventArgs): this(eventArgs.Value, eventArgs.Error) { }

    /// <inheritdoc cref="InTheHand.Bluetooth.GattCharacteristicValueChangedEventArgs.Value" />
    public byte[]? Value { get; } = value;

    /// <inheritdoc cref="InTheHand.Bluetooth.GattCharacteristicValueChangedEventArgs.Error" />
    public Exception? Error { get; } = error;

}