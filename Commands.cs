﻿using System.Text;
using Terraria;
using TShockAPI;

namespace AutoFish
{
    public class Commands
    {
        #region 菜单方法
        private static void HelpCmd(TSPlayer plr)
        {
            if (plr == null)
            {
                return;
            }
            else
            {
                //普通玩家
                if (!plr.HasPermission("autofish.admin"))
                {
                    var mess = new StringBuilder();
                    mess.AppendFormat($"          [i:3455][c/AD89D5:自][c/D68ACA:动][c/DF909A:钓][c/E5A894:鱼][i:3454]");

                    if (AutoFish.Config.AdvertisementEnabled)
                    {
                        mess.AppendFormat(AutoFish.Config.Advertisement);
                    }

                    mess.AppendFormat("\n/af -- 查看自动钓鱼菜单\n" +
                     "/af on -- 自动钓鱼[c/4686D4:开启]功能\n" +
                     "/af off -- 自动钓鱼[c/F25055:关闭]功能\n" +
                     "/af buff -- 开启丨关闭[c/F6B152:钓鱼BUFF]");

                    if (AutoFish.Config.DoorItems.Any())
                    {
                        mess.AppendFormat("\n/af loot -- 查看[c/F25055:额外渔获表]");
                    }

                    if (AutoFish.Config.ConMod)
                    {
                        mess.AppendFormat("\n/af list -- 列出消耗模式[c/F5F251:指定物品表]");
                    }

                    plr.SendMessage(mess.ToString(), 193, 223, 186);
                }

                //管理员
                else
                {
                    var mess = new StringBuilder();
                    mess.AppendFormat($"          [i:3455][c/AD89D5:自][c/D68ACA:动][c/DF909A:钓][c/E5A894:鱼][i:3454]");

                    if (AutoFish.Config.AdvertisementEnabled)
                    {
                        mess.AppendFormat(AutoFish.Config.Advertisement);
                    }

                    mess.AppendFormat("\n/af on 或 off -- 自动钓鱼[c/4686D4:开启]|[c/F25055:关闭]功能\n" +
                     "/af buff -- 开启丨关闭[c/F6B152:钓鱼BUFF]\n" +
                     "/af more -- 开启丨关闭[c/DB48A7:多线模式]\n" +
                     "/af duo 数字 -- 设置多线的[c/4686D4:钩子数量]\n" +
                     "/af mod -- 开启丨关闭[c/50D647:消耗模式]");

                    if (AutoFish.Config.ConMod)
                    {
                        mess.AppendFormat("\n/af list -- 列出消耗[c/F5F251:指定物品表]\n" +
                        "/af set 数量 -- 设置消耗[c/47C2D5:物品数量]要求\n" +
                        "/af time 数字 -- 设置消耗[c/F6B152:自动时长]\n" +
                        "/af add 或 del 物品名 -- [c/87DF86:添加]|[c/F25055:移除]消耗指定物品");
                    }

                    if (AutoFish.Config.DoorItems.Any())
                    {
                        mess.AppendFormat("\n/af loot -- 查看[c/F25055:额外渔获表]\n" +
                            "/af + 或 - 名字 -- 为额外渔获[c/87DF86:添加]|[c/F25055:移除]物品");
                    }

                    plr.SendMessage(mess.ToString(), 193, 223, 186);
                }
            }
        }
        #endregion

