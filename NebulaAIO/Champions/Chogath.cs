﻿using System;
using System.Collections.Generic;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using System.Threading.Tasks;
using System.Text;
using SharpDX;
using Color = System.Drawing.Color;
using EnsoulSharp.SDK.MenuUI;
using System.Reflection;
using System.Net.Http;
using System.Net;
using System.Text.RegularExpressions;

namespace NebulaAio.Champions
{

    public class Chogath
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuC;
        private static HitChance hitchance;

        public static void OnGameLoad()
        {
            if (ObjectManager.Player.CharacterName != "Chogath")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 950f);
            W = new Spell(SpellSlot.W, 650f);
            E = new Spell(SpellSlot.E, ObjectManager.Player.GetRealAutoAttackRange());
            R = new Spell(SpellSlot.R, 255f);

            Q.SetSkillshot(0.5f, 200f, float.MaxValue, false, SpellType.Circle);
            R.SetTargetted(0.25f, float.MaxValue);


            Config = new Menu("Chogath", "[Nebula]: Chogath", true);

            menuC = new Menu("Csettings", "Combo");
            menuC.Add(new MenuBool("UseQ", "Use Q in Combo"));
            menuC.Add(new MenuBool("UseW", "Use W in Combo"));
            menuC.Add(new MenuBool("UseE", "Use E in Combo"));
            menuC.Add(new MenuBool("UseR", "Use R in Combo"));
            menuC.Add(new MenuList("Pred", "Prediction Hitchance",
                new string[] {"Low", "Medium", "High", "Very High"}, 2));

            var menuL = new Menu("Clear", "Clear");
            menuL.Add(new MenuBool("LcQ", "Use Q in Lanclear"));
            menuL.Add(new MenuBool("LcW", "Use W in Lanclear"));
            menuL.Add(new MenuBool("JcQ", "Use Q in Jungleclear"));
            menuL.Add(new MenuBool("JcW", "Use W in Jungleclear"));
            menuL.Add(new MenuBool("JcE", "Use E in Jungleclear"));
            
            var menuK = new Menu("Killsteal", "Killsteal");
            menuK.Add(new MenuBool("KsQ", "Use Q to Killsteal"));
            menuK.Add(new MenuBool("KsW", "Use W to Killsteal"));
            menuK.Add(new MenuBool("KsR", "Use R to Killsteal"));

            var menuM = new Menu("Misc", "Misc");
            menuM.Add(new MenuSliderButton("Skin", "SkindID", 0, 0, 30, false));

            var menuD = new Menu("dsettings", "Drawings ");
            menuD.Add(new MenuBool("drawQ", "Q Range  (White)", true));
            menuD.Add(new MenuBool("drawW", "W Range  (White)", true));
            menuD.Add(new MenuBool("drawE", "E Range (White)", true));
            menuD.Add(new MenuBool("drawR", "R Range  (Red)", true));
            menuD.Add(new MenuBool("drawD", "Draw Combo Damage", true));



            Config.Add(menuC);
            Config.Add(menuL);
            Config.Add(menuK);
            Config.Add(menuM);
            Config.Add(menuD);

            Config.Attach();

