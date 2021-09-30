using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace Unity.VisualScripting.Interpreter
{
    internal static class HashUtility
    {
        public static uint HashCollection<T>(IList<T> coll, Func<T, uint, uint> hashFunction, uint hash)
        {
            for (var index = 0; index < coll.Count; index++)
                hash = hashFunction(coll[index], hash);

            return hash;
        }

        public static unsafe uint HashUnmanagedStruct<T>(T obj, uint seed) where T : unmanaged
        {
            void* ptr = &obj;
            seed = hash((byte*)ptr, UnsafeUtility.SizeOf(obj.GetType()), seed);
            return seed;
        }

        /// <summary>
        /// Hash a struct that has been boxed. Given a struct Node : INode, this allows to hash a value of static type INode and actual type Node. The actual type of obj must be unmanaged
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="seed"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static unsafe uint HashBoxedUnmanagedStruct<T>(T obj, uint seed)
        {
            const int managedObjectHeaderSize = 16;
            Assert.IsTrue(UnsafeUtility.IsUnmanaged(obj.GetType()), $"Type {obj.GetType().Name} is managed");

            var ptr = UnsafeUtility.PinGCObjectAndGetAddress(obj, out var handle);
            seed = hash((byte*)ptr + managedObjectHeaderSize, UnsafeUtility.SizeOf(obj.GetType()), seed);
            UnsafeUtility.ReleaseGCObject(handle);
            return seed;
        }

        /// <summary>
        /// Hash anything else. Relies on object.GetHashCode()
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="seed"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static uint HashManaged<T>(T obj, uint seed)
        {
            return (uint)((obj?.GetHashCode() ?? 0) ^ (seed * 11));
        }

        // copied from unity.maths
        public struct uint4 //: System.IEquatable<uint4>, IFormattable
        {
            public uint x;
            public uint y;
            public uint z;
            public uint w;

            /// <summary>uint4 zero value.</summary>
            public static readonly uint4 zero;

            /// <summary>Constructs a uint4 vector from four uint values.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public uint4(uint x, uint y, uint z, uint w)
            {
                this.x = x;
                this.y = y;
                this.z = z;
                this.w = w;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static uint4 operator +(uint4 lhs, uint rhs) { return new uint4(lhs.x + rhs, lhs.y + rhs, lhs.z + rhs, lhs.w + rhs); }

            /// <summary>Returns the result of a componentwise addition operation on two uint4 vectors.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static uint4 operator +(uint4 lhs, uint4 rhs) { return new uint4(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z, lhs.w + rhs.w); }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static uint4 operator *(uint4 lhs, uint rhs) { return new uint4(lhs.x * rhs, lhs.y * rhs, lhs.z * rhs, lhs.w * rhs); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static uint4 operator *(uint4 lhs, uint4 rhs) { return new uint4(lhs.x * rhs.x, lhs.y * rhs.y, lhs.z * rhs.z, lhs.w * rhs.w); }
            /// <summary>Returns the result of a componentwise left shift operation on a uint4 vector by a number of bits specified by a single int.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static uint4 operator <<(uint4 x, int n) { return new uint4(x.x << n, x.y << n, x.z << n, x.w << n); }

            /// <summary>Returns the result of a componentwise right shift operation on a uint4 vector by a number of bits specified by a single int.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static uint4 operator >>(uint4 x, int n) { return new uint4(x.x >> n, x.y >> n, x.z >> n, x.w >> n); }
            /// <summary>Returns the result of a componentwise bitwise or operation on two uint4 vectors.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static uint4 operator |(uint4 lhs, uint4 rhs) { return new uint4(lhs.x | rhs.x, lhs.y | rhs.y, lhs.z | rhs.z, lhs.w | rhs.w); }
        }

        /// <summary>copied from unity.maths. Returns a uint hash from a block of memory using the xxhash32 algorithm. Can only be used in an unsafe context.</summary>
        /// <param name="pBuffer">A pointer to the beginning of the data.</param>
        /// <param name="numBytes">Number of bytes to hash.</param>
        /// <param name="seed">Starting seed value.</param>
        public static unsafe uint hash(void* pBuffer, int numBytes, uint seed = 0)
        {
            unchecked
            {
                const uint Prime1 = 2654435761;
                const uint Prime2 = 2246822519;
                const uint Prime3 = 3266489917;
                const uint Prime4 = 668265263;
                const uint Prime5 = 374761393;

                uint4* p = (uint4*)pBuffer;
                uint hash = seed + Prime5;
                if (numBytes >= 16)
                {
                    uint4 state = new uint4(Prime1 + Prime2, Prime2, 0, (uint)-Prime1) + seed;

                    int count = numBytes >> 4;
                    for (int i = 0; i < count; ++i)
                    {
                        state += *p++ * Prime2;
                        state = (state << 13) | (state >> 19);
                        state *= Prime1;
                    }

                    hash = rol(state.x, 1) + rol(state.y, 7) + rol(state.z, 12) + rol(state.w, 18);
                }

                hash += (uint)numBytes;

                uint* puint = (uint*)p;
                for (int i = 0; i < ((numBytes >> 2) & 3); ++i)
                {
                    hash += *puint++ * Prime3;
                    hash = rol(hash, 17) * Prime4;
                }

                byte* pbyte = (byte*)puint;
                for (int i = 0; i < ((numBytes) & 3); ++i)
                {
                    hash += (*pbyte++) * Prime5;
                    hash = rol(hash, 11) * Prime1;
                }

                hash ^= hash >> 15;
                hash *= Prime2;
                hash ^= hash >> 13;
                hash *= Prime3;
                hash ^= hash >> 16;

                return hash;
            }
        }

        /// <summary>Returns the result of rotating the bits of a uint left by bits n.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint rol(uint x, int n) { return (x << n) | (x >> (32 - n)); }
    }
}
