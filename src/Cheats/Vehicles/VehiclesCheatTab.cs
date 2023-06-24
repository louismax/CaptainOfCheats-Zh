﻿using System;
using System.Collections.Generic;
using CaptainOfCheats.Extensions;
using Mafi;
using Mafi.Collections;
using Mafi.Core;
using Mafi.Core.Syncers;
using Mafi.Localization;
using Mafi.Unity;
using Mafi.Unity.InputControl;
using Mafi.Unity.UiFramework;
using Mafi.Unity.UiFramework.Components;
using Mafi.Unity.UiFramework.Components.Tabs;
using Mafi.Unity.UserInterface.Components;
using UnityEngine;

namespace CaptainOfCheats.Cheats.Vehicles
{
    [GlobalDependency(RegistrationMode.AsEverything)]
    public class VehiclesCheatTab : Tab, ICheatProviderTab
    {
        private readonly VehiclesCheatProvider _vehiclesCheatProvider;
        private readonly Dict<SwitchBtn, Func<bool>> _switchBtns = new Dict<SwitchBtn, Func<bool>>();

        public VehiclesCheatTab(NewInstanceOf<VehiclesCheatProvider> vehiclesCheatProvider) : base(nameof(VehiclesCheatTab), SyncFrequency.OncePerSec)
        {
            _vehiclesCheatProvider = vehiclesCheatProvider.Instance;
        }

        private Dictionary<int, Action<int>> VehicleCapIncrementButtonConfig =>
            new Dictionary<int, Action<int>>
            {
                { 5, _vehiclesCheatProvider.ChangeVehicleLimit },
                { 25, _vehiclesCheatProvider.ChangeVehicleLimit },
                { 50, _vehiclesCheatProvider.ChangeVehicleLimit }
            };

        public string Name => "车辆";

        public string IconPath => Assets.Unity.UserInterface.Toolbar.Vehicles_svg;

        
        public override void RenderUpdate(GameTime gameTime)
        {
            RefreshValues();
            base.RenderUpdate(gameTime);
        }

        public override void SyncUpdate(GameTime gameTime)
        {
            RefreshValues();
            base.SyncUpdate(gameTime);
        }

        private void RefreshValues()
        {
            foreach (var kvp in _switchBtns) kvp.Key.SetIsOn(kvp.Value());
        }
        
