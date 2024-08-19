using KoKo.Events;
using KoKo.Property;
using UnitsNet;

namespace SousVide;

/// <summary>
/// <para>Cooking device that maintains water at a set temperature so that submerged bagged food's internal temperature will slowly equilibrate to water temperature, allowing for precise, even internal doneness.</para>
/// </summary>
public interface ISousVide: IDisposable {

    /// <summary>
    /// <para>The unique ID of this Bluetooth device.</para>
    /// </summary>
    string DeviceId { get; }

    /// <summary>
    /// <para>The amount of time between polls to update <see cref="ActualTemperature"/>, <see cref="DesiredTemperature"/>, and <see cref="IsRunning"/>.</para>
    /// <para>By default, this is 1 poll per second.</para>
    /// <para>Note that <see cref="KoKoNotifyPropertyChanged{T}.PropertyChanged"/> will only fire when the value is different on a poll, not on every poll.</para>
    /// </summary>
    TimeSpan PropertyUpdateInterval { get; set; }

    /// <summary>
    /// <para>Whether or not the sous vide is running, which means the impeller motor is spinning and the heating element is able to turn on to heat the water.</para>
    /// <para>Control this with <see cref="Start"/> and <see cref="Stop"/>.</para>
    /// </summary>
    Property<bool> IsRunning { get; }

    /// <summary>
    /// <para>The current temperature of the water.</para>
    /// <para>For the target temperature, see <see cref="DesiredTemperature"/>.</para>
    /// </summary>
    Property<Temperature> ActualTemperature { get; }

    /// <summary>
    /// <para>The set point of the water temperature, and by extension, the target internal temperature of your food.</para>
    /// <para>When running, the sous vide will use its heating element to heat the water to reach this temperature and maintain it over a long period of time.</para>
    /// <para>To set this programmatically, call <see cref="SetDesiredTemperature"/>. You can also set it using physical controls on the device, and both changes will be reflected in this property.</para>
    /// <para>For the current water temperature, see <see cref="ActualTemperature"/>.</para>
    /// </summary>
    Property<Temperature> DesiredTemperature { get; }

    /// <summary>
    /// <para>Change the set point of the water temperature, which should be the target internal temperature of your food.</para>
    /// <para>When running, the sous vide will use its heating element to heat the water to reach this temperature and maintain it over a long period of time.</para>
    /// <para>You can also set it using physical controls on the device, and both changes will be reflected in <see cref="DesiredTemperature"/>.</para>
    /// <para>For the current water temperature, see <see cref="ActualTemperature"/>.</para>
    /// </summary>
    /// <param name="desiredTemperature">Target water temperature</param>
    Task SetDesiredTemperature(Temperature desiredTemperature);

    /// <summary>
    /// Begin cooking by heating the water to <see cref="DesiredTemperature"/>.
    /// </summary>
    Task Start();

    /// <summary>
    /// Stop cooking.
    /// </summary>
    Task Stop();

    /// <summary>
    /// <para>Start a countdown timer that will automatically <see cref="Stop"/> cooking when it finishes.</para>
    /// <para>After calling this method, call <see cref="Start"/> to begin cooking.</para>
    /// <para>To stop a timer, call <see cref="StopTimer"/>.</para>
    /// <para>If you call <see cref="Start"/> without a timer running, the sous vide will cook until you manually stop it using <see cref="Stop"/>, physical device controls, or unplugging it.</para>
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="duration"/> is less than 1 minute</exception>
    Task StartTimer(TimeSpan duration);

    /// <summary>
    /// Stop an existing countdown timer that was previously started using <see cref="StartTimer"/>.
    /// </summary>
    Task StopTimer();

}