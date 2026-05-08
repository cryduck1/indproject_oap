using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace AnketaSearch
{
    public partial class MainWindow : Window
    {
        private List<Anketa> _allAnkety = new List<Anketa>();
        private readonly char _delimiter = ';';
        
        private Dictionary<string, int> _sortStates = new Dictionary<string, int>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnLoadFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Текстовые файлы (*.txt;*.csv)|*.txt;*.csv|Все файлы (*.*)|*.*",
                Title = "Выберите файл с анкетами"
            };

            if (dialog.ShowDialog() == true)
            {
                LoadFile(dialog.FileName);
            }
        }

        private void LoadFile(string filePath)
        {
            try
            {
                _allAnkety.Clear();
                _sortStates.Clear();

                byte[] bytes = File.ReadAllBytes(filePath);
                string content = DetectEncodingAndGetString(bytes, out string detectedEncoding);
                string[] lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                if (lines.Length < 2)
                {
                    LblStatus.Text = "⚠️ Файл пуст или содержит только заголовок.";
                    return;
                }

                for (int i = 1; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line)) continue;

                    var parts = line.Split(_delimiter);
                    if (parts.Length >= 8)
                    {
                        _allAnkety.Add(new Anketa
                        {
                            Id = parts[0].Trim(),
                            Fio = parts[1].Trim(),
                            BirthDate = parts[2].Trim(),
                            City = parts[3].Trim(),
                            Phone = parts[4].Trim(),
                            Email = parts[5].Trim(),
                            Education = parts[6].Trim(),
                            Comment = parts[7].Trim()
                        });
                    }
                }

                DgAnkety.ItemsSource = _allAnkety;
                LblStatus.Text = $"✅ Загружено {_allAnkety.Count} анкет. Кодировка: {detectedEncoding}";
                TxtDetails.Text = "Выберите анкету для просмотра полной информации...";
            }
            catch (Exception ex)
            {
                LblStatus.Text = $"❌ Ошибка загрузки: {ex.Message}";
            }
        }

        private string DetectEncodingAndGetString(byte[] bytes, out string encodingName)
        {
            encodingName = "не определена";
            if (bytes.Length == 0) return string.Empty;

            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                encodingName = "UTF-8 (BOM)";
                return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
            }
            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            {
                encodingName = "UTF-16 LE";
                return Encoding.Unicode.GetString(bytes);
            }

            try
            {
                string test = Encoding.UTF8.GetString(bytes);
                if (!test.Contains('\uFFFD'))
                {
                    encodingName = "UTF-8";
                    return test;
                }
            }
            catch { }

            encodingName = "Windows-1251";
            return Encoding.GetEncoding(1251).GetString(bytes);
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            string query = TxtSearch.Text.Trim().ToLower();
            _sortStates.Clear();

            if (string.IsNullOrEmpty(query))
            {
                DgAnkety.ItemsSource = _allAnkety;
                LblStatus.Text = "Показаны все анкеты";
                return;
            }

            var filtered = _allAnkety.Where(a =>
                a.Fio.ToLower().Contains(query) ||
                a.BirthDate.ToLower().Contains(query) ||
                a.City.ToLower().Contains(query) ||
                a.Education.ToLower().Contains(query) ||
                a.Comment.ToLower().Contains(query) ||
                a.Email.ToLower().Contains(query) ||
                a.Id.ToLower().Contains(query) ||
                a.Phone.ToLower().Contains(query)
            ).ToList();

            DgAnkety.ItemsSource = filtered;
            LblStatus.Text = $"🔍 Найдено {filtered.Count} анкет по запросу: \"{TxtSearch.Text}\"";
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            TxtSearch.Clear();
            _sortStates.Clear();
            DgAnkety.ItemsSource = _allAnkety;
            LblStatus.Text = _allAnkety.Count > 0 ? "🔄 Показаны все анкеты" : "Файл не загружен";
        }

        private void DgAnkety_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgAnkety.SelectedItem is Anketa selected)
            {
                TxtDetails.Text = $"📋 {selected.Fio}\n" +
                                  $"Дата рождения: {selected.BirthDate}\n" +
                                  $"Город: {selected.City}\n" +
                                  $"Телефон: {selected.Phone}\n" +
                                  $"Email: {selected.Email}\n" +
                                  $"Образование: {selected.Education}\n" +
                                  $"Комментарий: {selected.Comment}";
            }
            else
            {
                TxtDetails.Text = "Выберите анкету для просмотра полной информации...";
            }
        }

        private void DgAnkety_Sorting(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true;
            var propertyName = e.Column.SortMemberPath;
            if (string.IsNullOrEmpty(propertyName)) return;

            if (!_sortStates.ContainsKey(propertyName))
                _sortStates[propertyName] = 0;

            _sortStates[propertyName] = (_sortStates[propertyName] + 1) % 3;

            var view = CollectionViewSource.GetDefaultView(DgAnkety.ItemsSource);
            if (view == null) return;

            view.SortDescriptions.Clear();

            if (_sortStates[propertyName] == 1)
            {
                view.SortDescriptions.Add(new SortDescription(propertyName, ListSortDirection.Ascending));
            }
            else if (_sortStates[propertyName] == 2)
            {
                view.SortDescriptions.Add(new SortDescription(propertyName, ListSortDirection.Descending));
            }

            view.Refresh();
        }
    }

    public class Anketa
    {
        public string Id { get; set; } = string.Empty;
        public string Fio { get; set; } = string.Empty;
        public string BirthDate { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Education { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
    }
}