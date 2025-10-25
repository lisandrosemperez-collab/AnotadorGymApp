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
}
