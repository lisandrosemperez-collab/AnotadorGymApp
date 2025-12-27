using AnotadorGymApp.Data;
using AnotadorGymApp.Services;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AnotadorGymApp.MainPageViews;

public partial class SplashPage : ContentPage
{
    private readonly DataService _dataService;
    private readonly ConfigService _configService;
    private readonly ImagenPersistenteService _imagenPersistenteService;
    private CancellationTokenSource cancellationToken;

    public SplashPage(DataService dataService,ConfigService configService,ImagenPersistenteService imagenPersistenteService)
	{
		InitializeComponent();
		this._dataService = dataService;
        this._configService = configService;
        this._imagenPersistenteService = imagenPersistenteService;
        BindingContext = _dataService;
    }        
    protected override async void OnAppearing()
    {
        base.OnAppearing();        
        await InicializarAplicacion();
    }
    private async Task InicializarAplicacion()
    {
        cancellationToken = new CancellationTokenSource();
        _ = Task.Run(() => ActualizarMensajeProgreso(cancellationToken.Token));
        
        bool primerArranque = Preferences.Get("PrimerArranque", true);
        bool imagenesCargadas = Preferences.Get("ImagenesCargadas", false);
        var minTiempoSplash = Task.Delay(4000); // Tiempo mínimo de splash
        
        try
        {
            await _dataService._database.Database.MigrateAsync(); // Esto va si o si Sea o no El primer Arranque
            if (primerArranque || !_dataService._database.Rutinas.Any() || !imagenesCargadas)
            {
                await _configService.CargarExercisesInicialesAsync(_dataService);
                await _configService.CargarRutinasInicialesAsync(_dataService, _imagenPersistenteService);                                
                await minTiempoSplash;
            }
            else
            {
                for (int i = 1; i <= 100; i++)
                {
                    _dataService.Progreso = i / 100.0;
                    await Task.Delay(40);                    
                }                
            }
#if DEBUG
        await _configService.CargarWorkoutDayPrueba(_dataService);
        await VerificarMigracionesAplicadas(_dataService._database);
        await VerificarRutinasAplicadas(_dataService._database);
        await VerificarWourKoutDays(_dataService._database);
        await VerificarEstadisticasBaseDatos(_dataService._database);
#endif
            cancellationToken.Cancel();
            Application.Current.MainPage = new AppShell();
        }catch (Exception ex){Debug.WriteLine(ex);}

    }    
    private async Task ActualizarMensajeProgreso(CancellationToken cancellationToken)
    {
        var mensajes = new[]
        {
            "Calentando motores  🏋️",
            "Preparando las pesas  💪",
            "Cargando energía  ⚡",
            "Activando modo bestia  🦍",
            "Cargando ganancias  📈",
            "Forjando el físico  ⚒️",
            "Construyendo músculo  🏗️",
            "Preparando la batalla  🛡️",
            "Modo guerrero ON  ⚔️",
            "Legendario en proceso  👑",
            "Despertando fibras  🎯",
            "Inyectando motivación  💉",
            "Preparando la quema  🔥",
            "Afinando la técnica  ✨",
            "Cargando determinación  💯",
            "Quemando excusas  🚫",
            "Activando ganancias  📊",
            "Preparando el pump  💥",
            "Sacando el animal  🐯",
            "No pain, no gain  😤",
        };
        int indice = 0;

        while (!cancellationToken.IsCancellationRequested)
        {            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusLabel.Text = mensajes[indice];
            });
            indice = Random.Shared.Next(0, mensajes.Length);

