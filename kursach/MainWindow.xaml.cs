using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using kursachFile;
using Newtonsoft.Json;

namespace kursach
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // string content = File.ReadAllText("../../src/Сравнение способов измерения дистанции с камер.rtf");

            string json = File.ReadAllText("../../src/content.json");
            var root = JsonConvert.DeserializeObject<kursachFile.Root>(json);

            // Генерация кнопок
            GenerateChapterButtons(root.Content.Chapters);            
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

                chapterButton.Click += (s, e) =>
                {
                    var button = s as Button;
                    OpenTextFile($"{button.Content}.rtf");
                };

                ChaptersPanel.Children.Add(chapterButton);
            }
        }


        // открытие страницы
        public void OpenTextFile(string path)
        {
            TextRange range = new TextRange(
                myRichBox.Document.ContentStart,
                myRichBox.Document.ContentEnd
            );

            using (FileStream fileStream = new FileStream("../../src/texts/" + path, FileMode.Open))
            {
                range.Load(fileStream, DataFormats.Rtf);
            }
        }

        private void Page_Back(object sender, RoutedEventArgs e)
        {

        }

        private void Page_Next(object sender, RoutedEventArgs e)
        {

        }
    }
}
