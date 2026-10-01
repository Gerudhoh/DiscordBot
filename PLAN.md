# Weekly Movie Poll Discord Bot — Design Plan

## Context
Learning project: the user wants to practice C# by writing a Discord bot themselves. Every Sunday from Oct 4 to Dec 13, 2026 (11 Sundays), it reads a Google Sheet of candidate movies (3 columns) and posts a poll to a specific Discord channel. The user deletes or strikes out movies already watched, and those must be skipped. Scheduling runs on GitHub Actions cron (free). **The user writes the code; Claude guides and reviews.** The repo is currently empty.

## Architecture: short-lived console app, not a long-running bot
GitHub Actions cron -> `dotnet run` -> read sheet -> pick movies -> log in to Discord, post poll, exit. No hosting, no always-on process.

```
DiscordBot/
  .github/workflows/weekly-poll.yml
  src/MoviePollBot/
    MoviePollBot.csproj          (net9.0 console)
    Program.cs                   (wiring: config -> sheet -> poll)
    Config.cs                    (env vars: token, channel id, sheet id, creds)
    SheetReader.cs               (Google Sheets -> List<string> of unwatched movies)
    MoviePicker.cs               (pure logic: shuffle/pick up to N; easy to unit test)
    PollPoster.cs                (Discord.Net: login, send poll, logout)
  tests/MoviePollBot.Tests/      (xUnit: MoviePicker, date-window guard)
```
Keep `MoviePicker` and the date check pure (no I/O) so the user can practice unit testing.

## Key design decisions
1. **Library: Discord.Net.** Most tutorials, and it fits a login, post, exit app (`DiscordRestClient` is enough, with no gateway connection needed). Native polls need a recent version (3.17+ to my recollection; **verify** on NuGet). If they aren't supported, fall back to an embed with number-emoji reactions.
2. **Poll type:** native Discord poll. Limits: up to 10 answers, question up to 300 chars, duration in hours (e.g. 24-48h). Use fallback embed+reactions only if the library lacks support.
3. **Strikethrough detection forces the Sheets API.** Published-CSV and `values.get` return text only, with no formatting. Use `spreadsheets.get` with `includeGridData=true` and a `fields` mask (`sheets.data.rowData.values(formattedValue,effectiveFormat.textFormat.strikethrough)`). A movie is **skipped** if the cell is empty or `strikethrough == true`. Deleted entries are just empty cells.
   - Auth: Google Cloud service account (Sheets API enabled), sheet shared with the service account's email as Viewer. NuGet: `Google.Apis.Sheets.v4`.
4. **Movie selection:** the 3 columns are October, November and December movies. Pick the column from the current month (UTC date: Oct -> col A, Nov -> col B, Dec -> col C). Drop skipped cells (empty or struck out), shuffle, take up to 10. If fewer than 2 remain, don't post and fail the job loudly. `MoviePicker` takes the month and the grid, so the column mapping is unit-testable. Prefer matching by the header row text ("October" etc.) over hardcoded column indexes, if the sheet has headers.
**skipped** shuffle; out of scope for a simple project.
5. **Scheduling window:** cron has no year or range. Use `cron: '0 15 * * 0'` (Sundays 15:00 UTC, which is 11am EDT, and GitHub cron can drift by up to about 15 minutes or more), and add a date guard in code: exit 0 silently unless today (UTC) is in [2026-10-04, 2026-12-13]. Also expose `workflow_dispatch` so the user can test-run manually. Optionally add a `DRY_RUN=true` mode that prints the poll instead of posting.
**skipped** DRY_RUN (out of scope). I limited the job to run oct - dec. It will still run after the mid december cutoff. That's fine by me.
6. **Secrets** (GitHub repo secrets, read as env vars, never committed): `DISCORD_TOKEN`, `DISCORD_CHANNEL_ID`, `SHEET_ID`, `GOOGLE_CREDENTIALS_JSON` (whole service-account JSON; parse with `GoogleCredential.FromJson`). Locally use `dotnet user-secrets` or a gitignored `.env`.
7. **Discord setup (one-time, manual):** create an app in the Developer Portal, add the bot, invite it with the `Send Messages` + `Create Polls` permissions (no privileged intents needed), and copy the channel ID with Developer Mode on.

## Workflow file sketch
Checkout, `actions/setup-dotnet@v4` (9.0.x), `dotnet run --project src/MoviePollBot` with the secrets passed through `env:`.

## Suggested build order (for the user to code)
1. **Setup (Claude does this one):** in /home/Gerudhoh/dev/DiscordBot, run `dotnet new sln -n DiscordBot`, `dotnet new console -n MoviePollBot -o src/MoviePollBot`, `dotnet new xunit -n MoviePollBot.Tests -o tests/MoviePollBot.Tests`, add both to the sln, add a project reference from the tests to the bot, `dotnet new gitignore`, then add the NuGet packages (`Discord.Net` and `Google.Apis.Sheets.v4` to the bot) and confirm `dotnet build` and `dotnet test` pass. The code files themselves are left to the user.
2. `Config.cs` (env vars, fail fast on missing).
3. `MoviePicker` + tests (pure logic first).
4. Date-window guard + tests.
5. `SheetReader` (print unwatched movies to the console; verify the strikethrough handling against the real sheet).
6. `PollPoster` with DRY_RUN, then a real post to a private test channel.
7. Workflow YAML, add secrets, trigger via `workflow_dispatch`, then enable the cron.

## Verification
- Unit tests: picker never returns struck or empty cells, caps at 10
- Real post to a test channel locally, then a `workflow_dispatch` run in Actions, checking that the poll appears in the target channel.
- After Oct 4, confirm the scheduled run fired (Actions tab); a schedule-triggered run only starts from the default branch.

## Risks / notes
- GitHub may delay scheduled runs, and disables them after 60 days of repo inactivity (not an issue for an 11-week window).
- The service-account JSON is sensitive; never log it.
- Native-poll support in Discord.Net is the main item to verify before coding.
