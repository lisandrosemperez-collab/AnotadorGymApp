using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;
using AnotadorGymApp.Data;
using AnotadorGymApp.MainPageViews;
using CommunityToolkit.Maui.Views;
using Microcharts;
using Microcharts.Maui;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Controls.Platform.Compatibility;
using System.Diagnostics;

namespace AnotadorGymApp
{
    public partial class MainPage : ContentPage
    {
        private readonly DataService _DataService;             
        DateTime? day = DateTime.Now;
        public Rutinas RutinaActiva { get; set; }                
        public WorkoutDay? WorkoutDay { get; set; }        
        public ResumenSemanal ResumenSemanal { get; set; }        
        public MainPage(DataService dataService)
        {
            InitializeComponent();
            _DataService = dataService;                                     
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarUi();
            BindingContext = this;
        }
        private async Task CargarUi()
        {
            try
            {
                var TaskRutina = CargarRutinaAsync();
                var TaskEntrenoHoy = CargarEntrenoDeHoy();
                var TaskResumenSemanal = CargarResumenSemanal();                

                await Task.WhenAll(TaskRutina, TaskEntrenoHoy,TaskResumenSemanal);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error cargando datos iniciales: {ex.Message}");                
            }
        }
        private async Task CargarRutinaAsync()
        {
            RutinaActiva = await _DataService.ObtenerRutinaActiva();
            try
            {
                if (RutinaActiva == null)
                {
                    // 🔥 Usar Dispatcher para cambios de UI
                    Dispatcher.Dispatch(() => RutinaActivaContenedor.IsVisible = false);
                    Debug.WriteLine("ℹ️ No hay rutina activa");
                }
                else
                {                    
                    RutinaActivaContenedor.IsVisible = true;
                }                
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error cargando rutina: {ex.Message}");
                Dispatcher.Dispatch(() => RutinaActivaContenedor.IsVisible = false);
            }            
        }            
        private async Task CargarEntrenoDeHoy()
        {
            try
            {
                var hoy = DateTime.Today;
                WorkoutDay = await _DataService._database.WorkoutDay.
                                    Where(w => w.Date.Date == hoy).
                                        Include(w => w.ExerciseLogs)
                                            .ThenInclude(e => e.SetsLog).
                                        Include(w => w.ExerciseLogs)
                                            .ThenInclude(e => e.Exercise).
                                    AsNoTracking().
                                    FirstOrDefaultAsync();
                
                
                if (WorkoutDay != null && WorkoutDay.ExerciseLogs.Any())
                {
                    WorkutDayBorder.IsVisible = true;
                    ExerciseLogBorder.IsVisible = true;
                }
                else { WorkutDayBorder.IsVisible = false; ExerciseLogBorder.IsVisible = false; }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error cargando entreno: {ex.Message}");
                // También ocultar en caso de error
                WorkutDayGrid.IsVisible = false;
                ExerciseLogCollectionView.IsVisible = false;
            }
        }
        private async Task CargarResumenSemanal()
        {
            try
            {
                var fechaInicioSemana = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
                var fechaFinSemana = fechaInicioSemana.AddDays(6);

                var diasSemana = await _DataService._database.WorkoutDay
                    .Where(w => w.Date >= fechaInicioSemana && w.Date <= fechaFinSemana)
                    .Include(w => w.ExerciseLogs)
                        .ThenInclude(e => e.SetsLog)
                    .ToListAsync();

                // Crear resumen en memoria
                ResumenSemanal = new ResumenSemanal
                {
                    FechaInicio = fechaInicioSemana,
                    FechaFin = fechaFinSemana,
                    DiasEntrenados = diasSemana.Count,
                    VolumenTotal = diasSemana.Sum(d => d.VolumenTotal),
                    EjerciciosTotal = diasSemana.Sum(d => d.EjerciciosTotal),
                    SeriesTotal = diasSemana.Sum(d => d.SeriesTotal)
                };

                // Mostrar/ocultar según si hay datos
                ResumenSemanalGrid.IsVisible = diasSemana.Any();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error calculando resumen semanal: {ex.Message}");
                ResumenSemanalGrid.IsVisible = false;
            }
        }
        private void IniciarRutinaButton_Clicked(object sender, EventArgs e)
        {
            Shell.Current.GoToAsync("//Rutinas");            
        }
        private async void ContinuarRutinaButton_Clicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync($"ComienzoRutina?rutinaId={RutinaActiva.RutinaId}");
            
        }        
        
    }

}
