using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PennyWise.Models
{
    public class BudgetLimit
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        
        [Required]
        public int CategoryId { get; set; }
        public Category Category { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal LimitAmount { get; set; } // Aylık limit
        
        [Required]
        public int Month { get; set; } // 1 - 12
        
        [Required]
        public int Year { get; set; }
    }
}
