/*
 * 実行には C# 環境が必要です。
 * _token 変数を書き換えて実行してください。
 *
 * サンプル初期実装
 * 移動先に資源が無い回収車を、ランダムに選んだ出現中の資源へと移動させる
 * ただしこのとき２台以上の回収車が同じ資源を選ばないようにする
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace cs
{
    internal readonly struct AgentMove
    {
        [JsonPropertyName("x")]
        public double X { get; }
        [JsonPropertyName("y")]
        public double Y { get; }
        [JsonPropertyName("t")]
        public int T { get; }
        [JsonConstructor]
        public AgentMove(double x, double y, int t) => (X, Y, T) = (x, y, t);
    }

    internal readonly struct Agent
    {
        [JsonPropertyName("move")]
        public AgentMove[] AgentMove { get;  }
        [JsonConstructor]
        public Agent(AgentMove[] agentMove) => AgentMove = agentMove;
    }

    internal readonly struct Resource
    {
        [JsonPropertyName("id")]
        public int Id { get; }
        [JsonPropertyName("x")]
        public int X { get; }
        [JsonPropertyName("y")]
        public int Y { get; }
        [JsonPropertyName("t0")]
        public int T0 { get; }
        [JsonPropertyName("t1")]
        public int T1 { get; }
        [JsonPropertyName("type")]
        public string Type { get; }
        [JsonPropertyName("weight")]
        public int Weight { get; }
        [JsonConstructor]
        public Resource(int id, int x, int y, int t0, int t1, string type, int weight) => (Id, X, Y, T0, T1, Type, Weight) = (id, x, y, t0, t1, type, weight);
    }

    internal readonly struct ResourceWithAmount
    {
        [JsonPropertyName("id")]
        public int Id { get; }
        [JsonPropertyName("x")]
        public int X { get; }
        [JsonPropertyName("y")]
        public int Y { get; }
        [JsonPropertyName("t0")]
        public int T0 { get; }
        [JsonPropertyName("t1")]
        public int T1 { get; }
        [JsonPropertyName("type")]
        public string Type { get; }
        [JsonPropertyName("weight")]
        public int Weight { get; }
        [JsonPropertyName("amount")]
        public double Amount { get; }
        [JsonConstructor]
        public ResourceWithAmount(int id, int x, int y, int t0, int t1, string type, int weight, double amount) => (Id, X, Y, T0, T1, Type, Weight, Amount) = (id, x, y, t0, t1, type, weight, amount);
    }

    internal readonly struct OwnedResource
    {
        [JsonPropertyName("type")]
        public string Type { get; }
        [JsonPropertyName("amount")]
        public double Amount { get; }
        [JsonConstructor]
        public OwnedResource(string type, double amount) => (Type, Amount) = (type, amount);
    }

    internal readonly struct Game
    {
        [JsonPropertyName("status")]
        public string Status { get; }
        [JsonPropertyName("now")]
        public int Now { get; }
        [JsonPropertyName("agent")]
        public Agent[] Agent { get; }
        [JsonPropertyName("resource")]
        public Resource[] Resource { get; }
        [JsonPropertyName("next_resource")]
        public int NextResource { get; }
        [JsonPropertyName("owned_resource")]
        public OwnedResource[] OwnedResource { get; }
        [JsonConstructor]
        public Game(string status, int now, Agent[] agent, Resource[] resource, int nextResource, OwnedResource[] ownedResource) => (Status, Now, Agent, Resource, NextResource, OwnedResource) = (status, now, agent, resource, nextResource, ownedResource);
    }

    internal readonly struct Move
    {
        [JsonPropertyName("status")]
        public string Status { get; }
        [JsonPropertyName("now")]
        public int Now { get; }
        [JsonPropertyName("move")]
        public AgentMove[] AgentMove { get; }
        [JsonConstructor]
        public Move(string status, int now, AgentMove[] agentMove) => (Status, Now, AgentMove) = (status, now, agentMove);
    }

    internal readonly struct Resources
    {
        [JsonPropertyName("status")]
        public string Status { get; }
        [JsonPropertyName("resource")]
        public ResourceWithAmount[] Resource { get; }
        [JsonConstructor]
        public Resources(string status, ResourceWithAmount[] resource) => (Status, Resource) = (status, resource);
    }

    internal class Program
    {
        private static readonly HttpClient Client = new HttpClient();

        private readonly string _gameServer;
        private readonly string _token;

        private async Task<byte[]> CallApi(string x)
        {
            var url = $"{_gameServer}{x}";
            Console.WriteLine(url);
            var res = await Client.GetAsync(url);
            return await res.Content.ReadAsByteArrayAsync();
        }

        async Task<Game> CallGame()
        {
            var json = await CallApi($"/api/game/{_token}");
            return JsonSerializer.Deserialize<Game>(json);
        }

        async Task<Move> CallMove(int index, int x, int y)
        {
            var json = await CallApi($"/api/move/{_token}/{index}-{x}-{y}");
            return JsonSerializer.Deserialize<Move>(json);
        }

        async Task<Move> CallWillMove(int index, int x, int y, int t)
        {
            var json = await CallApi($"/api/will_move/{_token}/{index}-{x}-{y}-{t}");
            return JsonSerializer.Deserialize<Move>(json);
        }

        private async Task<Resources> CallResources(IEnumerable<int> ids)
        {
            var idStr = string.Join("-", ids.Select(x => x.ToString()));
            var json = await CallApi($"/api/resources/{_token}/{idStr}");
            return JsonSerializer.Deserialize<Resources>(json);
        }

        private static double CalcScore(Game game)
        {
            var a = game.OwnedResource.Select(o => o.Amount).OrderBy(x => x).ToArray();
            return a[0] + 0.1 * a[1] + 0.01 * a[2];
        }

        private static async Task Main(string[] args)
        {
            await new Program().Solve();
        }

        private async Task Solve()
        {
            var random = new Random();
            for (;;)
            {
                var game = await CallGame();
                if (game.Status != "ok")
                {
                    Console.WriteLine(game.Status);
                    break;
                }

                foreach (var o in game.OwnedResource)
                {
                    Console.Write($"{o.Type}: {o.Amount:F2} ");
                }

                Console.WriteLine($"Score: {CalcScore(game):F2}");

                var resourcePositions = new HashSet<(int, int)>();
                foreach (var r in game.Resource)
                {
                    if (r.T0 <= game.Now && game.Now < r.T1)
                    {
                        resourcePositions.Add((r.X, r.Y));
                    }
                }

                var indexList = new List<int>();
                for (var index = 1; index <= 5; index++)
                {
                    var moves = game.Agent[index - 1].AgentMove;
                    var m = moves[moves.Length - 1];
                    if (!resourcePositions.Remove(((int) m.X, (int) m.Y)))
                    {
                        indexList.Add(index);
                    }
                }

                foreach (var index in indexList)
                {
                    if (resourcePositions.Count == 0) break;
                    var a = new List<(int, int)>(resourcePositions);
                    var (x, y) = a[random.Next() % a.Count];
                    await CallMove(index, x, y);
                    resourcePositions.Remove((x, y));
                }

                await Task.Delay(1000);
            }
        }

        private Program()
        {
            _gameServer = Environment.GetEnvironmentVariable("GAME_SERVER") ?? "https://contest.2021-autumn.gbc.tenka1.klab.jp";
            // _gameServer = "https://contest.2021-autumn.gbc.tenka1.klab.jp/staging"; // 開発用環境
            _token = Environment.GetEnvironmentVariable("TOKEN") ?? "YOUR_TOKEN";
        }
    }
}