        protected override void BuildUi()
        {
            var tabContainer = CreateStackContainer();
            
            var firstRowContainer = Builder
                .NewStackContainer("firstRowContainer")
                .SetStackingDirection(StackContainer.Direction.LeftToRight)
                .SetSizeMode(StackContainer.SizeMode.StaticDirectionAligned)
                .SetItemSpacing(10f)
                .AppendTo(tabContainer, offset: Offset.All(0), size: 30);
            
            var fuelToggle = NewToggleSwitch(
                "禁用油耗",
                "将油耗设置为零（选中）或默认（未选中）。",
                toggleVal => _vehiclesCheatProvider.SetVehicleFuelConsumptionToZero(toggleVal),
                () => _vehiclesCheatProvider.IsVehicleFuelConsumptionZero());
            fuelToggle.AppendTo(firstRowContainer, new Vector2(fuelToggle.GetWidth(), 25), ContainerPosition.LeftOrTop);
            
            Builder.AddSectionTitle(tabContainer, new LocStrFormatted("车辆上限"), new LocStrFormatted("使用增量按钮向上或向下调整车辆限制上限。"));
            var vehicleCapIncrementButtonPanel = Builder.NewIncrementButtonGroup(VehicleCapIncrementButtonConfig);
            vehicleCapIncrementButtonPanel.AppendTo(tabContainer, new float?(50f), Offset.All(0));
            
            Builder.AddSectionTitle(tabContainer, new LocStrFormatted("卡车容量乘数"));
            var capacityMultiplierPanel = Builder.NewPanel("capacityMultiplierPanel").SetBackground(Builder.Style.Panel.ItemOverlay);
            capacityMultiplierPanel.AppendTo(tabContainer, size: 50f, Offset.All(0));
            
            var capacityMultiplierBtnContainer = Builder
                .NewStackContainer("capacityMultiplierBtnContainer")
                .SetStackingDirection(StackContainer.Direction.LeftToRight)
                .SetSizeMode(StackContainer.SizeMode.StaticDirectionAligned)
                .SetItemSpacing(10f)
                .PutToLeftOf(capacityMultiplierPanel, 0.0f, Offset.Left(10f));
            
            var plus100CapacityBtn = Builder.NewBtnGeneral("button")
                .SetButtonStyle(Style.Global.PrimaryBtn)
                .SetText(new LocStrFormatted("+100%"))
                .AddToolTip("将卡车的容量增加 100%")
                .OnClick(() => _vehiclesCheatProvider.SetTruckCapacityMultiplier(VehiclesCheatProvider.TruckCapacityMultiplier.OneHundred));
            plus100CapacityBtn.AppendTo(capacityMultiplierBtnContainer, plus100CapacityBtn.GetOptimalSize(), ContainerPosition.MiddleOrCenter);
            
            var plus200CapacityBtn = Builder.NewBtnGeneral("button")
                .SetButtonStyle(Style.Global.PrimaryBtn)
                .SetText(new LocStrFormatted("+200%"))
                .AddToolTip("将卡车的容量增加 200%")
                .OnClick(() => _vehiclesCheatProvider.SetTruckCapacityMultiplier(VehiclesCheatProvider.TruckCapacityMultiplier.TwoHundred));
            plus200CapacityBtn.AppendTo(capacityMultiplierBtnContainer, plus200CapacityBtn.GetOptimalSize(), ContainerPosition.MiddleOrCenter);
            
            var plus500CapacityBtn = Builder.NewBtnGeneral("button")
                .SetButtonStyle(Style.Global.PrimaryBtn)
                .SetText(new LocStrFormatted("+500%"))
                .AddToolTip("将卡车的容量增加 500%")
                .OnClick(() => _vehiclesCheatProvider.SetTruckCapacityMultiplier(VehiclesCheatProvider.TruckCapacityMultiplier.FiveHundred));
            plus500CapacityBtn.AppendTo(capacityMultiplierBtnContainer, plus500CapacityBtn.GetOptimalSize(), ContainerPosition.MiddleOrCenter);
            
            var resetCapacityBtn = Builder.NewBtnGeneral("button")
                .SetButtonStyle(Style.Global.DangerBtn)
                .SetText(new LocStrFormatted("重置"))
                .AddToolTip("重置卡车的容量")
                .OnClick(() => _vehiclesCheatProvider.SetTruckCapacityMultiplier(VehiclesCheatProvider.TruckCapacityMultiplier.Reset));
            resetCapacityBtn.AppendTo(capacityMultiplierBtnContainer, resetCapacityBtn.GetOptimalSize(), ContainerPosition.MiddleOrCenter);
            
        }
        private StackContainer CreateStackContainer()
        {
            var topOf = Builder
                .NewStackContainer("container")
                .SetStackingDirection(StackContainer.Direction.TopToBottom)
                .SetSizeMode(StackContainer.SizeMode.Dynamic)
                .SetInnerPadding(Offset.All(15f))
                .SetItemSpacing(5f)
                .PutToTopOf(this, 0.0f);
            return topOf;
        }
        
        private SwitchBtn NewToggleSwitch(string text, string tooltip, Action<bool> onToggleAction, Func<bool> isToggleEnabled)
        {
            var toggleBtn = Builder.NewSwitchBtn()
                .SetText(text)
                .AddTooltip(new LocStrFormatted(tooltip))
                .SetOnToggleAction(onToggleAction);

            _switchBtns.Add(toggleBtn, isToggleEnabled);

            return toggleBtn;
        }
    }
}