            GameEvent.OnGameTick += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            Interrupter.OnInterrupterSpell += OnInterrupterSpell;
            AntiGapcloser.OnGapcloser += Gapcloser_OnGapcloser;
        }
        
        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        static void OnInterrupterSpell(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (args.DangerLevel <= Interrupter.DangerLevel.Low)
            {
                return;
            }

            if (sender.IsAlly)
            {
                return;
            }

            if (sender.IsEnemy && W.IsReady() && sender.Distance(GameObjects.Player) <= W.Range)
            {
                W.Cast(sender);
            }
            else
            {
                if (sender.IsEnemy && Q.IsReady() && sender.Distance(GameObjects.Player) <= Q.Range)
                {
                    Q.Cast(sender);
                }
            }
        }

        private static void Gapcloser_OnGapcloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (sender.IsAlly)
                return;
            
            if (args.SpellName == "ZedR")
                return;
            
            if(args.EndPosition.DistanceToPlayer() < args.StartPosition.DistanceToPlayer())
            {
                if(args.EndPosition.DistanceToPlayer() <= 300 && sender.IsValidTarget(950))
                {
                    if (Q.Cast(sender) == CastStates.SuccessfullyCasted)
                        return;
                }
                else
                {
                    return;
                }
            }
        }

        public static void OnGameUpdate(EventArgs args)
        {


            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicQ();
                LogicW();
                LogicE();
                LogicR();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                Laneclear();
                Jungle();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LastHit)
            {

            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.Harass)
            {

            }
            Killsteal();
            skind();
        }

        private static void skind()
        {
            if (Config["Misc"].GetValue<MenuSliderButton>("Skin").Enabled)
            {
                int skinnu = Config["Misc"].GetValue<MenuSliderButton>("Skin").Value;
                
                if (GameObjects.Player.SkinId != skinnu)
                    GameObjects.Player.SetSkin(skinnu);
            }
        }
        
        private static float ComboDamage(AIBaseClient enemy)
        {
            var damage = 0d;
            if (Q.IsReady())
                damage += ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.Q);
            if (W.IsReady())
                damage += ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.W);
            if (E.IsReady())
                damage += ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.E);

            return (float) damage;
        }

        private static void OnDraw(EventArgs args)
        {
            if (Config["dsettings"].GetValue<MenuBool>("drawQ").Enabled)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.White);
            }
            
            if (Config["dsettings"].GetValue<MenuBool>("drawW").Enabled)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.White);
            }

            if (Config["dsettings"].GetValue<MenuBool>("drawE").Enabled)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.White);
            }

            if (Config["dsettings"].GetValue<MenuBool>("drawR").Enabled)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, System.Drawing.Color.Red);
            }
            
            if (Config["dsettings"].GetValue<MenuBool>("drawD").Enabled)
            {
                foreach (
                    var enemyVisible in 
                    ObjectManager.Get<AIHeroClient>().Where(enemyVisible => enemyVisible.IsValidTarget()))
                {

                    if (ComboDamage(enemyVisible) > enemyVisible.Health)
                    {
                        Drawing.DrawText(Drawing.WorldToScreen(enemyVisible.Position)[0] + 50,
                            Drawing.WorldToScreen(enemyVisible.Position)[1] - 40, Color.Red,
                            "Combo=Kill");
                    }
                    else if (ComboDamage(enemyVisible) +
                        ObjectManager.Player.GetAutoAttackDamage(enemyVisible, true) * 2 > enemyVisible.Health)
                    {
                        Drawing.DrawText(Drawing.WorldToScreen(enemyVisible.Position)[0] + 50,
                            Drawing.WorldToScreen(enemyVisible.Position)[1] - 40, Color.Orange,
                            "Combo + 2 AA = Kill");
                    }
                    else
                        Drawing.DrawText(Drawing.WorldToScreen(enemyVisible.Position)[0] + 50,
                            Drawing.WorldToScreen(enemyVisible.Position)[1] - 40, Color.Green,
                            "Unkillable with combo + 2 AA");
                    
                }
            }
        }

        private static void LogicR()
        {
            var target = TargetSelector.GetTarget(1000);
            var useR = Config["Csettings"].GetValue<MenuBool>("UseR");
            if (target == null) return;
            
            if (R.IsReady() && useR.Enabled && target.Health < RDamage(target) && target.IsValidTarget(R.Range))
            {
                R.Cast(target);
            }
        }

        private static void LogicW()
        {
            var target = TargetSelector.GetTarget(W.Range);
            var useW = Config["Csettings"].GetValue<MenuBool>("UseW");
            var input = W.GetPrediction(target);
            if (target == null) return;
            
            switch (comb(menuC, "Pred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }

            if (W.IsReady() && useW.Enabled && input.Hitchance >= hitchance && target.IsValidTarget(W.Range))
            {
                W.Cast(input.CastPosition);
            }
        }

        private static void LogicE()
        {
            var target = TargetSelector.GetTarget(E.Range);
            var useE = Config["Csettings"].GetValue<MenuBool>("UseE");
            if (target == null) return;
            

            if (E.IsReady() && useE.Enabled &&  target.IsValidTarget(E.Range))
            {
                E.Cast();
            }
        }
        

        private static void LogicQ()
        {
            var target = TargetSelector.GetTarget(1000);
            var useQ = Config["Csettings"].GetValue<MenuBool>("UseQ");
            var input = Q.GetPrediction(target);
            if (target == null) return;
            
            switch (comb(menuC, "Pred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }

            if (Q.IsReady() && useQ.Enabled && input.Hitchance >= hitchance && target.IsValidTarget(Q.Range))
            {
                Q.Cast(input.CastPosition);
            }
        }

        private static void Jungle()
        {
            var JcEe = Config["Clear"].GetValue<MenuBool>("JcE");
            var JcWe = Config["Clear"].GetValue<MenuBool>("JcW");
            var JcQq = Config["Clear"].GetValue<MenuBool>("JcQ");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(E.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcEe.Enabled && E.IsReady() && ObjectManager.Player.Distance(mob.Position) < E.Range) E.Cast(mob);
                if (JcWe.Enabled && W.IsReady() && ObjectManager.Player.Distance(mob.Position) < W.Range) W.Cast(mob);
                if (JcQq.Enabled && Q.IsReady() &&  ObjectManager.Player.Distance(mob.Position) < Q.Range) Q.Cast(mob.Position);
            }
        }


        private static void Laneclear()
        {
            var lcq = Config["Clear"].GetValue<MenuBool>("LcQ");
            if (lcq.Enabled && Q.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                if (minions.Any())
                {
                    var qFarmLocation = Q.GetCircularFarmLocation(minions);
                    if (qFarmLocation.Position.IsValid())
                    {
                        Q.Cast(qFarmLocation.Position);
                        return;
                    }
                }
            }

            var lcw = Config["Clear"].GetValue<MenuBool>("LcW");
            if (lcw.Enabled && W.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                if (minions.Any())
                {
                    var wFarmLocation = W.GetLineFarmLocation(minions);
                    if (wFarmLocation.Position.IsValid())
                    {
                        W.Cast(wFarmLocation.Position);
                        return;
                    }
                }
            }
        }
        
        private static double RDamage(AIHeroClient target)
        {
            if (target == null || !target.IsValidTarget())
            {
                return 0;
            }

            var rLevel = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Level;
            if (rLevel <= 0)
            {
                return 0;
            }

            var baseDamage = new[] {0, 300, 475, 650}[rLevel];
            var extraDamage = new[] {0, 300, 475, 650}[rLevel] + (10 / 100 * GameObjects.Player.BonusHealth);
            var apdamage = new[] {0, 300, 475, 650}[rLevel] + 0.50f * ObjectManager.Player.TotalMagicalDamage;
            var resultDamage =
                ObjectManager.Player.CalculateDamage(target, DamageType.Magical, baseDamage + extraDamage + apdamage);
            if (ObjectManager.Player.HasBuff("SummonerExhaust"))
            {
                resultDamage = 0.6f;
            }

            return resultDamage;
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksW = Config["Killsteal"].GetValue<MenuBool>("KsW").Enabled;
            var ksR = Config["Killsteal"].GetValue<MenuBool>("KsR").Enabled;
            var target = TargetSelector.GetTarget(1000);

            if (target == null) return;
            if (target.IsInvulnerable) return;

            if (!(ObjectManager.Player.Distance(target.Position) <= Q.Range) ||
                !(ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q) >= target.Health + 20)) return;
            if (Q.IsReady() && ksQ) Q.Cast(target);
            
            if (!(ObjectManager.Player.Distance(target.Position) <= W.Range) ||
                !(ObjectManager.Player.GetSpellDamage(target, SpellSlot.W) >= target.Health + 20)) return;
            if (W.IsReady() && ksW) W.Cast(target);
            
            if (!(ObjectManager.Player.Distance(target.Position) <= R.Range) ||
                !(ObjectManager.Player.GetSpellDamage(target, SpellSlot.R) >= target.Health + 20)) return;
            if (R.IsReady() && ksR) R.Cast(target);
            
        }
    }
}