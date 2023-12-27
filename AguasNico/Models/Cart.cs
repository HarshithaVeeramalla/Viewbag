﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AguasNico.Models
{
    public enum State
    {
        [Display(Name = "Pendiente")] Pending = 0,
        [Display(Name = "Confirmado")] Confirmed = 1,
        [Display(Name = "No estaba")] Ausent = 2,
        [Display(Name = "No necesitaba")] NotNeeded = 3,
        [Display(Name = "De vacaciones")] Holidays = 4
    }
    public class Cart
    {
        [Key]
        public long ID { get; set; }

        [Required]
        public long ClientID { get; set; }
        
        [Required]
        public long RouteID { get; set; }

        public int Priority { get; set; }

        [Required]
        public State State { get; set; } = State.Pending;
        
        public bool IsStatic { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(-3);

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow.AddHours(-3);

        public DateTime? DeletedAt { get; set; }

        public virtual Client Client { get; set; } = null!;
        public virtual IEnumerable<CartProduct> Products { get; set; } = null!;
        public virtual IEnumerable<ReturnedProduct> ReturnedProducts { get; set; } = null!;
        public virtual IEnumerable<CartPaymentMethod> PaymentMethods { get; set; } = null!;

        [JsonIgnore]
        public virtual Route Route { get; set; } = null!;
    }
}
