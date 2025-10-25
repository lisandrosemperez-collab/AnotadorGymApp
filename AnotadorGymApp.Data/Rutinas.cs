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
        public ICollection<RutinaSemana> Semanas { get; set; } = new List<RutinaSemana>();        
        public ObservableCollection<RutinaSemana> SemanasObservable { get; set; } = new ObservableCollection<RutinaSemana>();
        #endregion

        #region PROPS
        private string? imageSource;
        private string nombre = string.Empty;
        private string? descripcion;
        private bool activa;
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public bool Activa { get { return activa; }set { activa = value; } }
        public string? ImageSource {get {return imageSource; } set { imageSource = value; } }
        public string Nombre { get { return nombre; } set { nombre = value;OnPropertyChanged(nameof(Nombre)); } }
        public string? Descripcion { get { return descripcion; } set { descripcion = value; } }
        #endregion        
    }    
    public class RutinaSemana : INotifyPropertyChanged
    {
        public int SemanaId { get; set; }
        public int RutinaId { get; set; }
        public Rutinas Rutina { get; set; }
        public ICollection<RutinaDia> Dias { get; set; } = new List<RutinaDia>();
        [NotMapped]
        public ObservableCollection<RutinaDia> DiasObservable { get; set; } = new ObservableCollection<RutinaDia>();
        public string NombreSemana { get; set; } = string.Empty;
        private bool _isExpanded;
        [NotMapped]
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged(nameof(IsExpanded));
                }
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    public class RutinaDia : INotifyPropertyChanged
    {             
        public int DiaId { get; set; }
        public int RutinaId { get; set; }
        public int SemanaId { get; set; }
        public RutinaSemana Semana { get; set; }
        public bool Completado { get; set; }
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
        public int RutinaId { get; set; }
        public int SemanaId { get; set; }
        public Exercise Exercise { get; set; }         
        public RutinaDia Dia { get; set; }        
        public ICollection<RutinaSeries> Series { get; set; } = new List<RutinaSeries>();
        [NotMapped]
        public ObservableCollection<RutinaSeries> SeriesObservable { get; set; } = new ObservableCollection<RutinaSeries>();        

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
        public int RutinaId { get; set; }
        public int SemanaId { get; set; }
        public int DiaId { get; set; }
        public int RutinaSeriesId { get; set; }
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
