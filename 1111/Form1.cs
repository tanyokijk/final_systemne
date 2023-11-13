using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Reflection.Emit;
using System.Windows.Forms.VisualStyles;
namespace _1111
{

    public partial class Form1 : Form
    {
        private string destinationFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private string inputfolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string[] forbiddenWords;
        private BackgroundWorker backgroundWorker = new BackgroundWorker();
        public Form1()
        {
            InitializeComponent();
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.WorkerSupportsCancellation = true;
            backgroundWorker.DoWork += backgroundWorker1_DoWork;
            backgroundWorker.ProgressChanged += backgroundWorker1_ProgressChanged;
            backgroundWorker.RunWorkerCompleted += backgroundWorker1_RunWorkerCompleted;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Title = "Виберіть текстовий файл";
            openFileDialog.Filter = "Текстові файли (*.txt)|*.txt|Всі файли (*.*)|*.*";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string selectedFilePath = openFileDialog.FileName;
                string[] lines = File.ReadAllLines(selectedFilePath);

                string fileContent = string.Join(" ", lines);

                forbiddenWords = fileContent.Split(new[] { ' ', '.', ',' }, StringSplitOptions.RemoveEmptyEntries);
            }
            label1.Text = "";
            foreach (string world in forbiddenWords)
            {
                label1.Text += world + " ";
            }
            ResizeLabelToFitText();

        }
        private void button2_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(destinationFolder))
            {
                MessageBox.Show("Виберіть папку для пошуку файлів!", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else if (forbiddenWords == null || forbiddenWords.Length == 0)
            {
                MessageBox.Show("Введіть або завантажте файл зі забороненими словами!", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                if (!backgroundWorker.IsBusy)
                {
                    backgroundWorker.RunWorkerAsync(destinationFolder);
                }
                else
                {
                    MessageBox.Show("Пошук і копіювання вже виконуються.", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            

        }



        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {

            SearchForWordsInFiles(inputfolder);
        }

        private void SearchForWordsInFiles(string directoryPath)
        {
            try
            {
                string[] files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);

                int processedFiles = 0;
                int totalFiles = files.Length;

                foreach (string filePath in files)
                {

                    if (IsTextFile(filePath))
                    {
                        string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

                        MatchCollection matches = Regex.Matches(fileContent, @"\b\w+\b");

                        foreach (Match match in matches)
                        {
                            string word = match.Value;

                            foreach (string forbiddenWord in forbiddenWords)
                            {
                                if (word.ToLower() == forbiddenWord.ToLower())
                                {
                                    if (Path.GetFileName(filePath) != "Report.txt")
                                    { CopyAndReplaceFile(filePath); }
                                    break;
                                }
                            }
                        }
                    }

                    processedFiles++;
                    int progressPercentage = (int)(((double)processedFiles / totalFiles) * 100);

                    progressBar1.Invoke((MethodInvoker)delegate
                    {
                        progressBar1.Value = progressPercentage;
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Виникла помилка при пошуку у папці {directoryPath}: {ex.Message}");
            }
        }


        private void CopyAndReplaceFile(string sourceFilePath)
        {
            try
            {
                string fileName = Path.GetFileName(sourceFilePath);
                string destinationPath = Path.Combine(destinationFolder, fileName);

                File.Copy(sourceFilePath, destinationPath, true);
                Console.WriteLine($"Файл скопійовано до {destinationPath}");

                string content = File.ReadAllText(destinationPath, Encoding.UTF8);
                int replacementsCount = 0;
                List<string> replacedWords = new List<string>();

                foreach (string forbiddenWord in forbiddenWords)
                {
                    int wordReplacements = Regex.Matches(content, $@"\b{forbiddenWord}\b", RegexOptions.IgnoreCase).Count;
                    replacementsCount += wordReplacements;

                    if (wordReplacements > 0)
                    {
                        replacedWords.Add(forbiddenWord);
                        content = content.Replace(forbiddenWord, "*******");
                    }
                }

                File.WriteAllText(destinationPath, content, Encoding.UTF8);

                string reportFilePath = Path.Combine(destinationFolder, "Report.txt");

                if (replacementsCount > 0 && !IsReportEntryExists(reportFilePath, fileName, sourceFilePath))
                {
                    string replacedWordsString = replacedWords.Any() ? string.Join(", ", replacedWords) : "немає";
                    string reportEntry = $"{DateTime.Now}: Знайдено слово(а) у файлі {fileName}, Шлях: {sourceFilePath}, Розмір: {new FileInfo(destinationPath).Length} байт, Кількість замін: {replacementsCount}, Замінені слова: {replacedWordsString}" + Environment.NewLine;

                    if (!File.Exists(reportFilePath))
                    {
                        File.WriteAllText(reportFilePath, "", Encoding.UTF8);
                    }

                    File.AppendAllText(reportFilePath, reportEntry, Encoding.UTF8);
                    Console.WriteLine($"Додано інформацію до файлу звіту: {reportFilePath}");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при копіюванні або заміні файлів: {ex.Message}");
            }
        }

        private bool IsReportEntryExists(string reportFilePath, string fileName, string destinationPath)
        {
            try
            {
                string reportEntry = $"{DateTime.Now}: Знайдено слово(а) у файлі {fileName}, Шлях: {destinationPath}";
                return File.ReadAllText(reportFilePath).Contains(reportEntry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при перевірці існування запису в файлі звіту: {ex.Message}");
                return false;
            }
        }



        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MessageBox.Show("Пошук і копіювання завершено.", "Завершено", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private bool IsTextFile(string filePath)
        {
            string[] textExtensions = { ".txt", ".log", ".xml", ".cs", ".html", ".cpp", ".java" };
            string extension = Path.GetExtension(filePath);

            return textExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }


        private void button3_Click(object sender, EventArgs e)
        {
            if (!backgroundWorker.IsBusy)
            {
                backgroundWorker.CancelAsync();
            }
            else
            { 
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

            string fileContent = string.Join(" ", textBox1.Text);

            forbiddenWords = fileContent.Split(new[] { ' ', '.', ',' }, StringSplitOptions.RemoveEmptyEntries);

        }

        private void button4_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "Виберіть папку для пошуку файлів";
            folderBrowserDialog.ShowNewFolderButton = false;

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                inputfolder = folderBrowserDialog.SelectedPath;
            }
            label7.Text = inputfolder;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "Виберіть папку для запису файлів";
            folderBrowserDialog.ShowNewFolderButton = false;

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                destinationFolder = folderBrowserDialog.SelectedPath;
            }
            label6.Text = destinationFolder;
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string fileContent = string.Join(" ", textBox1.Text);

                forbiddenWords = fileContent.Split(new[] { ' ', '.', ',' }, StringSplitOptions.RemoveEmptyEntries);
                label1.Text = "";
                foreach (string world in forbiddenWords)
                {
                    label1.Text += world + " ";
                }
                ResizeLabelToFitText();
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            try
            {
                string reportFilePath = Path.Combine(destinationFolder, "Report.txt");

                if (File.Exists(reportFilePath))
                {
                    Process.Start(reportFilePath); 
                }
                else
                {
                    MessageBox.Show("Файл звіту не існує.", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при відкритті файлу звіту: {ex.Message}");
            }
        }
        private void ResizeLabelToFitText()
        {
            // Встановлюємо AutoSize в true, щоб текст автоматично переносився на новий рядок
            label1.AutoSize = true;

            // Встановлюємо MaximumSize для обмеження розміру Label
            label1.MaximumSize = new Size(300, 0); // Замініть 300 на бажаний максимальний розмір
        }

        private void button7_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("explorer.exe", destinationFolder);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при відкритті папки: {ex.Message}");
            }
        }
    }
}