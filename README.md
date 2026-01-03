# 🏋️ Anotador Gym App - .NET MAUI

[![.NET MAUI](https://img.shields.io/badge/.NET%20MAUI-8.0-blueviolet)](https://dotnet.microsoft.com/apps/maui)
[![C#](https://img.shields.io/badge/C%23-12.0-green)](https://docs.microsoft.com/dotnet/csharp/)
[![GitHub stars](https://img.shields.io/github/stars/lisandrosemperez-collab/AnotadorGymApp)](https://github.com/lisandrosemperez-collab/AnotadorGymApp/stargazers)

<div align="center">
  <img src="https://raw.githubusercontent.com/lisandrosemperez-collab/AnotadorGymApp/master/screenshots/Splash.jpeg" width="300">
  <br>
  <em>Splash screen de la aplicación</em>
</div>

Una aplicación móvil nativa y multiplataforma para el seguimiento profesional de rutinas de entrenamiento, desarrollada completamente en **.NET MAUI**. Implementa una arquitectura **MVVM** robusta, persistencia de datos local con **SQLite** y una interfaz de usuario moderna con temas dinámicos.

### Interfaz Principal
| Tema Claro | Tema Oscuro |
| :---: | :---: |
| <img src="https://raw.githubusercontent.com/lisandrosemperez-collab/AnotadorGymApp/master/screenshots/MainLightTheme.jpeg" width="250"> | <img src="https://raw.githubusercontent.com/lisandrosemperez-collab/AnotadorGymApp/master/screenshots/MainDarkTheme.jpeg" width="250" > |

### Funcionalidades Clave
| Gestión Rutinas | Seguimiento | Gráficos | Configuración |
| :---: | :---: | :---: | :---: |
| <img src="https://raw.githubusercontent.com/lisandrosemperez-collab/AnotadorGymApp/master/screenshots/Rutines.jpeg" width="250"> | <img src="https://raw.githubusercontent.com/lisandrosemperez-collab/AnotadorGymApp/master/screenshots/ChartsViews.jpeg" width="250" > | <img src="https://raw.githubusercontent.com/lisandrosemperez-collab/AnotadorGymApp/master/screenshots/ChartsViews1.jpeg" width="250" > | <img src="https://raw.githubusercontent.com/lisandrosemperez-collab/AnotadorGymApp/master/screenshots/Config.jpeg" width="250"> |

## ✨ Características Principales

### 🏗️ Arquitectura y Diseño
- **Arquitectura MVVM** con separación clara de responsabilidades
- **Inyección de dependencias** manual para servicios principales
- **Patrón Repository** implementado en `DataService`
- **Navegación con Shell** para experiencia fluida entre páginas

### 💾 Persistencia de Datos
- **SQLite** con **Entity Framework Core** para almacenamiento local
- **Migraciones automáticas** y manejo optimizado de esquema
- **Importación masiva eficiente** de ejercicios (1,000+ registros)
- **Modelo relacional completo**: `Rutina` → `Exercise` → `ExercisesLogs`

### 🎨 Experiencia de Usuario
- **Temas claro/oscuro dinámicos** con `DynamicResource`
- **Gráficos interactivos** para seguimiento visual del progreso
- **UI responsive** y adaptable a diferentes tamaños de pantalla
- **Converters personalizados** (`TimeFilterToColorConverter`) para lógica de UI

### ⚡ Optimizaciones de Rendimiento
- **Carga por lotes (batching)** para operaciones masivas de base de datos
- **Caché en memoria** con diccionarios para búsquedas O(1)
- **Configuración SQLite optimizada** (WAL mode, ajustes temporales)
- **Progreso en tiempo real** durante operaciones largas

## 🚀 Cómo Ejecutar el Proyecto

### Prerrequisitos
- **Visual Studio 2022** (versión 17.8 o superior)
- **Carga de trabajo ".NET Multi-platform App UI development"**
- **.NET 8.0 SDK** o superior
- Dispositivo Android físico/emulador o entorno iOS configurado

### Instalación
```bash
# 1. Clonar el repositorio
git clone https://github.com/lisandrosemperez-collab/AnotadorGymApp.git
cd AnotadorGymApp

# 2. Abrir en Visual Studio
# 3. Seleccionar plataforma objetivo (Android/iOS)
# 4. Compilar y ejecutar (F5)
``` 

### Configuración Inicial
La aplicación creará automáticamente la base de datos SQLite en el primer inicio

Importará la base de ejercicios predefinida (~1,000 ejercicios)

Configurar preferencias de tema en la página de ajustes

## ⚙️ Decisiones Técnicas Destacadas
### 1. Optimización de Carga Masiva (DataService.cs)
```csharp
// Estrategias implementadas:
// • Diccionarios en memoria para búsquedas O(1)
// • Procesamiento por lotes de 100 elementos (TAMANO_BATCH)
// • Transacciones explícitas para integridad de datos
// • Configuración temporal SQLite (WAL mode) para máximo rendimiento
// • Seguimiento de progreso en tiempo real con INotifyPropertyChanged
```

### 2. Arquitectura de Servicios
```csharp
// Inyección manual de dependencias en App.xaml.cs
public App(DataService dataService, ConfigService configService, 
           ImagenPersistenteService imagenPersistenteService)
{
    // Inicialización con servicios inyectados
}
```

## 🛠️ Stack Tecnológico
| Categoría | Tecnologías |
|-----------|-------------|
| **Plataforma** | .NET MAUI 8.0, XAML, C# 12 |
| **Base de Datos** | SQLite, Entity Framework Core 8 |
| **Arquitectura** | MVVM, Repository Pattern, DI |
| **UI/UX** | Data Binding, Styles, Converters, Shell Navigation |
| **Desarrollo** | Visual Studio 2022, Git, GitHub |
| **Optimización** | WAL Mode, Batching, Caching Strategies |

## 📈 Roadmap y Mejoras Futuras
### Próximas Versiones
- [ ] **Sincronización en la nube** con backend .NET Web API
- [ ] **Autenticación de usuarios** y perfiles personalizados
- [ ] **Widgets para pantalla de inicio** (Android/iOS)
- [ ] **Exportación/Importación** de rutinas en formato JSON
- [ ] **Compartir rutinas** con otros usuarios

### Mejoras Técnicas Planeadas
- [ ] **Implementar `BaseViewModel`** para reducir código repetitivo
- [ ] **Suite de pruebas unitarias** con xUnit/NUnit
- [ ] **Logging estructurado** con Serilog o equivalente
- [ ] **CI/CD pipeline** para builds automáticos
- [ ] **Internacionalización** (español/inglés/portugués)

## ✉️ Contacto y Soporte
**Desarrollador:** [Lisandro Semperez](https://github.com/lisandrosemperez-collab)

- **Reportar un problema**: [Issues](https://github.com/lisandrosemperez-collab/AnotadorGymApp/issues)
- **Solicitar una funcionalidad**: [Discussions](https://github.com/lisandrosemperez-collab/AnotadorGymApp/discussions)

⭐ **Si este proyecto te resulta útil, ¡considera darle una estrella en GitHub!**
