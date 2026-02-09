using System;
using System.Collections.Specialized;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using XPlayer.Desktop.Common;
using XPlayer.Desktop.Constants;
using XPlayer.Desktop.Models.Configuration;
using XPlayer.Desktop.Services.Abstractions;

namespace XPlayer.Desktop.Services;

public class ConfigurationService : ReactiveObject, IConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;
    
    public General General { get; }
    public ConfigurationService(ILogger<ConfigurationService> logger)
    {
        _logger = logger;
        _logger.LogInformation("Loading configurations...");
        
        // Load configurations
        General = ConfigUtil.LoadConfig<General>(Constant.General);

        // Set global access for legacy compatibility (if needed)
        Global.Config.GeneralConf = General;
        
        // Setup Auto-Save Subscriptions
        SetupAutoSave();
        
        _logger.LogInformation("Configurations loaded successfully");
    }

    private void SetupAutoSave()
    {
        // General Config - Simple Property Changes
        General.Changed
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Subscribe(_ =>
            {
                _logger.LogDebug("Auto-saving General configuration");
                ConfigUtil.SaveConfig(Constant.General, General);
            });
    }
}