using System;
using JetBrains.Annotations;
using Unity.VisualScripting.Interpreter;
using UnityEditor.Graphs;
using UnityEngine;

namespace Unity.VisualScripting
{
    interface IConstantBuilder
    {
        Type Type { get; }

        IConstantNode Build(object value);
    }

    abstract class ConstantBuilder<T> : IConstantBuilder
    {
        public Type Type => typeof(T);

        public abstract IConstantNode Build(object value);
    }

    [UsedImplicitly]
    class ConstantTypeBuilder : ConstantBuilder<Type>
    {
        public override IConstantNode Build(object value)
        {
            return new ConstantType { Type = new SerializableType(((Type)value).AssemblyQualifiedName) };
        }
    }

    [UsedImplicitly]
    class ConstantRayBuilder : ConstantBuilder<Ray>
    {
        public override IConstantNode Build(object value)
        {
            var ray = (Ray)value;
            return new ConstantRay { Origin = ray.origin, Direction = ray.direction };
        }
    }

    [UsedImplicitly]
    class ConstantRay2DBuilder : ConstantBuilder<Ray2D>
    {
        public override IConstantNode Build(object value)
        {
            var ray = (Ray2D)value;
            return new ConstantRay2D { Origin = ray.origin, Direction = ray.direction };
        }
    }
}
