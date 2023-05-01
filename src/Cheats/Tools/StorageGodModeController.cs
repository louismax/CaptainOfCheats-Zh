using System;
using CaptainOfCheats.Constants;
using CaptainOfCheats.Extensions;
using CaptainOfCheats.ReimplementedBaseClasses;
using Mafi;
using Mafi.Collections;
using Mafi.Collections.ImmutableCollections;
using Mafi.Collections.ReadonlyCollections;
using Mafi.Core.Buildings.Storages;
using Mafi.Core.Entities;
using Mafi.Core.Entities.Static;
using Mafi.Core.Factory.Transports;
using Mafi.Core.GameLoop;
using Mafi.Core.Gfx;
using Mafi.Core.Input;
using Mafi.Localization;
using Mafi.Unity;
using Mafi.Unity.Entities;
using Mafi.Unity.InputControl;
using Mafi.Unity.InputControl.AreaTool;
using Mafi.Unity.InputControl.Cursors;
using Mafi.Unity.InputControl.Factory;
using Mafi.Unity.InputControl.Toolbar;
using Mafi.Unity.UiFramework.Styles;
using Mafi.Unity.UserInterface;
using UnityEngine;

namespace CaptainOfCheats.Cheats.Tools
{
    [GlobalDependency(RegistrationMode.AsAllInterfaces)]
    public class StorageGodModeController : BaseEntityCursorInputController<IStaticEntity>
    {
        private readonly EntitiesIconRenderer _iconRenderer;
        private readonly ToolbarController _toolbarController;
        private readonly IEntitiesManager _entitiesManager;
        private CursorStyle _cursorStyle;

        public StorageGodModeController(ToolbarController toolbarController, ShortcutsManager shortcutsManager, IUnityInputMgr inputManager, CursorPickingManager cursorPickingManager,
            CursorManager cursorManager,
            AreaSelectionToolFactory areaSelectionToolFactory,
            IEntitiesManager entitiesManager,
            NewInstanceOf<EntityHighlighter> highlighter,
            EntitiesIconRenderer iconRenderer,
            IGameLoopEvents gameLoopEvents)
            : base(shortcutsManager, inputManager, cursorPickingManager, cursorManager, areaSelectionToolFactory, entitiesManager, highlighter, (Option<NewInstanceOf<TransportTrajectoryHighlighter>>) Option.None)
        {
            _toolbarController = toolbarController;
            _entitiesManager = entitiesManager;
            _iconRenderer = iconRenderer;
            
            gameLoopEvents.RegisterRendererInitState(this, InitState);
        }

        private void InitState()
        {
            foreach (var entity in _entitiesManager.Entities)
            {
                if (entity is Storage storage)
                {
                  SetGodModeIconOnStorage(storage);
                }
                    
            }
        }

        public override void RegisterUi(UiBuilder builder)
        {
            _cursorStyle = new CursorStyle("StorageGodModeControllerStyle", "Assets/Unity/UserInterface/EntityIcons/Storage.svg", new Vector2(14f, 14f));
            InitializeUi(builder, _cursorStyle, builder.Audio.Assign, ColorRgba.White, ColorRgba.Green);

            _toolbarController
                .AddLeftMenuButton("上帝存储模式", this, "Assets/Unity/UserInterface/EntityIcons/Storage.svg", 70f, manager => KeyBindings.EMPTY)
                .AddTooltip(new LocStrFormatted("[Captain of Cheats(中文)] 在仓库建筑上启用上帝模式。 " +
                                                "当仓库启用了上帝模式时，将绿色滑块向右拖动以从该存储中获取无限产品。 " +
                                                "将红色滑块向左拖动以销毁仓库中的产品（可销毁通过任何运输方式进入该仓库的产品）。"));
        }

        protected override bool OnFirstActivated(IStaticEntity hoveredEntity, Lyst<IStaticEntity> selectedEntities, Lyst<SubTransport> selectedPartialTransports)
        {
            return false;
        }

        protected override void OnEntitiesSelected(IIndexable<IStaticEntity> selectedEntities, IIndexable<SubTransport> selectedPartialTransports, bool isAreaSelection, bool isLeftMouse)
        {
            if (selectedEntities.Count == 0) return;

            foreach (var storage in selectedEntities.Select(s => (Storage)s))
            {
                storage.SetGodMode(!storage.IsGodModeEnabled());
                SetGodModeIconOnStorage(storage);
            }
        }

        private void SetGodModeIconOnStorage(Storage storage)
        {
            if (storage.IsGodModeEnabled())
            {
                _iconRenderer.AddIcon(new IconSpec(IconsPaths.ToolbarCaptainWheel, ColorRgba.Cyan), storage);
            }
            else
            {
                _iconRenderer.RemoveIcon(new IconSpec(IconsPaths.ToolbarCaptainWheel, ColorRgba.Cyan), storage);
            }
        }
        
        protected override bool Matches(IStaticEntity entity, bool isAreaSelection, bool isLeftClick)
        {
            if (entity.IsDestroyed)
                return false;
            if (entity is IStaticEntity staticEntity && !staticEntity.IsConstructed)
                return false;

            if (entity is Transport) return false;

            if (entity is Storage) return true;

            return false;
        }
    }
}