using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Security.Policy;

namespace NebulaAio.Champions
{

    public class Blitzcrank
    {
        private static Spell Q, W, E, R;
        private static Menu Config;

        public static void OnGameLoad()
        {
            if (ObjectManager.Player.CharacterName != "Blitzcrank")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 1150f);
            W = new Spell(SpellSlot.W, ObjectManager.Player.GetRealAutoAttackRange());
            E = new Spell(SpellSlot.E, ObjectManager.Player.GetRealAutoAttackRange());
            R = new Spell(SpellSlot.R, 600f);
            
            Q.SetSkillshot(0.25f, 140f, 1800f, true, SpellType.Line);


            Config = new Menu("Blitzcrank", "[Nebula]: Blitzcrank", true);

            var menuC = new Menu("Csettings", "Combo");
            menuC.Add(new MenuBool("UseQ", "Use Q in Combo"));
            menuC.Add(new MenuBool("UseW", "Use W in Combo"));
            menuC.Add(new MenuBool("UseE", "Use E in Combo"));
            menuC.Add(new MenuBool("UseR", "Use R in Combo"));
            menuC.Add(new MenuSlider("rcount", "Min enemys To use R", 2, 1, 5));

            var menuL = new Menu("Clear", "Clear");
            menuL.Add(new MenuBool("LcR", "Use R in Lanclear", false));
            menuL.Add(new MenuBool("JcQ", "Use Q in Jungleclear"));
            menuL.Add(new MenuBool("JcE", "Use E in Jungleclear"));
            
            var menuK = new Menu("Killsteal", "Killsteal");
            menuK.Add(new MenuBool("KsQ", "Use Q to Killsteal"));
            menuK.Add(new MenuBool("KsE", "Use E to Killsteal"));
            menuK.Add(new MenuBool("KsR", "Use R to Killsteal"));

            var menuM = new Menu("Misc", "Misc");
            menuM.Add(new MenuBool("Aq", "Auto Q on Stun or dashing targets"));

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
        }

        public static void OnGameUpdate(EventArgs args)
        {


            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicQ();
                LogicR();
                LogicE();
                LogicW();
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
            autoQ();
            autoQq();
            Killsteal();
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

        private static void autoQ()
        {
            var target = TargetSelector.GetTarget(1000);
            var autoq = Config["Misc"].GetValue<MenuBool>("Aq");
            if (target == null) return;
            
            if (!Q.IsReady() || !Q.IsInRange(target) || !autoq.Enabled || !target.IsValidTarget(Q.Range)) return;
            if (target.HasBuffOfType(BuffType.Stun) ||
                target.HasBuffOfType(BuffType.Snare) ||
                target.HasBuffOfType(BuffType.Knockup) ||
                target.HasBuffOfType(BuffType.Suppression) ||
                target.HasBuffOfType(BuffType.Charm) ||
                target.IsRecalling())
            {
                Q.Cast(target);
            }
        }

        private static void autoQq()
        {
            var target = TargetSelector.GetTarget(1000);
            var autoqq = Config["Misc"].GetValue<MenuBool>("Aq");
            if (target == null) return;

            if (Q.IsReady() && target.IsValidTarget(Q.Range) && autoqq.Enabled)
            {
                var pred = Q.GetPrediction(target);
                if (pred.Hitchance == HitChance.Dash)
                {
                    Q.Cast(pred.CastPosition);
                }
            }
        }
        
        private static int GetHitByR(AIBaseClient target) // Credits to Trelli For helping me with this one!
        {
            int totalHit = 0;
            foreach (AIHeroClient current in ObjectManager.Get<AIHeroClient>())
            {
                if (current.IsEnemy && Vector3.Distance(ObjectManager.Player.Position, current.Position) <= R.Range)
                {
                    totalHit = totalHit + 1;
                }
            }
            return totalHit;
        }

        private static void LogicR()
        {
            var target = TargetSelector.GetTarget(1000);
            var useR = Config["Csettings"].GetValue<MenuBool>("UseR");
            var combor = Config["Csettings"].GetValue<MenuSlider>("rcount").Value;
            if (target == null) return;
            
            if (R.IsReady() && useR.Enabled && GetHitByR(target) >= combor)
            {
                R.Cast();
            }
            
            if (R.IsReady() && useR.Enabled && ObjectManager.Player.GetSpellDamage(target, SpellSlot.R) > target.Health)
            {
                R.Cast();
            }
        }

        private static void LogicW()
        {
            var target = TargetSelector.GetTarget(W.Range);
            var useW = Config["Csettings"].GetValue<MenuBool>("UseW");

            if (W.IsReady() && useW.Enabled && target.IsValidTarget(Q.Range))
            {
                W.Cast();
            }
        }

        private static void LogicE()
        {
            var target = TargetSelector.GetTarget(E.Range);
            var useE = Config["Csettings"].GetValue<MenuBool>("UseE");
            if (target == null) return;

            if (E.IsReady() && useE.Enabled && target.IsValidTarget(E.Range))
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

            if (Q.IsReady() && useQ.Enabled && target.IsValidTarget(Q.Range))
            {
                if (input.Hitchance >= HitChance.High)
                {
                    Q.Cast(input.UnitPosition);
                }
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
                if (JcEe.Enabled && E.IsReady() && ObjectManager.Player.Distance(mob.Position) < E.Range) E.Cast();
                if (JcQq.Enabled && Q.IsReady() && ObjectManager.Player.Distance(mob.Position) < Q.Range) Q.Cast(mob);
            }
        }


        private static void Laneclear()
        {
            var lcr = Config["Clear"].GetValue<MenuBool>("LcR");
            if (lcr.Enabled && R.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(R.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                if (minions.Any())
                {
                    var rFarmLocation = R.GetCircularFarmLocation(minions);
                    if (rFarmLocation.Position.IsValid())
                    {
                        R.Cast(rFarmLocation.Position);
                        return;
                    }
                }
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksR = Config["Killsteal"].GetValue<MenuBool>("KsR").Enabled;
            var ksE = Config["Killsteal"].GetValue<MenuBool>("KsE").Enabled;
            var target = TargetSelector.GetTarget(Q.Range);
            var targets = TargetSelector.GetTarget(W.Range);
            var targetss = TargetSelector.GetTarget(E.Range);

            if (target == null) return;
            if (target.IsInvulnerable) return;
            if (targets == null) return;
            if (targets.IsInvulnerable) return;
            if (targetss == null) return;
            if (targetss.IsInvulnerable) return;

            if (!(ObjectManager.Player.Distance(target.Position) <= Q.Range) ||
                !(ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q) >= target.Health + 20)) return;
            if (Q.IsReady() && ksQ) Q.Cast(target);
            
            if (!(ObjectManager.Player.Distance(target.Position) <= R.Range) ||
                !(ObjectManager.Player.GetSpellDamage(target, SpellSlot.R) >= target.Health + 20)) return;
            if (R.IsReady() && ksR) R.Cast(target);
            
            if (!(ObjectManager.Player.Distance(target.Position) <= E.Range) ||
                !(ObjectManager.Player.GetSpellDamage(target, SpellSlot.E) >= target.Health + 20)) return;
            if (E.IsReady() && ksE) E.Cast(target);
            
        }
    }
}