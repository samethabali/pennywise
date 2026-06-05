using System;
using System.ComponentModel.DataAnnotations;

namespace PennyWise.ViewModels
{
    public class TransactionViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Lütfen bir kategori seçiniz.")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Tutar alanı boş bırakılamaz.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Harcama tutarı negatif veya sıfır olamaz.")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Tarih seçimi zorunludur.")]
        public DateTime Date { get; set; }
        
        [StringLength(250)]
        public string? Description { get; set; }
    }
}
