using AnotadorGymApp.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;

namespace AnotadorGymApp.MainPageViews;

public partial class SplashPage : ContentPage
{
	public DataService _dataService { get; private set; }
    private CancellationTokenSource cancellationToken;

    public SplashPage(DataService dataService)
	{
		InitializeComponent();
		this._dataService = dataService;
        BindingContext = this;
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
        var minTiempoSplash = Task.Delay(4000); // Tiempo mínimo de splash
        
        try
        {
            await _dataService._database.Database.MigrateAsync(); // Esto va si o si Sea o no El primer Arranque
            if (primerArranque)
            {
                await CargarDatosInicialesAsync();                
                await minTiempoSplash;
            }
            else
            {
                for (int i = 0; i <= 100; i++)
                {
                    _dataService.Progreso = i / 100.0;
                    await Task.Delay(40);                    
                }                
            }            
#if DEBUG
        await CargarWorkoutDayPrueba();
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
    private async Task CargarWorkoutDayPrueba()
    {        
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("WorkoutDays.json");
            using var reader = new StreamReader(stream);
            string json = await reader.ReadToEndAsync().ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine("📦 Contenido del archivo JSON:");
            System.Diagnostics.Debug.WriteLine(json);

            if (string.IsNullOrWhiteSpace(json))
            {
                Console.WriteLine("⚠️ El archivo está vacío");
            }
            else
            {
                var datos = JsonSerializer.Deserialize<List<WorkoutDay>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true // ← Importante para que funcione el binding
                });

                Console.WriteLine($"✅ Se cargaron {datos?.Count ?? 0} WorkutDays");
                if (datos.Any())
                {
                    await _dataService.IniciarWorkutDaysPrueba(datos);
                    Debug.WriteLine($"✅WorkutDays Cargados {datos.Count}");
                }
                else { Debug.WriteLine($"WorkutDays no tiene datos: {datos.Count}"); }
                return;
            }
        }
        catch (Exception ex) { Debug.WriteLine(ex.ToString()); }
    }
    private async Task CargarDatosInicialesAsync()
    {
        string ruta = Path.Combine(FileSystem.AppDataDirectory, "GymApp.db");        
        try
        {
            await _dataService._database.Database.MigrateAsync();

            if (!_dataService._database.Exercises.Any() && Preferences.Get("PrimerArranque", true))
            { 
                #region JsonABaseDeDatos

                using var stream = await FileSystem.OpenAppPackageFileAsync("Ejercicios.json");
                using var reader = new StreamReader(stream);
                string json = await reader.ReadToEndAsync().ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine("📦 Contenido del archivo JSON:");
                System.Diagnostics.Debug.WriteLine(json);

                if (string.IsNullOrWhiteSpace(json))
                {
                    Console.WriteLine("⚠️ El archivo está vacío");
                }
                else
                {
                    var datos = JsonSerializer.Deserialize<List<ExerciseJson>>(json);
                    Console.WriteLine($"✅ Se cargaron {datos?.Count ?? 0} ejercicios");
                    await _dataService.IniciarDatos(datos);
                    Preferences.Set("PrimerArranque", false);
                    Debug.WriteLine("✅ Datos iniciales cargados");
                    return;                       
                }

                #endregion
            }
            Preferences.Set("PrimerArranque", false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error al aplicar migración: {ex.Message}");
        }

        Debug.WriteLine(_dataService._database.Rutinas.Count());
        VerificarMigracionesAplicadas(_dataService._database);
             
    }    
    private void VerificarMigracionesAplicadas(DataBase db)
    {
        try
        {
            var connection = db.Database.GetDbConnection();
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
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        cancellationToken?.Cancel();
    }
}