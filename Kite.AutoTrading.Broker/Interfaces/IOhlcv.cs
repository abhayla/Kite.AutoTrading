using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trady.Core.Infrastructure;

namespace Kite.AutoTrading.Broker.Interfaces
{
    public class IOhlcv : ITick
    {
        
        DateTime ITick.DateTime { get; }

        decimal Open { get; }

        decimal High { get; }

        decimal Low { get; }

        decimal Close { get; }

        decimal Volume { get; }
    }
}
