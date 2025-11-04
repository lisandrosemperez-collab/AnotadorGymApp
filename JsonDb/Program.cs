using System.Text;
using AnotadorGymApp.Data;
using System.Data;
using JsonDb;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Reflection.Metadata;
using Azure.AI.Translation.Document;
using Azure;

using System.Text.Json;
using System.Text.Encodings.Web;

internal class Program
{
    private static readonly string key = "7eQuVeRuPkPtexcrzUaEVMDgPbhuuwnWHSBuydvsSJ97Hj2UKC3HJQQJ99BCACZoyfiXJ3w3AAAbACOGi4df";
    private static readonly string endpoint = "https://api.cognitive.microsofttranslator.com/";
    private static readonly string region = "brazilsouth";
    private static readonly string route = "/translate?api-version=3.0&to=es";

    private static readonly Uri Sourceuri = new Uri("https://jsoninglesgym.blob.core.windows.net/prueba-traduccion?sp=racwdl&st=2025-04-07T06:50:09Z&se=2025-04-08T02:57:09Z&spr=https&sv=2024-11-04&sr=c&sig=r0yvWvvfX2ZwH34WwTTxBACo%2FMuAy3ogSuWne8%2BIzRg%3D");
    private static readonly Uri Targeteuri = new Uri($"https://jsoninglesgym.blob.core.windows.net/prueba-traducida?sp=racwl&st=2025-04-07T07:00:53Z&se=2025-04-08T03:03:53Z&spr=https&sv=2024-11-04&sr=c&sig=mAv%2BjYT6%2Fe1skb6kJj0l9OoqZ%2BzfhNItmJ9n65N2CtI%3D");
    private static string rutaEntrada = "C:\\Users\\Admin\\Desktop\\back\\ejerciciosOrginal.json";
    private static string rutaSalida = "C:\\Users\\Admin\\Desktop\\back\\ejerciciosModificado.json";
    static async Task Main(string[] args)
    {
        //await CaracteresEscapados();
        //await TraducirEjerciciosConAzureAsync();
        await ObtenerExercisesNombres();
    }

    private static async Task ObtenerExercisesNombres()
    {
        var exercises = JsonSerializer.Deserialize<List<ExercisesNames>>(File.ReadAllText(rutaEntrada));
        var opcionesJson = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        string contenidolimpio = JsonSerializer.Serialize(exercises,opcionesJson);
        File.WriteAllText(rutaSalida, contenidolimpio);
        Console.WriteLine("✅ Archivo limpiado y guardado correctamente.");

    }

    private static async Task CaracteresEscapados()
    {        
        var ejerciciosprueba = JsonSerializer.Deserialize<List<ExerciseInglish>>(File.ReadAllText(rutaEntrada));
        var opcionesJson = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        string contenidolimpio = JsonSerializer.Serialize(ejerciciosprueba, opcionesJson);
        File.WriteAllText(rutaSalida, contenidolimpio);

        Console.WriteLine("✅ Archivo limpiado y guardado correctamente.");

    }
    private static async Task TraducirEjerciciosConAzureAsync()
    {
        // Cargar JSON original
        var ejerciciosprueba = JsonSerializer.Deserialize<List<ExerciseInglish>>(File.ReadAllText(rutaEntrada));
        // Cargar JSON prueba
        //var jsonprueba = File.ReadAllText("JsonPrueba.json");
        //var ejerciciosprueba = JsonSerializer.Deserialize<List<ExerciseInglish>>(jsonprueba);
        // Recolectar todos los textos a traducir
        List<string> textos = new();
        foreach (var ej in ejerciciosprueba)
        {
            textos.Add(ej.name);
            textos.Add(ej.bodyPart);
            textos.AddRange(ej.secondaryMuscles);
        }

        // Eliminar duplicados para traducir menos
        var TextosDistintos = textos.Distinct().ToList();
        var traducciones = new Dictionary<string, string>();
        var noTraducidos = new List<string>();

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);
        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Region", region);

        // Azure admite 100 elementos por request, así que vamos por bloques
        int chunkSize = 100;
        for (int i = 0; i < TextosDistintos.Count; i += chunkSize)
        {
            //Crea lista, sin tomar la posicion 0, agarra 100, y devuelve la lista
            List<string> chunk = TextosDistintos.Skip(i).Take(chunkSize).ToList();

            //Crea el cuerpo del request con el formato que pide Azure: un array de objetos { "Text": "..." }.
            var body = chunk.Select(t => new { Text = t }).ToArray();
            var requestBody = JsonSerializer.Serialize(body);

            Console.WriteLine(requestBody);
            Console.ReadLine();

            //Envia la petición a Azure Translator con el cuerpo en JSON.
            using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(endpoint + route, content);
            var result = await response.Content.ReadAsStringAsync();
            Console.WriteLine(result);
            Console.ReadLine();
            
            //Lista de los Textos traducidos (TranslationResult)
            var traducidos = JsonSerializer.Deserialize<List<TranslationResult>>(result, new JsonSerializerOptions { PropertyNameCaseInsensitive=true});

            for (int j = 0; j < chunk.Count; j++)
            {                
                string original = chunk[j];
                string traducido = traducidos[j].translations[0].text;
                if (original.Equals(traducido,StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"'{original}' no fue traducido, podría requerir revisión manual.");
                    noTraducidos.Add(original);
                    traducciones[original] = traducido;
                }
                else 
                {
                    traducciones[original] = traducido;
                    Console.WriteLine($"'{original}' → '{traducido}'");
                }
                    
                // Mostrar en consola
                
            }
        }        
        
        foreach (var e in ejerciciosprueba)
        {
            e.name = traducciones[e.name];
            e.bodyPart = traducciones[e.bodyPart];
            e.secondaryMuscles = e.secondaryMuscles.Select(m => traducciones[m]).ToList();
        }

        var JsonTraducido = JsonSerializer.Serialize(ejerciciosprueba, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(rutaSalida,JsonTraducido);
        Console.WriteLine("✅ Traducción completada. Archivo guardado como 'ejercicios_traducidos.json'.");
        var pathNoTraducidos = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "NoTraducidos.json");

        await File.WriteAllTextAsync(pathNoTraducidos, JsonSerializer.Serialize(noTraducidos, new JsonSerializerOptions { WriteIndented = true }));

        Console.WriteLine($"Se guardaron {noTraducidos.Count} términos no traducidos en: {pathNoTraducidos}");
    }    
}
public class DetectedLanguage
{
    public string language { get; set; }
    public double score { get; set; }
}
public class TranslationResult
{
    public DetectedLanguage detectedLanguage { get; set; }
    public List<TranslationText> translations { get; set; }
}
public class TranslationText
{
    public string text { get; set; }
    public string to { get; set; }
}