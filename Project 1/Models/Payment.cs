using System;
using System.ComponentModel.DataAnnotations;

namespace Project_1.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        public int ListingId { get; set; }    // Auction listing ID

        public decimal Amount { get; set; }   // Amount paid

        public DateTime PaymentDate { get; set; } = DateTime.Now;
    }
}
