using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SQLite;
using System.Xml.Serialization;
using FastReport;
using System.IO;

namespace Blacksmith_Store
{
    public partial class FormReport : Form
    {
        public class SaleReportItem
        {
            public string Name { get; set; }
            public double Size { get; set; }
            public string Color { get; set; }
            public int Quantity { get; set; }
            public decimal PriceAtSale { get; set; }
            public decimal TotalSale { get; set; }
        }

        public class ReportData
        {
            public List<SaleReportItem> Items { get; set; } = new List<SaleReportItem>();
            public decimal OverallTotalAmount { get; set; }
            public int OverallTotalQuantity { get; set; }

            public string SelectedSubtype { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
        }

        private const string ConnectionString = @"Data Source=D:\Все для навчання\4_Курс\Blacksmith_Store\Blacksmith_Store\bin\Debug\Blacksmith_StoreBD";

        private ReportData _currentReportData = new ReportData();

        public FormReport()
        {
            InitializeComponent();
            msMenu.BringToFront();

            this.Load += FormReport_Load;

            monthCalendar1.SelectionStart = DateTime.Now.Date;
            monthCalendar1.SelectionEnd = DateTime.Now.Date;

            SetDataGridViewFont();
        }

        private void SetDataGridViewFont()
        {
            try
            {
                Font customFont = new Font("Tahoma", 10.8f, FontStyle.Regular, GraphicsUnit.Point);

                dgvReport.Font = customFont;
                dgvReport.ColumnHeadersDefaultCellStyle.Font = customFont;
                dgvReport.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
                dgvReport.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при встановленні шрифту: {ex.Message}");
            }
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

        private void ClearReportData()
        {
            dgvReport.DataSource = null;
            if (lbReportTotalAmount != null) lbReportTotalAmount.Text = "Загальна сума: 0.00 ₴";
            if (lbReportTotalQuantity != null) lbReportTotalQuantity.Text = "Продано одиниць: 0 шт";

            _currentReportData = new ReportData();
        }

        private void FormReport_Load(object sender, EventArgs e)
        {
            UpdateCartSummary();

            LoadSubtypesToComboBox();
            SetupReportGrid();
            ClearReportData();
        }

        private void SetupReportGrid()
        {
            dgvReport.AutoGenerateColumns = false;
            dgvReport.ReadOnly = true;
            dgvReport.AllowUserToAddRows = false;
            dgvReport.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            dgvReport.Columns.Clear();

            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "Назва товару", Width = 200, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Size", HeaderText = "Розмір", Width = 70 });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Color", HeaderText = "Колір", Width = 90 });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Quantity", HeaderText = "Кількість", Width = 70 });

