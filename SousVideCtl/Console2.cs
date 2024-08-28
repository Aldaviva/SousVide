namespace SousVideCtl;

public static class Console2 {

    /// <summary>
    /// Clear screen and move to the top-left position
    /// </summary>
    public static void Clear() {
        Console.Write("\x1b[1J\x1b[1;1H");
    }

    public static string Color(Colors? foregroundColor, Colors? backgroundColor) {
        bool hasForegroundAndBackground = foregroundColor != null && backgroundColor != null;
        return $"\x1b[{(int?) foregroundColor:D}{(hasForegroundAndBackground ? ";" : "")}{(int?) backgroundColor + 10:D}m";
    }

    public static string ResetColor { get; } = Color(Colors.Default, Colors.Default);

    public static void SetCursorVisibility(bool visible) {
        Console.Write(visible ? "\x1b[?25h" : "\x1b[?25l");
    }

    /// <summary>
    /// See <see href="https://en.wikipedia.org/wiki/ANSI_escape_code#3-bit_and_4-bit"/>
    /// </summary>
    public enum Colors {

        Black = 30,
        Red,
        Green,
        Yellow,
        Blue,
        Magenta,
        Cyan,
        LightGray,
        Default = 39,
        Gray    = 90,
        BrightRed,
        BrightGreen,
        BrightYellow,
        BrightBlue,
        BrightMagenta,
        BrightCyan,
        White

    }

}