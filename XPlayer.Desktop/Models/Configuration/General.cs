using Newtonsoft.Json;
using ReactiveUI;

namespace XPlayer.Desktop.Models.Configuration;

[JsonObject(MemberSerialization.OptIn)]
public class General : ReactiveObject
{
    [JsonProperty]
    public WindowClosingBehavior ClosingBehavior
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = WindowClosingBehavior.Ask;
    
    [JsonProperty]
    public string? Language
    {
        get => field ?? "English";
        set => this.RaiseAndSetIfChanged(ref field, value ?? "English");
    } = "English";
}