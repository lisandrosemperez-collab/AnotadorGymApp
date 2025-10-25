using System.Collections.ObjectModel;
using AnotadorGymApp.Data;
using AnotadorGymApp.RutinasPage;
using AnotadorGymApp.MainPageViews;
using AnotadorGymApp.MetricasPageViews;
namespace AnotadorGymApp
{
    public partial class AppShell : Shell
    {        
        public AppShell()
        {           
            InitializeComponent();                        
            Routing.RegisterRoute("AgregarRutina", typeof(AgregarRutinaPage));            
            Routing.RegisterRoute("ComienzoRutina",typeof(ComienzoRutinaPage));
            Routing.RegisterRoute("SplashPage", typeof(SplashPage));
        }
    }
}
