using System;
using Unity.Mathematics;
using UnityEngine;

namespace Hollow
{
    public interface IStaticQuadTree<T>
    {
        public int LevelCount { get; }
    
        public int NodesCount { get; }
        
        public ref T this[int index] { get; }
        
        public bool HasChildren      (int nodeIndex);
        public int CountNodesAtLevel (int level);
        public int ChildrenStartIndex(int nodeIndex);
        public int ParentIndex       (int nodeIndex);
        // also mono is bad at math optimization so use shifts instead
        public int FirstNodeAtLevel  (int level);
        public int GetNodeLevel      (int nodeIndex);
        public int IteratorAtLevelStart(int level);
        public int IteratorAtLevelEnd  (int level);
    }

    [System.Serializable]
    public class StaticQuadTree<T> : IStaticQuadTree<T>
    {
        internal StaticQuadTree()
        {
        }

        public StaticQuadTree(int levelCount = 1)
        {
#if UNITY_ASSERTIONS
            if (levelCount < 1)
                throw new ArgumentOutOfRangeException(nameof(levelCount), "Deepness of a quad tree can not be less than 1");
#endif 

            levelNodesIndices = new int[levelCount+1];

            int totalNodesCount = 0;
            
            for (int i = 0; i < levelCount; i++)
            {
                levelNodesIndices[i] = totalNodesCount;
                totalNodesCount += (int)Math.Pow(4, i);
            }

            // Could use closed form but need lookup table :(
            // totalNodesCount = (int)((1 - Math.Pow(4, levelCount)) / (1 - 4));

            levelNodesIndices[^1] = totalNodesCount;

            nodes = new T[totalNodesCount];
        }
    
        [UnityEngine.SerializeField] public T[]      nodes;
        [UnityEngine.SerializeField] public int[]    levelNodesIndices;

        // [HostGame.SafeProperty]
        public int LevelCount => levelNodesIndices.Length - 1;
        
        //[HostGame.SafeProperty]
        public int NodesCount => nodes.Length;
        
        public T[] Data => nodes;

        public ref T this[int index]
        {
            get
            {
#if UNITY_ASSERTIONS
                if (nodes is null)
                    throw new System.InvalidOperationException("Tree is in invalid state (Nodes array is null)");

                if (index < 0 || index >= nodes.Length)
                    throw new System.IndexOutOfRangeException($"Index '{index}' is out of bounds (Length: '{nodes.Length}')");
#endif 

                return ref nodes[index];
            }
        }
        
        public bool HasChildren      (int nodeIndex) => ChildrenStartIndex(nodeIndex) < NodesCount;
        public int CountNodesAtLevel (int level)     => levelNodesIndices[level+1] - levelNodesIndices[level];
        public int ChildrenStartIndex(int nodeIndex) => (nodeIndex << 2) + 1;
        
        // Without Max(0, nodeIndex) it can go very wrong for node at 0 index
        public int ParentIndex       (int nodeIndex) => Mathf.Max(0, (nodeIndex - 1)) >> 2; // integer division always floors so it will work for any child,
                                                                              // also mono is bad at math optimization so use shifts instead
        public int FirstNodeAtLevel  (int level)     => levelNodesIndices[level];
        public int GetNodeLevel      (int nodeIndex)
        {
            return math.floorlog2(nodeIndex*3 + 1) >> 1;
        }


        public int IteratorAtLevelStart(int level) => FirstNodeAtLevel(level);
        public int IteratorAtLevelEnd  (int level) => levelNodesIndices[level+1];
    }
}
