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
using System.Text;
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
            int isRetryCount = 5;
            while (isRetryCount > 0)
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
                    if (historical != null && historical.Count > 0)
                    {
                        var candles = new List<Candle>();
                        for (int i = 0; i < historical.Count; i++)
                            candles.Add(new Candle(Convert.ToDateTime(historical[i].TimeStamp), 
                                historical[i].Open, historical[i].High, historical[i].Low, historical[i].Close, historical[i].Volume));
                        return candles;
                    }
                    isRetryCount -= 1;
                }
                catch (Exception ex)
                {
                    isRetryCount -= 1;
                    Thread.Sleep(100);

                    ApplicationLogger.LogException("Exception Occured in GetData() for Sybmol : " + JsonConvert.SerializeObject(symbol) + Environment.NewLine +
                    "FromDate : " + fromDate.ToString() + Environment.NewLine +
                    "ToDate : " + toDate.ToString() + Environment.NewLine +
                    "Exception : " + JsonConvert.SerializeObject(ex));                    
                }
            }
            return null;
        }

        public async Task<IEnumerable<Candle>> GetCachedDataAsync(Symbol symbol, string period, DateTime fromDate, DateTime toDate, bool isContinous = false, bool isDevelopment=false)
        {
            var results = new List<Candle>();
            var cachedFilePath = GetFilePath(symbol, toDate.AddDays(-1).EndOfDay(), period);
            if (!File.Exists(cachedFilePath))
            {
                var cachedData = await SetCacheData(symbol, period, fromDate, toDate.AddDays(-1).EndOfDay(), cachedFilePath);
                if(cachedData!=null )
                    results.AddRange(cachedData);
            }
            else
                results.AddRange(await SerializerHelper.Deserialize<IEnumerable<Candle>>(cachedFilePath));

            if (results.Count > 0)
            {
                //patch todays data
                var todaysData = GetData(symbol, period, toDate.StartOfDay(), toDate);
                if (todaysData != null && todaysData.Count() > 0)
                {
                    results.AddRange(todaysData);
                    return results;
                }
                else if (isDevelopment)
                    return results;
            }

            
            return null;
        }

        public string PlaceOrder(BrokerOrderModel brokerOrderModel)
        {
            Dictionary<string, dynamic> response = null;
            try
            {                 
                if (brokerOrderModel.Variety == Constants.VARIETY_CO)
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
                else if (brokerOrderModel.Variety == Constants.VARIETY_BO)
                    response = this._kite.PlaceOrder(
                        Exchange: brokerOrderModel.Exchange,
                        TradingSymbol: brokerOrderModel.TradingSymbol,
                        TransactionType: brokerOrderModel.TransactionType,
                        Quantity: brokerOrderModel.Quantity,
                        Price: GetRoundToTick(brokerOrderModel.Price.Value, brokerOrderModel.TickSize.Value),
                        Product: brokerOrderModel.Product,
                        OrderType: brokerOrderModel.OrderType,
                        Validity: brokerOrderModel.Validity,
                        Variety: brokerOrderModel.Variety,
                        TriggerPrice: GetRoundToTick(brokerOrderModel.TriggerPrice.Value, brokerOrderModel.TickSize.Value),
                        SquareOffValue: GetRoundToTick(brokerOrderModel.SquareOffValue.Value, brokerOrderModel.TickSize.Value),
                        StoplossValue: GetRoundToTick(brokerOrderModel.StoplossValue.Value, brokerOrderModel.TickSize.Value),
                        TrailingStoploss: GetRoundToTick(brokerOrderModel.TrailingStoploss.Value, brokerOrderModel.TickSize.Value)
                    );
                return Convert.ToString(response["data"]["order_id"]);
            }
            finally
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("-----------------------------------------------------------------------------------" + Environment.NewLine);
                sb.Append(brokerOrderModel.TransactionType + " Order is Placed at " + GlobalConfigurations.IndianTime.ToString() + Environment.NewLine);
                sb.Append("- Order Request " + JsonConvert.SerializeObject(brokerOrderModel) + Environment.NewLine);
                sb.Append("- Order Response " + JsonConvert.SerializeObject(response) + Environment.NewLine);
                ApplicationLogger.LogJob(brokerOrderModel.JobId, sb.ToString());
            }
        }


        #region PRIVATE_METHODS        

        private async Task<IEnumerable<Candle>> SetCacheData(Symbol symbol, string period, DateTime fromDate, DateTime toDate, string cachedFilePath, bool isContinous = false)
        {
            var candles = GetData(symbol, period, fromDate, toDate);
            if (candles != null && candles.Count() > 0)
            {
                await SerializerHelper.Serialize<IEnumerable<Candle>>(candles, cachedFilePath);
                return candles;
            }
            return null;
        }

        private string GetFilePath(Symbol symbol, DateTime toDate, string period)
        {
            return string.Format("{0}{1}-{2}-{3}.bin",
                GlobalConfigurations.CachedDataPath,
                symbol.TradingSymbol,
                toDate.ToString("yyyy-dd-M"),
                period);
        }

        private decimal GetRoundToTick(decimal price, decimal tickSize)
        {
            var tickSizeDouble = Convert.ToDouble(tickSize);
            var priceDouble = Math.Round(Convert.ToDouble(price),2);

            return Convert.ToDecimal(priceDouble - priceDouble % tickSizeDouble + ((priceDouble % tickSizeDouble < tickSizeDouble / 2) ? 0.0 : tickSizeDouble));
        }

        #endregion
    }
}
