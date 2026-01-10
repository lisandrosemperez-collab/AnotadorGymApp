using AnotadorGymApp.ConfiguracionPage;
using AnotadorGymApp.Data;
using AnotadorGymApp.MainPageViews;
using AnotadorGymApp.Services;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Microcharts;
using Microcharts.Maui;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Platform.Compatibility;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace AnotadorGymApp
{
    public partial class MainPage : ContentPage
    {
        private readonly DataService _dataService;
        private readonly ConfigService _configService;
        private readonly ConfigPage _configPage;
        DateTime? day = DateTime.Now;
        public Rutinas RutinaActiva { get; set; }                
        public WorkoutDay? WorkoutDay { get; set; }        
        public ResumenSemanal ResumenSemanal { get; set; }        
        public MainPage(DataService dataService,ConfigService configService,ConfigPage configPage)
        {
            InitializeComponent();
            _dataService = dataService;         
            _configService = configService;
            _configPage = configPage;
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

                bool esDemo = Preferences.Get("UsarDatosDemo", false);
                bool notificacionDemo = Preferences.Get("MostrarNotificacionDemoInicial", false);

                if (esDemo && notificacionDemo)
                {

                    bool respuesta = await Shell.Current.DisplayAlert(
                        "🎯 Modo Demo",
                        "Estás usando la versión de demostración con contenido limitado.\n\n" +
                        "¿Deseas obtener la versión completa con todos los ejercicios y rutinas?",
                        "Sí, quiero la versión completa",
                        "Continuar en demo"
                    );

                    if (respuesta)
                    {
                        //EN DESARROLLO
                        //await Launcher.OpenAsync("https://tudominio.com/descargar-app-completa");
                    }
                                        
                    Preferences.Set("MostrarNotificacionDemoInicial", false);
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error cargando datos iniciales: {ex.Message}");                
            }
        }
        private async Task CargarRutinaAsync()
        {
            RutinaActiva = await _dataService.ObtenerRutinaActiva();
            try
            {
                if (RutinaActiva == null)
                {
                    // 🔥 Usar Dispatcher para cambios de UI
                    Dispatcher.Dispatch(() => RutinaActivaContenedor.IsVisible = false);
                    Dispatcher.Dispatch(() => SinRutinaContenedor.IsVisible = true);
                    Dispatcher.Dispatch(() => IniciarRutinaButton.IsVisible = false);
                    
                    Debug.WriteLine("ℹ️ No hay rutina activa");
                }
                else
                {
                    Dispatcher.Dispatch(() => IniciarRutinaButton.IsVisible = true);
                    Dispatcher.Dispatch(() => RutinaActivaContenedor.IsVisible = true);
                    Dispatcher.Dispatch(() => SinRutinaContenedor.IsVisible = false);
                }                
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error cargando rutina: {ex.Message}");
                Dispatcher.Dispatch(() => IniciarRutinaButton.IsVisible = false);
                Dispatcher.Dispatch(() => RutinaActivaContenedor.IsVisible = false);
                Dispatcher.Dispatch(() => SinRutinaContenedor.IsVisible = true);
            }            
        }            
        private async Task CargarEntrenoDeHoy()
        {
            try
            {
                var hoy = DateTime.Today;
                WorkoutDay = await _dataService._database.WorkoutDay.
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

                var diasSemana = await _dataService._database.WorkoutDay
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
                SinDatosSemanalesContenedor.IsVisible = !diasSemana.Any();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error calculando resumen semanal: {ex.Message}");
                ResumenSemanalGrid.IsVisible = false;
                SinDatosSemanalesContenedor.IsVisible = true;
            }
        }
        private async void OnConfigClicked(object sender, EventArgs e)
        {
            // Efecto visual opcional
            await ConfigButton.ScaleTo(0.8, 50, Easing.Linear);
            await ConfigButton.ScaleTo(1.0, 50, Easing.Linear);
                        
            await Navigation.PushAsync(_configPage);
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
