using HarmonyLib;
using ODMatrix.Aggregation;

namespace ODMatrix.Patches
{
    [HarmonyPatch(typeof(TouristAI), "StartTransfer")]
    internal static class TouristAIStartTransferPatch
    {
        private static void Prefix(uint citizenID, ref Citizen data, TransferManager.TransferReason material, TransferManager.TransferOffer offer)
        {
            ResidentTravelCapture.RecordTouristTransfer(citizenID, data, material, offer, "TouristAI.StartTransfer");
        }
    }
}
