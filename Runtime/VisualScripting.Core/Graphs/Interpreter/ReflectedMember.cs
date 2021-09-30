using System;
using System.Linq;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [Serializable]
    public struct ReflectedMember
    {
        public enum ParameterModifier { None, Ref, Out }
        [SerializeField] private Member.Source m_Type;
        [SerializeField] private string m_TargetTypeFullyQualifiedName;

        [SerializeField]
        private string[] m_ParameterTypes;

        [SerializeField]
        public ParameterModifier[] ParameterModifiers;

        [SerializeField]
        private string m_Name;

        [DoNotSerialize]
        private Member m_Member;

        public Type TargetType { get; private set; }
        public Type[] ParameterTypes { get; private set; }
        public bool RequiresTarget
        {
            get { EnsureReflected(); return m_Member.requiresTarget; }
        }
        public bool IsGettable
        {
            get { EnsureReflected(); return m_Member.isGettable; }
        }

        public ReflectedMember(Member m) : this()
        {
            m.EnsureReflected();
            m_Type = m.source;
            m_Name = m.name;
            TargetType = m.declaringType;
            m_TargetTypeFullyQualifiedName = TidyAssemblyTypeName(TargetType.AssemblyQualifiedName);
            m_ParameterTypes = m.parameterTypes == null ? new String[0] : m.parameterTypes.Select(t => TidyAssemblyTypeName(t.AssemblyQualifiedName)).ToArray();
            ParameterModifiers = m.parameterTypes == null ? new ParameterModifier[0] : m.GetParameterInfos().Select(t =>
                t.IsOut ? ParameterModifier.Out : t.ParameterType.IsByRef ? ParameterModifier.Ref : ParameterModifier.None).ToArray();
        }

        public void EnsureReflected()
        {
            if (m_Type == Member.Source.Unknown)
                return;
            if (m_Member == null)
            {
                TargetType = Type.GetType(m_TargetTypeFullyQualifiedName);
                ParameterTypes = m_ParameterTypes.Select(Type.GetType).ToArray();
                m_Member = new Member(TargetType, m_Name, ParameterTypes);
                m_Member.EnsureReflected();
            }
        }

        internal static string TidyAssemblyTypeName(string assemblyTypeName)
        {
            if (string.IsNullOrEmpty(assemblyTypeName))
                return assemblyTypeName;
            int num = int.MaxValue;
            int val1_1 = assemblyTypeName.IndexOf(", Version=");
            if (val1_1 != -1)
                num = Math.Min(val1_1, num);
            int val1_2 = assemblyTypeName.IndexOf(", Culture=");
            if (val1_2 != -1)
                num = Math.Min(val1_2, num);
            int val1_3 = assemblyTypeName.IndexOf(", PublicKeyToken=");
            if (val1_3 != -1)
                num = Math.Min(val1_3, num);
            if (num != int.MaxValue)
                assemblyTypeName = assemblyTypeName.Substring(0, num);
            int length = assemblyTypeName.IndexOf(", UnityEngine.");
            if (length != -1 && assemblyTypeName.EndsWith("Module"))
                assemblyTypeName = assemblyTypeName.Substring(0, length) + ", UnityEngine";
            return assemblyTypeName;
        }

        public object Get(object target)
        {
            EnsureReflected();
            return m_Member.Get(target);
        }

        public void Set(object target, object value)
        {
            EnsureReflected();
            m_Member.Set(target, value);
        }

        public object Invoke(object target, object[] args)
        {
            EnsureReflected();
            return m_Member.Invoke(target, args);
        }
    }
}
