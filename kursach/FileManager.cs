using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows;

namespace kursachFile
{ 
    public class Root
    {
        public string Heading { get; set; }
        public Content Content { get; set; }
    }

    public class Content
    {
        public int ChapterCount { get; set; }
        public Dictionary<string, Chapter> Chapters { get; set; }
    }

    public class Chapter
    {
        public string ChapterName { get; set; }
        public ChapterContent ChapterContent { get; set; }
    }

    public class ChapterContent
    {
        public int Pages { get; set; }
        public Dictionary<string, Page> PagesContent { get; set; }
    }

    public class Page
    {
        public string PageType { get; set; }
        public string PageContent { get; set; }
    }
}
