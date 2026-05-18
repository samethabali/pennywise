using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PennyWise.Models
{
    public class Bill
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Fatura başlığı zorunludur.")]
        [StringLength(100)]
        [Display(Name = "Fatura Adı")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Fatura tutarı zorunludur.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Tutar 0'dan büyük olmalıdır.")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Tutar")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Son ödeme tarihi zorunludur.")]
        [DataType(DataType.Date)]
        [Display(Name = "Son Ödeme Tarihi")]
        public DateTime DueDate { get; set; }

        [Display(Name = "Ödendi mi?")]
        public bool IsPaid { get; set; }

        [Required(ErrorMessage = "Kategori seçimi zorunludur.")]
        [Display(Name = "Kategori")]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
    }
}
