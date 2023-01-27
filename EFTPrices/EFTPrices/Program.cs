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
            // Declare an array of valid item types that can be retrieved from the API
            string[] validItemTypes = { "ammo", "ammoBox", "any", "armor", "backpack", "barter", "container", "glasses", "grenade", "gun", "headphones", "helmet", "injectors", "keys", "markedOnly", "meds", "mods", "noFlea", "pistolGrip", "preset", "provisions", "rig", "suppressor", "wearable" };

            // Prompt the user to input a comma-separated list of item types they wish to retrieve
            Console.WriteLine("What types of items do you want to retrieve? (Please enter a comma-separated list) The options are: ammo, ammoBox, any, armor, backpack, barter, container, glasses, grenade, gun, headphones, helmet, injectors, keys, markedOnly, meds, mods, noFlea, pistolGrip, preset, provisions, rig, suppressor, wearable");
            string itemTypes = Console.ReadLine();

            // Check if the input item types are valid
            if (!itemTypes.Split(',').All(validItemTypes.Contains))
            {
                Console.WriteLine("Invalid item types specified. valid item types: ammo, ammoBox, any, armor, backpack, barter, container, glasses, grenade, gun, headphones, helmet, injectors, keys, markedOnly, meds, mods, noFlea, pistolGrip, preset, provisions, rig, suppressor, wearable");
                Console.ReadKey();
                return;
            }

            // Construct a GraphQL query using the input item types
            string query = "query GetItems { Items: items(types: [" + itemTypes + "]) { id name avg24hPrice sellFor{ price source } } }";

            // Send an HTTP request to the API with the query
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.tarkov.dev/graphql");
            request.Content = new StringContent(JsonConvert.SerializeObject(new { query }), Encoding.UTF8, "application/json");
            var client = new HttpClient();

            try
            {
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                // Deserialize and parse the response from the API into a JObject
                var json = JObject.Parse(await response.Content.ReadAsStringAsync());

                // Select the desired information from the response and order it by highest price
                var outputList = json["data"]["Items"]
                .Select(item => new
                {
                    id = item["id"],
                    highestPrice = item["sellFor"].Select(i => (int)i["price"]).Concat(new[] { (int)item["avg24hPrice"] }).Max()
                })
                .OrderByDescending(item => item.highestPrice)
                .ToList();

                // Start a stopwatch to measure the elapsed time of the process
                var stopwatch = new System.Diagnostics.Stopwatch();
                stopwatch.Start();
                // Create a new StreamWriter object to write to the file "prices.txt"
                using (var outFile = new StreamWriter("prices.txt"))
                {
                    // Iterate through each item in the outputList
                    foreach (var item in outputList)
                    {
                        // Write a string "set_price " followed by the item's id and highestPrice to the file
                        outFile.WriteLine("set_price " + item.id + " " + item.highestPrice);
                    }
                }

                // Stop the stopwatch and print the elapsed time in seconds
                stopwatch.Stop();
                Console.WriteLine("elapsed time: " + stopwatch.Elapsed.TotalSeconds + " sec");
                Console.ReadKey();
            }
            // If there is any exception while sending the HTTP request, catch it and print the error message
            catch (HttpRequestException ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }
}
