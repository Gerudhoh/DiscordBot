using System;
using System.Linq;
using Discord;
using Discord.Rest;
using Google.Apis.Sheets.v4;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4.Data;

namespace MoviePoll.Bot {
    public class MoviePollBot {
        private static Dictionary<string, string> envVars = [];
        
         static void Main(string[] args) {
            LoadEnvVars();
            LoadSpreadsheetData().GetAwaiter().GetResult();
            // SendMessageAsync().GetAwaiter().GetResult();
            // Console.WriteLine("Sent hello world message.");
        }

        private static void LoadEnvVars()
        {
            if (File.Exists("secrets/.env")) DotNetEnv.Env.Load("secrets/.env");
            var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")
                ?? throw new InvalidOperationException("DISCORD_TOKEN is not set");
                envVars["token"] = token;
            var channelIdText = Environment.GetEnvironmentVariable("DISCORD_CHANNEL_ID")
                ?? throw new InvalidOperationException("DISCORD_CHANNEL_ID is not set");
                envVars["channelIdText"] = channelIdText;
            var spreadsheetId = Environment.GetEnvironmentVariable("SPREADSHEET_ID")
                ?? throw new InvalidOperationException("SPREADSHEET_ID is not set");
                envVars["spreadsheetId"] = spreadsheetId;
            var gcpTokenFilename = Environment.GetEnvironmentVariable("GCP_JSON") 
                ?? throw new InvalidOperationException("SPREADSHEET_ID is not set");
                envVars["gcpTokenFilename"] = gcpTokenFilename;
        }

        public static async Task SendMessageAsync() {
            using var client = new DiscordRestClient();
            await client.LoginAsync(TokenType.Bot, envVars["token"]);
        
            var channelId = ulong.Parse(envVars["channelIdText"]);
            var channel = await client.GetChannelAsync(channelId) as IMessageChannel
                ?? throw new InvalidOperationException($"Channel {channelId} not found or is not a text channel");

            await channel.SendMessageAsync("Hello, world!");

            await client.LogoutAsync();
        }

        public static async Task LoadSpreadsheetData()
        {
            try
            {
                var serviceAccountCredential = CredentialFactory.FromFile<ServiceAccountCredential>($"secrets/{envVars["gcpTokenFilename"]}");
                GoogleCredential credential = serviceAccountCredential.ToGoogleCredential();
                 var service = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Google Sheets Reader"
                });
                string range = "Sheet1!F2:H";
                SpreadsheetsResource.ValuesResource.GetRequest request =
                    service.Spreadsheets.Values.Get(envVars["spreadsheetId"], range);

                ValueRange response = await request.ExecuteAsync();
                IList<IList<object>> values = response.Values;

                string fullMonthName = DateTime.Now.ToString("MMMM"); 


                if (values != null && values.Count > 0)
                {
                    var currentOptions = values
                        .Select(row => row.ElementAtOrDefault(1)?.ToString())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToList();
                }
                else
                {
                    Console.WriteLine("No data found in the specified range.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred: {ex.Message}");
            }
        }
    }
}
