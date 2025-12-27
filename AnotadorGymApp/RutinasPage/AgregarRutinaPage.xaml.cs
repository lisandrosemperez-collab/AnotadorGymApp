using AnotadorGymApp.Data;
using AnotadorGymApp.PopUp;
using AnotadorGymApp.Services;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Storage;
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
    private int _semanaIndexSeleccionado = 1;
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

            if (_semanaIndexSeleccionado == value) return;

            _semanaIndexSeleccionado = value;
            OnPropertyChanged(nameof(SemanaIndexSeleccionado));

            Debug.WriteLine($"📊 SemanaIndex: {value}");            
            // Usar Dispatcher para ejecutar después
            Dispatcher.Dispatch(async () =>
            {
                if (value > 0)
                {
                    await SemanaPicker_SelectedIndexChanged(value);
                    OnPropertyChanged(nameof(RutinaActual.SemanasObservable));
                }
            });
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

    private readonly DataService dataService;
    private readonly ImagenPersistenteService imagenPersistenteService;
    private bool _isPopupOpen;
    public List<TipoSerie> TipoSerieEnum => Enum.GetValues(typeof(TipoSerie)).Cast<TipoSerie>().ToList();    
    
    public AgregarRutinaPage(DataService dataService,ImagenPersistenteService imagenPersistenteService)
    {
        InitializeComponent();        
        this.dataService = dataService;    
        this.imagenPersistenteService = imagenPersistenteService;

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
                    await dataService.AgregarRutinaDia(itemSemana,rutinaActual);                    
                    break;

                case "Ejercicio":                                        
                    var itemDia = button.BindingContext as RutinaDia;

                    var popup = new BuscarEjerciciosPopUp(dataService);
                    _isPopupOpen = true;
                    IPopupResult result = await this.ShowPopupAsync(popup, PopupOptions.Empty, CancellationToken.None);
                    _isPopupOpen = false; // ← Popup cerrado
                    // Usar ToList() para evitar modificación concurrente
                    var excSeleccionados = dataService.ExercisesAgregarEjerciciosRutina.ToList();

                    if (excSeleccionados.Count > 0)
                    {
                        await dataService.GuardarRutinaEjercicio(itemDia, excSeleccionados);
                        dataService.ExercisesAgregarEjerciciosRutina.Clear();
                    }                    
                    break;

                case "Serie":
                    var itemEjercicio = button.BindingContext as RutinaEjercicio;                    
                    await dataService.AgregarRutinaSerie(itemEjercicio);
                    
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
                    await dataService.EliminarRutinaDia(dia);
                    break;

                case "Ejercicio":
                    var ejercicio = button.BindingContext as RutinaEjercicio;                    
                    await dataService.EliminarRutinaEjercicio(ejercicio);
                    break;

                case "Serie":
                    var serie = button.BindingContext as RutinaSeries;                    
                    await dataService.EliminarRutinaSerie(serie);
                    break;
            }
        }
    }
    #endregion

    #region SemanaPicker
    private void AlternarExpandido_Clicked(object sender, EventArgs e)
    {
        if(sender is Button btn && btn.BindingContext is RutinaSemana semana)
        {
            
            if (semana.Seleccionado == true)
            {
                semana.Seleccionado = !semana.Seleccionado;
                btn.Text = "Mostrar";
            }
            else {
                semana.Seleccionado = !semana.Seleccionado;
                btn.Text = "Ocultar";
            }
            
        }
    }
    private async Task SemanaPicker_SelectedIndexChanged(int nuevaCantidadSemanas)
    {
        if (RutinaActual == null || nuevaCantidadSemanas < 0)
            return;

        try
        {
            int cantidadActual = RutinaActual.Semanas?.Count ?? 0;

            if (cantidadActual == nuevaCantidadSemanas)
            {
                Debug.WriteLine("✅ Ya tiene la cantidad correcta, omitiendo");
                return;
            }
            if (nuevaCantidadSemanas < cantidadActual)
            {
                await EliminarSemanasAsync(cantidadActual, nuevaCantidadSemanas);
            }
            else if (nuevaCantidadSemanas > cantidadActual)
            {
                await AgregarSemanasAsync(cantidadActual, nuevaCantidadSemanas);
            }
        }
        catch (Exception ex)
        {
            int cantidadActual = RutinaActual?.Semanas?.Count ?? 0;
            RestablecerPicker(cantidadActual);

            Debug.WriteLine($"❌ Error: {ex.Message}");

            await Application.Current.MainPage.DisplayAlert(
                "Error",
                $"Por favor, intente nuevamente.",
                "OK");
        }
    }
    private async Task EliminarSemanasAsync(int cantidadActual, int nuevaCantidad)
    {
        // Confirmación con el usuario
        bool confirmar = await Application.Current.MainPage.DisplayAlert(
            "Confirmar eliminación",
            $"¿Seguro que querés eliminar las semanas {nuevaCantidad} a {cantidadActual}?",
            "Sí, eliminar", "Cancelar");

        int semanasAEliminar = cantidadActual - nuevaCantidad;

        if (!confirmar)
        {
            RestablecerPicker(cantidadActual);
            return;
        }

        // Ejecutar eliminación
        bool eliminacionExitosa = await dataService.EliminarSemanasDeRutinaAsync(RutinaActual, semanasAEliminar);

        if (eliminacionExitosa)
        {            
            await Application.Current.MainPage.DisplayAlert(
                "Éxito",
                "Semanas eliminadas correctamente",
                "OK");
        }
        else
        {
            await Application.Current.MainPage.DisplayAlert(
                "Error",
                "No se pudieron eliminar las semanas",
                "OK");
            RestablecerPicker(cantidadActual);
        }
    }
    private async Task AgregarSemanasAsync(int cantidadActual, int nuevaCantidad)
    {
        int semanasAAgregar = nuevaCantidad - cantidadActual;

        bool agregadas = await dataService.AgregarSemanasARutinaAsync(RutinaActual,semanasAAgregar);

        if (agregadas)
        {            
            await Application.Current.MainPage.DisplayAlert(
                "Éxito",
                $"{semanasAAgregar} semanas agregadas correctamente",
                "OK");
            OnPropertyChanged(nameof(RutinaActual));
            OnPropertyChanged(nameof(RutinaActual.SemanasObservable));
        }
        else
        {
            await Application.Current.MainPage.DisplayAlert(
                "Error",
                "No se pudieron agregar las semanas",
                "OK");
            RestablecerPicker(cantidadActual);
        }
    }
    private void RestablecerPicker(int cantidadOriginal)
    {
        // Restablecer el picker a su valor anterior
        int indiceAnterior = Math.Max(0, cantidadOriginal - 1);

        // Usar Dispatcher para actualizar la UI
        Dispatcher.Dispatch(() => {
            SemanaIndexSeleccionado = indiceAnterior;
        });
    }
    #endregion

    #region Seleccion Imgen Rutina
    private async void AgregarImagenButton_Clicked(object sender, EventArgs e)
    {
        try
        {            
            PermissionStatus status = await Permissions.RequestAsync<Permissions.Photos>();
            if (status != PermissionStatus.Granted)
            {
                await Application.Current.MainPage.DisplayAlert("Permiso denegado",
                    "Se necesita acceso a la galería para seleccionar una imagen", "OK");
                return;
            }                           

            var options = new PickOptions()
            {
                PickerTitle = "Seleccionar imagen de la rutina",
                FileTypes = FilePickerFileType.Images,
            };

            var result = await FilePicker.Default.PickAsync(options);
            if (result != null)
            {
                IsBusy = true;
                string rutaArchivo = await imagenPersistenteService.GuardarImagenUsuarioAsync(RutinaActual.Nombre,result);
                RutinaActual.ImageSource = rutaArchivo;
                await dataService._database.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error seleccionando imagen: {ex.Message}");
            await Application.Current.MainPage.DisplayAlert("Error",
                "No se pudo seleccionar la imagen", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }    
    #endregion
    private async void GuardarRutinaButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (RutinaActual == null)
            {
                await DisplayAlert("Error", "No hay rutina para guardar", "OK");
                return;
            }
            RutinaActual.Nombre = RutinaActual.Nombre?.Trim();
            if (string.IsNullOrWhiteSpace(RutinaActual.Nombre))
            {
                await DisplayAlert("Error", "La rutina debe tener un nombre", "OK");
                return;
            }

            if (RutinaActual.Semanas == null || !RutinaActual.Semanas.Any())
            {
                bool continuar = await DisplayAlert(
                    "Advertencia",
                    "La rutina no tiene semanas. ¿Desea guardarla igual?",
                    "Sí, guardar", "Cancelar");

                if (!continuar) return;
            }

            bool confirmar = await DisplayAlert(
                                    "Listo",
                                    $"¿Terminaste de editar '{RutinaActual.Nombre}'?",
                                    "Sí, salir",
                                    "Seguir editando");
            if (!confirmar) return;
            
            IsBusy = true;            

            await dataService._database.SaveChangesAsync();
                            
            await dataService.DebugDescansoEnBD();

            Debug.WriteLine($"✅ Rutina guardada: '{RutinaActual.Nombre}' (ID: {RutinaActual.RutinaId})");            

            await Shell.Current.GoToAsync("..", animate: true);

        }
        catch(Exception ex)
        {
            Debug.WriteLine($"❌ Error al guardar rutina: {ex.Message}");
            await DisplayAlert("Error", $"No se pudo guardar: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
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
        if (dataService.GetIntIdAgregarRutinaPopUp() != 0)
            RutinaId = dataService.GetIntIdAgregarRutinaPopUp();

        #region RutinaId
        try
        {
            if (RutinaId != 0)
            {
                // MODO EDICIÓN
                var rutina = await dataService.ObtenerRutinaActualyUI(RutinaId);            
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
                    ImageSource = "rutina_default.jpg",
                    Activa = false,                
                };

                await dataService._database.Rutinas.AddAsync(RutinaActual);                

                await dataService._database.SaveChangesAsync();                
                RutinaId = RutinaActual.RutinaId;
            }
        }catch (Exception ex) {Debug.WriteLine($"{ex}"); }

        #endregion

        #region Inicializar SemanaPicker
        if (RutinaActual?.Semanas != null)
        {
            int semanasCount = RutinaActual.Semanas.Count;

            // Si no tiene semanas, agregar una por defecto
            if (semanasCount == 0)
            {
                Debug.WriteLine("➕ Rutina sin semanas, agregando primera semana...");
                await dataService.AgregarSemanasARutinaAsync(rutinaActual,1);
                semanasCount = RutinaActual.Semanas?.Count ?? 0;
            }            

            Debug.WriteLine($"🔧 Inicializando picker: {semanasCount}");
            SemanaIndexSeleccionado = semanasCount;

        }
        #endregion

        dataService.SetIntIdAgregarRutinaPopUp(RutinaId); // lo guardo por si hay popups                      
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
                dataService._database.Rutinas.Remove(RutinaActual);
                await dataService._database.SaveChangesAsync();
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
        dataService.ClearIntIdAgregarRutinaPopUp();
        dataService._database.ChangeTracker.Clear();
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