using KoKo.Events;
using SousVide;
using System.Globalization;
using UnitsNet;

namespace SousVideCtl;

public static class Program {

    private static readonly CultureInfo CurrentCulture = CultureInfo.CurrentCulture /* = CultureInfo.GetCultureInfo("en-GB")*/;
    private static readonly string      BrightBlue     = Console2.Color(Console2.Colors.BrightBlue, Console2.Colors.Black);
    private static readonly string      BrightGreen    = Console2.Color(Console2.Colors.BrightGreen, Console2.Colors.Black);
    private static readonly string      Reset          = Console2.ResetColor;

    private static event EventHandler<ConsoleKeyInfo>? KeyPressed;

    private static ISousVide? sousVide;

    public static async Task<int> Main() {
        Console.WriteLine("Connecting to Anova Precision Cooker over Bluetooth...");
        sousVide = Environment.GetEnvironmentVariable("MOCK")?.ToLowerInvariant() is "true" or "1" ? new MockSousVide() : await AnovaPrecisionCooker.Create();
        if (sousVide is null) {
            Console.WriteLine("Could not connect to any Anova Precision Cookers, or none were paired over Bluetooth");
            return 1;
        }

        CancellationTokenSource cts = new();
        Console.CancelKeyPress += (_, eventArgs) => {
            eventArgs.Cancel = true;
            cts.Cancel();
        };

        _ = Task.Run(() => {
            while (!cts.IsCancellationRequested) {
                KeyPressed?.Invoke(null, Console.ReadKey(true));
            }
        }, cts.Token);

        KeyPressed += (_, pressedKey) => {
            switch (pressedKey.Key) {
                case ConsoleKey.S when sousVide.IsRunning.Value:
                    sousVide.Stop();
                    break;
                case ConsoleKey.S:
                    sousVide.Start();
                    break;
                case ConsoleKey.X:
                    cts.Cancel();
                    break;
                case ConsoleKey.UpArrow:
                    sousVide.SetDesiredTemperature(sousVide.DesiredTemperature.Value.Plus(0.5));
                    break;
                case ConsoleKey.DownArrow:
                    sousVide.SetDesiredTemperature(sousVide.DesiredTemperature.Value.Plus(-0.5));
                    break;
            }
        };

        Console2.SetCursorVisibility(false);
        Render();
        sousVide.ActualTemperature.PropertyChanged  += OnSousVidePropertyChanged;
        sousVide.DesiredTemperature.PropertyChanged += OnSousVidePropertyChanged;
        sousVide.IsRunning.PropertyChanged          += OnSousVidePropertyChanged;

        await cts.Token.Wait();
        Console2.SetCursorVisibility(true);
        return 0;
    }

    private static void Render() {
        Console2.Clear();
        Console.WriteLine(
            $"Current temperature: {BrightBlue}{sousVide!.ActualTemperature.Value.Value,5:N1}{Reset} {Temperature.GetAbbreviation(sousVide.ActualTemperature.Value.Unit, CurrentCulture)}");
        Console.WriteLine(
            $"Target temperature:  {BrightBlue}{sousVide.DesiredTemperature.Value.Value,5:N1}{Reset} {Temperature.GetAbbreviation(sousVide.DesiredTemperature.Value.Unit, CurrentCulture)}");
        Console.Write($"\n{(sousVide.IsRunning.Value ? $"{BrightGreen}S{Reset}top " : $"{BrightGreen}S{Reset}tart")}  {BrightGreen}↑↓{Reset} Set temperature  E{BrightGreen}x{Reset}it");
    }

    private static void OnSousVidePropertyChanged<T>(object sender, KoKoPropertyChangedEventArgs<T> eventArgs) => Render();

}