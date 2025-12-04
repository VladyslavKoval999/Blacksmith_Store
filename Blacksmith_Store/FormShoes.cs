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
    public partial class FormShoes : Form
    {
        private const string ConnectionString = @"Data Source=D:\Все для навчання\4_Курс\Blacksmith_Store\Blacksmith_Store\bin\Debug\Blacksmith_StoreBD";
        private const string ImagesFolderPath = @"D:\Все для навчання\4_Курс\Blacksmith_Store\Blacksmith_Store\bin\Debug\PNG\Product";

        private ImageList imageListShoes;
        private string currentCategoryFilter = string.Empty;
        private bool _isNavigating = false;

        public FormShoes()
        {
            InitializeComponent();

            imageListShoes = new ImageList();
            imageListShoes.ImageSize = new Size(158, 242);
            lvShoes.LargeImageList = imageListShoes;
            lvShoes.SmallImageList = imageListShoes;

            this.Load += FormShoes_Load;

            this.lvShoes.DoubleClick -= this.lvShoes_DoubleClick;
            this.lvShoes.DoubleClick += this.lvShoes_DoubleClick;

            this.pbSearch.Click += new System.EventHandler(this.pbSearch_Click);
            this.btnReset.Click += new System.EventHandler(this.btnReset_Click);

            msMenu.BringToFront();
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

        private void FormShoes_Load(object sender, EventArgs e)
        {
            _isNavigating = false;
            LoadComboBoxData();
            LoadShoes(string.Empty);

            rbAll.Checked = true;
            rbAllScore.Checked = true;

            UpdateCartSummary();
        }

        private void LoadComboBoxData()
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                try
                {
                    connection.Open();

                    string subtypeQuery = @"
                        SELECT DISTINCT T.name  
                        FROM product_subtypes AS T
                        INNER JOIN products AS P ON T.subtype_id = P.subtype_id
                        WHERE P.product_type = 'Взуття'
                        ORDER BY T.name";

                    LoadDataToComboBox(connection, subtypeQuery, cbTypeShoes);
                    LoadDataToComboBox(connection, "SELECT name FROM brands ORDER BY name", cbBrand);
                    LoadDataToComboBox(connection, "SELECT name FROM colors ORDER BY name", cbColor);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка підключення до бази даних: {ex.Message}", "Помилка БД", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void LoadDataToComboBox(SqliteConnection connection, string query, ComboBox comboBox)
        {
            comboBox.Items.Clear();
            using (var command = new SqliteCommand(query, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    comboBox.Items.Add(reader["name"].ToString());
                }
            }
        }

        private void LoadShoes(string additionalFilter)
        {
            string sql = @"
                SELECT
                    P.product_id, P.name, P.base_price, P.season,
                    B.name AS brand_name,
                    T.name AS subtype_name,
                    P.images,
                    CTG.name AS category_name,
                    MIN(S.availability_status) AS availability_status_aggregated,
                    SUM(S.quantity) AS total_quantity
                FROM products AS P
                INNER JOIN brands AS B ON P.brand_id = B.brand_id
                INNER JOIN stock AS S ON P.product_id = S.product_id
                INNER JOIN categories AS CTG ON P.category_id = CTG.category_id
                LEFT JOIN product_subtypes AS T ON P.subtype_id = T.subtype_id
                WHERE P.product_type = 'Взуття'";

            if (!string.IsNullOrEmpty(currentCategoryFilter))
                sql += $" AND CTG.name = '{currentCategoryFilter.Replace("'", "''")}'";

            if (!string.IsNullOrEmpty(additionalFilter))
                sql += $" AND {additionalFilter}";

            sql += @"
                GROUP BY P.product_id, P.name, P.base_price, P.season, B.name, T.name, P.images, CTG.name
                ORDER BY P.name";

            LoadProductsToListView(sql);
        }

        private void UpdateCategoryButtonsBackgroundColor(Button selectedButton)
        {
            Color activeColor = Color.FromArgb(10, 55, 153);

            Color defaultColor = Color.FromArgb(51, 102, 215);

            btnShoesWoman.BackColor = defaultColor;
            btnShoesMan.BackColor = defaultColor;
            btnShoesChildren.BackColor = defaultColor;

            if (selectedButton != null)
            {
                selectedButton.BackColor = activeColor;
            }
        }

        private void btnSearchShoes_Click(object sender, EventArgs e)
        {
            List<string> filters = new List<string>();
            bool sizeOrColorFilterApplied = false;

            if (!string.IsNullOrWhiteSpace(tbSearch.Text))
                filters.Add($"(P.name LIKE '%{tbSearch.Text.Trim().Replace("'", "''")}%' OR P.description LIKE '%{tbSearch.Text.Trim().Replace("'", "''")}%')");

            if (cbTypeShoes.SelectedItem is string subtype)
                filters.Add($"T.name = '{subtype.Replace("'", "''")}'");

            if (rbSummer.Checked) filters.Add("P.season = 'Літо'");
            else if (rbAutumn.Checked) filters.Add("P.season = 'Осінь'");
            else if (rbWinter.Checked) filters.Add("P.season = 'Зима'");
            else if (rbSpring.Checked) filters.Add("P.season = 'Весна'");

            if (cbBrand.SelectedItem is string brand)
                filters.Add($"B.name = '{brand.Replace("'", "''")}'");

            bool sizeFilterExists = false;
            if (float.TryParse(tbSizeFrom.Text, out float sizeFrom))
            {
                sizeFilterExists = true;
                sizeOrColorFilterApplied = true;
                filters.Add($"Z.value >= {sizeFrom}");
            }
            if (float.TryParse(tbSizeTo.Text, out float sizeTo))
            {
                sizeFilterExists = true;
                sizeOrColorFilterApplied = true;
                filters.Add($"Z.value <= {sizeTo}");
            }

            bool colorFilterExists = false;
            if (cbColor.SelectedItem is string color)
            {
                colorFilterExists = true;
                sizeOrColorFilterApplied = true;
                filters.Add($"C.name = '{color.Replace("'", "''")}'");
            }

            if (decimal.TryParse(tbPriceFrom.Text, out decimal priceFrom))
                filters.Add($"P.base_price >= {priceFrom}");
            if (decimal.TryParse(tbPriceTo.Text, out decimal priceTo))
                filters.Add($"P.base_price <= {priceTo}");

            if (rbInScore.Checked)
                filters.Add("S.availability_status = 'В наявності'");
            else if (rbComingSoon.Checked)
                filters.Add("S.availability_status = 'Скоро буде'");

            string filterClause = string.Join(" AND ", filters);

            if (sizeOrColorFilterApplied)
            {
                string sql = $@"
                    SELECT
                        P.product_id, P.name, P.base_price, P.season,
                        B.name AS brand_name,
                        T.name AS subtype_name,
                        P.images,
                        CTG.name AS category_name,
                        MIN(S.availability_status) AS availability_status_aggregated,
                        SUM(S.quantity) AS total_quantity
                    FROM products AS P
                    INNER JOIN brands AS B ON P.brand_id = B.brand_id
                    INNER JOIN stock AS S ON P.product_id = S.product_id
                    {(colorFilterExists ? "INNER" : "LEFT")} JOIN colors AS C ON S.color_id = C.color_id
                    {(sizeFilterExists ? "INNER" : "LEFT")} JOIN sizes AS Z ON S.size_id = Z.size_id
                    INNER JOIN categories AS CTG ON P.category_id = CTG.category_id
                    LEFT JOIN product_subtypes AS T ON P.subtype_id = T.subtype_id
                    WHERE P.product_type = 'Взуття'
                    {(string.IsNullOrEmpty(currentCategoryFilter) ? "" : $" AND CTG.name = '{currentCategoryFilter.Replace("'", "''")}'")}
                    AND {filterClause}
                    GROUP BY P.product_id, P.name, P.base_price, P.season, B.name, T.name, P.images, CTG.name
                    ORDER BY P.name";

                LoadShoesExtended(sql);
            }
            else
            {
                LoadShoes(filterClause);
            }
        }

        private void LoadShoesExtended(string sqlQuery)
        {
            LoadProductsToListView(sqlQuery);
        }

        private void LoadProductsToListView(string sqlQuery)
        {
            lvShoes.Items.Clear();
            imageListShoes.Images.Clear();

            using (var connection = new SqliteConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    using (var command = new SqliteCommand(sqlQuery, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string fileName = reader["images"]?.ToString() ?? "";
                            string imageKey = "";
                            int productId = reader.GetInt32(reader.GetOrdinal("product_id"));

                            if (!string.IsNullOrEmpty(fileName))
                            {
                                string fullPath = Path.Combine(ImagesFolderPath, fileName);
                                if (File.Exists(fullPath))
                                {
                                    try
                                    {
                                        imageKey = fileName;
                                        if (!imageListShoes.Images.ContainsKey(imageKey))
                                        {
                                            byte[] imageBytes = File.ReadAllBytes(fullPath);
                                            using (MemoryStream ms = new MemoryStream(imageBytes))
                                            {
                                                using (Image img = Image.FromStream(ms))
                                                {
                                                    imageListShoes.Images.Add(imageKey, (Image)img.Clone());
                                                }
                                            }
                                        }
                                    }
                                    catch { imageKey = ""; }
                                }
                            }

                            var item = new ListViewItem(reader["name"].ToString()) { Tag = productId };
                            if (!string.IsNullOrEmpty(imageKey)) item.ImageKey = imageKey;

                            item.SubItems.Add(reader["subtype_name"]?.ToString() ?? "N/A");
                            item.SubItems.Add(reader["brand_name"].ToString());
                            item.SubItems.Add(reader["season"]?.ToString() ?? "N/A");
                            item.SubItems.Add("Усі");
                            item.SubItems.Add("Усі");
                            item.SubItems.Add(reader["base_price"].ToString());
                            item.SubItems.Add(reader["availability_status_aggregated"].ToString());
                            item.SubItems.Add(reader["total_quantity"].ToString());

                            lvShoes.Items.Add(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка завантаження даних: {ex.Message}", "Помилка БД", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            tbSearch.Text = string.Empty;
            tbSizeFrom.Text = string.Empty;
            tbSizeTo.Text = string.Empty;
            tbPriceFrom.Text = string.Empty;
            tbPriceTo.Text = string.Empty;

            cbTypeShoes.SelectedIndex = -1;
            cbBrand.SelectedIndex = -1;
            cbColor.SelectedIndex = -1;

            rbAll.Checked = true;
            rbAllScore.Checked = true;

            currentCategoryFilter = string.Empty;
            UpdateCategoryButtonsBackgroundColor(null);
            LoadShoes(string.Empty);
        }

        private void FormShoes_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }

        private void btnShoesWoman_Click(object sender, EventArgs e)
        {
            currentCategoryFilter = "Жінкам";
            UpdateCategoryButtonsBackgroundColor(btnShoesWoman);
            ClearFilterUI();
            LoadShoes(string.Empty);
        }

        private void btnShoesMan_Click(object sender, EventArgs e)
        {
            currentCategoryFilter = "Чоловікам";
            UpdateCategoryButtonsBackgroundColor(btnShoesMan);
            ClearFilterUI();
            LoadShoes(string.Empty);
        }

        private void btnShoesChildren_Click(object sender, EventArgs e)
        {
            currentCategoryFilter = "Дітям";
            UpdateCategoryButtonsBackgroundColor(btnShoesChildren);
            ClearFilterUI();
            LoadShoes(string.Empty);
        }

        private void ClearFilterUI()
        {
            tbSearch.Text = string.Empty;
            tbSizeFrom.Text = string.Empty;
            tbSizeTo.Text = string.Empty;
            tbPriceFrom.Text = string.Empty;
            tbPriceTo.Text = string.Empty;

            cbTypeShoes.SelectedIndex = -1;
            cbBrand.SelectedIndex = -1;
            cbColor.SelectedIndex = -1;

            rbAll.Checked = true;
            rbAllScore.Checked = true;
        }

        private void pbSearch_Click(object sender, EventArgs e)
        {
            btnSearchShoes_Click(sender, e);
        }

        private void lvShoes_DoubleClick(object sender, EventArgs e)
        {
            if (_isNavigating) return;

            if (lvShoes.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = lvShoes.SelectedItems[0];

                if (selectedItem.Tag is int productId)
                {
                    try
                    {
                        _isNavigating = true;
                        FormProduct formProduct = new FormProduct(productId);
                        formProduct.Show();
                        this.Hide();
                    }
                    catch (Exception ex)
                    {
                        _isNavigating = false;
                        MessageBox.Show($"Помилка при відкритті форми продукту: {ex.Message}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
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
            FormReport formReport = new FormReport();
            formReport.Show();
        }

        private void tsmiAddProduct_Click(object sender, EventArgs e)
        {
            this.Hide();
            FormAddProduct formAddProduct = new FormAddProduct();
            formAddProduct.Show();
        }
    }
}
