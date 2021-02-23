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

    public class Viego
    {
        private static Spell Q, W, E, R;
        private static Menu Config;

        public static void OnGameLoad()
        {
            if (ObjectManager.Player.CharacterName != "Viego")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 600f);
            W = new Spell(SpellSlot.W, 300f);
            E = new Spell(SpellSlot.E, 400f);
            R = new Spell(SpellSlot.R, 500f);

            Q.SetSkillshot(0.3f, 125, float.MaxValue, false, SpellType.Line);
            W.SetSkillshot(0f, 120f, float.MaxValue, true, SpellType.Line);
            E.SetSkillshot(0f, 0f, float.MaxValue, false, SpellType.Line);
            R.SetSkillshot(0.5f, 300f, float.MaxValue, false, SpellType.Circle);
            
            W.SetCharged("ViegoW", "ViegoW", 300, 900, 1f);
            

            Config = new Menu("Viego", "[Nebula]: Viego", true);

            var menuC = new Menu("Csettings", "Combo");
            menuC.Add(new MenuBool("UseQ", "Use Q in Combo"));
            menuC.Add(new MenuBool("UseW", "Use W in Combo"));
            menuC.Add(new MenuBool("UseE", "Use E in Combo"));
            menuC.Add(new MenuBool("UseR", "Use R in Combo"));

            var menuL = new Menu("Clear", "Clear");
            menuL.Add(new MenuBool("LcQ", "Use Q in Lanclear"));
            menuL.Add(new MenuBool("JcQ", "Use Q in Jungleclear"));
            menuL.Add(new MenuBool("JcW", "Use W in Jungleclear"));
            menuL.Add(new MenuBool("JcE", "Use E in Jungleclear"));
            
            var menuK = new Menu("Killsteal", "Killsteal");
            menuK.Add(new MenuBool("KsQ", "Use Q to Killsteal"));

            var menuD = new Menu("dsettings", "Drawings ");
            menuD.Add(new MenuBool("drawQ", "Q Range  (White)", true));
            menuD.Add(new MenuBool("drawW", "W Range  (White)", true));
            menuD.Add(new MenuBool("drawE", "E Range (White)", true));
            menuD.Add(new MenuBool("drawR", "R Range  (Red)", true));



            Config.Add(menuC);
            Config.Add(menuL);
            Config.Add(menuK);
            Config.Add(menuD);

            Config.Attach();

            GameEvent.OnGameTick += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
        }

        public static void OnGameUpdate(EventArgs args)
        {

            if (W.IsCharging)
            {
                Orbwalker.AttackEnabled = false;
            }
            else
            {
                Orbwalker.AttackEnabled = true;
            }


            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicW();
                LogicQ();
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

        private static void LogicR()
        {
            var target = TargetSelector.GetTarget(R.Range);
            var input = R.GetPrediction(target);
            var useR = Config["Csettings"].GetValue<MenuBool>("UseR").Enabled;
            if (target == null) return;

            if (input.Hitchance >= HitChance.High && !target.IsInvulnerable && R.IsInRange(target) && useR && ObjectManager.Player.GetSpellDamage(target, SpellSlot.R) > target.Health)
            {
                R.Cast(input.UnitPosition);
            }

            if (input.Hitchance >= HitChance.High && !target.IsInvulnerable && R.IsInRange(target) && useR &&
                Q.GetDamage(target) + W.GetDamage(target) + R.GetDamage(target) >= target.Health)
            {
                R.Cast(input.UnitPosition);
            }
        }
        
        public static float lastchangingQ = 0;
        private static void LogicW()
        {
            var target = TargetSelector.GetTarget(W.Range);
            var useW = Config["Csettings"].GetValue<MenuBool>("UseW");
            var input = W.GetPrediction(target);
            if (target == null) return;

            if (!useW.Enabled) return;

            if (W.IsReady() && input.Hitchance >= HitChance.High)
            {
                W.StartCharging();

                {
                    Orbwalker.AttackEnabled = false;

                    if ((!useW.Enabled || Variables.GameTimeTickCount - lastchangingQ > 1000))
                    {
                        if (W.IsChargedSpell)
                        {
                            W.Cast(input.UnitPosition);
                        }
                    }
                }
            }
        }

        private static void LogicE()
        {
            var target = TargetSelector.GetTarget(E.Range);
            var useE = Config["Csettings"].GetValue<MenuBool>("UseE").Enabled;
            if (target == null) return;
            
            if (E.IsReady() && useE)
            {
                E.Cast(target);
            }
        }


        private static void LogicQ()
        {
            var target = TargetSelector.GetTarget(Q.Range);
            var input = Q.GetPrediction(target);
            var useQ = Config["Csettings"].GetValue<MenuBool>("UseQ").Enabled;
            if (target == null) return;

            if (input.Hitchance >= HitChance.High && useQ && Q.IsInRange(target))
            {
                Q.Cast(input.UnitPosition);
            }
        }

        private static void Jungle()
        {
            var JcEe = Config["Clear"].GetValue<MenuBool>("JcE");
            var JcWw = Config["Clear"].GetValue<MenuBool>("JcW");
            var JcQq = Config["Clear"].GetValue<MenuBool>("JcQ");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(E.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcWw.Enabled && W.IsReady() && ObjectManager.Player.Distance(mob.Position) < W.ChargedMaxRange)
                {
                    W.StartCharging();
                    {
                        if (W.IsChargedSpell)
                        {
                            W.Cast(mob);
                        }
                    }
                }
                if (JcEe.Enabled && E.IsReady() && ObjectManager.Player.Distance(mob.Position) < E.Range) E.Cast(mob);
                if (JcQq.Enabled && Q.IsReady() &&  ObjectManager.Player.Distance(mob.Position) < Q.Range) Q.Cast(mob.Position);
            }
        }


        private static void Laneclear()
        {
            var allMinions = GameObjects.Minions.Where(x => x.IsValidTarget(E.Range * 2 + E.Width) && !x.IsMoving).Cast<AIBaseClient>().ToList();
            var rangedMinions = GameObjects.Minions.Where(x => x.IsValidTarget(E.Range * 2 + E.Width) && x.IsRanged && !x.IsMoving).Cast<AIBaseClient>().ToList();
            var eLocation = E.GetCircularFarmLocation(allMinions, E.Range);
            var e2Location = E.GetCircularFarmLocation(rangedMinions, E.Range);
            var bestLocation = (eLocation.MinionsHit > e2Location.MinionsHit + 1) ? eLocation : e2Location;

            if (Q.IsReady() && Config["Clear"].GetValue<MenuBool>("LcQ").Enabled)
            {
                if (bestLocation.MinionsHit < 3 && bestLocation.MinionsHit > 0)
                {
                    Q.Cast(bestLocation.Position);
                }
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var target = TargetSelector.GetTarget(Q.Range);

            if (target == null) return;
            if (target.IsInvulnerable) return;

            if (!(ObjectManager.Player.Distance(target.Position) <= Q.Range) ||
                !(ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q) >= target.Health + 20)) return;
            if (Q.IsReady() && ksQ) Q.Cast(target);
            
        }
    }
}