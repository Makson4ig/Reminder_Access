using System;
using System.Windows;
using System.Data.OleDb;
using System.Data;
using Dapper;
using System.Windows.Threading;

namespace Reminder
{

    public partial class MainWindow : Window
    {
        private OleDbConnection Connect = new OleDbConnection(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source = C:\\Users\\User\\Desktop\\Сдача\\Reminder\\Reminder.mdb"); // Строка подключения к базе данных Access
        private string localDateTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:00"); // Шаблон текущего времени 
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) // Загрузка окна 
        {
            Refresh();
            DispatcherTimer timer = new DispatcherTimer(); // Переменная таймера для запуска метода DateCheck
            timer.Tick += new EventHandler(DateCheck); // Тик 
            timer.Interval = new TimeSpan(0, 0, 5); // Интвервал 
            timer.Start(); // Запуск Таймер диспетчера
        }

        private void DeleteClick_Click(object sender, RoutedEventArgs e) // Кнопка удаления задачи из DataGrid и базы Access
        {
            if (DateGrid.SelectedItems != null) // Проверка выбран ли элементы 
            {
                for (int i = 0; i < DateGrid.SelectedItems.Count; i++) // Цикл по выбранным элементам на DataGrid
                {
                    DataRowView datarowView = DateGrid.SelectedItems[i] as DataRowView; // Переменная просмотра строки данных 
                    if (datarowView != null) // Проверка на пустоту переменной 
                    {
                        string id = datarowView.Row[0].ToString(); // Переменная id с помощью которой будет удаляться запись из БД 
                        Connect.Open(); // Открываю базу данных
                        DataRow dataRow = (DataRow)datarowView.Row; // Переменаня просмотра строки 
                        Connect.Execute("DELETE * FROM Reminder WHERE id = " + (Convert.ToInt16(id))); // Выполнение запроса (Удаление выбранной строки)
                        dataRow.Delete(); // Удаление строки из DataGrid 
                        Connect.Close(); // Закрытие базы данных 
                    }
                }
            }
            else 
            {
                MessageBox.Show("База пустая", "Error");
            }
        }

        private void AddClick_Click(object sender, RoutedEventArgs e) // Кнопка добавления записи
        {
            if (TaskDatePicker.Text.Length != 0 && TaskTextBox.Text != null && TimeTextBox.Text != null) // Проверка условия: Дата, Задача и Время не пустые 
            {
                using (OleDbCommand command = new OleDbCommand(@"select max(id) from Reminder", Connect)) // Переменная выполнения команды 
                {
                    try
                    {
                        Connect.Open(); // Открываем базу данных 
                        OleDbDataReader reader = command.ExecuteReader(); // Переменная чтения запроса выполненного переменной command
                        reader.Read(); // Читаем 
                        var id = reader.GetValue(0).ToString(); // Переменная для добавления id в Базу данных, присваем значение максимально id в Базе данных 
                        if (id == "") id = "0"; // Проверяем если значение пустое, то присваиваем 0, так как в базе пусто
                        reader.Close(); // Закрываем чтение 

                        Connect.Execute("INSERT INTO [Reminder] (ID,[Задача],[Время начало],[Время выполнения]) VALUES (" + (Convert.ToInt16(id)+1) +",'" + TaskTextBox.Text + "','" + DateTime.UtcNow + "','" + Convert.ToDateTime(TaskDatePicker.Text +" "+ TimeTextBox.Text) + "')"); // Выполняем запрос на добавление id, задачи, текущей даты и времени, Даты и Время выполнения задачи.
                        Connect.Close(); // Закрываем базу данных 

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        Connect.Close();
                    }
                    TaskTextBox.Clear(); // Очищаем TextBox
                    TaskDatePicker.Text = ""; // Присваиваем пусто
                    TimeTextBox.Clear(); // Очищаем TextBox
                }
                Refresh(); // Запускаем метод для обновления DataGrid.
            }
            else
            {
                MessageBox.Show("Задача или Дата Пустые", "Error");
            }
            
        }

        public void DateCheck(object sender, EventArgs e) // Проверка даты выполнение (Сообщение для выполнения задачи)
        {
            Connect.Open(); // Открываем базу данных 
            var s = Connect.Query<String>(@"select [Задача]+""|""+[Время выполнения] from Reminder"); // Присваем переменной массив данных выполненного запроса

            foreach (String i in s) // Проходим по всем значениям запроса 
            {
                string[] split = i.Split('|'); // Массив split разделяет строку на две части находя | 
                if (split[1] == localDateTime) // Проверяем Дата выполнения равна Дате текущей 
                {
                    MessageBox.Show("Выполните задачу: " + split[0], "Напоминание"); // Выводим Окно Напоминания
                }
            }
            Connect.Close(); // Закрываем базу данных 
        }

        public void Refresh() // Обновления DataGrid.
        {
            OleDbDataAdapter dataAdapter = new OleDbDataAdapter("Select ID,[Задача],[Время выполнения],[Выполнено] FROM Reminder ORder BY id", Connect); // Переменная заполняющая DataSet данными из БД.

            OleDbCommandBuilder commandBuilder = new OleDbCommandBuilder(dataAdapter); // Переменная commandBuilder позволяем автоматически сгенерировать нужные выражения
            DataSet dataSet = new DataSet(); // Переменная хранилища данных 

            dataAdapter.Fill(dataSet, "Reminder"); // DataSet для заполнения с записями и Строка, указывающая имя исходной таблицы.
            DateGrid.ItemsSource = dataSet.Tables["Reminder"].DefaultView; // Выводим все данные на DataGrid 
            DateGrid.Columns[0].Visibility = Visibility.Hidden; // Скрываем колонку id на DataGrid, она нужна только для вычисления
        }
    }
}
