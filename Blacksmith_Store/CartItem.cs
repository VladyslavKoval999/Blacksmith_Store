using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blacksmith_Store
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string SubtypeName { get; set; }
        public string ImageFileName { get; set; }
        public decimal Price { get; set; }
        public float? Size { get; set; }
        public string Color { get; set; }
        public int Quantity { get; set; } = 1;

        public decimal TotalPrice => Price * Quantity;

        public override bool Equals(object obj)
        {
            return obj is CartItem other && ProductId == other.ProductId && Size == other.Size && string.Equals(Color, other.Color, StringComparison.OrdinalIgnoreCase);
        }
    }
}
