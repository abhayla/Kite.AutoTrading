namespace Kite.AutoTrading.Common.Models
{
    public class BrokerOrderModel
    {
        public string TradingSymbol { get; set; }
        public string InstrumentToken { get; set; }
        public string Exchange { get; set; }
        public string TransactionType { get; set; }
        public int Quantity { get; set; }
        public decimal? Price { get; set; }
        public string Product { get; set; }
        public string OrderType { get; set; }
        public string Validity { get; set; }
        public int? DisclosedQuantity { get; set; }
        public decimal? TriggerPrice { get; set; }
        public decimal? SquareOffValue { get; set; }
        public decimal? StoplossValue { get; set; }
        public decimal? TrailingStoploss { get; set; }
        public string Variety { get; set; }
        public string Tag { get; set; }

        public string OrderStatus { get; set; }
        public string BrokerOrderId { get; set; }
        public int SymbolId { get; set; }
        public int JobId { get; set; }
    }
}