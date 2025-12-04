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
    public partial class FormChildren : Form
    {
        private const string ConnectionString = @"Data Source=D:\Все для навчання\4_Курс\Blacksmith_Store\Blacksmith_Store\bin\Debug\Blacksmith_StoreBD";
        private const string ImagesFolderPath = @"D:\Все для навчання\4_Курс\Blacksmith_Store\Blacksmith_Store\bin\Debug\PNG\Product";

        private ImageList imageListChildren;
        private bool _isNavigating = false;

        public FormChildren()
        {
            InitializeComponent();

            imageListChildren = new ImageList();
            imageListChildren.ImageSize = new Size(158, 242);
            lvChildren.LargeImageList = imageListChildren;
            lvChildren.SmallImageList = imageListChildren;

            this.Load += FormChildren_Load;

            this.lvChildren.DoubleClick -= this.lvChildren_DoubleClick;
            this.lvChildren.DoubleClick += this.lvChildren_DoubleClick;

            this.pbSearch.Click += new System.EventHandler(this.pbSearch_Click);

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

        private void FormChildren_Load(object sender, EventArgs e)
        {
            _isNavigating = false;
            LoadComboBoxData();
            LoadChildrenProducts(string.Empty);

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
                    LoadDataToComboBox(connection, "SELECT name FROM product_subtypes ORDER BY name", cbTypeChildren);
                    LoadDataToComboBox(connection, "SELECT name FROM brands ORDER BY name", cbBrand);
                    LoadDataToComboBox(connection, "SELECT name FROM colors ORDER BY name", cbColor);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка підключення до бази даних при завантаженні фільтрів: {ex.Message}", "Помилка БД", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private string GetCategoryNameForSubtype(string subtypeName)
        {
            if (string.IsNullOrWhiteSpace(subtypeName))
            {
                return null;
            }

            string categoryName = null;
            string query = @"
                SELECT 
                    CTG.name 
                FROM 
                    product_subtypes AS PST 
                INNER JOIN 
                    categories AS CTG ON PST.category_id = CTG.category_id 
                WHERE 
                    PST.name = @SubtypeName 
                LIMIT 1;
            ";

            using (var connection = new SqliteConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@SubtypeName", subtypeName);
                        object result = command.ExecuteScalar();
                        if (result != null)
                        {
                            categoryName = result.ToString();
                        }
                    }
                }
                catch (Exception) {}
            }
            return categoryName;
        }

        private void LoadChildrenProducts(string filterWhereClause)
        {
            string baseSql = @"
                SELECT
                    P.product_id, 
                    P.name, 
                    P.base_price, 
                    P.season,
                    B.name AS brand_name,
                    T.name AS subtype_name,
                    P.images,
                    MIN(S.availability_status) AS availability_status_aggregated,
                    SUM(S.quantity) AS total_quantity
                FROM
                    products AS P
                INNER JOIN brands AS B ON P.brand_id = B.brand_id
                INNER JOIN stock AS S ON P.product_id = S.product_id
                INNER JOIN categories AS CTG ON P.category_id = CTG.category_id
                LEFT JOIN product_subtypes AS T ON P.subtype_id = T.subtype_id
                WHERE
                    CTG.name = 'Дітям' 
            ";

            if (!string.IsNullOrEmpty(filterWhereClause))
            {
                baseSql += " AND " + filterWhereClause;
            }

            baseSql += @" 
                GROUP BY 
                    P.product_id, P.name, P.base_price, P.season, B.name, T.name, P.images
                ORDER BY 
                    P.name;
            ";

            LoadDataToListView(baseSql);
        }

        private void LoadDataToListView(string sqlQuery)
        {
            lvChildren.Items.Clear();
            imageListChildren.Images.Clear();

            try
            {
                using (var connection = new SqliteConnection(ConnectionString))
                {
                    connection.Open();
                    using (var command = new SqliteCommand(sqlQuery, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string fileName = reader["images"] is DBNull ? string.Empty : reader["images"].ToString();
                            string imageKey = string.Empty;

                            int productId = reader.GetInt32(reader.GetOrdinal("product_id"));

                            if (!string.IsNullOrEmpty(fileName))
                            {
                                string fullPath = Path.Combine(ImagesFolderPath, fileName);

                                if (File.Exists(fullPath))
                                {
                                    try
                                    {
                                        imageKey = fileName;
                                        if (!imageListChildren.Images.ContainsKey(imageKey))
                                        {
                                            using (Image img = Image.FromFile(fullPath))
                                            {
                                                imageListChildren.Images.Add(imageKey, (Image)img.Clone());
                                            }
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        imageKey = string.Empty;
                                    }
                                }
                            }

                            var item = new ListViewItem(reader["name"].ToString());

                            item.Tag = productId;

                            if (!string.IsNullOrEmpty(imageKey))
                            {
                                item.ImageKey = imageKey;
                            }

                            item.SubItems.Add(reader["subtype_name"] is DBNull ? "N/A" : reader["subtype_name"].ToString());
                            item.SubItems.Add(reader["brand_name"].ToString());
                            item.SubItems.Add(reader["season"] is DBNull ? "N/A" : reader["season"].ToString());

                            item.SubItems.Add("Усі");
                            item.SubItems.Add("Усі");

                            item.SubItems.Add(reader["base_price"].ToString());

                            item.SubItems.Add(reader["availability_status_aggregated"].ToString());
                            item.SubItems.Add(reader["total_quantity"].ToString());

                            lvChildren.Items.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження даних: {ex.Message}", "Помилка БД", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnResult_Click(object sender, EventArgs e)
        {
            List<string> filters = new List<string>();
            bool sizeOrColorFilterApplied = false;
            bool sizeFilterAttempted = false;

            if (!string.IsNullOrWhiteSpace(tbSearch.Text))
            {
                string searchText = tbSearch.Text.Trim().Replace("'", "''");
                filters.Add($"(P.name LIKE '%{searchText}%' OR P.description LIKE '%{searchText}%')");
            }

            string selectedSubtype = cbTypeChildren.SelectedItem?.ToString();
            if (!string.IsNullOrWhiteSpace(selectedSubtype))
            {
                filters.Add($"T.name = '{selectedSubtype.Trim().Replace("'", "''")}'");
            }

            if (rbSummer.Checked) filters.Add("P.season = 'Літо'");
            else if (rbAutumn.Checked) filters.Add("P.season = 'Осінь'");
            else if (rbWinter.Checked) filters.Add("P.season = 'Зима'");
            else if (rbSpring.Checked) filters.Add("P.season = 'Весна'");

            if (cbBrand.SelectedItem != null && !string.IsNullOrWhiteSpace(cbBrand.Text))
            {
                filters.Add($"B.name = '{cbBrand.Text.Trim().Replace("'", "''")}'");
            }

            float sizeFrom = 0, sizeTo = 0;
            sizeFilterAttempted = float.TryParse(tbSizeFrom.Text, out sizeFrom) || float.TryParse(tbSizeTo.Text, out sizeTo);

            if (sizeFilterAttempted)
            {
                string category = GetCategoryNameForSubtype(selectedSubtype);

                if (string.IsNullOrWhiteSpace(selectedSubtype))
                {
                    MessageBox.Show("Фільтр за розміром доступний лише для категорії 'Взуття'. Спочатку оберіть тип взуття.", "Увага", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                else if (category != null && category != "Взуття")
                {
                    MessageBox.Show($"Неможливо застосувати фільтр за розміром для підтипу '{selectedSubtype}' (Категорія: {category}). Розміри доступні лише для 'Взуття'.", "Увага", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                else if (category == "Взуття")
                {
                    if (float.TryParse(tbSizeFrom.Text, out sizeFrom))
                    {
                        sizeOrColorFilterApplied = true;
                        filters.Add($"Z.size_value >= {sizeFrom}");
                    }
                    if (float.TryParse(tbSizeTo.Text, out sizeTo))
                    {
                        sizeOrColorFilterApplied = true;
                        filters.Add($"Z.size_value <= {sizeTo}");
                    }
                }
            }

            if (cbColor.SelectedItem != null && !string.IsNullOrWhiteSpace(cbColor.Text))
            {
                sizeOrColorFilterApplied = true;
                filters.Add($"C.name = '{cbColor.Text.Trim().Replace("'", "''")}'");
            }

            if (decimal.TryParse(tbPriceFrom.Text, out decimal priceFrom))
            {
                filters.Add($"P.base_price >= {priceFrom}");
            }
            if (decimal.TryParse(tbPriceTo.Text, out decimal priceTo))
            {
                filters.Add($"P.base_price <= {priceTo}");
            }

            if (rbInScore.Checked) filters.Add("S.availability_status = 'В наявності'");
            else if (rbComingSoon.Checked) filters.Add("S.availability_status = 'Скоро буде'");

            string filterWhereClause = string.Join(" AND ", filters);

            if (sizeOrColorFilterApplied)
            {
                string extendedSql = $@"
                    SELECT
                        P.product_id,  
                        P.name,  
                        P.base_price,  
                        P.season,
                        B.name AS brand_name,
                        T.name AS subtype_name,
                        P.images,
                        MIN(S.availability_status) AS availability_status_aggregated,
                        SUM(S.quantity) AS total_quantity
                    FROM
                        products AS P
                    INNER JOIN brands AS B ON P.brand_id = B.brand_id
                    
                    INNER JOIN stock AS S ON P.product_id = S.product_id
                    INNER JOIN colors AS C ON S.color_id = C.color_id
                    INNER JOIN sizes AS Z ON S.size_id = Z.size_id
                    
                    INNER JOIN categories AS CTG ON P.category_id = CTG.category_id
                    LEFT JOIN product_subtypes AS T ON P.subtype_id = T.subtype_id
                    WHERE
                        CTG.name = 'Дітям'
                        AND ({filterWhereClause})
                    GROUP BY
                        P.product_id, P.name, P.base_price, P.season, B.name, T.name, P.images
                    ORDER BY
                        P.name;
                ";
                LoadDataToListView(extendedSql);
            }
            else
            {
                LoadChildrenProducts(filterWhereClause);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            tbSearch.Text = string.Empty;
            tbSizeFrom.Text = string.Empty;
            tbSizeTo.Text = string.Empty;
            tbPriceFrom.Text = string.Empty;
            tbPriceTo.Text = string.Empty;

            cbTypeChildren.SelectedIndex = -1;
            cbBrand.SelectedIndex = -1;
            cbColor.SelectedIndex = -1;

            rbAll.Checked = true;
            rbAllScore.Checked = true;

            LoadChildrenProducts(string.Empty);
        }

        private void pbSearch_Click(object sender, EventArgs e)
        {
            btnResult_Click(sender, e);
        }

        private void lvChildren_DoubleClick(object sender, EventArgs e)
        {
            if (_isNavigating) return;

            if (lvChildren.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = lvChildren.SelectedItems[0];

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

        private void FormChildren_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }

        private void btnWoman_Click(object sender, EventArgs e)
        {
            FormWoman formWoman = new FormWoman();
            formWoman.Show();
            this.Hide();
        }

        private void btnMan_Click(object sender, EventArgs e)
        {
            FormMan formMan = new FormMan();
            formMan.Show();
            this.Hide();
        }

        private void pbSale_Click(object sender, EventArgs e)
        {
            FormSale formSale = new FormSale();
            formSale.Show();
            this.Hide();
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
