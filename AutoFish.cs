using AutoFish.Utils;
using Terraria;
using Terraria.ID;
using TShockAPI;
using TShockAPI.Hooks;
using TerrariaApi.Server;
using System.Text;

namespace AutoFish;

[ApiVersion(2, 1)]
public class AutoFish : TerrariaPlugin
{

    #region 插件信息
    public override string Name => "自动钓鱼";
    public override string Author => "羽学 少司命";
    public override Version Version => new Version(1, 2, 0);
    public override string Description => "涡轮增压不蒸鸭";
    #endregion

    #region 注册与释放
    public AutoFish(Main game) : base(game) { }
    public override void Initialize()
    {
        LoadConfig();
        GeneralHooks.ReloadEvent += ReloadConfig;
        GetDataHandlers.NewProjectile += ProjectNew!;
        GetDataHandlers.NewProjectile += BuffUpdate!;
        ServerApi.Hooks.ServerJoin.Register(this, this.OnJoin);
        GetDataHandlers.PlayerUpdate.Register(this.OnPlayerUpdate);
        ServerApi.Hooks.ProjectileAIUpdate.Register(this, ProjectAiUpdate);
        TShockAPI.Commands.ChatCommands.Add(new Command("autofish", Commands.Afs, "af", "autofish"));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            GeneralHooks.ReloadEvent -= ReloadConfig;
            GetDataHandlers.NewProjectile -= ProjectNew!;
            GetDataHandlers.NewProjectile -= BuffUpdate!;
            ServerApi.Hooks.ServerJoin.Deregister(this, this.OnJoin);
            GetDataHandlers.PlayerUpdate.UnRegister(this.OnPlayerUpdate);
            ServerApi.Hooks.ProjectileAIUpdate.Deregister(this, ProjectAiUpdate);
            TShockAPI.Commands.ChatCommands.RemoveAll(x => x.CommandDelegate == Commands.Afs);
        }
        base.Dispose(disposing);
    }
    #endregion

    #region 配置重载读取与写入方法
    internal static Configuration Config = new();
    private static void ReloadConfig(ReloadEventArgs args)
    {
        LoadConfig();
        args.Player.SendInfoMessage("[多线钓鱼]重新加载配置完毕。");
    }
    private static void LoadConfig()
    {
        Config = Configuration.Read();
        Config.Write();
    }
    #endregion

    #region 玩家更新配置方法（创建配置结构）
    internal static MyData Data = new();
    private void OnJoin(JoinEventArgs args)
    {
        if (args == null || !Config.Enabled)
        {
            return;
        }

        var plr = TShock.Players[args.Who];

        if (plr == null)
        {
            return;
        }

        // 如果玩家不在数据表中，则创建新的数据条目
        if (!Data.Items.Any(item => item.Name == plr.Name))
        {
            Data.Items.Add(new MyData.ItemData()
            {
                Name = plr.Name,
                Enabled = true,
                Mod = false,
                Bait = new Dictionary<int, int> { { 0, 0 }, }
            });
        }
    }
    #endregion

    #region 触发自动钓鱼方法
    private void ProjectAiUpdate(ProjectileAiUpdateEventArgs args)
    {
        if (args.Projectile.owner is < 0 or > Main.maxPlayers ||
            !args.Projectile.active ||
            !args.Projectile.bobber ||
            !Config.Enabled)
            return;

        var plr = TShock.Players[args.Projectile.owner];
        if (plr == null || !plr.Active || !plr.HasPermission("autofish"))
            return;

        // 从数据表中获取与玩家名字匹配的配置项
        var list = Data.Items.FirstOrDefault(x => x.Name == plr.Name);
        // 如果没有找到配置项，或者自动钓鱼功能或启用状态未设置，则返回
        if (list == null || !list.Enabled)
        {
            return;
        }

        //开启消费模式
        if (Config.ConMod)
        {
            //多加一个list.Mod来判断玩家是否花费了【指定鱼饵】来换取功能时长
            if (list.Mod && list.Enabled)
            {
                ControlFishing(args, plr,list);
            }
        }
        else
        {
            //否则只要打开插件开关就能使用功能
            if (list.Enabled)
            {
                ControlFishing(args, plr,list);
            }
        }
    }
    #endregion

    #region 自动钓鱼核心逻辑
    private static void ControlFishing(ProjectileAiUpdateEventArgs args, TSPlayer plr, MyData.ItemData list)
    {
        // 当鱼漂上钩了物品
        if (args.Projectile.ai[1] < 0)
        {
            args.Projectile.ai[0] = 1.0f; //设置ai[0]为1.0f，用于控制收杆行为

            // 检查并选择消耗饵料
            plr.TPlayer.ItemCheck_CheckFishingBobber_PickAndConsumeBait(args.Projectile, out var pullTheBobber, out var baitTypeUsed);
            if (pullTheBobber) // 如果成功拉起鱼漂
            {
                // 执行拉起鱼漂的动作
                plr.TPlayer.ItemCheck_CheckFishingBobber_PullBobber(args.Projectile, baitTypeUsed);

                // 更新玩家背包 使用饵料信息
                for (var i = 0; i < plr.TPlayer.inventory.Length; i++)
                {
                    if (plr.TPlayer.inventory[i].bait > 0 && baitTypeUsed == plr.TPlayer.inventory[i].type)
                    {
                        plr.SendData(PacketTypes.PlayerSlot, "", plr.Index, i);
                    }

                    //避免线程卡死 留一个物品中止循环
                    if (baitTypeUsed == plr.TPlayer.inventory[i].type && plr.TPlayer.inventory[i].stack <= 1)
                    {
                        list.Enabled = false;
                        plr.SendMessage($"[c/46C2D4:{plr.Name}] 鱼饵不足，已关闭[c/F5F251:自动钓鱼]功能|重新开启:[c/46C2D4:/af on] ", 247, 244, 150);
                    }
                }

                // 使用松露虫则把ai[1]设为默认值（也就是钓猪鲨）
                if (baitTypeUsed == 2673)
                {
                    args.Projectile.ai[1] = 0;
                    plr.SendData(PacketTypes.ProjectileNew, "", args.Projectile.whoAmI);
                    return;
                }
            }

            // 如果配置了自定义物品
            if (Config.DoorItems.Any())
            {
                // 随机选取一个特殊物品作为鱼漂的ai[1]
                args.Projectile.ai[1] = Convert.ToSingle(Config.DoorItems.OrderByDescending(x => Guid.NewGuid()).First());
            }
            else
            {
                // 否则进行常规的钓鱼检查
                do
                {
                    args.Projectile.FishingCheck();
                    args.Projectile.ai[1] = args.Projectile.localAI[1];
                }
                while (args.Projectile.ai[1] <= 0); // 确保ai[1]大于0

            }
            plr.SendData(PacketTypes.ProjectileNew, "", args.Projectile.whoAmI);

            // 创建新的弹幕实例
            var index = SpawnProjectile.NewProjectile(Main.projectile[args.Projectile.whoAmI].GetProjectileSource_FromThis(),
                args.Projectile.position, args.Projectile.velocity, args.Projectile.type, 0, 0, args.Projectile.owner, 0, 0, 0);

            plr.SendData(PacketTypes.ProjectileNew, "", index);
        }
    }
    #endregion

    #region 多线钓鱼
    public void ProjectNew(object sender, GetDataHandlers.NewProjectileEventArgs e)
    {
        var plr = e.Player;
        var guid = Guid.NewGuid().ToString();
        var HookCount = Main.projectile.Count(p => p.active && p.owner == e.Owner && p.bobber); // 浮漂计数

        if (plr == null ||
            !plr.Active ||
            !plr.IsLoggedIn ||
            !Config.Enabled ||
            !Config.MoreHook ||
            !plr.HasPermission("autofish") ||
            HookCount > Config.HookMax - 1)
            return;

        // 从数据表中获取与玩家名字匹配的配置项
        var list = Data.Items.FirstOrDefault(x => x.Name == plr.Name);
        // 如果没有找到配置项，或者自动钓鱼功能或启用状态未设置，则返回
        if (list == null || !list.Enabled)
        {
            return;
        }

        //开启消费模式
        if (Config.ConMod)
        {
            //玩家的自动钓鱼开关
            if (list.Mod && list.Enabled)
            {
                // 检查是否上钩
                if (Tools.BobbersActive(e.Owner))
                {
                    //构建新弹幕
                    var index = SpawnProjectile.NewProjectile(Main.projectile[e.Index].GetProjectileSource_FromThis(),
                        e.Position, e.Velocity, e.Type, (int)e.Damage, e.Knockback, e.Owner, 0, 0, 0, -1, guid);

                    plr.SendData(PacketTypes.ProjectileNew, "", index);

                    // 更新多线计数
                    HookCount++;
                }
            }
        }

        else //正常模式下多线
        {
            if (list.Enabled)
            {
                if (Tools.BobbersActive(e.Owner))
                {
                    var index = SpawnProjectile.NewProjectile(Main.projectile[e.Index].GetProjectileSource_FromThis(),
                        e.Position, e.Velocity, e.Type, (int)e.Damage, e.Knockback, e.Owner, 0, 0, 0, -1, guid);

                    plr.SendData(PacketTypes.ProjectileNew, "", index);

                    HookCount++;
                }
            }
        }
    }
    #endregion

    #region Buff更新方法
    public void BuffUpdate(object sender, GetDataHandlers.NewProjectileEventArgs e)
    {
        var plr = e.Player;

        if (plr == null || !plr.Active || !plr.IsLoggedIn || !Config.Enabled || !Config.Buff || !plr.HasPermission("autofish"))
        {
            return;
        }

        // 从数据表中获取与玩家名字匹配的配置项
        var list = Data.Items.FirstOrDefault(x => x.Name == plr.Name);
        if (list == null)
        {
            return;
        }

        //出现鱼钩摆动就给玩家施加buff
        if (list.Enabled)
        {
            if (Tools.BobbersActive(e.Owner))
            {
                foreach (var buff in Config.BuffID)
                {
                    plr.SetBuff(buff.Key, buff.Value);
                }
            }
        }
    }
    #endregion

    #region 消费模式
    private static int ClearCount = 0; //需要关闭钓鱼权限的玩家计数
    private void OnPlayerUpdate(object? sender, GetDataHandlers.PlayerUpdateEventArgs e)
    {
        var plr = e.Player;
        if (!Config.Enabled || !Config.ConMod || e == null || plr == null || !plr.IsLoggedIn || !plr.Active || !plr.HasPermission("autofish"))
        {
            return;
        }

        var data = Data.Items.FirstOrDefault(x => x.Name == plr.Name);

        if (data == null || !data.Enabled)
        {
            return;
        }

        // 玩家自己插件开关
        if (data != null)
        {
            bool flag = false;
            int TotalCount = 0; // 用于记录背包中所有鱼饵的总数量

            // 遍历背包58格
            for (var i = 0; i < plr.TPlayer.inventory.Length; i++)
            {
                var inv = plr.TPlayer.inventory[i];

                // 是指定鱼饵
                if (Config.BaitType.Contains(inv.type))
                {
                    // 添加到玩家自己的字典
                    Tools.UpDict(data.Bait, inv.type, inv.stack);

                    // 累加背包中的鱼饵数量
                    TotalCount += inv.stack;

                    // 如果总数量超过设定值
                    if (TotalCount >= Config.BaitStack)
                    {
                        // 开启标识
                        flag = true;
                        break;
                    }
                }
            }

            if (flag && !data.Mod)
            {
                int Remain = Config.BaitStack;
                var Message = new StringBuilder();

                // 遍历并更新库存
                foreach (var pair in data.Bait.ToList())
                {
                    var index = Tools.GetBait(plr, pair.Key); // 获取背包中对应鱼饵的索引
                    if (index != -1)
                    {
                        int BaitStack = Math.Min(Remain, plr.TPlayer.inventory[index].stack);// 计算需要消耗的鱼饵数量

                        plr.TPlayer.inventory[index].stack -= BaitStack;// 从背包中扣除鱼饵
                        plr.SendData(PacketTypes.PlayerSlot, "", plr.Index, PlayerItemSlotID.Inventory0 + index);// 同步背包变化

                        // 更新字典中的数量
                        data.Bait[pair.Key] -= BaitStack;
                        if (data.Bait[pair.Key] < 1)
                        {
                            data.Bait.Remove(pair.Key); // 如果数量为0，移除该项
                        }

                        // 记录消耗的鱼饵
                        Message.AppendFormat("{0}({1}),", TShock.Utils.GetItemById(pair.Key).Name, BaitStack);

                        // 减少剩余需要消耗的鱼饵数量
                        Remain -= BaitStack;

                        if (Remain <= 0)
                        {
                            break; // 已经消耗完毕，退出循环
                        }
                    }
                }

                if (Remain <= 0)
                {
                    // 开启自动钓鱼开关
                    data.Mod = true;

                    // 记录当前时间
                    data.LogTime = DateTime.Now;

                    plr.SendMessage($"[c/46C2D4:{plr.Name}] 已开启[c/F5F251:自动钓鱼]功能 消耗物品为:{Message}", 247, 244, 150);
                }
            }
        }



        // 检查是否有玩家的自动钓鱼权限需要关闭
        var Remove = Data.Items.Where(list => list != null && list.LogTime != default &&
            (DateTime.Now - list.LogTime).TotalMinutes >= Config.timer).ToList();

        if (Remove != null)
        {
            var mess = new StringBuilder();
            mess.AppendLine($"[i:3455][c/AD89D5:自][c/D68ACA:动][c/DF909A:钓][c/E5A894:鱼][i:3454]");
            mess.AppendLine($"以下玩家超过 [c/E17D8C:{Config.timer}] 分钟 已关闭[c/76D5B4:自动钓鱼]权限：");

            foreach (var plr2 in Remove)
            {
                // 只显示分钟
                var Minutes = (DateTime.Now - plr2.LogTime).TotalMinutes;
                FormattableString minutes = $"{Minutes:F0}";

                // 更新时间超过自动时长 关闭自动钓鱼权限
                if (Minutes >= Config.timer)
                {
                    ClearCount++;
                    plr2.Mod = false;
                    plr2.LogTime = default; // 清空记录时间
                    mess.AppendFormat("[c/A7DDF0:{0}]:[c/74F3C9:{1}分钟], ", plr2.Name, minutes);
                }
            }

            // 确保有一个玩家计数，只播报一次
            if (ClearCount > 0 && mess.Length > 0)
            {
                //广告开关
                if (Config.AdvertisementEnabled)
                {
                    //自定义广告内容
                    mess.AppendLine(Config.Advertisement);
                }
                plr.SendMessage(mess.ToString(), 247, 244, 150);
                ClearCount = 0;
            }
        }
    }
    #endregion

}