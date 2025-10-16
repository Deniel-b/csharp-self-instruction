using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Newtonsoft.Json;
using ChapterModel = kursachFile.Chapter;
using CourseContentModel = kursachFile.CourseContent;
using PageModel = kursachFile.Page;
using LearningTaskModel = kursachFile.LearningTask;
using TaskOptionModel = kursachFile.TaskOption;

namespace kursach;

public partial class MainWindow : Window
{
    private const string ContentPath = "src/content.v2.json";
    private const string AssetsRoot = "src";
    private static readonly Thickness TaskContainerMargin = new(0, 0, 0, 15);
    private static readonly Thickness OptionMargin = new(0, 6, 0, 0);
    private static readonly SolidColorBrush PageButtonForeground = new(Color.FromRgb(0xE0, 0xE0, 0xE0));
    private static readonly SolidColorBrush PageButtonSelectedForeground = Brushes.White;
    private static readonly SolidColorBrush PageButtonSelectedBackground = new(Color.FromRgb(0x37, 0xA1, 0x57));
    private static readonly SolidColorBrush PageButtonHoverBackground = new(Color.FromRgb(0x2B, 0x6F, 0x3A));
    private static readonly SolidColorBrush PageButtonDefaultBackground = Brushes.Transparent;

    private static readonly SolidColorBrush StepperDefaultBackground = new(Color.FromRgb(0x3A, 0x3A, 0x3A));
    private static readonly SolidColorBrush StepperHoverBackground = new(Color.FromRgb(0x4A, 0x4A, 0x4A));
    private static readonly SolidColorBrush StepperSelectedBackground = new(Color.FromRgb(0x37, 0xA1, 0x57));
    private static readonly SolidColorBrush StepperForeground = Brushes.WhiteSmoke;

    private readonly Dictionary<string, PageButtonInfo> _pageButtonMap = new(StringComparer.OrdinalIgnoreCase);

    private static string MakePageKey(string chapterId, int pageIndex) => $"{chapterId}|{pageIndex}";

    private CourseContentModel? _course;
    private IReadOnlyList<ChapterModel> _chapters = Array.Empty<ChapterModel>();
    private string? _currentChapterId;
    private IReadOnlyList<PageModel> _currentChapterPages = Array.Empty<PageModel>();
    private int _currentPageIndex;
    private int _lastLoadedPageIndex = -1;

    public MainWindow()
    {
        InitializeComponent();
        UpdateNavigationButtons();
        LoadContent();
    }

