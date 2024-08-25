/*
using NodaTime;
using System.Globalization;
using Timer = System.Timers.Timer;

namespace SousVideCtl;

internal static class Program2 {

    private static readonly IDateTimeZoneProvider Olson          = DateTimeZoneProviders.Tzdb;
    private static readonly SystemClock           Clock          = SystemClock.Instance;
    private static readonly IFormatProvider       CurrentCulture = CultureInfo.CurrentCulture;

    private static event EventHandler<ConsoleKeyInfo>? KeyPressed;
    private static DateTimeZone currentZone = Olson.GetSystemDefault();

    private static async Task MainTime() {
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

        using Timer timer = new(500) { AutoReset = true };
        KeyPressed += (_, info) => {
            DateTimeZone? newZone = info.KeyChar switch {
                'p' => Olson["America/Los_Angeles"],
                'm' => Olson["America/Denver"],
                'c' => Olson["America/Chicago"],
                'e' => Olson["America/New_York"],
                _   => null
            };
            if (newZone is not null && newZone != currentZone) {
                timer.Enabled = false;
                currentZone   = newZone;
                Render();
                timer.Enabled = true;
            }
        };

        timer.Elapsed += (_, _) => Render();

        Console.Write("\x1b[?25l"); // hide cursor
        Console.WriteLine("Change time zone: [P]acific [M]ountain [C]entral [E]astern");
        Console.WriteLine("Press Ctrl+C to exit");
        Console.WriteLine();
        Render();
        timer.Enabled = true;
        await cts.Token.Wait();
        Console.Write("\x1b[?25h"); // show cursor
    }

    private static void Render() {
        Console.Write("\r\x1b[K" + Clock.GetCurrentInstant().InZone(currentZone).ToString("M/d/yyyy hh:mm:ss tt x", CurrentCulture));
    }

}

// await cts.Token.Wait();

/*Console.WriteLine("Connecting to Anova Precision Cooker...");
await using ISousVide? sousVide = await AnovaPrecisionCooker.Create();
if (sousVide is null) {
    Console.WriteLine("No Anova Precision Cooker found. Pair an \"Anova\" Bluetooth device (the pairing code is 0000).");
    return 1;
} else {
    Console.WriteLine("Connected");
}

// If you use a CancellationToken or synchronously wait on a SemaphoreSlim rather than awaiting a task, they will block PropertyChanged events from firing
TaskCompletionSource shutdown = new();
Console.CancelKeyPress += (_, eventArgs) => {
    eventArgs.Cancel = true;
    shutdown.TrySetResult();
};

Console.WriteLine($"{(sousVide.IsRunning.Value ? "Running" : "Stopped")}, desired temperature: {sousVide.DesiredTemperature.Value:N1}");
LogState();
sousVide.ActualTemperature.PropertyChanged += (_, _) => LogState();

await shutdown.Task;
return 0;

void LogState() {
    Console.WriteLine($"Current temperature: {sousVide.ActualTemperature.Value:N1}");
}*/

