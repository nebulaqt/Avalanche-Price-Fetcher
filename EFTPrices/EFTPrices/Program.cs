using System;
using System.Linq;
using System.IO;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace EFTPrices
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            string[] validItemTypes = { "ammo", "ammoBox", "any", "armor", "backpack", "barter", "container", "glasses", "grenade", "gun", "headphones", "helmet", "injectors", "keys", "markedOnly", "meds", "mods", "noFlea", "pistolGrip", "preset", "provisions", "rig", "suppressor", "wearable" };
            Console.WriteLine("Which item types do you want to retrieve (comma-separated list)?: ammo, ammoBox, any, armor, backpack, barter, container, glasses, grenade, gun, headphones, helmet, injectors, keys, markedOnly, meds, mods, noFlea, pistolGrip, preset, provisions, rig, suppressor, wearable");
            string itemTypes = Console.ReadLine();

            if (!itemTypes.Split(',').All(validItemTypes.Contains))
            {
                Console.WriteLine("Invalid item types specified. valid item types :" + string.Join(",", validItemTypes.ToString()));
                return;
            }

            // Make the GraphQL API request
            string query = "query GetItems { Items: items(types: [" + itemTypes + "]) { id name avg24hPrice sellFor{ price source } } }";
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.tarkov.dev/graphql");
            request.Content = new StringContent(JsonConvert.SerializeObject(new { query }), Encoding.UTF8, "application/json");
            var client = new HttpClient();

            try
            {
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                var outputList = json["data"]["Items"]
                .Select(item => new
                {
                    id = item["id"],
                    highestPrice = item["sellFor"].Select(i => (int)i["price"]).Concat(new[] { (int)item["avg24hPrice"] }).Max()
                })
                .OrderByDescending(item => item.highestPrice)
                .ToList();
                var stopwatch = new System.Diagnostics.Stopwatch();
                stopwatch.Start();

                using (var outFile = new StreamWriter("prices.txt"))
                {
                    foreach (var item in outputList)
                    {
                        outFile.WriteLine("set_price " + item.id + " " + item.highestPrice);
                    }
                }

                stopwatch.Stop();
                Console.WriteLine("elapsed time: " + stopwatch.Elapsed.TotalSeconds + " sec");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }
}



