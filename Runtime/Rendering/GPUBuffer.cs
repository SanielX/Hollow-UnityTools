using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace Hollow.Rendering
{
    /// <summary>
    /// Wrapper around raw <see cref="GraphicsBuffer"/> that is more type safe and can automatically resize underlying buffer if required
    /// </summary>
    [System.Obsolete("Better off just using GraphicsBuffer")]
    public class GPUBuffer : IDisposable
    {
        private GPUBuffer() 
        {
            globalBindings = new(16);
        }

        HashSet<int> globalBindings;

        public GraphicsBuffer.Target target { get; private set; }
        public int count                    { get; private set; }
        public int elementSize              { get; private set; }

        public GraphicsBuffer gpuBuffer; // can't pass it to funtions unless make public, whatever

        public static unsafe GPUBuffer Create<T>(GraphicsBuffer.Target target, int elementsCount, string name = null) where T : unmanaged
        {
            GPUBuffer res   = new GPUBuffer();
            res.target               =  target;
            res.gpuBuffer            =  new GraphicsBuffer(target, elementsCount, sizeof(T));
            
            res.count                =  elementsCount;
            res.elementSize          =  sizeof(T);
            
            if(name is not null)
                res.SetDebugName(name);

            return res;
        }
        
        public int size => count * elementSize;

#if UNITY_ASSERTIONS
        string     name;

        /// <summary>
        /// Sets name to debug buffer that can be viewed in RenderDoc, calls to this method are ignored in release build
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_ASSERTIONS")]
        public void SetDebugName(string name)
        {
            gpuBuffer.name = name;
            this.name = name;
        }
#else 
        [System.Diagnostics.Conditional("UNITY_ASSERTIONS")]
        public void SetDebugName(string name) { }
#endif
        
        /// <summary>
        /// Note that resizing GPUBuffer may remove it from global bindings
        /// </summary>
        public void Resize(int count)
        {
            Assert.IsTrue(count > 0, "Resize count must be feater than 0");
            
            gpuBuffer.Dispose();
            gpuBuffer = new GraphicsBuffer(target, count, elementSize);
            this.count = count;
            
#if UNITY_ASSERTIONS
            gpuBuffer.name = name;
#endif 
        }
        
        /// <summary>
        /// Updates GPU buffer with new data. It will also automatically resize buffer if needed. Note that you should rebind the buffer after updating it as resizing will create new underlaying GraphicsBuffer
        /// </summary>
        [System.Obsolete("Use version of this method that does not have an allocator parameter")]
        public unsafe void UploadDataToGPU<T>(UnsafeList<T> data, Allocator allocator) where T : unmanaged
        {
            var na = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(data.Ptr, data.Length, data.Allocator.ToAllocator);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref na, AtomicSafetyHandle.Create());
#endif 
            UploadDataToGPU(na);
        }
        
        public void UploadDataToGPU<T>(T data, int gpuBufferUploadStartIndex = 0) where T : unmanaged
        {
            if(count == 0)
                Resize(4);
            
            NativeArray<T> a = new(1, Allocator.Temp);
            a[0] = data;
            
            gpuBuffer.SetData(a, 0, gpuBufferUploadStartIndex, 1);
        }

        public void UploadDataToGPU<T>(NativeArray<T> data) where T : unmanaged
        {
            if (data.Length > count)
                Resize(data.Length + 256);

            gpuBuffer.SetData(data);
        }
        
        public void UploadDataToGPU<T>(T[] data) where T : unmanaged
        {
            if (data.Length > count)
                Resize(data.Length + 256);

            gpuBuffer.SetData(data);
        }

        public void SetGlobalBuffer(string id) => SetGlobalBuffer(Shader.PropertyToID(id));

        public void SetGlobalBuffer(int id)
        {
            Shader.SetGlobalBuffer(id, gpuBuffer);
            globalBindings.Add(id);
        }
        
        public void SetGlobalConstantBuffer(string id) => SetGlobalConstantBuffer(Shader.PropertyToID(id));

        public void SetGlobalConstantBuffer(int id)
        {
            Shader.SetGlobalConstantBuffer(id, gpuBuffer, 0, count * elementSize);
        }

        public void Bind(CommandBuffer cmd, string id)
        {
            Bind(cmd, Shader.PropertyToID(id));
        }

        public void Bind(CommandBuffer cmd, int id)
        {
            cmd.SetGlobalBuffer(id, gpuBuffer);
        }
        
        public void BindConstant(CommandBuffer cmd, int id)
        {
            cmd.SetGlobalConstantBuffer(gpuBuffer, id, 0, size);
        }

        public void Dispose()
        {
            gpuBuffer.Dispose();

            // Most of GPUBuffer class follows similar one from SRP library
            // where they say that global bindings might need to be cleared manually on some platforms
            foreach (var id in globalBindings)
                Shader.SetGlobalBuffer(id, (GraphicsBuffer)null);
        }

        public static implicit operator GraphicsBuffer(GPUBuffer buf) => buf?.gpuBuffer;

        public void Release()
        {
            Dispose();
        }
    }
}