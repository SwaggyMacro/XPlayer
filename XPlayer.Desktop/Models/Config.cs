using System.Diagnostics.CodeAnalysis;
using XPlayer.Desktop.Models.Configuration;

namespace XPlayer.Desktop.Models;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class Config
{
    public General GeneralConf { get; set; } = new();
}