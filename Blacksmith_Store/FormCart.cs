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
using System.Globalization;
using FastReport;
using System.IO;
using System.Xml.Serialization;

namespace Blacksmith_Store
{
    public partial class FormCart : Form
    {
        public class ReceiptData
        {
            public List<CartItem> Items { get; set; } = new List<CartItem>();
            public decimal TotalAmountToPay { get; set; }
            public DateTime DateOfPrint { get; set; }
            public long OrderId { get; set; }
        }

        private const string ConnectionString = @"Data Source=D:\Все для навчання\4_Курс\Blacksmith_Store\Blacksmith_Store\bin\Debug\Blacksmith_StoreBD";
        private const string ImagesFolderPath = @"D:\Все для навчання\4_Курс\Blacksmith_Store\Blacksmith_Store\bin\Debug\PNG\Product";

        private const string ReceiptXmlFileName = "cart_receipt_for_print.xml";
        private const string ReceiptFrxTemplate = "ProductsReceipt.frx";

        private int _lastSelectedRowIndex = -1;

        public FormCart()
        {
            InitializeComponent();
            this.Load += FormCart_Load;

            SetDataGridViewFont();

            this.dgvCart.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvCart_CellClick);

            msMenu.BringToFront();
        }

        private void SetDataGridViewFont()
        {
            try
            {
                Font customFont = new Font("Tahoma", 13.8f, FontStyle.Regular, GraphicsUnit.Point);

                dgvCart.Font = customFont;
                dgvCart.ColumnHeadersDefaultCellStyle.Font = customFont;

                dgvCart.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
                dgvCart.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

                dgvCart.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgvCart.MultiSelect = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при встановленні шрифту: {ex.Message}");
            }
        }

