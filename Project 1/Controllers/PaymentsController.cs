using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using Project_1.Data;
using Project_1.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Project_1.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult CreateCheckoutSession(decimal amount, int listingId)
        {
            var domain = "https://localhost:44348";

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                CustomerEmail = User.Identity.Name, // optional
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmountDecimal = amount * 100,
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Auction Payment"
                            }
                        },
                        Quantity = 1
                    }
                },
                Mode = "payment",
                SuccessUrl = domain + $"/Payment/Success?listingId={listingId}&amount={amount}",
                CancelUrl = domain + "/Payment/Cancel"
            };

            var service = new SessionService();
            var session = service.Create(options);

            return Redirect(session.Url);
        }

        [HttpGet]
        public async Task<IActionResult> Success(int listingId, decimal amount)
        {
            var payment = new Payment
            {
                ListingId = listingId,
                Amount = amount,
                PaymentDate = DateTime.Now
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return View(payment);
        }

        [HttpGet]
        public IActionResult Cancel()
        {
            return View();
        }
    }
}
