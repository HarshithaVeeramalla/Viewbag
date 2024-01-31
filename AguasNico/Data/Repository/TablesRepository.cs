using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using AguasNico.Data.Repository.IRepository;
using AguasNico.Models;
using Microsoft.AspNetCore.Mvc;
using AguasNico.Models.ViewModels.Tables;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AguasNico.Data.Repository
{
    public class TablesRepository(ApplicationDbContext db) : ITablesRepository
    {
        private readonly ApplicationDbContext _db = db;

        public async Task<List<InvoiceTable>> GetInvoicesByDates(DateTime startDate, DateTime endDate, Day invoiceDay, string invoiceDealer)
        {
            var clients = await _db
                .Clients
                .Where(x => x.DealerID == invoiceDealer && x.DeliveryDay == invoiceDay && x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

            var clientIDs = clients.Select(x => x.ID).ToList();

            var cartProducts = await _db
                .CartProducts
                .Include(x => x.Cart)
                .Where(x => clientIDs.Contains(x.Cart.ClientID) && x.CreatedAt.Date >= startDate.Date && x.CreatedAt.Date <= endDate.Date)
                .ToListAsync();

            var cartAbonoProducts = await _db
                .CartAbonoProducts
                .Include(x => x.Cart)
                .Where(x => clientIDs.Contains(x.Cart.ClientID) && x.CreatedAt.Date >= startDate.Date && x.CreatedAt.Date <= endDate.Date)
                .ToListAsync();

            List<InvoiceTable> invoices = [];
            foreach (var client in clients)
            {
                var cartProductsByClient = cartProducts.Where(x => x.Cart.ClientID == client.ID).ToList();
                if (cartProductsByClient.Count > 0)
                    invoices.Add(new()
                    {
                        Client = client,
                        Products = cartProductsByClient.GroupBy(x => x.Type).Select(Type => new InvoiceProduct
                        {
                            Type = Type.Key.GetDisplayName(),
                            Quantity = Type.Sum(x => x.Quantity),
                            Total = Type.Sum(x => x.SettedPrice * x.Quantity),
                        }).ToList(),
                    });

                var abonoProductsByClient = cartAbonoProducts.Where(x => x.Cart.ClientID == client.ID).ToList();
                if (abonoProductsByClient.Count > 0)
                {
                    var invoice = invoices.FirstOrDefault(x => x.Client.ID == client.ID);
                    if (invoice != null)
                    {
                        foreach (var abonoProduct in abonoProductsByClient)
                        {
                            var product = invoice.Products.FirstOrDefault(x => x.Type == abonoProduct.Type.GetDisplayName());
                            if (product != null)
                            {
                                product.Quantity += abonoProduct.Quantity;
                            }
                            else
                            {
                                invoice.Products.Add(new InvoiceProduct
                                {
                                    Type = abonoProduct.Type.GetDisplayName(),
                                    Quantity = abonoProduct.Quantity,
                                    Total = 0,
                                });
                            }
                        }
                    }
                    else
                    {
                        invoices.Add(new()
                        {
                            Client = client,
                            Products = abonoProductsByClient.GroupBy(x => x.Type).Select(Type => new InvoiceProduct
                            {
                                Type = Type.Key.GetDisplayName(),
                                Quantity = Type.Sum(x => x.Quantity),
                                Total = 0,
                            }).ToList(),
                        });
                    }
                }
            }

            return invoices;
        }
            
        public List<SoldProductsTable> GetSoldProductsByDate(DateTime date)
        {
            List<CartProduct> cartProducts = [.. _db.CartProducts.Where(x => x.Cart.CreatedAt.Date == date.Date)];
            List<CartAbonoProduct> cartAbonoProducts = [.. _db.CartAbonoProducts.Where(x => x.Cart.CreatedAt.Date == date.Date)];
            List<DispatchedProduct> dispatchedProducts = [.. _db.DispatchedProducts.Where(x => x.Route.CreatedAt.Date == date.Date)];
            List<ReturnedProduct> returnedProducts = [.. _db.ReturnedProducts.Where(x => x.Cart.CreatedAt.Date == date.Date)];
            List<SoldProductsTable> soldProducts = [];

            foreach (ProductType type in Enum.GetValues(typeof(ProductType)))
            {
                List<CartProduct> cartProductsByType = cartProducts.Where(x => x.Type == type).ToList();
                List<CartAbonoProduct> cartAbonoProductsByType = cartAbonoProducts.Where(x => x.Type == type).ToList();
                List<DispatchedProduct> dispatchedProductsByType = dispatchedProducts.Where(x => x.Type == type).ToList();
                List<ReturnedProduct> returnedProductsByType = returnedProducts.Where(x => x.Type == type).ToList();

                SoldProductsTable soldProduct = new()
                {
                    Name = type.GetDisplayName(),
                    Sold = cartProductsByType != null ? cartProductsByType.Sum(x => x.Quantity) : 0,
                    Dispatched = dispatchedProductsByType != null ? dispatchedProductsByType.Sum(x => x.Quantity) : 0,
                    Returned = returnedProductsByType != null ? returnedProductsByType.Sum(x => x.Quantity) : 0,
                };
                soldProduct.Sold += cartAbonoProductsByType != null ? cartAbonoProductsByType.Sum(x => x.Quantity) : 0;

                soldProducts.Add(soldProduct);
            }

            return soldProducts;
        }

        public List<SoldProductsTable> GetSoldProductsByDateAndRoute(DateTime date, long routeID)
        {
            List<CartProduct> cartProducts = [.. _db.CartProducts.Where(x => x.CreatedAt.Date == date.Date && x.Cart.RouteID == routeID)];
            List<CartAbonoProduct> cartAbonoProducts = [.. _db.CartAbonoProducts.Where(x => x.CreatedAt.Date == date.Date && x.Cart.RouteID == routeID)];
            List<DispatchedProduct> dispatchedProducts = [.. _db.DispatchedProducts.Where(x => x.CreatedAt.Date == date.Date && x.RouteID == routeID)];
            List<ReturnedProduct> returnedProducts = [.. _db.ReturnedProducts.Where(x => x.CreatedAt.Date == date.Date && x.Cart.RouteID == routeID)];
            List<SoldProductsTable> soldProducts = [];

            foreach (ProductType type in Enum.GetValues(typeof(ProductType)))
            {
                List<CartProduct> cartProductsByType = cartProducts.Where(x => x.Type == type).ToList();
                List<CartAbonoProduct> cartAbonoProductsByType = cartAbonoProducts.Where(x => x.Type == type).ToList();
                List<DispatchedProduct> dispatchedProductsByType = dispatchedProducts.Where(x => x.Type == type).ToList();
                List<ReturnedProduct> returnedProductsByType = returnedProducts.Where(x => x.Type == type).ToList();

                SoldProductsTable soldProduct = new()
                {
                    Name = type.GetDisplayName(),
                    Sold = cartProductsByType != null ? cartProductsByType.Sum(x => x.Quantity) : 0,
                    Dispatched = dispatchedProductsByType != null ? dispatchedProductsByType.Sum(x => x.Quantity) : 0,
                    Returned = returnedProductsByType != null ? returnedProductsByType.Sum(x => x.Quantity) : 0,
                };
                soldProduct.Sold += cartAbonoProductsByType != null ? cartAbonoProductsByType.Sum(x => x.Quantity) : 0;

                soldProducts.Add(soldProduct);
            }

            return soldProducts;
        }

        public List<SoldProductsTable> GetSoldProductsByRoute(long routeID)
        {
            try
            {
                List<CartProduct> cartProducts = [.. _db.CartProducts.Where(x => x.Cart.RouteID == routeID)];
                List<CartAbonoProduct> cartAbonoProducts = [.. _db.CartAbonoProducts.Where(x => x.Cart.RouteID == routeID)];
                List<DispatchedProduct> dispatchedProducts = [.. _db.DispatchedProducts.Where(x => x.RouteID == routeID)];
                List<ReturnedProduct> returnedProducts = [.. _db.ReturnedProducts.Where(x => x.Cart.RouteID == routeID)];
                List<ClientProduct> clientStock = [.. _db.Carts.Where(x => x.RouteID == routeID).Select(x => x.Client).SelectMany(x => x.Products).Include(x => x.Product)];
                List<SoldProductsTable> soldProducts = [];

                foreach (ProductType type in Enum.GetValues(typeof(ProductType)))
                {
                    List<CartProduct> cartProductsByType = cartProducts.Where(x => x.Type == type).ToList();
                    List<CartAbonoProduct> cartAbonoProductsByType = cartAbonoProducts.Where(x => x.Type == type).ToList();
                    List<DispatchedProduct> dispatchedProductsByType = dispatchedProducts.Where(x => x.Type == type).ToList();
                    List<ReturnedProduct> returnedProductsByType = returnedProducts.Where(x => x.Type == type).ToList();
                    List<ClientProduct> clientStockByType = clientStock.Where(x => x.Product.Type == type).ToList();

                    SoldProductsTable soldProduct = new()
                    {
                        Name = type.GetDisplayName(),
                        Sold = cartProductsByType != null ? cartProductsByType.Sum(x => x.Quantity) : 0,
                        Dispatched = dispatchedProductsByType != null ? dispatchedProductsByType.Sum(x => x.Quantity) : 0,
                        Returned = returnedProductsByType != null ? returnedProductsByType.Sum(x => x.Quantity) : 0,
                        ClientStock = clientStockByType != null ? clientStockByType.Sum(x => x.Stock) : 0,
                    };

                    soldProduct.Sold += cartAbonoProductsByType != null ? cartAbonoProductsByType.Sum(x => x.Quantity) : 0;

                    soldProducts.Add(soldProduct);
                }
                return soldProducts;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}