using Kite.AutoTrading.Common.Configurations;
using Kite.AutoTrading.Common.Helper;
using Kite.AutoTrading.Common.Models;
using Kite.AutoTrading.Common.Utility;
using Kite.AutoTrading.Data.DataServices;
using Kite.AutoTrading.Data.EF;
using KiteConnect;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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

        public IEnumerable<Candle> GetData(Symbol symbol, string period, DateTime fromDate, DateTime toDate, bool isContinous = false)
        {
            bool isRetry = true;
            while (isRetry)
            {
                try
                {
                    List<Historical> historical = _kite.GetHistoricalData(
                        InstrumentToken: symbol.InstrumentToken,
                        FromDate: fromDate,
                        ToDate: toDate,
                        Interval: period,
                        Continuous: isContinous
                    );
                    if (historical.Count > 0)
                    {
                        var candles = new List<Candle>();
                        for (int i = 0; i < historical.Count; i++)
                            candles.Add(new Candle(Convert.ToDateTime(historical[i].TimeStamp), historical[i].Open, historical[i].High, historical[i].Low, historical[i].Close, historical[i].Volume));
                        isRetry = false;
                        return candles;
                    }
                    else
                        isRetry = false;
                }
                catch (Exception ex)
                {
                    isRetry = true;
                    Thread.Sleep(100);
                }
            }
            return null;
        }

        public async Task<IEnumerable<Candle>> GetCachedDataAsync(Symbol symbol, string period, DateTime fromDate, DateTime toDate, bool isContinous = false)
        {
            var results = new List<Candle>();
            var cachedFilePath = GetFilePath(symbol);
            if (!File.Exists(cachedFilePath))
                results.AddRange(await SetCacheData(symbol, period, fromDate, toDate.AddDays(-1).EndOfDay()));
            else
                results.AddRange(await SerializerHelper.Deserialize<IEnumerable<Candle>>(cachedFilePath));

            //patch todays data
            var todaysData = GetData(symbol, period, toDate.StartOfDay(), toDate);
            if(todaysData!= null && todaysData.Count()>0)
                results.AddRange(todaysData);
            return results;
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

        private async Task<IEnumerable<Candle>> SetCacheData(Symbol symbol, string period, DateTime fromDate, DateTime toDate, bool isContinous = false)
        {
            var candles = GetData(symbol, period, fromDate, toDate);
            if (candles.Count() > 0)
            {
                await SerializerHelper.Serialize<IEnumerable<Candle>>(candles, GetFilePath(symbol));
                return candles;
            }
            return null;
        }

        private string GetFilePath(Symbol symbol)
        {
            string filepath = GlobalConfigurations.LogPath + "CachedData\\";

            if (!Directory.Exists(filepath))
                Directory.CreateDirectory(filepath);

            return filepath + symbol.TradingSymbol + ".bin";
        }
    }
}
