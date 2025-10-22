using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Blacksmith_Store
{
    public class Product
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double BasePrice { get; set; }
        public string ProductType { get; set; }
        public string Season { get; set; }
        public int CategoryId { get; set; }
        public string BrandName { get; set; }
        public string SubtypeName { get; set; }


        // Потрібен для XML-серіалізації
        public Product() { }

        // Потрібен для завантаження з БД
        public Product(string line)
        {
            try
            {
                var parts = line.Split('|');

                ProductId = int.TryParse(parts.ElementAtOrDefault(0), out int id) ? id : 0;
                Name = parts.ElementAtOrDefault(1);
                Description = string.IsNullOrWhiteSpace(parts.ElementAtOrDefault(2)) ? null : parts[2];
                BasePrice = double.TryParse(parts.ElementAtOrDefault(3), out double price) ? price : 0;
                ProductType = parts.ElementAtOrDefault(4);
                Season = string.IsNullOrWhiteSpace(parts.ElementAtOrDefault(5)) ? null : parts[5];
                CategoryId = int.TryParse(parts.ElementAtOrDefault(6), out int catId) ? catId : 0;
                BrandName = parts.ElementAtOrDefault(7);
                SubtypeName = string.IsNullOrWhiteSpace(parts.ElementAtOrDefault(8)) ? null : parts[8];
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка при створенні Product: " + ex.Message);
            }
        }

    }
}
