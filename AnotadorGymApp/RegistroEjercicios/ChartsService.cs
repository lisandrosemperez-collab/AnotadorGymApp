using Microcharts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microcharts.Maui;
using SkiaSharp;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.ComponentModel.DataAnnotations;
using AnotadorGymApp.Data;

namespace AnotadorGymApp.RegistroEjercicios
{
    public class ChartsService
    {                
        private bool VerificacionEjercicio(Exercise ejercicio)
        {
            return ejercicio != null && ejercicio.ExerciseLogs != null && ejercicio.ExerciseLogs.Any(e => e.PesoMaximo > 0);
        }

        public List<ChartEntry> ObtenerPesosMaximos(Exercise ejercicio,string tipoSerie)
        {
            var pesosMaximos = new List<ChartEntry>();

            if (!VerificacionEjercicio(ejercicio))
            {
                return pesosMaximos;
            }

            var logsOrdenados = ejercicio.ExerciseLogs?
                .Where(e => e.PesoMaximo > 0)               
                .OrderBy(e => e.WorkoutDay.Date)
                .ToList() ?? new List<ExerciseLog>();

            foreach (var diaEjercicio in logsOrdenados)
            {
                var charEntry = new ChartEntry((float)diaEjercicio.PesoMaximo)
                {                    
                    Label = diaEjercicio.WorkoutDay.Date.ToString("d/M"),                                        
                    ValueLabel = $"{diaEjercicio.PesoMaximo:0} kg",
                    Color = SKColor.Parse("#7AB09F"),           // GreenPrimary - punto/línea
                    TextColor = SKColor.Parse("#39554C"),       // GreenDarker - labels
                    ValueLabelColor = SKColor.Parse("#486A5F")  // GreenDark - value labels
                };                               
                pesosMaximos.Add(charEntry);
            }
            return pesosMaximos;
        }
        public List<ChartEntry> ObtenerVolumenEjercicio(Exercise ejercicio, string tipoSerie)
        {
            var volumenData = new List<ChartEntry>();

            if (!VerificacionEjercicio(ejercicio))
                return volumenData;

            var logsAgrupados = ejercicio.ExerciseLogs
                .Where(e => e.VolumenTotal > 0 &&
                    e.SetsLog.Any(s => s.Tipo.ToString() == tipoSerie))
                .OrderBy(e => e.WorkoutDay.Date)                
                .ToList();

            foreach (var dia in logsAgrupados)
            {
                var chartEntry = new ChartEntry((float)dia.VolumenTotal)
                {
                    Label = dia.WorkoutDay.Date.ToString("d/M"),
                    ValueLabel = $"{dia.VolumenTotal:0} kg",
                    Color = SKColor.Parse("#7AB09F"),           // GreenPrimary - punto/línea
                    TextColor = SKColor.Parse("#39554C"),       // GreenDarker - labels
                    ValueLabelColor = SKColor.Parse("#486A5F")  // GreenDark - value labels
                };
                volumenData.Add(chartEntry);
            }

            return volumenData;
        }
        public List<ChartEntry> ObtenerRepeticionesPromedio(Exercise ejercicio, string tipoSerie)
        {
            var repsData = new List<ChartEntry>();

            if (!VerificacionEjercicio(ejercicio))
                return repsData;

            // Ordenar por fecha y filtrar logs válidos
            var logsValidos = ejercicio.ExerciseLogs
                .Where(log => log.SetsLog != null &&
                              log.SetsLog.Any(s => s.Reps > 0 && s.Tipo.ToString() == tipoSerie))
                .OrderBy(log => log.WorkoutDay.Date)
                .ToList();

            foreach (var log in logsValidos)
            {
                // Calcular promedio de repeticiones para este log
                var repsPromedio = log.SetsLog
                    .Where(s => s.Reps > 0)
                    .Average(s => s.Reps);

                var chartEntry = new ChartEntry((float)repsPromedio)
                {
                    Label = log.WorkoutDay.Date.ToString("d/M"),
                    ValueLabel = $"{repsPromedio:0.0} reps",
                    Color = SKColor.Parse("#7AB09F"),           // GreenPrimary - punto/línea
                    TextColor = SKColor.Parse("#39554C"),       // GreenDarker - labels
                    ValueLabelColor = SKColor.Parse("#486A5F")  // GreenDark - value labels                   
                };
                repsData.Add(chartEntry);
            }

            return repsData;
        }

        //public List<ChartEntry> TiempoActivoDia(List<RegistroRutinas> ejercicio)
        //{
        //    if (ejercicio == null || ejercicio.Any())
        //    {
        //        return Pesosmaximos;
        //    }
        //    foreach (var dia in ejercicio)
        //    {                
        //        float? fecha = dia.Dia.HasValue ? (float)dia.Dia.Value.Day : 0f;
        //        float tiempo = (float)dia.tiempoactivo.Elapsed.TotalMinutes;                
        //        Pesosmaximos.Add(ObtenerChartEntry(tiempo, fecha));
        //    }
        //    return Pesosmaximos;
        //}
        //public List<ChartEntry> TiempoTotalDia(List<RegistroRutinas> ejercicio)
        //{
        //    if (ejercicio == null || ejercicio.Any())
        //    {
        //        return Pesosmaximos;
        //    }
        //    foreach (var dia in ejercicio)
        //    {
        //        float? fecha = dia.Dia.HasValue ? (float)dia.Dia.Value.Day : 0f;
        //        float tiempo = (float)dia.tiempototal.Elapsed.TotalMinutes;                
        //        Pesosmaximos.Add(ObtenerChartEntry(tiempo, fecha));
        //    }
        //    return Pesosmaximos;
        //}
        //public List<ChartEntry> TiempoRestDia(List<RegistroRutinas> ejercicio)
        //{
        //    if (ejercicio == null || ejercicio.Any())
        //    {
        //        return Pesosmaximos;
        //    }
        //    foreach (var dia in ejercicio)
        //    {
        //        float? fecha = dia.Dia.HasValue ? (float)dia.Dia.Value.Day : 0f;
        //        float tiempo = (float)dia.tiemporest.Elapsed.TotalMinutes;                
        //        Pesosmaximos.Add(ObtenerChartEntry(tiempo, fecha));
        //    }
        //    return Pesosmaximos;
        //}
    }
    public class RegistroRutinas
    {
        public Stopwatch tiempoactivo;
        public Stopwatch tiemporest;
        public Stopwatch tiempototal;
        public double? volumentotal;
        [Key]
        public DateTime? Dia;
    }
}
