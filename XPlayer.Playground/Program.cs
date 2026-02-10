using XPlayer.Core.Models;
using XPlayer.Core.Network;
using XPlayer.Providers.Jellyfin;

namespace XPlayer.Playground
{
    class Program
    {
        static IMediaServer? _server;

        static async Task Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Title = "XPlayer Playground";

            PrintHeader("XPlayer Media Server Playground");

            // --- Connect ---
            var profile = new ConsoleClientProfile();
            IMediaProvider provider = new JellyfinMediaProvider(profile);

            Console.Write("  服务器地址 (默认 http://localhost:8096): ");
            var url = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(url)) url = "http://localhost:8096";

            Console.Write("  连接中...");
            _server = await provider.ConnectAsync(url);

            if (_server == null || !_server.IsConnected)
            {
                PrintError("连接失败！请检查地址是否正确。");
                return;
            }

            PrintSuccess($"已连接: {_server.Name} (v{_server.Version})");
            Console.WriteLine();

            // --- Login ---
            Console.Write("  用户名: ");
            var username = Console.ReadLine();
            Console.Write("  密码: ");
            var password = ReadPassword();
            Console.WriteLine();

            if (string.IsNullOrWhiteSpace(username))
            {
                PrintError("用户名不能为空。");
                return;
            }

            Console.Write("  认证中...");
            var authOk = await _server.Authentication.AuthenticateByNameAsync(username, password);

            if (!authOk)
            {
                PrintError("认证失败！");
                return;
            }

            PrintSuccess($"认证成功！用户 ID: {_server.Authentication.CurrentUserId}");

            // Display user profile
            var user = _server.Authentication.CurrentUser;
            if (user != null)
            {
                Console.WriteLine($"  用户名:   {user.Name}");
                Console.WriteLine($"  角色:     {user.Policy ?? "N/A"}");
                if (user.AvatarUrl != null)
                    Console.WriteLine($"  头像:     {user.AvatarUrl}{(user.HasPrimaryImage ? "" : " (未设置头像)")}");
                if (user.LastLoginDate.HasValue)
                    Console.WriteLine($"  上次登录: {user.LastLoginDate:yyyy-MM-dd HH:mm}");
                if (user.LastActivityDate.HasValue)
                    Console.WriteLine($"  上次活动: {user.LastActivityDate:yyyy-MM-dd HH:mm}");
            }
            Console.WriteLine();

