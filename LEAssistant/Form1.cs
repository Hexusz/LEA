using SubtitlesParser.Classes;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace LEAssistant
{
    public partial class Form1 : Form
    {
        private List<SubtitleItem> items;
        public static string BearerCode;

        public string WordsStr = "";
        public string SafeFileName = "";
        public string FileName = "";

        public Form1()
        {
            InitializeComponent();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            var fileContent = string.Empty;
            var filePath = string.Empty;
            await GetBearer();
            var asd = TranslateWord("I'm robot");
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                //openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "All files (*.*)|*.*";
                //openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    filePath = openFileDialog.FileName;

                    //Read the contents of the file into a stream
                    // var fileStream = openFileDialog.OpenFile();

                    var parser = new SubtitlesParser.Classes.Parsers.SubParser();
                    using (var fileStream = openFileDialog.OpenFile())
                    {
                        items = parser.ParseStream(fileStream);
                    }

                }

                SafeFileName = openFileDialog.SafeFileName;
                FileName = openFileDialog.FileName;

                foreach (var item in items)
                {
                    foreach (var wordList in item.Lines)
                    {

                        WordsStr += wordList + " ";
                    }
                }

                var words = Regex.Split(Regex.Replace(WordsStr.ToLower(), @"[^a-zA-Z ']+", ""), " ")
                    .Where(x => !string.IsNullOrEmpty(x))
                    .GroupBy(g => g)
                    .Select(s => new { Word = s.Key, Count = s.Count() });
                int countwords = 0;

                foreach (var count in words.OrderByDescending(x => x.Count).ToList())
                {
                    countwords++;
                    Console.WriteLine(count);
                    ListViewItem lvi = new ListViewItem();
                    // установка названия файла
                    lvi.Text = count.Word + " " + count.Count;
                    listView1.Items.Add(lvi);
                }
            }

        }

        public Root TranslateWord(string word)
        {
            Root root = new Root();

            using (var wb = new WebClient())
            {
                HttpClient httpClient = new HttpClient
                {
                    BaseAddress = new Uri("https://developers.lingvolive.com")
                };

                httpClient.DefaultRequestHeaders.Add($"Authorization", $"Bearer {BearerCode}");

                var result = httpClient.GetAsync($"api/v1/Minicard?text={word}&srcLang=1033&dstLang=1049").Result;
                var str = result.Content.ReadAsStringAsync();

                if (result.StatusCode != HttpStatusCode.OK)
                {
                    return root;
                }

                var ResultsList = JsonConvert.DeserializeObject<Root>(str.Result);

                root = ResultsList;
            }

            return root;
        }

        public async Task GetBearer()
        {
            using (var wb = new WebClient())
            {
                HttpClient httpClient = new HttpClient
                {
                    BaseAddress = new Uri("https://developers.lingvolive.com")
                };

                httpClient.DefaultRequestHeaders.Add($"Authorization", $"Basic MzgyNDdjOTUtNWJkNS00NWVhLWJiOTItNzY2YTlkODY5MTg4OmRlN2Y0MGQ0MTE1ZjQwOTQ5ZTYzYWE0MjYxMGViODcy");

                var result = await httpClient.PostAsync("/api/v1.1/authenticate/", null);
                BearerCode = await result.Content.ReadAsStringAsync();
            }
        }

        public class Root
        {
            public int SourceLanguage { get; set; }
            public int TargetLanguage { get; set; }
            public string Heading { get; set; }
            public Translation2 Translation { get; set; }
            public List<object> SeeAlso { get; set; }
        }

        public class Translation2
        {
            public string Heading { get; set; }
            public string Translation { get; set; }
            public string DictionaryName { get; set; }
            public string SoundName { get; set; }
            public int Type { get; set; }
            public string OriginalWord { get; set; }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            AnkiSharp.Anki ankiList = new AnkiSharp.Anki(SafeFileName);

            var words = Regex.Split(Regex.Replace(WordsStr.ToLower(), @"[^a-zA-Z ']+", ""), " ")
                .Where(x => !string.IsNullOrEmpty(x))
                .GroupBy(g => g)
                .Select(s => new { Word = s.Key, Count = s.Count() });

            foreach (var word in words)
            {
                var translate = TranslateWord(word.Word);
                if (translate.Translation == null) { continue; }

                ankiList.AddItem(translate.Translation.Heading, translate.Translation.Translation);

            }
            ankiList.CreateApkgFile(FileName.Replace(SafeFileName, ""));
        }

        private void button3_Click(object sender, EventArgs e)
        {
            AnkiSharp.Anki ankiList = new AnkiSharp.Anki("Phrases " + SafeFileName);
            foreach (var item in items.Take(2))
            {
                var translate = TranslateWord(string.Join(" ", item.Lines));
                if (translate.Translation == null) { continue; }

                ankiList.AddItem(translate.Translation.Heading, translate.Translation.Translation);

            }
            ankiList.CreateApkgFile(FileName.Replace(SafeFileName, ""));

        }

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            var item = listView1.SelectedItems;
            if (item.Count <= 0) { return; }

            var str = Regex.Replace(item[0].Text, "[0-9]", "", RegexOptions.IgnoreCase);

            var translate = TranslateWord(str);
            if (translate.Translation == null)
            {
                richTextBox1.Text = "Ошибка";
                return;
            }

            richTextBox1.Text = translate.Translation.Heading + "\n\n" + translate.Translation.Translation;
        }
    }
}
