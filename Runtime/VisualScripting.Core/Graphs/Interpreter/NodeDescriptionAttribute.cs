using System;
using JetBrains.Annotations;

namespace Unity.VisualScripting.Interpreter
{
    [AttributeUsage(AttributeTargets.Struct), MeansImplicitUse]
    public class NodeDescriptionAttribute : Attribute
    {
        public Type ModelType;


        public string[] UnmappedPorts { get; set; }

        /// <summary>
        /// Used when one unit can be translated to one among many units, and an enum on the unit is used to pick one
        /// </summary>
        public object SpecializationOf { get; set; }

        public NodeDescriptionAttribute(Type modelType)
        {
            ModelType = modelType;
        }

        public static string ToString(Type modelType)
        {
            return $"[{nameof(NodeDescriptionAttribute).Replace("Attribute", null)}(typeof({modelType}))]";
        }
    }
}