        public static void Afs(CommandArgs args)
        {
            var plr = args.Player;
            var data = AutoFish.Data.Items.FirstOrDefault(item => item.Name == plr.Name);

            if (!AutoFish.Config.Enabled)
            {
                return;
            }

            if (data == null)
            {
                args.Player.SendInfoMessage("请用角色[c/D95065:重进服务器]后输入：/af 指令查看菜单\n羽学声明：本插件纯属[c/7E93DE:免费]请勿上当受骗", 217, 217, 217);
                return;
            }

            //消耗模式下的剩余时间记录
            var Minutes = AutoFish.Config.timer - (DateTime.Now - data.LogTime).TotalMinutes;

            if (args.Parameters.Count == 0)
            {
                HelpCmd(args.Player);

                if (!data.Enabled)
                {
                    args.Player.SendSuccessMessage($"请输入该指令开启→: [c/92C5EC:/af on]");
                }

                //开启了消耗模式
                else if (AutoFish.Config.ConMod)
                {
                    args.Player.SendMessage($"自动钓鱼[c/46C4D4:剩余时长]：[c/F3F292:{Math.Floor(Minutes)}]分钟", 243, 181, 145);
                }

                //检测到血月
                if(Main.bloodMoon)
                {
                    args.Player.SendMessage($"当前为[c/F25055:血月]无法钓上怪物，可[c/46C4D4:关闭]插件：[c/F3F292:/af off]", 243, 181, 145);
                }
                return;
            }

            if (args.Parameters.Count == 1)
            {
                if (args.Parameters[0].ToLower() == "on")
                {
                    data.Enabled = true;
                    args.Player.SendSuccessMessage($"玩家 [{args.Player.Name}] 已[c/92C5EC:启用]自动钓鱼功能。");
                    return;
                }

                if (args.Parameters[0].ToLower() == "off")
                {
                    data.Enabled = false;
                    args.Player.SendSuccessMessage($"玩家 [{args.Player.Name}] 已[c/92C5EC:禁用]自动钓鱼功能。");
                    return;
                }

                if (args.Parameters[0].ToLower() == "buff")
                {
                    var isEnabled = data.Buff;
                    data.Buff = !isEnabled;
                    var Mess = isEnabled ? "禁用" : "启用";
                    args.Player.SendSuccessMessage($"玩家 [{args.Player.Name}] 已[c/92C5EC:{Mess}]自动钓鱼BUFF");
                    return;
                }

                if (args.Parameters[0].ToLower() == "list" && AutoFish.Config.ConMod)
                {
                    args.Player.SendInfoMessage($"[指定消耗物品表]\n" + string.Join(", ", AutoFish.Config.BaitType.Select(x => TShock.Utils.GetItemById(x).Name + "([c/92C5EC:{0}])".SFormat(x))));
                    args.Player.SendSuccessMessage($"兑换规则为：每[c/F5F252:{AutoFish.Config.BaitStack}]个 => [c/92C5EC:{AutoFish.Config.timer}]分钟");
                    return;
                }

                if (args.Parameters[0].ToLower() == "loot" && AutoFish.Config.DoorItems.Any())
                {
                    args.Player.SendInfoMessage($"[额外渔获表]\n" + string.Join(", ", AutoFish.Config.DoorItems.Select(x => TShock.Utils.GetItemById(x).Name + "([c/92C5EC:{0}])".SFormat(x))));
                    return;
                }

                //管理权限
                if (plr.HasPermission("autofish.admin"))
                {
                    if (args.Parameters[0].ToLower() == "more")
                    {
                        var isEnabled = AutoFish.Config.MoreHook;
                        AutoFish.Config.MoreHook = !isEnabled;
                        var Mess = isEnabled ? "禁用" : "启用";
                        args.Player.SendSuccessMessage($"玩家 [{args.Player.Name}] 已[c/92C5EC:{Mess}]多线模式");
                        AutoFish.Config.Write();
                        return;
                    }

                    if (args.Parameters[0].ToLower() == "mod")
                    {
                        var isEnabled = AutoFish.Config.ConMod;
                        AutoFish.Config.ConMod = !isEnabled;
                        var Mess = isEnabled ? "禁用" : "启用";
                        args.Player.SendSuccessMessage($"玩家 [{args.Player.Name}] 已[c/92C5EC:{Mess}]消耗模式");
                        AutoFish.Config.Write();
                        return;
                    }
                }
            }

            //管理权限
            if (plr.HasPermission("autofish.admin"))
            {
                if (args.Parameters.Count == 2)
                {
                    Item item;
                    var Items = TShock.Utils.GetItemByIdOrName(args.Parameters[1]);
                    if (Items.Count > 1)
                    {
                        args.Player.SendMultipleMatchError(Items.Select(i => i.Name));
                        return;
                    }

                    if (Items.Count == 0)
                    {
                        args.Player.SendErrorMessage("不存在该物品，\"物品查询\": \"[c/92C5EC:https://terraria.wiki.gg/zh/wiki/Item_IDs]\"");
                        return;
                    }

                    else
                    {
                        item = Items[0];
                    }

                    switch (args.Parameters[0].ToLower())
                    {
                        case "add":
                            {
                                if (AutoFish.Config.BaitType.Contains(item.type))
                                {
                                    args.Player.SendErrorMessage("物品 [c/92C5EC:{0}] 已在指定鱼饵表中!", item.Name);
                                    return;
                                }
                                AutoFish.Config.BaitType.Add(item.type);
                                AutoFish.Config.Write();
                                args.Player.SendSuccessMessage("已成功将物品添加指定鱼饵表: [c/92C5EC:{0}]!", item.Name);
                                break;
                            }

                        case "del":
                            {
                                if (!AutoFish.Config.BaitType.Contains(item.type))
                                {
                                    args.Player.SendErrorMessage("物品 {0} 不在指定鱼饵表中!", item.Name);
                                    return;
                                }
                                AutoFish.Config.BaitType.Remove(item.type);
                                AutoFish.Config.Write();
                                args.Player.SendSuccessMessage("已成功从指定鱼饵表移出物品: [c/92C5EC:{0}]!", item.Name);
                                break;
                            }

                        case "+":
                            {
                                if (AutoFish.Config.DoorItems.Contains(item.type))
                                {
                                    args.Player.SendErrorMessage("物品 [c/92C5EC:{0}] 已在额外渔获表中!", item.Name);
                                    return;
                                }
                                AutoFish.Config.DoorItems.Add(item.type);
                                AutoFish.Config.Write();
                                args.Player.SendSuccessMessage("已成功将物品添加额外渔获表: [c/92C5EC:{0}]!", item.Name);
                                break;
                            }

                        case "-":
                            {
                                if (!AutoFish.Config.DoorItems.Contains(item.type))
                                {
                                    args.Player.SendErrorMessage("物品 {0} 不在额外渔获中!", item.Name);
                                    return;
                                }
                                AutoFish.Config.DoorItems.Remove(item.type);
                                AutoFish.Config.Write();
                                args.Player.SendSuccessMessage("已成功从额外渔获移出物品: [c/92C5EC:{0}]!", item.Name);
                                break;
                            }

                        case "set":
                            {
                                if (int.TryParse(args.Parameters[1], out var num))
                                {
                                    AutoFish.Config.BaitStack = num;
                                    AutoFish.Config.Write();
                                    args.Player.SendSuccessMessage("已成功将物品数量要求设置为: [c/92C5EC:{0}] 个!", num);
                                }
                                break;
                            }

                        case "duo":
                            {
                                if (int.TryParse(args.Parameters[1], out var num))
                                {
                                    AutoFish.Config.HookMax = num;
                                    AutoFish.Config.Write();
                                    args.Player.SendSuccessMessage("已成功将多钩数量上限设置为: [c/92C5EC:{0}] 个!", num);
                                }
                                break;
                            }

                        case "time":
                            {
                                if (int.TryParse(args.Parameters[1], out var num))
                                {
                                    AutoFish.Config.timer = num;
                                    AutoFish.Config.Write();
                                    args.Player.SendSuccessMessage("已成功将自动时长设置为: [c/92C5EC:{0}] 分钟!", num);
                                }
                                break;
                            }

                        default:
                            {
                                HelpCmd(args.Player);
                                break;
                            }
                    }
                }
            }
        }
    }
}
