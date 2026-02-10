using System.ComponentModel;
using Material.Icons;
using XPlayer.Desktop.Models;
using XPlayer.Desktop.Services.Abstractions;
using XPlayer.Lang;

namespace XPlayer.Desktop.ViewModels.Pages;

public class HomeViewModel : Page
{
    public HomeViewModel(IConfigurationService configService, Services.UpdateCheckService updateCheckService) : 
        base("Home", MaterialIconKind.Home)
    {
    }
}