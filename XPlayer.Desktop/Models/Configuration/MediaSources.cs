using System.Collections.ObjectModel;
using Newtonsoft.Json;
using ReactiveUI;

namespace XPlayer.Desktop.Models.Configuration;

[JsonObject(MemberSerialization.OptIn)]
public class MediaSources : ReactiveObject
{
    [JsonProperty]
    public ObservableCollection<MediaSourceConfig> Sources { get; set; } = new();
}
