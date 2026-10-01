using Discord;
using Discord.Rest;
using Google.Apis.Sheets.v4;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4.Data;

namespace MoviePoll.Bot {

    public enum Months { OCTOBER, NOVEMBER, DECEMBER, OUT_OF_SCOPE };
    public class MoviePollBot {
        private static Dictionary<string, string> envVars = [];
        
         static void Main(string[] args) {
            LoadEnvVars();
            var movieOptions = LoadSpreadsheetData().GetAwaiter().GetResult();
            SendMessageAsync(movieOptions).GetAwaiter().GetResult();
        }

        private static void LoadEnvVars()
        {
            var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")
                ?? throw new InvalidOperationException("DISCORD_TOKEN is not set");
                envVars["token"] = token;
            var channelIdText = Environment.GetEnvironmentVariable("DISCORD_CHANNEL_ID")
                ?? throw new InvalidOperationException("DISCORD_CHANNEL_ID is not set");
                envVars["channelIdText"] = channelIdText;
            var spreadsheetId = Environment.GetEnvironmentVariable("SPREADSHEET_ID")
                ?? throw new InvalidOperationException("SPREADSHEET_ID is not set");
                envVars["spreadsheetId"] = spreadsheetId;
        }

        public static async Task SendMessageAsync(List<string> movieOptions) {
            using var client = new DiscordRestClient();
            await client.LoginAsync(TokenType.Bot, envVars["token"]);
        
            var channelId = ulong.Parse(envVars["channelIdText"]);
            var channel = await client.GetChannelAsync(channelId) as IMessageChannel
                ?? throw new InvalidOperationException($"Channel {channelId} not found or is not a text channel");

            var moviePoll = BuildPoll(movieOptions);

            await channel.SendMessageAsync(text:"Weekly Movie Poll", poll:moviePoll);

            await client.LogoutAsync();
        }

        public static PollProperties BuildPoll(List<string> movieOptions)
        {
            return new PollProperties
            {
                Question = new PollMediaProperties { Text = "Which movie(s) do you want to watch?" },
                Answers = movieOptions.Select(m => new PollMediaProperties { Text = m }).ToList(),
                AllowMultiselect = true,
                Duration = 48, // hours
                LayoutType = PollLayout.Default
            };
        }

        public static async Task<List<string>> LoadSpreadsheetData()
        {
            List<string> movieOptions = [];
            try
            {
                var serviceAccountCredential = CredentialFactory
                    .FromJson<ServiceAccountCredential>(Environment.GetEnvironmentVariable("GOOGLE_SHEETS_ACCESS"));
                if(serviceAccountCredential == null)
                {
                    return movieOptions;
                }
                
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
                var currentMonth = fullMonthName.ToUpper() switch
                {
                    "OCTOBER" => Months.OCTOBER,
                    "NOVEMBER" => Months.NOVEMBER,
                    "DECEMBER" => Months.DECEMBER,
                    _ => Months.OUT_OF_SCOPE,
                };

                if (values != null && currentMonth != Months.OUT_OF_SCOPE && values.Count > 0)
                {
                    var currentOptions = values
                        .Select(row => row.ElementAtOrDefault((int)currentMonth)?.ToString())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToList();
                    
                    currentOptions.ForEach(option => { 
                        if (!string.IsNullOrWhiteSpace(option)) {
                                movieOptions.Add(option);
                        }           
                    });

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

            return movieOptions;
        }
    }
}
