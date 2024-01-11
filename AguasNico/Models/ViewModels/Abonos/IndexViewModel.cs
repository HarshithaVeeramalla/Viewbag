﻿using Microsoft.AspNetCore.Mvc.Rendering;

namespace AguasNico.Models.ViewModels.Abonos
{
    public class IndexViewModel
    {
        public ApplicationUser User = new();
        public IEnumerable<Abono> Abonos { get; set; } = new List<Abono>();
        public Abono EditedAbono { get; set; } = new();
    }
}