            try
            {
                await Task.Delay(2000, cancellationToken);
            }
            catch (TaskCanceledException) { break; }
        }        
    }    
    private async Task VerificarRutinasAplicadas(DataBase database)
    {
        try
        {
            Debug.WriteLine("🔍 VERIFICANDO RUTINAS...");

            var rutinas = await database.Rutinas
                .Include(r => r.Semanas)
                .AsNoTracking()
                .ToListAsync();

            if(rutinas.Count == 0 || rutinas == null)
            {
                Debug.WriteLine($"No Hay rutinas o es null {rutinas.Count}");
                return;
            }

            foreach (Rutinas rutina in rutinas)
            {
                Debug.WriteLine($"Rutina numero: {rutina.RutinaId} Nombre: {rutina.Nombre}");
                Debug.WriteLine($"Cantidad de Semanas: {rutina.Semanas.Count()}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }
    private async Task VerificarMigracionesAplicadas(DataBase database)
    {
        try
        {
            Debug.WriteLine("🔍 VERIFICANDO MIGRACIONES...");

            var connection = database.Database.GetDbConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT MigrationId FROM __EFMigrationsHistory";

            using var reader = command.ExecuteReader();
            Debug.WriteLine("📋 Migraciones aplicadas:");
            while (reader.Read())
            {
                string migrationId = reader.GetString(0);
                Debug.WriteLine($"✅ {migrationId}");
            }

            connection.Close();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Error al verificar migraciones: {ex.Message}");
        }
    }
    private async Task VerificarWourKoutDays(DataBase database)
    {
        try
        {
            Debug.WriteLine("🔍 VERIFICANDO WORKOUT DAYS...");

            var wourkoutdays = await database.WorkoutDay
                .Include(r => r.ExerciseLogs)
                    .ThenInclude(e => e.SetsLog)
                    .AsNoTracking()
                .OrderByDescending(w => w.Date)
                .ToListAsync();

            if (wourkoutdays.Count == 0 || wourkoutdays == null)
            {
                Debug.WriteLine("📭 No hay WorkoutDays en la base de datos");
                return;
            }
            Debug.WriteLine($"📊 Total de WorkoutDays: {wourkoutdays.Count}");

            foreach (WorkoutDay wourkoutday in wourkoutdays)
            {
                Debug.WriteLine($"WourkoutDay numero: {wourkoutday.DayId} Fecha: {wourkoutday.Date.Date}");
                Debug.WriteLine($"Cantidad de ExerciseLogs: {wourkoutday.ExerciseLogs.Count()}");
                foreach(ExerciseLog log in wourkoutday.ExerciseLogs)
                {
                    Debug.WriteLine($"ExerciseLog numero: {log.ExerciseLogId} SetsLogs: {log.SetsLog.Count}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ ERROR en VerificarWorkoutDays: {ex.Message}");
            Debug.WriteLine($"🔍 StackTrace: {ex.StackTrace}");
        }
    }
    private async Task VerificarEstadisticasBaseDatos(DataBase database)
    {
        try
        {
            Debug.WriteLine("📈 ESTADÍSTICAS DE LA BASE DE DATOS:");

            var totalRutinas = await database.Rutinas.CountAsync();
            var totalWorkoutDays = await database.WorkoutDay.CountAsync();
            var totalExerciseLogs = await database.ExercisesLogs.CountAsync();
            var totalSetsLog = await database.SetLogs.CountAsync();
            var totalMuscleGroup = await database.Muscles.ToListAsync();
            var totalBodyParts = await database.BodyParts.ToListAsync();

            
            Debug.WriteLine($"🏋️‍♂️ Rutinas: {totalRutinas}");
            Debug.WriteLine($"📅 WorkoutDays: {totalWorkoutDays}");
            Debug.WriteLine($"📊 ExerciseLogs: {totalExerciseLogs}");
            Debug.WriteLine($"⚖️ SetsLog: {totalSetsLog}");
            Debug.WriteLine($"⚖️ Muscles: {totalMuscleGroup.Count}");
            foreach (Muscle muscle in totalMuscleGroup)
            {
                Debug.WriteLine($"⚖️ Muscle: {muscle.Name} Id: {muscle.MuscleId}");
            }
            Debug.WriteLine($"⚖️ BodyParts: {totalBodyParts.Count}");
            foreach (BodyParts bodypart in totalBodyParts)
            {
                Debug.WriteLine($"⚖️ BodyPart: {bodypart.BodyPart} Id: {bodypart.BodyPartId}");                
            }
            

            // Verificar la ruta de la base de datos
            var connection = database.Database.GetDbConnection();
            Debug.WriteLine($"🗃️ Ruta de BD: {connection.DataSource}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ ERROR en VerificarEstadisticas: {ex.Message}");
        }
    }
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        cancellationToken?.Cancel();
    }
}