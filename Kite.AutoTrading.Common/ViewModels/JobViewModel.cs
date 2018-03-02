using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kite.AutoTrading.Common.ViewModels
{
    public class JobViewModel
    {
        public decimal MaxProfit { get; set; }
        public decimal MaxLoss { get; set; }
        public int WatchlistId { get; set; }
        public int UserSessionId { get; set; }
        public string Status { get; set; }
    }
}