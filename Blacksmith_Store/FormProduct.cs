using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;
using System.IO;

namespace Blacksmith_Store
{
    public partial class FormProduct : Form
    {
        private const string ConnectionString = @"Data Source=D:\Все для навчання\4_Курс\Blacksmith_Store\Blacksmith_Store\bin\Debug\Blacksmith_StoreBD";
        private const string ImagesFolderPath = @"D:\Все для навчання\4_Курс\Blacksmith_Store\Blacksmith_Store\bin\Debug\PNG\Product";

        private int _productId;
        private string _productType;
        private string _productName;
        private string _imageFileName;
        private string _productColor;
        private decimal _basePrice;
        private string _productSubtypeName;

        private DataTable _productDetailsTable;

        public FormProduct(int productId)
        {
            InitializeComponent();
            _productId = productId;
            this.Text = $"Деталі товару (ID: {productId})";
            this.Load += FormProduct_Load;
            _productDetailsTable = new DataTable();

            lbDescription.AutoSize = false;
            int formWidth = 650;
            int lbDescriptionX = 244;
            int padding = 20;
            int maxWidth = formWidth - lbDescriptionX - padding;

            if (maxWidth > 100)
            {
                lbDescription.MaximumSize = new Size(maxWidth, 800);
                lbDescription.Size = new Size(maxWidth, 330);
            }
        }
                
        private void FormProduct_Load(object sender, EventArgs e)
        {
            LoadProductDetails();
        }

