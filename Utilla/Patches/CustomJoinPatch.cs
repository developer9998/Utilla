using GorillaNetworking;
using HarmonyLib;

namespace Utilla.Patches;

[HarmonyPatch(typeof(PhotonNetworkController), nameof(PhotonNetworkController.OnJoinedRoom))]
internal class CustomJoinPatch
{
    public static void Prefix(out string[] __state)
    {
        __state = GorillaComputer.instance.allowedMapsToJoin;

        string[] newMaps = new string[__state.Length + 1];
        GorillaComputer.instance.allowedMapsToJoin.CopyTo(newMaps, 0);
        newMaps[^1] = "MOD_";

        GorillaComputer.instance.allowedMapsToJoin = newMaps;
    }

    public static void Postfix(string[] __state)
    {
        GorillaComputer.instance.allowedMapsToJoin = __state;
    }
}