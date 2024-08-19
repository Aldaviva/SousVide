namespace SousVide.Exceptions;

/// <summary>
/// An error occurred while communicating with the sous vide device.
/// </summary>
/// <param name="deviceId">The unique ID of this device</param>
/// <param name="message">Description of the error</param>
/// <param name="innerException">Underlying cause of the error</param>
public abstract class SousVideException(string deviceId, string? message, Exception? innerException = null): ApplicationException(message, innerException) {

    /// <summary>
    /// The unique ID of this device.
    /// </summary>
    public string DeviceId { get; init; } = deviceId;

}

/// <summary>
/// The specified device is not supported, possibly because it is the wrong model or not a sous vide.
/// </summary>
/// <param name="deviceId">The unique ID of this device</param>
/// <param name="message">Description of the error</param>
public class UnsupportedDevice(string deviceId, string? message): SousVideException(deviceId, message);