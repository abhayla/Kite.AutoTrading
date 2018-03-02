using Kite.AutoTrading.Business.Brokers;
using Kite.AutoTrading.Common.Models;
using Kite.AutoTrading.Data.DataServices;
using KiteConnect;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kite.AutoTrading.StrategyManager.Strategy
{
    public class MasterStrategy
    {

        private readonly BrokerOrderService _brokerOrderService;
        private readonly BrokerPositionService _brokerPositionService;
        private readonly UserSessionService _userSessionService;
        private ZerodhaBroker _zeropdhaService;
        public MasterStrategy()
        {
            _brokerOrderService = new BrokerOrderService();
            _brokerPositionService = new BrokerPositionService();
            _userSessionService = new UserSessionService();
        }
        private async Task Start()
        {
            var userSession = await _userSessionService.GetCurrentSession();
            _zeropdhaService = new ZerodhaBroker(AutoMapper.Mapper.Map<UserSessionModel>(userSession));
            if (IsMarketClosed())
            {
                //Copy All Orders to DB
                var orders = _zeropdhaService._kite.GetOrders();
                if (orders != null)
                    await _brokerOrderService.SyncOrders(orders);
                //Copy All Day Positions
                var positions = _zeropdhaService._kite.GetPositions();
                if (positions.Day != null)
                    await _brokerPositionService.SyncPositions(positions.Day);

                //Write code for following tasks
                //Kill Jobs
                //Shutdown VM
            }
        }
        private bool IsMarketClosed()
        {
            //check indian standard time
            TimeZoneInfo INDIAN_ZONE = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            var indianTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, INDIAN_ZONE).TimeOfDay;
            var start = new TimeSpan(10, 0, 0); //10 o'clock
            var end = new TimeSpan(15, 30, 0); //12 o'clock
            if ((indianTime > start) && (indianTime < end))
                return false;
            else
                return true;
        }
    }
}
