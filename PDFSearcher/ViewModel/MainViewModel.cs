using GalaSoft.MvvmLight.CommandWpf;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using Microsoft.Win32;
using PDFSearcher.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System;

namespace PDFSearcher.ViewModel
{
    class MainViewModel : BaseViewModel
    {
        private string text = "";
        private Regex Regex = null;
        private static Regex RedoRegex(string word) => new Regex("(\\b" + word + "\\w*)", RegexOptions.IgnoreCase);

        private ObservableCollection<Document> collections = new ObservableCollection<Document>();
        public ObservableCollection<Document> Collections { get { return collections; } }

        private int selectedIndex = 0;
        public int SelectedIndex { get { return selectedIndex; } set { selectedIndex = value; OnPropertyChanged(); OnPropertyChanged("CurrentDocument"); } }
        public Document CurrentDocument { get { return Collections.Count > 0 ? Collections[selectedIndex] : new Document(); } }

        public RelayCommand openfilesCommand => new RelayCommand(OpenFiles);
        public RelayCommand<string> searchCommand => new RelayCommand<string>(StartSearch);

        private BackgroundWorker worker = new BackgroundWorker();
        private string[] filenames = null;

        public MainViewModel()
        {
            worker.DoWork += Worker_DoWork;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.WorkerReportsProgress = true;
        }

        private void OpenFiles()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "pdf files (*.pdf)|*.pdf";
            dialog.Multiselect = true;
            var success = dialog.ShowDialog();
            if (success.HasValue && success.Value)
            {
                filenames = dialog.FileNames;
            }
        }

        private void StartSearch(string text)
        {
            if (filenames != null)
            {
                this.text = text;
                Regex = RedoRegex(text);
                collections.Clear();
                worker.RunWorkerAsync(filenames);
            }
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            collections.Add((Document)e.UserState);
            OnPropertyChanged("Collections");
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            string[] filenames = e.Argument as string[];
            foreach (string filename in filenames)
            {
                FileInfo file = new FileInfo(filename);
                using (PdfReader reader = new PdfReader(file.FullName))
                {
                    var c = CreateDocument(reader, file.Name);
                    if (0 < c.Matches)
                    {
                        worker.ReportProgress(1, c);
                    }
                }
            }
        }

        private void PopulateCollections(string[] filenames)
        {
            foreach (string filename in filenames)
            {
                FileInfo file = new FileInfo(filename);
                using (PdfReader reader = new PdfReader(file.FullName))
                {
                    var c = CreateDocument(reader, file.Name);
                    if (0 < c.Matches)
                    {
                        collections.Add(c);
                        OnPropertyChanged("Collections");
                    }
                }
            }
            SelectedIndex = 0;
        }

        private Document CreateDocument(PdfReader reader, string name)
        {
            int numOfLines = 0;
            int matchCount = 0;
            Document collection = new Document();
            for (int i = 1; i <= reader.NumberOfPages; i++)
            {
                var lines = PdfTextExtractor.GetTextFromPage(reader, i).Split("\n\r".ToCharArray());
                foreach (string line in lines)
                {
                    numOfLines++;
                    var matches = Regex.Matches(line);
                    if (0 < matches.Count)
                    {
                        collection.Lines.Add(new Model.Line() { Text = line, LineNumber = numOfLines, PageNumber = i });
                        matchCount += matches.Count;
                    }
                }
            }
            collection.Matches = matchCount;
            collection.File = name;
            return collection;
        }
    }
}
