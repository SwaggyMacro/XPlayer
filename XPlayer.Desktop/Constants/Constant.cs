using System;
using System.IO;

namespace XPlayer.Desktop.Constants;

public static class Constant
{
    public const string General = "General";
    
#if DEBUG
    public static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config");
#else
    public static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "config");
#endif
}