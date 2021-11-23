﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Data;
using Nop.Services.Shipping.Pickup;
using Nop.Services.Shipping.Tracking;

namespace Nop.Services.Shipping
{
    /// <summary>
    /// Shipment service
    /// </summary>
    public partial class ShipmentService : IShipmentService
    {
        #region Fields

        protected IPickupPluginManager PickupPluginManager { get; }
        protected IRepository<Address> AddressRepository { get; }
        protected IRepository<Order> OrderRepository { get; }
        protected IRepository<OrderItem> OrderItemRepository { get; }
        protected IRepository<Product> ProductRepository { get; }
        protected IRepository<Shipment> ShipmentRepository { get; }
        protected IRepository<ShipmentItem> ShipmentItemRepository { get; }
        protected IShippingPluginManager ShippingPluginManager { get; }

        #endregion

        #region Ctor

        public ShipmentService(IPickupPluginManager pickupPluginManager,
            IRepository<Address> addressRepository,
            IRepository<Order> orderRepository,
            IRepository<OrderItem> orderItemRepository,
            IRepository<Product> productRepository,
            IRepository<Shipment> shipmentRepository,
            IRepository<ShipmentItem> siRepository,
            IShippingPluginManager shippingPluginManager)
        {
            PickupPluginManager = pickupPluginManager;
            AddressRepository = addressRepository;
            OrderRepository = orderRepository;
            OrderItemRepository = orderItemRepository;
            ProductRepository = productRepository;
            ShipmentRepository = shipmentRepository;
            ShipmentItemRepository = siRepository;
            ShippingPluginManager = shippingPluginManager;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Deletes a shipment
        /// </summary>
        /// <param name="shipment">Shipment</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public virtual async Task DeleteShipmentAsync(Shipment shipment)
        {
            await ShipmentRepository.DeleteAsync(shipment);
        }

        /// <summary>
        /// Search shipments
        /// </summary>
        /// <param name="vendorId">Vendor identifier; 0 to load all records</param>
        /// <param name="warehouseId">Warehouse identifier, only shipments with products from a specified warehouse will be loaded; 0 to load all orders</param>
        /// <param name="shippingCountryId">Shipping country identifier; 0 to load all records</param>
        /// <param name="shippingStateId">Shipping state identifier; 0 to load all records</param>
        /// <param name="shippingCounty">Shipping county; null to load all records</param>
        /// <param name="shippingCity">Shipping city; null to load all records</param>
        /// <param name="trackingNumber">Search by tracking number</param>
        /// <param name="loadNotShipped">A value indicating whether we should load only not shipped shipments</param>
        /// <param name="loadNotDelivered">A value indicating whether we should load only not delivered shipments</param>
        /// <param name="orderId">Order identifier; 0 to load all records</param>
        /// <param name="createdFromUtc">Created date from (UTC); null to load all records</param>
        /// <param name="createdToUtc">Created date to (UTC); null to load all records</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipments
        /// </returns>
        public virtual async Task<IPagedList<Shipment>> GetAllShipmentsAsync(int vendorId = 0, int warehouseId = 0,
            int shippingCountryId = 0,
            int shippingStateId = 0,
            string shippingCounty = null,
            string shippingCity = null,
            string trackingNumber = null,
            bool loadNotShipped = false,
            bool loadNotDelivered = false,
            int orderId = 0,
            DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
            int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var shipments = await ShipmentRepository.GetAllPagedAsync(query =>
            {
                if (orderId > 0)
                    query = query.Where(o => o.OrderId == orderId);

                if (!string.IsNullOrEmpty(trackingNumber))
                    query = query.Where(s => s.TrackingNumber.Contains(trackingNumber));

                if (shippingCountryId > 0)
                    query = from s in query
                        join o in OrderRepository.Table on s.OrderId equals o.Id
                        where AddressRepository.Table.Any(a =>
                            a.Id == (o.PickupInStore ? o.PickupAddressId : o.ShippingAddressId) &&
                            a.CountryId == shippingCountryId)
                        select s;

                if (shippingStateId > 0)
                    query = from s in query
                        join o in OrderRepository.Table on s.OrderId equals o.Id
                        where AddressRepository.Table.Any(a =>
                            a.Id == (o.PickupInStore ? o.PickupAddressId : o.ShippingAddressId) &&
                            a.StateProvinceId == shippingStateId)
                        select s;

                if (!string.IsNullOrWhiteSpace(shippingCounty))
                    query = from s in query
                        join o in OrderRepository.Table on s.OrderId equals o.Id
                        where AddressRepository.Table.Any(a =>
                            a.Id == (o.PickupInStore ? o.PickupAddressId : o.ShippingAddressId) &&
                            a.County.Contains(shippingCounty))
                        select s;

                if (!string.IsNullOrWhiteSpace(shippingCity))
                    query = from s in query
                        join o in OrderRepository.Table on s.OrderId equals o.Id
                        where AddressRepository.Table.Any(a =>
                            a.Id == (o.PickupInStore ? o.PickupAddressId : o.ShippingAddressId) &&
                            a.City.Contains(shippingCity))
                        select s;

                if (loadNotShipped)
                    query = query.Where(s => !s.ShippedDateUtc.HasValue);

                if (loadNotDelivered)
                    query = query.Where(s => !s.DeliveryDateUtc.HasValue);

                if (createdFromUtc.HasValue)
                    query = query.Where(s => createdFromUtc.Value <= s.CreatedOnUtc);

                if (createdToUtc.HasValue)
                    query = query.Where(s => createdToUtc.Value >= s.CreatedOnUtc);

                query = from s in query
                    join o in OrderRepository.Table on s.OrderId equals o.Id
                    where !o.Deleted
                    select s;

                query = query.Distinct();

                if (vendorId > 0)
                {
                    var queryVendorOrderItems = from orderItem in OrderItemRepository.Table
                        join p in ProductRepository.Table on orderItem.ProductId equals p.Id
                        where p.VendorId == vendorId
                        select orderItem.Id;

                    query = from s in query
                        join si in ShipmentItemRepository.Table on s.Id equals si.ShipmentId
                        where queryVendorOrderItems.Contains(si.OrderItemId)
                        select s;

                    query = query.Distinct();
                }

                if (warehouseId > 0)
                {
                    query = from s in query
                        join si in ShipmentItemRepository.Table on s.Id equals si.ShipmentId
                        where si.WarehouseId == warehouseId
                        select s;

                    query = query.Distinct();
                }

                query = query.OrderByDescending(s => s.CreatedOnUtc);

                return query;
            }, pageIndex, pageSize);

            return shipments;
        }

        /// <summary>
        /// Get shipment by identifiers
        /// </summary>
        /// <param name="shipmentIds">Shipment identifiers</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipments
        /// </returns>
        public virtual async Task<IList<Shipment>> GetShipmentsByIdsAsync(int[] shipmentIds)
        {
            return await ShipmentRepository.GetByIdsAsync(shipmentIds);
        }

        /// <summary>
        /// Gets a shipment
        /// </summary>
        /// <param name="shipmentId">Shipment identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipment
        /// </returns>
        public virtual async Task<Shipment> GetShipmentByIdAsync(int shipmentId)
        {
            return await ShipmentRepository.GetByIdAsync(shipmentId);
        }

        /// <summary>
        /// Gets a list of order shipments
        /// </summary>
        /// <param name="orderId">Order identifier</param>
        /// <param name="shipped">A value indicating whether to count only shipped or not shipped shipments; pass null to ignore</param>
        /// <param name="vendorId">Vendor identifier; pass 0 to ignore</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result
        /// </returns>
        public virtual async Task<IList<Shipment>> GetShipmentsByOrderIdAsync(int orderId, bool? shipped = null, int vendorId = 0)
        {
            if (orderId == 0)
                return new List<Shipment>();

            var shipments = ShipmentRepository.Table;

            if (shipped.HasValue) 
                shipments = shipments.Where(s => s.ShippedDateUtc.HasValue == shipped);

            return await shipments.Where(shipment => shipment.OrderId == orderId).ToListAsync();
        }

        /// <summary>
        /// Inserts a shipment
        /// </summary>
        /// <param name="shipment">Shipment</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public virtual async Task InsertShipmentAsync(Shipment shipment)
        {
            await ShipmentRepository.InsertAsync(shipment);
        }

        /// <summary>
        /// Updates the shipment
        /// </summary>
        /// <param name="shipment">Shipment</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public virtual async Task UpdateShipmentAsync(Shipment shipment)
        {
            await ShipmentRepository.UpdateAsync(shipment);
        }
        
        /// <summary>
        /// Gets a shipment items of shipment
        /// </summary>
        /// <param name="shipmentId">Shipment identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipment items
        /// </returns>
        public virtual async Task<IList<ShipmentItem>> GetShipmentItemsByShipmentIdAsync(int shipmentId)
        {
            if (shipmentId == 0)
                return null;

            return await ShipmentItemRepository.Table.Where(si => si.ShipmentId == shipmentId).ToListAsync();
        }

        /// <summary>
        /// Inserts a shipment item
        /// </summary>
        /// <param name="shipmentItem">Shipment item</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public virtual async Task InsertShipmentItemAsync(ShipmentItem shipmentItem)
        {
            await ShipmentItemRepository.InsertAsync(shipmentItem);
        }

        /// <summary>
        /// Deletes a shipment item
        /// </summary>
        /// <param name="shipmentItem">Shipment Item</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public virtual async Task DeleteShipmentItemAsync(ShipmentItem shipmentItem)
        {
            await ShipmentItemRepository.DeleteAsync(shipmentItem);
        }

        /// <summary>
        /// Updates a shipment item
        /// </summary>
        /// <param name="shipmentItem">Shipment item</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public virtual async Task UpdateShipmentItemAsync(ShipmentItem shipmentItem)
        {
            await ShipmentItemRepository.UpdateAsync(shipmentItem);
        }

        /// <summary>
        /// Gets a shipment item
        /// </summary>
        /// <param name="shipmentItemId">Shipment item identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipment item
        /// </returns>
        public virtual async Task<ShipmentItem> GetShipmentItemByIdAsync(int shipmentItemId)
        {
            return await ShipmentItemRepository.GetByIdAsync(shipmentItemId);
        }

        /// <summary>
        /// Get quantity in shipments. For example, get planned quantity to be shipped
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="warehouseId">Warehouse identifier</param>
        /// <param name="ignoreShipped">Ignore already shipped shipments</param>
        /// <param name="ignoreDelivered">Ignore already delivered shipments</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the quantity
        /// </returns>
        public virtual async Task<int> GetQuantityInShipmentsAsync(Product product, int warehouseId,
            bool ignoreShipped, bool ignoreDelivered)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            //only products with "use multiple warehouses" are handled this way
            if (product.ManageInventoryMethod != ManageInventoryMethod.ManageStock)
                return 0;
            if (!product.UseMultipleWarehouses)
                return 0;

            const int cancelledOrderStatusId = (int)OrderStatus.Cancelled;

            var query = ShipmentItemRepository.Table;

            query = from si in query
                join s in ShipmentRepository.Table on si.ShipmentId equals s.Id
                join o in OrderRepository.Table on s.OrderId equals o.Id
                where !o.Deleted && o.OrderStatusId != cancelledOrderStatusId
                    select si;

            query = query.Distinct();

            if (warehouseId > 0)
                query = query.Where(si => si.WarehouseId == warehouseId);
            if (ignoreShipped)
            {
                query = from si in query
                    join s in ShipmentRepository.Table on si.ShipmentId equals s.Id
                    where !s.ShippedDateUtc.HasValue
                    select si;
            }

            if (ignoreDelivered)
            {
                query = from si in query
                    join s in ShipmentRepository.Table on si.ShipmentId equals s.Id
                    where !s.DeliveryDateUtc.HasValue
                    select si;
            }

            var queryProductOrderItems = from orderItem in OrderItemRepository.Table
                                         where orderItem.ProductId == product.Id
                                         select orderItem.Id;
            query = from si in query
                    where queryProductOrderItems.Any(orderItemId => orderItemId == si.OrderItemId)
                    select si;

            //some null validation
            var result = Convert.ToInt32(await query.SumAsync(si => (int?)si.Quantity));
            return result;
        }

        /// <summary>
        /// Get the tracker of the shipment
        /// </summary>
        /// <param name="shipment">Shipment</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipment tracker
        /// </returns>
        public virtual async Task<IShipmentTracker> GetShipmentTrackerAsync(Shipment shipment)
        {
            var order = await OrderRepository.GetByIdAsync(shipment.OrderId, cache => default);

            if (!order.PickupInStore)
            {
                var shippingRateComputationMethod = await ShippingPluginManager
                    .LoadPluginBySystemNameAsync(order.ShippingRateComputationMethodSystemName);

                return await shippingRateComputationMethod?.GetShipmentTrackerAsync();
            }

            var pickupPointProvider = await PickupPluginManager
                .LoadPluginBySystemNameAsync(order.ShippingRateComputationMethodSystemName);
            return await pickupPointProvider?.GetShipmentTrackerAsync();
        }

        #endregion
    }
}