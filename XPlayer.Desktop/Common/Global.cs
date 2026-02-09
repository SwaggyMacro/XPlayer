using System;
using Microsoft.Extensions.DependencyInjection;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using XPlayer.Desktop.Models;

namespace XPlayer.Desktop.Common;

public class Global
{
    public static IServiceProvider? Services { get; set; }
    public static Config Config { get; set; } = new();
    
    public static ISukiToastManager ToastManager => Services?.GetRequiredService<ISukiToastManager>()
                                                    ?? throw new InvalidOperationException(
                                                        "ToastManager not initialized");
    
    public static ISukiDialogManager DialogManager => Services?.GetRequiredService<ISukiDialogManager>()
                                                      ?? throw new InvalidOperationException(
                                                          "DialogManager not initialized");
}