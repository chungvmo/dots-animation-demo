using Base.Component;
using Unity.Animation;
using Unity.Collections;
using Unity.DataFlowGraph;
using Unity.Entities;

namespace UnityTemplateProjects.Base.System
{
    public abstract class
        BaseSystem<TISetupComponent, TIDataComponent, TAnimationGraphTag, TAnimationGraph> : ComponentSystem
        where TISetupComponent : struct, ISetupComponent
        where TIDataComponent : struct, IDataComponent
        where TAnimationGraphTag : struct, IAnimationSystemTag
        where TAnimationGraph : SystemBase, IAnimationSystem<TAnimationGraphTag>
    {
        protected TAnimationGraph _graphSystem;

        private EntityQueryBuilder.F_EDD<Rig, TISetupComponent> _createLambda;
        private EntityQueryBuilder.F_ED<TIDataComponent> _destroyLambda;

        protected override void OnCreate()
        {
            base.OnCreate();
            _graphSystem = World.GetOrCreateSystem<TAnimationGraph>();
            _graphSystem.AddRef();
            _graphSystem.Set.RendererModel = NodeSet.RenderExecutionModel.Islands;

            _createLambda = (Entity e, ref Rig rig, ref TISetupComponent setupComponent) =>
            {
                var data = CreateGraph(e, ref rig, _graphSystem, ref setupComponent);
                PostUpdateCommands.AddComponent(e, data);
            };

            _destroyLambda = (Entity e, ref TIDataComponent dataComponent) =>
            {
                DestroyGraph(e, _graphSystem, ref dataComponent);
                PostUpdateCommands.RemoveComponent<TIDataComponent>(e);
            };
        }

        protected override void OnUpdate()
        {
            Entities.WithNone<TIDataComponent>().ForEach(_createLambda);
            Entities.WithNone<TISetupComponent>().ForEach(_destroyLambda);
        }

        protected override void OnDestroy()
        {
            if (_graphSystem == null)
            {
                return;
            }

            var entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            Entities.ForEach((Entity e, ref TIDataComponent dataComponent) =>
            {
                DestroyGraph(e, _graphSystem, ref dataComponent);
                entityCommandBuffer.RemoveComponent<TIDataComponent>(e);
            });

            entityCommandBuffer.Playback(EntityManager);
            entityCommandBuffer.Dispose();
            _graphSystem.RemoveRef();
            base.OnDestroy();
        }

        protected abstract TIDataComponent CreateGraph(Entity e, ref Rig rig, TAnimationGraph animationGraph,
            ref TISetupComponent setupComponent);

        protected abstract void DestroyGraph(Entity e, TAnimationGraph animationGraph,
            ref TIDataComponent dataComponent);
    }
}