# Custom Research View (RimWorld 1.6)

Este repositorio contiene un Mod para RimWorld 1.6 que reemplaza la vista estándar de
investigaciones utilizando Harmony. El nuevo panel organiza los proyectos por nivel
tecnológico, muestra el progreso actual, los requisitos y permite activar rápidamente la
investigación deseada.

## Estructura del proyecto

- `About/` &mdash; Metadatos del Mod utilizados por RimWorld.
- `Assemblies/` &mdash; Carpeta donde debe ubicarse el ensamblado compilado (`CustomResearchView.dll`).
- `Source/ResearchViewReplacer/` &mdash; Código fuente C# con los parches de Harmony.

## Compilación

1. Cree un archivo `Directory.Build.props` en la carpeta `Source/` (o exporte variables de
   entorno) que establezca las rutas hacia la carpeta `Managed/` de su instalación de
   RimWorld y de Unity. Ejemplo:

   ```xml
   <Project>
     <PropertyGroup>
       <RimWorldManagedPath>C:\\Steam\\steamapps\\common\\RimWorld\\RimWorldWin64_Data\\Managed\\</RimWorldManagedPath>
       <UnityManagedPath>C:\\Steam\\steamapps\\common\\RimWorld\\RimWorldWin64_Data\\Managed\\</UnityManagedPath>
     </PropertyGroup>
   </Project>
   ```

   No es necesario añadir referencias adicionales a `Verse.dll` o `RimWorld.dll`; los
   espacios de nombres `Verse` y `RimWorld` se encuentran dentro de `Assembly-CSharp.dll`
   en la versión 1.6.

2. Desde `Source/ResearchViewReplacer/` ejecute `dotnet build -c Release`.
3. Copie el ensamblado generado (`bin/Release/net48/CustomResearchView.dll`) a la carpeta
   `Assemblies/` del Mod.
4. Active el Mod en el menú de contenido de RimWorld.

## Notas

- El parche principal sustituye el método `FillTab` de `MainTabWindow_Research` y dibuja una
  vista desplazable con categorías por nivel tecnológico, barra de progreso y botón para
  activar la investigación.
- Se utilizan traducciones existentes del juego para mantener compatibilidad con otros
  idiomas; los textos personalizados cuentan con claves fallback en inglés.

¡Disfruta de una vista de investigación más clara y directa!
