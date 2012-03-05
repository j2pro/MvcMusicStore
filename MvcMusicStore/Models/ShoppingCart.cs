using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MvcMusicStore.Models
{
    public partial class ShoppingCart
    {
        MusicStoreEntities storeDb = new MusicStoreEntities();

        string ShoppingCartId { get; set; }

        public const string CartSessionKey = "CartId";

        public static ShoppingCart GetCart(HttpContextBase context)
        {
            var cart = new ShoppingCart();
            cart.ShoppingCartId = cart.GetCartId(context);
            return cart;
        }

        //Helper method to simplify shopping cart calls
        public static ShoppingCart GetCart(Controller controller)
        {
            return GetCart(controller.HttpContext);
        }

        public void AddToCart(Album album)
        {
            //Get the matching cart and album instances
            var cartItem = storeDb.Carts.SingleOrDefault(
                c => c.CartId == ShoppingCartId
                    && c.AlbumId == album.AlbumId);

            if (cartItem == null)
            {
                //create a new cart item if no cart item exists

                cartItem = new Cart
                {
                    AlbumId = album.AlbumId,
                    CartId = ShoppingCartId,
                    Count = 1,
                    DateCreated = DateTime.Now
                };

                storeDb.Carts.Add(cartItem);
            }
            else
            {
                // if the item does exist in the cart, then add one to the quantity
                cartItem.Count++;
            }

            //Save Changes
            storeDb.SaveChanges();
        }

        public int RemoveFromCart(int id)
        {
            //Get the cart
            var cartItem = storeDb.Carts.Single(
                cart => cart.CartId == ShoppingCartId
                    && cart.RecordId == id);
            int itemCount = 0;

            if (cartItem != null)
            {
                if (cartItem.Count > 1)
                {
                    cartItem.Count--;
                    itemCount = cartItem.Count;
                }
                else
                {
                    storeDb.Carts.Remove(cartItem);
                }

                //save changes
                storeDb.SaveChanges();
            }
            return itemCount;
        }

        public void EmptyCart()
        {
            var cartItems = storeDb.Carts.Where(cart => cart.CartId == ShoppingCartId).ToList();

            foreach (var cartItem in cartItems)
            {
                storeDb.Carts.Remove(cartItem);
            }

            //save changes
            storeDb.SaveChanges();
        }

        public List<Cart> GetCartItems()
        {
            return storeDb.Carts.Where(cart => cart.CartId == ShoppingCartId).ToList();
        }

        public int GetCount()
        {
            //Get the count of each item in the cart and sum them up
            int? count = (from cartItems in storeDb.Carts
                          where cartItems.CartId == ShoppingCartId
                          select (int?)cartItems.Count).Sum();

            return count ?? 0;
        }

        public decimal GetTotal()
        {
            // Multiply album price by count of that album to get
            // the current price for each of those albums in the cart
            // sum all album price totals to get the cart total

            decimal? total = (from cartItems in storeDb.Carts
                              where cartItems.CartId == ShoppingCartId
                              select (int?)cartItems.Count * cartItems.Album.Price).Sum();
            return total ?? decimal.Zero;
        }

        public int CreateOrder(Order order)
        {
            decimal orderTotal = 0;

            var cartItems = GetCartItems();

            //Iterate over the items in the cart, adding the order details for each
            foreach (var item in cartItems)
            {
                var orderDetail = new OrderDetail
                {
                    AlbumId = item.AlbumId,
                    OrderId = order.OrderId,
                    UnitPrice = item.Album.Price,
                    Quantity = item.Count
                };

                orderTotal += (item.Count * item.Album.Price);

                storeDb.OrderDetails.Add(orderDetail);
            }

            order.Total = orderTotal;

            storeDb.SaveChanges();

            EmptyCart();

            return order.OrderId;
        }

        public string GetCartId(HttpContextBase context)
        {
            if (context.Session[CartSessionKey] == null)
            {
                if (!string.IsNullOrWhiteSpace(context.User.Identity.Name))
                {
                    context.Session[CartSessionKey] = context.User.Identity.Name;
                }
                else
                {
                    Guid tempCartId = Guid.NewGuid();

                    context.Session[CartSessionKey] = tempCartId.ToString();
                }
            }
            return context.Session[CartSessionKey].ToString();
        }

        public void MigrateCart(string userName)
        {
            var shoppingCart = storeDb.Carts.Where(c => c.CartId == ShoppingCartId);

            foreach (Cart item in shoppingCart)
            {
                item.CartId = userName;
            }

            storeDb.SaveChanges();
        }
    }
}