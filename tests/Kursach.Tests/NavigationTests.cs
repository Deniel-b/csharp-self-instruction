using kursach.Services;
using kursachFile;

namespace Kursach.Tests;

public sealed class NavigationTests
{
    [Fact]
    public void NavigateTo_ValidSelection_SetsCurrentPage()
    {
        var course = BuildCourse();
        var navigator = new CourseNavigator(course);

        var result = navigator.NavigateTo("ch1", "sec1", 0);

        Assert.True(result.Success);
        Assert.Equal("p1", navigator.CurrentPage?.Id);

        var next = navigator.MoveNext();
        Assert.True(next.Success);
        Assert.Equal("p2", navigator.CurrentPage?.Id);
    }

    [Fact]
    public void MoveNext_MovesToNextSection_WhenSectionEnds()
    {
        var course = new CourseContent
        {
            Chapters =
            [
                new Chapter
                {
                    Id = "ch1",
                    Order = 1,
                    Title = "Глава 1",
                    Sections =
                    [
                        new Section
                        {
                            Id = "sec1",
                            Order = 1,
                            Title = "Раздел 1",
                            Pages =
                            [
                                new Page { Id = "p1", Order = 1, Title = "Страница 1" }
                            ]
                        },
                        new Section
                        {
                            Id = "sec2",
                            Order = 2,
                            Title = "Раздел 2",
                            Pages =
                            [
                                new Page { Id = "p2", Order = 1, Title = "Страница 2" }
                            ]
                        }
                    ]
                }
            ]
        };

        var navigator = new CourseNavigator(course);
        navigator.NavigateTo("ch1", "sec1", 0);

        var move = navigator.MoveNext();

        Assert.True(move.Success);
        Assert.Equal("sec2", navigator.CurrentSection?.Id);
        Assert.Equal("p2", navigator.CurrentPage?.Id);
    }

    [Fact]
    public void MovePrevious_ReturnsOutOfRange_OnFirstPage()
    {
        var course = BuildCourse();
        var navigator = new CourseNavigator(course);
        navigator.NavigateTo("ch1", "sec1", 0);

        var result = navigator.MovePrevious();

        Assert.False(result.Success);
        Assert.Equal(NavigationFailureKind.OutOfRange, result.FailureKind);
    }

    [Fact]
    public void NavigateTo_ReturnsNotFound_ForMissingChapter()
    {
        var course = BuildCourse();
        var navigator = new CourseNavigator(course);

        var result = navigator.NavigateTo("missing", "sec1", 0);

        Assert.False(result.Success);
        Assert.Equal(NavigationFailureKind.NotFoundChapter, result.FailureKind);
    }

    private static CourseContent BuildCourse()
    {
        return new CourseContent
        {
            Chapters =
            [
                new Chapter
                {
                    Id = "ch1",
                    Order = 1,
                    Title = "Глава 1",
                    Sections =
                    [
                        new Section
                        {
                            Id = "sec1",
                            Order = 1,
                            Title = "Раздел 1",
                            Pages =
                            [
                                new Page { Id = "p1", Order = 1, Title = "Страница 1" },
                                new Page { Id = "p2", Order = 2, Title = "Страница 2" }
                            ]
                        }
                    ]
                }
            ]
        };
    }
}
