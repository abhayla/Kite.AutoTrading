using Kite.AutoTrading.Common.Helper;
using Kite.AutoTrading.Common.Models;
using Kite.AutoTrading.Data.DataServices;
using Kite.AutoTrading.Data.EF;
using KiteConnect;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Trady.Core;

namespace Kite.AutoTrading.Business.Brokers
{
    public class ZerodhaBroker
    {

        public KiteConnect.Kite _kite;
        public readonly UserSessionModel _userSession;
        public readonly BrokerOrderService _brokerOrderService;
        public ZerodhaBroker(UserSessionModel userSession)
        {
            _kite = new KiteConnect.Kite(userSession.ApiKey, userSession.AccessToken);
            _userSession = userSession;
            _brokerOrderService = new BrokerOrderService();
        }
        public IEnumerable<Candle> GetData(Symbol symbol, int noOfDays, string period)
        {
            List<Historical> historical = _kite.GetHistoricalData(
                InstrumentToken: symbol.InstrumentToken,
                FromDate: DateTime.Now.AddDays(noOfDays),
                ToDate: DateTime.Now,
                Interval: period,
                Continuous: false
            );

            if (historical.Count > 0)
            {
                var candles = new List<Candle>();
                for (int i = 0; i < historical.Count; i++)
                    candles.Add(new Candle(Convert.ToDateTime(historical[i].TimeStamp), historical[i].Open, historical[i].High, historical[i].Low, historical[i].Close, historical[i].Volume));
                return candles;
            }
            return null;
        }
        public string PlaceOrder(BrokerOrderModel brokerOrderModel)
        {
            Dictionary<string, dynamic> response = null;
            response = this._kite.PlaceOrder(
                    Exchange: brokerOrderModel.Exchange,
                    TradingSymbol: brokerOrderModel.TradingSymbol,
                    TransactionType: brokerOrderModel.TransactionType,
                    Quantity: brokerOrderModel.Quantity,
                    Price: brokerOrderModel.Price,
                    OrderType: brokerOrderModel.OrderType,
                    Product: brokerOrderModel.Product,
                    Variety: brokerOrderModel.Variety,
                    Validity: brokerOrderModel.Validity,
                    TriggerPrice: brokerOrderModel.TriggerPrice,
                    Tag: brokerOrderModel.JobId.ToString()
                );


            //if (!string.IsNullOrWhiteSpace(orderId))
            //{
            //    brokerOrderModel.BrokerOrderId = orderId;
            //    brokerOrderModel.Tag = brokerOrderModel.JobId.ToString();
            //    brokerOrderModel.OrderStatus = Convert.ToString(response["status"]);
            //    //Log Into DB
            //    _brokerOrderService.Create(brokerOrderModel);

            //Log Order Information into LogFile
            ApplicationLogger.LogJob(brokerOrderModel.JobId, brokerOrderModel.TransactionType + " Order is Placed at " + DateTime.Now.ToString());
            ApplicationLogger.LogJob(brokerOrderModel.JobId, "- Order Input " + JsonConvert.SerializeObject(brokerOrderModel));
            ApplicationLogger.LogJob(brokerOrderModel.JobId, "- Order Response " + JsonConvert.SerializeObject(response));
            return Convert.ToString(response["data"]["order_id"]);
        }
    }
}