            dgvReport.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "PriceAtSale",
                HeaderText = "Ціна продажу",
                Width = 100,
                DefaultCellStyle = { Format = "C2" }
            });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "TotalSale",
                HeaderText = "Загальна сума",
                Width = 120,
                DefaultCellStyle = { Format = "C2" }
            });
        }

        private void LoadSubtypesToComboBox()
        {
            cbSubtype.Items.Clear();
            cbSubtype.Items.Add("Усі типи");

            string sql = "SELECT name FROM product_subtypes ORDER BY name";

            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand(sql, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                cbSubtype.Items.Add(reader["name"].ToString());
                            }
                        }
                    }
                }
                cbSubtype.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження типів: {ex.Message}", "Помилка БД", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GenerateReport()
        {
            DateTime startDate = monthCalendar1.SelectionStart.Date;
            DateTime calendarEndDate = monthCalendar1.SelectionEnd.Date;

            DateTime exclusiveEndDate = calendarEndDate.AddDays(1);

            string selectedSubtype = cbSubtype.SelectedItem?.ToString();

            if (startDate > calendarEndDate)
            {
                MessageBox.Show("Початкова дата не може бути пізнішою за кінцеву дату.", "Помилка вибору дати", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string sql = @"
                SELECT 
                    p.name AS ProductName,
                    s.value AS Size,
                    c.name AS Color,
                    SUM(oi.quantity) AS TotalQuantity,
                    oi.price_at_time_of_sale AS Price,
                    SUM(oi.quantity * oi.price_at_time_of_sale) AS TotalSale
                FROM orders o
                JOIN order_items oi ON o.order_id = oi.order_id
                JOIN stock st ON oi.stock_id = st.stock_id
                JOIN products p ON st.product_id = p.product_id
                LEFT JOIN sizes s ON st.size_id = s.size_id
                LEFT JOIN colors c ON st.color_id = c.color_id
                LEFT JOIN product_subtypes ps ON p.subtype_id = ps.subtype_id
                                
                WHERE o.order_date >= @StartDate AND o.order_date < @ExclusiveEndDate
                " + (selectedSubtype != "Усі типи" ? " AND ps.name = @SubtypeName" : "") + @"
                GROUP BY 
                    p.name, 
                    s.value, 
                    c.name,
                    oi.price_at_time_of_sale
                ORDER BY TotalSale DESC;";

            List<SaleReportItem> reportData = new List<SaleReportItem>();

            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@StartDate", startDate.ToString("yyyy-MM-dd"));
                        cmd.Parameters.AddWithValue("@ExclusiveEndDate", exclusiveEndDate.ToString("yyyy-MM-dd"));

                        if (selectedSubtype != "Усі типи")
                        {
                            cmd.Parameters.AddWithValue("@SubtypeName", selectedSubtype);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                double sizeValue = 0.0;
                                if (!reader.IsDBNull(reader.GetOrdinal("Size")))
                                {
                                    double.TryParse(reader["Size"].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out sizeValue);
                                }

                                reportData.Add(new SaleReportItem
                                {
                                    Name = reader["ProductName"].ToString(),
                                    Size = sizeValue,
                                    Color = reader.IsDBNull(reader.GetOrdinal("Color")) ? "Н/Д" : reader["Color"].ToString(),
                                    Quantity = Convert.ToInt32(reader["TotalQuantity"]),
                                    PriceAtSale = Convert.ToDecimal(reader["Price"]),
                                    TotalSale = Convert.ToDecimal(reader["TotalSale"])
                                });
                            }
                        }
                    }
                }

                dgvReport.DataSource = reportData;

                if (reportData.Any())
                {
                    decimal overallTotal = reportData.Sum(item => item.TotalSale);
                    int overallQuantity = reportData.Sum(item => item.Quantity);

                    _currentReportData.Items = reportData;
                    _currentReportData.OverallTotalAmount = overallTotal;
                    _currentReportData.OverallTotalQuantity = overallQuantity;

                    _currentReportData.SelectedSubtype = selectedSubtype;
                    _currentReportData.StartDate = startDate;
                    _currentReportData.EndDate = calendarEndDate;

                    if (lbReportTotalAmount != null)
                    {
                        lbReportTotalAmount.Text = $"Загальна сума: {overallTotal:C2}";
                    }
                    if (lbReportTotalQuantity != null)
                    {
                        lbReportTotalQuantity.Text = $"Продано одиниць: {overallQuantity} шт";
                    }
                }
                else
                {
                    ClearReportData();
                    MessageBox.Show("За вибраний період та типом не знайдено жодного продажу.", "Звіт порожній", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка формування звіту: {ex.Message}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ClearReportData();
            }
        }

        private List<Product> productList = new List<Product>();

        private void FormReport_FormClosing(object sender, FormClosingEventArgs e)
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

        private void tsmiAddProduct_Click(object sender, EventArgs e)
        {
            this.Hide();
            FormAddProduct formAddProduct = new FormAddProduct();
            formAddProduct.Show();
        }

        private void pbPrint_Click(object sender, EventArgs e)
        {
            if (_currentReportData == null || !_currentReportData.Items.Any())
            {
                MessageBox.Show("Спочатку згенеруйте звіт про продажі, натиснувши кнопку 'Результат', та переконайтеся, що дані присутні.", "Друк неможливий", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string xmlFileName = "sales_report_for_print.xml";
            string reportTemplatePath = "ProductsReport.frx";

            try
            {
                ClassSerialiaze.SerializeToXml(ref _currentReportData, xmlFileName);

                using (Report report = new Report())
                {
                    if (!File.Exists(reportTemplatePath))
                    {
                        MessageBox.Show($"Файл шаблону FastReport не знайдено: {reportTemplatePath}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    report.Load(reportTemplatePath);

                    if (File.Exists(xmlFileName))
                    {
                        report.RegisterData(xmlFileName, "ReportData");
                    }
                    else
                    {
                        MessageBox.Show("Помилка: Не вдалося створити файл XML даних.", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    report.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при підготовці/відображенні звіту FastReport: {ex.Message}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnReportResult_Click(object sender, EventArgs e)
        {
            GenerateReport();
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            monthCalendar1.SelectionStart = DateTime.Now.Date;
            monthCalendar1.SelectionEnd = DateTime.Now.Date;

            ClearReportData();
            LoadSubtypesToComboBox();
            
        }
    }
}
