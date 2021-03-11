using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Policy;

namespace NebulaAio.Champions
{

    public class Swain
    {
        private static Spell Q, W, E, R;
        private static Menu Config;

        public static void OnGameLoad()
        {
            if (ObjectManager.Player.CharacterName != "Swain")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 725f);
            W = new Spell(SpellSlot.W, 5500);
            E = new Spell(SpellSlot.E, 850f);
            R = new Spell(SpellSlot.R, 650f);
            
            Q.SetSkillshot(0.25f, 0f, float.MaxValue, false, SpellType.Arc);
            W.SetSkillshot(0.25f, 325f, float.MaxValue, false, SpellType.Circle);
            E.SetSkillshot(0.25f, 100f, float.MaxValue, true, SpellType.Line);


            Config = new Menu("Swain", "[Nebula]: swain", true);

            var menuC = new Menu("Csettings", "Combo");
            menuC.Add(new MenuBool("UseQ", "Use Q in Combo"));
            menuC.Add(new MenuBool("UseW", "Use W in Combo"));
            menuC.Add(new MenuBool("UseE", "Use E in Combo"));
            menuC.Add(new MenuBool("UseR", "Use R in Combo", false));
            menuC.Add(new MenuSlider("rcount", "Min enemys To use R", 2, 1, 5));

            var menuL = new Menu("Clear", "Clear");
            menuL.Add(new MenuBool("LcQ", "Use Q in Lanclear"));
            menuL.Add(new MenuBool("LcW", "Use W in Lanclear"));
            menuL.Add(new MenuBool("JcQ", "Use Q in Jungleclear"));
            menuL.Add(new MenuBool("JcW", "Use W in Jungleclear"));
            
            var menuK = new Menu("Killsteal", "Killsteal");
            menuK.Add(new MenuBool("KsQ", "Use Q to Killsteal"));
            menuK.Add(new MenuBool("KsE", "Use E to Killsteal"));

            var menuM = new Menu("Misc", "Misc");
            menuM.Add(new MenuSliderButton("Skin", "SkindID", 0, 0, 30, false));

            var menuD = new Menu("dsettings", "Drawings ");
            menuD.Add(new MenuBool("drawQ", "Q Range  (White)", true));
            menuD.Add(new MenuBool("drawW", "W Range  (White)", true));
            menuD.Add(new MenuBool("drawE", "E Range (White)", true));
            menuD.Add(new MenuBool("drawR", "R Range  (Red)", true));



            Config.Add(menuC);
            Config.Add(menuL);
            Config.Add(menuK);
            Config.Add(menuM);
            Config.Add(menuD);

            Config.Attach();

            GameEvent.OnGameTick += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            AntiGapcloser.OnGapcloser += Gapcloser_OnGapcloser;
        }

        public static void OnGameUpdate(EventArgs args)
        {

            if (W.Level > 0)
            {
                W.Range = 5500 + 500 * (ObjectManager.Player.Level -1);
            }


            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicR();
                LogicE();
                LogicW();
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
        
        private static void skind()
        {
            if (Config["Misc"].GetValue<MenuSliderButton>("Skin").Enabled)
            {
                int skinnu = Config["Misc"].GetValue<MenuSliderButton>("Skin").Value;
                
                if (GameObjects.Player.SkinId != skinnu)
                    GameObjects.Player.SetSkin(skinnu);
            }
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

        private static void LogicR()
        {
            var target = TargetSelector.GetTarget(1000);
            var useR = Config["Csettings"].GetValue<MenuBool>("UseR");
            var combor = Config["Csettings"].GetValue<MenuSlider>("rcount").Value;
            if (target == null) return;

            if (R.IsReady() && useR.Enabled && !ObjectManager.Player.HasBuff("SwainR") && ObjectManager.Player.CountEnemyHeroesInRange(R.Range) >= combor)
            {
                R.Cast();
            }
            else if (R.IsReady() && useR.Enabled && ObjectManager.Player.HasBuff("SwainR") &&
                     ObjectManager.Player.CountEnemyHeroesInRange(R.Range) >= combor &&
                     R.GetDamage(target) + Q.GetDamage(target) >= target.Health)
            {
                R.Cast();
            }
        }

        private static void LogicW()
        {
            var target = TargetSelector.GetTarget(W.Range);
            var useW = Config["Csettings"].GetValue<MenuBool>("UseW");
            var input = W.GetPrediction(target);
            if (target == null) return;

            if (W.IsReady() && useW.Enabled && input.Hitchance >= HitChance.High && target.IsValidTarget(W.Range))
            {
                W.Cast(input.CastPosition);
            }
        }

        private static void LogicE()
        {
            var target = TargetSelector.GetTarget(E.Range);
            var useE = Config["Csettings"].GetValue<MenuBool>("UseE");
            var input = E.GetPrediction(target);
            if (target == null) return;

            if (E.IsReady() && useE.Enabled && !target.HasBuffOfType(BuffType.SpellShield) && input.Hitchance >= HitChance.High && target.IsValidTarget(E.Range))
            {
                E.Cast(input.CastPosition);
            }
        }
        

        private static void LogicQ()
        {
            var target = TargetSelector.GetTarget(1000);
            var useQ = Config["Csettings"].GetValue<MenuBool>("UseQ");
            if (target == null) return;

            if (Q.IsReady() && useQ.Enabled && target.IsValidTarget(Q.Range))
            {
                Q.Cast(target);
            }
        }

        private static void Jungle()
        {
            var JcWe = Config["Clear"].GetValue<MenuBool>("JcW");
            var JcQq = Config["Clear"].GetValue<MenuBool>("JcQ");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(E.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcWe.Enabled && W.IsReady() && ObjectManager.Player.Distance(mob.Position) < W.Range) W.Cast(mob);
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
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksE = Config["Killsteal"].GetValue<MenuBool>("KsE").Enabled;
            var target = TargetSelector.GetTarget(1000);

            if (target == null) return;
            if (target.IsInvulnerable) return;

            if (!(ObjectManager.Player.Distance(target.Position) <= Q.Range) ||
                !(ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q) >= target.Health + 20)) return;
            if (Q.IsReady() && ksQ) Q.Cast(target);

            if (!(ObjectManager.Player.Distance(target.Position) <= E.Range) ||
                !(ObjectManager.Player.GetSpellDamage(target, SpellSlot.E) >= target.Health + 20)) return;
            if (E.IsReady() && ksE) E.Cast(target);
            
        }
    }
}