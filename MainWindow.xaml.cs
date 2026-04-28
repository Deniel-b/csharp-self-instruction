using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Input;
using Newtonsoft.Json;
using kursach.Services;
using kursach.ViewModels;
using ChapterModel = kursachFile.Chapter;
using CourseContentModel = kursachFile.CourseContent;
using PageModel = kursachFile.Page;
using PageContentModel = kursachFile.PageContent;
using SectionModel = kursachFile.Section;
using LearningTaskModel = kursachFile.LearningTask;
using PageResourceModel = kursachFile.PageResource;
using TaskOptionModel = kursachFile.TaskOption;
using TaskScoringModel = kursachFile.TaskScoring;
using CodeEntryPointModel = kursachFile.CodeEntryPoint;
using TaskTestCaseModel = kursachFile.TaskTestCase;

namespace kursach;

public partial class MainWindow : Window
{
    private const string ContentPath = "src/content.v2.json";
    private const string AssetsRoot = "src";
    private static readonly Thickness TaskContainerMargin = new(0, 0, 0, 15);
    private static readonly Thickness OptionMargin = new(0, 6, 0, 0);
    private SolidColorBrush _pageButtonForeground = new(Color.FromRgb(0xD6, 0xE0, 0xEC));
    private SolidColorBrush _pageButtonSelectedForeground = new(Colors.White);
    private SolidColorBrush _pageButtonSelectedBackground = new(Color.FromRgb(0xFF, 0x7A, 0x3D));
    private SolidColorBrush _pageButtonHoverBackground = new(Color.FromRgb(0x1C, 0x27, 0x33));
    private Brush _pageButtonDefaultBackground = Brushes.Transparent;

    private SolidColorBrush _stepperDefaultBackground = new(Color.FromRgb(0x1C, 0x27, 0x33));
    private SolidColorBrush _stepperHoverBackground = new(Color.FromRgb(0x2B, 0x3A, 0x4B));
    private SolidColorBrush _stepperSelectedBackground = new(Color.FromRgb(0xFF, 0x7A, 0x3D));
    private SolidColorBrush _stepperForeground = new(Colors.WhiteSmoke);
    private SolidColorBrush _stepperInactiveForeground = new(Color.FromRgb(0x9B, 0xAB, 0xBF));

    private readonly Dictionary<string, SectionButtonInfo> _sectionButtonMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, PageButtonInfo> _pageButtonMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly ContentValidator _contentValidator = new();
    private readonly CodeTaskRunner _codeTaskRunner = new();
    private readonly ContentEditingService _contentEditingService = new();
    private readonly AdminViewModel _adminViewModel;

    private static string MakeSectionKey(string chapterId, string sectionId) => $"{chapterId}|{sectionId}";
    private static string MakePageKey(string chapterId, string sectionId, int pageIndex) => $"{chapterId}|{sectionId}|{pageIndex}";

