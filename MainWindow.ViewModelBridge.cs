using kursach.Services;
using kursach.ViewModels;
using ChapterModel = kursachFile.Chapter;
using CourseContentModel = kursachFile.CourseContent;
using PageModel = kursachFile.Page;
using SectionModel = kursachFile.Section;

namespace kursach;

public partial class MainWindow
{
    private readonly MainWindowViewModel _viewModel = new();

    private IList<SectionModel> CurrentSections => _viewModel.CurrentSections;
    private IList<PageModel> CurrentPages => _viewModel.CurrentPages;
    private int CurrentPageIndex => _viewModel.CurrentPageIndex;
    private ChapterModel? CurrentChapter => _viewModel.CurrentChapter;
    private SectionModel? CurrentSection => _viewModel.CurrentSection;

    private void LoadContent()
    {
        var state = _viewModel.LoadCourse(ContentPath, AssetsRoot);
        if (state.Issues.Count > 0)
        {
            LogValidationIssues(state.Issues);
        }

        if (!state.Success)
        {
            MessageBox.Show(state.ErrorMessage ?? "Не удалось загрузить контент.",
                            "Ошибка загрузки",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
            return;
        }

        if (!string.IsNullOrWhiteSpace(_viewModel.CourseTitle))
        {
            Title = _viewModel.CourseTitle;
        }

        if (state.WarningCount > 0)
        {
            ShowAdminStatus($"Контент: {state.WarningCount} предупреждений (см. logs/app.log)", isError: true);
        }

        RenderNavigation();
        NavigateToFirstPage();
        InitializeAdminUi();
        InitializeHistory("Контент загружен");
    }

    private void NavigateToFirstPage()
    {
        _viewModel.NavigateToFirstPage();
    }

    private void NavigateToPage(string chapterId, string sectionId, int pageIndex)
    {
        if (_viewModel.Navigator is null)
        {
            MessageBox.Show("Контент еще не загружен.",
                            "Ошибка навигации",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
            return;
        }

        var result = _viewModel.Navigator.NavigateTo(chapterId, sectionId, pageIndex);
        if (!result.Success && result.FailureKind is NavigationFailureKind.NotFoundChapter or NavigationFailureKind.NotFoundSection)
        {
            MessageBox.Show(result.Message ?? "Цель навигации не найдена.",
                            "Ошибка навигации",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
            return;
        }

        if (CurrentChapter is not null && CurrentSection is not null)
        {
            RenderPageStepper(CurrentChapter, CurrentSection);
            _lastLoadedSectionId = CurrentSection.Id;
        }
        else
        {
            PageStepperPanel.Children.Clear();
        }

        if (CurrentPages.Count == 0)
        {
            ClearReadingContent();
            myRichBox.Visibility = Visibility.Collapsed;
            TasksScrollViewer.Visibility = Visibility.Collapsed;
            if (SectionTitleBlock is not null)
            {
                SectionTitleBlock.Text = CurrentChapter?.Title ?? string.Empty;
            }

            PageTitleBlock.Text = string.Empty;
            MessageBox.Show(result.Message ?? "В выбранном разделе нет страниц.",
                            "Пустой раздел",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
            UpdateNavigationHighlight();
            UpdateNavigationButtons(CurrentPages);
            return;
        }

        _lastLoadedPageIndex = -1;
        LoadPage();
    }

    private void Page_Back(object sender, RoutedEventArgs e)
    {
        if (_viewModel.Navigator is null)
        {
            return;
        }

        if (_viewModel.Navigator.MovePrevious().Success)
        {
            LoadPage();
        }
    }

    private void Page_Next(object sender, RoutedEventArgs e)
    {
        if (_viewModel.Navigator is null)
        {
            return;
        }

        if (_viewModel.Navigator.MoveNext().Success)
        {
            LoadPage();
        }
    }

    private void ApplyCourseSnapshot(CourseContentModel course)
    {
        var currentChapterId = CurrentChapter?.Id;
        var currentSectionId = CurrentSection?.Id;
        var currentPageIndex = CurrentPageIndex;

        _suppressHistory = true;
        _viewModel.SetCourse(course);

        var courseTitle = _viewModel.Course?.Title;
        if (!string.IsNullOrWhiteSpace(courseTitle))
        {
            Title = courseTitle;
        }

        RenderNavigation();
        if (!TryNavigateTo(currentChapterId, currentSectionId, currentPageIndex))
        {
            NavigateToFirstPage();
        }

        InitializeAdminUi();
        _suppressHistory = false;
    }

    private bool TryNavigateTo(string? chapterId, string? sectionId, int pageIndex)
    {
        return _viewModel.TryNavigateTo(chapterId, sectionId, pageIndex);
    }
}

