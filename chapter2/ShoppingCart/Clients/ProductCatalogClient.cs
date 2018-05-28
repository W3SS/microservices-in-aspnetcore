﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Polly;
using ShoppingCart.Domains;

namespace ShoppingCart.Clients
{
    public class ProductCatalogClient : IProductCatalogClient
    {
        // URL for the fake Product Catalog microservice
        private static readonly string productCatalogBaseUrl =
            @"http://private-05cc8-chapter2productcataloguemicroservice.apiary-mock.com";

        private static readonly string getProductPathTemplate = "/products?productIds=[{0}]";

        // Use Polly's fluent API to set up a retry policy with an exponential back-off
        private static readonly Policy exponentialRetryPolicy = Policy.Handle<Exception>()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)));

        public Task<IEnumerable<ShoppingCartItem>> GetShoppingCartItems(int[] productCatalogIds) =>
            // Wraps calls to the Product Catalog microservice in the retry policy
            exponentialRetryPolicy.ExecuteAsync(
                async () => await GetItemsFromCatalogService(productCatalogIds).ConfigureAwait(false)
            );

        private async Task<IEnumerable<ShoppingCartItem>> GetItemsFromCatalogService(int[] productCatalogIds)
        {
            var response = await RequestProductFromProductCatalog(productCatalogIds).ConfigureAwait(false);
            return await ConvertToShoppingCartItems(response).ConfigureAwait(false);
        }

        private static async Task<HttpResponseMessage> RequestProductFromProductCatalog(int[] productCatalogIds)
        {
            // Adds the product IDs as a query string parameter to the path of the /products endpoint
            var productsResource = string.Format(getProductPathTemplate, string.Join(",", productCatalogIds));

            // Creates a client for making the HTTP GET request
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(productCatalogBaseUrl);

                // Tells HttpClient to perform HTTP GET asynchronously
                return await httpClient.GetAsync(productsResource).ConfigureAwait(false);
            }
        }

        private static async Task<IEnumerable<ShoppingCartItem>> ConvertToShoppingCartItems(
            HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();

            // Uses Json.NET to deserialize the JSON from the Product Catalog microservice
            var products =
                JsonConvert.DeserializeObject<List<ProductCatalogProduct>>(
                    await response.Content.ReadAsStringAsync()
                        .ConfigureAwait(false)
                );

            // Creates a ShoppingCartItem for each product in the response
            return products.Select(
                p => new ShoppingCartItem(int.Parse(p.ProductId), p.ProductName, p.ProductDescription, p.Price));
        }

        // Use a private class to represent the product data
        private class ProductCatalogProduct
        {
            public string ProductId { get; set; }
            public string ProductName { get; set; }
            public string ProductDescription { get; set; }
            public Money Price { get; set; }
        }
    }
}
