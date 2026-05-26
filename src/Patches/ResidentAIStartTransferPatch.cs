using HarmonyLib;
using ODMatrix.Aggregation;

namespace ODMatrix.Patches
{
    [HarmonyPatch(typeof(ResidentAI), "StartTransfer")]
    internal static class ResidentAIStartTransferPatch
    {
        private static void Prefix(uint citizenID, ref Citizen data, TransferManager.TransferReason reason, TransferManager.TransferOffer offer)
        {
            ResidentTravelCapture.RecordResidentTransfer(citizenID, data, reason, offer, "ResidentAI.StartTransfer");
        }
    }
}
