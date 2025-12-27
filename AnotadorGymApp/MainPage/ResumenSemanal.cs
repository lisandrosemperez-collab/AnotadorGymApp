using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnotadorGymApp.MainPageViews
{
    public class ResumenSemanal
    {        
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public int? DiasEntrenados { get; set; }
        public double? VolumenTotal { get; set; }
        public int? EjerciciosTotal { get; set; }
        public int? SeriesTotal { get; set; }

        [NotMapped]
        public string RangoSemanasDisplay => $"{FechaInicio:dd/MM} - {FechaFin:dd/MM}";

        [NotMapped]
        public string DiasEntrenadosDisplay => $"{DiasEntrenados}/7 días";
     
    }
}
