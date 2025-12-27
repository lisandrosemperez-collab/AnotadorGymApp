using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnotadorGymApp.Data
{
    public class ExerciseJson
    {         
        public string Name { get; set; }
        public string bodyPart { get; set; }
        public Muscle primaryMuscle { get; set; }
        public List<Muscle> secondaryMuscles { get; set; } = new List<Muscle>();
        
    }        
}
