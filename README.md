# RemoveStartTemple

Parche BepInEx para retirar `StartTemple` del catálogo de ubicaciones antes de que el juego genere el mundo. Las búsquedas vanilla del punto de inicio se redirigen al origen del mundo (`x=0, z=0`). La posición vertical final depende de la lógica normal de aparición del jugador.

## Interceptaciones

Inspeccioné `assembly_valheim.dll` del servidor local. La búsqueda directa del punto de inicio encontrada en el código vanilla es `Game.FindSpawnPoint`, que llama a `ZoneSystem.GetLocationIcon(Game.m_StartLocation)`; `Game.m_StartLocation` vale `StartTemple` de forma predeterminada. El parche intercepta:

1. `ZoneSystem.SetupLocations` (postfix): elimina cada `StartTemple` de `m_locations` y de `m_locationsByHash`, evitando que participe en la generación y en las consultas por hash.
2. `ZoneSystem.GetLocationIcon(string, out Vector3)` (prefix): para `StartTemple`, devuelve `Vector3.zero` y éxito. Esto redirige la búsqueda que hace `Game.FindSpawnPoint` al origen. Las demás ubicaciones continúan con el comportamiento vanilla.

No encontré otra llamada vanilla que busque directamente `StartTemple` por nombre. El parche no cambia las búsquedas de otras ubicaciones ni el manejo de puntos de aparición personalizados o de reaparición tras una muerte.

## Depuración

El log `[RemoveStartTemple]` registra la inicialización y retirada del parche. `[WorldGen]` informa de la comprobación de ubicaciones y de la retirada de cada entrada de la lista e índice hash (o de cualquier problema al hacerlo). `[Spawn]` registra una sola vez la redirección del punto de inicio al origen, para evitar llenar el log si el juego repite la consulta.

Este cambio afecta la generación de ubicaciones del mundo. Para comprobarlo desde el inicio, usa un mundo nuevo; las ubicaciones ya guardadas pueden permanecer en zonas generadas anteriormente.

Para compilar, define `ValheimServerDir` o las propiedades `ValheimManagedDir` y `BepInExCoreDir` con las rutas de las DLL correspondientes.
