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
    public partial class FormSale : Form
    {
        private const string SaleImagesFolderPath = @"D:\Все для навчання\4_Курс\Blacksmith_Store\Blacksmith_Store\bin\Debug\PNG\Sale";

        private List<string> _saleImageFiles;

        private int _currentImageIndex = 0;
        public FormSale()
        {
            InitializeComponent();
            msMenu.BringToFront();

            this.Load += FormSale_Load;
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

        private void FormSale_Load(object sender, EventArgs e)
        {
            LoadSaleImages();
            UpdatePictureBox();

            UpdateCartSummary();
        }

        private void LoadSaleImages()
        {
            _saleImageFiles = new List<string>();
            try
            {
                if (Directory.Exists(SaleImagesFolderPath))
                {
                    string[] files = Directory.GetFiles(SaleImagesFolderPath, "*.png")
                        .Concat(Directory.GetFiles(SaleImagesFolderPath, "*.jpg"))
                        .ToArray();

                    _saleImageFiles.AddRange(files);
                    _saleImageFiles.Sort();
                }
                else
                {
                    MessageBox.Show($"Папка із зображеннями не знайдена: {SaleImagesFolderPath}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при завантаженні зображень: {ex.Message}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            _currentImageIndex = 0;

            if (_saleImageFiles.Count == 0)
            {
                btnLeft.Enabled = false;
                btnRight.Enabled = false;
            }
            else
            {
                btnLeft.Enabled = true;
                btnRight.Enabled = true;
            }
        }

        private void UpdatePictureBox()
        {
            if (_saleImageFiles != null && _saleImageFiles.Count > 0)
            {
                try
                {
                    if (pbSale.Image != null)
                    {
                        pbSale.Image.Dispose();
                    }

                    string imagePath = _saleImageFiles[_currentImageIndex];
                    using (Image img = Image.FromFile(imagePath))
                    {
                        pbSale.Image = (Image)img.Clone();
                    }
                    pbSale.SizeMode = PictureBoxSizeMode.Zoom;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка відображення зображення: {ex.Message}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    pbSale.Image = null;
                }
            }
            else
            {
                pbSale.Image = null;
            }
        }

        private void FormSale_FormClosing(object sender, FormClosingEventArgs e)
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

        private void btnLeft_Click(object sender, EventArgs e)
        {
            if (_saleImageFiles != null && _saleImageFiles.Count > 0)
            {
                _currentImageIndex = (_currentImageIndex - 1 + _saleImageFiles.Count) % _saleImageFiles.Count;
                UpdatePictureBox();
            }
        }

        private void btnRight_Click(object sender, EventArgs e)
        {
            if (_saleImageFiles != null && _saleImageFiles.Count > 0)
            {
                _currentImageIndex = (_currentImageIndex + 1) % _saleImageFiles.Count;
                UpdatePictureBox();
            }
        }
    }
}
