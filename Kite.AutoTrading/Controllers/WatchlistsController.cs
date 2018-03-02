using Hangfire;
using Kite.AutoTrading.Data.EF;
using System;
using System.Data.Entity;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Kite.AutoTrading.Controllers
{
    public class WatchlistsController : Controller
    {
        private KiteAutotradingEntities db = new KiteAutotradingEntities();

        // GET: Watchlists
        public async Task<ActionResult> Index()
        {
            return View(await db.Watchlists.ToListAsync());
        }

        // GET: Watchlists/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Watchlist watchlist = await db.Watchlists.FindAsync(id);
            if (watchlist == null)
            {
                return HttpNotFound();
            }
            return View(watchlist);
        }

        // GET: Watchlists/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Watchlists/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Id,Name")] Watchlist watchlist)
        {
            if (ModelState.IsValid)
            {
                watchlist.CreatedDate = DateTime.Now;
                db.Watchlists.Add(watchlist);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            return View(watchlist);
        }

        // GET: Watchlists/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Watchlist watchlist = await db.Watchlists.FindAsync(id);
            if (watchlist == null)
            {
                return HttpNotFound();
            }
            return View(watchlist);
        }

        // POST: Watchlists/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,Name")] Watchlist watchlist)
        {
            if (ModelState.IsValid)
            {
                watchlist.ModifiedDate = DateTime.Now;
                db.Entry(watchlist).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(watchlist);
        }

        // GET: Watchlists/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Watchlist watchlist = await db.Watchlists.FindAsync(id);
            if (watchlist == null)
            {
                return HttpNotFound();
            }
            return View(watchlist);
        }

        // POST: Watchlists/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Watchlist watchlist = await db.Watchlists.FindAsync(id);
            db.Watchlists.Remove(watchlist);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
