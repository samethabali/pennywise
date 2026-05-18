using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PennyWise.Models
{
    public class SavingsGoal
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Hedef başlığı zorunludur.")]
        [StringLength(100)]
        [Display(Name = "Hedef Adı")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Hedef tutarı zorunludur.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Hedef tutarı 0'dan büyük olmalıdır.")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Hedef Tutar")]
        public decimal TargetAmount { get; set; }

        [Required(ErrorMessage = "Biriken tutar zorunludur.")]
        [Range(0.0, double.MaxValue, ErrorMessage = "Biriken tutar 0 veya daha büyük olmalıdır.")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Biriken Tutar")]
        public decimal CurrentAmount { get; set; }

        [Required(ErrorMessage = "Hedef tarihi zorunludur.")]
        [DataType(DataType.Date)]
        [Display(Name = "Hedef Tarih")]
        public DateTime TargetDate { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        // Yüzdelik ilerleme durumunu hesaplayan readonly property
        [NotMapped]
        public double ProgressPercentage
        {
            get
            {
                if (TargetAmount <= 0) return 0;
                var percentage = (double)(CurrentAmount / TargetAmount) * 100;
                return Math.Min(Math.Round(percentage, 1), 100);
            }
        }
    }
}
