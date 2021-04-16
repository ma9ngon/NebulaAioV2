using System;
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

    public class Lux
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuC;
        private static SpellSlot igniteslot;
        private static HitChance hitchance;

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

            Q.SetSkillshot(0.25f, 140f, 2400f, false, SpellType.Line);
            W.SetSkillshot(0.25f, 160f, 1700f, true, SpellType.Line);
            E.SetSkillshot(0.25f, 310f, 1200f, false, SpellType.Circle);
            R.SetSkillshot(1.00f, 200f, float.MaxValue, false, SpellType.Line);    
            
            igniteslot = ObjectManager.Player.GetSpellSlot("SummonerDot");

            Config = new Menu("Lux", "[Nebula]: Lux", true);

            menuC = new Menu("Csettings", "Combo");
            menuC.Add(new MenuBool("UseQ", "Use Q in Combo"));
            menuC.Add(new MenuBool("UseW", "Use W to shield low allies"));
            menuC.Add(new MenuBool("UseE", "Use E in Combo"));
            menuC.Add(new MenuBool("UseR", "Use R in Combo"));
            menuC.Add(new MenuList("Pred", "Prediction Hitchance",
                new string[] {"Low", "Medium", "High", "Very High"}, 2));

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
            menuM.Add(new MenuBool("bc", "Auto Burst Combo"));
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
            AntiGapcloser.OnGapcloser += Gapcloser_OnGapcloser;
        }
        
        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
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
                if(args.EndPosition.DistanceToPlayer() <= 300 && sender.IsValidTarget(Q.Range))
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

        private static void BurstCombo()
        {
            var target = TargetSelector.GetTarget(1000);
            var bc = Config["Misc"].GetValue<MenuBool>("bc");
            var input = Q.GetPrediction(target);
            var inputt = E.GetPrediction(target);
            var inputtt = R.GetPrediction(target);
            if (target == null) return;
            
            switch (comb(menuC, "Pred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }

            if (input.Hitchance >= hitchance && inputt.Hitchance >= hitchance && inputtt.Hitchance >= hitchance && bc.Enabled &&
                target.IsValidTarget(E.Range) &&
                Q.GetDamage(target) + E.GetDamage(target) + R.GetDamage(target) >= target.Health && Q.IsReady() && E.IsReady() && R.IsReady() && !target.IsInvulnerable)
            {
                Q.Cast(input.CastPosition);
                E.Cast(inputt.CastPosition);
                R.Cast(inputtt.CastPosition);
            }

        }

        private static void LogicR()
        {
            var target = TargetSelector.GetTarget(1000);
            var useR = Config["Csettings"].GetValue<MenuBool>("UseR");
            var input = R.GetPrediction(target);
            if (target == null) return;
            
            switch (comb(menuC, "Pred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }

            if (input.Hitchance >= hitchance && useR.Enabled && ObjectManager.Player.GetSpellDamage(target, SpellSlot.R) > target.Health && !target.IsInvulnerable &&
                target.IsValidTarget(R.Range))
            {
                R.Cast(input.CastPosition);
            }
        }

        private static void LogicW()
        {
            var target = TargetSelector.GetTarget(W.Range);
            var useW = Config["Csettings"].GetValue<MenuBool>("UseW");
            var ally = GameObjects.AllyHeroes.Where(x => x.IsValidTarget(W.Range, false) && x.HealthPercent < 35 && !x.IsMe)
                .OrderBy(x => x.Health).ToList();

            if (ally.Count > 0 && useW.Enabled)
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
            
            switch (comb(menuC, "Pred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }

            if (input.Hitchance >= hitchance && target.IsValidTarget(E.Range) && useE)
            {
                E.Cast(input.CastPosition);
            }
        }


        private static void LogicQ()
        {
            var target = TargetSelector.GetTarget(Q.Range);
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

            var col = Q.GetCollision(GameObjects.Player.Position.ToVector2(),
                new List<Vector2> {input.CastPosition.ToVector2()});
            var minions = col.Where(x => !(x is AIHeroClient)).OrderBy(x => x.IsMinion()).Take(2)
                .Count(x => x.IsMinion());
            
            var useQ = Config["Csettings"].GetValue<MenuBool>("UseQ").Enabled;

            if (minions < 2)
            {
                if (input.Hitchance >= hitchance && useQ && target.IsValidTarget(Q.Range))
                {
                    Q.Cast(input.CastPosition);
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