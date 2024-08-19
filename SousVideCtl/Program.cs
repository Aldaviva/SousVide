using SousVide;

Console.WriteLine("Connecting to Anova Precision Cooker...");
using ISousVide? sousVide = await AnovaPrecisionCooker.Create();
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
}