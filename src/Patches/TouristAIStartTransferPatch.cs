using HarmonyLib;
using ODMatrix.Aggregation;
using ODMatrix.Models;

namespace ODMatrix.Patches
{
    [HarmonyPatch(typeof(TouristAI), "StartTransfer")]
    internal static class TouristAIStartTransferPatch
    {
        private static void Prefix(uint citizenID, ref Citizen data, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
        {
            ResidentTravelCapture.RecordTransfer(TravelerType.Tourist, citizenID, data, material, offer);
        }
    }
}
