using AutoMapper;
using Kite.AutoTrading.Common.Models;
using Kite.AutoTrading.Data.EF;

namespace Kite.AutoTrading.App_Start
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<UserSession, UserSessionModel>();
            CreateMap<BrokerOrderModel, BrokerOrder>();
        }
    }
}