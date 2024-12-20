using kursachFile;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace kursach
{
    public partial class MainWindow : Window
    {
        private object _currentChapter;
        private int _currentPageIndex = 1;
        private int _pageIndex = 1;

        public MainWindow()
        {
            InitializeComponent();

            // Чтение JSON
            string json = File.ReadAllText("../../src/content.json");
            var root = JsonConvert.DeserializeObject<kursachFile.Root>(json);

            if (root != null && root.Content != null && root.Content.Chapters != null)
            {
                GenerateChapterButtons(root.Content.Chapters);
            }
            else
            {
                MessageBox.Show("Не удалось загрузить содержание книги.");
            }

        }

        private void GenerateChapterButtons(Dictionary<string, Chapter> chapters)
        {
            foreach (var chapter in chapters)
            {
                Button chapterButton = new Button
                {
                    Content = chapter.Value.ChapterName,
                    Tag = chapter.Key,
                    Margin = new Thickness(5),
                    Height = 30
                };
                // Chapter tmp;
                chapterButton.Click += (s, e) =>
                {
                    var button = s as Button;
                    _currentChapter = button.Content;
                    _currentPageIndex = 1;
                    OpenTextFile($"{_currentChapter}/{_currentChapter}" + _currentPageIndex+".rtf");
                };

                ChaptersPanel.Children.Add(chapterButton);
            }
        }


        private void Page_Back(object sender, RoutedEventArgs e)
        {
            _currentPageIndex--;
            OpenTextFile($"{_currentChapter}/{_currentChapter}" + _currentPageIndex + ".rtf");
        }

        private void Page_Next(object sender, RoutedEventArgs e)
        {
            _currentPageIndex++;
            OpenTextFile($"{_currentChapter}/{_currentChapter}" + _currentPageIndex + ".rtf");
        }

        public void OpenTextFile(string path)
        {
            Trace.WriteLine(path);
            TextRange range = new TextRange(
                myRichBox.Document.ContentStart,
                myRichBox.Document.ContentEnd
            );


            try
            {
                using (FileStream fileStream = new FileStream("../../src/texts/" + path, FileMode.Open))
                {
                    range.Load(fileStream, DataFormats.Rtf);
                }
                _pageIndex = _currentPageIndex;
            }
            catch(FileNotFoundException)  
            {
                _currentPageIndex = _pageIndex;
            }
        }
    }
}