        private void ConfigureDGVLayout()
        {
            if (dgvCart.Columns.Contains("ProductId"))
            {
                dgvCart.Columns["ProductId"].Visible = false;
            }

            if (dgvCart.Columns.Contains("Type"))
            {
                dgvCart.Columns["Type"].Visible = false;
            }
            if (dgvCart.Columns.Contains("ImageFileName"))
            {
                dgvCart.Columns["ImageFileName"].Visible = false;
            }

            if (dgvCart.Columns.Contains("Price"))
            {
                dgvCart.Columns["Price"].DefaultCellStyle.Format = "C2";
            }
            if (dgvCart.Columns.Contains("TotalPrice"))
            {
                dgvCart.Columns["TotalPrice"].DefaultCellStyle.Format = "C2";
            }

            if (dgvCart.Columns.Contains("Price"))
            {
                dgvCart.Columns["Price"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            if (dgvCart.Columns.Contains("TotalPrice"))
            {
                dgvCart.Columns["TotalPrice"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            if (dgvCart.Columns.Contains("Quantity"))
            {
                dgvCart.Columns["Quantity"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            if (dgvCart.Columns.Contains("Size"))
            {
                dgvCart.Columns["Size"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        }

        private void FormCart_Load(object sender, EventArgs e)
        {
            SetupCartGrid();
            LoadCartItems();
        }

        private void SetupCartGrid()
        {
            dgvCart.AutoGenerateColumns = false;
            dgvCart.ReadOnly = true;

            dgvCart.Columns.Clear();

            dgvCart.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Name",
                HeaderText = "Назва товару",
                Width = 160,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            dgvCart.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "SubtypeName",
                HeaderText = "Тип",
                Width = 140
            });

            dgvCart.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Size",
                HeaderText = "Розмір",
                Width = 70
            });

            dgvCart.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Color",
                HeaderText = "Колір",
                Width = 120
            });

            dgvCart.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Price",
                HeaderText = "Ціна за 1 шт.",
                Width = 120,
                DefaultCellStyle = { Format = "C2" }
            });

            dgvCart.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Quantity",
                HeaderText = "Кількість",
                Width = 100
            });

            dgvCart.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "TotalPrice",
                HeaderText = "Загальна ціна",
                Width = 120,
                DefaultCellStyle = { Format = "C2" }
            });

            dgvCart.Columns.Add(new DataGridViewButtonColumn
            {
                HeaderText = "Дія",
                Text = "Видалити",
                UseColumnTextForButtonValue = true,
                Width = 100
            });

            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ProductId", Visible = false });

            dgvCart.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        }

        public void LoadCartItems()
        {
            int? selectedProductId = null;
            float? selectedSize = null;
            string selectedColor = null;

            if (dgvCart.SelectedRows.Count > 0)
            {
                var currentItem = dgvCart.SelectedRows[0].DataBoundItem as CartItem;
                if (currentItem != null)
                {
                    selectedProductId = currentItem.ProductId;
                    selectedSize = currentItem.Size;
                    selectedColor = currentItem.Color;
                }
            }

            dgvCart.DataSource = null;
            dgvCart.DataSource = new List<CartItem>(CartManager.CartItems);

            ConfigureDGVLayout();
            CalculateTotals();

            dgvCart.ClearSelection();
            _lastSelectedRowIndex = -1;

            if (selectedProductId.HasValue)
            {
                foreach (DataGridViewRow row in dgvCart.Rows)
                {
                    var item = row.DataBoundItem as CartItem;

                    if (item != null &&
                        item.ProductId == selectedProductId &&
                        Nullable.Equals(item.Size, selectedSize) &&
                        item.Color == selectedColor)
                    {
                        row.Selected = true;
                        _lastSelectedRowIndex = row.Index;
                        break;
                    }
                }
            }
        }

        private void dgvCart_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex == 7) return;

            if (dgvCart.Rows[e.RowIndex].Selected && e.RowIndex == _lastSelectedRowIndex)
            {
                dgvCart.ClearSelection();
                _lastSelectedRowIndex = -1;
            }
            else
            {
                _lastSelectedRowIndex = e.RowIndex;
            }
        }

        private void CalculateTotals()
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
            if (lbCost != null)
            {
                lbCost.Text = formattedTotal;
            }
        }

        private int GetRealStockQuantity(int productId, float? size, string color)
        {
            int quantity = 0;
            string sql = @"
                SELECT S.quantity 
                FROM stock S
                LEFT JOIN sizes Z ON S.size_id = Z.size_id
                LEFT JOIN colors C ON S.color_id = C.color_id
                WHERE S.product_id = @Pid";

            if (size.HasValue) sql += " AND Z.value = @Size";
            if (!string.IsNullOrEmpty(color)) sql += " AND C.name = @Color";

            try
            {
                using (var connection = new SqliteConnection(ConnectionString))
                {
                    connection.Open();
                    using (var command = new SqliteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Pid", productId);

                        if (size.HasValue) command.Parameters.AddWithValue("@Size", size.Value);
                        if (!string.IsNullOrEmpty(color)) command.Parameters.AddWithValue("@Color", color);

                        object result = command.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            quantity = Convert.ToInt32(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка перевірки складу: " + ex.Message);
            }
            return quantity;
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Ви впевнені, що хочете очистити кошик?", "Очистити кошик", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                CartManager.ClearCart();
                LoadCartItems();
                MessageBox.Show("Кошик очищено.", "Успіх", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void FormCart_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
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

        private void btnMinus_Click(object sender, EventArgs e)
        {
            ChangeQuantity(-1);
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            ChangeQuantity(1);
        }

        private void ChangeQuantity(int change)
        {
            if (dgvCart.SelectedRows.Count == 0)
            {
                MessageBox.Show("Будь ласка, виберіть товар для зміни кількості.", "Увага", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataGridViewRow row = dgvCart.SelectedRows[0];
            CartItem selectedItem = row.DataBoundItem as CartItem;

            if (selectedItem != null)
            {
                int newQuantity = selectedItem.Quantity + change;

                if (change > 0)
                {
                    int stockAvailable = GetRealStockQuantity(selectedItem.ProductId, selectedItem.Size, selectedItem.Color);

                    if (newQuantity > stockAvailable)
                    {
                        MessageBox.Show($"Вибачте, на складі доступно всього {stockAvailable} шт. цього товару.\nБільше додати неможливо.",
                                        "Обмеження складу", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    int remainingAfterPurchase = stockAvailable - newQuantity;
                    if (remainingAfterPurchase < 3)
                    {
                        MessageBox.Show($"Увага! Товар закінчується.\n\nТовар: {selectedItem.Name}\nЗалишиться на складі: {remainingAfterPurchase} шт.",
                                        "Обмежений залишок", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (newQuantity <= 0)
                {
                    CartManager.RemoveItem(selectedItem.ProductId, selectedItem.Size, selectedItem.Color);
                }
                else
                {
                    CartManager.UpdateQuantity(selectedItem.ProductId, selectedItem.Size, selectedItem.Color, newQuantity);
                }

                LoadCartItems();
            }
        }

        private void btnContinueShopping_Click(object sender, EventArgs e)
        {
            FormMain formMain = new FormMain();
            formMain.Show();
            this.Hide();
        }

        private void PrintReceipt(long orderId)
        {
            if (CartManager.CartItems.Count == 0)
            {
                return;
            }

            ReceiptData receiptData = new ReceiptData
            {
                DateOfPrint = DateTime.Now,
                TotalAmountToPay = CartManager.CartItems.Sum(item => item.TotalPrice),
                OrderId = orderId,
                Items = new List<CartItem>(CartManager.CartItems)
            };

            try
            {
                ClassSerialiaze.SerializeToXml(ref receiptData, ReceiptXmlFileName);

                using (Report report = new Report())
                {
                    if (!File.Exists(ReceiptFrxTemplate))
                    {
                        MessageBox.Show($"Файл шаблону FastReport не знайдено: {ReceiptFrxTemplate}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    report.Load(ReceiptFrxTemplate);

                    if (File.Exists(ReceiptXmlFileName))
                    {
                        report.RegisterData(ReceiptXmlFileName, "ReceiptData");
                    }
                    else
                    {
                        MessageBox.Show("Помилка: Не вдалося створити файл XML даних чека.", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    report.SetParameterValue("ReportOrderId", orderId.ToString("D10"));
                    report.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при підготовці/відображенні чека FastReport: {ex.Message}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnPlaceOrder_Click(object sender, EventArgs e)
        {
            if (CartManager.CartItems.Count == 0)
            {
                MessageBox.Show("Ваш кошик порожній. Додайте товари для оформлення замовлення.", "Помилка замовлення", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("Підтвердити оформлення замовлення?", "Підтвердження", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
            {
                return;
            }

            try
            {
                long orderId = PlaceOrder(CartManager.CartItems);

                PrintReceipt(orderId);

                MessageBox.Show("Замовлення успішно оформлено! Кошик очищено.", "Замовлення оформлено", MessageBoxButtons.OK, MessageBoxIcon.Information);
                CartManager.ClearCart();
                LoadCartItems();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при оформленні замовлення: {ex.Message}", "Критична помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            FormMain formMain = new FormMain();
            formMain.Show();
            this.Hide();
        }

        private long PlaceOrder(IReadOnlyList<CartItem> items)
        {
            if (items == null || items.Count == 0) return 0;

            decimal totalAmount = items.Sum(item => item.TotalPrice);
            long newOrderId = 0;

            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string insertOrderSql = "INSERT INTO orders (order_date, total_amount) VALUES (@Date, @Total); SELECT last_insert_rowid();";
                        using (var cmd = new SqliteCommand(insertOrderSql, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@Date", DateTime.Now.ToString("yyyy-MM-dd"));
                            cmd.Parameters.AddWithValue("@Total", totalAmount);
                            newOrderId = (long)cmd.ExecuteScalar();
                        }

                        foreach (var item in items)
                        {
                            StockDetails details =
                                DatabaseHelper.GetStockDetails(item.ProductId, item.Color, item.Size, connection, transaction);

                            if (details.StockId == 0)
                            {
                                throw new Exception($"Не вдалося знайти товар на складі (StockID) для: {item.Name}.");
                            }

                            string insertItemSql = @"
                                INSERT INTO order_items (order_id, stock_id, quantity, price_at_time_of_sale) 
                                VALUES (@OrderId, @StockId, @Quantity, @Price)";

                            using (var cmd = new SqliteCommand(insertItemSql, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@OrderId", newOrderId);
                                cmd.Parameters.AddWithValue("@StockId", details.StockId);
                                cmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                                cmd.Parameters.AddWithValue("@Price", item.Price);
                                cmd.ExecuteNonQuery();
                            }

                            string updateStockSql = "UPDATE stock SET quantity = quantity - @Quantity WHERE stock_id = @StockId AND quantity >= @Quantity";
                            using (var cmd = new SqliteCommand(updateStockSql, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                                cmd.Parameters.AddWithValue("@StockId", details.StockId);

                                int rowsAffected = cmd.ExecuteNonQuery();
                                if (rowsAffected == 0)
                                {
                                    throw new Exception($"Недостатня кількість товару {item.Name} на складі.");
                                }
                            }
                        }

                        transaction.Commit();
                        return newOrderId;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception($"Не вдалося оформити замовлення. Причина: {ex.Message}", ex);
                    }
                }
            }
        }

        private void dgvCart_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == dgvCart.Columns[7].Index && e.RowIndex >= 0)
            {
                if (dgvCart.Rows[e.RowIndex].DataBoundItem is CartItem itemToRemove)
                {
                    CartManager.RemoveItem(itemToRemove.ProductId, itemToRemove.Size, itemToRemove.Color);
                    LoadCartItems();
                    MessageBox.Show($"{itemToRemove.Name} видалено з кошика.", "Видалення", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}
