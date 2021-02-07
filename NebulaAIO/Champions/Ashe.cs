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

    public class Ashe
    {
        private static Spell Q, W, E, R;
        private static Menu Config;

        public static void OnGameLoad()
        {
            if (ObjectManager.Player.CharacterName != "Ashe")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, ObjectManager.Player.GetRealAutoAttackRange());
            W = new Spell(SpellSlot.W, 1200f);
            E = new Spell(SpellSlot.E, 25000f);
            R = new Spell(SpellSlot.R, 25000f);
            
            W.SetSkillshot(0.25f, 0f, 2000f, true, SpellType.Cone);
            E.SetSkillshot(0.25f, 300f, 1400f, false, SpellType.Line);
            R.SetSkillshot(0.25f, 130f, 1600f, false, SpellType.Line);


            Config = new Menu("Ashe", "[Nebula]: Ashe", true);

            var menuC = new Menu("Csettings", "Combo");
            menuC.Add(new MenuBool("UseQ", "Use Q in Combo"));
            menuC.Add(new MenuBool("UseW", "Use W in Combo"));
            menuC.Add(new MenuBool("UseE", "Use E in Combo"));
            menuC.Add(new MenuBool("UseR", "Use R in Combo"));

            var menuL = new Menu("Clear", "Clear");
            menuL.Add(new MenuBool("LcW", "Use W in Lanclear"));
            menuL.Add(new MenuBool("JcQ", "Use Q in Jungleclear"));
            menuL.Add(new MenuBool("JcW", "Use W in Jungleclear"));
            
            var menuK = new Menu("Killsteal", "Killsteal");
            menuK.Add(new MenuBool("KsW", "Use W to Killsteal"));
            menuK.Add(new MenuBool("KsR", "Use R to Killsteal", false));

            var menuH = new Menu("skillpred", "SkillShot HitChance ");
            menuH.Add(new MenuList("rchance", "R HitChance:", new[] { "Low", "Medium", "High", }, 2));

            var menuM = new Menu("Misc", "Misc");
            menuM.Add(new MenuBool("Agc", "R Antigapcloser"));
            menuM.Add(new MenuBool("inter", "R interrupt"));
            menuM.Add(new MenuKeyBind("semiR", "Semi R", Keys.T, KeyBindType.Press));
            menuM.Add(new MenuSlider("Rrange", "R Range Slider", 2500, 0, 25000));

            var menuD = new Menu("dsettings", "Drawings ");
            menuD.Add(new MenuBool("drawQ", "Q Range  (White)", true));
            menuD.Add(new MenuBool("drawW", "W Range  (White)", true));
            menuD.Add(new MenuBool("drawE", "E Range (White)", true));
            menuD.Add(new MenuBool("drawR", "R Range  (Red)", true));



            Config.Add(menuC);
            Config.Add(menuL);
            Config.Add(menuK);
            Config.Add(menuM);
            Config.Add(menuH);
            Config.Add(menuD);

            Config.Attach();

            GameEvent.OnGameTick += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            AntiGapcloser.OnGapcloser += OnGapcloser;
            Interrupter.OnInterrupterSpell += OnInterruptible;
        }

        public static void OnGameUpdate(EventArgs args)
        {
            
            if (ObjectManager.Player.IsDead || ObjectManager.Player.IsRecalling())
            {
                return;
            }

            if (Config["Misc"].GetValue<MenuKeyBind>("semiR").Active)
            {
                SemiR();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicQ();
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

            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.Harass)
            {

            }
            LogicE();
            Killsteal();
        }

        private static void SemiR()
        {
            var target = TargetSelector.GetTarget(1500);
            if (target == null || !target.IsValidTarget(1500)) return;
            var rPred = R.GetPrediction(target);
            if (rPred.Hitchance >= HitChance.High) R.Cast(rPred.CastPosition);
        }

        private static void OnInterruptible(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (!Config["Misc"].GetValue<MenuBool>("inter").Enabled) return;

            if (R.IsReady() && sender != null && sender.IsValidTarget(1500))
            {
                var pred = R.GetPrediction(sender);

                if (pred != null && pred.Hitchance >= HitChance.High) R.Cast(pred.UnitPosition);
            }
        }
        
        private static void OnGapcloser(AIBaseClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (Config["Misc"].GetValue<MenuBool>("Agc").Enabled && R.IsReady() && sender.IsEnemy)
            {
                if (sender.IsValidTarget(R.Range))
                {
                    R.Cast(sender);
                }
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
                Render.Circle.DrawCircle(ObjectManager.Player.Position, Config["Misc"].GetValue<MenuSlider>("Rrange").Value, System.Drawing.Color.Red);
            }
        }

        private static void LogicR()
        {
            var target = TargetSelector.GetTarget(1000);
            var useR = Config["Csettings"].GetValue<MenuBool>("UseR");
            var input = R.GetPrediction(target);
            var rFarmSet = Config["skillpred"].GetValue<MenuList>("rchance").SelectedValue;
            if (target == null) return;
            
            string final = rFarmSet;
            var skill = HitChance.High;
            
            if (final == "0") {
                skill = HitChance.Low;
            }

            if (final == "1") {
                skill = HitChance.Medium;
            }

            if (final == "2") {
                skill = HitChance.High;
            }

            if (R.IsReady() && input.Hitchance >= skill && useR.Enabled && ObjectManager.Player.GetSpellDamage(target, SpellSlot.R) > target.Health && target.IsValidTarget(Config["Misc"].GetValue<MenuSlider>("Rrange").Value))
            {
                R.Cast(input.UnitPosition);
            }
            
            if (R.IsReady() && useR.Enabled && input.Hitchance >= skill && R.GetDamage(target) + W.GetDamage(target) >= target.Health && target.IsValidTarget(Config["Misc"].GetValue<MenuSlider>("Rrange").Value))
            {
                R.Cast(input.UnitPosition);
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
            var targe = E.GetPrediction(target);
            if (target == null) return;
            
            if (E.IsReady() && useE.Enabled && target.IsValidTarget(W.Range))
            {
                if (NavMesh.GetCollisionFlags(targe.CastPosition) == CollisionFlags.Grass)
                {
                    E.Cast(targe.CastPosition);
                }
            }
        }
        

        private static void LogicQ()
        {
            var target = TargetSelector.GetTarget(1000);
            var useQ = Config["Csettings"].GetValue<MenuBool>("UseQ");
            if (target == null) return;

            if (Q.IsReady() && useQ.Enabled && target.IsValidTarget(ObjectManager.Player.GetRealAutoAttackRange()))
            {
                Q.Cast();
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
                if (JcQq.Enabled && Q.IsReady() &&  ObjectManager.Player.Distance(mob.Position) < Q.Range) Q.Cast();
            }
        }


        private static void Laneclear()
        {
            var lcw = Config["Clear"].GetValue<MenuBool>("LcW");
            if (lcw.Enabled && W.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(W.Range) && x.IsMinion())
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

        private static void Killsteal()
        {
            var ksR = Config["Killsteal"].GetValue<MenuBool>("KsR").Enabled;
            var ksW = Config["Killsteal"].GetValue<MenuBool>("KsW").Enabled;
            var target = TargetSelector.GetTarget(R.Range);
            var targets = TargetSelector.GetTarget(W.Range);

            if (target == null) return;
            if (target.IsInvulnerable) return;
            if (targets == null) return;
            if (targets.IsInvulnerable) return;

            if (!(ObjectManager.Player.Distance(target.Position) <= R.Range) ||
                !(ObjectManager.Player.GetSpellDamage(target, SpellSlot.R) >= target.Health + 20)) return;
            if (R.IsReady() && ksR) R.Cast(target);
            
            if (!(ObjectManager.Player.Distance(target.Position) <= W.Range) ||
                !(ObjectManager.Player.GetSpellDamage(target, SpellSlot.W) >= target.Health + 20)) return;
            if (W.IsReady() && ksW) W.Cast(target);

        }
    }
}