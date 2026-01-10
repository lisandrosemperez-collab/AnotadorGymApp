using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;


namespace AnotadorGymApp.Data
{
    public class Rutinas : INotifyPropertyChanged
    {
        #region Gets
        [NotMapped]
        public int? CountDiasPrimeraSemana => Semanas?.FirstOrDefault()?.Dias?.Count ?? 0;
        [NotMapped]
        public int? CountDiasCompletados => Semanas?.FirstOrDefault()?.Dias?.Count(d => d.Completado) ?? 0;
        [NotMapped]
        public int? CountDiasTotal => Semanas?.Sum(s => s.Dias.Count) ?? 0;
        [NotMapped]
        public double? ProgresoRutina => (CountDiasTotal ?? 0) == 0 ? 0 : (double)(CountDiasCompletados ?? 0) / CountDiasTotal;
        #endregion

        #region EF
        public int RutinaId { get; set; }
        public bool Completado { get; set; } = false;
        public ICollection<RutinaSemana> Semanas { get; set; } = new List<RutinaSemana>();
        [NotMapped]
        private ObservableCollection<RutinaSemana> _semanasObservable = new ObservableCollection<RutinaSemana>();
        [NotMapped]
        public ObservableCollection<RutinaSemana> SemanasObservable
        {
            get => _semanasObservable;
            set
            {
                if (_semanasObservable == value) return;
                
                if (_semanasObservable != null)
                {
                    _semanasObservable.CollectionChanged -= OnSemanasCollectionChanged;
                }

                _semanasObservable = value;

                if (_semanasObservable != null)
                {
                    _semanasObservable.CollectionChanged += OnSemanasCollectionChanged;
                    RecalcularSemanaIdsUI();
                }
            }
        }
        private void OnSemanasCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {            
            RecalcularSemanaIdsUI();
        }
        private void RecalcularSemanaIdsUI()
        {
            if (_semanasObservable == null) return;

            for (int i = 0; i < _semanasObservable.Count; i++)
            {
                _semanasObservable[i].SemanaIdUI = i + 1;
            }
        }
        #endregion

        #region PROPS
        private string? imageSource;
        private string nombre = string.Empty;
        private string? descripcion;
        private bool activa;
        private string? tiempoPorSesion = string.Empty;
        private string? dificultad = string.Empty;
        private string? frecuenciaPorGrupo = string.Empty;
        public bool Activa { get { return activa; }set { activa = value; } }
        public string? ImageSource {get {return imageSource; } set { imageSource = value; OnPropertyChanged(nameof(ImageSource)); } }
        public string Nombre { get { return nombre; } set { nombre = value;OnPropertyChanged(nameof(Nombre)); } }
        public string? Descripcion { get { return descripcion; } set { descripcion = value; OnPropertyChanged(nameof(Descripcion)); } }
        public string? TiempoPorSesion { get { return tiempoPorSesion; } set { tiempoPorSesion = value; OnPropertyChanged(nameof(TiempoPorSesion)); } }
        public string? Dificultad { get { return dificultad; } set { dificultad = value; OnPropertyChanged(nameof(Dificultad)); } }
        public string? FrecuenciaPorGrupo { get { return frecuenciaPorGrupo; } set { frecuenciaPorGrupo = value; OnPropertyChanged(nameof(FrecuenciaPorGrupo)); } }
        #endregion                
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }    
    public class RutinaSemana : INotifyPropertyChanged
    {
        public int SemanaId { get; set; }
        public int RutinaId { get; set; }
        public Rutinas Rutina { get; set; }
        public ICollection<RutinaDia> Dias { get; set; } = new List<RutinaDia>();
        public bool Completado { get; set; } = false;
        [NotMapped]
        private ObservableCollection<RutinaDia> _diasObservable = new ObservableCollection<RutinaDia>();
        [NotMapped]
        public ObservableCollection<RutinaDia> DiasObservable
        {
            get => _diasObservable;
            set
            {
                if (_diasObservable == value) return;

                if (_diasObservable != null)
                {
                    _diasObservable.CollectionChanged -= OnDiasCollectionChanged;
                }

                _diasObservable = value;

                if (_diasObservable != null)
                {
                    _diasObservable.CollectionChanged += OnDiasCollectionChanged;
                    RecalcularDiaIdsUI(); // ¡Aquí se dispara el cálculo!
                }
            }
        }
        private void OnDiasCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {            
            RecalcularDiaIdsUI();
        }
        private void RecalcularDiaIdsUI()
        {
            if (_diasObservable == null) return;

            for (int i = 0; i < _diasObservable.Count; i++)
            {
                _diasObservable[i].DiaIdUI = i + 1;                
                _diasObservable[i].NombreRutinaDia = $"Día {i + 1}";
            }
        }
        public string NombreSemana { get; set; } = string.Empty;
        
