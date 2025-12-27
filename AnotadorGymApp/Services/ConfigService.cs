using AnotadorGymApp.Data;
using AnotadorGymApp.Resources.Styles;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AnotadorGymApp.Services
{
    public class ConfigService
    {
        public bool TemaOscuro { get; private set; }
        public ConfigService()
        {
            // Cargar configuración al iniciar
            TemaOscuro = Preferences.Get("TemaOscuro", false);            
        }

        public void GuardarTema(bool temaOscuro)
        {
            TemaOscuro = temaOscuro;
            Preferences.Set("TemaOscuro", temaOscuro);
            AplicarTema();
        }

        public void CambiarTema()
        {
            GuardarTema(!TemaOscuro);
        }
        public void AplicarTema()
        {
            var mergedDictionaries = Application.Current.Resources.MergedDictionaries;
            mergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(new Styles());

            if (mergedDictionaries != null)
            {
                

                if (TemaOscuro)
                {
                    mergedDictionaries.Add(new DarkTheme());
                }
                else
                {
                    mergedDictionaries.Add(new LightTheme());
                }
            }
        }
        public async Task CargarExercisesInicialesAsync(DataService _dataService)
        {            
            try
            {                
                if (!_dataService._database.Exercises.Any())
                {
                    #region JsonABaseDeDatos

                    using var stream = await FileSystem.OpenAppPackageFileAsync("Ejercicios.json");
                    using var reader = new StreamReader(stream);
                    string json = await reader.ReadToEndAsync().ConfigureAwait(false);
                    Debug.WriteLine("📦 Contenido del archivo JSON:");
                    Debug.WriteLine(json);

                    if (string.IsNullOrWhiteSpace(json))
                    {
                        Console.WriteLine("⚠️ El archivo está vacío");
                    }
                    else
                    {
                        var datos = JsonSerializer.Deserialize<List<ExerciseJson>>(json);
                        Console.WriteLine($"✅ Se cargaron {datos?.Count ?? 0} ejercicios, Iniciando Migrar Datos Json a DB");
                        await _dataService.IniciarDatosExercises(datos);
                        Preferences.Set("PrimerArranque", false);
                        Debug.WriteLine("✅ Datos iniciales cargados");
                    }

                    #endregion
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al aplicar migración: {ex.Message}");
            }
            
        }
        public async Task CargarRutinasInicialesAsync(DataService _dataService,ImagenPersistenteService imagenPersistenteService)
        {            
            try
            {
                if (!_dataService._database.Rutinas.Any())
                {
                    #region JsonABaseDeDatos

                    using var stream = await FileSystem.OpenAppPackageFileAsync("Rutinas.json");
                    using var reader = new StreamReader(stream);
                    string json = await reader.ReadToEndAsync().ConfigureAwait(false);
                    Debug.WriteLine("📦 Contenido del archivo JSON:");
                    Debug.WriteLine(json);

                    if (string.IsNullOrWhiteSpace(json))
                    {
                        Console.WriteLine("⚠️ El archivo está vacío");
                    }
                    else
                    {
                        var rutinas = JsonSerializer.Deserialize<List<Rutinas>>(json);
                        Console.WriteLine($"✅ Se cargaron {rutinas?.Count ?? 0} rutinas, Iniciando Migrar Rutionas Json a DB");
                        
                        await _dataService.IniciarDatosRutinas(rutinas);                        
                        Preferences.Set("PrimerArranque", false);
                        Debug.WriteLine("✅ Datos iniciales cargados");
                    }

                    #endregion
                }
                if (!Preferences.Get("ImagenesCargadas", false))
                {
                    await CargarImagenesRutinasAsync(_dataService, imagenPersistenteService);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al aplicar migración: {ex.Message}");
            }
        }
        private async Task CargarImagenesRutinasAsync(DataService _dataService, ImagenPersistenteService imagenPersistenteService)
        {
            try
            {
                var rutinas = await _dataService._database.Rutinas.ToListAsync();
                foreach (var rutina in rutinas)
                {                   
                    var nuevaRuta = await imagenPersistenteService.CopiarImagenEmbebidaAsync(rutina.ImageSource);
                    if (nuevaRuta != null)
                    {
                        rutina.ImageSource = nuevaRuta;
                    }
                    else
                    {                        
                        var defaultRuta = await imagenPersistenteService.CopiarImagenEmbebidaAsync("rutina_default.jpg");
                        rutina.ImageSource = defaultRuta ?? "rutina_default.jpg";
                    }
                }
                await _dataService._database.SaveChangesAsync();
                Preferences.Set("ImagenesCargadas",true);
            } catch (Exception ex) 
            {
                Preferences.Set("ImagenesCargadas", false);
                Debug.WriteLine($"Error Cargando Imagenes Rutinas: {ex.Message}"); 
            }
        }
        public async Task CargarWorkoutDayPrueba(DataService _dataService)
        {
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("WorkoutDays.json");
                using var reader = new StreamReader(stream);
                string json = await reader.ReadToEndAsync().ConfigureAwait(false);
                Debug.WriteLine("📦 Contenido del archivo JSON:");
                Debug.WriteLine(json);

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
    }
}
