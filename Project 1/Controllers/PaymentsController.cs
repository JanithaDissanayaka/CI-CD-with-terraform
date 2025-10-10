using Microsoft.AspNetCore.Mvc;
using Project_1.Data;
using Project_1.Models;
using System.Threading.Tasks;

namespace Project_1.Controllers
{
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Payments/Confirm
        public async Task<IActionResult> Confirm(int listingId, string orderId)
        {
            // Here, you can handle the payment confirmation.
            // For example, mark the listing as sold and update the database accordingly.

            var listing = await _context.Listings.FindAsync(listingId);
            if (listing == null)
            {
                return NotFound();
            }

            // Mark the listing as sold
            listing.IsSold = true;
            _context.Update(listing);
            await _context.SaveChangesAsync();

            // Optionally, you can notify the seller about the sale here
            // You can also log the orderId or do more operations related to the payment.

            return View(); // Return a confirmation view or redirect to another action.
        }
    }
}
