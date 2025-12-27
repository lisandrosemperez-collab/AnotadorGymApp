using AnotadorGymApp.Data;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace AnotadorGymApp.RutinasPage;

public partial class PrincipalRutinasPage : ContentPage
{
    private readonly DataService _dataService;
    public ObservableCollection<Rutinas> rutinas { get; set; } = new ObservableCollection<Rutinas>();
    public ICommand StarRutinaCommand { get; private set; }
    public ICommand EditRutinaCommand { get; private set;}
    public ICommand FavRutinaCommand { get; private set; }
    public PrincipalRutinasPage(DataService dataservice)
    {
        InitializeComponent();
        _dataService = dataservice;        
        BindingContext = this;
        StarRutinaCommand = new Command<Rutinas>(StarRutina);
        EditRutinaCommand = new Command<Rutinas>(EditRutina);
        FavRutinaCommand = new Command<Rutinas>(FavRutina);        
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!rutinas.Any())
        {
            await CargarRutinas();
        }
    }

    private async Task CargarRutinas()
    {
        try
        {
            var listaRutinas = await _dataService.ObtenerRutinas();

            rutinas.Clear();            
            foreach(var rutina in listaRutinas)
            {
                var existe = File.Exists(rutina.ImageSource);
                if (!existe)
                {
                    Debug.WriteLine($"{rutina.Nombre} image source no existe: {rutina.ImageSource}");
                }
                rutinas.Add(rutina);
            }            
        }
        catch(Exception ex)
        {
            Debug.WriteLine("Error: ");
            Debug.WriteLine(ex);
        }        
    }
    private void FavRutina(Rutinas rutinas)
    {
        throw new NotImplementedException();
    }
    private async void AñadirRutinaButton_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"AgregarRutina?rutinaId={0}");                    
    }
    private async void EditRutina(Rutinas rutina)
    {        
        await Shell.Current.GoToAsync($"AgregarRutina?rutinaId={rutina.RutinaId}");
    }    
    private async void StarRutina(Rutinas rutina)
    {                        
        await Shell.Current.GoToAsync($"ComienzoRutina?rutinaId={rutina.RutinaId}");
    }
    private async void EliminarRutina_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is Rutinas rutina)
        {
            // Confirmación (opcional pero recomendable)
            bool confirmado = await Application.Current.MainPage.DisplayAlert(
                "Confirmar",
                $"¿Seguro que querés eliminar la rutina '{rutina.Nombre}'?",
                "Sí",
                "No");

            if (confirmado)
            {
                _dataService._database.Remove(rutina);
                rutinas.Remove(rutina);
                await _dataService._database.SaveChangesAsync();
            }
        }
    }
}

public class SeleccionPlantilla : DataTemplateSelector
{
    public DataTemplate SinImagenTemplate { get; set; }
    public DataTemplate ConImagenTemplate { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        if (item is Rutinas rutina)
        {
            return !string.IsNullOrEmpty(rutina.ImageSource)
                ? ConImagenTemplate
                : SinImagenTemplate;
        }

        return SinImagenTemplate;
    }
}
