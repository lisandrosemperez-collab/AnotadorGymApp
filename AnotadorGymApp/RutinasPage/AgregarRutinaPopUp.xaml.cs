using AnotadorGymApp.Data;
using AnotadorGymApp.PopUp;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Threading.Tasks;
namespace AnotadorGymApp.RutinasPage;

[QueryProperty(nameof(RutinaId),"rutinaId")]
public partial class AgregarRutinaPage : ContentPage, INotifyPropertyChanged
{
    protected void OnPropertyChanged(string nombre)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));
    public event PropertyChangedEventHandler PropertyChanged;
    private int _semanaIndexSeleccionado = 0;
    public int SemanaIndexSeleccionado
    {
        get
        {
            Debug.WriteLine($"🔍 GET SemanaSeleccionada: {_semanaIndexSeleccionado}");
            return _semanaIndexSeleccionado;
        }
        set
        {
            Debug.WriteLine($"🔍 SET SemanaSeleccionada: {_semanaIndexSeleccionado} → {value}");

            // Permite -1 para inicialización, pero no cambios a -1 después
            if (_semanaIndexSeleccionado != value)
            {
                _semanaIndexSeleccionado = value;
                OnPropertyChanged();
                Debug.WriteLine($"✅ SemanaSeleccionada notificada: {value}");

                // Solo ejecutar el cambio si no es -1
                if (value != -1)
                {
                    _ = SemanaPicker_SelectedIndexChanged(value + 1);
                }
            }
        }
    }
    public List<int> OpcionesSemanas { get; } = new List<int>(Enumerable.Range(1, 8));
    public List<TimeSpan> OpcionesSegundos { get; } = Enumerable.Range(0,21).Select(Range => TimeSpan.FromSeconds(Range*15)).ToList();    

    private Rutinas rutinaActual;
    public Rutinas RutinaActual
    {
        get => rutinaActual;
        set
        {
            rutinaActual = value;
            OnPropertyChanged(nameof(RutinaActual));
        }
    }
    private int _rutinaId;
    public int RutinaId
    {
        get => _rutinaId;
        set
        {
            Debug.WriteLine($"🔄 QueryProperty SET: {_rutinaId} → {value}");
            _rutinaId = value;
        }
    }

    public DataService DataService;
    private bool _isPopupOpen;
    public List<TipoSerie> TipoSerieEnum => Enum.GetValues(typeof(TipoSerie)).Cast<TipoSerie>().ToList();    
    
    public AgregarRutinaPage(DataService dataService)
    {
        InitializeComponent();        
        DataService = dataService;        
    }
    #region Entrys
    private void NombreRutinaEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        RutinaActual.Nombre = e.NewTextValue;
    }
    private void NombreDiaEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is Entry entry && entry.BindingContext is RutinaDia rutinaDia)
        {
            rutinaDia.NombreRutinaDia = e.NewTextValue;
        }
    }
    #endregion

    #region ELIMINAR Y AGREGAR
    private async void AgregarElemento_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is string tipo)
        {
            switch (tipo)
            {
                case "Dia":
                    var itemSemana = button.BindingContext as RutinaSemana;
                    await DataService.GuardarRutinaDia(itemSemana,rutinaActual);                    
                    break;

                case "Ejercicio":                                        
                    var itemDia = button.BindingContext as RutinaDia;

                    var popup = new BuscarEjerciciosPopUp(DataService);
                    _isPopupOpen = true;
                    IPopupResult result = await this.ShowPopupAsync(popup, PopupOptions.Empty, CancellationToken.None);
                    _isPopupOpen = false; // ← Popup cerrado
                    // Usar ToList() para evitar modificación concurrente
                    var excSeleccionados = DataService.ExercisesAgregarEjerciciosRutina.ToList();

                    if (excSeleccionados.Count > 0)
                    {
                        await DataService.GuardarRutinaEjercicio(itemDia, excSeleccionados);
                        DataService.ExercisesAgregarEjerciciosRutina.Clear();
                    }                    
                    break;

                case "Serie":
                    var itemEjercicio = button.BindingContext as RutinaEjercicio;                    
                    await DataService.GuardarRutinaSerie(itemEjercicio);
                    
                    break;
            }            
        }
    }               
    private async void EliminarElemento_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is string tipo)
        {
            switch (tipo)
            {
                case "Dia":
                    var dia = button.BindingContext as RutinaDia;
                    await DataService.EliminarRutinaDia(dia);
                    break;

                case "Ejercicio":
                    var ejercicio = button.BindingContext as RutinaEjercicio;                    
                    await DataService.EliminarRutinaEjercicio(ejercicio);
                    break;

                case "Serie":
                    var serie = button.BindingContext as RutinaSeries;                    
                    await DataService.EliminarRutinaSerie(serie);
                    break;
            }
        }
    }
    #endregion
    private async void GuardarRutinaButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(RutinaActual.Nombre))
            {
                await DisplayAlert("Error","La rutina debe tener Nombre","OK");
                return;
            }                        
            await DataService._database.SaveChangesAsync();
            await DataService.DebugDescansoEnBD();
            BorrarUi();
            await Shell.Current.GoToAsync("..");
        }
        catch(Exception ex)
        {
            await DisplayAlert("Error",$"{ex}","OK");
        }
    }
    
    private void BorrarObservableCollections(Rutinas rutina)
    {
        if(RutinaActual == null) { return; }        
        rutina.SemanasObservable?.Clear();

        foreach (var semana in rutina.Semanas)
        {
            semana.DiasObservable?.Clear();
            foreach (var dia in semana.Dias)
            {
                dia.EjerciciosObservable?.Clear();

                foreach (var ejercicio in dia.Ejercicios)
                {
                    ejercicio.SeriesObservable?.Clear();
                }
            }
        }
        RutinaActual = null;
    }    
    private void AlternarExpandido_Clicked(object sender, EventArgs e)
    {
        if(sender is Button btn && btn.BindingContext is RutinaSemana semana)
        {
            
            if (semana.IsExpanded == true)
            {
                semana.IsExpanded = !semana.IsExpanded;
                btn.Text = "Mostrar";
            }
            else {
                semana.IsExpanded = !semana.IsExpanded;
                btn.Text = "Ocultar";
            }
            
        }
    }
    private async Task SemanaPicker_SelectedIndexChanged(int cantidadSeleccionada)
    {
        if (cantidadSeleccionada == null || RutinaActual?.Semanas == null) return;        

        int cantidadActual = RutinaActual.Semanas.Count;        

        if (cantidadSeleccionada < cantidadActual)
        {
            bool confirmar = await Application.Current.MainPage.DisplayAlert(
                "Confirmar",
                $"¿Seguro que querés eliminar las semanas {cantidadSeleccionada + 1} a {cantidadActual}?",
                "Sí", "Cancelar");

            if (!confirmar)
            {
                // Volver al valor anterior
                SemanaIndexSeleccionado = cantidadActual;
                return;
            }

            // Eliminar semanas
            while (RutinaActual.Semanas.Count > cantidadSeleccionada)
            {
                var ultimaSemana = RutinaActual.Semanas.Last();
                RutinaActual.Semanas.Remove(ultimaSemana);
                RutinaActual.SemanasObservable.Remove(ultimaSemana);
                DataService._database.Remove(ultimaSemana);
            }

            await DataService._database.SaveChangesAsync();
        }
        else if (cantidadSeleccionada > cantidadActual)
        {
            // Agregar semanas
            for (int i = cantidadActual + 1; i <= cantidadSeleccionada; i++)
            {
                var itemSemana = new RutinaSemana()
                {
                    Rutina = RutinaActual,
                    RutinaId = RutinaActual.RutinaId,
                    SemanaId = i
                };
                RutinaActual.Semanas.Add(itemSemana);
                RutinaActual.SemanasObservable.Add(itemSemana);
                DataService._database.RutinaSemanas.Add(itemSemana);
            }
            await DataService._database.SaveChangesAsync();
        }
    }    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Si es por un popup, no recargar
        if (_isPopupOpen)
        {
            _isPopupOpen = false;
            return;
        }
        //ESTO ES PARA QUE AL VOLVER DEL POPUP, TENGAMOS DEVUELTA EL ID DE LA RUTINA NUEVA, Y NO NOS VUELVA A CREAR OTRA RUTINA        
        // Al crear Una Rutina Nueva, RUTINAID es igual a 0, luego editando la rutina sigue siendo 0 pero la rutina Internamente ya tiene su propido ID por eso se usa
        // este metodo para guardar en DataService el ID REAL de la nueva Rutina
        if (DataService.GetIntIdAgregarRutinaPopUp() != 0)
            RutinaId = DataService.GetIntIdAgregarRutinaPopUp();

        #region RutinaId
        if (RutinaId != 0)
        {
            // MODO EDICIÓN
            var rutina = await DataService.ObtenerRutinaActualyUI(RutinaId);            
            if (rutina != null)
            {                                
                RutinaActual = rutina;
            }
            else
            {
                await DisplayAlert("Error", "No se encontró la rutina", "OK");
                await Shell.Current.GoToAsync(".."); // vuelve para atrás
            }
        }
        else
        {
            // MODO NUEVA RUTINA
            RutinaActual = new Rutinas
            {
                Nombre = "Nueva Rutina",
                Activa = false,                
            };

            await DataService._database.Rutinas.AddAsync(RutinaActual);
            await DataService._database.SaveChangesAsync(); // necesario para que RutinaId se genere            
            RutinaId = RutinaActual.RutinaId;
        }



        #endregion

        #region Inicializar SemanaPicker
        if (RutinaActual?.Semanas != null && OpcionesSemanas?.Count > 0)
        {
            int semanasCount = RutinaActual.Semanas.Count;

            // Calcular índice (base 0)
            int nuevoIndex = Math.Min(semanasCount - 1, OpcionesSemanas.Count - 1);
            nuevoIndex = Math.Max(0, nuevoIndex); // No menor que 0
            await Task.Delay(50);

            Debug.WriteLine("🟡 ANTES de setear SemanaSeleccionada");
            Device.BeginInvokeOnMainThread(() =>
            {
                SemanaIndexSeleccionado = nuevoIndex;
                SemanaPicker.SelectedIndex = nuevoIndex;
            });
        }
        #endregion

        DataService.SetIntIdAgregarRutinaPopUp(RutinaId); // lo guardo por si hay popups                      
        BindingContext = this;                      
    }
    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        // Si es por abrir un popup, no borrar
        if (_isPopupOpen)
        {
            return;
        }
        //Si No Tiene Ejercicios al menos 1 dia, Borrar Rutina
        try
        {
            bool tieneEjercicios = RutinaActual.Semanas?
                .SelectMany(s => s.Dias ?? Enumerable.Empty<RutinaDia>()).Any() == true;

            if (!tieneEjercicios)
            {
                // Opción A: Eliminar
                DataService._database.Rutinas.Remove(RutinaActual);
                await DataService._database.SaveChangesAsync();
                BorrarUi();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error en OnDisappearing: {ex.Message}");
        }

        BorrarUi();
    }    

    public void BorrarUi()
    {
        DataService.ClearIntIdAgregarRutinaPopUp();
        DataService._database.ChangeTracker.Clear();
        BorrarObservableCollections(rutinaActual);
        RutinaActual = new Rutinas();
        RutinaId = 0;
        NombreRutinaEntry.Text = string.Empty;
        SemanaPicker.SelectedIndex = -1;
        SemanaPicker.SelectedItem = null;
    }
}


public class IntToTimeSpanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TimeSpan tiempo)
        {
            if (tiempo == TimeSpan.Zero) return "Sin descanso";
            return $"{tiempo.Minutes} min {tiempo.Seconds} seg";
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (int.TryParse(value?.ToString(), out int minutos))
            return TimeSpan.FromMinutes(minutos);
        return null;
    }
}