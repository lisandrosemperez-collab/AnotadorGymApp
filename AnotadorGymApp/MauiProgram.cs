using CommunityToolkit.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.Logging;
using Microcharts.Maui;
using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Storage;
using System.Diagnostics;
using Microsoft.Maui.Controls.PlatformConfiguration;
using AnotadorGymApp.RutinasPage;
using AnotadorGymApp.MainPageViews;
using AnotadorGymApp.MetricasPageViews;
using AnotadorGymApp.Data;
using AnotadorGymApp.ConfiguracionPage;
using AnotadorGymApp.Services;

namespace AnotadorGymApp
{
    public static class MauiProgram
    {   
        public static MauiApp CreateMauiApp()
        {            
            string ruta = Path.Combine(FileSystem.AppDataDirectory, "GymApp.db");
#if DEBUG
            if (File.Exists(ruta))
            {
                Debug.WriteLine(ruta);
                //Preferences.Set("PrimerArranque", true);
                //File.Delete(ruta);
            }
#endif

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMicrocharts()
                .UseMauiCommunityToolkit()
                .Services
                .AddSingleton<DataService>()                                                
                .AddSingleton<ConfigPage>()    
                .AddSingleton<ImagenPersistenteService>()
                .AddSingleton<ConfigService>();
            
            builder.Services.AddDbContext<DataBase>(
                options =>
                {
                    options.UseSqlite($"Data Source={ruta}",sqliteOptions => { sqliteOptions.MigrationsAssembly("AnotadorGymApp.Data"); });                    
                }
            );

#if DEBUG
            builder.Logging.AddDebug();
#endif
            return builder.Build();
        }        

    }
}
