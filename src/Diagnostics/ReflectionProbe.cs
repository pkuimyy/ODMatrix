using System;
using System.Linq;
using System.Reflection;

namespace ODMatrix.Diagnostics
{
    internal static class ReflectionProbe
    {
        internal static void Run()
        {
            try
            {
                Assembly gameAssembly = typeof(ResidentAI).Assembly;

                ModLogger.Info("Starting dependency reflection probe.");
                ProbeMethod(gameAssembly, "ResidentAI", "StartTransfer");
                ProbeMethod(gameAssembly, "TouristAI", "StartTransfer");
                ProbeMethod(gameAssembly, "PathManager", "CreatePath");
                ProbeMethod(gameAssembly, "CitizenInstance", "GetLastFramePosition");
                ProbeField(gameAssembly, "Building", "m_position");
                ProbeProperty(gameAssembly, "InstanceID", "Building");
                ProbeProperty(gameAssembly, "InstanceID", "CitizenInstance");
                ProbeProperty(gameAssembly, "InstanceID", "RawData");
                ProbeProperty(gameAssembly, "Citizen", "CurrentLocation");
                ModLogger.Info("Dependency reflection probe completed.");
            }
            catch (Exception ex)
            {
                ModLogger.Error("Dependency reflection probe failed.", ex);
            }
        }

        private static void ProbeMethod(Assembly assembly, string typeName, string methodName)
        {
            Type targetType = FindType(assembly, typeName);
            if (targetType == null)
            {
                ModLogger.Warn("Reflection probe could not find type " + typeName + ".");
                return;
            }

            MethodInfo[] matches = targetType
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Where(method => method.Name == methodName)
                .ToArray();

            if (matches.Length == 0)
            {
                ModLogger.Warn("Reflection probe could not find method " + targetType.FullName + "." + methodName + ".");
                return;
            }

            for (int i = 0; i < matches.Length; i++)
            {
                ModLogger.Info("Reflection method found: " + FormatMethod(matches[i]));
            }
        }

        private static void ProbeField(Assembly assembly, string typeName, string fieldName)
        {
            Type targetType = FindType(assembly, typeName);
            if (targetType == null)
            {
                ModLogger.Warn("Reflection probe could not find type " + typeName + ".");
                return;
            }

            FieldInfo field = targetType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (field == null)
            {
                ModLogger.Warn("Reflection probe could not find field " + targetType.FullName + "." + fieldName + ".");
                return;
            }

            ModLogger.Info("Reflection field found: " + targetType.FullName + "." + field.Name + " : " + field.FieldType.FullName);
        }

        private static void ProbeProperty(Assembly assembly, string typeName, string propertyName)
        {
            Type targetType = FindType(assembly, typeName);
            if (targetType == null)
            {
                ModLogger.Warn("Reflection probe could not find type " + typeName + ".");
                return;
            }

            PropertyInfo property = targetType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (property == null)
            {
                ModLogger.Warn("Reflection probe could not find property " + targetType.FullName + "." + propertyName + ".");
                return;
            }

            ModLogger.Info("Reflection property found: " + targetType.FullName + "." + property.Name + " : " + property.PropertyType.FullName);
        }

        private static Type FindType(Assembly assembly, string typeName)
        {
            Type directType = assembly.GetType(typeName);
            if (directType != null)
            {
                return directType;
            }

            return assembly.GetTypes().FirstOrDefault(type => type.Name == typeName || type.FullName == typeName);
        }

        private static string FormatMethod(MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();
            string[] parts = new string[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                parts[i] = FormatParameter(parameters[i]);
            }

            return method.DeclaringType.FullName + "." + method.Name + "(" + string.Join(", ", parts) + ") : " + method.ReturnType.FullName;
        }

        private static string FormatParameter(ParameterInfo parameter)
        {
            Type parameterType = parameter.ParameterType;
            string prefix = string.Empty;

            if (parameterType.IsByRef)
            {
                prefix = "ref ";
                parameterType = parameterType.GetElementType();
            }

            return prefix + parameterType.FullName + " " + parameter.Name;
        }
    }
}
