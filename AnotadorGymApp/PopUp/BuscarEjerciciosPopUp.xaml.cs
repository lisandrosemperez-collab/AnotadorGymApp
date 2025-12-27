using AnotadorGymApp.Data;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Views;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace AnotadorGymApp.PopUp;

public partial class BuscarEjerciciosPopUp : Popup
{
    public readonly DataService _dataservice;    
    public List<Exercise> TodosEjercicios { get; set; } = new List<Exercise>();
    public ObservableCollection<Exercise> FiltradosEjercicios { get; set; } = new ObservableCollection<Exercise>();
    public ObservableCollection<Exercise> SeleccionadosEjercicios { get; set; } = new ObservableCollection<Exercise>();
    public List<int> seleccionadosEjercicios { get; set; } = new List<int>();
    public BuscarEjerciciosPopUp(DataService dataService)
	{
        InitializeComponent();
        _dataservice = dataService;        
        TodosEjercicios = _dataservice._database.Exercises.ToList();
        SeleccionadosCollectionView.ItemsSource = SeleccionadosEjercicios;
        FiltradosCollectionView.ItemsSource = FiltradosEjercicios;
        // Limpiar selecciones anteriores al abrir
        _dataservice.ExercisesAgregarEjerciciosRutina.Clear();
    }
    private async void EjerciciosSearchBar_TextChanged(object sender, TextChangedEventArgs e)
    {        
        try
        {
            await FilterEjerciciosAsync(e.NewTextValue ?? string.Empty);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Excepción en TextChanged: {ex}");
        }
    }
    private async Task FilterEjerciciosAsync(string filtro)
    {
        try
        {
            await Task.Delay(250);
            var filtrolower = filtro.ToLower();

            var resultados = TodosEjercicios
            .Where(x => x.Name.ToLower().Contains(filtrolower))
            .Take(4)
            .ToList();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                FiltradosEjercicios.Clear();
                foreach (var r in resultados)
                {
                    FiltradosEjercicios.Add(r);
                }
            });
        }        
        catch (Exception ex) { Debug.WriteLine($"Error filtrando ejercicios: {ex}"); }        
    }

    private void FiltradosCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Exercise seleccionado)
        {
            if (SeleccionadosEjercicios.Any(ej => ej.Name == seleccionado.Name))
            {
                FiltradosCollectionView.SelectedItem = null;
                return;
            }
            SeleccionadosEjercicios.Add(e.CurrentSelection.FirstOrDefault() as Exercise);
            FiltradosCollectionView.SelectedItem = null;
        }
    }

    private void BorrarEjercicio_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is Exercise ex)
        {
            SeleccionadosEjercicios.Remove(ex);
        }        
    }
    
    private async void AceptarButton_Clicked(object sender, EventArgs e)
    {
        var seleccionados = SeleccionadosEjercicios.ToList();        
        _dataservice.AgregarIdsEjercicios(seleccionados);
        try
        {
            await CloseAsync();                
        }catch (Exception ex) {
            Debug.WriteLine(ex);                         
        }
    }

    private async void CancelarButton_Clicked(object sender, EventArgs e)
    {
        _dataservice.ExercisesAgregarEjerciciosRutina.Clear();
        await this.CloseAsync();
    }
}