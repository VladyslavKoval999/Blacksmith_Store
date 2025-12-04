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
    public partial class FormTopSellers : Form
    {
        private const string ConnectionString = @"Data Source=D:\Все для навчання\4_Курс\Blacksmith_Store\Blacksmith_Store\bin\Debug\Blacksmith_StoreBD";
        private const string ImagesFolderPath = @"D:\Все для навчання\4_Курс\Blacksmith_Store\Blacksmith_Store\bin\Debug\PNG\Product";

        public FormTopSellers()
        {
            InitializeComponent();
            this.Load += FormTopSellers_Load;

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

        private void FormTopSellers_Load(object sender, EventArgs e)
        {
            LoadTopSellers();

            UpdateCartSummary();
        }

        private class ProductListItem
        {
            public int ProductId { get; set; }
            public string Name { get; set; }
            public string ImageFileName { get; set; }
        }

        private void LoadTopSellers()
        {
            try
            {
                var topSellers = GetTopSellingProducts(12);

                DisplayProducts(topSellers, new List<PictureBox> { pbN1, pbN2, pbN3, pbN4, pbN5, pbN6, pbN7, pbN8, pbN9, pbN10, pbN11, pbN12 });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження топ-продавців: {ex.Message}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private List<ProductListItem> GetTopSellingProducts(int limit)
        {
            var productList = new List<ProductListItem>();

            string sql = $@"
                SELECT 
                    P.product_id, P.name, P.images, SUM(OI.quantity) AS TotalSold
                FROM 
                    products AS P
                INNER JOIN 
                    stock AS S ON P.product_id = S.product_id -- З'єднання з таблицею stock
                INNER JOIN 
                    order_items AS OI ON S.stock_id = OI.stock_id -- З'єднання з order_items
                GROUP BY 
                    P.product_id, P.name, P.images
                ORDER BY 
                    TotalSold DESC
                LIMIT @Limit;";

            try
            {
                using (var connection = new SqliteConnection(ConnectionString))
                {
                    connection.Open();
                    using (var command = new SqliteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Limit", limit);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                productList.Add(new ProductListItem
                                {
                                    ProductId = reader.GetInt32(0),
                                    Name = reader.GetString(1),
                                    ImageFileName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка SQL-запиту: {ex.Message}");
                throw;
            }

            return productList;
        }

        private void DisplayProducts(List<ProductListItem> products, List<PictureBox> pictureBoxes)
        {
            for (int i = 0; i < pictureBoxes.Count; i++)
            {
                PictureBox pb = pictureBoxes[i];
                pb.Image = null;

                if (i < products.Count)
                {
                    var product = products[i];
                    pb.Tag = product.ProductId;

                    if (!string.IsNullOrEmpty(product.ImageFileName))
                    {
                        string fullPath = Path.Combine(ImagesFolderPath, product.ImageFileName);
                        if (File.Exists(fullPath))
                        {
                            try
                            {
                                using (var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
                                {
                                    pb.Image = Image.FromStream(fs);
                                }
                                pb.SizeMode = PictureBoxSizeMode.Zoom;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Не вдалося завантажити зображення {fullPath}: {ex.Message}");
                                pb.Image = null;
                            }
                        }
                    }

                    pb.Click -= ProductPictureBox_Click;
                    pb.DoubleClick -= ProductPictureBox_Click;
                    pb.DoubleClick += ProductPictureBox_Click;

                    pb.Visible = true;
                }
                else
                {
                    pb.Click -= ProductPictureBox_Click;
                    pb.DoubleClick -= ProductPictureBox_Click;
                    pb.Visible = false;
                }
            }
        }

        private void ProductPictureBox_Click(object sender, EventArgs e)
        {
            if (sender is PictureBox pb && pb.Tag is int productId)
            {
                this.Hide();
                FormProduct formProduct = new FormProduct(productId);
                formProduct.Show();
            }
        }

        private void FormTopSellers_FormClosing(object sender, FormClosingEventArgs e)
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
