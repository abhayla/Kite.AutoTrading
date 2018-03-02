using Kite.AutoTrading.Common.Models;
using Kite.AutoTrading.Data.EF;
using KiteConnect;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace Kite.AutoTrading.Data.DataServices
{
    public class BrokerOrderService
    {
        private readonly KiteAutotradingEntities _context;
        public BrokerOrderService()
        {
            _context = new KiteAutotradingEntities();
        }

        public async Task<BrokerOrder> GetBrokerOrder(string brokerId)
        {
            return await _context.BrokerOrders.Where(x => x.BrokerOrderId == brokerId).FirstOrDefaultAsync();
        }

        public async Task<BrokerOrder> GetBrokerOrder(int id)
        {
            return await _context.BrokerOrders.FindAsync(id);
        }

        public BrokerOrder Create(BrokerOrderModel brokerOrderModel)
        {
            var brokerOrder = AutoMapper.Mapper.Map<BrokerOrder>(brokerOrderModel);

            if (brokerOrder != null)
            {
                brokerOrder.CreatedDate = DateTime.Now;
                _context.BrokerOrders.Add(brokerOrder);
                _context.SaveChanges();
            }
            return brokerOrder;
        }

        public async Task<bool> SyncOrders(IList<Order> orders)
        {
            if (orders != null && orders.Count() > 0)
            {
                foreach (var order in orders)
                {
                    _context.BrokerOrders.Add(new BrokerOrder()
                    {
                        BrokerOrderId = order.OrderId,
                        CreatedDate = order.ExchangeTimestamp,
                        DisclosedQuantity = order.DisclosedQuantity,
                        JobId = Convert.ToInt32(order.Tag),
                        OrderStatus = order.Status,
                        OrderType = order.OrderType,
                        Price = order.Price,
                        Tag = order.Tag,
                        Product = order.Product,
                        Quantity = order.Quantity,
                        TradingSymbol = order.Tradingsymbol,
                        TriggerPrice = order.TriggerPrice,
                        TransactionType = order.TransactionType,
                        Validity = order.Validity,
                        Variety = order.Variety
                    });
                }
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
    }
}
