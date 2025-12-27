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
using AnotadorGymApp.Services;

namespace AnotadorGymApp
{    
    public partial class App : Application
    {                
        private DataService _dataService;         
        private ConfigService _configService;
        private ImagenPersistenteService _imagenPersistenteService;
        public App(DataService dataService,ConfigService configService,ImagenPersistenteService imagenPersistenteService)
        {            
            _dataService = dataService;        
            _configService = configService;
            _imagenPersistenteService = imagenPersistenteService;
            try
            {
                InitializeComponent();
                _configService.AplicarTema();               
                MainPage = new SplashPage(_dataService,_configService,imagenPersistenteService);
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