    private void LoadContent()
    {
        try
        {
            if (!File.Exists(ContentPath))
            {
                MessageBox.Show($"Content description file not found: {ContentPath}",
                                "Load Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                return;
            }

            var json = File.ReadAllText(ContentPath);
            _course = JsonConvert.DeserializeObject<CourseContentModel>(json);

            if (_course is null)
            {
                MessageBox.Show("Unable to parse content manifest.",
                                "Load Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                return;
            }

            if (!string.IsNullOrWhiteSpace(_course.Title))
            {
                Title = _course.Title;
            }

            _chapters = _course.OrderedChapters;

            if (_chapters.Count == 0)
            {
                MessageBox.Show("No chapters defined in the content manifest.",
                                "Load Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                return;
            }

            RenderNavigation();

            if (_chapters.Count > 0)
            {
                NavigateToPage(_chapters[0].Id, 0);
            }
        }
        catch (JsonException ex)
        {
            MessageBox.Show($"Content file is damaged: {ex.Message}",
                            "Load Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load content: {ex.Message}",
                            "Load Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
        }
    }

    private void RenderNavigation()
    {
        ChaptersPanel.Children.Clear();
        _pageButtonMap.Clear();

        int chapterNumber = 1;
        foreach (var chapter in _chapters)
        {
            ChaptersPanel.Children.Add(new TextBlock
            {
                Text = $"{chapterNumber} {chapter.Title}",
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                Margin = new Thickness(12, chapterNumber == 1 ? 12 : 24, 12, 6),
                TextWrapping = TextWrapping.Wrap
            });

            if (chapter.OrderedPages.Count == 0)
            {
                ChaptersPanel.Children.Add(new TextBlock
                {
                    Text = "No material pages yet",
                    Margin = new Thickness(24, 0, 12, 0),
                    Foreground = Brushes.LightGray,
                    FontStyle = FontStyles.Italic
                });
                chapterNumber++;
                continue;
            }

            int pageNumber = 1;
            foreach (var page in chapter.OrderedPages)
            {
                var pageIndex = pageNumber - 1;
                var button = CreatePageNavigationButton(chapter.Id, chapterNumber, pageNumber, page.Title, pageIndex);
                ChaptersPanel.Children.Add(button);
                var key = MakePageKey(chapter.Id, pageIndex);
                if (!_pageButtonMap.TryGetValue(key, out var info))
                {
                    info = new PageButtonInfo(chapter.Id, pageIndex);
                    _pageButtonMap[key] = info;
                }

                info.LeftButton = button;
                info.LeftLabel = button.Content as TextBlock;
                info.Page = page;

                pageNumber++;
            }

            chapterNumber++;
        }

        UpdateNavigationHighlight();
    }

    private void RenderPageStepper(ChapterModel chapter)
    {
        PageStepperPanel.Children.Clear();

        foreach (var info in _pageButtonMap.Values)
        {
            info.StepperButton = null;
            info.StepperLabel = null;
        }

        int pageIndex = 0;
        foreach (var page in chapter.OrderedPages)
        {
            var key = MakePageKey(chapter.Id, pageIndex);
            if (!_pageButtonMap.TryGetValue(key, out var info))
            {
                info = new PageButtonInfo(chapter.Id, pageIndex)
                {
                    Page = page
                };
                _pageButtonMap[key] = info;
            }

            info.Page = page;

            var stepperButton = CreateStepperButton(chapter.Id, pageIndex, page, pageIndex + 1);
            info.StepperButton = stepperButton;
            info.StepperLabel = stepperButton.Content as TextBlock;
            PageStepperPanel.Children.Add(stepperButton);

            pageIndex++;
        }
    }

    private Button CreatePageNavigationButton(string chapterId, int chapterNumber, int pageNumber, string pageTitle, int pageIndex)
    {
        var text = $"{chapterNumber}.{pageNumber} {pageTitle}";
        var label = new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Foreground = PageButtonForeground
        };

        var button = new Button
        {
            Content = label,
            Tag = new PageSelection(chapterId, pageIndex),
            Margin = new Thickness(6, 2, 6, 0),
            Padding = new Thickness(14, 6, 10, 6),
            HorizontalContentAlignment = HorizontalAlignment.Left,
            BorderThickness = new Thickness(0),
            BorderBrush = Brushes.Transparent,
            Background = PageButtonDefaultBackground,
            Foreground = PageButtonForeground,
            Focusable = false
        };

        button.Click += PageButton_Click;
        button.MouseEnter += PageButton_MouseEnter;
        button.MouseLeave += PageButton_MouseLeave;

        return button;
    }

    private Button CreateStepperButton(string chapterId, int pageIndex, PageModel page, int displayIndex)
    {
        var selection = new PageSelection(chapterId, pageIndex);
        var symbol = DetermineStepperSymbol(page, displayIndex);
        var label = new TextBlock
        {
            Text = symbol,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeights.SemiBold,
            Foreground = StepperForeground
        };

        var button = new Button
        {
            Width = 32,
            Height = 32,
            Margin = new Thickness(2, 0, 2, 0),
            Padding = new Thickness(0),
            Content = label,
            Background = StepperDefaultBackground,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Focusable = false,
            Tag = selection,
            ToolTip = page.Title
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

        if (!IsCurrentSelection(selection.ChapterId, selection.PageIndex))
        {
            button.Background = StepperHoverBackground;
        }
    }

    private void StepperButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is not Button button || button.Tag is not PageSelection selection)
        {
            return;
        }

        if (!IsCurrentSelection(selection.ChapterId, selection.PageIndex))
        {
            button.Background = StepperDefaultBackground;
        }
    }

    private void PageButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is not Button button || button.Tag is not PageSelection selection)
        {
            return;
        }

        if (!IsCurrentSelection(selection.ChapterId, selection.PageIndex))
        {
            button.Background = PageButtonHoverBackground;
        }
    }

