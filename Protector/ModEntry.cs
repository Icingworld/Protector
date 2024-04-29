using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Projectiles;
using static StardewValley.Projectiles.BasicProjectile;
using xTile.Dimensions;
using GenericModConfigMenu;
using System.Net.Http.Headers;

namespace Protector
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        // 配置文件
        private ModConfig config;

        // Protector信息
        private bool isActive = false;
        private bool isAutoActive;
        private int damage=9999;
        private bool explode;
        private int range;
        private float speed;
        private string bindKey;
        private SButton BindValueButton;

        // flags
        private bool isLaunching = false;
        private int tickCount = 0;

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            

            // 加载 config.json
            // 如果没有文件则会自动创建
            config = Helper.ReadConfig<ModConfig>();
            isAutoActive = config.IsAutoActive;
            isActive = isAutoActive;
            bindKey = config.BindKey;
            BindValueButton = (SButton)Enum.Parse(typeof(SButton), bindKey, true);
            explode = config.Explode;
            range = config.Range;
            speed = config.Speed;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // 在游戏启动后监听每一帧
            Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;

            // 注册menu api
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;
            // 注册 mod
            configMenu.Register(
                mod: ModManifest,
                reset: () => config = new ModConfig(),
                save: () => {
                    ModConfig newConfig = new(
                        isAutoActive: isAutoActive,
                        bindKey: bindKey,
                        speed: speed,
                        range: range,
                        explode: explode
                    );
                    Helper.WriteConfig(newConfig);
                    // 提醒配置已经更改
                    Game1.addHUDMessage(new HUDMessage("New Protector config saved.", 2));
                }
            );

            // 添加UI选项
            configMenu.AddBoolOption(
                mod: ModManifest,
                fieldId: "1",
                name: () => "自动开启",
                tooltip: () => "Protector是否自动启动",
                getValue: () => isAutoActive,
                setValue: value => isAutoActive = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                fieldId: "2",
                name: () => "状态",
                tooltip: () => "Protector当前状态",
                getValue: () => isActive,
                setValue: value => { isActive = value; AlertActivate(); }
            );
            configMenu.AddKeybind(
                mod: ModManifest,
                fieldId: "4",
                name: () => "按键绑定",
                tooltip: () => "Protector模组开启关闭按键",
                getValue: () => GetKeyBind(),
                setValue: value => SetValueBind(value)
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                fieldId: "5",
                name: () => "弹药速度",
                tooltip: () => "弹药飞行速度",
                min: 20,
                max: 30,
                getValue: () => speed,
                setValue: value => speed = value
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                fieldId: "6",
                name: () => "警戒范围",
                tooltip: () => "自动攻击范围内的敌人",
                min: 300,
                max: 900,
                getValue: () => range,
                setValue: value => range = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                fieldId: "3",
                name: () => "弹药爆炸",
                tooltip: () => "发射的弹药是否会爆炸",
                getValue: () => explode,
                setValue: value => explode = value
            );
        }

        private SButton GetKeyBind()
        {
            return BindValueButton;
        }

        private void SetValueBind(SButton newButton)
        {
            BindValueButton = newButton;
            // 转为string
            bindKey = BindValueButton.ToString();
        }

        private void OnUpdateTicked(object sender, EventArgs e)
        {
            if (!isActive || !Context.IsWorldReady)
            {
                return;
            }

            // 检查上一次的子弹是否已经销毁
            // isLaunching = Game1.currentLocation.projectiles.Count != 0;

            // 如果在射击，则tickCount+=1，过期销毁
            if (isLaunching)
            {
                tickCount++;
                if (tickCount >= 10)
                {
                    Game1.currentLocation.projectiles.Clear();
                    isLaunching = false;
                    tickCount = 0;
                }
            } else
            {
                tickCount = 0;
            }

            // 检查是否有怪物
            Vector2 position = Game1.player.Position;
            foreach (NPC monster in Game1.currentLocation.characters)
            {
                if (monster.IsMonster && Vector2.Distance(monster.Position, position) <= range && !isLaunching)
                {
                    // 设置锁
                    isLaunching = true;
                    // 设置伤害
                    // damage = ((Monster)monster).Health;
                    // 发射弹药
                    LaunchAmmo(position, monster);
                    break; // 只发射一次弹药
                }
            }
        }
        /*********
        ** Private methods
        *********/
        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;

            // 控制Protector启动
            if (e.Button == (SButton)Enum.Parse(typeof(SButton), bindKey, true))
            {
                isActive = !isActive;
                AlertActivate();
                if (!isActive) {
                    // 删除所有弹药，用于卡壳后的重新上弹
                    Game1.currentLocation.projectiles.Clear();
                }
            }
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            // AlertActivate();
        }

        private void AlertActivate()
        {
            // 检查是否开启，并提示
            if (isActive)
            {
                Game1.addHUDMessage(new HUDMessage("Protector activated.", 4));
            } else
            {
                Game1.addHUDMessage(new HUDMessage("Protector deactivated.", 3));
            }
        }

        private void LaunchAmmo(Vector2 position, NPC monster)
        {
            // 根据距离计算速度
            float speedValue = Vector2.Distance(monster.Position, position) / 15 + 10;
            // 计算发射方向
            Vector2 direction = Utility.getVelocityTowardPoint(position, monster.Position, speedValue);

            // 创建弹药实例并设置属性
            BasicProjectile ammo = new(
                damage,
                Projectile.shadowBall,
                0,
                0,
                0,
                direction.X,
                direction.Y,
                position,
                explode: explode,
                firer: Game1.player,
                damagesMonsters: true,
                location: Game1.currentLocation
                );

            // 将弹药添加到游戏中
            Game1.currentLocation.projectiles.Add(ammo);
        }
    }
}