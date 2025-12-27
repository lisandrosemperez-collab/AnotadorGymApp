using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnotadorGymApp.Data
{        
    public class BodyParts
    {
        public ICollection<Exercise> ExercisesList { get; set; } = new List<Exercise>();
        public int BodyPartId { get; set; }
        public string BodyPart { get; set; }
        public BodyParts() { }
        public BodyParts (string nombre)
        {
            BodyPart = nombre;
        }        
    }
    public class Muscle
    {        
        public ICollection<Exercise> ExercisesAsSecundary { get; set; } = new List<Exercise>();
        public ICollection<Exercise> ExercisesAsMain { get; set; } = new List<Exercise>();
        #region Propiedades
        public int MuscleId { get; set; }
        public string Name { get; set; }        
        public Muscle(string nombre)
        {
            Name = nombre;
        }
        public Muscle() { } 
        #endregion
    }

    public static class MuscleToBodyPartMap // A PROBAR
    {
        public static readonly Dictionary<string, string> Mapping = new(StringComparer.OrdinalIgnoreCase)
        {
            // ========== BRAZOS ==========
            ["Bíceps Braquial"] = "Brazos",
            ["Tríceps"] = "Brazos",
            ["Braquial"] = "Brazos",
            ["Braquiorradial"] = "Brazos",

            // ========== ANTEBRAZOS ==========
            ["Antebrazos"] = "Antebrazos",
            ["Flexores de la muñeca"] = "Antebrazos",
            ["Extensores de la muñeca"] = "Antebrazos",

            // ========== PECHO ==========
            ["Pectoral Mayor"] = "Pecho",
            ["Serrato Anterior"] = "Pecho", // Aunque también es de core

            // ========== HOMBROS ==========
            ["Deltoides Anterior"] = "Hombros",
            ["Deltoides Lateral"] = "Hombros",
            ["Deltoides Posterior"] = "Hombros",
            ["Hombros (general)"] = "Hombros",
            ["Supraespinoso"] = "Hombros",
            ["Infraespinoso"] = "Hombros",
            ["Redondo Menor"] = "Hombros",
            ["Subescapular"] = "Hombros",

            // ========== ESPALDA ==========
            ["Dorsal Ancho"] = "Espalda",
            ["Trapecio"] = "Espalda", // Aunque también es de hombros
            ["Romboides"] = "Espalda",
            ["Erector Espinal"] = "Espalda", // Aunque también es de core

            // ========== PIERNAS ==========
            ["Cuádriceps"] = "Piernas",
            ["Isquiotibiales"] = "Piernas",
            ["Glúteos"] = "Piernas",
            ["Aductor"] = "Piernas",
            ["Gemelos"] = "Piernas",
            ["Sóleo"] = "Piernas",
            ["Tibial Anterior"] = "Piernas",

            // ========== CORE ==========
            ["Abdominales"] = "Core",
            ["Oblicuos"] = "Core",
            ["Transverso Abdominal"] = "Core",
            ["Core"] = "Core",

            // ========== FULL BODY ==========
            // Estos músculos pueden aparecer en múltiples bodyParts
            // Se manejan como casos especiales
        };

        // Método para obtener BodyPart de un músculo
        public static string GetBodyPart(string muscleName)
        {
            if (Mapping.TryGetValue(muscleName, out var bodyPart))
            {
                return bodyPart;
            }

            // Músculos que pueden ser de múltiples bodyParts
            // Dependiendo del contexto
            return muscleName switch
            {
                "Erector Espinal" => "Espalda", // Por defecto espalda, pero puede ser core
                "Trapecio" => "Espalda", // Por defecto espalda, pero puede ser hombros
                "Serrato Anterior" => "Pecho", // Por defecto pecho, pero puede ser core
                _ => "Full Body" // Para cualquier otro no mapeado
            };
        }

        // Método para obtener todos los músculos de un BodyPart
        public static List<string> GetMusclesForBodyPart(string bodyPart)
        {
            return Mapping
                .Where(kvp => string.Equals(kvp.Value, bodyPart, StringComparison.OrdinalIgnoreCase))
                .Select(kvp => kvp.Key)
                .OrderBy(m => m)
                .ToList();
        }

        // Verificar si un músculo pertenece a un BodyPart
        public static bool BelongsToBodyPart(string muscleName, string bodyPart)
        {
            var muscleBodyPart = GetBodyPart(muscleName);
            return string.Equals(muscleBodyPart, bodyPart, StringComparison.OrdinalIgnoreCase);
        }
    }
}
