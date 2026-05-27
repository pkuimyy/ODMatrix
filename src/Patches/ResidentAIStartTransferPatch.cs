using HarmonyLib;
using ODMatrix.Aggregation;
using ODMatrix.Models;

namespace ODMatrix.Patches
{
    [HarmonyPatch(typeof(ResidentAI), "StartTransfer")]
    internal static class ResidentAIStartTransferPatch
    {
        private static void Prefix(uint citizenID, ref Citizen data, TransferManager.TransferReason reason, TransferManager.TransferOffer offer)
        {
            ResidentTravelCapture.RecordTransfer(TravelerType.Resident, citizenID, data, reason, offer);
        }
    }
}
