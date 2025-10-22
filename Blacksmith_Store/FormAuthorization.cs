using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SQLitePCL;

namespace Blacksmith_Store
{
    public partial class FormAuthorization : Form
    {
        public FormAuthorization()
        {
            raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlite3());
            Batteries.Init();
            InitializeComponent();
            tbPassword.PasswordChar = '•';

            LoadEyeImages();
        }

        private const string DbFileName = @"D:\Все для навчання\4_Курс\Blacksmith_Store\Blacksmith_Store\bin\Debug\Blacksmith_StoreBD";

        private bool isPasswordVisible = false;
        private Image eyeOpenImage;
        private Image eyeClosedImage;

        private void LoadEyeImages()
        {
            string currentPath = @"D:\Все для навчання\4_Курс\Blacksmith_Store\Blacksmith_Store\bin\Debug\PNG\Authorization";

            if (!Directory.Exists(currentPath))
            {
                MessageBox.Show("Директорія 'PNG/Authorization' не знайдена!", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                string eyeOpenPath = Path.Combine(currentPath, "eye_open.png");
                string eyeClosedPath = Path.Combine(currentPath, "eye_closed.png");

                if (!File.Exists(eyeOpenPath))
                {
                    MessageBox.Show($"Зображення 'eye_open.png' не знайдено: {eyeOpenPath}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!File.Exists(eyeClosedPath))
                {
                    MessageBox.Show($"Зображення 'eye_closed.png' не знайдено: {eyeClosedPath}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                eyeOpenImage = Image.FromFile(eyeOpenPath);
                eyeClosedImage = Image.FromFile(eyeClosedPath);
                pbView.Image = eyeClosedImage;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при завантаженні зображень: {ex.Message}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void pbView_Click(object sender, EventArgs e)
        {
            isPasswordVisible = !isPasswordVisible;

            if (isPasswordVisible)
            {
                tbPassword.PasswordChar = '\0';
                pbView.Image = eyeOpenImage;
            }
            else
            {
                tbPassword.PasswordChar = '•';
                pbView.Image = eyeClosedImage;
            }
        }

        private void FormAuthorization_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (!IsInputValid()) return;

            string login = tbLogin.Text.Trim();
            string password = tbPassword.Text.Trim();

            if (UserExists(login, password))
            {
                MessageBox.Show("Авторизація успішна!", "Успіх", MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.Hide();
                FormMain formMain = new FormMain();
                formMain.Show();
            }
            else
            {
                MessageBox.Show("Невірний логін або пароль.", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool IsInputValid()
        {
            if (string.IsNullOrWhiteSpace(tbLogin.Text))
            {
                MessageBox.Show("Будь ласка, введіть логін.", "Увага", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(tbPassword.Text))
            {
                MessageBox.Show("Будь ласка, введіть пароль.", "Увага", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private bool UserExists(string login, string password)
        {
            string query = "SELECT COUNT(*) FROM users WHERE username = @Login AND password_hash = @Password";

            string connectionString = $"Data Source={DbFileName}";

            try
            {
                using (var connection = new SqliteConnection(connectionString))
                {
                    connection.Open();

                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Login", login);
                        command.Parameters.AddWithValue("@Password", password);

                        long count = (long)command.ExecuteScalar();

                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка підключення/запиту до БД: {ex.Message}", "Помилка DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }
}