        private void LoadProductDetails()
        {
            string sql = @"
                SELECT 
                    P.name AS product_name,
                    P.description,
                    P.product_type,
                    P.base_price,
                    P.images,
                    T.name AS subtype_name,
                    P.season AS product_season,
                    B.name AS brand_name,
                    C.name AS color_name,
                    S.quantity AS stock_quantity,
                    S.availability_status AS availability,
                    SZ.value AS size_value
                FROM products AS P
                INNER JOIN brands AS B ON P.brand_id = B.brand_id
                LEFT JOIN product_subtypes AS T ON P.subtype_id = T.subtype_id
                
                LEFT JOIN stock AS S ON P.product_id = S.product_id
                LEFT JOIN colors AS C ON S.color_id = C.color_id
                LEFT JOIN sizes AS SZ ON S.size_id = SZ.size_id
                
                WHERE P.product_id = @ProductId;
            ";

            try
            {
                using (var connection = new SqliteConnection(ConnectionString))
                {
                    connection.Open();
                    using (var command = new SqliteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@ProductId", _productId);

                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                MessageBox.Show("Товар не знайдено.", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }

                            _productDetailsTable = new DataTable();
                            _productDetailsTable.Load(reader);

                            DataRow firstRow = _productDetailsTable.Rows[0];

                            _productName = firstRow["product_name"].ToString();
                            _productType = firstRow["product_type"].ToString();
                            _productSubtypeName = firstRow["subtype_name"] is DBNull ? string.Empty : firstRow["subtype_name"].ToString();
                            _basePrice = Convert.ToDecimal(firstRow["base_price"]);
                            _imageFileName = firstRow["images"] is DBNull ? string.Empty : firstRow["images"].ToString();
                            lbDescription.Text = firstRow["description"] is DBNull ? "Опис відсутній." : firstRow["description"].ToString();

                            _productColor = firstRow["color_name"] is DBNull ? "Не вказано" : firstRow["color_name"].ToString();

                            LoadProductImage();

                            SetupControlsByProductType();

                            if (_productType == "Взуття")
                            {
                                LoadSizesToComboBox();
                            }

                            UpdateProductLabels();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження деталей товару: {ex.Message}", "Помилка БД", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateProductLabels()
        {
            if (_productDetailsTable.Rows.Count == 0) return;

            DataRow firstRow = _productDetailsTable.Rows[0];

            decimal price = Convert.ToDecimal(firstRow["base_price"]);
            lbPrice.Text = $"{price:C2}";

            string subtype = _productSubtypeName;
            lbType.Text = $"{subtype}";

            if (_productType != "Взуття")
            {
                lbColor.Text = $"{_productColor}";

                string status = firstRow["availability"] is DBNull ? "" : firstRow["availability"].ToString();
                int qty = firstRow["stock_quantity"] is DBNull ? 0 : Convert.ToInt32(firstRow["stock_quantity"]);

                UpdateStatusLabel(status, qty);
            }

            btnAddCart.Enabled = true;
        }

        private void UpdateStatusLabel(string status, int quantity)
        {
            if (status.ToLower().Contains("скоро"))
            {
                lbScore.Text = "Скоро буде";
                lbScore.ForeColor = Color.FromArgb(51, 102, 215);
            }
            else if (quantity > 0)
            {
                lbScore.Text = "В наявності";
                lbScore.ForeColor = Color.FromArgb(51, 102, 215);
            }
            else
            {
                lbScore.Text = "Немає в наявності";
                lbScore.ForeColor = Color.Red;
            }
        }

        private void LoadProductImage()
        {
            if (!string.IsNullOrEmpty(_imageFileName))
            {
                string fullPath = Path.Combine(ImagesFolderPath, _imageFileName);
                if (File.Exists(fullPath))
                {
                    try
                    {
                        if (pbProduct.Image != null)
                        {
                            pbProduct.Image.Dispose();
                        }

                        pbProduct.Image = Image.FromFile(fullPath);
                        pbProduct.SizeMode = PictureBoxSizeMode.Zoom;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Помилка завантаження зображення: {ex.Message}", "Помилка файлів", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        pbProduct.Image = null;
                    }
                }
                else
                {
                    pbProduct.Image = null;
                }
            }
        }

        private void SetupControlsByProductType()
        {
            if (_productType == "Взуття")
            {
                cbSize.Visible = true;
                lbSize.Visible = true;
            }
            else
            {
                cbSize.Visible = false;
                lbSize.Visible = false;

                if (_productDetailsTable.Rows.Count > 0)
                {
                    _productColor = _productDetailsTable.Rows[0]["color_name"].ToString();
                }
            }
        }

        private void LoadSizesToComboBox()
        {
            cbSize.Items.Clear();

            var sizes = _productDetailsTable.AsEnumerable()
            .Where(row =>
            {
                if (row["size_value"] == DBNull.Value) return false;
                
                try
                {
                    int quantity = Convert.ToInt32(row["stock_quantity"]);
                    string status = row["availability"] is DBNull ? "" : row["availability"].ToString();
                    return quantity > 0 || status.ToLower().Contains("скоро");
                }
                catch
                {
                    return false;
                }
            }).Select(row => row["size_value"].ToString()).Distinct().OrderBy(size => { if (float.TryParse(size, out float fSize)) return fSize; return float.MaxValue; }).ToList();

            foreach (var size in sizes)
            {
                cbSize.Items.Add(size);
            }

            if (cbSize.Items.Count > 0)
            {
                cbSize.SelectedIndex = 0;
                cbSize.SelectedIndexChanged -= cbSize_SelectedIndexChanged;
                cbSize.SelectedIndexChanged += cbSize_SelectedIndexChanged;

                cbSize_SelectedIndexChanged(this, EventArgs.Empty);
            }
        }

        private void cbSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_productType == "Взуття" && cbSize.SelectedItem != null)
            {
                string selectedSizeString = cbSize.SelectedItem.ToString();

                DataRow sizeRow = _productDetailsTable.AsEnumerable().FirstOrDefault(row => { if (row["size_value"] == DBNull.Value) return false; return string.Equals(row["size_value"].ToString(), selectedSizeString, StringComparison.Ordinal);});

                if (sizeRow != null)
                {
                    _productColor = sizeRow["color_name"].ToString();
                    lbColor.Text = $"{_productColor}";

                    string status = sizeRow["availability"] is DBNull ? "" : sizeRow["availability"].ToString();
                    int qty = sizeRow["stock_quantity"] is DBNull ? 0 : Convert.ToInt32(sizeRow["stock_quantity"]);

                    UpdateStatusLabel(status, qty);
                }
                else
                {
                    _productColor = "Не вказано";
                    lbColor.Text = $"Колір: Не вказано";
                    lbScore.Text = "";
                }
            }
        }

        private void FormProduct_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }

        private void btnContinueShopping_Click(object sender, EventArgs e)
        {
            this.Hide();

            FormMain formMain = new FormMain();
            formMain.Show();
        }

        private void pbLogo_Click(object sender, EventArgs e)
        {
            this.Hide();

            FormMain formMain = new FormMain();
            formMain.Show();
        }

        private void btnAddCart_Click(object sender, EventArgs e)
        {
            float? selectedSize = null;
            string selectedColor = null;

            if (_productType == "Взуття")
            {
                if (cbSize.SelectedItem == null)
                {
                    MessageBox.Show("Будь ласка, оберіть розмір.", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string selectedSizeString = cbSize.SelectedItem.ToString();

                if (!float.TryParse(selectedSizeString, out float sizeValue))
                {
                    MessageBox.Show("Не вдалося визначити розмір.", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                selectedSize = sizeValue;

                DataRow sizeRow = _productDetailsTable.AsEnumerable().FirstOrDefault(row => { if (row["size_value"] == DBNull.Value) return false; return string.Equals(row["size_value"].ToString(), selectedSizeString, StringComparison.Ordinal);});

                if (sizeRow == null)
                {
                    MessageBox.Show($"Інформацію про розмір {selectedSizeString} не знайдено.", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string status = sizeRow["availability"] is DBNull ? "" : sizeRow["availability"].ToString();
                int qty = sizeRow["stock_quantity"] is DBNull ? 0 : Convert.ToInt32(sizeRow["stock_quantity"]);

                if (status.ToLower().Contains("скоро"))
                {
                    MessageBox.Show("Цей розмір очікується незабаром і наразі недоступний для покупки.", "Скоро у продажу", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (qty <= 0)
                {
                    MessageBox.Show("На жаль, цей розмір наразі відсутній на складі.", "Немає в наявності", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                selectedColor = sizeRow["color_name"].ToString();
            }
            else
            {
                DataRow productRow = _productDetailsTable.Rows.Count > 0 ? _productDetailsTable.Rows[0] : null;

                if (productRow == null)
                {
                    MessageBox.Show("Дані про товар відсутні.", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string status = productRow["availability"] is DBNull ? "" : productRow["availability"].ToString();
                int qty = productRow["stock_quantity"] is DBNull ? 0 : Convert.ToInt32(productRow["stock_quantity"]);

                if (status.ToLower().Contains("скоро"))
                {
                    MessageBox.Show("Цей товар очікується незабаром і наразі недоступний для покупки.", "Скоро у продажу", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (qty <= 0)
                {
                    MessageBox.Show("На жаль, цей товар наразі відсутній на складі.", "Немає в наявності", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                selectedColor = productRow["color_name"].ToString();
            }

            if (string.IsNullOrEmpty(selectedColor))
            {
                MessageBox.Show("Не вдалося визначити колір товару. Перевірте дані.", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            CartManager.AddItem(
                productId: _productId,
                name: _productName,
                type: _productType,
                subtypeName: _productSubtypeName,
                imageFileName: _imageFileName,
                price: _basePrice,
                size: selectedSize,
                color: selectedColor,
                quantity: 1
            );

            string message = $"{_productName}";
            if (selectedSize.HasValue)
            {
                message += $" (Розмір: {selectedSize})";
            }
            message += $" (Колір: {selectedColor}) додано до кошика!";

            MessageBox.Show(message, "Успіх", MessageBoxButtons.OK, MessageBoxIcon.Information);

            this.Hide();

            FormCart formCart = new FormCart();
            formCart.Show();
        }
    }
}
