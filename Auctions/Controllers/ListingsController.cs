using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Auctions.Data;
using Auctions.Models;
using Auctions.Data.Services;
using System.Security.Claims;

namespace Auctions.Controllers
{
    public class ListingsController : Controller
    {
        private readonly IListingsService _listingsService;
        private readonly IBidsService _bidsService;
        private readonly ICommentsService _commentsService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ListingsController(IListingsService listingService, IWebHostEnvironment webHostEnvironment, IBidsService bidsService, 
            ICommentsService commentsService)
        {
            _listingsService = listingService;
            _bidsService = bidsService;
            _commentsService = commentsService;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Listings
        public async Task<IActionResult> Index(int? pageNumber, string searchString)
        {
            var applicationDbContext = _listingsService.GetAll();
            var pageSize = 1;
            if (!string.IsNullOrEmpty(searchString))
            {
                applicationDbContext = applicationDbContext.Where(x => x.Title.Contains(searchString));
            }

            return View(await PaginatedList<Listing>.CreateAsync(applicationDbContext.Where(x => x.IsSold == false).AsNoTracking(), 
                pageNumber ?? 1, 
                pageSize));
        }

        public async Task<IActionResult> MyListings(int? pageNumber)
        {
            var applicationDbContext = _listingsService.GetAll();
            var pageSize = 3;
            
            return View("Index", 
                await PaginatedList<Listing>.CreateAsync(applicationDbContext
                    .Where(x => x.IdentityUserId == User.FindFirstValue(ClaimTypes.NameIdentifier)).AsNoTracking(),
                pageNumber ?? 1,
                pageSize));
        }

        //// GET: Listings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var listing = await _listingsService.GetById(id);
            if (listing == null)
            {
                return NotFound();
            }

            return View(listing);
        }

        // GET: Listings/Create
        public IActionResult Create()
        {

            return View();
        }

        // POST: Listings/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ListingVM listing)
        {
            if (listing.Image is not null)
            {
                var uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "img");
                var fileName = listing.Image.FileName;
                var filePath = Path.Combine(uploadDir, fileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    listing.Image.CopyTo(fileStream);
                }

                var listObj = new Listing
                {
                    Title = listing.Title,
                    Description = listing.Description,
                    Price = listing.Price,
                    IdentityUserId = listing.IdentityUserId,
                    ImagePath = fileName
                };

                await _listingsService.Add(listObj);

                return RedirectToAction("Index");
            }

            return View(listing);
        }

        [HttpPost]
        public async Task<ActionResult> AddBid([Bind("Id, Price, ListingId, IdentityUserId")] Bid bid)
        {
            if(ModelState.IsValid)
            {
                await _bidsService.Add(bid);
            }
            var listing = await _listingsService.GetById(bid.ListingId);
            listing.Price = bid.Price;
            await _listingsService.SaveChanges();

            return View("Details", listing);
        }

        public async Task<ActionResult> CloseBidding(int id)
        {
            var listing = await _listingsService.GetById(id);
            listing.IsSold = true;
            await _listingsService.SaveChanges();

            return View("Details", listing);
        }

        [HttpPost]
        public async Task<ActionResult> AddComment([Bind("Id, Content, ListingId, IdentityUserId")] Comment comment)
        {
            if (ModelState.IsValid)
            {
                await _commentsService.Add(comment);

            }

            var listing = await _listingsService.GetById(comment.ListingId);
            return View("Details", listing);
        }

        //// GET: Listings/Edit/5
        //public async Task<IActionResult> Edit(int? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var listing = await _listingsService.Listings.FindAsync(id);
        //    if (listing == null)
        //    {
        //        return NotFound();
        //    }
        //    ViewData["IdentityUserId"] = new SelectList(_listingsService.Users, "Id", "Id", listing.IdentityUserId);
        //    return View(listing);
        //}

        //// POST: Listings/Edit/5
        //// To protect from overposting attacks, enable the specific properties you want to bind to.
        //// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Price,ImagePath,IsSold,IdentityUserId")] Listing listing)
        //{
        //    if (id != listing.Id)
        //    {
        //        return NotFound();
        //    }

        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            _listingsService.Update(listing);
        //            await _listingsService.SaveChangesAsync();
        //        }
        //        catch (DbUpdateConcurrencyException)
        //        {
        //            if (!ListingExists(listing.Id))
        //            {
        //                return NotFound();
        //            }
        //            else
        //            {
        //                throw;
        //            }
        //        }
        //        return RedirectToAction(nameof(Index));
        //    }
        //    ViewData["IdentityUserId"] = new SelectList(_listingsService.Users, "Id", "Id", listing.IdentityUserId);
        //    return View(listing);
        //}

        //// GET: Listings/Delete/5
        //public async Task<IActionResult> Delete(int? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var listing = await _listingsService.Listings
        //        .Include(l => l.User)
        //        .FirstOrDefaultAsync(m => m.Id == id);
        //    if (listing == null)
        //    {
        //        return NotFound();
        //    }

        //    return View(listing);
        //}

        //// POST: Listings/Delete/5
        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> DeleteConfirmed(int id)
        //{
        //    var listing = await _listingsService.Listings.FindAsync(id);
        //    if (listing != null)
        //    {
        //        _listingsService.Listings.Remove(listing);
        //    }

        //    await _listingsService.SaveChangesAsync();
        //    return RedirectToAction(nameof(Index));
        //}

        //private bool ListingExists(int id)
        //{
        //    return _listingsService.Listings.Any(e => e.Id == id);
        //}
    }
}
