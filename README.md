# Movie Poll Bot

A small C# console app that posts a weekly movie poll to a Discord channel. It reads candidate movies from a Google Sheet and runs on a GitHub Actions cron schedule, so there is no server to host.

## How it works

1. GitHub Actions runs the app every Sunday in October through December (`0 16 * 10-12 0`, 16:00 UTC).
2. The app reads `Sheet1!F2:H` from the spreadsheet. The three columns are the October, November and December movies.
3. It takes the column for the current month (UTC) and drops empty cells.
4. It posts to the Discord channel:
   - 2 or more movies: a native poll with up to 10 answers (the first 10 in sheet order), multiselect, open for 48 hours.
   - 0 or 1 movies: a plain message, `We're watching: <movie>`.
5. Outside October to December the job is not run

## Project layout

```
src/MoviePollBot/          Console app (net9.0), all logic in Program.cs
tests/MoviePollBot.Tests/  xUnit tests for BuildPoll and GetMonthsMovies
.github/workflows/main.yml Scheduled workflow
```

## Configuration

The app reads these environment variables and fails fast if any of the first three are missing:

| Variable | Purpose |
| --- | --- |
| `DISCORD_TOKEN` | Bot token from the Discord Developer Portal |
| `DISCORD_CHANNEL_ID` | ID of the channel to post in |
| `SPREADSHEET_ID` | ID of the Google Sheet |
| `GOOGLE_SHEETS_ACCESS` | Full service account JSON, with read access to the sheet |

### One-time setup

- **Discord:** create an application, add a bot, and invite it with the `Send Messages` and `Create Polls` permissions.
- **Google:** create a service account, enable the Sheets API, and share the sheet with the service account's email as Viewer.

## Running locally

```bash
export DISCORD_TOKEN=...
export DISCORD_CHANNEL_ID=...
export SPREADSHEET_ID=...
export GOOGLE_SHEETS_ACCESS='{"type": "service_account", ...}'
dotnet run --project src/MoviePollBot
```

This posts a real message, so point it at a test channel first. The workflow can also be started manually from the Actions tab with `workflow_dispatch`.

## Tests

```bash
dotnet test tests/MoviePollBot.Tests
```
