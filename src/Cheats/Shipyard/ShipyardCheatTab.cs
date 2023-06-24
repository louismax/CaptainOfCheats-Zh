using System;
using System.Collections.Generic;
using System.Linq;
using Mafi;
using Mafi.Core.Products;
using Mafi.Core.Prototypes;
using Mafi.Core.Syncers;
using Mafi.Localization;
using Mafi.Unity;
using Mafi.Unity.InputControl;
using Mafi.Unity.UiFramework;
using Mafi.Unity.UiFramework.Components;
using Mafi.Unity.UiFramework.Components.Tabs;
using Mafi.Unity.UserInterface.Components;
using UnityEngine;

namespace CaptainOfCheats.Cheats.Shipyard
{
    [GlobalDependency(RegistrationMode.AsEverything)]
    public class ShipyardCheatTab : Tab, ICheatProviderTab
    {
        private readonly ShipyardCheatProvider _shipyardCheatProvider;
        private readonly IEnumerable<ProductProto> _productProtos;
        private readonly FleetCheatProvider _fleetCheatProvider;
        private readonly ProtosDb _protosDb;
        private float _quantity = 250;
        private ProductProto.ID? _selectedProduct;

        public ShipyardCheatTab(NewInstanceOf<ShipyardCheatProvider> productCheatProvider, NewInstanceOf<FleetCheatProvider> fleetCheatProvider, ProtosDb protosDb) : base(nameof(ShipyardCheatTab),
            SyncFrequency.OncePerSec)
        {
            _shipyardCheatProvider = productCheatProvider.Instance;
            _fleetCheatProvider = fleetCheatProvider.Instance;
            _protosDb = protosDb;
            _productProtos = _protosDb.Filter<ProductProto>(proto => proto.CanBeLoadedOnTruck).OrderBy(x => x);
        }

        public string Name => "船厂";
        public string IconPath => Assets.Unity.UserInterface.Toolbar.CargoShip_svg;

