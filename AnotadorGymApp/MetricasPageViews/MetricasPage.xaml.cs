using AnotadorGymApp.Data;
using AnotadorGymApp.RegistroEjercicios;
using Microcharts;
using Microcharts.Maui;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AnotadorGymApp.MetricasPageViews;

public partial class MetricasPage : ContentPage, INotifyPropertyChanged
{
    private DataService dataService;    
    private ObservableCollection<EjercicioConMetricas> _ejerciciosConMetricas = new ObservableCollection<EjercicioConMetricas>();
    public ObservableCollection<EjercicioConMetricas> EjerciciosConMetricas
    {
        get => _ejerciciosConMetricas;
        set
        {
            _ejerciciosConMetricas = value;
            OnPropertyChanged(nameof(EjerciciosConMetricas)); // ← Notificar cambio aquí
        }
    }
    private string filtroTiempoSeleccionado = "Todos";
    public string FiltroTiempoSeleccionado { get => filtroTiempoSeleccionado; set
        {
            filtroTiempoSeleccionado = value;
            OnPropertyChanged(nameof(FiltroTiempoSeleccionado));            
        } 
    }

    #region Ejercicio Buscado

    private string ejercicioBuscado = string.Empty;
    public string EjercicioBuscado { get => ejercicioBuscado; set
        {
            ejercicioBuscado = value;
            OnPropertyChanged(nameof(EjercicioBuscado));
            _ = DebounceFiltro();
        } 
    }
    private CancellationTokenSource _debounceCts;
    private async Task DebounceFiltro()
    {
        try
        {
            _debounceCts?.Cancel();
            _debounceCts = new CancellationTokenSource();
            await Task.Delay(300, _debounceCts.Token);
            await FiltrarEjercicios();
            InicializarCharts();
        }
        catch (TaskCanceledException ex) { }
    }
    #endregion

    private ObservableCollection<Exercise> EjerciciosFiltrados = new ObservableCollection<Exercise>();
    private ICommand selecionarFiltroTiempoCommand;
    public ICommand SeleccionarFiltroTiempoCommand => selecionarFiltroTiempoCommand ??= new Command<string>(async (filtro) =>
    {
        FiltroTiempoSeleccionado = filtro;
        await FiltrarEjercicios();
        InicializarCharts();
    });
    public MetricasPage(DataService dataService)
	{
		InitializeComponent();
        this.dataService = dataService;
        BindingContext = this;        
    }
    private async Task FiltrarEjercicios()
    {                
        EjerciciosFiltrados = await dataService.FiltrarEjercicios(EjercicioBuscado,FiltroTiempoSeleccionado);                
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();            
    }

    private async void InicializarCharts()
    {
        try
        {                                    
            var listEjerciciosConMetricas = new List<EjercicioConMetricas>();
            foreach (var ejercicio in EjerciciosFiltrados)
            {                                
                // Crear y asignar gráfico                
                var ejercicioConMetrica = new EjercicioConMetricas
                {
                    Ejercicio = ejercicio,                    
                    TotalSesiones = ejercicio.ExerciseLogs?.Count ?? 0,
                };
                listEjerciciosConMetricas.Add(ejercicioConMetrica);
            }
            EjerciciosConMetricas = new ObservableCollection<EjercicioConMetricas>(listEjerciciosConMetricas);
            OnPropertyChanged(nameof(EjerciciosConMetricas));

            Debug.WriteLine($"✅ Cargados {EjerciciosConMetricas.Count} ejercicios con gráficos");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Error mostrando gráfico: {ex.Message}");
        }
    }    
}

public class TimeFilterToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var filtroSeleccionado = value as string;        // "Semana" (del ViewModel)
        var miFiltro = parameter as string;              // "Semana" (del ConverterParameter)

        // ¿Soy el botón activo?
        bool soyActivo = (filtroSeleccionado == miFiltro);

        return soyActivo ?
            Color.FromArgb("#7AB09F") :  // Verde oscuro - ACTIVO
            Color.FromArgb("#A5EBD4");   // Verde claro - INACTIVO
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}