        [NotMapped]
        private bool seleccionado;
        [NotMapped]
        public bool Seleccionado
        {
            get => seleccionado;
            set
            {
                if (seleccionado != value)
                {
                    seleccionado = value;
                    OnPropertyChanged(nameof(Seleccionado));
                }
            }
        }        
        public int? SemanaIdUI { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    public class RutinaDia : INotifyPropertyChanged
    {             
        public int DiaId { get; set; }        
        public int DiaIdUI { get; set; }        
        public int SemanaId { get; set; }
        public RutinaSemana Semana { get; set; }
        public bool Completado { get; set; } = false;
        public string NombreRutinaDia { get; set; } = string.Empty;
        public string Notas { get; set; } = string.Empty;                
        public ICollection<RutinaEjercicio> Ejercicios { get; set; } = new List<RutinaEjercicio>();
        [NotMapped]
        public ObservableCollection<RutinaEjercicio> EjerciciosObservable { get; set; } = new ObservableCollection<RutinaEjercicio>();
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    }
    public class RutinaEjercicio : INotifyPropertyChanged
    {
        private bool completado;
        public bool Completado 
        { 
            get => completado;
            set
            {
                if (value != completado) { completado = value; OnPropertyChanged(nameof(Completado)); }
            }
        }
        public int EjercicioId { get; set; }
        public int ExerciseId { get; set; }
        public int DiaId { get; set; }                
        public Exercise Exercise { get; set; }         
        public RutinaDia Dia { get; set; }        
        public ICollection<RutinaSeries> Series { get; set; } = new List<RutinaSeries>();
        [NotMapped]
        private ObservableCollection<RutinaSeries> seriesObservable = new ObservableCollection<RutinaSeries>();
        [NotMapped]
        public ObservableCollection<RutinaSeries> SeriesObservable
        {
            get => seriesObservable;
            set
            {
                if (seriesObservable == value) return;

                if (seriesObservable != null)
                {
                    seriesObservable.CollectionChanged -= OnSeriesCollectionChanged;
                }

                seriesObservable = value;

                if (seriesObservable != null)
                {
                    seriesObservable.CollectionChanged += OnSeriesCollectionChanged;
                    RecalcularSeriesIdsUI(); // ¡Aquí se dispara el cálculo!
                }
            }
        }
        private void OnSeriesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Se dispara cuando se agregan/eliminan/reordenan días
            RecalcularSeriesIdsUI();
        }
        private void RecalcularSeriesIdsUI()
        {
            if (seriesObservable == null) return;

            for (int i = 0; i < seriesObservable.Count; i++)
            {
                seriesObservable[i].SerieIdUI = i + 1;                                
            }
        }

        [NotMapped]
        public int SeriesTotales => SeriesObservable?.Count ?? 0;
        [NotMapped]
        public int SeriesCompletadas => SeriesObservable?.Count(s => s.EstadoSerie == 3) ?? 0;
        [NotMapped]        
        public string ProgresoSeriesFormateado => $"📊 {SeriesCompletadas}/{SeriesTotales} series";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public class RutinaSeries : INotifyPropertyChanged
    {
        public event EventHandler DescansoTerminado;
        private TimeSpan? descanso = TimeSpan.Zero;      
        public TimeSpan? Descanso 
        {
            get => descanso;
            set
            {
                var newValue = value ?? TimeSpan.Zero;
                if (descanso != newValue)
                {
                    descanso = newValue;
                    OnPropertyChanged(nameof(Descanso));
                    Debug.WriteLine($"🔄 Descanso actualizado: {descanso}");
                }
            }
        }
        public int? Repeticiones { get; set; }        
        public int? Porcentaje1RM { get; set; }

        private TipoSerie tipo;
        public TipoSerie Tipo
        {
            get => tipo;
            set
            {
                if (tipo != value)
                {
                    tipo = value;
                    OnPropertyChanged(nameof(Tipo));                    
                }
            }
        }
        
        private int estadoSerie=1;        
        public int EstadoSerie 
        {
            get { return estadoSerie; }
            set
            {
                if (estadoSerie != value) { estadoSerie = value; OnPropertyChanged(nameof(EstadoSerie)); }
            }
        }  
        
        #region Entity
        public int EjercicioId { get; set; }        
        public int SerieId { get; set; }
        public RutinaEjercicio Ejercicio { get; set; }

        #endregion

        #region UI                
        [NotMapped]
        public int SerieIdUI { get; set; }
        [NotMapped]
        public double KilosTemp { get;set; }
        [NotMapped]
        public int RepsTemp { get; set; }        
        [NotMapped]
        public SetLog SetLog { get; set; } = new SetLog();
        //Esto es Solo para editar la Serie durante Ejecucion de los ejercicios

        #endregion
        
        #region Timer UI
        [NotMapped]
        private CancellationTokenSource? ctsTimer;

        [NotMapped]
        private TimeSpan? tempDescanso;
        [NotMapped]
        public TimeSpan? TempDescanso
        {
            get => tempDescanso;
            set
            {
                if (tempDescanso != value)
                {
                    tempDescanso = value;
                    OnPropertyChanged(nameof(TempDescanso));
                }
            }
        }
        public async void ComienzoRest()
        {                        

            try
            {
                // Verificar si ctsTimer ya existe
                if (ctsTimer != null)
                {
                    Debug.WriteLine($"[ComienzoRest] ctsTimer ya existe. Estado: {ctsTimer.Token.IsCancellationRequested}");
                    ctsTimer.Cancel();
                    ctsTimer.Dispose();
                }                

                ctsTimer = new CancellationTokenSource();

                var token = ctsTimer.Token;
                Debug.WriteLine($"[ComienzoRest] Token obtenido. Cancelado: {token.IsCancellationRequested}");

                while (tempDescanso?.TotalSeconds > 0)
                {
                    if (token.IsCancellationRequested) { Debug.WriteLine("[ComienzoRest] Token cancelado, saliendo del loop"); break; }
                    await Task.Delay(1000, token); // ← Usar el token aquí                    

                    // Reducir el tiempo en 1 segundo
                    TempDescanso = TempDescanso?.Subtract(TimeSpan.FromSeconds(1));
                    // Actualizar la UI
                    OnPropertyChanged(nameof(TempDescanso));
                    // Si llegó a 0, terminar
                    if(TempDescanso?.TotalSeconds <= 0)
                    {
                        Debug.WriteLine("[ComienzoRest] Temporizador completado");
                        OnDescansoTerminado();
                        break;
                    }                                        
                }
                Debug.WriteLine("[ComienzoRest] FIN - Loop terminado");
            }
            catch (TaskCanceledException tce)
            {
                Debug.WriteLine($"[ComienzoRest] TaskCanceledException: {tce.Message}");
            }
            catch (ObjectDisposedException ode)
            {
                Debug.WriteLine($"[ComienzoRest] ObjectDisposedException: {ode.Message}");
                Debug.WriteLine($"[ComienzoRest] StackTrace: {ode.StackTrace}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ComienzoRest] Exception: {ex.GetType().Name}");
                Debug.WriteLine($"[ComienzoRest] Message: {ex.Message}");
                Debug.WriteLine($"[ComienzoRest] StackTrace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"[ComienzoRest] Inner Exception: {ex.InnerException}");
                }
            }
            finally
            {
                Debug.WriteLine($"[ComienzoRest] Finally block ejecutado");
                ctsTimer.Dispose();
                ctsTimer = null;
            }
        }
        public void DetenerRest()
        {            
            Debug.WriteLine($"[DetenerRest] ctsTimer es null: {ctsTimer == null}");
            if (ctsTimer != null)
            {
                try
                {
                    Debug.WriteLine($"[DetenerRest] Antes de Cancel()");
                    ctsTimer.Cancel();
                    Debug.WriteLine($"[DetenerRest] Después de Cancel()");                    
                    //Descanso = tempDescanso;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DetenerRest] Error: {ex.Message}");
                }
                finally
                {                    
                    Debug.WriteLine($"[DetenerRest] ctsTimer seteado a null");
                }
                                
            }                                 
        }
        protected virtual void OnDescansoTerminado()
        {
            DescansoTerminado?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }        
    }   
    public enum TipoSerie
    {
        Normal,
        DropSet,
        Cluster,
        Myo_Reps,
        Negativas,
        Max_Rm
    }    
}
