using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PennyWise.Models
{
    public class RecurringTransaction
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        [Required(ErrorMessage = "Kategori seçimi zorunludur.")]
        [Display(Name = "Kategori")]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }


        [Required(ErrorMessage = "Tutar alanı zorunludur.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Tutar sıfırdan büyük olmalıdır.")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Tutar")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Açıklama alanı zorunludur.")]
        [StringLength(250)]
        [Display(Name = "Açıklama")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Tekrar periyodu seçimi zorunludur.")]
        [Display(Name = "Tekrar Periyodu")]
        public RecurrenceType RecurrenceType { get; set; }

        [Required(ErrorMessage = "Başlangıç tarihi zorunludur.")]
        [DataType(DataType.Date)]
        [Display(Name = "Başlangıç Tarihi")]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Bitiş Tarihi")]
        public DateTime? EndDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Sonraki İşlem Tarihi")]
        public DateTime NextOccurrenceDate { get; set; }

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;
    }

    public enum RecurrenceType
    {
        [Display(Name = "Günlük")]
        Daily = 0,

        [Display(Name = "Haftalık")]
        Weekly = 1,

        [Display(Name = "Aylık")]
        Monthly = 2,

        [Display(Name = "Yıllık")]
        Yearly = 3
    }
}
