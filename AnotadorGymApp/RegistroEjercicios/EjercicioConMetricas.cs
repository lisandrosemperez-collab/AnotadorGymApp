using AnotadorGymApp.Data;
using Microcharts;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnotadorGymApp.RegistroEjercicios
{
    public class EjercicioConMetricas : INotifyPropertyChanged
    {
        #region ChartEntrys y ChartService
        private ChartsService registroEjercicio = new ChartsService();
        private List<ChartEntry> datosPeso;
        private List<ChartEntry> datosVolumen;
        private List<ChartEntry> datosReps;
        #endregion
        public List<string> TiposSerieFiltros => Enum.GetNames(typeof(TipoSerie)).ToList();
        // Propiedades para filtros
        private string _tiposSerieSeleccionado = "Normal";
        public string TiposSerieSeleccionado
        {
            get => _tiposSerieSeleccionado;
            set
            {
                _tiposSerieSeleccionado = value ?? "Normal";
                OnPropertyChanged(TiposSerieSeleccionado);
                ActualizarGraficosFiltrados();
            }
        }
        private void ActualizarGraficosFiltrados()
        {
            datosPeso = registroEjercicio.ObtenerPesosMaximos(Ejercicio,TiposSerieSeleccionado);
            datosVolumen = registroEjercicio.ObtenerVolumenEjercicio(Ejercicio, TiposSerieSeleccionado);
            datosReps = registroEjercicio.ObtenerRepeticionesPromedio(Ejercicio, TiposSerieSeleccionado);

            GraficoPeso = CrearMiniGrafico(datosPeso, "#7AB09F");
            GraficoVolumen = CrearMiniGrafico(datosVolumen, "#689788");
            GraficoReps = CrearMiniGrafico(datosReps, "#4FC3F7");

            OnPropertyChanged(nameof(GraficoPeso));
            OnPropertyChanged(nameof(GraficoVolumen));
            OnPropertyChanged(nameof(GraficoReps));
        }

        private Exercise ejercicio;
        public Exercise Ejercicio
        {
            get => ejercicio; 
            set
            {
                if (ejercicio != value) { ejercicio = value;OnPropertyChanged(nameof(Ejercicio)); }
                ActualizarGraficosFiltrados();
            }
        }
        public Chart GraficoPeso { get; set; }
        public Chart GraficoVolumen { get; set; }
        public Chart GraficoReps { get; set; }
        public int TotalSesiones { get; set; }
        public double ProgresoUltimoMes { get; set; }

        private Chart CrearMiniGrafico(List<ChartEntry> entries, string color)
        {
            if (entries == null || !entries.Any())
            {
                return CrearGraficoVacio();
            }

            return new LineChart
            {
                Entries = entries,
                LabelTextSize = 30,
                ValueLabelTextSize = 34,

                // Estilo de línea y puntos
                LineSize = 4,
                PointSize = 10,
                PointMode = PointMode.Circle,

                // Colores usando tu paleta
                LabelColor = SKColor.Parse("#39554C"),          // GreenDarker - eje X            
                BackgroundColor = SKColor.Parse("#FFFFFF"),     // BackgroundColor                         

                // Orientación
                LabelOrientation = Orientation.Horizontal,
                ValueLabelOrientation = Orientation.Horizontal,

                // Animación
                AnimationDuration = TimeSpan.FromMilliseconds(800),
                IsAnimated = true,
                Margin = 40,
            };
        }
        private Chart CrearGraficoVacio()
        {
            return new LineChart
            {
                Entries = new[]
                {
            new ChartEntry(0) { Color = SKColor.Parse("#A5EBD4") }
        },
                BackgroundColor = SKColors.Transparent,
                LabelColor = SKColors.Transparent
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
