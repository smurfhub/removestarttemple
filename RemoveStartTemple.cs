using BepInEx;

namespace RemoveStartTemple
{
    [BepInPlugin("es.uyimbube.removestarttemple", "RemoveStartTemple", "0.1.0")]
    public class RemoveStartTemplePlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Logger.LogInfo("Paco fuma tabaco.");
        }
    }
}