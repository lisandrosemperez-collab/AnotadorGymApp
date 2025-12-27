using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnotadorGymApp.Services
{
    public class ImagenPersistenteService
    {
        private readonly string _carpetaImagenesUsuario;
        public ImagenPersistenteService()
        {
            _carpetaImagenesUsuario = Path.Combine(FileSystem.AppDataDirectory, "ImagenesUsuario");
            Directory.CreateDirectory(_carpetaImagenesUsuario);
        }

        public async Task<string> GuardarImagenUsuarioAsync(string nombreRutina, FileResult fileResult)
        {
            if (fileResult == null)
                throw new ArgumentNullException(nameof(fileResult));

            await using var stream = await fileResult.OpenReadAsync();
            return await GuardarImagenUsuarioAsync(nombreRutina, stream);
        }
        public async Task<string> GuardarImagenUsuarioAsync(string nombreRutina, Stream imagenStream)
        {
            // Generar nombre único
            var nombreArchivo = $"{NormalizarNombre(nombreRutina)}.jpg";
            var rutaCompleta = Path.Combine(_carpetaImagenesUsuario, nombreArchivo);
            try
            {
                if (File.Exists(rutaCompleta))
                {
                    File.Delete(rutaCompleta);
                    Debug.WriteLine($"🗑️ Imagen anterior eliminada: {rutaCompleta}");
                    await Task.Delay(100);
                }

                // Guardar imagen
                using var fileStream = new FileStream(rutaCompleta, FileMode.Create, FileAccess.Write);
                await imagenStream.CopyToAsync(fileStream);
                return rutaCompleta;
            }
            catch (IOException ioEx)
            {
                // Si hay error de IO (archivo en uso), usar nombre alternativo
                Debug.WriteLine($"⚠️ Error IO, usando nombre alternativo: {ioEx.Message}");

                var nombreAlternativo = $"{NormalizarNombre(nombreRutina)}.jpg";
                var rutaAlternativa = Path.Combine(_carpetaImagenesUsuario, nombreAlternativo);

                using var fileStream = new FileStream(rutaAlternativa, FileMode.Create, FileAccess.Write);
                await imagenStream.CopyToAsync(fileStream);

                return rutaAlternativa;
            }
        }
        public async Task<string> ObtenerRutaImagenDefault(string nombreRutina)
        {
            var ruta = Path.Combine("Resources", "Images", "RutinasImages","rutina_default.jpg");
            try
            {
                var stream = await FileSystem.OpenAppPackageFileAsync(ruta);
                stream.Dispose();
                return ruta;
            }
            catch (IOException ioEx)
            {
                return null;
            }
        }
        public async Task<string> CopiarImagenEmbebidaAsync(string nombreArchivoEmbebido)
        {
            try
            {
                nombreArchivoEmbebido = Path.GetFileName(nombreArchivoEmbebido);
                nombreArchivoEmbebido = NormalizarNombre(nombreArchivoEmbebido);

                var rutaRaw = Path.Combine("Resources", "Images", "RutinasImages", nombreArchivoEmbebido);

                var rutaDestino = Path.Combine(_carpetaImagenesUsuario, nombreArchivoEmbebido);
                if (File.Exists(rutaDestino))
                {
                    Debug.WriteLine($"✅ Imagen ya existe: {rutaDestino}");
                    return rutaDestino;
                }

                var ubicaciones = new[]
                {
                    nombreArchivoEmbebido,
                    Path.Combine("Resources", "Images", "RutinasImages", nombreArchivoEmbebido),
                    Path.Combine("Resources", "Images", nombreArchivoEmbebido),
                    Path.Combine("Images", "RutinasImages", nombreArchivoEmbebido),
                    Path.Combine("Images", nombreArchivoEmbebido),
                    Path.Combine("RutinasImages", nombreArchivoEmbebido)
                };


                using var stream = await FileSystem.OpenAppPackageFileAsync(rutaRaw);
                using var fileStream = new FileStream(rutaDestino, FileMode.Create, FileAccess.Write);
                await stream.CopyToAsync(fileStream);

                Debug.WriteLine($"✅ Copiada: {nombreArchivoEmbebido} desde {rutaDestino}");
                return rutaDestino;

                //foreach (var rutaEmbebida in ubicaciones)
                //{
                //    try
                //    {
                //        using var stream = await FileSystem.OpenAppPackageFileAsync(rutaEmbebida);
                //        using var fileStream = new FileStream(rutaDestino, FileMode.Create, FileAccess.Write);
                //        await stream.CopyToAsync(fileStream);

                //        Debug.WriteLine($"✅ Copiada: {nombreArchivoEmbebido} desde {rutaEmbebida}");
                //        return rutaDestino;
                //    }
                //    catch (FileNotFoundException)
                //    {
                //        // Continuar con siguiente ubicación
                //        continue;
                //    }
                //}
                // ❌ Si no se encuentra en ninguna ubicación
                //Debug.WriteLine($"⚠️ No se encontró: {nombreArchivoEmbebido} en recursos embebidos");
                return null;            

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error copiando {nombreArchivoEmbebido}: {ex}");
                return null;
            }                        
        }

        private string NormalizarNombre(string nombre)
        {
            return nombre.ToLower()
                .Replace(" ", "_")
                .Replace("-", "_")
                .Replace("/", "_")
                .Replace("\\", "_")
                .Replace(":", "")
                .Replace("?", "")
                .Replace("*", "");
        }
    }
}
