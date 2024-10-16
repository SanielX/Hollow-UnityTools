using System;
using System.Runtime.CompilerServices;
using UnityEngine;

#nullable enable

namespace Hollow
{
    /// <summary>
    /// Provides a way to edit UniqueTag in Inspector
    /// </summary>
    [Serializable]
    public struct UniqueTagProperty : ISerializationCallbackReceiver
    {
        private UniqueTag tag;
        [SerializeField] string m_Text;

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            tag = m_Text; // Shouldn't happen on the main thread btw
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        public override int GetHashCode() => tag.GetHashCode();
        public override bool Equals(object obj)   => (obj is UniqueTagProperty && Equals((UniqueTagProperty)obj)) || (obj is UniqueTag tag && this.tag.Equals(tag));
        public bool Equals(UniqueTagProperty obj) => obj.tag.Equals(tag);
        public bool Equals(UniqueTag obj)         => tag.Equals(obj);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UniqueTag(in UniqueTagProperty prop) => prop.tag;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UniqueTagProperty(in string prop) => new()
        {
            tag = prop, m_Text = prop
        };
    }
}