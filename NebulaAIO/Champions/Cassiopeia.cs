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

    public class Cassiopeia
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuC;
        private static SpellSlot igniteslot;
        private static HitChance hitchance;

        public static void OnGameLoad()
        {
            if (ObjectManager.Player.CharacterName != "Cassiopeia")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 850f);
            W = new Spell(SpellSlot.W, 700f);
            E = new Spell(SpellSlot.E, 700f);
            R = new Spell(SpellSlot.R, 825);

            Q.SetSkillshot(0.25f, 75f, float.MaxValue, false, SpellType.Circle);
            W.SetSkillshot(0.25f, 160, float.MaxValue, false, SpellType.Circle);
            R.SetSkillshot(0.5f, (float)(80 * Math.PI / 180), float.MaxValue, false, SpellType.Cone);
            
            E.SetTargetted(0.25f, float.MaxValue);
            
            igniteslot = ObjectManager.Player.GetSpellSlot("SummonerDot");


            Config = new Menu("Cassiopeia", "[Nebula]: Cassiopeia", true);

            menuC = new Menu("Csettings", "Combo");
            menuC.Add(new MenuBool("UseQ", "Use Q in Combo"));
            menuC.Add(new MenuBool("UseW", "Use W in Combo"));
            menuC.Add(new MenuBool("UseE", "Use E in Combo"));
            menuC.Add(new MenuBool("UseR", "Use R in Combo"));
            menuC.Add(new MenuList("Pred", "Prediction Hitchance",
                new string[] {"Low", "Medium", "High", "Very High"}, 2));

            var menuL = new Menu("Clear", "Clear");
            menuL.Add(new MenuBool("LcQ", "Use Q in Lanclear"));
            menuL.Add(new MenuBool("JcQ", "Use Q in Jungleclear"));
            menuL.Add(new MenuBool("JcE", "Use E in Jungleclear"));
            
            var menuK = new Menu("Killsteal", "Killsteal");
            menuK.Add(new MenuBool("KsQ", "Use Q to Killsteal"));
            menuK.Add(new MenuBool("KsE", "Use E to Killsteal"));

            var menuM = new Menu("Misc", "Misc");
            menuM.Add(new MenuBool("Epoison", "E only When Enemy is Poisoned"));
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
        }
        
        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        public static void OnGameUpdate(EventArgs args)
        {


            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicQ();
                LogicE();
                LogicW();
                LogicR();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                Laneclear();
                Jungle();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LastHit)
            {
                LastHit();
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
            if (igniteslot != SpellSlot.Unknown &&
                ObjectManager.Player.Spellbook.CanUseSpell(igniteslot) == SpellState.Ready)
                damage += ObjectManager.Player.GetSummonerSpellDamage(enemy, SummonerSpell.Ignite);
            if (Q.IsReady())
                damage += ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.Q);
            if (W.IsReady())
                damage += ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.W);
            if (E.IsReady())
                damage += ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.E);
            if (R.IsReady())
                damage += ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.R);

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
            var input = R.GetPrediction(target);
            if (target == null) return;

            if (target.HasBuffOfType(BuffType.Invulnerability)
                || target.HasBuffOfType(BuffType.SpellImmunity)
                || target.HasBuffOfType(BuffType.SpellShield))
            {
                return;
            }
            
            switch (comb(menuC, "Pred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }

            if (R.IsReady() && useR.Enabled && target.IsBothFacing(GameObjects.Player) && GameObjects.Player.CountEnemyHeroesInRange(400f) > 1 && input.Hitchance >= hitchance)
            {
                R.Cast(input.CastPosition);
            }
            
            if (R.IsReady() && useR.Enabled && R.GetDamage(target) + Q.GetDamage(target) + E.GetDamage(target) + W.GetDamage(target) >= target.Health && input.Hitchance >= hitchance)
            {
                R.Cast(input.CastPosition);
            }
        }

        private static void LogicW()
        {
            var target = TargetSelector.GetTarget(W.Range);
            var useW = Config["Csettings"].GetValue<MenuBool>("UseW");
            if (target == null) return;

            if (W.IsReady() && useW.Enabled && target.IsValidTarget(W.Range))
            {
                W.Cast(target);
            }
        }

        private static void LogicE()
        {
            var target = TargetSelector.GetTarget(E.Range);
            var useE = Config["Csettings"].GetValue<MenuBool>("UseE");
            var Epos = Config["Misc"].GetValue<MenuBool>("Epoison");
            if (target == null) return;

            if (E.IsReady() && useE.Enabled && !Epos.Enabled && target.IsValidTarget(E.Range))
            {
                E.Cast(target);
            }
            else if (E.IsReady() && useE.Enabled && Epos.Enabled && target.HasBuff("cassiopeiaqdebuff") &&
                     target.IsValidTarget(E.Range))
            {
                E.Cast(target);
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
            

            if (Q.IsReady() && useQ.Enabled && input.Hitchance >= hitchance && !target.HasBuffOfType(BuffType.Poison) && target.IsValidTarget(Q.Range))
            {
                Q.Cast(input.CastPosition);
            }
        }

        private static void Jungle()
        {
            var JcEe = Config["Clear"].GetValue<MenuBool>("JcE");
            var JcQq = Config["Clear"].GetValue<MenuBool>("JcQ");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(E.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcEe.Enabled && E.IsReady() && ObjectManager.Player.Distance(mob.Position) < E.Range) E.Cast(mob);
                if (JcQq.Enabled && Q.IsReady() &&  ObjectManager.Player.Distance(mob.Position) < Q.Range) Q.Cast(mob);
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
        }

        private static void LastHit()
        {
            var allMinions = GameObjects.EnemyMinions.Where(x => x.IsMinion() && !x.IsDead)
                .OrderBy(x => x.Distance(ObjectManager.Player.Position));

            foreach (var min in allMinions.Where(x => x.IsValidTarget(E.Range) && x.Health < E.GetDamage(x)))
            {
                Orbwalker.ForceTarget = min;
                E.Cast(min);
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksE = Config["Killsteal"].GetValue<MenuBool>("KsE").Enabled;
            var target = TargetSelector.GetTarget(Q.Range);
            var targetss = TargetSelector.GetTarget(E.Range);

            if (target == null) return;
            if (target.IsInvulnerable) return;
            if (targetss == null) return;
            if (targetss.IsInvulnerable) return;

            if (!(ObjectManager.Player.Distance(target.Position) <= Q.Range) ||
                !(ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q) >= target.Health + 20)) return;
            if (Q.IsReady() && ksQ) Q.Cast(target);

            if (!(ObjectManager.Player.Distance(target.Position) <= E.Range) ||
                !(ObjectManager.Player.GetSpellDamage(target, SpellSlot.E) >= target.Health + 20)) return;
            if (E.IsReady() && ksE) E.Cast(target);
            
        }
    }
}