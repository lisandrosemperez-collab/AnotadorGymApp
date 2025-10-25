using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AnotadorGymApp.Data
{    
    public class DataService : INotifyPropertyChanged
    {
        public int IdAgregarRutina {  get; set; }  = 0;
        public List<Exercise> ExercisesAgregarEjerciciosRutina { get; set; } = new List<Exercise>();
        public readonly DataBase _database;        
        public DataService (DataBase database) { _database = database; }
        public void Migrate()
        {
            _database.Database.Migrate();
        }

        #region DB
        public void AgregarEtiqueta(List<string> etiquetas)
        {
            foreach (string nombre in etiquetas)
            {
                _database.BodyParts.Add(new BodyParts(nombre));
            }
            _database.SaveChanges();
        }
        public void AgregarRutina (List<Rutinas> rutinas)
        {
            _database.Rutinas.AddRange(rutinas);
            _database.SaveChanges();
        }
        public void EliminarRutina(Rutinas rutina)
        {
            _database.Rutinas.Remove(rutina);
            _database.SaveChanges();
        }
        public void VaciarTabla<T>() where T : class
        {
            _database.Set<T>().RemoveRange(_database.Set<T>());
            _database.SaveChanges();
        }
        #endregion

        #region SplashScreen

        private double _progreso = 0;
        public double Progreso
        {
            get => _progreso;
            set
            {
                if (_progreso != value)
                {
                    _progreso = value;
                    OnPropertyChanged();
                }
            }
        }
        public async Task IniciarDatos(List<ExerciseJson> datos)
        {            
            Debug.WriteLine("Inicio de CargarDatosInicialesAsync");

            //TODAVIA SIN PROBAR
            try
            {
                var allBodyParts = await _database.BodyParts.ToListAsync();
                var allMuscles = await _database.Muscles.ToListAsync();
                var allExercises = await _database.Exercises.Select(e => e.Name.ToLower()).ToListAsync();
                int totalEjercicios = datos.Count;
                double iteracion = 0;

                foreach (var ex in datos)
                {

                    #region BodyPart
                    if (allExercises.Contains(ex.name.ToLower())) continue;

                    var bodypart = allBodyParts
                        .FirstOrDefault(b => b.BodyPart.ToLower() == ex.bodyPart.ToLower());

                    if (bodypart == null)
                    {
                        bodypart = new BodyParts() { BodyPart = ex.bodyPart };
                        _database.BodyParts.Add(bodypart);
                        allBodyParts.Add(bodypart);                        
                    }
                    #endregion

                    #region PrimaryMuscle
                    var primaryMuscle = allMuscles
                        .FirstOrDefault(p => p.Name.ToLower() == ex.primaryMuscle.ToLower());
                                        
                    if (primaryMuscle == null)
                    {
                        primaryMuscle = new Muscle() { Name = ex.primaryMuscle };
                        _database.Muscles.Add(primaryMuscle);
                        allMuscles.Add(primaryMuscle);
                    }
                    #endregion

                    #region SecondaryMuscles (lista)
                    var secondaryMuscleList = new List<Muscle>();
                    foreach (var sec in ex.secondaryMuscles)
                    {
                        var secondary = allMuscles
                            .FirstOrDefault(s => s.Name.ToLower() == sec.ToLower());
                                        
                        if (secondary == null)
                        {
                            secondary = new Muscle() { Name = sec };
                            allMuscles.Add(secondary);
                            _database.Muscles.Add(secondary);                            
                        }

                        secondaryMuscleList.Add(secondary);
                    }
                    #endregion

                    #region Exercise
                    var exist = await _database.Exercises.AnyAsync(e => e.Name == ex.name);
                    if (exist)
                        continue;
                    var exercise = new Exercise
                    {
                        Name = ex.name,
                        primaryMuscle = primaryMuscle,
                        secondaryMuscles = secondaryMuscleList,
                        bodyPart = bodypart
                    };
                    #endregion

                    _database.Exercises.Add(exercise);

                    if (iteracion % 100 == 0)
                    {
                        Progreso = Math.Round((double)(iteracion + 1) / datos.Count, 1);
                    }
                    iteracion++;
                }                
                await _database.SaveChangesAsync();
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"Error: {ex}");
            }
            await _database.SaveChangesAsync();
            Debug.WriteLine("Cantidad de Ejercicios:");
            int count = await _database.Exercises.CountAsync();
            Debug.WriteLine(count);
            Debug.WriteLine("Fin de CargarDatosInicialesAsync");
        }

        #region WorkoutDaysPrueba
        public async Task IniciarWorkutDaysPrueba(List<WorkoutDay> workoutDaysTemp)
        {
            var exercise = _database.Exercises.FirstOrDefault(e => e.Name == "Curva lateral de 45°");
            var exercise1 = _database.Exercises.FirstOrDefault(e => e.Name == "curl con barra");
            var exercise2 = _database.Exercises.FirstOrDefault(e => e.Name == "curl de arrastre con barra");
            var mitad = workoutDaysTemp.Count / 3;

            List<WorkoutDay> primeraMitad = workoutDaysTemp.Take(mitad).ToList();
            List<WorkoutDay> segundaMitad = workoutDaysTemp.Skip(mitad).Take(mitad).ToList();
            List<WorkoutDay> terceraMitad = workoutDaysTemp.Skip(mitad * 2).ToList();

            Debug.WriteLine($"📊 Dividido: {primeraMitad.Count} + {segundaMitad.Count} = {workoutDaysTemp.Count} días");

            if (exercise == null || exercise1 == null)
            {
                Debug.WriteLine($"❌ Ejercicios no encontrados: 'Curva lateral de 45°'={exercise == null}, 'curl con barra'={exercise1 == null}");
                return;
            }

            await Task.WhenAll(AgregarMitades(primeraMitad, exercise), AgregarMitades(segundaMitad, exercise1), AgregarMitades(segundaMitad, exercise1));
        }
        private async Task AgregarMitades(List<WorkoutDay> mitad,Exercise exercise)
        {
            foreach (var WorkoutDayActualTemp in mitad)
            {
                try
                {
                    var workoutDay = await _database.WorkoutDay.FirstOrDefaultAsync(w => w.Date == WorkoutDayActualTemp.Date.Date);
                    if (workoutDay != null)
                    {
                        _database.WorkoutDay.Remove(workoutDay);
                        await _database.SaveChangesAsync();
                    }

                    workoutDay = new WorkoutDay
                    {
                        Date = WorkoutDayActualTemp.Date,
                    };
                    _database.WorkoutDay.Add(workoutDay);
                    await _database.SaveChangesAsync();
                    Debug.WriteLine($"✅ Nuevo WorkoutDay creado: {workoutDay.Date:dd/MM/yyyy} (ID: {workoutDay.DayId})");


                    var exerciseLog = await _database.ExercisesLogs.FirstOrDefaultAsync(e => e.WorkoutDayId == workoutDay.DayId && e.ExerciseId == exercise.Id);

                    if (exerciseLog == null)
                    {
                        exerciseLog = new ExerciseLog()
                        {
                            Exercise = exercise,
                            ExerciseId = exercise.Id,
                        };
                        workoutDay.ExerciseLogs.Add(exerciseLog);
                        Debug.WriteLine($"✅ Nuevo ExerciseLog creado para {exercise.Name}");
                    }
                    else
                    {
                        Debug.WriteLine($"✅ ExerciseLog existente para {exercise.Name}");
                    }


                    foreach (var setLogTemp in WorkoutDayActualTemp.ExerciseLogs.First().SetsLog)
                    {
                        var setLog = new SetLog()
                        {
                            Kilos = setLogTemp.Kilos,
                            Reps = setLogTemp.Reps,
                            Tipo = setLogTemp.Tipo,
                        };
                        exerciseLog.SetsLog.Add(setLog);
                    }

                    await _database.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error: {ex}");

                }
            }
        }

        #endregion

        #endregion

        public async Task DebugDescansoEnBD()
        {
            var connection = _database.Database.GetDbConnection();
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT RutinaSeriesId, Descanso FROM RutinaSeries";

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var id = reader.GetInt32(0);
                    var descanso = reader.IsDBNull(1) ? "NULL" : reader.GetString(1);
                    Debug.WriteLine($"📀 BD: Serie {id} - Descanso = '{descanso}'");
                }
            }

            command.CommandText = "SELECT RutinaSeriesId, Descanso, typeof(Descanso) as TipoDato FROM RutinaSeries";

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var id = reader.GetInt32(0);
                    var descansoRaw = reader.IsDBNull(1) ? "NULL" : reader.GetString(1);
                    var tipoDato = reader.GetString(2);

                    Debug.WriteLine($"  Serie {id}: DescansoRaw='{descansoRaw}', TipoDato={tipoDato}");
                }
            }


            await connection.CloseAsync();


        }

        #region GETSRutinas
        public async Task<Rutinas> ObtenerRutinaActual(int id){
            var rutina = await _database.Rutinas
                .Where(r => r.RutinaId == id)
                    .AsSplitQuery()        
                        .Include(r => r.Semanas)
                            .ThenInclude(r => r.Dias)
                                .ThenInclude(r => r.Ejercicios)
                                    .ThenInclude(r => r.Series)
                        .Include(r => r.Semanas)
                            .ThenInclude(s => s.Dias)
                                .ThenInclude(d => d.Ejercicios)
                                    .ThenInclude(e => e.Exercise)
                                        .FirstOrDefaultAsync();
            return rutina;
        }
        public async Task<Rutinas> ObtenerRutinaActiva()
        {
            var rutina = await _database.Rutinas
                .Where(r => r.Activa == true)
                    .AsSplitQuery()
                        .Include(r => r.Semanas)
                            .ThenInclude(r => r.Dias)
                                .AsNoTracking()
                                    .FirstOrDefaultAsync();
            return rutina;
        }
        public async Task<Rutinas> VerificarSiHayRutinaActiva()
        {
            return await _database.Rutinas
                        .FirstOrDefaultAsync(r => r.Activa == true);
        }
        public async Task<List<Rutinas>> ObtenerRutinas()
        {
            var Rutinas = new List<Rutinas>();
            if (_database.Rutinas != null)
            {
                Rutinas = await _database.Rutinas
                    .AsSplitQuery()               
                        .Include(r => r.Semanas)
                            .ThenInclude(r => r.Dias)
                                .ThenInclude(r => r.Ejercicios)
                                    .ThenInclude(r => r.Series)
                        .Include(r => r.Semanas)
                            .ThenInclude(s => s.Dias)
                                .ThenInclude(d => d.Ejercicios)
                                    .ThenInclude(e => e.Exercise)
                        .ToListAsync();                
            }
            return Rutinas;
        }
        public async Task<List<Exercise>> ObtenerEjercicios()
        {
            return await _database.Exercises.ToListAsync();
        }
        public async Task<Rutinas> ObtenerRutinaActualyUI(int id)
        {
            Rutinas rutina = await ObtenerRutinaActual(id);
            if (rutina == null) return null;            

            rutina.SemanasObservable = new ObservableCollection<RutinaSemana>(rutina.Semanas);
            foreach (var semana in rutina.Semanas)
            {
                semana.DiasObservable = new ObservableCollection<RutinaDia>(semana.Dias);
                foreach (var dia in semana.Dias)
                {
                    dia.EjerciciosObservable = new ObservableCollection<RutinaEjercicio>(dia.Ejercicios);
                    foreach (var ejercicio in dia.Ejercicios)
                    {                        
                        ejercicio.SeriesObservable = new ObservableCollection<RutinaSeries>(ejercicio.Series);                        
                    }
                }
            }
            return rutina;
        }        
        public async Task<WorkoutDay> ObtenerWorkutDayActual()
        {
            var diaActual = DateTime.Today;

            var workoutDay = await _database.WorkoutDay
                .Include(w => w.ExerciseLogs)
                    .ThenInclude(e => e.SetsLog)
                .Include(w => w.ExerciseLogs)
                    .ThenInclude(e => e.Exercise)
                .FirstOrDefaultAsync(w => w.Date.Date == diaActual);

            if (workoutDay is not null)
                return workoutDay;

            // Si no existe, lo creo
            workoutDay = new WorkoutDay
            {
                Date = diaActual
            };

            _database.WorkoutDay.Add(workoutDay);
            await _database.SaveChangesAsync();

            return workoutDay;
        }
        #endregion

        #region GETSExercises
        public async Task<List<Exercise>> ObtenerEjerciciosRecientes7Dias()
        {
            var fechaLimite = DateTime.Today.AddDays(-7);

            var exerciseRecientes = await _database.Exercises.Where(e => e.ExerciseLogs.Any(e => e.WorkoutDay.Date >= fechaLimite)).ToListAsync();
            return exerciseRecientes;
        }
        public async Task<ObservableCollection<Exercise>> ObtenerEjerciciosRecientes30Dias()
        {
            try
            {
                var fechaLimite = DateTime.Today.AddDays(-30);

                var exercises = await _database.Exercises
                    .Where(e => e.ExerciseLogs.Any(log => log.WorkoutDay.Date >= fechaLimite))                    
                        .Include(e => e.ExerciseLogs)
                            .ThenInclude(log => log.WorkoutDay)
                        .Include(e => e.ExerciseLogs)
                            .ThenInclude(log => log.SetsLog)
                    .AsSplitQuery()
                    .OrderByDescending(e => e.ExerciseLogs.Max(log => log.WorkoutDay.Date))
                    .Take(10)
                    .ToListAsync();

                return new ObservableCollection<Exercise>(exercises);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error en ObtenerEjerciciosRecientes30Dias: {ex.Message}");
                return new ObservableCollection<Exercise>();
            }
        }

        #endregion

        #region SETS

        public async Task GuardarRutinaSerie (RutinaEjercicio itemEjercicio)
        {
            if (itemEjercicio != null)
            {
                int serieId = itemEjercicio.Series.Any() ?
                    itemEjercicio.Series.Max(e => e.RutinaSeriesId) + 1 : 1;

                var RutinaSerie = new RutinaSeries()
                {
                    Repeticiones = 10,
                    Descanso = TimeSpan.Zero,
                    Tipo = TipoSerie.Normal,
                    SerieIdUI = serieId,
                    Ejercicio = itemEjercicio,
                    RutinaSeriesId = serieId,
                    EjercicioId = itemEjercicio.EjercicioId,
                    DiaId = itemEjercicio.DiaId,
                    RutinaId = itemEjercicio.RutinaId,
                    SemanaId = itemEjercicio.SemanaId
                };
                itemEjercicio.Series.Add(RutinaSerie);
                itemEjercicio.SeriesObservable.Add(RutinaSerie);
                await _database.SaveChangesAsync();
                //USO DEL METODO RELOAD PARA ASEGURARSE DE QUE SE USE EL TIMESPANCONVERTER
                await _database.Entry(RutinaSerie).ReloadAsync();
            }
        }
        public async Task GuardarRutinaDia (RutinaSemana semana,Rutinas rutinaActual)
        {
            int diaId = semana.Dias.Any() ?
            semana.Dias.Max(d => d.DiaId) + 1 : 1;
            var nuevodia = new RutinaDia()
            {
                RutinaId = rutinaActual.RutinaId,
                SemanaId = semana.SemanaId,
                Semana = semana,
                DiaId = diaId
            };
            semana.Dias.Add(nuevodia);
            semana.DiasObservable.Add(nuevodia);
            await _database.SaveChangesAsync();
        }
        public async Task GuardarRutinaEjercicio (RutinaDia itemDia, List<Exercise> exercises)
        {
            if (exercises != null && itemDia != null)
            {
                foreach (var exercise in exercises)
                {
                    if (exercise != null)
                    {
                        int ejercicioId = itemDia.Ejercicios.Any() ?
                            itemDia.Ejercicios.Max(e => e.EjercicioId) + 1 : 1;

                        var RutinaEjercicio = new RutinaEjercicio() 
                        { 
                            ExerciseId = exercise.Id,
                            Dia = itemDia,
                            EjercicioId = ejercicioId,
                            DiaId = itemDia.DiaId,
                            RutinaId = itemDia.RutinaId,
                            SemanaId = itemDia.SemanaId 
                        };
                        itemDia.Ejercicios.Add(RutinaEjercicio);
                        itemDia.EjerciciosObservable.Add(RutinaEjercicio);
                    }
                }
                await _database.SaveChangesAsync();
                //COMPROBADO CON UN FOREACH QUE SE GUARDAN LAS REFERENCIAS A EXERCISE
            }
        }
        public async Task EliminarRutinaEjercicio (RutinaEjercicio ejercicio)
        {
            if (ejercicio == null || ejercicio.Dia == null) return;
            foreach (var serie in ejercicio.Series.ToList())
            {
                _database.Remove(serie);
            }
            var dia = ejercicio.Dia;
            dia?.Ejercicios.Remove(ejercicio);
            dia?.EjerciciosObservable.Remove(ejercicio);
            _database.Remove(ejercicio);
            await _database.SaveChangesAsync();
        }
        public async Task EliminarRutinaDia(RutinaDia dia)
        {
            if (dia == null || dia.Semana == null) { return; }

            foreach (var ejercicio in dia.Ejercicios.ToList())
            {
                foreach (var serie in ejercicio.Series.ToList())
                {
                    _database.Remove(serie);
                }
                _database.Remove(ejercicio);
            }
            var semana = dia.Semana;
            semana.DiasObservable.Remove(dia);
            semana.Dias.Remove(dia);
            _database.Remove(dia);
            await _database.SaveChangesAsync();
        }
        public async Task EliminarRutinaSerie(RutinaSeries? serie)
        {
            if (serie == null || serie.Ejercicio == null) { return; }
            var ejercicioPadre = serie.Ejercicio;
            ejercicioPadre.Series.Remove(serie);
            ejercicioPadre.SeriesObservable.Remove(serie);
            _database.Remove(serie);
            await _database.SaveChangesAsync();
        }

        #endregion

        #region RUTINAS PAGE
        public async Task SetIntIdAgregarRutinaPopUp(int id)
        {
            IdAgregarRutina = id;
        }
        public int GetIntIdAgregarRutinaPopUp()
        {
            return IdAgregarRutina;
        }
        public void ClearIntIdAgregarRutinaPopUp()
        {
            IdAgregarRutina = 0;
        }        
        public void AgregarIdsEjercicios(List<Exercise> exc)
        {
            ExercisesAgregarEjerciciosRutina = exc ?? new List<Exercise>();
        }
        #endregion

        #region Notify
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        #endregion

        #region MetricasPage
        public async Task<ObservableCollection<Exercise>> FiltrarEjercicios(string ejercicioBuscado, string filtroTiempoSeleccionado)
        {
            var ejerciciosFiltrados = new List<Exercise>();
            
            try
            {
                var consulta = _database.Exercises.Include(e => e.ExerciseLogs)
                                                    .ThenInclude(exLog => exLog.SetsLog)
                                                .Include(e => e.ExerciseLogs)
                                                    .ThenInclude(exLog => exLog.WorkoutDay)                                                
                                                    .AsQueryable();
            
                if(string.IsNullOrWhiteSpace(ejercicioBuscado))
                {                
                    consulta = consulta.Where(e => e.ExerciseLogs.Any(log => log.SetsLog.Any()));
                }
                else
                {
                    consulta = consulta.Where(e => e.Name.ToLower().Contains(ejercicioBuscado.ToLower()) && e.ExerciseLogs.Any(exLog => exLog.SetsLog.Any()));
                }

                ejerciciosFiltrados = await consulta.ToListAsync();

                ejerciciosFiltrados = filtroTiempoSeleccionado switch
                {
                    "Semana" => ejerciciosFiltrados.Where(e => TieneDatosRecientes(e, 7)).ToList(),
                    "Mes" => ejerciciosFiltrados.Where(e => TieneDatosRecientes(e, 30)).ToList(),
                    "3 Meses" => ejerciciosFiltrados.Where(e => TieneDatosRecientes(e, 90)).ToList(),
                    _ => ejerciciosFiltrados
                };
                
            }catch(Exception ex) { Debug.WriteLine(ex); }
            return new ObservableCollection<Exercise>(ejerciciosFiltrados);
        }
        private bool TieneDatosRecientes(Exercise e, int dias)
        {
            var fechaLimite = DateTime.Today.AddDays(-dias);
            return e.ExerciseLogs.Any(log => log.WorkoutDay.Date >= fechaLimite) == true;                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        
        }
        #endregion
    }
}