            // --- Main Loop ---
            await MainMenuLoop();
        }

        static async Task MainMenuLoop()
        {
            while (true)
            {
                PrintHeader("媒体库列表");
                var libraries = (await _server!.Library.GetLibrariesAsync()).ToList();

                for (int i = 0; i < libraries.Count; i++)
                {
                    Console.WriteLine($"  [{i + 1}] {libraries[i].Name}  ({libraries[i].Type})");
                }
                Console.WriteLine($"  [0] 退出");
                Console.WriteLine();

                Console.Write("  选择媒体库: ");
                var input = Console.ReadLine();

                if (input == "0" || string.IsNullOrEmpty(input)) break;
                if (int.TryParse(input, out int libIndex) && libIndex >= 1 && libIndex <= libraries.Count)
                {
                    await BrowseLibrary(libraries[libIndex - 1]);
                }
            }
        }

        static async Task BrowseLibrary(ILibrary library)
        {
            int page = 0;
            const int pageSize = 20;

            while (true)
            {
                PrintHeader($"媒体库: {library.Name} (第 {page + 1} 页)");

                var items = (await _server!.Library.GetItemsAsync(library.Id, skip: page * pageSize, limit: pageSize)).ToList();

                if (items.Count == 0)
                {
                    Console.WriteLine("  (空)");
                }

                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    var extra = item switch
                    {
                        IVideo v => $" [{FormatDuration(v.Duration)}]",
                        ISeries s => $" ({s.SeasonCount} 季, {s.EpisodeCount} 集)",
                        _ => ""
                    };
                    Console.WriteLine($"  [{i + 1}] [{item.Type}] {item.Name}{extra}");
                }

                Console.WriteLine();
                Console.WriteLine("  [n] 下一页  [p] 上一页  [0] 返回");
                Console.Write("  选择: ");
                var input = Console.ReadLine()?.Trim().ToLower();

                if (input == "0" || string.IsNullOrEmpty(input)) break;
                if (input == "n") { page++; continue; }
                if (input == "p") { page = Math.Max(0, page - 1); continue; }

                if (int.TryParse(input, out int idx) && idx >= 1 && idx <= items.Count)
                {
                    var selected = items[idx - 1];

                    // If it's a series/season, drill down into its children
                    if (selected is ISeries || selected.Type == "Season" || selected.Type == "Folder" || selected.Type == "CollectionFolder")
                    {
                        await BrowseFolder(selected.Id, selected.Name);
                    }
                    else
                    {
                        await ShowItemDetail(selected);
                    }
                }
            }
        }

        static async Task BrowseFolder(string parentId, string folderName)
        {
            int page = 0;
            const int pageSize = 20;

            while (true)
            {
                PrintHeader($"{folderName} (第 {page + 1} 页)");

                var items = (await _server!.Library.GetItemsAsync(parentId, skip: page * pageSize, limit: pageSize)).ToList();

                if (items.Count == 0)
                {
                    Console.WriteLine("  (空)");
                }

                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    var extra = item switch
                    {
                        IEpisode e => $" (E{e.Index:D2}) [{FormatDuration(e.Duration)}]",
                        IVideo v => $" [{FormatDuration(v.Duration)}]",
                        ISeason s => $" (第 {s.Index} 季)",
                        ISeries s => $" ({s.SeasonCount} 季)",
                        _ => ""
                    };
                    Console.WriteLine($"  [{i + 1}] [{item.Type}] {item.Name}{extra}");
                }

                Console.WriteLine();
                Console.WriteLine("  [n] 下一页  [p] 上一页  [0] 返回");
                Console.Write("  选择: ");
                var input = Console.ReadLine()?.Trim().ToLower();

                if (input == "0" || string.IsNullOrEmpty(input)) break;
                if (input == "n") { page++; continue; }
                if (input == "p") { page = Math.Max(0, page - 1); continue; }

                if (int.TryParse(input, out int idx) && idx >= 1 && idx <= items.Count)
                {
                    var selected = items[idx - 1];

                    if (selected is ISeries || selected is ISeason || selected.Type == "Folder")
                    {
                        await BrowseFolder(selected.Id, selected.Name);
                    }
                    else
                    {
                        await ShowItemDetail(selected);
                    }
                }
            }
        }

        static async Task ShowItemDetail(IMediaItem item)
        {
            // Fetch full detail
            var detail = await _server!.Library.GetItemAsync(item.Id) ?? item;

            Console.Clear();
            PrintHeader($"详细信息: {detail.Name}");

            Console.WriteLine($"  类型:     {detail.Type}");
            Console.WriteLine($"  ID:       {detail.Id}");
            
            if (detail.ProductionYear.HasValue)
                Console.WriteLine($"  年份:     {detail.ProductionYear}");

            if (!string.IsNullOrEmpty(detail.OfficialRating))
                Console.WriteLine($"  分级:     {detail.OfficialRating}");

            if (detail.CommunityRating.HasValue)
                Console.WriteLine($"  评分:     ⭐ {detail.CommunityRating:F1}/10");

            if (detail.CriticRating.HasValue)
                Console.WriteLine($"  影评评分: 🍅 {detail.CriticRating:F0}%");

            if (detail.PremiereDate.HasValue)
                Console.WriteLine($"  首播日期: {detail.PremiereDate:yyyy-MM-dd}");

            if (detail is IVideo video)
            {
                Console.WriteLine($"  时长:     {FormatDuration(video.Duration)}");
                Console.WriteLine($"  流地址:   {video.StreamUrl}");
            }

            if (detail is ISeries series)
            {
                Console.WriteLine($"  季数:     {series.SeasonCount}");
                Console.WriteLine($"  集数:     {series.EpisodeCount}");
                Console.WriteLine($"  状态:     {series.Status ?? "N/A"}");
            }

            if (detail is IEpisode ep)
            {
                Console.WriteLine($"  集号:     E{ep.Index:D2}");
            }

            // Genres
            if (detail.Genres.Count > 0)
                Console.WriteLine($"  类型:     {string.Join(" / ", detail.Genres)}");

            // Studios
            if (detail.Studios.Count > 0)
                Console.WriteLine($"  制片:     {string.Join(" / ", detail.Studios)}");

            // Overview
            if (!string.IsNullOrEmpty(detail.Overview))
            {
                Console.WriteLine();
                Console.WriteLine($"  简介:");
                var lines = WordWrap(detail.Overview, 70);
                foreach (var line in lines)
                    Console.WriteLine($"    {line}");
            }

            // People (Actors, Directors)
            if (detail.People.Count > 0)
            {
                Console.WriteLine();

                var directors = detail.People.Where(p => p.Type == "Director").ToList();
                if (directors.Count > 0)
                    Console.WriteLine($"  导演:     {string.Join(", ", directors.Select(d => d.Name))}");

                var writers = detail.People.Where(p => p.Type == "Writer").ToList();
                if (writers.Count > 0)
                    Console.WriteLine($"  编剧:     {string.Join(", ", writers.Select(w => w.Name))}");

                var actors = detail.People.Where(p => p.Type == "Actor").ToList();
                if (actors.Count > 0)
                {
                    Console.WriteLine($"  演员:");
                    foreach (var actor in actors.Take(10))
                    {
                        var role = string.IsNullOrEmpty(actor.Role) ? "" : $" (饰 {actor.Role})";
                        Console.WriteLine($"    • {actor.Name}{role}");
                    }
                    if (actors.Count > 10)
                        Console.WriteLine($"    ... 等 {actors.Count} 人");
                }
            }

            // Images
            Console.WriteLine();
            if (!string.IsNullOrEmpty(detail.ImageUrl))
                Console.WriteLine($"  封面:     {detail.ImageUrl}");
            if (!string.IsNullOrEmpty(detail.BackdropImageUrl))
                Console.WriteLine($"  背景图:   {detail.BackdropImageUrl}");

            Console.WriteLine();
            Console.WriteLine("  按任意键返回...");
            Console.ReadKey(true);
        }

        // --- Helpers ---

        static string FormatDuration(TimeSpan ts)
        {
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}h{ts.Minutes:D2}m";
            return $"{ts.Minutes}m{ts.Seconds:D2}s";
        }

        static void PrintHeader(string title)
        {
            Console.Clear();
            var fg = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine();
            Console.WriteLine($"  ═══ {title} ═══");
            Console.WriteLine();
            Console.ForegroundColor = fg;
        }

        static void PrintSuccess(string msg)
        {
            var fg = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($" ✓ {msg}");
            Console.ForegroundColor = fg;
        }

        static void PrintError(string msg)
        {
            var fg = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($" ✗ {msg}");
            Console.ForegroundColor = fg;
            Console.WriteLine("  按任意键退出...");
            Console.ReadKey(true);
        }

        static string ReadPassword()
        {
            var password = new System.Text.StringBuilder();
            ConsoleKeyInfo key;
            while ((key = Console.ReadKey(true)).Key != ConsoleKey.Enter)
            {
                if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password.Remove(password.Length - 1, 1);
                    Console.Write("\b \b");
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    password.Append(key.KeyChar);
                    Console.Write("*");
                }
            }
            return password.ToString();
        }

        static List<string> WordWrap(string text, int maxWidth)
        {
            var lines = new List<string>();
            var words = text.Split(' ');
            var current = "";

            foreach (var word in words)
            {
                if (current.Length + word.Length + 1 > maxWidth)
                {
                    lines.Add(current);
                    current = word;
                }
                else
                {
                    current = string.IsNullOrEmpty(current) ? word : current + " " + word;
                }
            }
            if (!string.IsNullOrEmpty(current))
                lines.Add(current);

            return lines;
        }
    }

    public class ConsoleClientProfile : IClientProfile
    {
        private readonly string _deviceId = Guid.NewGuid().ToString();
        public string ClientName => "XPlayer Playground";
        public string ClientVersion => "1.0.0";
        public string DeviceName => Environment.MachineName;
        public string DeviceId => _deviceId;
    }
}
