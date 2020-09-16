using SubtitlesParser.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using GoogleTranslateFreeApi;
using static GoogleTranslateFreeApi.TranslationData.ExtraTranslations;

namespace LEAssistant
{
    public partial class Form1 : Form
    {
        private List<SubtitleItem> items;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var fileContent = string.Empty;
            var filePath = string.Empty;

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
                string str="";
                foreach (var item in items) 
                {
                    foreach (var wordList in item.Lines)
                    {

                        str += wordList+" ";
                    }
                }
                var words = Regex.Split(Regex.Replace(str.ToLower(), @"[^a-zA-Z ']+", ""), " ")
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
                    lvi.Text = count.Word+" "+count.Count;
                    listView1.Items.Add(lvi);
                }
                Console.WriteLine("count = " + countwords);
            }
           
        }
        
        private async void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                
                string word = "";
                if (listView1.SelectedItems.Count > 0)
                {
                    
                    word = listView1.SelectedItems[0].Text.ToString().Split(' ')[0];
                    Console.WriteLine(word);
                    await TranslateWordAsync(word);
                }
            }
            catch { }
        }
        private async Task<string> TranslateWordAsync(string str)
        {
            GoogleTranslator translator = new GoogleTranslator();

            var result = await translator.TranslateAsync(str, Language.English, Language.Russian);

            if (result.ExtraTranslations != null) 
            { 
                Console.WriteLine(result.ExtraTranslations.ToString()); 
                richTextBox1.Text = result.ExtraTranslations.ToString();
            }

            
            return "";
        }
    }
}
