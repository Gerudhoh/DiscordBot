using Discord;
using MoviePoll.Bot;

namespace MoviePollBot.Tests;

public class BuildPollTests
{
    private static List<string> Movies(int count) =>
        Enumerable.Range(1, count).Select(i => $"Movie {i}").ToList();

    [Fact]
    public void BuildPoll_UsesEachMovieAsAnAnswer_InOrder()
    {
        var movies = new List<string> { "The Thing", "Rudolph", "Die Hard" };

        var poll = MoviePoll.Bot.MoviePollBot.BuildPoll(movies);

        Assert.Equal(movies, poll.Answers.Select(a => a.Text).ToList());
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(10, 10)]
    [InlineData(11, 10)]
    [InlineData(25, 10)]
    public void BuildPoll_CapsAnswersAtTen(int input, int expected)
    {
        var poll = MoviePoll.Bot.MoviePollBot.BuildPoll(Movies(input));

        Assert.Equal(expected, poll.Answers.Count);
    }

    [Fact]
    public void BuildPoll_WhenOverTen_KeepsTheFirstTen()
    {
        var movies = Movies(12);

        var poll = MoviePoll.Bot.MoviePollBot.BuildPoll(movies);

        Assert.Equal(movies.Take(10), poll.Answers.Select(a => a.Text));
    }

    [Fact]
    public void BuildPoll_SetsQuestionAndSettings()
    {
        var poll = MoviePoll.Bot.MoviePollBot.BuildPoll(Movies(3));

        Assert.Equal("Which movie(s) do you want to watch?", poll.Question.Text);
        Assert.True(poll.AllowMultiselect);
        Assert.Equal(48u, (uint)poll.Duration);
        Assert.Equal(PollLayout.Default, poll.LayoutType);
    }

    [Theory]
    [InlineData(Months.OCTOBER, "Scream,Halloween,Beetlejuice")]
    [InlineData(Months.NOVEMBER, "Nightmare Before Christmas,Edward Scissorhands")]
    [InlineData(Months.DECEMBER, "Little Women")]
    [InlineData(Months.OUT_OF_SCOPE, "")]
    public void GetMonthsMovies_Gets(Months month, string expectedMovies)
    {
        IList<IList<object>> data = new List<IList<object>>
        {
            new List<object> { "Scream" },                  
            new List<object> { "Halloween", "Nightmare Before Christmas" },
            new List<object> { "Beetlejuice", "Edward Scissorhands", "Little Women" },
        };
        var movies = MoviePoll.Bot.MoviePollBot.GetMonthsMovies(month, data);
        Assert.Equal(expectedMovies, string.Join(",", movies));
    }
}
