using RinBot.Commands.Modules.StellaWar.Core.Building;
using RinBot.Commands.Modules.StellaWar.Core.Ship;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RinBot.Commands.Modules.StellaWar.Core.War
{
    internal class AggressiveWar
    {
        public uint Attacker;
        public uint Defender;

        public List<BaseShip> AttackerFleet;
        public List<BaseShip> DefenderFleet;

        public StarBase AttackerStarBase;
        public StarBase DefenderStarBase;

        public List<BaseShip> AttackerLost = new();
        public List<BaseShip> AttackerRetreat = new();
        public List<BaseShip> DefenderLost = new();
        public List<BaseShip> DefenderRetreat = new();

        public long MemoryRobbed = 0;
        public int DayPassed = 0;

        public bool IsOver = false;
        public bool IsSuccess = false;

        public AggressiveWar(uint attacker, uint defender, StarBase attackerStarBase, StarBase defenderStarBase)
        {
            Attacker = attacker;
            Defender = defender;

            AttackerStarBase = attackerStarBase;
            AttackerFleet = attackerStarBase.AllShip;

            DefenderStarBase = defenderStarBase;
            DefenderFleet = defenderStarBase.AllShip;

            DefenderStarBase.UnderAttack = true;
        }

        public void Simulate()
        {
            if (IsOver) return;

            DayPassed++;
            Random random = new Random();

            //进攻方回合
            foreach (var ship in AttackerFleet)
            {
                //如果对方没有剩余舰船了就break
                if (DefenderFleet.Count <= 0)
                    break;

                //随机选择对方一个目标
                var target = DefenderFleet[random.Next(DefenderFleet.Count)];

                //计算是否命中
                //真实命中率 = 命中率 - (对方闪避率 - 索敌率)
                //如果索敌率大等于对方闪避率 则真实命中率 = 命中率
                float realAccuracy = ship.Accuracy -
                    (
                    ship.Tracking >= target.Evasion ?
                    0f :
                    target.Evasion - ship.Tracking
                    );

                if (random.NextSingle() > realAccuracy)
                {
                    //没中
                    continue;
                }
                else
                {
                    //噫 好了 我中了
                    //计算伤害
                    int damage = random.Next(ship.MinAttack, ship.MaxAttack);
                    //先减护盾
                    if (target.Shield > damage)
                        target.Shield -= damage;
                    else
                    {
                        //破盾
                        damage -= target.Shield;
                        if (target.Shield != 0) target.Shield = 0;

                        if (target.Health > damage)
                            target.Health -= damage;
                        else
                        {
                            //击毁
                            target.Health = 0;
                            DefenderLost.Add(target);
                            DefenderFleet.Remove(target);
                        }
                    }

                }
            }
            //计算防守方撤退
            DefenderRetreat = DefenderRetreat.Union(DefenderFleet.Where(x => x.Health <= x.MaxHealth * 0.2f)).ToList();
            DefenderFleet.RemoveAll(x => x.Health <= x.MaxHealth * 0.2f);

            
            //防守方回合
            //基地炮组
            if (AttackerFleet.Count > 0)
            {
                var target = AttackerFleet[random.Next(AttackerFleet.Count)];
                //计算是否命中
                //真实命中率 = 命中率 - (对方闪避率 - 索敌率)
                //如果索敌率大等于对方闪避率 则真实命中率 = 命中率
                
                //基地炮组命中率恒为60%
                float realAccuracy = 0.6f;

                if (random.NextSingle() > realAccuracy)
                {

                }
                else
                {
                    //噫 好了 我中了
                    //计算伤害
                    int damage = DefenderStarBase.DefensiveGunGroupAttack;



                    //先减护盾
                    if (target.Shield > damage)
                        target.Shield -= damage;
                    else
                    {
                        //破盾
                        damage -= target.Shield;
                        if (target.Shield != 0) target.Shield = 0;

                        if (target.Health > damage)
                            target.Health -= damage;
                        else
                        {
                            //击毁
                            target.Health = 0;
                            AttackerLost.Add(target);
                            AttackerFleet.Remove(target);
                        }
                    }

                }
            }
            foreach (var ship in DefenderFleet)
            {
                //如果对方没有剩余舰船了就break
                if (AttackerFleet.Count <= 0)
                    break;

                //随机选择对方一个目标
                var target = AttackerFleet[random.Next(AttackerFleet.Count)];

                //计算是否命中
                //真实命中率 = 命中率 - (对方闪避率 - 索敌率)
                //如果索敌率大等于对方闪避率 则真实命中率 = 命中率
                float realAccuracy = ship.Accuracy -
                    (
                    ship.Tracking >= target.Evasion ?
                    0f :
                    target.Evasion - ship.Tracking
                    );

                if (DefenderStarBase.Modules.Any(x => x.ID == "fire-control-computer"))
                    realAccuracy = Math.Clamp(ship.Accuracy * 1.2f, 0f, 1f);

                if (random.NextSingle() > realAccuracy)
                {
                    //没中
                    continue;
                }
                else
                {
                    //噫 好了 我中了
                    //计算伤害
                    int damage = random.Next(ship.MinAttack, ship.MaxAttack);


                    
                    //先减护盾
                    if (target.Shield > damage)
                        target.Shield -= damage;
                    else
                    {
                        //破盾
                        damage -= target.Shield;
                        if (target.Shield != 0) target.Shield = 0;

                        if (target.Health > damage)
                            target.Health -= damage;
                        else
                        {
                            //击毁
                            target.Health = 0;
                            AttackerLost.Add(target);
                            AttackerFleet.Remove(target);
                        }
                    }

                }
            }
            //计算进攻方撤退
            AttackerRetreat = AttackerRetreat.Union(AttackerFleet.Where(x => x.Health <= x.MaxHealth * 0.3f)).ToList();
            AttackerFleet.RemoveAll(x => x.Health <= x.MaxHealth * 0.2f);

            //判断一方是否兵力耗尽
            if (AttackerFleet.Count <= 0 || DefenderFleet.Count <= 0)
            {
                IsOver = true;
                DefenderStarBase.UnderAttack = false;

                //撤退的舰船归队
                AttackerRetreat.ForEach(x => AttackerFleet.Add(x));
                DefenderRetreat.ForEach(x => DefenderFleet.Add(x));

                AttackerStarBase.Flush();
                DefenderStarBase.Flush();


                if (AttackerFleet.Count > 0)
                {
                    IsSuccess = true;
                    MemoryRobbed = AttackerFleet.Count * 50000;
                }
            }
        }
    }
}
