# RemoveStartTemple

Retira `StartTemple` del catálogo de ubicaciones antes de generar el mundo y redirige su búsqueda vanilla al origen del mundo (`x=0, z=0`). La altura del jugador sigue la lógica normal de aparición.

## Interceptaciones vanilla

La inspección de `assembly_valheim.dll` encontró que `Game.FindSpawnPoint` consulta `ZoneSystem.GetLocationIcon(Game.m_StartLocation)`, y que `m_StartLocation` vale `StartTemple`. El mod intercepta:

1. `ZoneSystem.SetupLocations` (postfix): elimina `StartTemple` de la lista de ubicaciones y de su índice hash antes de generar el mundo.
2. `ZoneSystem.GetLocationIcon(string, out Vector3)` (prefix): cuando el nombre es `StartTemple`, devuelve `Vector3.zero` y éxito, redirigiendo `Game.FindSpawnPoint` al origen. Las otras ubicaciones conservan su comportamiento normal.

No se encontró otra búsqueda vanilla directa por nombre de `StartTemple`. El parche no cambia puntos de aparición personalizados ni reapariciones tras una muerte.

## Registro de depuración

El log `[RemoveStartTemple]` registra el ciclo de vida del parche. `[WorldGen]` registra la comprobación y la retirada de la ubicación y de su entrada hash. `[Spawn]` registra una vez la redirección al origen.

Usa un mundo nuevo para comprobar el cambio desde el inicio; las ubicaciones de zonas ya generadas pueden estar guardadas.
