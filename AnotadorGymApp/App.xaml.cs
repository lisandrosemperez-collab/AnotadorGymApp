#define WINDOWS
using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.LifecycleEvents;
using System.Runtime.InteropServices;
using Microcharts.Maui;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.Json;
using AnotadorGymApp.Data;
using AnotadorGymApp.MainPageViews;

namespace AnotadorGymApp
{    
    public partial class App : Application
    {        
        //DataService _dataService = new DataService();        
        private DataService _dataService;        
        public App(DataService dataService)
        {            
            _dataService = dataService;                       
            Application.Current.UserAppTheme = AppTheme.Light;            
            try
            {
                InitializeComponent();
                MainPage = new SplashPage(_dataService);
                Debug.WriteLine("🚀 App creada - SplashPage iniciada");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error en App: {ex}");
                // Fallback seguro
                MainPage = new AppShell();
            }
        }        
    }
}
