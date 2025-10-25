using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace AnotadorGymApp.Data
{
    
    public class Exercise : INotifyPropertyChanged
    {
        #region EF                
        public ICollection<Muscle> secondaryMuscles { get; set; } = new List<Muscle>();
        public ICollection<ExerciseLog> ExerciseLogs { get; set; } = new List<ExerciseLog>();
        public ICollection<RutinaEjercicio> RutinasEjercicios { get; set; } = new List<RutinaEjercicio>();
        public Muscle primaryMuscle { get; set; }
        public int primaryMuscleId { get; set; }
        public BodyParts bodyPart { get; set; }
        public int bodyPartId { get; set; }             
        public Exercise() { }
        #endregion
        [NotMapped]
        public DataService dataService { get; set; }
        public Exercise(string nombre, BodyParts bodypart,Muscle primary,List<Muscle> secondary) 
        {
            name = nombre;
            bodyPart = bodypart; primaryMuscle = primary; secondaryMuscles = secondary;
        }        
        #region Propiedades
        
        private double? mejor;

        private double? ultimo;

        private double? iniciar;

        private string name;

        private int id;
        public int Id
        {
            get { return id; }
            set
            {
                if (id != value)
                {
                    id = value;
                    OnPropertyChanged(nameof(Id));
                }
            } 
        }        
        public string Name
        {
            get { return name; }
            set
            {
                if (name != value)
                {
                    name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }
        public double? Mejor
        {
            get { return mejor; }
            set
            {
                if (mejor != value)
                {
                    mejor = value;
                    OnPropertyChanged(nameof(Mejor));
                    _ = GuardarCambiosAsync(); // ← Usar discard operator
                }
            }
        }
        public double? Iniciar
        {
            get { return iniciar; }
            set
            {
                if (iniciar != value)
                {
                    iniciar = value;
                    OnPropertyChanged(nameof(Iniciar));
                    _ = GuardarCambiosAsync(); // ← Usar discard operator
                }
            }
        }
        public double? Ultimo
        {
            get { return ultimo; }
            set
            {
                if (ultimo != value)
                {
                    ultimo = value;
                    OnPropertyChanged(nameof(Ultimo));
                    _ = GuardarCambiosAsync(); // ← Usar discard operator
                }
            }
        }
        private async Task GuardarCambiosAsync()
        {
            try
            {
                await dataService._database.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error guardando progreso de Exercise: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
    public class WorkoutDay : INotifyPropertyChanged
    {
        [NotMapped]
        public double? VolumenTotal => ExerciseLogs.Sum(e => e.VolumenTotal);
        [NotMapped]
        public int? EjerciciosTotal => ExerciseLogs.Count();
        [NotMapped]
        public int? SeriesTotal => ExerciseLogs.Sum(e => e.TotalSeries);
        public int? DayId { get ; set ; } 
        public DateTime Date { get; set; }
        public ICollection<ExerciseLog> ExerciseLogs { get; set; } = new List<ExerciseLog>();
        #region Propiedades
        private double volumen;                              
        public WorkoutDay() { }                        
        public double Volumen { get { return volumen; } set { volumen = value; OnPropertyChanged(nameof(Volumen)); } }                     
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
    public class ExerciseLog
    {
        [NotMapped]
        public double? PesoMaximo => SetsLog?.Where(s => s.Tipo == TipoSerie.Normal || s.Tipo== TipoSerie.Max_Rm).Max(s => s.Kilos) ?? 0;
        [NotMapped]
        public double? VolumenTotal => SetsLog?.Sum(s => s.Kilos * s.Reps) ?? 0;
        [NotMapped]
        public int? TotalSeries => SetsLog?.Count();

        public int ExerciseLogId { get; set; }
        public int WorkoutDayId { get; set; }
        public WorkoutDay WorkoutDay { get; set; }
        public int ExerciseId { get; set; }
        public Exercise Exercise { get; set; }
        public ICollection<SetLog?> SetsLog { get; set; } = new List<SetLog>();

    }
    public class SetLog : INotifyPropertyChanged
    {
        #region Entity
        public SetLog() { }  
        public int SetLogId { get; set; }
        public int ExerciseLogId { get; set; }
        public ExerciseLog ExerciseLog { get; set; }
        #endregion

        #region Propiededes

        private double kilos;
        private int reps;                   
        public double Kilos { get { return kilos; } set { kilos = value; OnPropertyChanged(nameof(Kilos)); } }
        public int Reps { get { return reps; } set { reps = value; OnPropertyChanged(nameof(Reps)); } }
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
                    Debug.WriteLine(Tipo);
                }
            }
        }

        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
}