        protected override void BuildUi()
        {
            var tabContainer = CreateStackContainer();
            
            Builder.AddSectionTitle(tabContainer, new LocStrFormatted("船厂产品储存"), new LocStrFormatted("在造船厂仓库中添加的产品。"), Offset.Zero);
            var sectionTitlesContainer = Builder
                .NewStackContainer("shipyardContainer")
                .SetStackingDirection(StackContainer.Direction.LeftToRight)
                .SetSizeMode(StackContainer.SizeMode.StaticDirectionAligned)
                .SetItemSpacing(10f)
                .AppendTo(tabContainer, offset: Offset.All(0), size: 30);

            var quantitySectionTitle = Builder.CreateSectionTitle(new LocStrFormatted("数量"), new LocStrFormatted("设置将受您的添加产品操作影响的产品数量。"));
            quantitySectionTitle.AppendTo(sectionTitlesContainer,  quantitySectionTitle.GetPreferedWidth(), Mafi.Unity.UiFramework.Offset.Left(10));
            
            var productSectionTitle = Builder.CreateSectionTitle(new LocStrFormatted("产品"), new LocStrFormatted("选择要从您的造船厂添加的产品。"));
            productSectionTitle.AppendTo(sectionTitlesContainer, productSectionTitle.GetPreferedWidth(), Offset.Left(245));
            
            var quantityAndProductContainer = Builder
                .NewStackContainer("quantityAndProductContainer")
                .SetStackingDirection(StackContainer.Direction.LeftToRight)
                .SetSizeMode(StackContainer.SizeMode.StaticDirectionAligned)
                .SetItemSpacing(10f)
                .AppendTo(tabContainer, offset: Offset.Left(10), size: 30);
            
            var quantitySlider = BuildQuantitySlider();
            quantitySlider.AppendTo(quantityAndProductContainer, new Vector2(200, 28f), ContainerPosition.LeftOrTop);
            
            var buildProductSelector = BuildProductSelector();
            buildProductSelector.AppendTo(quantityAndProductContainer, new Vector2(200, 28f), ContainerPosition.LeftOrTop, Offset.Left(100));

            var thirdRowContainer = Builder
                .NewStackContainer("secondRowContainer")
                .SetStackingDirection(StackContainer.Direction.LeftToRight)
                .SetSizeMode(StackContainer.SizeMode.StaticDirectionAligned)
                .SetItemSpacing(10f)
                .AppendTo(tabContainer,offset: Offset.Left(10), size: 30);

            var spawnProductBtn = BuildAddProductBtn();
            spawnProductBtn.AppendTo(thirdRowContainer, spawnProductBtn.GetOptimalSize(), ContainerPosition.LeftOrTop, Offset.Top(10f));
            
            Panel horSep = this.Builder.NewPanel("separator").AppendTo<Panel>(tabContainer, new Vector2?(new Vector2(630f, 20f)), ContainerPosition.MiddleOrCenter, Offset.Top(20));
            this.Builder.NewIconContainer("left").SetIcon("Assets/Unity/UserInterface/General/HorizontalGradientToLeft48.png", false).PutToLeftMiddleOf<IconContainer>((IUiElement) horSep, new Vector2(300f, 1f));
            this.Builder.NewIconContainer("symbol").SetIcon("Assets/Unity/UserInterface/General/Tradable128.png").PutToCenterMiddleOf<IconContainer>((IUiElement) horSep, new Vector2(20f, 20f));
            this.Builder.NewIconContainer("right").SetIcon("Assets/Unity/UserInterface/General/HorizontalGradientToRight48.png", false).PutToRightMiddleOf<IconContainer>((IUiElement) horSep, new Vector2(300f, 1f));
            
            Builder.AddSectionTitle(tabContainer, new LocStrFormatted("主舰"));
            var mainShipPanel = Builder.NewPanel("mainShipPanel").SetBackground(Builder.Style.Panel.ItemOverlay);
            mainShipPanel.AppendTo(tabContainer, size: 50f, Offset.All(0));

            var mainShipBtnContainer = Builder
                .NewStackContainer("mainShipBtnContainer")
                .SetStackingDirection(StackContainer.Direction.LeftToRight)
                .SetSizeMode(StackContainer.SizeMode.StaticDirectionAligned)
                .SetItemSpacing(10f)
                .PutToLeftOf(mainShipPanel, 0.0f, Offset.Left(10f));
            
            var forceUnloadShipBtn = BuildForceUnloadShipyardShipButton();
            forceUnloadShipBtn.AppendTo(mainShipBtnContainer, forceUnloadShipBtn.GetOptimalSize(), ContainerPosition.MiddleOrCenter);
            
            var finishExplorationBtn = BuildFinishExplorationButton();
            finishExplorationBtn.AppendTo(mainShipBtnContainer, finishExplorationBtn.GetOptimalSize(), ContainerPosition.MiddleOrCenter);
            
            var repairShipBtn = BuildRepairFleetButton();
            repairShipBtn.AppendTo(mainShipBtnContainer, repairShipBtn.GetOptimalSize(), ContainerPosition.MiddleOrCenter);
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

        private Btn BuildAddProductBtn()
        {
            var btn = Builder.NewBtnGeneral("button")
                .SetButtonStyle(Style.Global.PrimaryBtn)
                .SetText(new LocStrFormatted("添加产品"))
                .AddToolTip("将所选数量的产品添加到您的造船厂仓库中。")
                .OnClick(() => _shipyardCheatProvider.AddItemToShipyard(_selectedProduct.Value, (int)_quantity));

            return btn;
            
        }

        private Btn BuildFinishExplorationButton()
        {
            var btn = Builder.NewBtnGeneral("button")
                .SetButtonStyle(Style.Global.PrimaryBtn)
                .SetText(new LocStrFormatted("完成探索"))
                .AddToolTip("设置你的船做一个动作，然后按下这个按钮，他们会立即完成它。")
                .OnClick(() => _fleetCheatProvider.FinishExploration());

            return btn;
        }

        private Btn BuildRepairFleetButton()
        {
            var btn = Builder.NewBtnGeneral("button")
                .SetButtonStyle(Style.Global.PrimaryBtn)
                .SetText(new LocStrFormatted("修理船"))
                .AddToolTip("将您的主舰修复到完全无损状态。")
                .OnClick(() => _fleetCheatProvider.RepairFleet());

            return btn;
        }
        
        private Btn BuildForceUnloadShipyardShipButton()
        {
            var btn = Builder.NewBtnGeneral("button")
                .SetButtonStyle(Style.Global.PrimaryBtn)
                .SetText("强制卸载船舶")
                .AddToolTip("绕过造船厂货物容量检查并强行将您的船卸载到您的造船厂货物中。")
                .OnClick(() => _shipyardCheatProvider.ForceUnloadShipyardShip());
            return btn;
        }

        private Dropdwn BuildProductSelector()
        {
            var productDropdown = Builder
                .NewDropdown("ProductDropDown")
                .AddOptions(_productProtos.Select(x => ProductNameToChineseStr(x.Id.ToString().Replace("Product_", ""))).ToList())
                .OnValueChange(i => _selectedProduct = _productProtos.ElementAt(i)?.Id);

            _selectedProduct = _productProtos.ElementAt(0)?.Id;

            return productDropdown;
        }

        // 增加中文翻译
        private string ProductNameToChineseStr(string str)
        {
            switch (str)
            {
                case "Acid":
                    return "酸";
                case "Ammonia":
                    return "氨";
                case "Anesthetics":
                    return "麻醉剂";
                case "AnimalFeed":
                    return "动物饲料";
                case "Antibiotics":
                    return "抗生素";
                case "Biomass":
                    return "生物质";
                case "Bread":
                    return "面包";
                case "Bricks":
                    return "砖块(红砖)";
                case "Brine":
                    return "盐水";
                case "BrokenGlass":
                    return "碎玻璃";
                case "Cake":
                    return "蛋糕";
                case "Canola":
                    return "油菜籽";
                case "CarbonDioxide":
                    return "二氧化碳";
                case "Cement":
                    return "水泥";
                case "ChickenCarcass":
                    return "鸡肉";
                case "Chlorine":
                    return "氯";
                case "Coal":
                    return "煤炭";
                case "Compost":
                    return "堆肥";
                case "ConcreteSlab":
                    return "混凝土楼板(空心砖)";
                case "ConstructionParts":
                    return "结构件(一级)";
                case "ConstructionParts2":
                    return "结构件(二级)";
                case "ConstructionParts3":
                    return "结构件(三级)";
                case "ConstructionParts4":
                    return "结构件(四级)";
                case "ConsumerElectronics":
                    return "消费类电子产品";
                case "CookingOil":
                    return "食用油";
                case "Copper":
                    return "铜板";
                case "CopperOre":
                    return "铜矿";
                case "CopperOreCrushed":
                    return "粉碎铜矿石";
                case "CopperScrap":
                    return "废铜";
                case "CopperScrapPressed":
                    return "废铜废料";
                case "Corn":
                    return "玉米";
                case "CornMash":
                    return "玉米泥";
                case "CrudeOil":
                    return "原油";
                case "Diesel":
                    return "柴油";
                case "Dirt":
                    return "泥土";
                case "Disinfectant":
                    return "消毒剂";
                case "Eggs":
                    return "蛋";
                case "Electronics":
                    return "电子产品";
                case "Electronics2":
                    return "电子产品(二级)";
                case "Electronics3":
                    return "电子产品(三级)";
                case "Ethanol":
                    return "乙醇";
                case "Fertilizer":
                    return "肥料(一级)";
                case "Fertilizer2":
                    return "肥料(二级)";
                case "FertilizerOrganic":
                    return "有机肥料";
                case "FilterMedia":
                    return "过滤介质";
                case "Flour":
                    return "面粉";
                case "Flowers":
                    return "花";
                case "FoodPack":
                    return "包装食品";
                case "Fruit":
                    return "水果";
                case "FuelGas":
                    return "燃气";
                case "Glass":
                    return "玻璃";
                case "GlassMix":
                    return "玻璃混合物";
                case "Gold":
                    return "金";
                case "GoldOre":
                    return "金矿";
                case "GoldOreConcentrate":
                    return "金精矿";
                case "GoldOreCrushed":
                    return "粉碎金矿石";
                case "GoldOrePowder":
                    return "金矿粉";
                case "GoldScrap":
                    return "金废料";
                case "Graphite":
                    return "石墨";
                case "Gravel":
                    return "碎石";
                case "HeavyOil":
                    return "重油";
                case "HouseholdAppliances":
                    return "家用设备";
                case "HouseholdGoods":
                    return "家庭用品";
                case "Hydrogen":
                    return "氢";
                case "HydrogenFluoride":
                    return "氟化氢";
                case "ImpureCopper":
                    return "不纯铜";
                case "Iron":
                    return "铁";
                case "IronOre":
                    return "铁矿";
                case "IronOreCrushed":
                    return "粉碎铁矿石";
                case "IronScrap":
                    return "铁废料";
                case "LabEquipment":
                    return "实验室设备";
                case "LabEquipment2":
                    return "实验室设备(二级)";
                case "LabEquipment3":
                    return "实验室设备(三级)";
                case "LabEquipment4":
                    return "实验室设备(四级)";
                case "LightOil":
                    return "轻油";
                case "Limestone":
                    return "石灰石";
                case "Meat":
                    return "肉";
                case "MeatTrimmings":
                    return "碎肉";
                case "MechanicalParts":
                    return "机械零件";
                case "MedicalEquipment":
                    return "医用器材";
                case "MedicalSupplies":
                    return "医疗用品";
                case "MedicalSupplies2":
                    return "医疗用品(二级)";
                case "MedicalSupplies3":
                    return "医疗用品(三级)";
                case "MediumOil":
                    return "中油";
                case "Microchips":
                    return "微芯片";
                case "Morphine":
                    return "吗啡";
                case "Naphtha":
                    return "石脑油";
                case "Nitrogen":
                    return "氮";
                case "NitrogenLiquidTank":
                    return "氮气液罐";
                case "Oxygen":
                    return "氧";
                case "PCB":
                    return "电路板";
                case "Plastic":
                    return "塑料";
                case "PolySilicon":
                    return "多晶硅";
                case "Poppy":
                    return "罂粟";
                case "Potato":
                    return "土豆";
                case "Quartz":
                    return "石英";
                case "QuartzCrushed":
                    return "石英碎";
                case "Recyclables":
                    return "可回收物";
                case "Rock":
                    return "岩石";
                case "Rubber":
                    return "橡胶";
                case "Salt":
                    return "盐";
                case "Sand":
                    return "沙";
                case "Sausage":
                    return "香肠";
                case "Seawater":
                    return "海水";
                case "Slag":
                    return "矿渣";
                case "SlagCrushed":
                    return "粉碎矿渣";
                case "Sludge":
                    return "污泥";
                case "Snack":
                    return "小吃";
                case "SolarCell":
                    return "太阳能电池";
                case "SolarCellMono":
                    return "太阳能电池单体";
                case "SourWater":
                    return "酸水";
                case "Soybean":
                    return "黄豆";
                case "SpentFuel":
                    return "乏燃料";
                case "Steel":
                    return "钢";
                case "Sugar":
                    return "糖";
                case "SugarCane":
                    return "甘蔗";
                case "Sulfur":
                    return "硫";
                case "Tofu":
                    return "豆腐";
                case "ToxicSlurry":
                    return "有毒液体";
                case "UraniumOre":
                    return "铀矿石";
                case "UraniumOreCrushed":
                    return "粉碎铀矿石";
                case "UraniumPellets":
                    return "铀丸";
                case "UraniumRod":
                    return "铀棒";
                case "Vegetables":
                    return "蔬菜";
                case "VehicleParts":
                    return "车辆零件";
                case "VehicleParts2":
                    return "车辆零件(二级)";
                case "VehicleParts3":
                    return "车辆零件(三级)";
                case "Waste":
                    return "垃圾";
                case "WasteWater":
                    return "废水";
                case "Water":
                    return "水";
                case "Wheat":
                    return "小麦";
                case "Wood":
                    return "木头";
                case "Woodchips":
                    return "木屑";
                case "Yellowcake":
                    return "黄饼";
                case "FissionProduct":
                    return "裂变产物";
                case "GoldScrapPressed":
                    return "压金废料";
                case "IronScrapPressed":
                    return "压铁废料";
                case "ManufacturedSand":
                    return "机制砂";
                case "MoxRod":
                    return "Mox棒";
                case "SpentMox":
                    return "用过的Mox";
                case "Paper":
                    return "纸";
                case "Plutonium":
                    return "钚";
                case "RecyclablesPressed":
                    return "压实的可回收物";
                case "RetiredWaste":
                    return "可回收的核废料";
                case "Server":
                    return "服务器";
                case "SiliconWafer":
                    return "硅片";
                case "TreeSapling":
                    return "树苗";
                case "UraniumDepleted":
                    return "贫铀";
                case "UraniumEnriched":
                    return "浓缩铀(4%)";
                case "UraniumEnriched20":
                    return "浓缩铀(20%)";
                case "UraniumReprocessed":
                    return "再加工铀(1%)";
                case "WastePressed":
                    return "压缩的垃圾";
                default:
                    return str;
            }
        }

        private Slidder BuildQuantitySlider()
        {
            var sliderLabel = Builder
                .NewTxt("")
                .SetTextStyle(Builder.Style.Global.TextControls)
                .SetAlignment(TextAnchor.MiddleLeft);
            var qtySlider = Builder
                .NewSlider("qtySlider")
                .SimpleSlider(Builder.Style.Panel.Slider)
                .SetValuesRange(10f, 10000f)
                .OnValueChange(
                    qty => { sliderLabel.SetText(Math.Round(qty).ToString()); },
                    qty =>
                    {
                        sliderLabel.SetText(Math.Round(qty).ToString());
                        _quantity = qty;
                    })
                .SetValue(_quantity);


            sliderLabel.PutToRightOf(qtySlider, 90f, Offset.Right(-110f));

            return qtySlider;
        }
    }
}