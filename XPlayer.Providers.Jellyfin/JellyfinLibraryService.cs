using System.Net.Http.Json;
using System.Text.Json;
using XPlayer.Core.Models;
using XPlayer.Core.Network;

namespace XPlayer.Providers.Jellyfin
{
    public class JellyfinLibraryService : ILibraryService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthenticationService _auth;

        public JellyfinLibraryService(HttpClient httpClient, IAuthenticationService auth)
        {
            _httpClient = httpClient;
            _auth = auth;
        }

        public async Task<IEnumerable<ILibrary>> GetLibrariesAsync(CancellationToken cancellationToken = default)
        {
            if (!_auth.IsAuthenticated || _auth.CurrentUserId == null) return Array.Empty<ILibrary>();

            var response = await _httpClient.GetFromJsonAsync<JsonElement>($"Users/{_auth.CurrentUserId}/Views", cancellationToken);
             if (response.TryGetProperty("Items", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                var list = new List<ILibrary>();
                foreach (var item in items.EnumerateArray())
                {
                    list.Add(new LibraryItem(
                        item.GetProperty("Id").GetString() ?? "",
                        item.GetProperty("Name").GetString() ?? "",
                        item.GetProperty("CollectionType").GetString() ?? "unknown"
                    ));
                }
                return list;
            }
            return Array.Empty<ILibrary>();
        }

        public async Task<IEnumerable<IMediaItem>> GetItemsAsync(string parentId, int skip = 0, int limit = 100, CancellationToken cancellationToken = default)
        {
             if (!_auth.IsAuthenticated || _auth.CurrentUserId == null) return Array.Empty<IMediaItem>();

             var query = $"Users/{_auth.CurrentUserId}/Items?ParentId={parentId}&StartIndex={skip}&Limit={limit}&Fields=Overview,Path,MediaSources,People,Genres,Studios,Tags,DateCreated,OfficialRating,CommunityRating,CriticRating,ProductionYear";
             var response = await _httpClient.GetFromJsonAsync<JsonElement>(query, cancellationToken);
             
             if (response.TryGetProperty("Items", out var items) && items.ValueKind == JsonValueKind.Array)
             {
                 var list = new List<IMediaItem>();
                 foreach (var item in items.EnumerateArray())
                 {
                     list.Add(ParseItem(item));
                 }
                 return list;
             }

            return Array.Empty<IMediaItem>();
        }

        public async Task<IMediaItem?> GetItemAsync(string id, CancellationToken cancellationToken = default)
        {
            if (!_auth.IsAuthenticated || _auth.CurrentUserId == null) return null;

            try
            {
                var response = await _httpClient.GetFromJsonAsync<JsonElement>($"Users/{_auth.CurrentUserId}/Items/{id}", cancellationToken);
                return ParseItem(response);
            }
            catch
            {
                return null;
            }
        }

        public async Task<IEnumerable<IMediaItem>> GetResumeItemsAsync(int limit = 12, CancellationToken cancellationToken = default)
        {
            if (!_auth.IsAuthenticated || _auth.CurrentUserId == null) return Array.Empty<IMediaItem>();

            try
            {
                var query = $"Users/{_auth.CurrentUserId}/Items/Resume?Limit={limit}&Fields=Overview,Path,MediaSources,People,Genres,Studios,Tags,DateCreated,OfficialRating,CommunityRating,CriticRating,ProductionYear";
                var response = await _httpClient.GetFromJsonAsync<JsonElement>(query, cancellationToken);
                return ParseItemsResponse(response);
            }
            catch
            {
                return Array.Empty<IMediaItem>();
            }
        }

        public async Task<IEnumerable<IMediaItem>> GetLatestItemsAsync(string? parentId = null, int limit = 16, CancellationToken cancellationToken = default)
        {
            if (!_auth.IsAuthenticated || _auth.CurrentUserId == null) return Array.Empty<IMediaItem>();

            try
            {
                var query = $"Users/{_auth.CurrentUserId}/Items/Latest?Limit={limit}&Fields=Overview,Path,MediaSources,People,Genres,Studios,Tags,DateCreated,OfficialRating,CommunityRating,CriticRating,ProductionYear";
                if (!string.IsNullOrEmpty(parentId))
                    query += $"&ParentId={parentId}";

                var response = await _httpClient.GetFromJsonAsync<JsonElement>(query, cancellationToken);

                // Latest endpoint returns a JSON array directly, not { Items: [...] }
                if (response.ValueKind == JsonValueKind.Array)
                {
                    var list = new List<IMediaItem>();
                    foreach (var item in response.EnumerateArray())
                    {
                        list.Add(ParseItem(item));
                    }
                    return list;
                }

                return ParseItemsResponse(response);
            }
            catch
            {
                return Array.Empty<IMediaItem>();
            }
        }

        public async Task<IEnumerable<IMediaItem>> GetNextUpAsync(int limit = 12, CancellationToken cancellationToken = default)
        {
            if (!_auth.IsAuthenticated || _auth.CurrentUserId == null) return Array.Empty<IMediaItem>();

            try
            {
                var query = $"Shows/NextUp?UserId={_auth.CurrentUserId}&Limit={limit}&Fields=Overview,Path,MediaSources,People,Genres,Studios,Tags,DateCreated,OfficialRating,CommunityRating,CriticRating,ProductionYear";
                var response = await _httpClient.GetFromJsonAsync<JsonElement>(query, cancellationToken);
                return ParseItemsResponse(response);
            }
            catch
            {
                return Array.Empty<IMediaItem>();
            }
        }

        public async Task<IEnumerable<IMediaItem>> SearchAsync(string query, int limit = 50, CancellationToken cancellationToken = default)
        {
            if (!_auth.IsAuthenticated || _auth.CurrentUserId == null) return Array.Empty<IMediaItem>();

            try
            {
                var url = $"Users/{_auth.CurrentUserId}/Items?SearchTerm={Uri.EscapeDataString(query)}&Limit={limit}&Recursive=true&Fields=Overview,Path,MediaSources,People,Genres,Studios,Tags,DateCreated,OfficialRating,CommunityRating,CriticRating,ProductionYear&IncludeItemTypes=Movie,Series,Episode";
                var response = await _httpClient.GetFromJsonAsync<JsonElement>(url, cancellationToken);
                return ParseItemsResponse(response);
            }
            catch
            {
                return Array.Empty<IMediaItem>();
            }
        }

        public async Task<IEnumerable<IMediaItem>> GetPersonItemsAsync(string personName, int limit = 50, CancellationToken cancellationToken = default)
        {
            if (!_auth.IsAuthenticated || _auth.CurrentUserId == null) return Array.Empty<IMediaItem>();

            try
            {
                var url = $"Users/{_auth.CurrentUserId}/Items?PersonIds=&Person={Uri.EscapeDataString(personName)}&Limit={limit}&Recursive=true&Fields=Overview,Path,MediaSources,People,Genres,Studios,Tags,DateCreated,OfficialRating,CommunityRating,CriticRating,ProductionYear&IncludeItemTypes=Movie,Series";
                var response = await _httpClient.GetFromJsonAsync<JsonElement>(url, cancellationToken);
                return ParseItemsResponse(response);
            }
            catch
            {
                return Array.Empty<IMediaItem>();
            }
        }

        /// <summary>
        /// Helper to parse the standard { Items: [...] } response format.
        /// </summary>
        private IEnumerable<IMediaItem> ParseItemsResponse(JsonElement response)
        {
            if (response.TryGetProperty("Items", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                var list = new List<IMediaItem>();
                foreach (var item in items.EnumerateArray())
                {
                    list.Add(ParseItem(item));
                }
                return list;
            }
            return Array.Empty<IMediaItem>();
        }

        private IMediaItem ParseItem(JsonElement item)
        {
            var type = item.GetProperty("Type").GetString();
            var id = item.GetProperty("Id").GetString() ?? "";
            var name = item.GetProperty("Name").GetString() ?? "";
            var overview = item.TryGetProperty("Overview", out var ov) ? ov.GetString() ?? "" : "";
            
            // Image URL
            string? imageUrl = null;
            if (item.TryGetProperty("ImageTags", out var imgTags) && imgTags.TryGetProperty("Primary", out var primaryTag))
            {
                imageUrl = $"{_httpClient.BaseAddress}Items/{id}/Images/Primary?tag={primaryTag.GetString()}&maxWidth=400";
            }

            // Backdrop
            string? backdropUrl = null;
            if (item.TryGetProperty("BackdropImageTags", out var bdTags) && bdTags.ValueKind == JsonValueKind.Array)
            {
                var arr = bdTags.EnumerateArray().ToList();
                if (arr.Count > 0)
                    backdropUrl = $"{_httpClient.BaseAddress}Items/{id}/Images/Backdrop/0?tag={arr[0].GetString()}&maxWidth=960";
            }

            // Duration
            var ticks = item.TryGetProperty("RunTimeTicks", out var rt) ? rt.GetInt64() : 0;
            var duration = TimeSpan.FromTicks(ticks);

            // Stream URL
            string streamUrl = $"{_httpClient.BaseAddress}Videos/{id}/stream?static=true&api_key={_auth.AccessToken}";

            // Metadata
            int? year = item.TryGetProperty("ProductionYear", out var py) ? py.GetInt32() : null;
            float? communityRating = item.TryGetProperty("CommunityRating", out var cr) ? (float)cr.GetDouble() : null;
            float? criticRating = item.TryGetProperty("CriticRating", out var crt) ? (float)crt.GetDouble() : null;
            string? officialRating = item.TryGetProperty("OfficialRating", out var or) ? or.GetString() : null;
            DateTime? premiereDate = item.TryGetProperty("PremiereDate", out var pd) ? pd.GetDateTime() : null;
            DateTime? dateCreated = item.TryGetProperty("DateCreated", out var dc) ? dc.GetDateTime() : null;

            // Genres
            var genres = new List<string>();
            if (item.TryGetProperty("Genres", out var g) && g.ValueKind == JsonValueKind.Array)
                foreach (var genre in g.EnumerateArray())
                    genres.Add(genre.GetString() ?? "");

            // Tags
            var tags = new List<string>();
            if (item.TryGetProperty("Tags", out var t) && t.ValueKind == JsonValueKind.Array)
                foreach (var tag in t.EnumerateArray())
                    tags.Add(tag.GetString() ?? "");

            // Studios
            var studios = new List<string>();
            if (item.TryGetProperty("Studios", out var s) && s.ValueKind == JsonValueKind.Array)
                foreach (var studio in s.EnumerateArray())
                {
                    if (studio.ValueKind == JsonValueKind.Object)
                        studios.Add(studio.GetProperty("Name").GetString() ?? "");
                    else
                        studios.Add(studio.GetString() ?? "");
                }

            // People
            var people = new List<IPerson>();
            if (item.TryGetProperty("People", out var p) && p.ValueKind == JsonValueKind.Array)
            {
                foreach (var person in p.EnumerateArray())
                {
                    var personId = person.TryGetProperty("Id", out var pid) ? pid.GetString() ?? "" : "";
                    var personName = person.GetProperty("Name").GetString() ?? "";
                    var role = person.TryGetProperty("Role", out var r) ? r.GetString() : null;
                    var personType = person.TryGetProperty("Type", out var pt) ? pt.GetString() : null;
                    
                    string? personImg = null;
                    if (person.TryGetProperty("PrimaryImageTag", out var pit) && !string.IsNullOrEmpty(personId))
                        personImg = $"{_httpClient.BaseAddress}Items/{personId}/Images/Primary?tag={pit.GetString()}&maxWidth=200";
                    
                    people.Add(new PersonInfo(personName, role, personType, personImg));
                }
            }

            // Played percentage from UserData
            double? playedPercentage = null;
            if (item.TryGetProperty("UserData", out var userData))
            {
                if (userData.TryGetProperty("PlayedPercentage", out var pp))
                    playedPercentage = pp.GetDouble();
            }

            var meta = new MediaMetadata(backdropUrl, year, communityRating, criticRating, officialRating, genres, tags, studios, people, premiereDate, dateCreated, playedPercentage);

            return type switch
            {
                "Movie" => new MovieItem(id, name, overview, imageUrl, meta, duration, streamUrl),
                "Series" => new SeriesItem(id, name, overview, imageUrl, meta,
                    item.TryGetProperty("ChildCount", out var sc) ? sc.GetInt32() : 0,
                    item.TryGetProperty("RecursiveItemCount", out var ec) ? ec.GetInt32() : 0,
                    item.TryGetProperty("Status", out var st) ? st.GetString() : null),
                "Season" => new SeasonItem(id, name, overview, imageUrl, meta,
                    item.TryGetProperty("IndexNumber", out var si) ? si.GetInt32() : 0,
                    item.TryGetProperty("SeriesId", out var sid) ? sid.GetString() ?? "" : ""),
                "Episode" => new EpisodeItem(id, name, overview, imageUrl, meta, duration, streamUrl,
                    item.TryGetProperty("IndexNumber", out var ei) ? ei.GetInt32() : 0,
                    item.TryGetProperty("SeasonId", out var seid) ? seid.GetString() ?? "" : "",
                    item.TryGetProperty("SeriesId", out var srid) ? srid.GetString() ?? "" : "",
                    item.TryGetProperty("SeriesName", out var sname) ? sname.GetString() : null),
                _ => new BaseItem(id, name, overview, imageUrl, meta, type ?? "Unknown")
            };
        }
    }

    // --- Metadata wrapper ---
    public record MediaMetadata(
        string? BackdropImageUrl,
        int? ProductionYear,
        float? CommunityRating,
        float? CriticRating,
        string? OfficialRating,
        IReadOnlyList<string> Genres,
        IReadOnlyList<string> Tags,
        IReadOnlyList<string> Studios,
        IReadOnlyList<IPerson> People,
        DateTime? PremiereDate,
        DateTime? DateCreated,
        double? PlayedPercentage
    );

    public record PersonInfo(string Name, string? Role, string? Type, string? ImageUrl) : IPerson;

    // --- Record types ---
    public record LibraryItem(string Id, string Name, string Type) : ILibrary;

    public record BaseItem(string Id, string Name, string Overview, string? ImageUrl, MediaMetadata Meta, string Type) : IMediaItem
    {
        public string? BackdropImageUrl => Meta.BackdropImageUrl;
        public int? ProductionYear => Meta.ProductionYear;
        public float? CommunityRating => Meta.CommunityRating;
        public float? CriticRating => Meta.CriticRating;
        public string? OfficialRating => Meta.OfficialRating;
        public IReadOnlyList<string> Genres => Meta.Genres;
        public IReadOnlyList<string> Tags => Meta.Tags;
        public IReadOnlyList<string> Studios => Meta.Studios;
        public IReadOnlyList<IPerson> People => Meta.People;
        public DateTime? PremiereDate => Meta.PremiereDate;
        public DateTime? DateCreated => Meta.DateCreated;
        public double? PlayedPercentage => Meta.PlayedPercentage;
    }
    
    public record MovieItem(string Id, string Name, string Overview, string? ImageUrl, MediaMetadata Meta, TimeSpan Duration, string? StreamUrl) : IVideo
    {
        public string Type => "Movie";
        public string? BackdropImageUrl => Meta.BackdropImageUrl;
        public int? ProductionYear => Meta.ProductionYear;
        public float? CommunityRating => Meta.CommunityRating;
        public float? CriticRating => Meta.CriticRating;
        public string? OfficialRating => Meta.OfficialRating;
        public IReadOnlyList<string> Genres => Meta.Genres;
        public IReadOnlyList<string> Tags => Meta.Tags;
        public IReadOnlyList<string> Studios => Meta.Studios;
        public IReadOnlyList<IPerson> People => Meta.People;
        public DateTime? PremiereDate => Meta.PremiereDate;
        public DateTime? DateCreated => Meta.DateCreated;
        public double? PlayedPercentage => Meta.PlayedPercentage;
    }

    public record SeriesItem(string Id, string Name, string Overview, string? ImageUrl, MediaMetadata Meta, int SeasonCount, int EpisodeCount, string? Status) : ISeries
    {
        public string Type => "Series";
        public string? BackdropImageUrl => Meta.BackdropImageUrl;
        public int? ProductionYear => Meta.ProductionYear;
        public float? CommunityRating => Meta.CommunityRating;
        public float? CriticRating => Meta.CriticRating;
        public string? OfficialRating => Meta.OfficialRating;
        public IReadOnlyList<string> Genres => Meta.Genres;
        public IReadOnlyList<string> Tags => Meta.Tags;
        public IReadOnlyList<string> Studios => Meta.Studios;
        public IReadOnlyList<IPerson> People => Meta.People;
        public DateTime? PremiereDate => Meta.PremiereDate;
        public DateTime? DateCreated => Meta.DateCreated;
        public double? PlayedPercentage => Meta.PlayedPercentage;
    }

    public record SeasonItem(string Id, string Name, string Overview, string? ImageUrl, MediaMetadata Meta, int Index, string SeriesId) : ISeason
    {
        public string Type => "Season";
        public string? BackdropImageUrl => Meta.BackdropImageUrl;
        public int? ProductionYear => Meta.ProductionYear;
        public float? CommunityRating => Meta.CommunityRating;
        public float? CriticRating => Meta.CriticRating;
        public string? OfficialRating => Meta.OfficialRating;
        public IReadOnlyList<string> Genres => Meta.Genres;
        public IReadOnlyList<string> Tags => Meta.Tags;
        public IReadOnlyList<string> Studios => Meta.Studios;
        public IReadOnlyList<IPerson> People => Meta.People;
        public DateTime? PremiereDate => Meta.PremiereDate;
        public DateTime? DateCreated => Meta.DateCreated;
        public double? PlayedPercentage => Meta.PlayedPercentage;
    }

    public record EpisodeItem(string Id, string Name, string Overview, string? ImageUrl, MediaMetadata Meta, TimeSpan Duration, string? StreamUrl, int Index, string SeasonId, string SeriesId, string? SeriesName = null) : IEpisode
    {
        public string Type => "Episode";
        public string? BackdropImageUrl => Meta.BackdropImageUrl;
        public int? ProductionYear => Meta.ProductionYear;
        public float? CommunityRating => Meta.CommunityRating;
        public float? CriticRating => Meta.CriticRating;
        public string? OfficialRating => Meta.OfficialRating;
        public IReadOnlyList<string> Genres => Meta.Genres;
        public IReadOnlyList<string> Tags => Meta.Tags;
        public IReadOnlyList<string> Studios => Meta.Studios;
        public IReadOnlyList<IPerson> People => Meta.People;
        public DateTime? PremiereDate => Meta.PremiereDate;
        public DateTime? DateCreated => Meta.DateCreated;
        public double? PlayedPercentage => Meta.PlayedPercentage;
    }
}
