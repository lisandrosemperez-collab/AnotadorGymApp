using AnotadorGymApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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
        #endregion

        #region ConfigPage        
        public async Task IniciarDatosExercises(List<ExerciseJson> exercises)
        {
            try
            {
                // 1. Cargar datos existentes UNA vez
                var existingBodyParts = await _database.BodyParts
                    .ToDictionaryAsync(b => b.BodyPart.ToLower(), b => b);

                var existingMuscles = await _database.Muscles
                    .ToDictionaryAsync(m => m.Name.ToLower(), m => m);

                var existingExerciseNames = await _database.Exercises
                    .Select(e => e.Name.ToLower())
                    .ToHashSetAsync();

                var nuevosExercises = new List<Exercise>();
                var ejerciciosProcesados = 0;
                var ejerciciosDuplicados = 0;

                // 3. Procesar en memoria
                foreach (var ex in exercises)
                {
                    try
                    {
                        // Validación rápida
                        if (string.IsNullOrWhiteSpace(ex.Name))
                        {
                            Debug.WriteLine($"⚠️ Ejercicio sin nombre - Saltando");
                            continue;
                        }

                        var nombreLower = ex.Name.Trim().ToLower();

                        // Verificar si ya existe
                        if (existingExerciseNames.Contains(nombreLower))
                        {
                            ejerciciosDuplicados++;
                            continue;
                        }

                        // BodyPart
                        var bodyPartKey = ex.bodyPart?.Trim().ToLower();
                        BodyParts? bodypart = null;

                        if (!string.IsNullOrWhiteSpace(ex.bodyPart))
                        {
                            var key = ex.bodyPart.Trim().ToLower();
                            if (!existingBodyParts.TryGetValue(key, out BodyParts? bodyPartDb))
                            {
                                // Crear nuevo
                                bodyPartDb = new BodyParts { BodyPart = ex.bodyPart.Trim() };
                                existingBodyParts[key] = bodyPartDb; // Agregar al diccionario
                            }
                            bodypart = bodyPartDb;
                        }

                        // Primary Muscle                        
                        Muscle? primaryMuscle = null;
                        
                        if (ex.primaryMuscle != null && !string.IsNullOrWhiteSpace(ex.primaryMuscle.Name.Trim().ToLower()))
                        {
                            var key = ex.primaryMuscle.Name.Trim().ToLower();
                            if (!existingMuscles.TryGetValue(key, out primaryMuscle))
                            {
                                primaryMuscle = new Muscle { Name = ex.primaryMuscle.Name.Trim() };
                                existingMuscles[key] = primaryMuscle;
                            }
                        }

                        // Secondary Muscles
                        var secondaryMuscleList = new List<Muscle>();

                        if (ex.secondaryMuscles != null)
                        {
                            foreach (var sec in ex.secondaryMuscles.Where(s => !string.IsNullOrWhiteSpace(s?.Name)))
                            {
                                var secKey = sec.Name.Trim().ToLower();

                                if (!existingMuscles.TryGetValue(secKey, out var secondary))
                                {
                                    secondary = new Muscle { Name = sec.Name.Trim() };
                                    existingMuscles[secKey] = secondary;
                                }
                                secondaryMuscleList.Add(secondary);
                            }
                        }

                        // Crear Exercise
                        var exercise = new Exercise
                        {
                            Name = ex.Name.Trim(),
                            primaryMuscle = primaryMuscle,
                            bodyPart = bodypart,                            
                        };

                        // Agregar músculos secundarios
                        foreach (var secMuscle in secondaryMuscleList)
                        {
                            exercise.secondaryMuscles.Add(secMuscle);
                        }

                        nuevosExercises.Add(exercise);
                        existingExerciseNames.Add(nombreLower);
                    }
                    catch (Exception innerEx)
                    {
                        Debug.WriteLine($"❌ ERROR en '{ex.Name}': {innerEx.Message}");
                    }
                }

                #region Guardado en Base De Datos
                List<object> todasLasEntidades = new List<object>();                                

                int totalElementos = nuevosExercises.Count;
                int elementosGuardados = 0;

                Debug.WriteLine($"🚀 Guardando {totalElementos} elementos en batches...");
                Progreso = 0;

                // Optimizar SQLite temporalmente
                await OptimizarSqliteParaInsercionMasiva();

                using var transaction = await _database.Database.BeginTransactionAsync();
                try
                {
                    // Primero: Guardar músculos y BodyParts NUEVOS
                    var nuevosBodyParts = existingBodyParts.Values
                        .Where(b => b.BodyPartId == 0) // Solo los que no tienen ID (nuevos)
                        .ToList();

                    var nuevosMuscles = existingMuscles.Values
                        .Where(m => m.MuscleId == 0) // Solo los que no tienen ID (nuevos)
                        .ToList();

                    // Guardar BodyParts nuevos
                    if (nuevosBodyParts.Any())
                    {
                        await _database.BodyParts.AddRangeAsync(nuevosBodyParts);
                        await _database.SaveChangesAsync();

                        // Actualizar IDs en el diccionario
                        foreach (var bodyPart in nuevosBodyParts)
                        {
                            var key = bodyPart.BodyPart.ToLower();
                            existingBodyParts[key] = bodyPart;
                        }
                    }

                    // Guardar músculos nuevos
                    if (nuevosMuscles.Any())
                    {
                        await _database.Muscles.AddRangeAsync(nuevosMuscles);
                        await _database.SaveChangesAsync();

                        // Actualizar IDs en el diccionario
                        foreach (var muscle in nuevosMuscles)
                        {
                            var key = muscle.Name.ToLower();
                            existingMuscles[key] = muscle;
                        }
                    }

                    // Ahora guardar los ejercicios nuevos
                    if (nuevosExercises.Any())
                    {
                        // Configurar músculos y BodyParts para los ejercicios
                        foreach (var exercise in nuevosExercises)
                        {
                            // Actualizar BodyPart si es necesario
                            if (exercise.bodyPart != null && exercise.bodyPart.BodyPartId == 0)
                            {
                                var key = exercise.bodyPart.BodyPart.ToLower();
                                if (existingBodyParts.TryGetValue(key, out var existingBodyPart))
                                {
                                    exercise.bodyPart = existingBodyPart;
                                }
                            }

                            // Actualizar músculo primario si es necesario
                            if (exercise.primaryMuscle != null && exercise.primaryMuscle.MuscleId == 0)
                            {
                                var key = exercise.primaryMuscle.Name.ToLower();
                                if (existingMuscles.TryGetValue(key, out var existingMuscle))
                                {
                                    exercise.primaryMuscle = existingMuscle;
                                }
                            }

                            // Actualizar músculos secundarios si es necesario
                            if (exercise.secondaryMuscles.Any())
                            {
                                var secondaryMusclesToUpdate = new List<Muscle>();
                                foreach (var secMuscle in exercise.secondaryMuscles)
                                {
                                    if (secMuscle.MuscleId == 0)
                                    {
                                        var key = secMuscle.Name.ToLower();
                                        if (existingMuscles.TryGetValue(key, out var existingMuscle))
                                        {
                                            secondaryMusclesToUpdate.Add(existingMuscle);
                                        }
                                        else
                                        {
                                            secondaryMusclesToUpdate.Add(secMuscle);
                                        }
                                    }
                                    else
                                    {
                                        secondaryMusclesToUpdate.Add(secMuscle);
                                    }
                                }

                                // Limpiar y agregar los músculos actualizados
                                exercise.secondaryMuscles.Clear();
                                foreach (var muscle in secondaryMusclesToUpdate)
                                {
                                    exercise.secondaryMuscles.Add(muscle);
                                }
                            }
                        }

                        // Guardar ejercicios en batches
                        const int TAMANO_BATCH = 100;
                        int ejerciciosGuardados = 0;

                        for (int i = 0; i < nuevosExercises.Count; i += TAMANO_BATCH)
                        {
                            var batch = nuevosExercises.Skip(i).Take(TAMANO_BATCH).ToList();
                            await _database.Exercises.AddRangeAsync(batch);
                            await _database.SaveChangesAsync(); // Guardar este batch

                            ejerciciosGuardados += batch.Count;
                            Progreso = (double)ejerciciosGuardados / (nuevosExercises.Count * 2);
                            Debug.WriteLine($"📊 Progreso: {ejerciciosGuardados}/{nuevosExercises.Count}");
                        }

                        Debug.WriteLine($"✅ Ejercicios guardados: {nuevosExercises.Count}");
                    }
                    await transaction.CommitAsync();
                    await RestaurarConfiguracionSqliteNormal();
                    Debug.WriteLine($"✅ Guardado completado: {nuevosExercises.Count} ejercicios nuevos");

                    #endregion

                    Progreso = 50;                    
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    await transaction.CommitAsync();
                    await RestaurarConfiguracionSqliteNormal();
                    Debug.WriteLine($"❌ ERROR durante el guardado: {ex.Message}");
                    Debug.WriteLine($"Detalles: {ex.InnerException?.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ ERROR CRÍTICO: {ex.Message}");
                RestaurarConfiguracionSqliteNormal();
                Progreso = 0; // Resetear en error crítico
                throw;                
            }

        }               
        private async Task OptimizarSqliteParaInsercionMasiva()
        {           
            try
            {
                Debug.WriteLine("⚙️ Activando optimizaciones SQLite...");

                // Aplicar optimizaciones que funcionan fuera de transacciones
                await _database.Database.ExecuteSqlRawAsync("PRAGMA journal_mode = WAL");
                await _database.Database.ExecuteSqlRawAsync("PRAGMA synchronous = NORMAL");
                await _database.Database.ExecuteSqlRawAsync("PRAGMA cache_size = 10000");
                await _database.Database.ExecuteSqlRawAsync("PRAGMA temp_store = MEMORY");

                Debug.WriteLine("✅ SQLite optimizado para inserción masiva");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ No se pudo optimizar SQLite: {ex.Message}");
            }
        }
        private async Task RestaurarConfiguracionSqliteNormal()
        {
            try
            {
                Debug.WriteLine("⚙️ Restaurando configuración normal de SQLite...");

                // Esperar un momento para asegurar que no hay transacción activa
                await Task.Delay(50);

                await _database.Database.ExecuteSqlRawAsync("PRAGMA synchronous = FULL");
                await _database.Database.ExecuteSqlRawAsync("PRAGMA journal_mode = DELETE");
                await _database.Database.ExecuteSqlRawAsync("PRAGMA cache_size = -2000");
                await _database.Database.ExecuteSqlRawAsync("PRAGMA temp_store = DEFAULT");

                Debug.WriteLine("✅ Configuración SQLite restaurada");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Error al restaurar configuración: {ex.Message}");
            }
        }
        public async Task IniciarDatosRutinas(List<Rutinas> rutinas)
        {
            if (rutinas == null || !rutinas.Any())
            {
                Debug.WriteLine("⚠️ Lista de rutinas vacía");
                return;
            }
            var exercisesNoEncontrados = new List<string>();
            var rutinasGuardadas = 0;
            await OptimizarSqliteParaInsercionMasiva();
            using (var transaction = await _database.Database.BeginTransactionAsync())
            {
                try
                {
                    #region COMPROBACION DE RUTINAS NUEVAS
                    var rutinasJson = rutinas.Select(r => r.Nombre.Trim().ToLower())
                                            .Where(r => !string.IsNullOrEmpty(r))
                                            .Distinct()
                                            .ToArray();
                    Debug.WriteLine($"📊 {rutinasJson.Length} nombres únicos de rutinas en input");

                    var rutinasExistentes = await _database.Rutinas                                                                
                                                            .Where(n => rutinasJson.Contains(n.Nombre.Trim().ToLower()))
                                                            .Select(r => r.Nombre.Trim().ToLower())
                                                            .ToHashSetAsync();
                    Debug.WriteLine($"📊 {rutinasExistentes.Count} rutinas ya existen en BD");

                    var rutinasNuevas = rutinas.Where(r => !rutinasExistentes.Contains(r.Nombre.Trim().ToLower())).ToList();

                    if (!rutinasNuevas.Any())
                    {
                        Debug.WriteLine("✅ Todas las rutinas ya existen en la base de datos");
                        await transaction.CommitAsync();                                                            
                        return;
                    }
                    Debug.WriteLine($"🆕 {rutinasNuevas.Count} rutinas nuevas para agregar");
                    #endregion

                    #region PRECARGAR EXERCISES
                    var nombreExercises = rutinasNuevas.SelectMany(r => r.Semanas)
                                                .SelectMany(s => s.Dias)
                                                .Where(d => d.Ejercicios != null)
                                                .SelectMany(d => d.Ejercicios)
                                                .Where(e => e?.Exercise != null && !string.IsNullOrWhiteSpace(e.Exercise.Name))
                                                .Select(e => e.Exercise.Name.Trim().ToLower())
                                                .Distinct()
                                                .ToArray();

                    // Precargar exercises existentes
                    var exercisesExistentes = new Dictionary<string, Exercise>();

                    if (!nombreExercises.Any())
                    {
                        Debug.WriteLine("⚠️ No hay ejercicios para buscar en la BD");
                        exercisesExistentes = new Dictionary<string, Exercise>();
                    }
                    else
                    {
                        exercisesExistentes = await _database.Exercises
                            .Where(e => nombreExercises.Contains(e.Name.Trim().ToLower()))
                            .ToDictionaryAsync(e => e.Name.Trim().ToLower(), e => e);
                    }

                    #endregion

                    foreach (var rutina in rutinasNuevas.ToList())
                    {
                        rutina.RutinaId = 0;
                        foreach (var semana in rutina.Semanas.ToList())
                        {
                            semana.SemanaId = 0;
                            semana.RutinaId = 0;
                            foreach (var dia in semana.Dias.ToList())
                            {
                                dia.DiaId = 0;
                                dia.SemanaId = 0;
                                #region VALIDACIÓN INICIAL
                                if (dia.Ejercicios == null)
                                {
                                    semana.Dias.Remove(dia);
                                    Debug.WriteLine($"🗑️ Día '{dia.NombreRutinaDia}' eliminado (Ejercicios es null)");
                                    continue;
                                }
                                #endregion

                                #region FILTRAR EJERCICIOS VÁLIDOS Y SERIES                                                                

                                foreach (var ejercicio in dia.Ejercicios.ToList())
                                {

                                    if (ejercicio?.Exercise == null || string.IsNullOrWhiteSpace(ejercicio.Exercise.Name))
                                    {
                                        dia.Ejercicios.Remove(ejercicio);
                                        Debug.WriteLine($"❌ Ejercicio eliminado (Exercise es null o sin nombre)"); continue;
                                    }

                                    ejercicio.EjercicioId = 0;
                                    ejercicio.DiaId = 0;

                                    var nombreEjercicio = ejercicio.Exercise.Name.Trim().ToLower();

                                    if (exercisesExistentes.TryGetValue(nombreEjercicio, out var exercise))
                                    {
                                        ejercicio.Exercise = exercise;
                                        ejercicio.ExerciseId = exercise.ExerciseId;                                                                                
                                        Debug.WriteLine($"✅ Ejercicio encontrado: {nombreEjercicio}");
                                    }
                                    else
                                    {
                                        dia.Ejercicios.Remove(ejercicio);
                                        exercisesNoEncontrados.Add(nombreEjercicio);
                                        Debug.WriteLine($"❌ Exercise no encontrado en día '{dia.NombreRutinaDia}': {nombreEjercicio}");
                                        continue;
                                    }

                                    #region INICIALIZAR SERIE ID A 0
                                    if (ejercicio.Series != null)
                                    {
                                        foreach (var serie in ejercicio.Series)
                                        {
                                            serie.SerieId = 0;
                                            serie.EjercicioId = 0;
                                        }
                                    }
                                    #endregion
                                }
                                #endregion

                                #region VERIFICAR SI EL DÍA QUEDÓ VACÍO
                                if (!dia.Ejercicios.Any())  // Si no quedaron ejercicios válidos
                                {
                                    // Eliminar el día completo de la semana
                                    semana.Dias.Remove(dia);
                                    Debug.WriteLine($"🗑️ Día eliminado: todos los ejercicios eran inválidos");
                                }
                                else
                                {
                                    Debug.WriteLine($"✅ Día conservado: {dia.Ejercicios.Count} ejercicios válidos");
                                }
                                #endregion


                            }

                            #region VERIFICAR SI LA SEMANA QUEDÓ VACÍA
                            if (!semana.Dias.Any()) 
                            {
                                // Eliminar la semana completa de la rutina
                                rutina.Semanas.Remove(semana);
                                Debug.WriteLine($"🗑️ Semana eliminada: todos los días eran inválidos");
                            }
                            #endregion
                        }
                        #region VERIFICAR SI LA RUTINA QUEDÓ VACÍA
                        if (!rutina.Semanas.Any())  // Si no quedaron semanas válidas
                        {
                            // Eliminar la rutina completa
                            rutinasNuevas.Remove(rutina);
                            Debug.WriteLine($"🗑️ Rutina '{rutina.Nombre}' eliminada: todas las semanas eran inválidas");
                        }
                        #endregion
                    }

                    #region GUARDAR EN BD                    
                    Progreso = 50;
                    if (rutinasNuevas.Any())
                    {
                        rutinasGuardadas = 0;
                        int totalRutinas = rutinasNuevas.Count;                                                

                        foreach (var rutina in rutinasNuevas)
                        {
                            try
                            {
                                await _database.Rutinas.AddAsync(rutina);
                                var registrosGuardados = await _database.SaveChangesAsync();

                                rutinasGuardadas++;

                                // Actualizar progreso (ejemplo: si quieres que llegue al 50%)                                
                                Progreso = 0.5 + (rutinasGuardadas / (double)totalRutinas * 0.5);

                                // O si quieres progreso lineal del 0% al 50%:
                                // Progreso = (double)rutinasGuardadas / totalRutinas * 0.5;

                                Debug.WriteLine($"✅ Rutina guardada: {rutinasGuardadas}/{totalRutinas} - {rutina.Nombre}");
                                Debug.WriteLine($"   📊 Progreso actual: {Progreso:P0}");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"❌ Error al guardar rutina '{rutina.Nombre}': {ex.Message}");
                                // Opcional: puedes continuar con las siguientes rutinas
                                continue;
                            }
                        }
                        
                        Debug.WriteLine($"✅ Guardadas {rutinasGuardadas}/{totalRutinas} rutinas nuevas");

                        // Mostrar ejercicios no encontrados (fuera del bucle)
                        if (exercisesNoEncontrados.Any())
                        {
                            var unicos = exercisesNoEncontrados.Distinct().ToList();
                            Debug.WriteLine($"⚠️ {unicos.Count} ejercicios no encontrados en BD:");
                            foreach (var ex in unicos.Take(10)) // Mostrar solo los primeros 10
                            {
                                Debug.WriteLine($"   - {ex}");
                            }
                            if (unicos.Count > 10)
                            {
                                Debug.WriteLine($"   ... y {unicos.Count - 10} más");
                            }
                        }

                        await transaction.CommitAsync();
                        await RestaurarConfiguracionSqliteNormal();
                    }
                    else
                    {
                        Debug.WriteLine("ℹ️ No hay rutinas válidas para guardar");
                        await transaction.CommitAsync();
                        await RestaurarConfiguracionSqliteNormal();
                    }
                    #endregion
                    Debug.WriteLine($"✅ Rutinas guardadas exitosamente: {rutinasNuevas.Count}");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    await transaction.CommitAsync();
                    await RestaurarConfiguracionSqliteNormal();
                    Debug.WriteLine($"❌ Error en transacción: {ex.Message}");
                    throw;
                }

            }                                   

            await VerificarInsercion(_database.Rutinas.Count());
        }
        private async Task VerificarInsercion(int rutinasEsperadas)
        {
            var rutinasInsertadas = await _database.Rutinas.ToListAsync();
            var semanasInsertadas = await _database.RutinaSemanas.ToListAsync();
            var diasInsertadas = await _database.RutinaDias.ToListAsync();
            var ejerciciosInsertados = await _database.RutinaEjercicios.ToListAsync();
            var seriesInsertadas = await _database.RutinaSeries.ToListAsync();

            Debug.WriteLine("=== RESUMEN DE INSERCIÓN ===");
            Debug.WriteLine($"Rutinas: {rutinasInsertadas.Count}/{rutinasEsperadas}");
            Debug.WriteLine($"Semanas: {semanasInsertadas.Count}");
            Debug.WriteLine($"Días: {diasInsertadas.Count}");
            Debug.WriteLine($"Ejercicios en rutinas: {ejerciciosInsertados.Count}");
            Debug.WriteLine($"Series: {seriesInsertadas.Count}");

            // Mostrar estadísticas detalladas
            foreach (var rutina in rutinasInsertadas)
            {
                var semanasDeRutina = semanasInsertadas.Count(s => s.RutinaId == rutina.RutinaId);
                var ejerciciosDeRutina = await _database.RutinaEjercicios
                    .Where(e => e.Dia.Semana.RutinaId == rutina.RutinaId)
                    .CountAsync();

                Debug.WriteLine($"  {rutina.Nombre}: {semanasDeRutina} semanas, {ejerciciosDeRutina} ejercicios");
            }
        }

        public async Task EliminarTodosLosEntrenamientos()
        {
            try
            {
                var todosWorkoutDays = await _database.WorkoutDay.ToListAsync();
                _database.WorkoutDay.RemoveRange(todosWorkoutDays);
                await _database.SaveChangesAsync();

                Debug.WriteLine("🗑️ Todos los entrenamientos eliminados");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error eliminando entrenamientos: {ex.Message}");
                throw;
            }
        }
        public async Task EliminarEjerciciosPorDefecto()
        {
            try
            {                
                var todosEjercicios = await _database.Exercises.ToListAsync();

                _database.Exercises.RemoveRange(todosEjercicios);
                await _database.SaveChangesAsync();

                Debug.WriteLine($"🗑️ {todosEjercicios.Count} ejercicios personalizados eliminados");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error eliminando ejercicios personalizados: {ex.Message}");
                throw;
            }
        }        
        #endregion
        public async Task DebugDescansoEnBD()
        {
            var connection = _database.Database.GetDbConnection();
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT SerieId, Descanso FROM RutinaSeries";

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var id = reader.GetInt32(0);
                    var descanso = reader.IsDBNull(1) ? "NULL" : reader.GetString(1);
                    Debug.WriteLine($"📀 BD: Serie {id} - Descanso = '{descanso}'");
                }
            }

            command.CommandText = "SELECT SerieId, Descanso, typeof(Descanso) as TipoDato FROM RutinaSeries";

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

        #region ExercisesLogs

        public async Task<ExerciseLog> ObtenerOCrearExerciseLogAsync(RutinaSeries rutinaSeries, WorkoutDay workoutDayActual)
        {
            if (rutinaSeries?.Ejercicio == null || workoutDayActual == null)
            {
                Debug.WriteLine("⚠️ Datos incompletos para ObtenerOCrearExerciseLogAsync");
                return null;
            }

            // Buscar ExerciseLog existente
            var exerciseLog = workoutDayActual.ExerciseLogs
                .FirstOrDefault(w => w.ExerciseId == rutinaSeries.Ejercicio.ExerciseId);

            // Si no existe, crear uno nuevo
            if (exerciseLog == null)
            {
                exerciseLog = new ExerciseLog()
                {
                    Exercise = rutinaSeries.Ejercicio.Exercise,                    
                };

                workoutDayActual.ExerciseLogs.Add(exerciseLog);
                Debug.WriteLine($"📝 Nuevo ExerciseLog creado para ejercicio: {rutinaSeries.Ejercicio.Exercise.Name}");
            }

            return exerciseLog;
        }
        public async Task<SetLog> ObtenerOCrearSetLogAsync(ExerciseLog exerciseLog,RutinaSeries rutinaSeries)
        {
            if (exerciseLog == null || rutinaSeries == null)
            {
                Debug.WriteLine("⚠️ Datos incompletos para ObtenerOCrearSetLogAsync");
                return null;
            }

            // Buscar SetLog existente
            var setLog = exerciseLog.SetsLog?.FirstOrDefault(s =>
                s.SetLogId == rutinaSeries.SetLog?.SetLogId);

            // Si no existe, crear uno nuevo
            if (setLog == null)
            {
                setLog = new SetLog()
                {
                    
                };

                exerciseLog.SetsLog ??= new List<SetLog>();
                exerciseLog.SetsLog.Add(setLog);
                Debug.WriteLine($"➕ Nuevo SetLog creado en {exerciseLog.Exercise.Name}");
            }

            // Actualizar valores
            setLog.Kilos = rutinaSeries.KilosTemp;
            setLog.Reps = rutinaSeries.RepsTemp;
            setLog.Tipo = rutinaSeries.Tipo;

            return setLog;
        }
        public async Task ActualizarProgresoExerciseAsync(Exercise exercise, SetLog setLog)
        {
            if (exercise == null || setLog == null)
            {
                Debug.WriteLine("⚠️ Exercise o SetLog nulo en ActualizarProgresoExerciseAsync");
                return;
            }

            // Asignar dataService para que funcione PropertyChanged
            exercise.dataService = this;

            // Solo procesar series que afectan progreso RM
            if (setLog.Tipo != TipoSerie.Max_Rm && setLog.Tipo != TipoSerie.Normal)
            {
                Debug.WriteLine($"ℹ️ Serie tipo {setLog.Tipo} - No afecta progreso RM");
                return;
            }

            // Validar datos
            if (setLog.Kilos <= 0 || setLog.Reps <= 0)
            {
                Debug.WriteLine($"⚠️ SetLog inválido: {setLog.Kilos}kg x {setLog.Reps}");
                return;
            }

            // Guardar valores anteriores para comparación
            var anteriorMejor = exercise.Mejor;
            var anteriorIniciar = exercise.Iniciar;

            // ÚLTIMO: Siempre actualizar
            exercise.Ultimo = setLog.Kilos;

            // MEJOR: Solo si es mejor que el anterior
            if (exercise.Mejor == null || setLog.Kilos > exercise.Mejor)
            {
                exercise.Mejor = setLog.Kilos;
                Debug.WriteLine($"🏆 Nuevo récord: {setLog.Kilos}kg");
            }

            // INICIAR: Solo si es Max_Rm y es nulo/cero
            if (setLog.Tipo == TipoSerie.Max_Rm)
            {
                if (exercise.Iniciar == 0 || exercise.Iniciar == null)
                {
                    exercise.Iniciar = setLog.Kilos;
                    Debug.WriteLine($"🎯 Primer RM: {setLog.Kilos}kg");
                }
            }

            Debug.WriteLine($"📊 Progreso - Iniciar: {exercise.Iniciar}kg, " +
                           $"Mejor: {exercise.Mejor}kg, Último: {exercise.Ultimo}kg");
        }

        #endregion

        #region Verificar RutinasCompletadas
        public async Task<bool> VerificarDiaCompletadoAsync(int diaId)
        {
            var dia = await _database.RutinaDias
                .Include(d => d.Ejercicios)
                .FirstOrDefaultAsync(d => d.DiaId == diaId);

            if (dia == null) return false;

            return dia.Ejercicios?.All(e => e.Completado) ?? false;
        }
        public async Task<bool> VerificarSemanaCompletadoAsync(int semanaId)
        {
            var semana = await _database.RutinaSemanas
                .Include(s => s.Dias)
                .ThenInclude(d => d.Ejercicios)
                .FirstOrDefaultAsync(s => s.SemanaId == semanaId);

            if (semana == null) return false;

            return semana.Dias?.All(d => d.Completado) ?? false;
        }
        #endregion

        #region WorkoutDaysPrueba
        public async Task IniciarWorkutDaysPrueba(List<WorkoutDay> workoutDaysTemp)
        {
            var exercise = _database.Exercises.FirstOrDefault(e => e.Name == "Curl de Bíceps con Barra Recta");
            var exercise1 = _database.Exercises.FirstOrDefault(e => e.Name == "Pájaros en Polea Cruzada");
            var exercise2 = _database.Exercises.FirstOrDefault(e => e.Name == "Russian Twist en Banco Declinado");
            var mitad = workoutDaysTemp.Count / 3;

            List<WorkoutDay> primeraMitad = workoutDaysTemp.Take(mitad).ToList();
            List<WorkoutDay> segundaMitad = workoutDaysTemp.Skip(mitad).Take(mitad).ToList();
            List<WorkoutDay> terceraMitad = workoutDaysTemp.Skip(mitad * 2).ToList();

            Debug.WriteLine($"📊 Dividido: {primeraMitad.Count} + {segundaMitad.Count} + {terceraMitad.Count} = {workoutDaysTemp.Count} días");

            if (exercise == null || exercise1 == null)
            {
                Debug.WriteLine($"❌ Ejercicios no encontrados: 'Curva lateral de 45°'={exercise == null}, 'curl con barra'={exercise1 == null}");
                return;
            }

            await AgregarMitades(primeraMitad, exercise);
            await AgregarMitades(segundaMitad, exercise1);
            await AgregarMitades(terceraMitad, exercise2);            
        }
        private async Task AgregarMitades(List<WorkoutDay> mitad,Exercise exercise)
        {
            foreach (var WorkoutDayActualTemp in mitad)
            {
                try
                {
                    var workoutDay = await _database.WorkoutDay.FirstOrDefaultAsync(w => w.Date.Date == WorkoutDayActualTemp.Date.Date);
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

                    Debug.WriteLine("Buscando ExerciseLog");
                    var exerciseLog = await _database.ExercisesLogs.FirstOrDefaultAsync(e => e.WorkoutDayId == workoutDay.DayId && e.ExerciseId == exercise.ExerciseId);                    

                    if (exerciseLog == null)
                    {
                        exerciseLog = new ExerciseLog()
                        {
                            Exercise = exercise,
                            ExerciseId = exercise.ExerciseId,
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
            var rutinas = new List<Rutinas>();
            
            rutinas = await _database.Rutinas.Include(r => r.Semanas)
                                                .ThenInclude(s => s.Dias)
                                                    .AsNoTracking()                                                    
                                                        .ToListAsync();                 
            return rutinas;
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

        #region Rutinas
        public async Task<bool> AgregarSemanasARutinaAsync(Rutinas rutina,int semanasAAgregar)
        {
            if (rutina == null || semanasAAgregar <= 0)
                return false;

            try
            {
                using var transaction = await _database.Database.BeginTransactionAsync();
                
                int cantidadActualSemanas = rutina.Semanas?.Count ?? 0;
                int numeroSemanaInicial = cantidadActualSemanas + 1;

                for (int i = 0; i < semanasAAgregar; i++)
                {
                    int numeroSemana = numeroSemanaInicial + i;

                    var nuevaSemana = new RutinaSemana
                    {
                        Rutina = rutina,
                        RutinaId = rutina.RutinaId,                        
                        NombreSemana = $"Semana {numeroSemana}",                        
                    };                    
                    
                    rutina.Semanas.Add(nuevaSemana);
                    rutina.SemanasObservable.Add(nuevaSemana);
                }

                await _database.SaveChangesAsync();
                await transaction.CommitAsync();                                
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error al agregar semanas: {ex.Message}");
                return false;
            }
        }
        public async Task<RutinaSeries> AgregarRutinaSerie (RutinaEjercicio itemEjercicio)
        {
            try
            {

                if (itemEjercicio == null)
                {
                    Debug.WriteLine("⚠️ itemEjercicio es nulo");
                    return null;
                }
            
                int serieIdUi = itemEjercicio.Series.Any() ?
                    itemEjercicio.Series.Max(e => e.SerieId) + 1 : 1;

                var RutinaSerie = new RutinaSeries()
                {                    
                    SerieIdUI = serieIdUi,                                
                    EstadoSerie = 0, // Estado inicial
                };
                itemEjercicio.Series.Add(RutinaSerie);
                itemEjercicio.SeriesObservable.Add(RutinaSerie);
                await _database.SaveChangesAsync();
                //USO DEL METODO RELOAD PARA ASEGURARSE DE QUE SE USE EL TIMESPANCONVERTER
                await _database.Entry(RutinaSerie).ReloadAsync();
                return RutinaSerie;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error creando serie: {ex.Message}");
                return null;
            }


        }
        public async Task<RutinaDia> AgregarRutinaDia (RutinaSemana semana,Rutinas rutinaActual)
        {
            try
            {
                int numeroNuevoDia = semana.Dias.Count + 1;

                var nuevodia = new RutinaDia()
                {                                    
                    NombreRutinaDia = $"Día {semana.Dias.Count + 1}",                    
                    Completado = false
                };
                semana.Dias.Add(nuevodia);
                semana.DiasObservable.Add(nuevodia);
                await _database.SaveChangesAsync();
                return nuevodia;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error creando día: {ex.Message}");
                return null;
            }
        }
        public async Task GuardarRutinaEjercicio (RutinaDia itemDia, List<Exercise> exercises)
        {
            if (exercises != null && itemDia != null)
            {
                foreach (var exercise in exercises)
                {
                    if (exercise != null)
                    {                        
                        var RutinaEjercicio = new RutinaEjercicio()
                        {                            
                            Exercise = exercise,
                        };
                        itemDia.Ejercicios.Add(RutinaEjercicio);
                        itemDia.EjerciciosObservable.Add(RutinaEjercicio);
                    }
                }
                await _database.SaveChangesAsync();
                //COMPROBADO CON UN FOREACH QUE SE GUARDAN LAS REFERENCIAS A EXERCISE
            }
        }        
        public async Task<bool> EliminarSemanasDeRutinaAsync(Rutinas rutina,int semanasAEliminar)
        {
            if (rutina?.Semanas == null || semanasAEliminar >= rutina.Semanas.Count)
                return false;

            try
            {
                using var transaction = await _database.Database.BeginTransactionAsync();                

                // Obtener las últimas semanas a eliminar
                var semanasParaEliminar = rutina.Semanas
                    .OrderByDescending(s => s.SemanaIdUI ?? 0)
                    .ThenByDescending(s => s.SemanaId)
                    .Take(semanasAEliminar)
                    .ToList();

                if (!semanasParaEliminar.Any())
                    return false;
                
                foreach (var semana in semanasParaEliminar)
                {
                    rutina.Semanas.Remove(semana);
                    rutina.SemanasObservable.Remove(semana);
                }
                                
                _database.RutinaSemanas.RemoveRange(semanasParaEliminar);


                int cambios = await _database.SaveChangesAsync();
                await transaction.CommitAsync();

                Debug.WriteLine($"✅ Eliminadas {semanasParaEliminar.Count} semanas ({cambios} cambios en BD)");
                Debug.WriteLine($"🏁 Semanas restantes: {rutina.Semanas.Count}");

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error al eliminar semanas: {ex.Message}");
                return false;
            }
        }
        public async Task<bool> EliminarRutinaDia(RutinaDia dia)
        {
            if (dia == null)
            {
                Debug.WriteLine("⚠️ Día es nulo");
                return false;
            }

            try
            {                
                var diaCompleto = await _database.RutinaDias
                    .Include(d => d.Semana)
                    .FirstOrDefaultAsync(d => d.DiaId == dia.DiaId);

                if (diaCompleto == null)
                {
                    Debug.WriteLine($"⚠️ Día ID {dia.DiaId} no encontrado");
                    return false;
                }
                
                if (diaCompleto.Semana != null)
                {
                    diaCompleto.Semana.Dias?.Remove(diaCompleto);
                    diaCompleto.Semana.DiasObservable?.Remove(diaCompleto);
                }
                
                _database.RutinaDias.Remove(diaCompleto);

                await _database.SaveChangesAsync();

                Debug.WriteLine($"✅ Día eliminado: {diaCompleto.NombreRutinaDia}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error eliminando día: {ex.Message}");
                return false;
            }
        }        
        public async Task<bool> EliminarRutinaEjercicio(RutinaEjercicio ejercicio)
        {
            if (ejercicio == null)
            {
                Debug.WriteLine("⚠️ Ejercicio es nulo");
                return false;
            }

            try
            {                
                var ejercicioCompleto = await _database.RutinaEjercicios
                    .Include(e => e.Dia)
                    .FirstOrDefaultAsync(e => e.EjercicioId == ejercicio.EjercicioId);

                if (ejercicioCompleto == null)
                {
                    Debug.WriteLine($"⚠️ Ejercicio ID {ejercicio.EjercicioId} no encontrado");
                    return false;
                }
                
                if (ejercicioCompleto.Dia != null)
                {
                    ejercicioCompleto.Dia.Ejercicios?.Remove(ejercicioCompleto);
                    ejercicioCompleto.Dia.EjerciciosObservable?.Remove(ejercicioCompleto);
                }
                
                _database.RutinaEjercicios.Remove(ejercicioCompleto);

                await _database.SaveChangesAsync();

                Debug.WriteLine($"✅ Ejercicio eliminado: {ejercicioCompleto.Exercise?.Name}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error eliminando ejercicio: {ex.Message}");
                return false;
            }
        }        
        public async Task<bool> EliminarRutinaSerie(RutinaSeries serie)
        {
            if (serie == null)
            {
                Debug.WriteLine("⚠️ Serie es nula");
                return false;
            }

            try
            {
                var serieCompleta = await _database.RutinaSeries
                    .Include(s => s.Ejercicio)
                    .FirstOrDefaultAsync(s => s.SerieId == serie.SerieId);

                if (serieCompleta == null)
                {
                    Debug.WriteLine($"⚠️ Serie ID {serie.SerieId} no encontrada");
                    return false;
                }
                
                if (serieCompleta.Ejercicio != null)
                {
                    serieCompleta.Ejercicio.Series?.Remove(serieCompleta);
                    serieCompleta.Ejercicio.SeriesObservable?.Remove(serieCompleta);
                }
                
                _database.RutinaSeries.Remove(serieCompleta);

                await _database.SaveChangesAsync();

                Debug.WriteLine($"✅ Serie eliminada");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error eliminando serie: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region RutinasPage
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

        #region MetricasPage
        public async Task<ObservableCollection<Exercise>> FiltrarEjercicios(string ejercicioBuscado, string filtroTiempoSeleccionado)
        {
            var ejerciciosFiltrados = new List<Exercise>();
            
            try
            {                
                var consulta = _database.Exercises
                                                .Where(e => e.ExerciseLogs.Any(log => log.SetsLog.Any()))                                                
                                                .AsQueryable();                

                if (!string.IsNullOrWhiteSpace(ejercicioBuscado))
                {
                    consulta = consulta.Where(e => e.Name.ToLower().Contains(ejercicioBuscado.ToLower()));
                }
                                     

                if (filtroTiempoSeleccionado != "Todos")
                {
                    var dias = filtroTiempoSeleccionado switch
                    {
                        "Semana" => 7,
                        "Mes" => 30,
                        "3 Meses" => 90,
                        _ => 0
                    };

                    var fechaLimite = DateTime.Today.AddDays(-dias);                                        

                    consulta = consulta.Where(e => e.ExerciseLogs
                                            .Any(log => log.WorkoutDay.Date >= fechaLimite));
                }

                var idsFiltrados = await consulta
                                        .Select(e => e.ExerciseId)
                                        .ToListAsync();

                Debug.WriteLine($"🎯 Ejercicios filtrados: {idsFiltrados.Count}");

                if (idsFiltrados.Any())
                {
                    ejerciciosFiltrados = await _database.Exercises
                        .Where(e => idsFiltrados.Contains(e.ExerciseId))
                        .Include(e => e.ExerciseLogs)
                            .ThenInclude(log => log.WorkoutDay)
                        .Include(e => e.ExerciseLogs)
                            .ThenInclude(log => log.SetsLog)
                        .Include(e => e.primaryMuscle)
                            .AsSplitQuery()
                                .ToListAsync();                                        
                }

                var sql = consulta.ToQueryString();
                
                Debug.WriteLine($"📋 SQL GENERADO: {sql}");                
            }
            catch (Exception ex) { Debug.WriteLine(ex); }
            return new ObservableCollection<Exercise>(ejerciciosFiltrados);
        }                
        #endregion

        #region Notify
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        #endregion
    }
}
