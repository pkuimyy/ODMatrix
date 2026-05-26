using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using ODMatrix.Aggregation;
using UnityEngine;

namespace ODMatrix.Patches
{
    [HarmonyPatch]
    internal static class PathManagerCreatePathPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            MethodInfo[] methods = typeof(PathManager).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name == "CreatePath")
                {
                    yield return methods[i];
                }
            }
        }

        private static void Prefix(PathManager __instance, MethodBase __originalMethod, object[] __args)
        {
            string sourceTag = __originalMethod == null
                ? "PathManager.CreatePath"
                : __originalMethod.DeclaringType.FullName + "." + __originalMethod.Name;

            string detail = BuildPathDetail(__instance, __originalMethod, __args);
            ResidentTravelCapture.RecordPathRequest(sourceTag, detail);
        }

        private static string BuildPathDetail(PathManager pathManager, MethodBase originalMethod, object[] args)
        {
            if (pathManager == null || originalMethod == null || args == null)
            {
                return null;
            }

            ParameterInfo[] parameters = originalMethod.GetParameters();
            PathUnit.Position? startPositionA = null;
            PathUnit.Position? startPositionB = null;
            PathUnit.Position? endPositionA = null;
            PathUnit.Position? endPositionB = null;
            string laneTypes = null;
            string vehicleTypes = null;
            string vehicleCategories = null;
            string maxLength = null;

            for (int i = 0; i < parameters.Length && i < args.Length; i++)
            {
                string parameterName = parameters[i].Name;
                System.Type parameterType = parameters[i].ParameterType;
                if (parameterType.IsByRef)
                {
                    parameterType = parameterType.GetElementType();
                }

                if (parameterType == typeof(PathUnit.Position))
                {
                    PathUnit.Position position = (PathUnit.Position)args[i];
                    if (parameterName == "startPos" || parameterName == "startPosA")
                    {
                        startPositionA = position;
                    }
                    else if (parameterName == "startPosB")
                    {
                        startPositionB = position;
                    }
                    else if (parameterName == "endPos" || parameterName == "endPosA")
                    {
                        endPositionA = position;
                    }
                    else if (parameterName == "endPosB")
                    {
                        endPositionB = position;
                    }

                    continue;
                }

                if (parameterName == "laneTypes")
                {
                    laneTypes = args[i].ToString();
                }
                else if (parameterName == "vehicleTypes")
                {
                    vehicleTypes = args[i].ToString();
                }
                else if (parameterName == "vehicleCategories")
                {
                    vehicleCategories = args[i].ToString();
                }
                else if (parameterName == "maxLength")
                {
                    maxLength = args[i].ToString();
                }
            }

            return string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "Method={0}; StartA={1}; StartB={2}; EndA={3}; EndB={4}; LaneTypes={5}; VehicleTypes={6}; VehicleCategories={7}; MaxLength={8}",
                originalMethod.Name,
                FormatPosition(pathManager, startPositionA),
                FormatPosition(pathManager, startPositionB),
                FormatPosition(pathManager, endPositionA),
                FormatPosition(pathManager, endPositionB),
                laneTypes,
                vehicleTypes,
                vehicleCategories,
                maxLength);
        }

        private static string FormatPosition(PathManager pathManager, PathUnit.Position? pathPosition)
        {
            if (!pathPosition.HasValue)
            {
                return "null";
            }

            PathUnit.Position position = pathPosition.Value;
            Vector3 worldPosition = PathManager.CalculatePosition(position);

            return string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "Seg={0},Lane={1},Offset={2},World=({3:F2},{4:F2},{5:F2})",
                position.m_segment,
                position.m_lane,
                position.m_offset,
                worldPosition.x,
                worldPosition.y,
                worldPosition.z);
        }
    }
}
