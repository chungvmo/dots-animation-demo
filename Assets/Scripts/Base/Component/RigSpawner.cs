using Unity.Entities;

namespace Base.Component
{
    public struct RigSpawner : IComponentData
    {
        public Entity RigPrefab;
        public int CountX;
        public int CountY;
    }
}