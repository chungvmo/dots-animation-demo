using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Base.MonoBehaviour
{
    public abstract class AnimationGraphBase : UnityEngine.MonoBehaviour
    {
        public abstract void AddGraphSetupComponent(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem);

        public virtual void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs) { }

        public virtual void PreProcessData<T>(T data) { }
    }
}