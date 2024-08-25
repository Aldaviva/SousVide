using KoKo.Events;
using SousVide;
using System.Globalization;
using UnitsNet;

namespace SousVideCtl;

public static class Program {

    private static readonly CultureInfo CurrentCulture = CultureInfo.CurrentCulture /* = CultureInfo.GetCultureInfo("en-GB")*/;

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
                ConsoleKeyInfo consoleKeyInfo = Console.ReadKey(true);
                KeyPressed?.Invoke(null, consoleKeyInfo);
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

        Console.Write("\x1b[?25l"); // hide cursor
        Render();
        sousVide.ActualTemperature.PropertyChanged  += OnSousVidePropertyChanged;
        sousVide.DesiredTemperature.PropertyChanged += OnSousVidePropertyChanged;
        sousVide.IsRunning.PropertyChanged          += OnSousVidePropertyChanged;

        await cts.Token.Wait();
        Console.Write("\x1b[?25h"); // show cursor
        return 0;
    }

    private static void Render() {
        Console.Write("\x1b[1J\x1b[1;1H"); // clear screen and move to top-left position
        Console.WriteLine("Current temperature: \x1b[94;40m{0,5:N1}\x1b[39;49m {1}", sousVide!.ActualTemperature.Value.Value,
            Temperature.GetAbbreviation(sousVide.ActualTemperature.Value.Unit, CurrentCulture));
        Console.WriteLine("Target temperature:  \x1b[94;40m{0,5:N1}\x1b[39;49m {1}", sousVide.DesiredTemperature.Value.Value,
            Temperature.GetAbbreviation(sousVide.DesiredTemperature.Value.Unit, CurrentCulture));
        Console.Write("\n{0}  {1}  {2}", sousVide.IsRunning.Value ? "\x1b[92;40mS\x1b[39;49mtop " : "\x1b[92;40mS\x1b[39;49mtart", "\x1b[92;40m↑↓\x1b[39;49m Set temperature",
            "E\u001b[92;40mx\u001b[39;49mit");
    }

    private static void OnSousVidePropertyChanged<T>(object sender, KoKoPropertyChangedEventArgs<T> eventArgs) => Render();

}