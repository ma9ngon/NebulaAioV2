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

    public class Lux
    {
        private static Spell Q, W, E, R;
        private static Menu Config;

        public static void OnGameLoad()
        {
            if (ObjectManager.Player.CharacterName != "Lux")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 1300);
            W = new Spell(SpellSlot.W, 1175f);
            E = new Spell(SpellSlot.E, 1100f);
            R = new Spell(SpellSlot.R, 3400f);

            Q.SetSkillshot(0.25f, 140f, 2400f, false, SpellType.Line, HitChance.None);
            W.SetSkillshot(0.25f, 160f, 1700f, true, SpellType.Line, HitChance.None);
            E.SetSkillshot(0.25f, 310f, 1200f, false, SpellType.Circle, HitChance.None);
            R.SetSkillshot(1.00f, 200f, float.MaxValue, false, SpellType.Line, HitChance.None);                     

            Config = new Menu("Lux", "[Nebula]: Lux", true);

            var menuC = new Menu("Csettings", "Combo");
            menuC.Add(new MenuBool("UseQ", "Use Q in Combo"));
            menuC.Add(new MenuBool("UseW", "Use W to shield low allies"));
            menuC.Add(new MenuBool("UseE", "Use E in Combo"));
            menuC.Add(new MenuBool("UseR", "Use R in Combo"));

            var menuL = new Menu("Clear", "Clear");
            menuL.Add(new MenuBool("LcE", "Use E in Lanclear"));
            menuL.Add(new MenuBool("JcQ", "Use Q in Jungleclear"));
            menuL.Add(new MenuBool("JcE", "Use E in Jungleclear"));
            
            var menuK = new Menu("Killsteal", "Killsteal");
            menuK.Add(new MenuBool("KsQ", "Use Q to Killsteal"));
            menuK.Add(new MenuBool("KsE", "Use E to Killsteal"));
            menuK.Add(new MenuBool("KsR", "Use R to Killsteal"));
            
            var menuM = new Menu("Misc", "Misc");
            menuM.Add(new MenuBool("rdrake", "Drake/Baron steal"));
            menuM.Add(new MenuBool("Agc", "Q Antigapcloser"));
            menuM.Add(new MenuBool("bc", "Auto Burst Combo"));

            var menuH = new Menu("skillpred", "SkillShot HitChance ");
            menuH.Add(new MenuList("qchance", "Q HitChance:", new[] { "Low", "Medium", "High", }, 2));
            menuH.Add(new MenuList("echance", "E HitChance:", new[] { "Low", "Medium", "High", }, 2));
            

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
        }

        public static void OnGameUpdate(EventArgs args)
        {

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicE();
                LogicQ();
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
            BurstCombo();
            steal();
            Killsteal();
            LogicW();
        }

        private static void OnGapcloser(AIBaseClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (Config["Misc"].GetValue<MenuBool>("Agc").Enabled && Q.IsReady() && sender.IsEnemy)
            {
                if (sender.IsValidTarget(Q.Range))
                {
                    Q.Cast(sender);
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
                Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, System.Drawing.Color.Red);
            }
        }

        private static void BurstCombo()
        {
            var target = TargetSelector.GetTarget(1000);
            var bc = Config["Misc"].GetValue<MenuBool>("bc");
            var input = Q.GetPrediction(target);
            var inputt = E.GetPrediction(target);
            if (target == null) return;

            if (input.Hitchance >= HitChance.High && inputt.Hitchance >= HitChance.High && bc.Enabled &&
                target.IsValidTarget(E.Range) &&
                Q.GetDamage(target) + E.GetDamage(target) + R.GetDamage(target) >= target.Health && Q.IsReady() && E.IsReady() && R.IsReady())
            {
                E.Cast(inputt.UnitPosition);
                Q.Cast(input.UnitPosition);
                R.Cast(target);
            }

        }

        private static void LogicR()
        {
            var target = TargetSelector.GetTarget(1000);
            var useR = Config["Csettings"].GetValue<MenuBool>("UseR");
            var input = R.GetPrediction(target);
            if (target == null) return;

            if (input.Hitchance >= HitChance.High && useR.Enabled && ObjectManager.Player.GetSpellDamage(target, SpellSlot.R) > target.Health && !target.IsInvulnerable &&
                target.IsValidTarget(R.Range))
            {
                R.Cast(input.UnitPosition);
            }
        }

        private static void LogicW()
        {
            var target = TargetSelector.GetTarget(W.Range);
            var ally = GameObjects.AllyHeroes.Where(x => x.IsValidTarget(W.Range, false) && x.HealthPercent < 35 && !x.IsMe)
                .OrderBy(x => x.Health).ToList();

            if (ally.Count > 0)
            {
                W.Cast(ally[0].Position);
            }
        }


        private static void LogicE()
        {
            var target = TargetSelector.GetTarget(E.Range);
            var useE = Config["Csettings"].GetValue<MenuBool>("UseE").Enabled;
            var input = E.GetPrediction(target);
            if (target == null) return;

            var eFarmSet = Config["skillpred"].GetValue<MenuList>("echance").SelectedValue;
            string final = eFarmSet;
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
            
            if (input.Hitchance >= skill && target.IsValidTarget(E.Range) && useE)
            {
                E.Cast(input.UnitPosition);
            }
        }


        private static void LogicQ()
        {
            var target = TargetSelector.GetTarget(Q.Range);
            var input = Q.GetPrediction(target);
            if (target == null) return;

            var col = Q.GetCollision(GameObjects.Player.Position.ToVector2(),
                new List<Vector2> {input.CastPosition.ToVector2()});
            var minions = col.Where(x => !(x is AIHeroClient)).OrderBy(x => x.IsMinion()).Take(2)
                .Count(x => x.IsMinion());

            var qFarmSet = Config["skillpred"].GetValue<MenuList>("qchance").SelectedValue;
            string final = qFarmSet;
            var skill = HitChance.High;
            var useQ = Config["Csettings"].GetValue<MenuBool>("UseQ").Enabled;

            if (final == "0")
            {
                skill = HitChance.Low;
            }

            if (final == "1")
            {
                skill = HitChance.Medium;
            }

            if (final == "2")
            {
                skill = HitChance.High;
            }

            if (minions < 2)
            {
                if (input.Hitchance >= skill && useQ && target.IsValidTarget(Q.Range))
                {
                    Q.Cast(input.UnitPosition);
                    LogicW();
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
                if (JcEe.Enabled && E.IsReady() && ObjectManager.Player.Distance(mob.Position) < E.Range) E.Cast(mob.Position);
                if (JcQq.Enabled && Q.IsReady() &&  ObjectManager.Player.Distance(mob.Position) < Q.Range) Q.Cast(mob.Position);
            }
        }

        private static void steal()
        {
            if (Config["Misc"].GetValue<MenuBool>("rdrake").Enabled && R.IsReady() )
            {
                var mob = ObjectManager.Get<AIMinionClient>()
                    .Where(x => x.IsValidTarget(R.Range) && x.AttackRange >= 500f && x.IsJungle())
                    .OrderBy(x => x.Health).FirstOrDefault();
                if (mob == null)
                    return;

                if (mob.Health <= R.GetDamage(mob) + 25f && !mob.IsDead)
                {
                    R.Cast(mob.Position);
                }
            }
        }


        private static void Laneclear()
        {
            var lce = Config["Clear"].GetValue<MenuBool>("LcE");
            if (lce.Enabled && E.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                if (minions.Any())
                {
                    var eFarmLocation = E.GetCircularFarmLocation(minions);
                    if (eFarmLocation.Position.IsValid())
                    {
                        E.Cast(eFarmLocation.Position);
                        return;
                    }
                }
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksE = Config["Killsteal"].GetValue<MenuBool>("KsE").Enabled;
            var ksR = Config["Killsteal"].GetValue<MenuBool>("KsR").Enabled;
            var target = TargetSelector.GetTarget(Q.Range);
            var targets = TargetSelector.GetTarget(E.Range);
            var targetss = TargetSelector.GetTarget(R.Range);

            if (target == null) return;
            if (target.IsInvulnerable) return;
            if (targets == null) return;
            if (targets.IsInvulnerable) return;
            if (targetss == null) return;
            if (targetss.IsInvulnerable) return;

            if (!(ObjectManager.Player.Distance(target.Position) <= Q.Range) ||
                !(ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q) >= target.Health + 20)) return;
            if (Q.IsReady() && ksQ) Q.Cast(target);
            
            if (!(ObjectManager.Player.Distance(target.Position) <= E.Range) ||
                !(ObjectManager.Player.GetSpellDamage(target, SpellSlot.E) >= target.Health + 20)) return;
            if (E.IsReady() && ksE) E.Cast(target);
            
            if (!(ObjectManager.Player.Distance(target.Position) <= R.Range) ||
                !(ObjectManager.Player.GetSpellDamage(target, SpellSlot.R) >= target.Health + 20)) return;
            if (R.IsReady() && ksR) R.Cast(target);
            
        }
    }
}