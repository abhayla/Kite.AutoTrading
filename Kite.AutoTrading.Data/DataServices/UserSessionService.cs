using Kite.AutoTrading.Common.Models;
using Kite.AutoTrading.Data.EF;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace Kite.AutoTrading.Data.DataServices
{
    public class UserSessionService
    {
        private readonly KiteAutotradingEntities _context;
        public UserSessionService()
        {
            _context = new KiteAutotradingEntities();
        }
        public async Task<UserSession> GetCurrentSession()
        {
            return await _context.UserSessions.Where(x => x.CreatedDate >= DateTime.Today).FirstOrDefaultAsync();
        }

        public async Task<bool> SetCurrentSession(UserSessionModel userSession)
        {
            var existingSession = await _context.UserSessions.Where(x => x.CreatedDate >= DateTime.Today).FirstOrDefaultAsync();
            if (existingSession == null)
            {
                _context.UserSessions.Add(new UserSession()
                {
                    AccessToken = userSession.AccessToken,
                    ApiKey = userSession.ApiKey,
                    AppSecret = userSession.AppSecret,
                    CreatedDate = DateTime.Now,
                    PublicToken = userSession.PublicToken,
                    UserId = userSession.UserId
                });
            }
            else
            {
                existingSession.AccessToken = userSession.AccessToken;
                existingSession.ApiKey = userSession.ApiKey;
                existingSession.AppSecret = userSession.AppSecret;
                existingSession.CreatedDate = DateTime.Now;
                existingSession.PublicToken = userSession.PublicToken;
                existingSession.UserId = userSession.UserId;
            }
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
