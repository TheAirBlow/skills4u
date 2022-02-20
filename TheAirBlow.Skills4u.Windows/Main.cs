using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using AngleSharp;
using AngleSharp.Html.Parser;
using AngleSharp.Scripting;
using Jint.Native;
using Jint.Native.Array;

namespace TheAirBlow.Skills4u.Windows
{
    public partial class Main : Form
    {
        /// <summary>
        /// Pair of TestID and Number
        /// </summary>
        private class TestIdNumberPair
        {
            public string TestId;
            public string Number;
        }

        private List<string> _allAnswers = new();
        private Thread _thread;

        public Main() => InitializeComponent();

        /// <summary>
        /// Solve the exercise
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            _allAnswers.Clear();
            panel1.Enabled = true;
            panel1.Visible = true;
            comboBox1.Items.Clear();
            button1.Enabled = false;
            textBox1.Enabled = false;
            checkedListBox1.Items.Clear();
            var link = textBox1.Text;
            
            // ReSharper disable once AsyncVoidLambda
            _thread = new Thread(async () => {
                try {
                    Invoke(() => {
                        progressBar1.Style = ProgressBarStyle.Marquee;
                        progressBar1.Value = 0;
                    });
                    if (!link.StartsWith("https://skills4u.ru/"))
                        throw new Exception("Not skills4u!");

                    Invoke(() => label2.Text = "Загружаем сайт...");
                    var context = BrowsingContext.New(Configuration.Default
                        .WithJs().WithDefaultLoader());
                    var js = context.GetService<JsScriptingService>();
                    var document = await context.OpenAsync(link);
                    Invoke(() => label2.Text = "Получаем ответы...");
                    var lesson = (ArrayInstance)js?.EvaluateScript(document, 
                        "window.lesson");

                    // Enumerate each exercise
                    var comboItems = new List<string>();
                    var list = lesson?.GetOwnProperties().Select(
                        p => p.Value.Value)!;
                    var jsValues = list as JsValue[] ?? list?.ToArray();
                    Invoke(() => {
                        progressBar1.Style = ProgressBarStyle.Blocks;
                        progressBar1.Maximum = jsValues!.Count();
                    });
                    for (var i = 0; i < jsValues!.Count() - 1; i++) {
                        Invoke(() => {
                            label2.Text = $"Решаем задание {i + 1} из {jsValues!.Count() - 1}";
                            progressBar1.Increment(1);
                        });
                        var item = jsValues[i];
                        var obj = item.AsObject();
                        var question = obj.GetProperty("question").Value.AsString();
                        var answer = obj.GetProperty("answer").Value.AsString();
                        var xml = new XmlDocument();
                        xml.LoadXml(question);
                        comboItems.Add($"Задание №{i + 1}: {xml.InnerText}");
                        xml.LoadXml(answer);
                        _allAnswers.Add($"Ответ: {xml.InnerText}");
                    }
                    
                    Invoke(() => {
                        panel1.Enabled = false;
                        panel1.Visible = false;
                        button1.Enabled = true;
                        textBox1.Enabled = true;
                        // ReSharper disable once CoVariantArrayConversion
                        comboBox1.Items.AddRange(comboItems.ToArray());
                        comboBox1.SelectedIndex = 0;
                    });
                } catch (Exception ex) {
                    Invoke(() => {
                        panel1.Enabled = true;
                        panel1.Visible = true;
                        button1.Enabled = true;
                        textBox1.Enabled = true;
                        progressBar1.Style = ProgressBarStyle.Marquee;
                        label2.Text = $"Ошибка во время решения, ожидание команд...";
                    });
                    Console.WriteLine(ex);
                    MessageBox.Show("Не удалось решить задание.\nПроверьте код, " +
                                    "который вы ввели, а также связь с Интернетом." +
                                    $"\n{ex.Message}", 
                        "Ошибка во время решения!", MessageBoxButtons.OK, 
                        MessageBoxIcon.Error);
                }
            });
            
            _thread.Start();
        }
        
        /// <summary>
        /// Select an exercise
        /// </summary>
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            checkedListBox1.Items.Clear();
            checkedListBox1.Items.Add(_allAnswers[comboBox1.SelectedIndex]);

            button3.Enabled = comboBox1.SelectedIndex - 1 != -1;
            button4.Enabled = comboBox1.SelectedIndex + 1 != comboBox1.Items.Count;
        }
        
        private void OnClosed(object sender, EventArgs e)
            => Environment.Exit(0);

        /// <summary>
        /// Verify bearer token
        /// </summary>
        private void Main_Load(object sender, EventArgs e)
            => Closed += OnClosed;

        private void button3_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex - 1 != -1)
                comboBox1.SelectedIndex--;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex + 1 != comboBox1.Items.Count)
                comboBox1.SelectedIndex++;
        }
    }
}