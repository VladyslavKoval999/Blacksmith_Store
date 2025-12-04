using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blacksmith_Store
{
    public static class CartManager
    {
        private static List<CartItem> _cartItems = new List<CartItem>();

        public static IReadOnlyList<CartItem> CartItems => _cartItems.AsReadOnly();

        public static void AddItem(int productId, string name, string type, string subtypeName, string imageFileName, decimal price, float? size, string color, int quantity = 1)
        {
            var itemToAdd = new CartItem
            {
                ProductId = productId,
                Name = name,
                Type = type,
                SubtypeName = subtypeName,
                ImageFileName = imageFileName,
                Price = price,
                Size = size,
                Color = color,
                Quantity = quantity
            };

            var existingItem = _cartItems.FirstOrDefault(item => item.ProductId == itemToAdd.ProductId && item.Size == itemToAdd.Size && string.Equals(item.Color, itemToAdd.Color, StringComparison.OrdinalIgnoreCase));

            if (existingItem != null)
            {
                existingItem.Quantity += itemToAdd.Quantity;
            }
            else
            {
                _cartItems.Add(itemToAdd);
            }
        }

        public static void UpdateQuantity(int productId, float? size, string color, int newQuantity)
        {
            var item = _cartItems.FirstOrDefault(i => i.ProductId == productId && i.Size == size && string.Equals(i.Color, color, StringComparison.OrdinalIgnoreCase));

            if (item != null)
            {
                if (newQuantity > 0)
                {
                    item.Quantity = newQuantity;
                }
                else
                {
                    _cartItems.Remove(item);
                }
            }
        }

        public static void RemoveItem(int productId, float? size, string color)
        {
            _cartItems.RemoveAll(i => i.ProductId == productId && i.Size == size && string.Equals(i.Color, color, StringComparison.OrdinalIgnoreCase));
        }

        public static void ClearCart()
        {
            _cartItems.Clear();
        }
    }
}
