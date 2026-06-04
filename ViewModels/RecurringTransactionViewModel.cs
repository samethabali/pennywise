using System;
using System.ComponentModel.DataAnnotations;
using PennyWise.Models;

namespace PennyWise.ViewModels
{
    public class RecurringTransactionViewModel
    {
        [Required(ErrorMessage = "Lütfen bir kategori seçiniz.")]
        [Display(Name = "Kategori")]
        public int CategoryId { get; set; }


        [Required(ErrorMessage = "Tutar alanı boş bırakılamaz.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Tutar sıfırdan büyük olmalıdır.")]
        [Display(Name = "Tutar")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Açıklama alanı boş bırakılamaz.")]
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
        [Display(Name = "Bitiş Tarihi (Opsiyonel)")]
        public DateTime? EndDate { get; set; }
    }
}