    private void PageButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is not Button button || button.Tag is not PageSelection selection)
        {
            return;
        }

        if (!IsCurrentSelection(selection.ChapterId, selection.PageIndex))
        {
            button.Background = PageButtonDefaultBackground;
        }
    }

    private void PageButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not PageSelection selection)
        {
            return;
        }

        NavigateToPage(selection.ChapterId, selection.PageIndex);
    }

    private void UpdateNavigationHighlight()
    {
        foreach (var info in _pageButtonMap.Values)
        {
            var isSelected = IsCurrentSelection(info.ChapterId, info.PageIndex);
            if (info.LeftButton is not null)
            {
                info.LeftButton.Background = isSelected ? PageButtonSelectedBackground : PageButtonDefaultBackground;
            }

            if (info.LeftLabel is not null)
            {
                info.LeftLabel.Foreground = isSelected ? PageButtonSelectedForeground : PageButtonForeground;
                info.LeftLabel.FontWeight = isSelected ? FontWeights.SemiBold : FontWeights.Normal;
            }

            if (info.StepperButton is not null)
            {
                info.StepperButton.Background = isSelected ? StepperSelectedBackground : StepperDefaultBackground;
            }

            if (info.StepperLabel is not null)
            {
                info.StepperLabel.Foreground = isSelected ? StepperForeground : Brushes.LightGray;
                info.StepperLabel.FontWeight = isSelected ? FontWeights.Bold : FontWeights.Normal;
            }
        }
    }

    private bool IsCurrentSelection(string chapterId, int pageIndex)
    {
        return string.Equals(_currentChapterId, chapterId, StringComparison.OrdinalIgnoreCase)
               && _currentPageIndex == pageIndex;
    }

    private void NavigateToPage(string chapterId, int pageIndex)
    {
        if (!TryGetChapter(chapterId, out var chapter) || chapter is null)
        {
            MessageBox.Show($"Chapter not found: {chapterId}",
                            "Navigation Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
            return;
        }

        _currentChapterId = chapterId;
        _currentChapterPages = chapter.OrderedPages;
        RenderPageStepper(chapter);

        if (_currentChapterPages.Count == 0)
        {
            myRichBox.Document.Blocks.Clear();
            myRichBox.Visibility = Visibility.Collapsed;
            TasksScrollViewer.Visibility = Visibility.Collapsed;
            PageTitleBlock.Text = chapter.Title;
            MessageBox.Show($"Chapter \"{chapter.Title}\" has no pages to display.",
                            "Empty Chapter",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
            UpdateNavigationHighlight();
            UpdateNavigationButtons(_currentChapterPages);
            return;
        }

        _currentPageIndex = Math.Clamp(pageIndex, 0, _currentChapterPages.Count - 1);
        _lastLoadedPageIndex = -1;

        LoadPage();
    }

    private bool TryGetChapter(string? chapterId, out ChapterModel? chapter)
    {
        if (_course is null)
        {
            chapter = null;
            return false;
        }

        return _course.TryGetChapter(chapterId, out chapter);
    }

    private void Page_Back(object sender, RoutedEventArgs e)
    {
        if (_currentPageIndex <= 0)
        {
            return;
        }

        _currentPageIndex--;
        LoadPage();
    }

    private void Page_Next(object sender, RoutedEventArgs e)
    {
        if (_currentChapterPages.Count == 0 || _currentPageIndex >= _currentChapterPages.Count - 1)
        {
            return;
        }

        _currentPageIndex++;
        LoadPage();
    }

    private void LoadPage()
    {
        try
        {
            if (_currentChapterPages.Count == 0)
            {
                ClearReadingContent();
                HideTasksPanel();
                UpdateNavigationButtons();
                return;
            }

            if (_currentPageIndex < 0 || _currentPageIndex >= _currentChapterPages.Count)
            {
                ClearReadingContent();
                HideTasksPanel();
                MessageBox.Show("Requested page is outside of the available range.",
                                "Navigation Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                UpdateNavigationButtons(_currentChapterPages);
                return;
            }

            var page = _currentChapterPages[_currentPageIndex];
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
            UpdateNavigationButtons(_currentChapterPages);
            UpdateNavigationHighlight();
        }
    }

    private void ShowReadingPage(PageModel page)
    {
        myRichBox.Visibility = Visibility.Visible;
        TasksScrollViewer.Visibility = Visibility.Collapsed;
        TasksPanel.Children.Clear();
        ClearReadingContent();

        if (!string.Equals(page.Content.Format, "richText", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show($"Unsupported page format: {page.Content.Format}",
                            "Content Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(page.Content.Source))
        {
            MessageBox.Show($"Page \"{page.Title}\" does not specify a content source.",
                            "Content Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
            return;
        }

        var relativePath = page.Content.Source.Replace('/', Path.DirectorySeparatorChar);
        var filePath = Path.Combine(AssetsRoot, relativePath);
        Trace.WriteLine($"Loading page: {filePath}");

        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var documentRange = new TextRange(myRichBox.Document.ContentStart, myRichBox.Document.ContentEnd);
            documentRange.Load(stream, DataFormats.Rtf);
            myRichBox.ScrollToHome();

            _lastLoadedPageIndex = _currentPageIndex;
        }
        catch (FileNotFoundException)
        {
            MessageBox.Show($"Page file not found: {filePath}",
                            "Read Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

            if (_lastLoadedPageIndex >= 0)
            {
                _currentPageIndex = _lastLoadedPageIndex;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open page file: {ex.Message}",
                            "Read Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
        }
    }

    private void ShowTaskPage(PageModel page)
    {
        myRichBox.Visibility = Visibility.Collapsed;
        ClearReadingContent();
        TasksScrollViewer.Visibility = Visibility.Visible;

        if (page.Tasks.Count == 0)
        {
            RenderTasks(Array.Empty<LearningTaskModel>(), showPlaceholder: true);
        }
        else
        {
            RenderTasks(page.Tasks, showPlaceholder: false);
        }

        TasksScrollViewer.ScrollToHome();
        _lastLoadedPageIndex = _currentPageIndex;
    }

    private void ClearReadingContent()
    {
        myRichBox.Document.Blocks.Clear();
    }

    private void HideTasksPanel()
    {
        TasksScrollViewer.Visibility = Visibility.Collapsed;
        TasksPanel.Children.Clear();
    }

    private static PageDisplayMode DetermineDisplayMode(PageModel page)
    {
        return page.KindNormalized switch
        {
            "quiz" => PageDisplayMode.Tasks,
            "code" => PageDisplayMode.Tasks,
            "assessment" => PageDisplayMode.Tasks,
            "tasks" => PageDisplayMode.Tasks,
            _ => PageDisplayMode.Reading
        };
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
                    Text = "Tasks for this section will appear here.",
                    FontStyle = FontStyles.Italic,
                    Foreground = Brushes.Gray
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
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Background = Brushes.WhiteSmoke
        };

        var layout = new StackPanel();
        container.Child = layout;

        var heading = !string.IsNullOrWhiteSpace(task.Title) ? task.Title : "Quiz";
        layout.Children.Add(new TextBlock
        {
            Text = heading,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold
        });

        if (!string.IsNullOrWhiteSpace(task.Question))
        {
            layout.Children.Add(new TextBlock
            {
                Text = task.Question,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 6, 0, 0)
            });
        }

        if (task.Scoring.Points > 0)
        {
            layout.Children.Add(new TextBlock
            {
                Text = $"Points: {task.Scoring.Points}" + (task.Scoring.Partial ? " (partial credit supported)" : string.Empty),
                FontSize = 12,
                Foreground = Brushes.DimGray,
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
            Content = "Check Answer",
            Padding = new Thickness(12, 6, 12, 6),
            Margin = new Thickness(0, 0, 10, 0)
        };

        var resetButton = new Button
        {
            Content = "Clear Selection",
            Padding = new Thickness(12, 6, 12, 6)
        };

        var feedbackBlock = new TextBlock
        {
            Margin = new Thickness(0, 10, 0, 0),
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.Gray
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
                Header = "Hints",
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
                    Margin = new Thickness(0, 2, 0, 0)
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
            BorderBrush = Brushes.SlateGray,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Background = Brushes.WhiteSmoke
        };

        var layout = new StackPanel();
        container.Child = layout;

        var heading = !string.IsNullOrWhiteSpace(task.Title) ? task.Title : "Coding Task";
        layout.Children.Add(new TextBlock
        {
            Text = heading,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold
        });

        if (!string.IsNullOrWhiteSpace(task.Prompt))
        {
            layout.Children.Add(new TextBlock
            {
                Text = task.Prompt,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 6, 0, 10)
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
            Margin = new Thickness(0, 0, 0, 10)
        };
        layout.Children.Add(codeEditor);

        var buttonsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal
        };

        var runButton = new Button
        {
            Content = "Run Tests",
            Padding = new Thickness(12, 6, 12, 6),
            Margin = new Thickness(0, 0, 10, 0)
        };

        var resetButton = new Button
        {
            Content = "Reset Code",
            Padding = new Thickness(12, 6, 12, 6)
        };

        buttonsPanel.Children.Add(runButton);
        buttonsPanel.Children.Add(resetButton);
        layout.Children.Add(buttonsPanel);

        var feedbackBlock = new TextBlock
        {
            Margin = new Thickness(0, 10, 0, 0),
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.Gray
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
                Text = "Sample tests:",
                Margin = new Thickness(0, 12, 0, 0),
                FontWeight = FontWeights.SemiBold
            });

            foreach (var test in publicTests)
            {
                var testBlock = new TextBlock
                {
                    TextWrapping = TextWrapping.Wrap,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 12,
                    Margin = new Thickness(0, 6, 0, 0),
                    Text = $"Input:\n{test.Input}\nExpected:\n{test.ExpectedOutput}"
                };
                layout.Children.Add(testBlock);

                if (!string.IsNullOrWhiteSpace(test.Explanation))
                {
                    layout.Children.Add(new TextBlock
                    {
                        Text = test.Explanation,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 2, 0, 0),
                        FontStyle = FontStyles.Italic
                    });
                }
            }
        }
        else if (task.Tests.Count > 0)
        {
            layout.Children.Add(new TextBlock
            {
                Text = "Hidden tests will also be executed when this feature is implemented.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 12, 0, 0),
                Foreground = Brushes.Gray
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
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(1),
            Background = Brushes.WhiteSmoke,
            Child = new TextBlock
            {
                Text = $"Task type \"{task.Type}\" is not supported yet.",
                TextWrapping = TextWrapping.Wrap
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
            context.FeedbackBlock.Text = "Select at least one option before checking.";
            context.FeedbackBlock.Foreground = Brushes.DarkOrange;
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
                toggle.Foreground = Brushes.ForestGreen;
            }
            else if (toggle.IsChecked == true)
            {
                toggle.Foreground = Brushes.Firebrick;
            }
            else
            {
                toggle.ClearValue(Control.ForegroundProperty);
            }
        }

        if (missing.Count == 0 && wrong.Count == 0)
        {
            var explanation = context.Task.Solution?.Explanation;
            context.FeedbackBlock.Text = !string.IsNullOrWhiteSpace(explanation)
                ? explanation
                : "Correct! Well done.";
            context.FeedbackBlock.Foreground = Brushes.ForestGreen;
            return;
        }

        var messages = new List<string>();
        if (wrong.Count > 0)
        {
            messages.Add("Some selected options are incorrect.");
        }

        if (missing.Count > 0)
        {
            messages.Add("Some correct options are not selected.");
        }

        var explanationText = context.Task.Solution?.Explanation;
        if (!string.IsNullOrWhiteSpace(explanationText))
        {
            messages.Add(explanationText!);
        }

        context.FeedbackBlock.Text = string.Join(" ", messages);
        context.FeedbackBlock.Foreground = Brushes.Firebrick;
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
            toggle.ClearValue(Control.ForegroundProperty);
        }

        context.FeedbackBlock.Text = string.Empty;
        context.FeedbackBlock.Foreground = Brushes.Gray;
    }

    private void RunCodeTask_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not CodeTaskContext context)
        {
            return;
        }

        context.FeedbackBlock.Foreground = Brushes.DarkSlateBlue;
        context.FeedbackBlock.Text = "Code execution is not implemented yet. This feature is planned for a future update.";
    }

    private void ResetCodeTask_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not CodeTaskContext context)
        {
            return;
        }

        context.CodeEditor.Text = context.StarterCode ?? string.Empty;
        context.FeedbackBlock.Text = "Code template restored.";
        context.FeedbackBlock.Foreground = Brushes.Gray;
    }

    private void UpdateNavigationButtons(IReadOnlyList<PageModel>? pages = null)
    {
        if (pages is null || pages.Count == 0)
        {
            backButton.IsEnabled = false;
            nextButton.IsEnabled = false;
            return;
        }

        backButton.IsEnabled = _currentPageIndex > 0;
        nextButton.IsEnabled = _currentPageIndex < pages.Count - 1;
    }

    private enum PageDisplayMode
    {
        Reading,
        Tasks
    }

    private sealed class PageButtonInfo
    {
        public PageButtonInfo(string chapterId, int pageIndex)
        {
            ChapterId = chapterId;
            PageIndex = pageIndex;
        }

        public string ChapterId { get; }
        public int PageIndex { get; }
        public Button? LeftButton { get; set; }
        public TextBlock? LeftLabel { get; set; }
        public Button? StepperButton { get; set; }
        public TextBlock? StepperLabel { get; set; }
        public PageModel? Page { get; set; }
    }

    private sealed class PageSelection
    {
        public PageSelection(string chapterId, int pageIndex)
        {
            ChapterId = chapterId;
            PageIndex = pageIndex;
        }

        public string ChapterId { get; }
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