    private int _lastLoadedPageIndex = -1;
    private string? _lastLoadedSectionId;
    private Point _dragStartPoint;
    private readonly JsonSerializerSettings _adminJsonSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore
    };

    private bool _isAdminMode
    {
        get => _adminViewModel.IsAdminMode;
        set => _adminViewModel.IsAdminMode = value;
    }

    private bool _suppressAdminEvents
    {
        get => _adminViewModel.SuppressAdminEvents;
        set => _adminViewModel.SuppressAdminEvents = value;
    }

    private bool _suppressHistory
    {
        get => _adminViewModel.SuppressHistory;
        set => _adminViewModel.SuppressHistory = value;
    }

    private ChapterModel? _adminChapter
    {
        get => _adminViewModel.SelectedChapter;
        set => _adminViewModel.SelectedChapter = value;
    }

    private SectionModel? _adminSection
    {
        get => _adminViewModel.SelectedSection;
        set => _adminViewModel.SelectedSection = value;
    }

    private PageModel? _adminPage
    {
        get => _adminViewModel.SelectedPage;
        set => _adminViewModel.SelectedPage = value;
    }

    private LearningTaskModel? _adminTask
    {
        get => _adminViewModel.SelectedTask;
        set => _adminViewModel.SelectedTask = value;
    }

    private TaskOptionModel? _adminOption
    {
        get => _adminViewModel.SelectedOption;
        set => _adminViewModel.SelectedOption = value;
    }

    private TaskTestCaseModel? _adminTest
    {
        get => _adminViewModel.SelectedTest;
        set => _adminViewModel.SelectedTest = value;
    }

    private PageResourceModel? _adminResource
    {
        get => _adminViewModel.SelectedResource;
        set => _adminViewModel.SelectedResource = value;
    }

    public MainWindow()
    {
        _adminViewModel = new AdminViewModel(_contentEditingService);
        InitializeComponent();
        ThemeToggle.IsChecked = false;
        ApplyTheme(isDark: false);
        UpdateNavigationButtons();
        LoadContent();
    }



    private void RenderNavigation()
    {
        ChaptersPanel.Children.Clear();
        _sectionButtonMap.Clear();

        int chapterNumber = 1;
        foreach (var chapter in _viewModel.Chapters)
        {
            ChaptersPanel.Children.Add(new TextBlock
            {
                Text = $"{chapterNumber} {chapter.Title}",
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = ResolveBrush("SidebarText", Brushes.White),
                Margin = new Thickness(12, chapterNumber == 1 ? 12 : 24, 12, 6),
                TextWrapping = TextWrapping.Wrap
            });

            if (chapter.OrderedSections.Count == 0)
            {
                ChaptersPanel.Children.Add(new TextBlock
                {
                    Text = "Разделов пока нет",
                    Margin = new Thickness(24, 0, 12, 0),
                    Foreground = ResolveBrush("SidebarMuted", Brushes.LightGray),
                    FontStyle = FontStyles.Italic
                });
                chapterNumber++;
                continue;
            }

            int sectionNumber = 1;
            foreach (var section in chapter.OrderedSections)
            {
                var button = CreateSectionNavigationButton(chapter.Id, section.Id, chapterNumber, sectionNumber, section.Title);
                ChaptersPanel.Children.Add(button);
                var key = MakeSectionKey(chapter.Id, section.Id);
                if (!_sectionButtonMap.TryGetValue(key, out var info))
                {
                    info = new SectionButtonInfo(chapter.Id, section.Id);
                    _sectionButtonMap[key] = info;
                }

                info.LeftButton = button;
                info.LeftLabel = button.Content as TextBlock;
                info.Section = section;

                sectionNumber++;
            }

            chapterNumber++;
        }

        UpdateNavigationHighlight();
    }

    private Brush ResolveBrush(string key, Brush fallback)
    {
        return TryFindResource(key) as Brush ?? fallback;
    }

    private void RenderPageStepper(ChapterModel chapter, SectionModel section)
    {
        PageStepperPanel.Children.Clear();
        _pageButtonMap.Clear();

        int pageIndex = 0;
        foreach (var page in section.OrderedPages)
        {
            var key = MakePageKey(chapter.Id, section.Id, pageIndex);
            if (!_pageButtonMap.TryGetValue(key, out var info))
            {
                info = new PageButtonInfo(chapter.Id, section.Id, pageIndex)
                {
                    Page = page
                };
                _pageButtonMap[key] = info;
            }

            info.Page = page;

            var stepperButton = CreateStepperButton(chapter.Id, section.Id, pageIndex, page, pageIndex + 1);
            info.StepperButton = stepperButton;
            info.StepperLabel = stepperButton.Content as TextBlock;
            PageStepperPanel.Children.Add(stepperButton);

            pageIndex++;
        }
    }

    private Button CreateSectionNavigationButton(string chapterId, string sectionId, int chapterNumber, int sectionNumber, string sectionTitle)
    {
        var text = $"{chapterNumber}.{sectionNumber} {sectionTitle}";
        var label = new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Foreground = _pageButtonForeground
        };

        var button = new Button
        {
            Content = label,
            Tag = new SectionSelection(chapterId, sectionId),
            Margin = new Thickness(6, 2, 6, 0),
            Padding = new Thickness(14, 6, 10, 6),
            HorizontalContentAlignment = HorizontalAlignment.Left,
            BorderThickness = new Thickness(0),
            BorderBrush = Brushes.Transparent,
            Background = _pageButtonDefaultBackground,
            Foreground = _pageButtonForeground,
            Focusable = false,
            Style = (Style)FindResource("NavButtonStyle")
        };

        button.Click += SectionButton_Click;
        button.MouseEnter += SectionButton_MouseEnter;
        button.MouseLeave += SectionButton_MouseLeave;

        return button;
    }

    private Button CreateStepperButton(string chapterId, string sectionId, int pageIndex, PageModel page, int displayIndex)
    {
        var selection = new PageSelection(chapterId, sectionId, pageIndex);
        var symbol = DetermineStepperSymbol(page, displayIndex);
        var label = new TextBlock
        {
            Text = symbol,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeights.SemiBold,
            Foreground = _stepperForeground
        };

        var button = new Button
        {
            Width = 32,
            Height = 32,
            Margin = new Thickness(2, 0, 2, 0),
            Padding = new Thickness(0),
            Content = label,
            Background = _stepperDefaultBackground,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Focusable = false,
            Tag = selection,
            ToolTip = page.Title,
            Style = (Style)FindResource("StepperButtonStyle")
        };

        button.Click += PageButton_Click;
        button.MouseEnter += StepperButton_MouseEnter;
        button.MouseLeave += StepperButton_MouseLeave;

        return button;
    }

    private static string DetermineStepperSymbol(PageModel page, int displayIndex)
    {
        return page.KindNormalized switch
        {
            "quiz" => "?",
            "code" => "{}",
            "assessment" => "!",
            _ => displayIndex.ToString()
        };
    }

    private void StepperButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is not Button button || button.Tag is not PageSelection selection)
        {
            return;
        }

        if (!IsCurrentPageSelection(selection.ChapterId, selection.SectionId, selection.PageIndex))
        {
            button.Background = _stepperHoverBackground;
        }
    }

    private void StepperButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is not Button button || button.Tag is not PageSelection selection)
        {
            return;
        }

        if (!IsCurrentPageSelection(selection.ChapterId, selection.SectionId, selection.PageIndex))
        {
            button.Background = _stepperDefaultBackground;
        }
    }

    private void SectionButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is not Button button || button.Tag is not SectionSelection selection)
        {
            return;
        }

        if (!IsCurrentSectionSelection(selection.ChapterId, selection.SectionId))
        {
            button.Background = _pageButtonHoverBackground;
        }
    }

    private void SectionButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is not Button button || button.Tag is not SectionSelection selection)
        {
            return;
        }

        if (!IsCurrentSectionSelection(selection.ChapterId, selection.SectionId))
        {
            button.Background = _pageButtonDefaultBackground;
        }
    }

    private void SectionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not SectionSelection selection)
        {
            return;
        }

        NavigateToSection(selection.ChapterId, selection.SectionId);
    }

    private void PageButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not PageSelection selection)
        {
            return;
        }

        NavigateToPage(selection.ChapterId, selection.SectionId, selection.PageIndex);
    }

    private void UpdateNavigationHighlight()
    {
        foreach (var info in _sectionButtonMap.Values)
        {
            var isSelected = IsCurrentSectionSelection(info.ChapterId, info.SectionId);
            if (info.LeftButton is not null)
            {
                info.LeftButton.Background = isSelected ? _pageButtonSelectedBackground : _pageButtonDefaultBackground;
            }

            if (info.LeftLabel is not null)
            {
                info.LeftLabel.Foreground = isSelected ? _pageButtonSelectedForeground : _pageButtonForeground;
                info.LeftLabel.FontWeight = isSelected ? FontWeights.SemiBold : FontWeights.Normal;
            }
        }

        foreach (var info in _pageButtonMap.Values)
        {
            var isSelected = IsCurrentPageSelection(info.ChapterId, info.SectionId, info.PageIndex);
            if (info.StepperButton is not null)
            {
                info.StepperButton.Background = isSelected ? _stepperSelectedBackground : _stepperDefaultBackground;
            }

            if (info.StepperLabel is not null)
            {
                info.StepperLabel.Foreground = isSelected ? _stepperForeground : _stepperInactiveForeground;
                info.StepperLabel.FontWeight = isSelected ? FontWeights.Bold : FontWeights.Normal;
            }
        }
    }

    private bool IsCurrentSectionSelection(string chapterId, string sectionId)
    {
        return CurrentChapter is not null
               && CurrentSection is not null
               && string.Equals(CurrentChapter.Id, chapterId, StringComparison.OrdinalIgnoreCase)
               && string.Equals(CurrentSection.Id, sectionId, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsCurrentPageSelection(string chapterId, string sectionId, int pageIndex)
    {
        return IsCurrentSectionSelection(chapterId, sectionId)
               && CurrentPageIndex == pageIndex;
    }

    private void NavigateToSection(string chapterId, string sectionId)
    {
        NavigateToPage(chapterId, sectionId, 0);
    }


    private void LoadPage()
    {
        try
        {
            if (_viewModel.Navigator is null)
            {
                UpdateNavigationButtons();
                return;
            }

            var pages = CurrentPages;
            if (pages.Count == 0)
            {
                ClearReadingContent();
                HideTasksPanel();
                UpdateNavigationButtons(pages);
                return;
            }

            if (CurrentPageIndex < 0 || CurrentPageIndex >= pages.Count)
            {
                ClearReadingContent();
                HideTasksPanel();
                MessageBox.Show("Запрошенная страница вне доступного диапазона.",
                                "Ошибка навигации",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                UpdateNavigationButtons(pages);
                return;
            }

            var page = pages[CurrentPageIndex];
            if (CurrentChapter is not null && CurrentSection is not null
                && !string.Equals(_lastLoadedSectionId, CurrentSection.Id, StringComparison.OrdinalIgnoreCase))
            {
                RenderPageStepper(CurrentChapter, CurrentSection);
                _lastLoadedSectionId = CurrentSection.Id;
            }

            if (SectionTitleBlock is not null)
            {
                SectionTitleBlock.Text = CurrentSection?.Title ?? string.Empty;
            }

            PageTitleBlock.Text = page.Title;

            switch (DetermineDisplayMode(page))
            {
                case PageDisplayMode.Tasks:
                    ShowTaskPage(page);
                    break;
                case PageDisplayMode.Reading:
                default:
                    ShowReadingPage(page);
                    break;
            }
        }
        finally
        {
            UpdateNavigationButtons(CurrentPages);
            UpdateNavigationHighlight();
        }
    }

    private void ShowReadingPage(PageModel page)
    {
        myRichBox.Visibility = Visibility.Visible;
        TasksScrollViewer.Visibility = Visibility.Collapsed;
        TasksPanel.Children.Clear();
        ClearReadingContent();

        if (page.Content is null)
        {
            MessageBox.Show($"Page \"{page.Title}\" не содержит материал для чтения.",
                            "Ошибка контента",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
            return;
        }

        if (!string.Equals(page.Content.Format, "richText", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show($"Неподдерживаемый формат страницы: {page.Content.Format}",
                            "Ошибка контента",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(page.Content.Source))
        {
            MessageBox.Show($"Page \"{page.Title}\" не указан источник контента.",
                            "Ошибка контента",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
            return;
        }

        var relativePath = page.Content.Source.Replace('/', Path.DirectorySeparatorChar);
        var filePath = Path.Combine(AssetsRoot, relativePath);
        Trace.WriteLine($"Загрузка страницы: {filePath}");

        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var documentRange = new TextRange(myRichBox.Document.ContentStart, myRichBox.Document.ContentEnd);
            documentRange.Load(stream, DataFormats.Rtf);
            ApplyReadingDocumentTheme();
            myRichBox.ScrollToHome();

            _lastLoadedPageIndex = CurrentPageIndex;
        }
        catch (FileNotFoundException)
        {
            MessageBox.Show($"Файл страницы не найден: {filePath}",
                            "Ошибка чтения",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

            if (_lastLoadedPageIndex >= 0)
            {
                if (_viewModel.Navigator is not null && CurrentChapter is not null && CurrentSection is not null)
                {
                    _viewModel.Navigator.NavigateTo(CurrentChapter.Id, CurrentSection.Id, _lastLoadedPageIndex);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось открыть файл страницы: {ex.Message}",
                            "Ошибка чтения",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
        }
    }

    private void ShowTaskPage(PageModel page)
    {
        myRichBox.Visibility = Visibility.Collapsed;
        ClearReadingContent();
        TasksScrollViewer.Visibility = Visibility.Visible;

        var tasks = page.Tasks ?? new List<LearningTaskModel>();
        if (tasks.Count == 0)
        {
            RenderTasks(Array.Empty<LearningTaskModel>(), showPlaceholder: true);
        }
        else
        {
            RenderTasks(tasks, showPlaceholder: false);
        }

        TasksScrollViewer.ScrollToHome();
        _lastLoadedPageIndex = CurrentPageIndex;
    }

    private void ClearReadingContent()
    {
        myRichBox.Document.Blocks.Clear();
    }

    private void ApplyReadingDocumentTheme()
    {
        if (myRichBox?.Document is null)
        {
            return;
        }

        var textRange = new TextRange(myRichBox.Document.ContentStart, myRichBox.Document.ContentEnd);
        textRange.ApplyPropertyValue(TextElement.ForegroundProperty, ResolveBrush("TextPrimary", Brushes.Black));
        textRange.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.Transparent);
        myRichBox.Background = Brushes.Transparent;
    }

    private void HideTasksPanel()
    {
        TasksScrollViewer.Visibility = Visibility.Collapsed;
        TasksPanel.Children.Clear();
    }

    private static PageDisplayMode DetermineDisplayMode(PageModel page)
    {
        return page.IsTaskPage ? PageDisplayMode.Tasks : PageDisplayMode.Reading;
    }

    private void RenderTasks(IReadOnlyList<LearningTaskModel>? tasks, bool showPlaceholder = false)
    {
        TasksPanel.Children.Clear();

        if (tasks is null || tasks.Count == 0)
        {
            if (showPlaceholder)
            {
                TasksPanel.Children.Add(new TextBlock
                {
                    Text = "Задания для этого раздела появятся здесь.",
                    FontStyle = FontStyles.Italic,
                    Foreground = ResolveBrush("TaskHintText", Brushes.Gray)
                });
            }

            return;
        }

        foreach (var task in tasks)
        {
            TasksPanel.Children.Add(CreateTaskElement(task));
        }
    }

    private FrameworkElement CreateTaskElement(LearningTaskModel task)
    {
        if (string.Equals(task.Type, "quiz", StringComparison.OrdinalIgnoreCase))
        {
            return CreateQuizTask(task);
        }

        if (string.Equals(task.Type, "code", StringComparison.OrdinalIgnoreCase))
        {
            return CreateCodeTask(task);
        }

        return CreateUnsupportedTask(task);
    }

    private FrameworkElement CreateQuizTask(LearningTaskModel task)
    {
        var container = new Border
        {
            Margin = TaskContainerMargin,
            Padding = new Thickness(12),
            BorderBrush = ResolveBrush("TaskCardBorder", Brushes.LightGray),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Background = ResolveBrush("TaskCardBackground", Brushes.WhiteSmoke)
        };

        var layout = new StackPanel();
        container.Child = layout;

        var heading = !string.IsNullOrWhiteSpace(task.Title) ? task.Title : "Тест";
        layout.Children.Add(new TextBlock
        {
            Text = heading,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Foreground = ResolveBrush("TextPrimary", Brushes.Black)
        });

        if (!string.IsNullOrWhiteSpace(task.Question))
        {
            layout.Children.Add(new TextBlock
            {
                Text = task.Question,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 6, 0, 0),
                Foreground = ResolveBrush("TextPrimary", Brushes.Black)
            });
        }

        if (task.Scoring.Points > 0)
        {
            layout.Children.Add(new TextBlock
            {
                Text = $"Баллы: {task.Scoring.Points}" + (task.Scoring.Partial ? " (возможен частичный зачёт)" : string.Empty),
                FontSize = 12,
                Foreground = ResolveBrush("TaskHintText", Brushes.DimGray),
                Margin = new Thickness(0, 4, 0, 0)
            });
        }

        var optionsPanel = new StackPanel
        {
            Margin = new Thickness(0, 10, 0, 10)
        };

        var optionButtons = new List<ToggleButton>();
        var singleChoice = string.Equals(task.Selection, "single", StringComparison.OrdinalIgnoreCase);
        var radioGroupName = singleChoice ? $"QuizGroup_{Guid.NewGuid():N}" : string.Empty;

        foreach (var option in task.Options)
        {
            ToggleButton optionControl = singleChoice
                ? new RadioButton { GroupName = radioGroupName }
                : new CheckBox();

            optionControl.Content = option.Text;
            optionControl.Tag = option;
            optionControl.Margin = OptionMargin;
            optionControl.ToolTip = option.Feedback;
            optionControl.Foreground = ResolveBrush("TextPrimary", Brushes.Black);
            optionsPanel.Children.Add(optionControl);
            optionButtons.Add(optionControl);
        }

        layout.Children.Add(optionsPanel);

        var actionPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal
        };

        var checkButton = new Button
        {
            Content = "Проверить",
            Padding = new Thickness(12, 6, 12, 6),
            Margin = new Thickness(0, 0, 10, 0),
            Style = (Style)FindResource(typeof(Button))
        };

        var resetButton = new Button
        {
            Content = "Сбросить",
            Padding = new Thickness(12, 6, 12, 6),
            Style = (Style)FindResource(typeof(Button))
        };

        var feedbackBlock = new TextBlock
        {
            Margin = new Thickness(0, 10, 0, 0),
            TextWrapping = TextWrapping.Wrap,
            Foreground = ResolveBrush("StatusInfo", Brushes.Gray)
        };

        var context = new QuizTaskContext(task, optionButtons, feedbackBlock);
        checkButton.Tag = context;
        resetButton.Tag = context;

        checkButton.Click += QuizCheckButton_Click;
        resetButton.Click += QuizResetButton_Click;

        actionPanel.Children.Add(checkButton);
        actionPanel.Children.Add(resetButton);
        layout.Children.Add(actionPanel);
        layout.Children.Add(feedbackBlock);

        if (task.Hints.Count > 0)
        {
            var hintsExpander = new Expander
            {
                Header = "Подсказки",
                Margin = new Thickness(0, 10, 0, 0),
                IsExpanded = false
            };

            var hintsPanel = new StackPanel();
            foreach (var hint in task.Hints)
            {
                hintsPanel.Children.Add(new TextBlock
                {
                    Text = $"• {hint}",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 2, 0, 0),
                    Foreground = ResolveBrush("TextSecondary", Brushes.Gray)
                });
            }

            hintsExpander.Content = hintsPanel;
            layout.Children.Add(hintsExpander);
        }

        return container;
    }

    private FrameworkElement CreateCodeTask(LearningTaskModel task)
    {
        var container = new Border
        {
            Margin = TaskContainerMargin,
            Padding = new Thickness(12),
            BorderBrush = ResolveBrush("TaskCardBorder", Brushes.SlateGray),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Background = ResolveBrush("TaskCardBackground", Brushes.WhiteSmoke)
        };

        var layout = new StackPanel();
        container.Child = layout;

        var heading = !string.IsNullOrWhiteSpace(task.Title) ? task.Title : "Задание с кодом";
        layout.Children.Add(new TextBlock
        {
            Text = heading,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Foreground = ResolveBrush("TextPrimary", Brushes.Black)
        });

        if (!string.IsNullOrWhiteSpace(task.Prompt))
        {
            layout.Children.Add(new TextBlock
            {
                Text = task.Prompt,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 6, 0, 10),
                Foreground = ResolveBrush("TextPrimary", Brushes.Black)
            });
        }

        var codeEditor = new TextBox
        {
            AcceptsReturn = true,
            AcceptsTab = true,
            TextWrapping = TextWrapping.NoWrap,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 13,
            MinHeight = 160,
            Text = task.StarterCode ?? string.Empty,
            Margin = new Thickness(0, 0, 0, 10),
            Background = ResolveBrush("TaskCodeBackground", Brushes.WhiteSmoke),
            Foreground = ResolveBrush("TextPrimary", Brushes.Black),
            BorderBrush = ResolveBrush("TaskCardBorder", Brushes.SlateGray)
        };
        layout.Children.Add(codeEditor);

        var buttonsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal
        };

        var runButton = new Button
        {
            Content = "Запустить тесты",
            Padding = new Thickness(12, 6, 12, 6),
            Margin = new Thickness(0, 0, 10, 0),
            Style = (Style)FindResource(typeof(Button))
        };

        var resetButton = new Button
        {
            Content = "Сбросить код",
            Padding = new Thickness(12, 6, 12, 6),
            Style = (Style)FindResource(typeof(Button))
        };

        buttonsPanel.Children.Add(runButton);
        buttonsPanel.Children.Add(resetButton);
        layout.Children.Add(buttonsPanel);

        var feedbackBlock = new TextBlock
        {
            Margin = new Thickness(0, 10, 0, 0),
            TextWrapping = TextWrapping.Wrap,
            Foreground = ResolveBrush("StatusInfo", Brushes.Gray)
        };
        layout.Children.Add(feedbackBlock);

        var context = new CodeTaskContext(task, codeEditor, feedbackBlock, task.StarterCode ?? string.Empty);
        runButton.Tag = context;
        resetButton.Tag = context;
        runButton.Click += RunCodeTask_Click;
        resetButton.Click += ResetCodeTask_Click;

        var publicTests = task.Tests
            .Where(test => string.Equals(test.Visibility, "public", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (publicTests.Count > 0)
        {
            layout.Children.Add(new TextBlock
            {
                Text = "Примеры тестов:",
                Margin = new Thickness(0, 12, 0, 0),
                FontWeight = FontWeights.SemiBold,
                Foreground = ResolveBrush("TextPrimary", Brushes.Black)
            });

            foreach (var test in publicTests)
            {
                var testBlock = new TextBlock
                {
                    TextWrapping = TextWrapping.Wrap,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 12,
                    Margin = new Thickness(0, 6, 0, 0),
                    Foreground = ResolveBrush("TextPrimary", Brushes.Black),
                    Text = $"Вход:\n{test.Input}\nОжидается:\n{test.ExpectedOutput}"
                };
                layout.Children.Add(testBlock);

                if (!string.IsNullOrWhiteSpace(test.Explanation))
                {
                    layout.Children.Add(new TextBlock
                    {
                        Text = test.Explanation,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 2, 0, 0),
                        FontStyle = FontStyles.Italic,
                        Foreground = ResolveBrush("TaskHintText", Brushes.Gray)
                    });
                }
            }
        }
        else if (task.Tests.Count > 0)
        {
            layout.Children.Add(new TextBlock
            {
                Text = "Скрытые тесты также выполняются.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 12, 0, 0),
                Foreground = ResolveBrush("TaskHintText", Brushes.Gray)
            });
        }

        return container;
    }

    private FrameworkElement CreateUnsupportedTask(LearningTaskModel task)
    {
        return new Border
        {
            Margin = TaskContainerMargin,
            Padding = new Thickness(12),
            BorderBrush = ResolveBrush("TaskCardBorder", Brushes.LightGray),
            BorderThickness = new Thickness(1),
            Background = ResolveBrush("TaskCardBackground", Brushes.WhiteSmoke),
            Child = new TextBlock
            {
                Text = $"Тип задания \"{task.Type}\" пока не поддерживается.",
                TextWrapping = TextWrapping.Wrap,
                Foreground = ResolveBrush("TextPrimary", Brushes.Black)
            }
        };
    }

    private void QuizCheckButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not QuizTaskContext context)
        {
            return;
        }

        var selectedIds = context.OptionButtons
            .Where(option => option.IsChecked == true && option.Tag is TaskOptionModel)
            .Select(option => ((TaskOptionModel)option.Tag).Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (selectedIds.Count == 0)
        {
            context.FeedbackBlock.Text = "Выберите хотя бы один вариант перед проверкой.";
            context.FeedbackBlock.Foreground = ResolveBrush("StatusWarning", Brushes.DarkOrange);
            return;
        }

        var correctIds = context.Task.Options
            .Where(option => option.IsCorrect)
            .Select(option => option.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missing = correctIds.Where(id => !selectedIds.Contains(id)).ToList();
        var wrong = selectedIds.Where(id => !correctIds.Contains(id)).ToList();

        foreach (var toggle in context.OptionButtons)
        {
            if (toggle.Tag is not TaskOptionModel option)
            {
                continue;
            }

            if (option.IsCorrect)
            {
                toggle.Foreground = ResolveBrush("StatusSuccess", Brushes.ForestGreen);
            }
            else if (toggle.IsChecked == true)
            {
                toggle.Foreground = ResolveBrush("StatusError", Brushes.Firebrick);
            }
            else
            {
                toggle.Foreground = ResolveBrush("TextPrimary", Brushes.Black);
            }
        }

        if (missing.Count == 0 && wrong.Count == 0)
        {
            var explanation = context.Task.Solution?.Explanation;
            context.FeedbackBlock.Text = !string.IsNullOrWhiteSpace(explanation)
                ? explanation
                : "Верно! Отлично.";
            context.FeedbackBlock.Foreground = ResolveBrush("StatusSuccess", Brushes.ForestGreen);
            return;
        }

        var messages = new List<string>();
        if (wrong.Count > 0)
        {
            messages.Add("Некоторые выбранные варианты неверны.");
        }

        if (missing.Count > 0)
        {
            messages.Add("Некоторые правильные варианты не выбраны.");
        }

        var explanationText = context.Task.Solution?.Explanation;
        if (!string.IsNullOrWhiteSpace(explanationText))
        {
            messages.Add(explanationText!);
        }

        context.FeedbackBlock.Text = string.Join(" ", messages);
        context.FeedbackBlock.Foreground = ResolveBrush("StatusError", Brushes.Firebrick);
    }

    private void QuizResetButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not QuizTaskContext context)
        {
            return;
        }

        foreach (var toggle in context.OptionButtons)
        {
            toggle.IsChecked = false;
            toggle.Foreground = ResolveBrush("TextPrimary", Brushes.Black);
        }

        context.FeedbackBlock.Text = string.Empty;
        context.FeedbackBlock.Foreground = ResolveBrush("StatusInfo", Brushes.Gray);
    }

    private async void RunCodeTask_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not CodeTaskContext context)
        {
            return;
        }

        button.IsEnabled = false;
        context.FeedbackBlock.Foreground = ResolveBrush("StatusInfo", Brushes.DarkSlateBlue);
        context.FeedbackBlock.Text = "Запуск тестов...";

        var code = context.CodeEditor.Text ?? string.Empty;
        var task = context.Task;
        var tests = task.Tests ?? new List<TaskTestCaseModel>();
        var result = await _codeTaskRunner.RunAsync(code, task.EntryPoint, task.Constraints, tests);

        context.FeedbackBlock.Text = string.IsNullOrWhiteSpace(result.Summary)
            ? (result.Success ? "Все тесты пройдены." : "Тесты не пройдены.")
            : result.Summary;
        context.FeedbackBlock.Foreground = result.Success
            ? ResolveBrush("StatusSuccess", Brushes.ForestGreen)
            : ResolveBrush("StatusError", Brushes.Firebrick);
        button.IsEnabled = true;
    }

    private void ResetCodeTask_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not CodeTaskContext context)
        {
            return;
        }

        context.CodeEditor.Text = context.StarterCode ?? string.Empty;
        context.FeedbackBlock.Text = "Шаблон кода восстановлен.";
        context.FeedbackBlock.Foreground = ResolveBrush("StatusInfo", Brushes.Gray);
    }

    private void UpdateNavigationButtons(IReadOnlyList<PageModel>? pages = null)
    {
        if (_viewModel.Navigator is null || pages is null || pages.Count == 0)
        {
            backButton.IsEnabled = false;
            nextButton.IsEnabled = false;
            return;
        }

        backButton.IsEnabled = _viewModel.Navigator.CanMovePrevious;
        nextButton.IsEnabled = _viewModel.Navigator.CanMoveNext;
    }

    private void AdminToggle_Checked(object sender, RoutedEventArgs e)
    {
        SetAdminMode(true);
    }

    private void AdminToggle_Unchecked(object sender, RoutedEventArgs e)
    {
        SetAdminMode(false);
    }

    private void SetAdminMode(bool enabled)
    {
        _isAdminMode = enabled;
        if (AdminToggle is not null)
        {
            AdminToggle.IsChecked = enabled;
        }

        if (AdminPanel is not null)
        {
            AdminPanel.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
        }

        if (ContextPanel is not null)
        {
            ContextPanel.Visibility = enabled ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    private void InitializeAdminUi()
    {
        if (_viewModel.Course is null)
        {
            return;
        }

        _suppressAdminEvents = true;
        AdminChapterCombo.ItemsSource = _viewModel.Course.Chapters;
        AdminChapterCombo.SelectedIndex = _viewModel.Course.Chapters.Count > 0 ? 0 : -1;
        _suppressAdminEvents = false;

        UpdateAdminChapterSelection();
        SetAdminMode(_isAdminMode);
    }

    private void AdminChapterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressAdminEvents)
        {
            return;
        }

        UpdateAdminChapterSelection();
    }

    private void UpdateAdminChapterSelection()
    {
        _adminChapter = AdminChapterCombo.SelectedItem as ChapterModel;
        AdminChapterTitle.Text = _adminChapter?.Title ?? string.Empty;
        RefreshAdminSectionList();
    }

    private void RefreshAdminSectionList()
    {
        _suppressAdminEvents = true;
        AdminSectionCombo.ItemsSource = _adminChapter?.Sections;
        AdminSectionCombo.SelectedIndex = _adminChapter?.Sections.Count > 0 ? 0 : -1;
        _suppressAdminEvents = false;
        UpdateAdminSectionSelection();
    }

    private void AdminSectionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressAdminEvents)
        {
            return;
        }

        UpdateAdminSectionSelection();
    }

    private void UpdateAdminSectionSelection()
    {
        _adminSection = AdminSectionCombo.SelectedItem as SectionModel;
        AdminSectionTitle.Text = _adminSection?.Title ?? string.Empty;
        RefreshAdminPageList();
    }

    private void RefreshAdminPageList()
    {
        _suppressAdminEvents = true;
        AdminPageList.ItemsSource = _adminSection?.Pages;
        AdminPageList.SelectedIndex = _adminSection?.Pages.Count > 0 ? 0 : -1;
        _suppressAdminEvents = false;
        UpdateAdminPageSelection();
    }

    private void RefreshAdminTaskList()
    {
        if (_adminPage is null)
        {
            AdminTaskList.ItemsSource = null;
            _adminTask = null;
            LoadAdminTaskEditor();
            return;
        }

        _adminPage.Tasks ??= new List<LearningTaskModel>();
        _suppressAdminEvents = true;
        AdminTaskList.ItemsSource = _adminPage.Tasks;
        AdminTaskList.SelectedIndex = _adminPage.Tasks.Count > 0 ? 0 : -1;
        _suppressAdminEvents = false;
        UpdateAdminTaskSelection();
    }

    private void RefreshAdminResourceList()
    {
        if (_adminPage is null)
        {
            AdminResourceList.ItemsSource = null;
            _adminResource = null;
            LoadAdminResourceEditor();
            return;
        }

        _adminPage.Resources ??= new List<PageResourceModel>();
        _suppressAdminEvents = true;
        AdminResourceList.ItemsSource = _adminPage.Resources;
        AdminResourceList.SelectedIndex = _adminPage.Resources.Count > 0 ? 0 : -1;
        _suppressAdminEvents = false;
        UpdateAdminResourceSelection();
    }

    private void AdminResourceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressAdminEvents)
        {
            return;
        }

        UpdateAdminResourceSelection();
    }

    private void UpdateAdminResourceSelection()
    {
        _adminResource = AdminResourceList.SelectedItem as PageResourceModel;
        LoadAdminResourceEditor();
    }

    private void LoadAdminResourceEditor()
    {
        if (_adminResource is null)
        {
            AdminResourceType.Text = string.Empty;
            AdminResourceTitle.Text = string.Empty;
            AdminResourceUrl.Text = string.Empty;
            return;
        }

        var type = _adminResource.Type ?? string.Empty;
        SelectComboItem(AdminResourceType, type);
        AdminResourceType.Text = type;
        AdminResourceTitle.Text = _adminResource.Title ?? string.Empty;
        AdminResourceUrl.Text = _adminResource.Url ?? string.Empty;
    }

    private void AdminTaskList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressAdminEvents)
        {
            return;
        }

        UpdateAdminTaskSelection();
    }

    private void UpdateAdminTaskSelection()
    {
        _adminTask = AdminTaskList.SelectedItem as LearningTaskModel;
        LoadAdminTaskEditor();
    }

    private void LoadAdminTaskEditor()
    {
        if (_adminTask is null)
        {
            AdminTaskTitle.Text = string.Empty;
            AdminTaskPoints.Text = string.Empty;
            AdminTaskPartial.IsChecked = false;
            AdminTaskHints.Text = string.Empty;
            AdminQuizQuestion.Text = string.Empty;
            AdminCodePrompt.Text = string.Empty;
            AdminCodeStarter.Text = string.Empty;
            AdminCodeClass.Text = string.Empty;
            AdminCodeMethod.Text = string.Empty;
            AdminCodeSignature.Text = string.Empty;
            AdminOptionList.ItemsSource = null;
            AdminTestList.ItemsSource = null;
            ShowTaskTypePanels(null);
            return;
        }

        _adminTask.Scoring ??= new TaskScoringModel();
        AdminTaskTitle.Text = _adminTask.Title ?? string.Empty;
        AdminTaskPoints.Text = _adminTask.Scoring.Points.ToString(CultureInfo.InvariantCulture);
        AdminTaskPartial.IsChecked = _adminTask.Scoring.Partial;
        AdminTaskHints.Text = string.Join(Environment.NewLine, _adminTask.Hints ?? new List<string>());

        var taskType = string.IsNullOrWhiteSpace(_adminTask.Type) ? "quiz" : _adminTask.Type;
        SelectComboItem(AdminTaskType, taskType);
        ShowTaskTypePanels(taskType);

        AdminQuizQuestion.Text = _adminTask.Question ?? string.Empty;
        SelectComboItem(AdminQuizSelection, _adminTask.Selection ?? "single");

        _adminTask.Options ??= new List<TaskOptionModel>();
        _suppressAdminEvents = true;
        AdminOptionList.ItemsSource = _adminTask.Options;
        AdminOptionList.SelectedIndex = _adminTask.Options.Count > 0 ? 0 : -1;
        _suppressAdminEvents = false;
        UpdateAdminOptionSelection();

        AdminCodePrompt.Text = _adminTask.Prompt ?? string.Empty;
        AdminCodeStarter.Text = _adminTask.StarterCode ?? string.Empty;
        AdminCodeClass.Text = _adminTask.EntryPoint?.ClassName ?? string.Empty;
        AdminCodeMethod.Text = _adminTask.EntryPoint?.MethodName ?? string.Empty;
        AdminCodeSignature.Text = _adminTask.EntryPoint?.Signature ?? string.Empty;

        _adminTask.Tests ??= new List<TaskTestCaseModel>();
        _suppressAdminEvents = true;
        AdminTestList.ItemsSource = _adminTask.Tests;
        AdminTestList.SelectedIndex = _adminTask.Tests.Count > 0 ? 0 : -1;
        _suppressAdminEvents = false;
        UpdateAdminTestSelection();
    }

    private void AdminTaskType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressAdminEvents)
        {
            return;
        }

        var selectedType = ExtractComboValue(AdminTaskType);
        ShowTaskTypePanels(selectedType);
    }

    private void ShowTaskTypePanels(string? type)
    {
        var normalized = type?.Trim().ToLowerInvariant();
        AdminQuizPanel.Visibility = normalized == "quiz" ? Visibility.Visible : Visibility.Collapsed;
        AdminCodePanel.Visibility = normalized == "code" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void AdminOptionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressAdminEvents)
        {
            return;
        }

        UpdateAdminOptionSelection();
    }

    private void UpdateAdminOptionSelection()
    {
        _adminOption = AdminOptionList.SelectedItem as TaskOptionModel;
        AdminOptionText.Text = _adminOption?.Text ?? string.Empty;
        AdminOptionIsCorrect.IsChecked = _adminOption?.IsCorrect ?? false;
        AdminOptionFeedback.Text = _adminOption?.Feedback ?? string.Empty;
    }

    private void AdminTestList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressAdminEvents)
        {
            return;
        }

        UpdateAdminTestSelection();
    }

    private void UpdateAdminTestSelection()
    {
        _adminTest = AdminTestList.SelectedItem as TaskTestCaseModel;
        AdminTestId.Text = _adminTest?.Id ?? string.Empty;
        SelectComboItem(AdminTestVisibility, _adminTest?.Visibility ?? "hidden");
        AdminTestInput.Text = _adminTest?.Input ?? string.Empty;
        AdminTestExpected.Text = _adminTest?.ExpectedOutput ?? string.Empty;
        AdminTestExplanation.Text = _adminTest?.Explanation ?? string.Empty;
    }

    private void AdminPageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressAdminEvents)
        {
            return;
        }

        UpdateAdminPageSelection();
    }

    private void UpdateAdminPageSelection()
    {
        _adminPage = AdminPageList.SelectedItem as PageModel;
        LoadAdminPageEditor();
    }

    private void LoadAdminPageEditor()
    {
        AdminPageTitle.Text = _adminPage?.Title ?? string.Empty;
        AdminPageTime.Text = _adminPage?.EstimatedTimeMinutes?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        AdminPageContentSource.Text = _adminPage?.Content?.Source ?? string.Empty;

        SelectComboItem(AdminPageKind, _adminPage?.KindNormalized ?? "reading");
        SelectComboItem(AdminPageContentFormat, _adminPage?.Content?.Format ?? "richText");

        if (_adminPage is not null)
        {
            _adminPage.Resources ??= new List<PageResourceModel>();
        }

        RefreshAdminResourceList();
        RefreshAdminTaskList();
    }

    private static void SelectComboItem(ComboBox comboBox, string value)
    {
        foreach (var item in comboBox.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(item.Content?.ToString(), value, StringComparison.OrdinalIgnoreCase))
            {
                comboBox.SelectedItem = item;
                return;
            }
        }

        comboBox.SelectedIndex = -1;
    }

    private void AdminChapterAdd_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.Course is null)
        {
            return;
        }

        var chapter = new ChapterModel
        {
            Id = GenerateId("chapter"),
            Title = "Новая глава"
        };
        _contentEditingService.AddChapter(_viewModel.Course, chapter);
        NormalizeOrdering();
        RefreshAdminChapterList(chapter);
        RefreshNavigationAfterEdit(chapter.Id, chapter.Sections.FirstOrDefault()?.Id, 0);
        ShowAdminStatus("Глава добавлена.");
        RecordHistory("Глава добавлена");
    }

    private void AdminChapterDelete_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.Course is null || _adminChapter is null)
        {
            return;
        }

        _contentEditingService.RemoveChapter(_viewModel.Course, _adminChapter);
        NormalizeOrdering();
        RefreshAdminChapterList(_viewModel.Course.Chapters.FirstOrDefault());
        RefreshNavigationAfterEdit(null, null, 0);
        ShowAdminStatus("Глава удалена.");
        RecordHistory("Глава удалена");
    }

    private void AdminChapterApply_Click(object sender, RoutedEventArgs e)
    {
        if (_adminChapter is null)
        {
            return;
        }

        _contentEditingService.UpdateChapter(_adminChapter, AdminChapterTitle.Text.Trim());
        RenderNavigation();
        UpdateNavigationHighlight();
        ShowAdminStatus("Глава обновлена.");
        RecordHistory("Глава обновлена");
    }

    private void AdminSectionAdd_Click(object sender, RoutedEventArgs e)
    {
        if (_adminChapter is null)
        {
            return;
        }

        var section = new SectionModel
        {
            Id = GenerateId("section"),
            Title = "Новый раздел"
        };
        _contentEditingService.AddSection(_adminChapter, section);
        NormalizeOrdering();
        RefreshAdminSectionList();
        AdminSectionCombo.SelectedItem = section;
        RefreshNavigationAfterEdit(_adminChapter.Id, section.Id, 0);
        ShowAdminStatus("Раздел добавлен.");
        RecordHistory("Раздел добавлен");
    }

    private void AdminSectionDelete_Click(object sender, RoutedEventArgs e)
    {
        if (_adminChapter is null || _adminSection is null)
        {
            return;
        }

        _contentEditingService.RemoveSection(_adminChapter, _adminSection);
        NormalizeOrdering();
        RefreshAdminSectionList();
        RefreshNavigationAfterEdit(_adminChapter.Id, _adminChapter.Sections.FirstOrDefault()?.Id, 0);
        ShowAdminStatus("Раздел удален.");
        RecordHistory("Раздел удален");
    }

    private void AdminSectionApply_Click(object sender, RoutedEventArgs e)
    {
        if (_adminSection is null)
        {
            return;
        }

        _contentEditingService.UpdateSection(_adminSection, AdminSectionTitle.Text.Trim());
        RenderNavigation();
        UpdateNavigationHighlight();
        ShowAdminStatus("Раздел обновлен.");
        RecordHistory("Раздел обновлен");
    }

    private void AdminPageAdd_Click(object sender, RoutedEventArgs e)
    {
        if (_adminSection is null)
        {
            return;
        }

        var page = new PageModel
        {
            Id = GenerateId("page"),
            Title = "Новая страница",
            Kind = "reading",
            Content = new PageContentModel { Format = "richText", Source = string.Empty }
        };
        _contentEditingService.AddPage(_adminSection, page);
        NormalizeOrdering();
        RefreshAdminPageList();
        AdminPageList.SelectedItem = page;
        RefreshNavigationAfterEdit(CurrentChapter?.Id, _adminSection.Id, _adminSection.Pages.IndexOf(page));
        ShowAdminStatus("Страница добавлена.");
        RecordHistory("Страница добавлена");
    }

    private void AdminPageDelete_Click(object sender, RoutedEventArgs e)
    {
        if (_adminSection is null || _adminPage is null)
        {
            return;
        }

        var index = _adminSection.Pages.IndexOf(_adminPage);
        _contentEditingService.RemovePage(_adminSection, _adminPage);
        NormalizeOrdering();
        RefreshAdminPageList();
        if (_adminSection.Pages.Count > 0)
        {
            AdminPageList.SelectedIndex = Math.Clamp(index, 0, _adminSection.Pages.Count - 1);
        }
        RefreshNavigationAfterEdit(CurrentChapter?.Id, _adminSection.Id, AdminPageList.SelectedIndex);
        ShowAdminStatus("Страница удалена.");
        RecordHistory("Страница удалена");
    }

    private void AdminPageMoveUp_Click(object sender, RoutedEventArgs e)
    {
        if (_adminSection is null || _adminPage is null)
        {
            return;
        }

        if (!_contentEditingService.MovePage(_adminSection, _adminPage, -1, out var newIndex))
        {
            return;
        }
        NormalizeOrdering();
        RefreshAdminPageList();
        AdminPageList.SelectedIndex = newIndex;
        RefreshNavigationAfterEdit(CurrentChapter?.Id, _adminSection.Id, newIndex);
        RecordHistory("Страница перемещена");
    }

    private void AdminPageMoveDown_Click(object sender, RoutedEventArgs e)
    {
        if (_adminSection is null || _adminPage is null)
        {
            return;
        }

        if (!_contentEditingService.MovePage(_adminSection, _adminPage, 1, out var newIndex))
        {
            return;
        }
        NormalizeOrdering();
        RefreshAdminPageList();
        AdminPageList.SelectedIndex = newIndex;
        RefreshNavigationAfterEdit(CurrentChapter?.Id, _adminSection.Id, newIndex);
        RecordHistory("Страница перемещена");
    }

    private void AdminPageApply_Click(object sender, RoutedEventArgs e)
    {
        if (_adminPage is null)
        {
            return;
        }

        var kindValue = ExtractComboValue(AdminPageKind);
        int? minutes = null;
        if (int.TryParse(AdminPageTime.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedMinutes))
        {
            minutes = parsedMinutes;
        }

        _contentEditingService.UpdatePage(
            _adminPage,
            AdminPageTitle.Text.Trim(),
            kindValue,
            minutes,
            ExtractComboValue(AdminPageContentFormat),
            AdminPageContentSource.Text.Trim());

        _adminPage.Resources ??= new List<PageResourceModel>();

        RenderNavigation();
        UpdateNavigationHighlight();
        RefreshAdminPageList();
        ShowAdminStatus("Страница обновлена.");
        RecordHistory("Страница обновлена");
    }

    private void AdminTaskAddQuiz_Click(object sender, RoutedEventArgs e)
    {
        if (_adminPage is null)
        {
            return;
        }

        _adminPage.Tasks ??= new List<LearningTaskModel>();
        var task = new LearningTaskModel
        {
            Id = GenerateId("quiz"),
            Type = "quiz",
            Title = "Новый тест",
            Selection = "single",
            Question = "Новый вопрос",
            Scoring = new TaskScoringModel { Points = 1, Partial = false },
            Options = new List<TaskOptionModel>
            {
                new() { Id = GenerateId("opt"), Text = "Вариант 1", IsCorrect = true },
                new() { Id = GenerateId("opt"), Text = "Вариант 2", IsCorrect = false }
            }
        };

        _contentEditingService.AddTask(_adminPage.Tasks, task);
        RefreshAdminTaskList();
        AdminTaskList.SelectedItem = task;
        ShowAdminStatus("Добавлен тест.");
        RecordHistory("Добавлен тест");
    }

    private void AdminTaskAddCode_Click(object sender, RoutedEventArgs e)
    {
        if (_adminPage is null)
        {
            return;
        }

        _adminPage.Tasks ??= new List<LearningTaskModel>();
        var task = new LearningTaskModel
        {
            Id = GenerateId("code"),
            Type = "code",
            Title = "Новая практика",
            Prompt = "Опишите задачу и добавьте тесты.",
            StarterCode = "using System;\n\npublic static class Solution\n{\n    public static void Solve()\n    {\n        // TODO\n    }\n}\n",
            EntryPoint = new CodeEntryPointModel
            {
                ClassName = "Solution",
                MethodName = "Solve",
                Signature = "public static void Solve()"
            },
            Scoring = new TaskScoringModel { Points = 1, Partial = true }
        };

        _contentEditingService.AddTask(_adminPage.Tasks, task);
        RefreshAdminTaskList();
        AdminTaskList.SelectedItem = task;
        ShowAdminStatus("Добавлено код-задание.");
        RecordHistory("Добавлено код-задание");
    }

    private void AdminTaskDelete_Click(object sender, RoutedEventArgs e)
    {
        if (_adminPage is null || _adminTask is null)
        {
            return;
        }

        _adminPage.Tasks ??= new List<LearningTaskModel>();
        var index = _adminPage.Tasks.IndexOf(_adminTask);
        _contentEditingService.RemoveTask(_adminPage.Tasks, _adminTask);
        RefreshAdminTaskList();
        if (_adminPage.Tasks.Count > 0)
        {
            AdminTaskList.SelectedIndex = Math.Clamp(index, 0, _adminPage.Tasks.Count - 1);
        }

        ShowAdminStatus("Задание удалено.");
        RecordHistory("Задание удалено");
    }

    private void AdminTaskMoveUp_Click(object sender, RoutedEventArgs e)
    {
        if (_adminPage?.Tasks is null || _adminTask is null)
        {
            return;
        }

        if (!_contentEditingService.MoveTask(_adminPage.Tasks, _adminTask, -1, out var newIndex))
        {
            return;
        }
        RefreshAdminTaskList();
        AdminTaskList.SelectedIndex = newIndex;
        RecordHistory("Задание перемещено");
    }

    private void AdminTaskMoveDown_Click(object sender, RoutedEventArgs e)
    {
        if (_adminPage?.Tasks is null || _adminTask is null)
        {
            return;
        }

        if (!_contentEditingService.MoveTask(_adminPage.Tasks, _adminTask, 1, out var newIndex))
        {
            return;
        }
        RefreshAdminTaskList();
        AdminTaskList.SelectedIndex = newIndex;
        RecordHistory("Задание перемещено");
    }

    private void AdminTaskApply_Click(object sender, RoutedEventArgs e)
    {
        if (_adminTask is null)
        {
            return;
        }

        var points = int.TryParse(AdminTaskPoints.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedPoints)
            ? parsedPoints
            : (int?)null;

        var hints = AdminTaskHints.Text
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        var type = ExtractComboValue(AdminTaskType) ?? _adminTask.Type;
        _contentEditingService.UpdateTaskCore(
            _adminTask,
            AdminTaskTitle.Text.Trim(),
            points,
            AdminTaskPartial.IsChecked == true,
            hints,
            type ?? "quiz");

        if (string.Equals(_adminTask.Type, "quiz", StringComparison.OrdinalIgnoreCase))
        {
            _adminTask.Question = AdminQuizQuestion.Text.Trim();
            _adminTask.Selection = ExtractComboValue(AdminQuizSelection) ?? "single";
        }

        if (string.Equals(_adminTask.Type, "code", StringComparison.OrdinalIgnoreCase))
        {
            _adminTask.Prompt = AdminCodePrompt.Text;
            _adminTask.StarterCode = AdminCodeStarter.Text;
            _adminTask.EntryPoint ??= new CodeEntryPointModel();
            _adminTask.EntryPoint.ClassName = AdminCodeClass.Text.Trim();
            _adminTask.EntryPoint.MethodName = AdminCodeMethod.Text.Trim();
            _adminTask.EntryPoint.Signature = AdminCodeSignature.Text.Trim();
        }

        RefreshAdminTaskList();
        ShowAdminStatus("Задание обновлено.");
        RecordHistory("Задание обновлено");
    }

    private void AdminOptionAdd_Click(object sender, RoutedEventArgs e)
    {
        if (_adminTask is null)
        {
            return;
        }

        _adminTask.Options ??= new List<TaskOptionModel>();
        var option = new TaskOptionModel
        {
            Id = GenerateId("opt"),
            Text = "Новый вариант",
            IsCorrect = false
        };
        _contentEditingService.AddOption(_adminTask.Options, option);
        _suppressAdminEvents = true;
        AdminOptionList.ItemsSource = _adminTask.Options;
        AdminOptionList.SelectedItem = option;
        _suppressAdminEvents = false;
        UpdateAdminOptionSelection();
        RecordHistory("Вариант добавлен");
    }

    private void AdminOptionDelete_Click(object sender, RoutedEventArgs e)
    {
        if (_adminTask?.Options is null || _adminOption is null)
        {
            return;
        }

        var index = _adminTask.Options.IndexOf(_adminOption);
        _contentEditingService.RemoveOption(_adminTask.Options, _adminOption);
        AdminOptionList.ItemsSource = _adminTask.Options;
        if (_adminTask.Options.Count > 0)
        {
            AdminOptionList.SelectedIndex = Math.Clamp(index, 0, _adminTask.Options.Count - 1);
        }
        UpdateAdminOptionSelection();
        RecordHistory("Вариант удален");
    }

    private void AdminOptionApply_Click(object sender, RoutedEventArgs e)
    {
        if (_adminOption is null)
        {
            return;
        }

        _contentEditingService.UpdateOption(
            _adminOption,
            AdminOptionText.Text.Trim(),
            AdminOptionIsCorrect.IsChecked == true,
            AdminOptionFeedback.Text.Trim());
        AdminOptionList.Items.Refresh();
        RecordHistory("Вариант обновлен");
    }

    private void AdminTestAdd_Click(object sender, RoutedEventArgs e)
    {
        if (_adminTask is null)
        {
            return;
        }

        _adminTask.Tests ??= new List<TaskTestCaseModel>();
        var test = new TaskTestCaseModel
        {
            Id = GenerateId("test"),
            Visibility = "public",
            Input = string.Empty,
            ExpectedOutput = string.Empty
        };
        _contentEditingService.AddTest(_adminTask.Tests, test);
        _suppressAdminEvents = true;
        AdminTestList.ItemsSource = _adminTask.Tests;
        AdminTestList.SelectedItem = test;
        _suppressAdminEvents = false;
        UpdateAdminTestSelection();
        RecordHistory("Тест добавлен");
    }

    private void AdminTestDelete_Click(object sender, RoutedEventArgs e)
    {
        if (_adminTask?.Tests is null || _adminTest is null)
        {
            return;
        }

        var index = _adminTask.Tests.IndexOf(_adminTest);
        _contentEditingService.RemoveTest(_adminTask.Tests, _adminTest);
        AdminTestList.ItemsSource = _adminTask.Tests;
        if (_adminTask.Tests.Count > 0)
        {
            AdminTestList.SelectedIndex = Math.Clamp(index, 0, _adminTask.Tests.Count - 1);
        }
        UpdateAdminTestSelection();
        RecordHistory("Тест удален");
    }

    private void AdminTestApply_Click(object sender, RoutedEventArgs e)
    {
        if (_adminTest is null)
        {
            return;
        }

        _contentEditingService.UpdateTest(
            _adminTest,
            AdminTestId.Text.Trim(),
            ExtractComboValue(AdminTestVisibility) ?? "hidden",
            AdminTestInput.Text,
            AdminTestExpected.Text,
            AdminTestExplanation.Text.Trim());
        AdminTestList.Items.Refresh();
        RecordHistory("Тест обновлен");
    }

    private void AdminResourceAdd_Click(object sender, RoutedEventArgs e)
    {
        if (_adminPage is null)
        {
            return;
        }

        _adminPage.Resources ??= new List<PageResourceModel>();
        var resource = new PageResourceModel
        {
            Type = "externalLink",
            Title = "Новый ресурс",
            Url = "https://"
        };
        _contentEditingService.AddResource(_adminPage.Resources, resource);
        RefreshAdminResourceList();
        AdminResourceList.SelectedItem = resource;
        ShowAdminStatus("Ресурс добавлен.");
        RecordHistory("Ресурс добавлен");
    }

    private void AdminResourceDelete_Click(object sender, RoutedEventArgs e)
    {
        if (_adminPage?.Resources is null || _adminResource is null)
        {
            return;
        }

        _contentEditingService.RemoveResource(_adminPage.Resources, _adminResource);
        RefreshAdminResourceList();
        ShowAdminStatus("Ресурс удален.");
        RecordHistory("Ресурс удален");
    }

    private void AdminResourceApply_Click(object sender, RoutedEventArgs e)
    {
        if (_adminResource is null)
        {
            return;
        }

        var typeValue = ExtractComboValue(AdminResourceType) ?? AdminResourceType.Text.Trim();
        _contentEditingService.UpdateResource(
            _adminResource,
            typeValue,
            AdminResourceTitle.Text.Trim(),
            AdminResourceUrl.Text.Trim());
        AdminResourceList.Items.Refresh();
        ShowAdminStatus("Ресурс обновлен.");
        RecordHistory("Ресурс обновлен");
    }

    private void AdminList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
    }

    private void AdminPageList_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var position = e.GetPosition(null);
        if (Math.Abs(position.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(position.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        if (sender is not ListBox listBox)
        {
            return;
        }

        var item = GetListBoxItemAt(listBox, e.GetPosition(listBox));
        if (item?.DataContext is PageModel page)
        {
            DragDrop.DoDragDrop(listBox, new DataObject(typeof(PageModel), page), DragDropEffects.Move);
        }
    }

    private void AdminTaskList_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var position = e.GetPosition(null);
        if (Math.Abs(position.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(position.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        if (sender is not ListBox listBox)
        {
            return;
        }

        var item = GetListBoxItemAt(listBox, e.GetPosition(listBox));
        if (item?.DataContext is LearningTaskModel task)
        {
            DragDrop.DoDragDrop(listBox, new DataObject(typeof(LearningTaskModel), task), DragDropEffects.Move);
        }
    }

    private void AdminList_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(PageModel)) || e.Data.GetDataPresent(typeof(LearningTaskModel)))
        {
            e.Effects = DragDropEffects.Move;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    private void AdminPageList_Drop(object sender, DragEventArgs e)
    {
        if (_adminSection?.Pages is null || _adminSection.Pages.Count == 0)
        {
            return;
        }

        if (!e.Data.GetDataPresent(typeof(PageModel)))
        {
            return;
        }

        var dropped = (PageModel)e.Data.GetData(typeof(PageModel))!;
        var listBox = (ListBox)sender;
        var targetItem = GetListBoxItemAt(listBox, e.GetPosition(listBox));
        var targetPage = targetItem?.DataContext as PageModel;

        var requestedIndex = targetPage is null ? _adminSection.Pages.Count - 1 : _adminSection.Pages.IndexOf(targetPage);
        if (requestedIndex < 0)
        {
            requestedIndex = _adminSection.Pages.Count - 1;
        }

        if (!_contentEditingService.MovePageToIndex(_adminSection, dropped, requestedIndex, out var newIndex))
        {
            return;
        }

        NormalizeOrdering();
        RefreshAdminPageList();
        AdminPageList.SelectedIndex = newIndex;
        RefreshNavigationAfterEdit(CurrentChapter?.Id, _adminSection.Id, newIndex);
        ShowAdminStatus("Страница перемещена.");
        RecordHistory("Страница перемещена");
    }

    private void AdminTaskList_Drop(object sender, DragEventArgs e)
    {
        if (_adminPage?.Tasks is null || _adminPage.Tasks.Count == 0)
        {
            return;
        }

        if (!e.Data.GetDataPresent(typeof(LearningTaskModel)))
        {
            return;
        }

        var dropped = (LearningTaskModel)e.Data.GetData(typeof(LearningTaskModel))!;
        var listBox = (ListBox)sender;
        var targetItem = GetListBoxItemAt(listBox, e.GetPosition(listBox));
        var targetTask = targetItem?.DataContext as LearningTaskModel;

        var requestedIndex = targetTask is null ? _adminPage.Tasks.Count - 1 : _adminPage.Tasks.IndexOf(targetTask);
        if (requestedIndex < 0)
        {
            requestedIndex = _adminPage.Tasks.Count - 1;
        }

        if (!_contentEditingService.MoveTaskToIndex(_adminPage.Tasks, dropped, requestedIndex, out var newIndex))
        {
            return;
        }

        RefreshAdminTaskList();
        AdminTaskList.SelectedIndex = newIndex;
        ShowAdminStatus("Задание перемещено.");
        RecordHistory("Задание перемещено");
    }

    private static ListBoxItem? GetListBoxItemAt(ListBox listBox, Point position)
    {
        var element = listBox.InputHitTest(position) as DependencyObject;
        while (element is not null && element is not ListBoxItem)
        {
            element = VisualTreeHelper.GetParent(element);
        }

        return element as ListBoxItem;
    }

    private void AdminSave_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.Course is null)
        {
            return;
        }

        NormalizeOrdering();
        var issues = _contentValidator.Validate(_viewModel.Course, ResolveAssetsRoot());
        var errorCount = issues.Count(issue => issue.Severity == ContentIssueSeverity.Error);
        var warningCount = issues.Count - errorCount;
        if (issues.Count > 0)
        {
            LogValidationIssues(issues);
            if (errorCount > 0)
            {
                ShowAdminStatus($"Сохранение отменено: {errorCount} ошибок. См. logs/app.log", isError: true);
                MessageBox.Show($"Сохранение отменено из-за ошибок контента: {errorCount}. Подробности см. в logs/app.log.",
                                "Ошибка проверки контента",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                return;
            }
        }

        var json = JsonConvert.SerializeObject(_viewModel.Course, Formatting.Indented, _adminJsonSettings);
        if (!_contentEditingService.TryWriteContentFile(ResolveContentPath(), json, out var saveError))
        {
            ShowAdminStatus($"Ошибка сохранения: {saveError}", isError: true);
            MessageBox.Show($"Не удалось сохранить content.v2.json: {saveError}",
                            "Ошибка сохранения",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
            return;
        }

        var statusMessage = warningCount > 0
            ? $"Сохранено (есть предупреждения: {warningCount}, см. logs/app.log)"
            : "Сохранено в content.v2.json";
        ShowAdminStatus(statusMessage);
        AppLogger.Info("Контент успешно сохранен.");
    }

    private void AdminReload_Click(object sender, RoutedEventArgs e)
    {
        var wasAdmin = _isAdminMode;
        LoadContent();
        SetAdminMode(wasAdmin);
        ShowAdminStatus("Перезагружено из content.v2.json");
    }

    private void RefreshAdminChapterList(ChapterModel? selectChapter)
    {
        if (_viewModel.Course is null)
        {
            return;
        }

        _suppressAdminEvents = true;
        AdminChapterCombo.ItemsSource = _viewModel.Course.Chapters;
        AdminChapterCombo.SelectedItem = selectChapter;
        if (AdminChapterCombo.SelectedIndex == -1 && _viewModel.Course.Chapters.Count > 0)
        {
            AdminChapterCombo.SelectedIndex = 0;
        }
        _suppressAdminEvents = false;
        UpdateAdminChapterSelection();
    }

    private static string? ExtractComboValue(ComboBox comboBox)
    {
        if (comboBox.SelectedItem is ComboBoxItem item)
        {
            if (item.Tag is string tag && !string.IsNullOrWhiteSpace(tag))
            {
                return tag;
            }

            return item.Content?.ToString();
        }

        return null;
    }

    private void NormalizeOrdering()
    {
        if (_viewModel.Course is not null)
        {
            _contentEditingService.NormalizeOrdering(_viewModel.Course);
        }
    }

    private void RefreshNavigationAfterEdit(string? chapterId, string? sectionId, int pageIndex)
    {
        RenderNavigation();

        if (!string.IsNullOrWhiteSpace(chapterId) && !string.IsNullOrWhiteSpace(sectionId))
        {
            NavigateToPage(chapterId, sectionId, Math.Max(0, pageIndex));
            return;
        }

        NavigateToFirstPage();
    }

    private string GenerateId(string prefix)
    {
        return _contentEditingService.GenerateId(prefix);
    }

    private void ShowAdminStatus(string message, bool isError = false)
    {
        if (AdminStatusText is null)
        {
            return;
        }

        AdminStatusText.Text = message;
        AdminStatusText.Foreground = isError
            ? ResolveBrush("StatusError", Brushes.Firebrick)
            : ResolveBrush("StatusSuccess", Brushes.DarkGreen);
    }

    private void InitializeHistory(string description)
    {
        _adminViewModel.InitializeHistory(_viewModel.Course, description, _adminJsonSettings);
        RefreshHistoryUi();
    }

    private void RecordHistory(string description, bool force = false)
    {
        _adminViewModel.RecordHistory(_viewModel.Course, description, _adminJsonSettings, force);
        RefreshHistoryUi();
    }

    private void RefreshHistoryUi()
    {
        if (AdminHistoryList is null || AdminUndoButton is null || AdminRedoButton is null)
        {
            return;
        }

        AdminHistoryList.ItemsSource = _adminViewModel.History
            .Select(entry => $"{entry.Timestamp:HH:mm:ss} {entry.Description}")
            .ToList();
        AdminHistoryList.SelectedIndex = _adminViewModel.HistoryIndex;
        AdminUndoButton.IsEnabled = _adminViewModel.CanUndo;
        AdminRedoButton.IsEnabled = _adminViewModel.CanRedo;
    }

    private void AdminUndo_Click(object sender, RoutedEventArgs e)
    {
        if (!_adminViewModel.TryUndo(out var entry) || entry is null)
        {
            return;
        }

        RestoreHistoryEntry(entry, "Отмена");
    }

    private void AdminRedo_Click(object sender, RoutedEventArgs e)
    {
        if (!_adminViewModel.TryRedo(out var entry) || entry is null)
        {
            return;
        }

        RestoreHistoryEntry(entry, "Повтор");
    }

    private void RestoreHistoryEntry(AdminHistoryEntry entry, string actionLabel)
    {
        try
        {
            var restored = _contentEditingService.RestoreSnapshot(entry.Snapshot);
            if (restored is null)
            {
                ShowAdminStatus("Не удалось восстановить историю.", isError: true);
                return;
            }

            ApplyCourseSnapshot(restored);
            ShowAdminStatus($"{actionLabel}: {entry.Description}");
            RefreshHistoryUi();
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "Не удалось восстановить запись истории.");
            ShowAdminStatus("Ошибка восстановления истории.", isError: true);
        }
    }
    private static string ResolveContentPath()
    {
        return Path.GetFullPath(ContentPath);
    }

    private static string ResolveAssetsRoot()
    {
        return Path.GetFullPath(AssetsRoot);
    }

    private static void LogValidationIssues(IEnumerable<ContentIssue> issues)
    {
        foreach (var issue in issues)
        {
            if (issue.Severity == ContentIssueSeverity.Error)
            {
                AppLogger.Error(issue.Message);
            }
            else
            {
                AppLogger.Warn(issue.Message);
            }
        }
    }

    private enum PageDisplayMode
    {
        Reading,
        Tasks
    }
private sealed class SectionButtonInfo
    {
        public SectionButtonInfo(string chapterId, string sectionId)
        {
            ChapterId = chapterId;
            SectionId = sectionId;
        }

        public string ChapterId { get; }
        public string SectionId { get; }
        public Button? LeftButton { get; set; }
        public TextBlock? LeftLabel { get; set; }
        public SectionModel? Section { get; set; }
    }

    private sealed class PageButtonInfo
    {
        public PageButtonInfo(string chapterId, string sectionId, int pageIndex)
        {
            ChapterId = chapterId;
            SectionId = sectionId;
            PageIndex = pageIndex;
        }

        public string ChapterId { get; }
        public string SectionId { get; }
        public int PageIndex { get; }
        public Button? StepperButton { get; set; }
        public TextBlock? StepperLabel { get; set; }
        public PageModel? Page { get; set; }
    }

    private sealed class SectionSelection
    {
        public SectionSelection(string chapterId, string sectionId)
        {
            ChapterId = chapterId;
            SectionId = sectionId;
        }

        public string ChapterId { get; }
        public string SectionId { get; }
    }

    private sealed class PageSelection
    {
        public PageSelection(string chapterId, string sectionId, int pageIndex)
        {
            ChapterId = chapterId;
            SectionId = sectionId;
            PageIndex = pageIndex;
        }

        public string ChapterId { get; }
        public string SectionId { get; }
        public int PageIndex { get; }
    }

    private sealed class QuizTaskContext
    {
        public QuizTaskContext(LearningTaskModel task, IReadOnlyList<ToggleButton> optionButtons, TextBlock feedbackBlock)
        {
            Task = task;
            OptionButtons = optionButtons;
            FeedbackBlock = feedbackBlock;
        }

        public LearningTaskModel Task { get; }
        public IReadOnlyList<ToggleButton> OptionButtons { get; }
        public TextBlock FeedbackBlock { get; }
    }

    private sealed class CodeTaskContext
    {
        public CodeTaskContext(LearningTaskModel task, TextBox codeEditor, TextBlock feedbackBlock, string starterCode)
        {
            Task = task;
            CodeEditor = codeEditor;
            FeedbackBlock = feedbackBlock;
            StarterCode = starterCode;
        }

        public LearningTaskModel Task { get; }
        public TextBox CodeEditor { get; }
        public TextBlock FeedbackBlock { get; }
        public string StarterCode { get; }
    }
}


