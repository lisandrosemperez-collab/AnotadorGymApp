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
        public async Task CargarExercisesInicialesAsync(DataService _dataService,IProgress<double>? progress = null)
        {
            try
            {
                if (!_dataService._database.Exercises.Any())
                {
                    #region JsonABaseDeDatos

                    List<ExerciseJson> datos = null;
                    string origenDatos = "";
                    bool usarDatosDemo = Preferences.Get("UsarDatosDemo", false);

                    string[] archivosPrioridad;

                    if (usarDatosDemo)
                    {
                        archivosPrioridad = new[] { "EjerciciosEJEMPLO.json", "Ejercicios.json" };
                        Debug.WriteLine("🔍 Priorizando archivo DEMO por preferencia");
                    }
                    else
                    {
                        archivosPrioridad = new[] { "Ejercicios.json", "EjerciciosEJEMPLO.json" };
                        Debug.WriteLine("🔍 Priorizando archivo PRODUCCIÓN por preferencia");
                    }

                    foreach (var archivo in archivosPrioridad)
                    {
                        try
                        {
                            Debug.WriteLine($"📦 Intentando cargar: {archivo}");

                            using var stream = await FileSystem.OpenAppPackageFileAsync(archivo);
                            using var reader = new StreamReader(stream);
                            string json = await reader.ReadToEndAsync().ConfigureAwait(false);
                            if (!string.IsNullOrWhiteSpace(json))
                            {
                                datos = JsonSerializer.Deserialize<List<ExerciseJson>>(json);

                                if (datos != null && datos.Count > 0)
                                {
                                    origenDatos = archivo.Contains("EJEMPLO") ?
                                        "demo (EjerciciosEJEMPLO.json)" :
                                        "producción (Ejercicios.json)";

                                    usarDatosDemo = archivo.Contains("EJEMPLO");
                                    Debug.WriteLine($"✅ Cargado desde {origenDatos}: {datos.Count} ejercicios");
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"⚠️ Error cargando {archivo}: {ex.Message}");
                            continue;
                        }
                    }

                    // PROCESAR LOS DATOS
                    if (datos != null && datos.Count > 0)
                    {
                        Console.WriteLine($"✅ Se cargaron {datos.Count} ejercicios desde {origenDatos}");
                        Console.WriteLine("📊 Iniciando migración de datos Json a DB");

                        await _dataService.IniciarDatosExercises(datos, progress);
                        Preferences.Set("PrimerArranque", false);
                        Preferences.Set("UsarDatosDemo", usarDatosDemo);
                        Debug.WriteLine("✅ Datos iniciales cargados correctamente");
                    }
                    else
                    {
                        Debug.WriteLine("❌ ERROR: No se pudieron cargar datos de ejercicios");
                        throw new InvalidOperationException("No hay datos de ejercicios disponibles");
                    }

                    #endregion
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error al aplicar migración de ejercicios: {ex.Message}");
            }
        }
        public async Task CargarRutinasInicialesAsync(DataService _dataService,ImagenPersistenteService imagenPersistenteService, IProgress<double>? progress = null)
        {            

            bool rutinasCargadas = false;
            bool usarDatosDemo = Preferences.Get("UsarDatosDemo", false);

            try
            {
                if (_dataService._database.Rutinas.Any())
                {
                    Debug.WriteLine("✅ Ya existen rutinas en la base de datos, omitiendo carga inicial");
                    return;
                }

                var archivos = usarDatosDemo ?
                            new[] { "RutinasEJEMPLO.json", "Rutinas.json" } :
                            new[] { "Rutinas.json", "RutinasEJEMPLO.json" };

                Debug.WriteLine($"🔍 Modo actual: {(usarDatosDemo ? "DEMO" : "PRODUCCIÓN")}");
                Debug.WriteLine($"🔍 Prioridad de archivos: {string.Join(" -> ", archivos)}");

                List<Rutinas> rutinas = null;
                string archivoCargado = null;                

                foreach (var archivo in archivos)
                {
                    try
                    {
                        Debug.WriteLine($"📦 Intentando cargar: {archivo}");

                        using var stream = await FileSystem.OpenAppPackageFileAsync(archivo);
                        if (stream == null)
                        {
                            Debug.WriteLine($"⚠️ Archivo {archivo} no encontrado");
                            continue;
                        }

                        using var reader = new StreamReader(stream);
                        var json = await reader.ReadToEndAsync().ConfigureAwait(false);

                        if (string.IsNullOrWhiteSpace(json))
                        {
                            Debug.WriteLine($"⚠️ Archivo {archivo} está vacío");
                            continue;
                        }

                        rutinas = JsonSerializer.Deserialize<List<Rutinas>>(json);

                        if (rutinas == null || rutinas.Count == 0)
                        {
                            Debug.WriteLine($"⚠️ Archivo {archivo} no contiene rutinas válidas");
                            continue;
                        }

                        archivoCargado = archivo;
                        usarDatosDemo = archivo.Contains("EJEMPLO");
                        Debug.WriteLine($"✅ Se cargaron {rutinas.Count} rutinas desde {archivo}");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ Error al cargar {archivo}: {ex.Message}");
                        continue;
                    }
                }                

                try
                {
                    if (rutinas != null && rutinas.Count > 0)
                    {

                        await _dataService.IniciarDatosRutinas(rutinas,progress);
                        Preferences.Set("PrimerArranque", false);
                        Preferences.Set("UsarDatosDemo", usarDatosDemo);
                        Preferences.Set("MostrarNotificacionDemoInicial", usarDatosDemo);
                        Debug.WriteLine("✅ Datos iniciales cargados");

                        rutinasCargadas = true;
                        Debug.WriteLine($"✅ Se insertaron {rutinas.Count} rutinas en la base de datos");

                        if (!Preferences.Get("ImagenesCargadas", false))
                        {
                            await CargarImagenesRutinasAsync(_dataService, imagenPersistenteService);
                        }

                        Debug.WriteLine($"🏁 Modo final: {(usarDatosDemo ? "DEMO" : "PRODUCCIÓN")}");
                        Debug.WriteLine($"📊 Ejercicios: {Preferences.Get("OrigenDatosEjercicios", "Desconocido")}");
                        Debug.WriteLine($"📊 Rutinas: {Preferences.Get("OrigenDatosRutinas", "Desconocido")}");                        

                    }
                    else
                    {
                        Debug.WriteLine("❌ No hay rutinas válidas para insertar");                        
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Error al insertar rutinas en BD: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error crítico en CargarRutinasInicialesAsync: {ex.Message}");
            }
            finally
            {                
                Debug.WriteLine($"🏁 Carga de rutinas completada. Éxito: {rutinasCargadas}");
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
                        PropertyNameCaseInsensitive = true
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
