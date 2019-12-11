using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace Sentiment_Analysis___PLC
{
    public partial class Form1 : Form
    {

        private const char SEPARATOR = ',';
        private Dictionary<string, double> _evidences;
        private static Dictionary<string, double> word_freq;
        private static int _totalWords; // not used in analysis procedure
        private static Dictionary<string, double> positiveWords,negativeWords;
   
        private static bool IsDigitsOnly(string str)
        {
            foreach (char c in str)
            {
                if (c < '0' || c > '9')
                    return false;
            }

            return true;
        }
        private static double ContainsKeyValue(Dictionary<string, double> DICT, string key)
        {
            if (!DICT.ContainsKey(key))
                return 0;
            return DICT[key];
        }
        private static string BuildDecision(string[] words) {

            string dec;
            double PScore = 0;
            double NScore = 0;
            foreach(string w in words)
            {
                PScore += (ContainsKeyValue(positiveWords, w) / ( Math.Floor(Math.Log10(ContainsKeyValue(positiveWords, w)) + 1)*10));
                NScore += (ContainsKeyValue(negativeWords, w) / (Math.Floor(Math.Log10(ContainsKeyValue(negativeWords, w)) + 1) * 10));

            }


            if (PScore / NScore > 1.5)
            {

                dec = "POSITIVE";
                
            }
            else if (PScore / NScore > 1.3) dec = "NEUTRAL";
            else dec = "NEGATIVE";
            return dec;// + (PScore / NScore).ToString();

        } 
        private static string[] BuildStringArray(string s)
        {

            return Regex.Split(s, @"\s+", RegexOptions.Singleline);
            
        }
        private Dictionary<string, double> BuildEvidenceArray(string filePath)
        {
             try
            {
                if (!File.Exists(filePath)) return _evidences;

                Debug.Write("Loading evidence cache from " + filePath);
                
                using (var file = new StreamReader(filePath))
                {
                    _evidences = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                    string line;
                    while ((line = file.ReadLine()) != null)
                    {
                        var keyValue = line.Split(SEPARATOR);
                        if (!_evidences.ContainsKey(keyValue[0]))
                        {
                            var value = int.Parse(keyValue[1]);
                            _evidences.Add(keyValue[0], value);
                            _totalWords += value;
                        }
                        else
                        {
                            throw new Exception(
                                string.Format("Duplicate entries of {0} found while loading evidences from {1}",
                                              keyValue[0], filePath));
                        }
                    }

                    return _evidences;
                }
            }
            catch (Exception ex)
            {
                Debug.Write("Error while loading evidence from cache", ex.ToString());
                throw;
            }



        }
        private void FileFreqCheck(Dictionary<string, double> file_freq)
        {
           
            var wordsF = new Dictionary<string, double>(BuildEvidenceArray("D:\\WordFrequency.txt"));
          
            double currentCount;
            
            var DifferencesToAdd = file_freq.Keys.Except(wordsF.Keys);
        
            //MessageBox.Show(DifferencesToAdd.ToString());
            var SimilaritiesToIncrement = file_freq.Keys.Intersect(wordsF.Keys);
         
            foreach (string dict in SimilaritiesToIncrement) //similarities
            {
            
                if (wordsF.TryGetValue(dict, out currentCount))
                    wordsF[dict] = currentCount + 1;  
            }
            foreach (string dict
                in DifferencesToAdd)
            {
                wordsF.Add(dict, 1);
            }
           
            using (StreamWriter file = new StreamWriter("D:\\WordFrequency.txt")) //file might require to be empty
                foreach (var entry in wordsF)
                    file.WriteLine("{0},{1}", entry.Key, entry.Value);

            // wordsF.AddOrUpdate(id, 1, (id, count) => count + 1);
            // file_freq.Add(key, value)

        }
        private static void DetermineFrequency(string[] words)
        {
            foreach (string word in words)
            {
                if (word_freq.ContainsKey(word))
                    word_freq[word] += 1;
                else
                    word_freq.Add(word, 1);
            }
            
        }
        private static string[] ValidateAllWords(string[] words)
        {
            int i = 0;
            foreach (string s in words)
                if (IsDigitsOnly(s) || Regex.IsMatch(s, @"^[a-zA-Z]+$"))
                    words[i++] = s;
                else words[i++] = "";

            return words;
        }
        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !char.IsLetter(e.KeyChar) && !char.IsControl(e.KeyChar)
                 && !char.IsSeparator(e.KeyChar) && !char.IsDigit(e.KeyChar);
        }
        private void SetLabelColor(object o,string s)
        {
            if (s == "POSITIVE")
            label2.ForeColor = System.Drawing.Color.Green;
            if (s == "NEGATIVE")
                label2.ForeColor = System.Drawing.Color.Red;
            if (s == "NEUTRAL")
                label2.ForeColor = System.Drawing.Color.Blue; ;
        }
        public Form1()
        {
            
            positiveWords = new Dictionary<string, double>(BuildEvidenceArray("D:\\PositiveEvidence.txt"));
            negativeWords = new Dictionary<string, double>(BuildEvidenceArray("D:\\NegativeEvidence.txt"));
            InitializeComponent();
            textBox1.KeyPress += new KeyPressEventHandler(textBox1_KeyPress);
         
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Count() < 3 || textBox1.Text.Count() > 255)
            {
                
                MessageBox.Show("Text < 3 or Text > 255!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            else
            {
                double i = 1;
                string[] InputText = BuildStringArray((textBox1.Text.Trim().ToLower()));
                InputText = ValidateAllWords(InputText);
                string[] q = InputText.Distinct().ToArray();
                word_freq = q.ToDictionary(item => item, item => i);
                FileFreqCheck(word_freq);
                label2.Text= BuildDecision(InputText);
                //AFTER BUILDING DECISION WE COULD USE FileFreqCheck(word_freq); AND ADD 
                //THE WORDS TO A POSITIVE OR NEGATIVE CONTEXT
                SetLabelColor(this, label2.Text);


            }
            
        }
    }
}
