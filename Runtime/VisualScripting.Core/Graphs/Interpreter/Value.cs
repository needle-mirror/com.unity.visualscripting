using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    /// <summary>
    /// The value type itself, stored in the GraphInstance's value array. Union of types
    /// </summary>
    [StructLayout(LayoutKind.Explicit), Serializable]
    public struct Value : IEquatable<Value>
    {
        public enum UndefinedValue { UndefinedValue }

        [FieldOffset(0)] public ValueType Type;

        [FieldOffset(sizeof(ValueType))] private bool _bool;
        [FieldOffset(sizeof(ValueType))] private int _int;
        [FieldOffset(sizeof(ValueType))] private float _float;
        [FieldOffset(sizeof(ValueType))] private Vector2 _float2;
        [FieldOffset(sizeof(ValueType))] private Vector3 _float3;
        [FieldOffset(sizeof(ValueType) + sizeof(int))] private ulong _enumType;

        [FieldOffset(sizeof(ValueType)), SerializeField]
        private Vector4 _float4;

        [FieldOffset(sizeof(ValueType))] private Quaternion _quaternion;
        [FieldOffset(sizeof(ValueType))] private Color _color;

        [FieldOffset(sizeof(ValueType))] private GCHandle _objectHandle;

        public static bool CanConvert(ValueType from, ValueType to, bool allowFloatToIntRounding)
        {
            // extracted from each property getter down this file
            if (from == to)
                return true;
            switch (to)
            {
                case ValueType.Quaternion:
                case ValueType.ManagedObject:
                case ValueType.Struct:
                case ValueType.Bool:
                    return false;
                case ValueType.Int:
                    return from == ValueType.Bool || (allowFloatToIntRounding && from == ValueType.Float);
                case ValueType.Float:
                    return from == ValueType.Int;
                case ValueType.Float2:
                case ValueType.Float3:
                case ValueType.Float4:
                    return from == ValueType.Int || from == ValueType.Float || from == ValueType.Float2 ||
                        from == ValueType.Float3 || from == ValueType.Float4;
                case ValueType.Unknown:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(nameof(to), to, null);
            }
        }

        public GCHandle Handle => _objectHandle;

        public bool Bool
        {
            get
            {
                VSAssert.AreEqual(Type, ValueType.Bool);
                return _bool;
            }
            set
            {
                Type = ValueType.Bool;
                _bool = value;
            }
        }

        public Enum GetBoxedEnum()
        {
            VSAssert.AreEqual(Type, ValueType.Enum);
            return (Enum)Enum.ToObject(TypeHash.GetType(_enumType), _int);
        }

        public int EnumValue
        {
            get
            {
                VSAssert.AreEqual(Type, ValueType.Enum);
                return _int;
            }
        }

        public void SetEnumValue(Enum value)
        {
            Type = ValueType.Enum;
            _int = Convert.ToInt32(value);
            _enumType = TypeHash.CacheStableTypeHash(value.GetType());
        }

        public void SetEnumValue<T>(T value) where T : struct, Enum
        {
            Type = ValueType.Enum;
            _int = UnsafeUtility.EnumToInt(value);
            _enumType = TypeHash.CacheStableTypeHash<T>();
        }

        public int Int
        {
            get
            {
                switch (Type)
                {
                    case ValueType.Bool:
                        return _bool ? 1 : 0;
                    case ValueType.Float:
                        return (int)_float;
                    case ValueType.Int:
                        return _int;
                    default: throw new InvalidDataException();
                }
            }
            set
            {
                Type = ValueType.Int;
                _int = value;
            }
        }

        public float Float
        {
            get
            {
                switch (Type)
                {
                    case ValueType.Float:
                        return _float;
                    case ValueType.Int:
                        return _int;
                    default: throw new InvalidDataException();
                }
            }
            set
            {
                Type = ValueType.Float;
                _float = value;
            }
        }

        public Vector2 Float2
        {
            get
            {
                switch (Type)
                {
                    case ValueType.Int:
                        return new Vector2(_int, 0);
                    case ValueType.Float:
                        return new Vector2(_float, 0);
                    case ValueType.Float2:
                        return _float2;
                    case ValueType.Float3:
                        return new Vector2(_float3.x, _float3.y);
                    case ValueType.Float4:
                        return new Vector2(_float4.x, _float4.y);
                    default: throw new InvalidDataException();
                }
            }
            set
            {
                Type = ValueType.Float2;
                _float2 = value;
            }
        }

        public Vector3 Float3
        {
            get
            {
                switch (Type)
                {
                    case ValueType.Int:
                        return new Vector3(_int, 0, 0);
                    case ValueType.Float:
                        return new Vector3(_float, 0, 0);
                    case ValueType.Float2:
                        return new Vector3(_float2.x, _float2.y, 0);
                    case ValueType.Float3:
                        return _float3;
                    case ValueType.Float4:
                        return new Vector3(_float4.x, _float4.y, _float4.z);
                    default: throw new InvalidDataException();
                }
            }
            set
            {
                Type = ValueType.Float3;
                _float3 = value;
            }
        }

        public Vector4 Float4
        {
            get
            {
                switch (Type)
                {
                    case ValueType.Int:
                        return new Vector4(_int, 0, 0, 0);
                    case ValueType.Float:
                        return new Vector4(_float, 0, 0, 0);
                    case ValueType.Float2:
                        return new Vector4(_float2.x, _float2.y, 0, 0);
                    case ValueType.Float3:
                        return new Vector4(_float3.x, _float3.y, _float3.z, 0);
                    case ValueType.Float4:
                        return _float4;
                    default: throw new InvalidDataException();
                }
            }
            set
            {
                Type = ValueType.Float4;
                _float4 = value;
            }
        }

        public Quaternion Quaternion
        {
            get
            {
                VSAssert.AreEqual(Type, ValueType.Quaternion);
                return _quaternion;
            }
            set
            {
                Type = ValueType.Quaternion;
                _quaternion = value;
            }
        }

        public Color Color
        {
            get
            {
                VSAssert.AreEqual(Type, ValueType.Quaternion);
                return _color;
            }
            set
            {
                Type = ValueType.Color;
                _color = value;
            }
        }

        // Cannot store a GO directly in the union, as the custom GC will find the field and try to follow the PPtr,
        // even if the value is a bool or an int. that causes a hard crash.
        public T Object<T>() where T : class
        {
            return Box(typeof(T)) as T;
        }

        public object Object(Type t)
        {
            VSAssert.AreEqual(Type, ValueType.ManagedObject);
            var target = GetHandleTarget(_objectHandle);
            if (target?.GetType() == t)
                return target;
            switch (target)
            {
                // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
                case GameObject go when typeof(Component).IsAssignableFrom(t):
                    return go.GetComponent(t);
                case Component c when typeof(Component).IsAssignableFrom(t):
                    return c.gameObject.GetComponent(t);
                case Component c when t == typeof(GameObject):
                    return c.gameObject;
                case IEnumerable enumerable when typeof(IEnumerable).IsAssignableFrom(t) && t.GenericTypeArguments.Any():
                    {
                        var dataType = t.GenericTypeArguments[0];
                        var targetType = enumerable.GetType().GenericTypeArguments[0];

                        if (dataType == targetType)
                            return target;

                        var list = Activator.CreateInstance(typeof(List<>).MakeGenericType(dataType)) as IList;
                        foreach (var targetData in enumerable)
                        {
                            if (targetData.GetType() == dataType)
                            {
                                list?.Add(targetData);
                                continue;
                            }

                            if (targetData is Value value)
                            {
                                var boxed = value.Box();
                                if (boxed.GetType() == dataType)
                                {
                                    list?.Add(boxed);
                                }
                            }
                        }

                        return list;
                    }

                default:
                    return target;
            }
        }

        public Value SetObject(object value)
        {
            Type = value.GetType().IsValueType ? ValueType.Struct : ValueType.ManagedObject;
            _objectHandle = GCHandle.Alloc(value, GCHandleType.Normal);
            return this;
        }

        public object Box()
        {
            switch (Type)
            {
                case ValueType.Bool:
                    return _bool;
                case ValueType.Int:
                    return _int;
                case ValueType.Float:
                    return _float;
                case ValueType.Float2:
                    return _float2;
                case ValueType.Float3:
                    return _float3;
                case ValueType.Float4:
                    return _float4;
                case ValueType.Quaternion:
                    return _quaternion;
                case ValueType.Struct:
                case ValueType.ManagedObject:
                    return GetHandleTarget(_objectHandle);
                case ValueType.Enum:
                    return GetBoxedEnum();
                case ValueType.Color:
                    return _color;
                case ValueType.Unknown:
                    return UndefinedValue.UndefinedValue;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public object Box(Type type)
        {
            return Type == ValueType.ManagedObject || Type == ValueType.Struct ? Object(type) : Box();
        }

        static object GetHandleTarget(GCHandle handle)
        {
            return handle.IsAllocated ? handle.Target : null;
        }

        public static Value FromObject(object o)
        {
            switch (o)
            {
                case Value v: return v; // can happen in situations where boxing is unavoidable (lists mainly)
                case bool b: return b;
                case int i: return i;
                case float f: return f;
                case Vector2 f: return f;
                case Vector3 f: return f;
                case Vector4 f: return f;
                case Quaternion q: return q;
                case null: return new Value { Type = ValueType.ManagedObject };
                default: return new Value().SetObject(o);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(Value a, Value b)
        {
            return (a.Type == ValueType.Int || a.Type == ValueType.Float) &&
                (b.Type == ValueType.Int || b.Type == ValueType.Float) && a.Float < b.Float;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(Value a, Value b)
        {
            return (a.Type == ValueType.Int || a.Type == ValueType.Float) &&
                (b.Type == ValueType.Int || b.Type == ValueType.Float) && a.Float > b.Float;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(Value a, Value b)
        {
            return (a.Type == ValueType.Int || a.Type == ValueType.Float) &&
                (b.Type == ValueType.Int || b.Type == ValueType.Float) && a.Float <= b.Float;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(Value a, Value b)
        {
            return (a.Type == ValueType.Int || a.Type == ValueType.Float) &&
                (b.Type == ValueType.Int || b.Type == ValueType.Float) && a.Float >= b.Float;
        }

        public static implicit operator Value(bool f)
        {
            return new Value { Bool = f };
        }

        public static implicit operator Value(int f)
        {
            return new Value { Int = f };
        }

        public static implicit operator Value(float f)
        {
            return new Value { Float = f };
        }

        public static implicit operator Value(Vector2 f)
        {
            return new Value { Float2 = f };
        }

        public static implicit operator Value(Vector3 f)
        {
            return new Value { Float3 = f };
        }

        public static implicit operator Value(Vector4 f)
        {
            return new Value { Float4 = f };
        }

        public static implicit operator Value(Quaternion f)
        {
            return new Value { Quaternion = f };
        }

        public static implicit operator Value(Color f)
        {
            return new Value { Color = f };
        }

        public override string ToString()
        {
            switch (Type)
            {
                case ValueType.Unknown:
                    return ValueType.Unknown.ToString();
                case ValueType.Bool:
                    return Bool.ToString(CultureInfo.InvariantCulture);
                case ValueType.Int:
                    return Int.ToString(CultureInfo.InvariantCulture);
                case ValueType.Float:
                    return Float.ToString(CultureInfo.InvariantCulture);
                case ValueType.Float2:
                    return Float2.ToString();
                case ValueType.Float3:
                    return Float3.ToString();
                case ValueType.Float4:
                    return Float4.ToString();
                case ValueType.Quaternion:
                    return Quaternion.ToString();
                case ValueType.Struct:
                case ValueType.ManagedObject:
                    return Object<object>()?.ToString();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public string ToPrettyString()
        {
            switch (Type)
            {
                case ValueType.Unknown:
                    return ValueType.Unknown.ToString();
                case ValueType.Bool:
                    return Bool.ToString(CultureInfo.InvariantCulture);
                case ValueType.Int:
                    return Int.ToString(CultureInfo.InvariantCulture);
                case ValueType.Float:
                    return Float.ToString("F2");
                case ValueType.Float2:
                    return Float2.ToString("F2", CultureInfo.InvariantCulture);
                case ValueType.Float3:
                    return Float3.ToString("F2", CultureInfo.InvariantCulture);
                case ValueType.Float4:
                    return Float4.ToString("F2", CultureInfo.InvariantCulture);
                case ValueType.Quaternion:
                    return Quaternion.ToString("F2", CultureInfo.InvariantCulture);
                case ValueType.Struct:
                case ValueType.ManagedObject:
                    return Object<object>()?.ToString();
                case ValueType.Enum:
                    return GetBoxedEnum().ToString();
                case ValueType.Color:
                    return Color.ToString();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static bool Equals(Value a, Value b) => a.Equals(b);

        public bool Equals(Value other)
        {
            if (Type != other.Type)
            {
                if (Type == ValueType.Float && other.Type == ValueType.Int ||
                    Type == ValueType.Int && other.Type == ValueType.Float)
                    return Mathf.Approximately(Float, other.Float);
            }

            switch (Type)
            {
                case ValueType.Unknown:
                    return false;
                case ValueType.Bool:
                    return Bool == other.Bool;
                case ValueType.Int:
                    return Int == other.Int;
                case ValueType.Float:
                    return Mathf.Approximately(Float, other.Float);
                case ValueType.Float2:
                    return Float2.Equals(other.Float2);
                case ValueType.Float3:
                    return Float3.Equals(other.Float3);
                case ValueType.Float4:
                    return Float4.Equals(other.Float4);
                case ValueType.Quaternion:
                    return Quaternion.Equals(other.Quaternion);
                case ValueType.Struct:
                    return Object<object>().Equals(other.Object<object>());
                case ValueType.ManagedObject:
                    var left = Object<object>();
                    var right = other.Object<object>();
                    return left == null ? right == null : left.Equals(right);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override bool Equals(object obj)
        {
            return obj is Value other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)Type;
        }

        // public static unsafe Value FromPtr(void* voidPtr, ValueType type)
        // {
        //     VSAssert.IsFalse(type == ValueType.StringReference, "String not handled");
        //
        //     switch (type)
        //     {
        //         case ValueType.Bool:
        //             return new Value { Bool = *(bool*)voidPtr };
        //         case ValueType.Int:
        //             return new Value { Int = *(int*)voidPtr };
        //         case ValueType.Float:
        //             return new Value { Float = *(float*)voidPtr };
        //         case ValueType.Float2:
        //             return new Value { Float2 = *(float2*)voidPtr };
        //         case ValueType.Float3:
        //             return new Value { Float3 = *(float3*)voidPtr };
        //         case ValueType.Float4:
        //             return new Value { Float4 = *(float4*)voidPtr };
        //         case ValueType.Quaternion:
        //             return new Value { Quaternion = *(quaternion*)voidPtr };
        //         case ValueType.Entity:
        //             return new Value { Entity = *(Entity*)voidPtr };
        //     }
        //     return new Value();
        // }
        //
        // public static unsafe void SetPtrToValue(void* voidPtr, ValueType type, Value setValue)
        // {
        //     VSAssert.IsFalse(type == ValueType.StringReference, "String not handled");
        //
        //     if (type == setValue.Type)
        //     {
        //         switch (type)
        //         {
        //             case ValueType.Bool:
        //                 *(bool*)voidPtr = setValue.Bool;
        //                 break;
        //             case ValueType.Int:
        //                 *(int*)voidPtr = setValue.Int;
        //                 break;
        //             case ValueType.Float:
        //                 *(float*)voidPtr = setValue.Float;
        //                 break;
        //             case ValueType.Float2:
        //                 *(float2*)voidPtr = setValue.Float2;
        //                 break;
        //             case ValueType.Float3:
        //                 *(float3*)voidPtr = setValue.Float3;
        //                 break;
        //             case ValueType.Float4:
        //                 *(float4*)voidPtr = setValue.Float4;
        //                 break;
        //             case ValueType.Quaternion:
        //                 *(quaternion*)voidPtr = setValue.Quaternion;
        //                 break;
        //             case ValueType.Entity:
        //                 *(Entity*)voidPtr = setValue.Entity;
        //                 break;
        //             default:
        //                 throw new ArgumentOutOfRangeException(nameof(type), type, null);
        //         }
        //     }
        // }

        internal static Value CoerceValueToType(ValueType coerceToType, Value val)
        {
            switch (coerceToType)
            {
                case ValueType.Unknown:
                    return val;
                case ValueType.Bool:
                    return val.Bool;
                case ValueType.Int:
                    return val.Int;
                case ValueType.Float:
                    return val.Float;
                case ValueType.Float2:
                    return val.Float2;
                case ValueType.Float3:
                    return val.Float3;
                case ValueType.Float4:
                    return val.Float4;
                case ValueType.Quaternion:
                    return val.Quaternion;
                case ValueType.Struct:
                case ValueType.ManagedObject:
                    return new Value().SetObject(val.Object<object>());
                case ValueType.Enum:
                    if (val.Type == ValueType.Enum) return val;
                    throw new ArgumentOutOfRangeException(nameof(coerceToType), coerceToType, null);
                default:
                    throw new ArgumentOutOfRangeException(nameof(coerceToType), coerceToType, null);
            }
        }
    }
}
