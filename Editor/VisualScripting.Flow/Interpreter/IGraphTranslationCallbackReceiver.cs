using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;

namespace Unity.VisualScripting.Interpreter
{
    public interface IGraphTranslationCallbackReceiver
    {
        void OnCacheInitialization();
    }

    [UsedImplicitly]
    class GraphTranslationCallbackReceiver : IGraphTranslationCallbackReceiver
    {
        internal static Dictionary<Type, Type> CreateListModelToRuntimeMapping;
        internal static Dictionary<Type, Type> LiteralModelToRuntimeMapping;
        internal static Dictionary<Type, IConstantBuilder> CustomConstantBuilders;
        internal static Dictionary<string, Type> InvokeMemberModelToRuntimeMapping;
        internal static Dictionary<string, Type> SetMemberModelToRuntimeMapping;

        public void OnCacheInitialization()
        {
            LiteralModelToRuntimeMapping = new Dictionary<Type, Type>();
            foreach (var type in TypeCache.GetTypesWithAttribute<ConstantNodeAttribute>())
            {
                var attr = type.GetAttribute<ConstantNodeAttribute>();
                LiteralModelToRuntimeMapping.Add(attr.Type, type);
            }

            CustomConstantBuilders = new Dictionary<Type, IConstantBuilder>();
            foreach (var type in TypeCache.GetTypesDerivedFrom<IConstantBuilder>().Where(t => !t.IsAbstract))
            {
                var translator = (IConstantBuilder)Activator.CreateInstance(type);
                CustomConstantBuilders.Add(translator.Type, translator);
            }

            CreateListModelToRuntimeMapping = new Dictionary<Type, Type>();
            foreach (var type in TypeCache.GetTypesWithAttribute<ListNodeAttribute>())
            {
                var attr = type.GetAttribute<ListNodeAttribute>();
                CreateListModelToRuntimeMapping.Add(attr.ElementType, type);
            }

            InvokeMemberModelToRuntimeMapping = new Dictionary<string, Type>();
            SetMemberModelToRuntimeMapping = new Dictionary<string, Type>();
            foreach (var type in TypeCache.GetTypesWithAttribute<MemberNodeAttribute>())
            {
                var attr = type.GetAttribute<MemberNodeAttribute>();
                if (attr.ModelType == typeof(SetMember))
                    SetMemberModelToRuntimeMapping.Add(attr.Member, type);
                else
                    InvokeMemberModelToRuntimeMapping.Add(attr.Member, type);
            }
        }
    }
}
