using AutoMapper;
using Kite.AutoTrading.Common.Models;
using Kite.AutoTrading.Data.EF;
using KiteConnect;
using Trady.Core;

namespace Kite.AutoTrading.App_Start
{
    public class AutoMapperProfile : AutoMapper.Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<UserSession, UserSessionModel>();
            CreateMap<BrokerOrderModel, BrokerOrder>();
            CreateMap<Historical, Candle>()
                .ForMember(dest => dest.DateTime, opt => opt.MapFrom(src => src.TimeStamp));
        }
    }
}