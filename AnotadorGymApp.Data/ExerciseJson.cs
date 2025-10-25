using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnotadorGymApp.Data
{
    public class ExerciseJson
    {         
        public string name { get; set; }
        public string bodyPart { get; set; }
        public string primaryMuscle { get; set; }
        public List<string> secondaryMuscles { get; set; }
        
    }
}
