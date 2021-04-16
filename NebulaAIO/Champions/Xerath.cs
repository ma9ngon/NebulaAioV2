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

    public class Xerath
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuC;
        private static bool CheckTarget = false;
        private static SpellSlot igniteslot;
        private static HitChance hitchance;

        public static void OnGameLoad()
        {
            if (ObjectManager.Player.CharacterName != "Xerath")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 750f);
            W = new Spell(SpellSlot.W, 1000f);
            E = new Spell(SpellSlot.E, 1050f);
            R = new Spell(SpellSlot.R, 5000f);

            Q.SetSkillshot(0.6f, 65f, 20000f, false, SpellType.Line);
            W.SetSkillshot(0.75f, 125f, float.MaxValue, false, SpellType.Circle);
            E.SetSkillshot(0.25f, 60f, 1400f, true, SpellType.Line);
            R.SetSkillshot(0.627f, 68f, 20000, false, SpellType.Circle);

            Q.SetCharged("XerathArcanopulseChargeUp", "XerathArcanopulseChargeUp", 750, 1550, 1.5f);
            
            igniteslot = ObjectManager.Player.GetSpellSlot("SummonerDot");

            Config = new Menu("Xerath", "[Nebula]: Xerath", true);

            menuC = new Menu("Csettings", "Combo");
            menuC.Add(new MenuBool("UseQ", "Use Q in Combo"));
            menuC.Add(new MenuBool("UseW", "Use W in Combo"));
            menuC.Add(new MenuBool("UseE", "Use E in Combo"));
            menuC.Add(new MenuBool("UseR", "Use R in Combo"));
            menuC.Add(new MenuList("Pred", "Prediction Hitchance",
                new string[] {"Low", "Medium", "High", "Very High"}, 2));

            var menuL = new Menu("Clear", "Clear");
            menuL.Add(new MenuBool("LcQ", "Use Q in Laneclear"));
            menuL.Add(new MenuBool("LcW", "Use W in Laneclear"));
            menuL.Add(new MenuBool("JcQ", "Use Q in Jungleclear"));
            menuL.Add(new MenuBool("JcW", "Use W in Jungleclear"));
            menuL.Add(new MenuBool("JcE", "Use E in Jungleclear"));

            var menuK = new Menu("Killsteal", "Killsteal");
            menuK.Add(new MenuBool("KsW", "Use W to Killsteal"));
            menuK.Add(new MenuBool("KsE", "Use E to Killsteal"));

            var menuM = new Menu("Misc", "Misc");
            menuM.Add(new MenuKeyBind("Rkey", "R Key", Keys.T, KeyBindType.Press));
            menuM.Add(new MenuSlider("Cusor", "Cusor Range", 400, 0, 2000));
            menuM.Add(new MenuBool("Qslow", "Q Slow Pred", true));
            menuM.Add(new MenuSliderButton("Skin", "SkindID", 0, 0, 30, false));

            var menuD = new Menu("dsettings", "Drawings ");
            menuD.Add(new MenuBool("drawQ", "Q Range  (White)", true));
            menuD.Add(new MenuBool("drawW", "W Range (White)", true));
            menuD.Add(new MenuBool("drawE", "E Range (White)", true));
            menuD.Add(new MenuBool("drawR", "R Range  (Red)", true));
            menuD.Add(new MenuBool("drawCusor", "R Cusor Range  (Red)", true));
            menuD.Add(new MenuBool("drawD", "Draw Combo Damage", true));
            
            Config.Add(menuC);
            Config.Add(menuL);
            Config.Add(menuK);
            Config.Add(menuM);
            Config.Add(menuD);

            Config.Attach();

            GameEvent.OnGameTick += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            AntiGapcloser.OnGapcloser += Gapcloser_OnGapcloser;
            Game.OnUpdate += Check;
        }
        
        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        public static void OnGameUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead)
                return;
            
            switch (comb(menuC, "Pred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }

            if (ObjectManager.Player.HasBuff("XerathLocusOfPower2"))
            {
                Orbwalker.AttackEnabled = false;
                Orbwalker.MoveEnabled = false;


                if (Config["Misc"].GetValue<MenuKeyBind>("Rkey").Active)
                {
                    var targets = GameObjects.EnemyHeroes.Where(i =>
                            i.Distance(Game.CursorPos) <= Config["Misc"].GetValue<MenuSlider>("Cusor").Value &&
                            !i.IsDead)
                        .OrderBy(i => i.Health);

                    if (targets != null)
                    {
                        CheckTarget = false;

                        if (targets != null)
                        {
                            var target = targets.Find(i =>
                                i.DistanceToCursor() <= Config["Misc"].GetValue<MenuSlider>("Cusor").Value);
                            var input = R.GetPrediction(target);

                            if (target != null && input.Hitchance >= hitchance && !target.IsInvulnerable)
                            {
                                R.Cast(input.CastPosition);
                                return;
                            }
                            else
                                return;
                        }
                    }
                    else
                    {
                        CheckTarget = true;
                    }
                }
            }
            else
            {
                CheckTarget = false;
            }

            if (Q.IsCharging)
                Orbwalker.AttackEnabled = false;
            
            if (Q.IsChargedSpell)
                Orbwalker.AttackEnabled = true;
            

            Orbwalker.MoveEnabled = true;

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicW();
                LogicE();
                LogicQ();
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
        
        private static void Gapcloser_OnGapcloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (sender.IsAlly)
                return;

            if (args.SpellName == "ZedR")
                return;

            if (args.EndPosition.DistanceToPlayer() < args.StartPosition.DistanceToPlayer())
            {
                if(args.EndPosition.DistanceToPlayer() <= 300 && sender.IsValidTarget(E.Range))
                {
                    if (E.Cast(sender) == CastStates.SuccessfullyCasted)
                        return;
                }
                else
                {
                    return;
                }
            }
        }

        private static void Check(EventArgs args)
        {
            if (ObjectManager.Player.IsDead)
                return;

            if (ObjectManager.Player.HasBuff("XerathArcanopulseChargeUp"))
                ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
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

            if (Config["dsettings"].GetValue<MenuBool>("drawCusor").Enabled)
            {
                Render.Circle.DrawCircle(Game.CursorPos, Config["Misc"].GetValue<MenuSlider>("Cusor").Value,
                    CheckTarget ? System.Drawing.Color.Red : System.Drawing.Color.Green);
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

        private static void LogicQ()
        {
            var target = TargetSelector.GetTarget(Q.Range);
            var useQ = Config["Csettings"].GetValue<MenuBool>("UseQ").Enabled;
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

            if (Q.IsReady() && useQ && target.IsValidTarget(Q.Range) && input.Hitchance >= hitchance)
            {
                Q.StartCharging();
                {
                    if (Q.IsChargedSpell && input.Hitchance >= hitchance)
                    {
                        Q.Cast(input.CastPosition);
                    }
                }
            }
        }

        private static void LogicW()
        {
            var target = TargetSelector.GetTarget(W.Range);
            var useW = Config["Csettings"].GetValue<MenuBool>("UseW").Enabled;
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

            if (W.IsReady() && useW && target.IsValidTarget(W.Range) && input.Hitchance >= hitchance)
            {
                W.Cast(input.CastPosition);
            }
        }

        private static void LogicE()
        {
            var target = TargetSelector.GetTarget(E.Range);
            var useE = Config["Csettings"].GetValue<MenuBool>("UseE").Enabled;
            var input = E.GetPrediction(target);
            if (target == null) return;
            
            switch (comb(menuC, "Pred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }
            
            if (E.IsReady() && useE && target.IsValidTarget(E.Range) && !target.HasBuffOfType(BuffType.SpellShield) &&
                input.Hitchance >= hitchance)
            {
                E.Cast(input.CastPosition);
            }
        }

        private static void Jungle()
        {
            var JcEe = Config["Clear"].GetValue<MenuBool>("JcE");
            var JcQq = Config["Clear"].GetValue<MenuBool>("JcQ");
            var JcWw = Config["Clear"].GetValue<MenuBool>("JcW");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(E.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcEe.Enabled && E.IsReady() && ObjectManager.Player.Distance(mob.Position) < E.Range) E.Cast(mob);
                if (JcQq.Enabled && Q.IsReady() && ObjectManager.Player.Distance(mob.Position) < Q.Range)
                    Q.Cast(mob);
                if (JcWw.Enabled && W.IsReady() && ObjectManager.Player.Distance(mob.Position) < W.Range)
                    W.Cast(mob.Position);
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
                    var qFarmLocation = Q.GetLineFarmLocation(minions);
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
                    var wFarmLocation = W.GetCircularFarmLocation(minions);
                    if (wFarmLocation.Position.IsValid())
                    {
                        W.Cast(wFarmLocation.Position);
                        return;
                    }
                }
            }
        }

        private static void Killsteal()
        {
            var ksW = Config["Killsteal"].GetValue<MenuBool>("KsW").Enabled;
            var ksE = Config["Killsteal"].GetValue<MenuBool>("KsE").Enabled;
            var target = TargetSelector.GetTarget(W.Range);
            var targetss = TargetSelector.GetTarget(E.Range);

            if (target == null) return;
            if (target.IsInvulnerable) return;
            if (targetss == null) return;
            if (targetss.IsInvulnerable) return;

            if (!(ObjectManager.Player.Distance(target.Position) <= W.Range) ||
                !(ObjectManager.Player.GetSpellDamage(target, SpellSlot.W) >= target.Health + 20)) return;
            if (W.IsReady() && ksW) W.Cast(target);

            if (!(ObjectManager.Player.Distance(target.Position) <= E.Range) ||
                !(ObjectManager.Player.GetSpellDamage(target, SpellSlot.E) >= target.Health + 20)) return;
            if (E.IsReady() && ksE) E.Cast(target);

        }
    }
}