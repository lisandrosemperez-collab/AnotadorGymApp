using AnotadorGymApp.Data;
using AnotadorGymApp.PopUp;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Extensions;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Compatibility;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;

namespace AnotadorGymApp;
[QueryProperty(nameof(RutinaId), "rutinaId")]
public partial class ComienzoRutinaPage : ContentPage, INotifyPropertyChanged
{
    //Guardado de RutinaActual    
    public DataService dataService;
    public int RutinaId { get; set; }
    public Rutinas RutinaActual { get; set; } = new Rutinas();
    public WorkoutDay WorkoutDayActual { get; set; } = new WorkoutDay();    

    #region Comandos
    RutinaSeries SerieActual;    
    public ICommand PlayPauseCommand { get; private set; }
    public ICommand ActualizarCommand { get;private set; }
    #endregion

    #region Timers
    CancellationTokenSource ctsTotalTimer = new CancellationTokenSource();
    public Stopwatch TotalTimer = new Stopwatch();
    public Stopwatch RestTimer = new Stopwatch();
    public Stopwatch ActTimer = new Stopwatch();
    private string tiempo;
    private string TiempoActivo;
    private string TiempoRest;    
    private bool _isPopupOpen;
    #endregion

    #region Notify
    public event PropertyChangedEventHandler? PropertyChanged;    
    public string tiemporest { get { return TiempoRest; } set 
        { 
            TiempoRest = value;
            OnPropertyChanged(nameof(tiemporest));
        }}
    public string tiempoactivo { get { return TiempoActivo; } set
        {
            TiempoActivo = value; OnPropertyChanged(nameof(tiempoactivo));
        }}
    public string Tiempo { get { return tiempo; } set 
        {            
            tiempo = value; OnPropertyChanged(nameof(Tiempo));            
        } }
    #endregion
    public ComienzoRutinaPage(DataService dataservice)
	{
        this.dataService = dataservice;        
		InitializeComponent();        
        PlayPauseCommand = new Command((parameters) =>
        {
            if (parameters is ValueTuple<RutinaSeries, RutinaEjercicio> tuple)
            {                
                var (rutinaSeries,exercise) = tuple;
                PlayPause(rutinaSeries, exercise);
            }            
        });                                     
    }   
    private void PlayPause(RutinaSeries rutinaSeries,RutinaEjercicio exercise)
    {
        if (!TotalTimer.IsRunning) { IniciarCronometro_Clicked(null,EventArgs.Empty); }                        
        var estado=rutinaSeries.EstadoSerie;
        
        switch (estado)
        {
            case 1: //PLAY
                if (SerieActual == null)
                {
                    rutinaSeries.EstadoSerie = 2;
                    IncicarActwatch(rutinaSeries);                                        
                    SerieActual = rutinaSeries;                    
                }                
                break;            

            case 2: //PARAR                
                rutinaSeries.EstadoSerie = 3;
                IniciarRestWatch(rutinaSeries);
                break;     

            case 3: //LISTO
                rutinaSeries.EstadoSerie = 4;
                rutinaSeries.DetenerRest();
                GuardarSerie(rutinaSeries);
                SerieActual = null;

                break;
            case 4: //EDITAR
                if (SerieActual == null)
                {
                    rutinaSeries.EstadoSerie = 3;
                    SerieActual = rutinaSeries;
                }
                break;
        }                                       
    }
    private async void GuardarSerie(RutinaSeries rutinaSeries)
    {
        #region ExerciseLog
        var ExerciseLog = WorkoutDayActual.ExerciseLogs.FirstOrDefault(w => w.ExerciseId == rutinaSeries.Ejercicio.ExerciseId);
        
        if (ExerciseLog == null)
        {
            ExerciseLog = new ExerciseLog()
            {
                Exercise = rutinaSeries.Ejercicio.Exercise,
                ExerciseId = rutinaSeries.Ejercicio.ExerciseId
            };
            WorkoutDayActual.ExerciseLogs.Add(ExerciseLog);            
        }
        #endregion

        #region SetLog
        var SetLog = ExerciseLog.SetsLog?.FirstOrDefault(s => s.SetLogId == rutinaSeries.SetLog.SetLogId);

        if (SetLog == null)
        {
            SetLog = new SetLog();
            ExerciseLog.SetsLog.Add(SetLog);
        }
        SetLog.Kilos = rutinaSeries.KilosTemp;
        SetLog.Reps = rutinaSeries.RepsTemp;
        SetLog.Tipo = rutinaSeries.Tipo;
        #endregion

        #region Exercise

        var Exercise = rutinaSeries.Ejercicio.Exercise;
        
        if (Exercise != null)
        {
            Exercise.dataService = dataService;

            if (SetLog.Tipo == TipoSerie.Max_Rm || SetLog.Tipo == TipoSerie.Normal)
            {
                // Validar que los kilos sean válidos
                if (SetLog.Kilos > 0 && SetLog.Reps > 0) 
                {
                    // ÚLTIMO: Siempre actualizar
                    Exercise.Ultimo = SetLog.Kilos;
                    if (Exercise.Mejor == null || SetLog.Kilos > Exercise.Mejor)
                    {
                        Exercise.Mejor = SetLog.Kilos;
                        Debug.WriteLine($"🏆 Nuevo récord personal: {SetLog.Kilos}kg");
                    }

                    // INICIAR: Solo si es nulo o cero y si es MaxRm (primera vez)
                    if (SetLog.Tipo == TipoSerie.Max_Rm)
                    {
                        if (Exercise.Iniciar == 0 || Exercise.Iniciar == null)
                        {
                            Exercise.Iniciar = SetLog.Kilos;
                            Debug.WriteLine($"🎯 Primer RM guardado como Iniciar: {SetLog.Kilos}kg");
                        }
                        // MEJOR: Solo si es mejor que el anterior
                    }

                    Debug.WriteLine($"📊 Progreso actual - Iniciar: {Exercise.Iniciar}kg, Mejor: {Exercise.Mejor}kg, Último: {Exercise.Ultimo}kg");                
                }
                else
                {
                    Debug.WriteLine($"⚠️ SetLog con kilos/reps inválidos: {SetLog.Kilos}kg x {SetLog.Reps}");
                }

            }
            else
            {
                Debug.WriteLine($"ℹ️ Serie de tipo {SetLog.Tipo} - No afecta progreso RM");
            }
        }                        
        #endregion

        #region Verificar Si El Ejercicio Esta Completado
        bool SeriesCompletadas = rutinaSeries.Ejercicio.Series.All(s => s.EstadoSerie == 4);
        rutinaSeries.Ejercicio.Completado = SeriesCompletadas;
        Debug.WriteLine($"Total series: {rutinaSeries.Ejercicio.Series?.Count}");
        Debug.WriteLine($"¿Todas completadas? {SeriesCompletadas}");
        #endregion

        #region Verificar Si El Dia Esta Completado
        RutinaDia itemDia = CollectionDias.SelectedItem as RutinaDia;
        if (itemDia != null)
        {
            bool EjerciciosCompletados = itemDia.Ejercicios?.All(eje => eje.Completado) ?? false;
        
            if (EjerciciosCompletados)
            {
                itemDia.Completado = true;
                RestTimer.Stop();
                rutinaSeries.DetenerRest();
                IniciarCronometro_Clicked(null, EventArgs.Empty);
                await Shell.Current.DisplayAlert("Terminado", "Ejercicios Completados, Agregue uno nuevo o Finalize el Dia", "Ok");
            }
            else { itemDia.Completado = false; }
        }
        #endregion

        await dataService._database.SaveChangesAsync();    
    }
    private async void IncicarActwatch(RutinaSeries rutinaSeries)
    {                
        ActTimer.Start();
        RestTimer.Stop();
        
        rutinaSeries.DetenerRest();

        while (rutinaSeries.EstadoSerie ==2)
        {
            tiempoactivo = $"{ActTimer.Elapsed.Minutes:D2}:{ActTimer.Elapsed.Seconds:D2}";
            await Task.Delay(1000);
        }
        ActTimer.Stop();
    }
    private async void IniciarRestWatch(RutinaSeries rutinaSeries)
    {                
        RestTimer.Start();
        rutinaSeries.ComienzoRest();

        while (RestTimer.IsRunning)
        {
            tiemporest = $"{RestTimer.Elapsed.Minutes:D2}:{RestTimer.Elapsed.Seconds:D2}";
            await Task.Delay(1000).ConfigureAwait(false);
        }        
    }      
    private async void IniciarCronometro_Clicked(object? sender, EventArgs e)
    {                
        if (!TotalTimer.IsRunning) 
        {
            IniciarButton.Text = "Pausar";
            #region Verificar Si ya Habia una Rutina Activa
            var rutinaActiva = await dataService.VerificarSiHayRutinaActiva();
            if (rutinaActiva != null)
            {
                rutinaActiva.Activa = false;                
            }
            RutinaActual.Activa = true;
            await dataService._database.SaveChangesAsync();
            #endregion
            TotalTimer.Start();

            try
            {
                while (TotalTimer.IsRunning && !ctsTotalTimer.IsCancellationRequested)
                {
                    Tiempo = $"{TotalTimer.Elapsed.Minutes:D2}:{TotalTimer.Elapsed.Seconds:D2}";

                    await Task.Delay(1000,ctsTotalTimer.Token).ConfigureAwait(false);
                }
            }
            catch (TaskCanceledException){}
        }
        else
        {
            Dispatcher.Dispatch(() =>
            {
                TotalTimer.Stop();
                IniciarButton.Text = "Iniciar";
            });

            ctsTotalTimer.Cancel();
            ctsTotalTimer.Dispose();
            ctsTotalTimer = new CancellationTokenSource();            
        }
    }
    private async void AñadirEjercicio_Clicked(object sender, EventArgs e)
    {
        RutinaDia itemDia = CollectionDias.SelectedItem as RutinaDia;
        if(itemDia != null)
        {
            try
            {
                var popup = new BuscarEjerciciosPopUp(dataService);
                _isPopupOpen = true;
                IPopupResult result = await this.ShowPopupAsync(popup, PopupOptions.Empty, CancellationToken.None);
                _isPopupOpen = false; // ← Popup cerrado

            }catch(Exception ex) { Debug.WriteLine($"{ex}"); }
            await Task.Delay(100);
            var excSeleccionados = dataService.ExercisesAgregarEjerciciosRutina.ToList();

            if (excSeleccionados.Count > 0)
            {                            
                await dataService.GuardarRutinaEjercicio(itemDia,excSeleccionados);
                dataService.ExercisesAgregarEjerciciosRutina.Clear();
            }                        
        }
        else
        {
            this.DisplayAlert("Seleccione un Dia", "Primero seleccione Semana y un Dia", "Ok");
        }        
    }
    private async void AñadirSerie_Clicked(object sender, EventArgs e)
    {
        var itemEjercicio = new RutinaEjercicio();
        if (sender is Button button)
        {
            itemEjercicio = button.BindingContext as RutinaEjercicio;
        }

        if (itemEjercicio != null)
        {
            await dataService.GuardarRutinaSerie(itemEjercicio);
            //OnPropertyChanged(nameof(itemEjercicio.SeriesObservable));
        }
    }
    private void CollectionSemanas_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {        
        if (e.CurrentSelection.FirstOrDefault() is RutinaSemana semana)
        {
            semana.DiasObservable = semana.Dias.ToObservableCollection();
            CollectionDias.ItemsSource = semana.DiasObservable;
        }
    }
    private void CollectionDias_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is RutinaDia dia)
        {
            dia.EjerciciosObservable = dia.Ejercicios.ToObservableCollection();
            CvEjercicios.ItemsSource = dia.EjerciciosObservable;    
        }        
    }        
    private async void Finalizar_Clicked(object sender, EventArgs e)
    {
        RutinaDia dia = CollectionDias.SelectedItem as RutinaDia;
        if (SerieActual == null)
        {            
            bool resul = await DisplayAlert("Finalizar","¿ Desea Finalizar la rutina y guardarla ?", "Guardar", "Cancelar");            
            if(resul && dia != null)
            {
                TotalTimer.Stop();
                RestTimer.Stop();
                ctsTotalTimer.Cancel();                                                
                dia.Completado = true;                                
                await Shell.Current.GoToAsync("///MainPage");
            }
        }
        else {await DisplayAlert("Termine el Ejercicio", "Termine y Guarde el Ejercicio antes de Finalizar", "OK"); }
    }

    #region Vibrar
    private void OnSerieDescansoTerminado(object sender, EventArgs e)
    {
        if (sender is RutinaSeries serie)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await MostrarAlertaDescansoTerminado();
            });
        }
    }
    private async Task MostrarAlertaDescansoTerminado()
    {
        CancellationTokenSource vibrationCts = new CancellationTokenSource();
        //Vobrar solo en Android
        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            _ = Task.Run(async () =>
            {
                if (Vibration.Default.IsSupported)
                {
                    while (!vibrationCts.IsCancellationRequested)
                    {                   
                        Vibration.Default.Vibrate();
                        await Task.Delay(1000,vibrationCts.Token);
                    }
                }
            },vibrationCts.Token);
            
        }

        //Mostrar Alerta de MAUI
        await DisplayAlert("¡Descanso Terminado!", "El tiempo de descanso ha finalizado. Presiona OK para continuar.", "OK");

        vibrationCts.Cancel();
    }       
    
    #endregion
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        #region Abierto Por PopUp?
        if (_isPopupOpen)
        {            
            return;
        }
        #endregion

        RutinaActual = await dataService.ObtenerRutinaActualyUI(RutinaId);
        WorkoutDayActual = await dataService.ObtenerWorkutDayActual();
        dataService.SetIntIdAgregarRutinaPopUp(RutinaId); // lo guardo por si hay popups


        #region Guardar Descanso en TempDescanso
        await dataService.DebugDescansoEnBD();
        var todasSeries = RutinaActual.SemanasObservable
                .SelectMany(s => s.DiasObservable)
                .SelectMany(d => d.EjerciciosObservable)
                .SelectMany(e => e.SeriesObservable)
                .ToList();
        foreach (var serie in todasSeries)
        {
            Debug.WriteLine($"Serie {serie.RutinaSeriesId}: " +
                   $"Descanso es null? {serie.Descanso == null}, " +
                   $"Valor: {serie.Descanso}");                      

            serie.TempDescanso = serie.Descanso;
            serie.DescansoTerminado += OnSerieDescansoTerminado;
        }
        #endregion

        #region Comprobar Semana y Dia No Completado
        var semanaNoCompletada = RutinaActual.Semanas.FirstOrDefault(semana => !semana.Dias.All(dia => dia.Completado));
        CollectionSemanas.SelectedItem = semanaNoCompletada;

        var diaNoCompletada = semanaNoCompletada?.Dias.FirstOrDefault(dia => !dia.Completado);
        CollectionDias.SelectedItem = diaNoCompletada;
        #endregion

        BindingContext = this;
    }
    protected override void OnDisappearing()
    {
        dataService.ClearIntIdAgregarRutinaPopUp();
    }
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {               

        if (sender is Microsoft.Maui.Controls.Grid grid)
        {
            CollectionView CvSeries = grid.FindByName("CvSeries") as CollectionView;
            Button AñadirSeriesButton = grid.FindByName("AñadirSerieButton") as Button;
            
            if (CvSeries.IsVisible)
            {
                await CvSeries.FadeTo(0, 250);
                CvSeries.IsVisible = false;
                AñadirSeriesButton.IsVisible = false;
            }
            else
            {
                AñadirSeriesButton.IsVisible = true;
                CvSeries.IsVisible = true;
                await CvSeries.FadeTo(1, 250);
            }
        }
    }

    
}
public class TupleConverter : IMultiValueConverter
{
    public object? Convert(object[] value, Type targetType, object? parameter, CultureInfo culture)
    {                        
        if (value[0] is RutinaSeries && value[1] is RutinaEjercicio )
        {
            return new ValueTuple<RutinaSeries, RutinaEjercicio>((RutinaSeries)value[0], (RutinaEjercicio)value[1]);
        }
        Debug.WriteLine(value?.GetType());
        return null;
    }

    public object[] ConvertBack(object? value, Type[] targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

}