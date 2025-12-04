using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Blacksmith_Store
{
    public partial class FormAddProduct : Form
    {
        private const string BaseImagesPath = @"D:\Все для навчання\4_Курс\Blacksmith_Store\Blacksmith_Store\bin\Debug\PNG\";

        private string _selectedImageFilePath = string.Empty;
        private string _selectedImageFileName = string.Empty;

        private Dictionary<string, long> _categoryData = new Dictionary<string, long>();
        private Dictionary<string, long> _brandData = new Dictionary<string, long>();
        private Dictionary<string, long> _subtypeData = new Dictionary<string, long>();
        private Dictionary<string, long> _colorData = new Dictionary<string, long>();

        private static readonly string[] AccessoryKeywords =
        {
            "Сумки", "Рюкзаки", "Крос-боді", "Кепки", "Поясні сумки",
            "Сумки хобо", "Шопери", "Мессенджери", "Панами", "Ремені", "Гаманці"
        };

        private Image _defaultImage;

        public FormAddProduct()
        {
            InitializeComponent();
            msMenu.BringToFront();
            this.Load += FormAddProduct_Load;
        }

        public void UpdateCartSummary()
        {
            decimal totalAmount = CartManager.CartItems.Sum(item => item.TotalPrice);
            int totalQuantity = CartManager.CartItems.Sum(item => item.Quantity);

            if (lbNumber != null)
            {
                lbNumber.Text = totalQuantity.ToString() + " шт";
            }

            string formattedTotal = totalAmount.ToString("C2", CultureInfo.CurrentCulture);

            if (lbPrice != null)
            {
                lbPrice.Text = formattedTotal;
            }
        }

        private void FormAddProduct_Load(object sender, EventArgs e)
        {
            cbType.DropDownStyle = ComboBoxStyle.DropDown;
            cbBrand.DropDownStyle = ComboBoxStyle.DropDown;
            cbSubtype.DropDownStyle = ComboBoxStyle.DropDown;
            cbColor.DropDownStyle = ComboBoxStyle.DropDown;

            LoadComboBoxData();

            cbSubtype.SelectedIndexChanged += cbSubtype_Changed;
            cbSubtype.TextChanged += cbSubtype_Changed;
            
            cbType.SelectedIndexChanged += cbType_SelectedIndexChanged;

            cbSubtype_Changed(null, null);

            rbAll.Checked = true;

            if (pbProduct.Image != null)
            {
                _defaultImage = (Image)pbProduct.Image.Clone();
            }

            UpdateCartSummary();
        }

        private void cbSubtype_Changed(object sender, EventArgs e)
        {
            string subtypeText = cbSubtype.Text.Trim();

            bool isAccessory = AccessoryKeywords.Any(keyword => subtypeText.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);

            lbSize.Visible = !isAccessory;
            panelSize.Visible = !isAccessory;

            if (isAccessory)
            {
                foreach (RadioButton rb in panelSize.Controls.OfType<RadioButton>())
                {
                    rb.Checked = false;
                }
            }
        }

        private void LoadComboBoxData()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    LoadReferenceData(connection, "SELECT category_id, name FROM categories", cbType, _categoryData);
                    LoadReferenceData(connection, "SELECT brand_id, name FROM brands", cbBrand, _brandData);
                    LoadReferenceData(connection, "SELECT subtype_id, name FROM product_subtypes", cbSubtype, _subtypeData);
                    LoadReferenceData(connection, "SELECT color_id, name FROM colors", cbColor, _colorData);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження початкових даних: {ex.Message}", "Помилка БД", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadReferenceData(SqliteConnection connection, string sql, ComboBox comboBox, Dictionary<string, long> dataDict)
        {
            dataDict.Clear();
            comboBox.Items.Clear();

            using (var command = new SqliteCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    long id = reader.GetInt64(0);
                    string name = reader.GetString(1);
                    dataDict.Add(name, id);
                    comboBox.Items.Add(name);
                }
            }
        }

        private void cbType_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void cbSubtype_TextChanged(object sender, EventArgs e)
        {
            cbType_SelectedIndexChanged(sender, e);
        }

        private void btnPicture_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                string targetDirectory = Path.Combine(BaseImagesPath, "Product");

                if (!Directory.Exists(targetDirectory))
                {
                    try
                    {
                        Directory.CreateDirectory(targetDirectory);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Помилка створення директорії: {ex.Message}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                ofd.InitialDirectory = targetDirectory;
                ofd.Filter = "PNG Files (*.png)|*.png";
                ofd.Title = "Оберіть зображення товару у форматі .png";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string sourceFilePath = ofd.FileName;
                    _selectedImageFileName = Path.GetFileName(sourceFilePath);
                    string destinationPath = Path.Combine(targetDirectory, _selectedImageFileName);

                    if (Path.GetExtension(sourceFilePath).ToLower() != ".png")
                    {
                        MessageBox.Show("Будь ласка, оберіть файл у форматі .png.", "Неправильний формат", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    try
                    {
                        if (!Path.GetDirectoryName(sourceFilePath).Equals(targetDirectory, StringComparison.OrdinalIgnoreCase))
                        {
                            File.Copy(sourceFilePath, destinationPath, true);
                            _selectedImageFilePath = destinationPath;
                        }
                        else
                        {
                            _selectedImageFilePath = sourceFilePath;
                        }

                        if (pbProduct.Image != null) pbProduct.Image.Dispose();
                        using (var img = Image.FromFile(_selectedImageFilePath))
                        {
                            pbProduct.Image = new Bitmap(img);
                        }
                        pbProduct.SizeMode = PictureBoxSizeMode.Zoom;

                        if (tbName != null && string.IsNullOrWhiteSpace(tbName.Text))
                        {
                            tbName.Text = Path.GetFileNameWithoutExtension(_selectedImageFileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Помилка завантаження/копіювання зображення: {ex.Message}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            string categoryName = cbType.Text.Trim();
            string brandName = cbBrand.Text.Trim();
            string subtypeName = cbSubtype.Text.Trim();
            string colorName = cbColor.Text.Trim();
            string productName = tbName.Text.Trim();
            string productDescription = tbDescription.Text.Trim();

            if (!ValidateFormData(out string selectedSizeValue, out long selectedSizeId))
            {
                return;
            }

            if (!decimal.TryParse(tbPriceFrom.Text, out decimal basePrice) || basePrice <= 0)
            {
                MessageBox.Show("Ціна повинна бути коректним додатним числом.", "Помилка валідації", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            long categoryId = DatabaseHelper.GetOrCreateId("categories", categoryName);
            long brandId = DatabaseHelper.GetOrCreateId("brands", brandName);
            long subtypeId = DatabaseHelper.GetOrCreateId("product_subtypes", subtypeName);
            long colorId = DatabaseHelper.GetOrCreateId("colors", colorName);

            if (categoryId <= 0 || brandId <= 0)
            {
                MessageBox.Show("Не вдалося створити/отримати ID для Категорії або Бренду. Перевірте з'єднання з БД.", "Помилка БД", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string productType = panelSize.Visible ? "Взуття" : "Аксесуар";

            string productSeason = GetSelectedSeason();
            string availabilityStatus = GetSelectedAvailability();

            int existingProductId = CheckProductExistence(productName, productType, productSeason, categoryId, brandId, subtypeId);

            if (existingProductId > 0)
            {
                string existingDescription = GetExistingProductDescription(existingProductId);
                string existingImageName = GetExistingProductImageName(existingProductId);

                if (!string.Equals(existingDescription, productDescription, StringComparison.Ordinal) ||
                    !string.Equals(existingImageName, _selectedImageFileName, StringComparison.OrdinalIgnoreCase))
                {
                    string diffMessage = CompareDescriptions(existingDescription, productDescription);

                    if (!string.Equals(existingImageName, _selectedImageFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        diffMessage += $"\n**Існуюче зображення:** {existingImageName}\n";
                        diffMessage += $"**Нове зображення:** {_selectedImageFileName}\n";
                    }

                    DialogResult result = MessageBox.Show(
                        $"Даний товар вже існує, але має відмінність в описі/зображенні:\n\n{diffMessage}\n\nБажаєте створити **НОВИЙ** продукт чи продовжити (тоді додасться лише нова варіація розмір-колір) **ІСНУЮЧОГО**?",
                        "Конфлікт даних про товар",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Warning
                    );

                    if (result == DialogResult.Cancel)
                    {
                        return;
                    }
                    if (result == DialogResult.Yes)
                    {
                        existingProductId = 0;
                    }
                }
            }

            SaveOrUpdateProduct(existingProductId, productName, productDescription, basePrice, productType, productSeason, categoryId, brandId, subtypeId, _selectedImageFileName, colorId, selectedSizeId, availabilityStatus);

            MessageBox.Show("Товар успішно додано/оновлено!", "Успіх", MessageBoxButtons.OK, MessageBoxIcon.Information);
            btnReset_Click(sender, e);

            FormMain formMain = new FormMain();
            formMain.Show();
            this.Hide();
        }

        private bool ValidateFormData(out string selectedSizeValue, out long selectedSizeId)
        {
            selectedSizeValue = string.Empty;
            selectedSizeId = 0;

            string productName = tbName.Text.Trim();
            string categoryName = cbType.Text.Trim();
            string brandName = cbBrand.Text.Trim();
            string subtypeName = cbSubtype.Text.Trim();
            string colorName = cbColor.Text.Trim();
            string description = tbDescription.Text.Trim();
            string priceText = tbPriceFrom.Text;

            if (string.IsNullOrWhiteSpace(productName))
            {
                MessageBox.Show("Поле 'Назва' є обов'язковим.", "Помилка валідації", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(categoryName))
            {
                MessageBox.Show("Поле 'Кому' є обов'язковим.", "Помилка валідації", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(brandName))
            {
                MessageBox.Show("Поле 'Бренд' є обов'язковим.", "Помилка валідації", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(subtypeName))
            {
                MessageBox.Show("Поле 'Тип' є обов'язковим.", "Помилка валідації", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                MessageBox.Show("Поле 'Опис' є обов'язковим.", "Помилка валідації", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(priceText))
            {
                MessageBox.Show("Поле 'Ціна' є обов'язковим.", "Помилка валідації", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (!decimal.TryParse(priceText, out decimal basePrice) || basePrice <= 0)
            {
                MessageBox.Show("Ціна повинна бути коректним додатним числом.", "Помилка валідації", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(GetSelectedAvailability()))
            {
                MessageBox.Show("Оберіть статус 'Наявність'.", "Помилка валідації", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(GetSelectedSeason()))
            {
                MessageBox.Show("Оберіть 'Сезон'.", "Помилка валідації", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(_selectedImageFileName))
            {
                MessageBox.Show("Будь ласка, оберіть зображення товару.", "Помилка валідації", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (panelSize.Visible && string.IsNullOrWhiteSpace(colorName))
            {
                MessageBox.Show("Для Взуття обов'язково оберіть або введіть Колір.", "Помилка валідації", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            else if (!panelSize.Visible && string.IsNullOrWhiteSpace(colorName)) { }

            if (panelSize.Visible)
            {
                RadioButton selectedSizeRb = panelSize.Controls.OfType<RadioButton>().FirstOrDefault(rb => rb.Checked);
                if (selectedSizeRb == null)
                {
                    MessageBox.Show("Для **Взуття** обов'язково оберіть розмір.", "Помилка валідації", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                selectedSizeValue = selectedSizeRb.Text;

                if (!ValidateSizeSelection(selectedSizeValue))
                {
                    return false;
                }

                try
                {
                    using (var connection = DatabaseHelper.GetConnection())
                    {
                        connection.Open();
                        if (!double.TryParse(selectedSizeValue, out double sizeValueDouble))
                        {
                            MessageBox.Show("Некоректний формат розміру. Очікується число.", "Помилка валідації", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }

                        var cmd = new SqliteCommand("SELECT size_id FROM sizes WHERE value = @Value", connection);
                        cmd.Parameters.AddWithValue("@Value", sizeValueDouble);
                        object result = cmd.ExecuteScalar();

                        if (result == null || result == DBNull.Value)
                        {
                            var insertCmd = new SqliteCommand("INSERT INTO sizes (value) VALUES (@Value); SELECT last_insert_rowid();", connection);
                            insertCmd.Parameters.AddWithValue("@Value", sizeValueDouble);
                            selectedSizeId = (long)insertCmd.ExecuteScalar();
                        }
                        else
                        {
                            selectedSizeId = (long)result;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка роботи з БД при валідації розміру: {ex.Message}", "Помилка БД", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            return true;
        }

        private bool ValidateSizeSelection(string sizeValue)
        {
            if (!double.TryParse(sizeValue, out double size))
            {
                MessageBox.Show("Некоректний формат розміру. Очікується число.", "Помилка валідації", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            bool isKids = size >= 21 && size <= 41;
            bool isWomen = size >= 36 && size <= 45;
            bool isMen = size >= 41 && size <= 50;

            string category = cbType.Text.Trim();

            if (category.Contains("Чоловік") && !isMen && !(isMen && isWomen && isKids))
            {
                MessageBox.Show($"Обрано чоловічу категорію. Розмір **{size}** має бути в діапазоні 41-50.", "Помилка валідації", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (category.Contains("Жінк") && !isWomen && !(isMen && isWomen && isKids))
            {
                MessageBox.Show($"Обрано жіночу категорію. Розмір **{size}** має бути в діапазоні 36-45.", "Помилка валідації", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (category.Contains("Дит") && !isKids && !(isMen && isWomen && isKids))
            {
                MessageBox.Show($"Обрано дитячу категорію. Розмір **{size}** має бути в діапазоні 21-41.", "Помилка валідації", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!isKids && !isWomen && !isMen)
            {
                MessageBox.Show($"Розмір {size} не відповідає жодному зі стандартних діапазонів (21-41, 36-45, 41-50).", "Помилка валідації розміру", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            tbName.Text = string.Empty;
            tbDescription.Text = string.Empty;
            tbPriceFrom.Text = string.Empty;

            cbType.SelectedIndex = -1;
            cbBrand.SelectedIndex = -1;
            cbSubtype.SelectedIndex = -1;
            cbColor.SelectedIndex = -1;
            cbType.Text = string.Empty;
            cbBrand.Text = string.Empty;
            cbSubtype.Text = string.Empty;
            cbColor.Text = string.Empty;

            if (pbProduct.Image != null && pbProduct.Image != _defaultImage)
            {
                pbProduct.Image.Dispose();
            }

            if (_defaultImage != null)
            {
                pbProduct.Image = (Image)_defaultImage.Clone();
                pbProduct.SizeMode = PictureBoxSizeMode.Zoom;
            }
            else
            {
                string fallbackPath = Path.Combine(BaseImagesPath, "images1.png");
                if (File.Exists(fallbackPath))
                {
                    pbProduct.Image = new Bitmap(Image.FromFile(fallbackPath));
                    pbProduct.SizeMode = PictureBoxSizeMode.Zoom;
                }
                else
                {
                    pbProduct.Image = null;
                }
            }

            _selectedImageFilePath = string.Empty;
            _selectedImageFileName = string.Empty;

            foreach (RadioButton rb in panelSeason.Controls.OfType<RadioButton>()) rb.Checked = (rb.Name == "rbAll");
            foreach (RadioButton rb in panelAvailability.Controls.OfType<RadioButton>()) rb.Checked = (rb.Name == "rbAllScore");
            foreach (RadioButton rb in panelSize.Controls.OfType<RadioButton>()) rb.Checked = false;

            cbSubtype_Changed(sender, e);
        }

        private string CompareDescriptions(string existing, string current)
        {
            string diff = string.Empty;
            int maxLen = 50;

            if (!string.Equals(existing, current, StringComparison.Ordinal))
            {
                diff += $"Існуючий опис (початок): {existing.Substring(0, Math.Min(existing.Length, maxLen))}...\n";
                diff += $"Новий опис (початок): {current.Substring(0, Math.Min(current.Length, maxLen))}...\n";
            }
            else
            {
                diff += $"Описи ідентичні.\n";
            }
            return diff;
        }

        private string GetSelectedSeason()
        {
            RadioButton selectedRb = panelSeason.Controls.OfType<RadioButton>().FirstOrDefault(rb => rb.Checked);

            string season = selectedRb?.Text;
            if (string.IsNullOrWhiteSpace(season) || selectedRb.Name == "rbAll")
            {
                return "Усі";
            }
            return season;
        }

        private string GetSelectedAvailability()
        {
            if (rbInScore.Checked) return "В наявності";
            if (rbComingSoon.Checked) return "Скоро буде";
            return "В наявності";
        }

        private int CheckProductExistence(string name, string type, string season, long categoryId, long brandId, long subtypeId)
        {
            string sql = @"
                SELECT product_id 
                FROM products 
                WHERE name = @Name 
                  AND product_type = @Type 
                  AND season = @Season 
                  AND category_id = @CategoryId 
                  AND brand_id = @BrandId 
                  AND subtype_id = @SubtypeId";

            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                using (var command = new SqliteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Name", name);
                    command.Parameters.AddWithValue("@Type", type);
                    command.Parameters.AddWithValue("@Season", season);
                    command.Parameters.AddWithValue("@CategoryId", categoryId);
                    command.Parameters.AddWithValue("@BrandId", brandId);
                    command.Parameters.AddWithValue("@SubtypeId", subtypeId <= 0 ? (object)DBNull.Value : subtypeId);

                    object result = command.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
        }

        private string GetExistingProductDescription(int productId)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                var cmd = new SqliteCommand("SELECT description FROM products WHERE product_id = @ProductId", connection);
                cmd.Parameters.AddWithValue("@ProductId", productId);
                return cmd.ExecuteScalar()?.ToString() ?? string.Empty;
            }
        }

        private string GetExistingProductImageName(int productId)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                var cmd = new SqliteCommand("SELECT images FROM products WHERE product_id = @ProductId", connection);
                cmd.Parameters.AddWithValue("@ProductId", productId);
                return cmd.ExecuteScalar()?.ToString() ?? string.Empty;
            }
        }

        private void SaveOrUpdateProduct(int existingProductId, string productName, string productDescription, decimal basePrice, string productType, string productSeason, long categoryId, long brandId, long subtypeId, string imageFileName, long colorId, long sizeId, string availabilityStatus)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        long productId = existingProductId;

                        if (productId == 0)
                        {
                            string insertProductSql = @"
                                INSERT INTO products (name, description, base_price, product_type, season, category_id, brand_id, subtype_id, images) 
                                VALUES (@Name, @Description, @BasePrice, @Type, @Season, @CategoryId, @BrandId, @SubtypeId, @Images);
                                SELECT last_insert_rowid();";

                            using (var cmd = new SqliteCommand(insertProductSql, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@Name", productName);
                                cmd.Parameters.AddWithValue("@Description", productDescription);
                                cmd.Parameters.AddWithValue("@BasePrice", basePrice);
                                cmd.Parameters.AddWithValue("@Type", productType);
                                cmd.Parameters.AddWithValue("@Season", productSeason);
                                cmd.Parameters.AddWithValue("@CategoryId", categoryId);
                                cmd.Parameters.AddWithValue("@BrandId", brandId);
                                cmd.Parameters.AddWithValue("@SubtypeId", subtypeId <= 0 ? (object)DBNull.Value : subtypeId);
                                cmd.Parameters.AddWithValue("@Images", imageFileName);

                                productId = (long)cmd.ExecuteScalar();
                            }
                        }
                        else
                        {
                            string updateProductSql = @"
                                UPDATE products 
                                SET description = @Description, images = @Images, base_price = @BasePrice 
                                WHERE product_id = @ProductId";

                            using (var cmd = new SqliteCommand(updateProductSql, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@Description", productDescription);
                                cmd.Parameters.AddWithValue("@Images", imageFileName);
                                cmd.Parameters.AddWithValue("@BasePrice", basePrice);
                                cmd.Parameters.AddWithValue("@ProductId", productId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        object sizeParam = sizeId <= 0 ? (object)DBNull.Value : sizeId;
                        object colorParam = colorId <= 0 ? (object)DBNull.Value : colorId;
                        object subtypeParam = subtypeId <= 0 ? (object)DBNull.Value : subtypeId;

                        string stockSelectSql = @"
                            SELECT stock_id, quantity 
                            FROM stock 
                            WHERE product_id = @ProductId 
                              AND ((color_id IS NULL AND @ColorId IS NULL) OR (color_id = @ColorId)) 
                              AND ((size_id IS NULL AND @SizeId IS NULL) OR (size_id = @SizeId))";

                        long stockId = 0;
                        int currentQuantity = 0;

                        using (var cmd = new SqliteCommand(stockSelectSql, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@ProductId", productId);
                            cmd.Parameters.AddWithValue("@ColorId", colorParam);
                            cmd.Parameters.AddWithValue("@SizeId", sizeParam);

                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    stockId = reader.GetInt64(0);
                                    currentQuantity = reader.GetInt32(1);
                                }
                            }
                        }

                        int newQuantity = currentQuantity + 1;

                        if (stockId > 0)
                        {
                            string updateStockSql = @"
                                UPDATE stock 
                                SET quantity = @Quantity, availability_status = @AvailabilityStatus
                                WHERE stock_id = @StockId";

                            using (var cmd = new SqliteCommand(updateStockSql, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@Quantity", newQuantity);
                                cmd.Parameters.AddWithValue("@AvailabilityStatus", availabilityStatus);
                                cmd.Parameters.AddWithValue("@StockId", stockId);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            string insertStockSql = @"
                                INSERT INTO stock (product_id, category_id, brand_id, color_id, size_id, subtype_id, quantity, availability_status) 
                                VALUES (@ProductId, @CategoryId, @BrandId, @ColorId, @SizeId, @SubtypeId, @Quantity, @AvailabilityStatus)";

                            using (var cmd = new SqliteCommand(insertStockSql, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@ProductId", productId);
                                cmd.Parameters.AddWithValue("@CategoryId", categoryId);
                                cmd.Parameters.AddWithValue("@BrandId", brandId);
                                cmd.Parameters.AddWithValue("@ColorId", colorParam);
                                cmd.Parameters.AddWithValue("@SizeId", sizeParam);
                                cmd.Parameters.AddWithValue("@SubtypeId", subtypeParam);
                                cmd.Parameters.AddWithValue("@Quantity", 1);
                                cmd.Parameters.AddWithValue("@AvailabilityStatus", availabilityStatus);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show($"Помилка при додаванні товару до БД (Транзакція відкочена): {ex.Message}", "Критична Помилка БД", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void FormAddProduct_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }

        private void pbCart_Click(object sender, EventArgs e)
        {
            FormCart formCart = new FormCart();
            formCart.Show();
            this.Hide();
        }

        private void pbLogo_Click(object sender, EventArgs e)
        {
            FormMain formMain = new FormMain();
            formMain.Show();
            this.Hide();
        }

        private void pbMenu_Click(object sender, EventArgs e)
        {
            if (msMenu.Visible == false) msMenu.Visible = true;
            else msMenu.Visible = false;
        }

        private void tsmiMain_Click(object sender, EventArgs e)
        {
            this.Hide();
            FormMain formMain = new FormMain();
            formMain.Show();
        }

        private void tsmiShoes_Click(object sender, EventArgs e)
        {
            this.Hide();
            FormShoes formShoes = new FormShoes();
            formShoes.Show();
        }

        private void tsmiAccessories_Click(object sender, EventArgs e)
        {
            this.Hide();
            FormAccessories formAccessories = new FormAccessories();
            formAccessories.Show();
        }

        private void tsmiNovetly_Click(object sender, EventArgs e)
        {
            this.Hide();
            FormNovetly formNovetly = new FormNovetly();
            formNovetly.Show();
        }

        private void tsmiTopSellers_Click(object sender, EventArgs e)
        {
            this.Hide();
            FormTopSellers formTopSellers = new FormTopSellers();
            formTopSellers.Show();
        }

        private void tsmiSale_Click(object sender, EventArgs e)
        {
            this.Hide();
            FormSale formSale = new FormSale();
            formSale.Show();
        }

        private void tsmiCart_Click(object sender, EventArgs e)
        {
            this.Hide();
            FormCart formCart = new FormCart();
            formCart.Show();
        }

        private void tsmiReport_Click(object sender, EventArgs e)
        {
            this.Hide();
            FormAddProduct formAddProduct = new FormAddProduct();
            formAddProduct.Show();
        }
    }
}
