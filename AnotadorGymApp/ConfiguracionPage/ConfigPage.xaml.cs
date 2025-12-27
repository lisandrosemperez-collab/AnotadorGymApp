using AnotadorGymApp.Data;
using AnotadorGymApp.Services;
using System.Diagnostics;

namespace AnotadorGymApp.ConfiguracionPage;

public partial class ConfigPage : ContentPage
{
	private readonly ConfigService _configService;
	private readonly DataService _dataService;
	public ConfigPage(DataService dataService,ConfigService configService)
	{
		InitializeComponent();
		_dataService = dataService;
		_configService = configService;
        TemaToggleSwitch.IsToggled = _configService.TemaOscuro;
	}

    private void OnTemaToggled(object sender, ToggledEventArgs e)
    {
        _configService.GuardarTema(e.Value);
    }

    private async void OnResetearEntrenamientosClicked(object sender, EventArgs e)
    {
        bool confirmar = await DisplayAlert(
            "⚠️ Resetear Entrenamientos",
            "¿Estás seguro de que quieres eliminar TODOS los entrenamientos guardados? Esta acción no se puede deshacer.",
            "Sí, eliminar",
            "Cancelar");

        if (confirmar)
        {
            try
            {
                await _dataService.EliminarTodosLosEntrenamientos();
                await DisplayAlert("✅ Listo", "Todos los entrenamientos han sido eliminados", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("❌ Error", $"No se pudieron eliminar los entrenamientos: {ex.Message}", "OK");
            }
        }
    }

    private async void OnBorrarEjerciciosClicked(object sender, EventArgs e)
    {
        bool confirmar = await DisplayAlert(
            "⚠️ Borrar Ejercicios",
            "¿Eliminar todos los ejercicios personalizados? Los ejercicios por defecto se mantendrán.",
            "Sí, borrar",
            "Cancelar");

        if (confirmar)
        {
            try
            {
                await _dataService.EliminarEjerciciosPorDefecto();
                await DisplayAlert("✅ Listo", "Ejercicios personalizados eliminados", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("❌ Error", $"Error al eliminar ejercicios: {ex.Message}", "OK");
            }
        }
    }

    private async void OnRestablecerEjerciciosClicked(object sender, EventArgs e)
    {
        bool confirmar = await DisplayAlert(
            "🔄 Restablecer Ejercicios",
            "¿Restablecer todos los ejercicios a los valores por defecto? Se perderán los cambios personalizados.",
            "Sí, restablecer",
            "Cancelar");

        if (confirmar)
        {
            try
            {
                await _dataService.EliminarEjerciciosPorDefecto();
                Debug.WriteLine("Ejercicios Eliminados");
                await _configService.CargarExercisesInicialesAsync(_dataService);
                Debug.WriteLine("Ejercicios Cargados");
                await DisplayAlert("✅ Listo", "Ejercicios restablecidos a valores por defecto", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("❌ Error", $"Error al restablecer ejercicios: {ex.Message}", "OK");
            }
        }
    }
}