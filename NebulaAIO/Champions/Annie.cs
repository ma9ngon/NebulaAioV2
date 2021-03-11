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

    public static class Annie
    {
        private static Spell Q, W, E, R;
        private static Menu Config;
        private static bool gotAggro;

        public static void OnGameLoad()
        {
            if (ObjectManager.Player.CharacterName != "Annie")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 625f);
            W = new Spell(SpellSlot.W, 625f);
            E = new Spell(SpellSlot.E, 800f);
            R = new Spell(SpellSlot.R, 600f);
            
            Q.SetTargetted(0.25f, float.MaxValue);
            W.SetSkillshot(0.25f, 49f, float.MaxValue, false, SpellType.Cone);
            R.SetSkillshot(0.25f, 250f, float.MaxValue, false, SpellType.Circle);


            Config = new Menu("Annie", "[Nebula]: Annie", true);

            var menuC = new Menu("Csettings", "Combo");
            menuC.Add(new MenuBool("UseQ", "Use Q in Combo"));
            menuC.Add(new MenuBool("UseW", "Use W in Combo"));
            menuC.Add(new MenuBool("UseE", "Use E in Combo"));
            menuC.Add(new MenuBool("UseR", "Use R in Combo"));

            var menuL = new Menu("Clear", "Clear");
            menuL.Add(new MenuBool("LHQ", "Use Q To LastHit"));
            menuL.Add(new MenuBool("LcW", "Use W in Lanclear"));
            menuL.Add(new MenuBool("JcQ", "Use Q in Jungleclear"));
            menuL.Add(new MenuBool("JcW", "Use W in Jungleclear"));

            var menuM = new Menu("Misc", "Misc");
            menuM.Add(new MenuBool("aes", "Auto Cast E To Get Last Passive Stack"));
            menuM.Add(new MenuBool("asf", "Auto Stack Passive In fountain"));
            menuM.Add(new MenuSliderButton("Skin", "SkindID", 0, 0, 30, false));

            var menuK = new Menu("Killsteal", "Killsteal");
            menuK.Add(new MenuBool("KsQ", "Use Q to Killsteal"));
            menuK.Add(new MenuBool("KsW", "Use W to Killsteal"));
            menuK.Add(new MenuBool("KsR", "Use R to Killsteal"));

            var menuD = new Menu("dsettings", "Drawings ");
            menuD.Add(new MenuBool("drawQ", "Q Range  (White)", true));
            menuD.Add(new MenuBool("drawW", "W Range  (White)", true));
            menuD.Add(new MenuBool("drawE", "E Range (White)", true));
            menuD.Add(new MenuBool("drawR", "R Range  (Red)", true));



            Config.Add(menuC);
            Config.Add(menuL);
            Config.Add(menuM);
            Config.Add(menuK);
            Config.Add(menuD);

            Config.Attach();

            GameEvent.OnGameTick += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            AIHeroClient.OnAggro += OnAggro;
        }

        public static void OnGameUpdate(EventArgs args)
        {


            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicR();
                LogicQ();
                LogicW();
                LogicE();
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
            basestack();
            EStack();
            Shield();
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

        private static void OnAggro(AIBaseClient sender, AIBaseClientAggroEventArgs args)
        {
            if (!ObjectManager.Player.IsDead && sender.IsEnemy && !sender.IsMinion() &&
                args.NetworkId == ObjectManager.Player.NetworkId) gotAggro = true;
        }

        private static bool InAARangeOf(this AIHeroClient player, AIHeroClient target)
        {
            if (player.Distance(target.Position) < target.AttackRange) return true;
            return false;
        }

        private static void LogicR()
        {
            var target = TargetSelector.GetTarget(1000);
            var useR = Config["Csettings"].GetValue<MenuBool>("UseR");
            if (target == null) return;
            
            if (R.IsReady() && useR.Enabled && Q.GetDamage(target) + W.GetDamage(target) + R.GetDamage(target) >= target.Health && target.IsValidTarget(R.Range))
            {
                var rpred = R.GetPrediction(target, false, 0);
                if (rpred.Hitchance >= HitChance.High)
                {
                    R.Cast(rpred.CastPosition);
                }
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
            if (target == null) return;

            if (E.IsReady() && useE.Enabled)
            {
                var close = GameObjects.EnemyHeroes.Where(x =>
                    ObjectManager.Player.InAARangeOf(x) && (x.IsFacing(ObjectManager.Player) ||
                                                            x.GetWaypoints().LastOrDefault().DistanceToPlayer() <
                                                            100f));
                if (gotAggro && !close.Any())
                {
                    gotAggro = false;
                }
                else if (gotAggro && close.Any())
                {
                    E.Cast();
                }
            }

            if (E.IsReady() && useE.Enabled && target.IsValidTarget(Q.Range) &&
                Q.GetDamage(target) + W.GetDamage(target) >= target.Health)
            {
                E.Cast();
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
        
        private static void Shield()
        {
            var target = TargetSelector.GetTarget(E.Range);
            var useE = Config["Csettings"].GetValue<MenuBool>("UseE");

            foreach (var allies in GameObjects.AllyHeroes.Where(y => y.HealthPercent < 35 && useE.Enabled && y.DistanceToPlayer() < E.Range && !ObjectManager.Player.IsMe))
            {
                E.Cast(allies);
            }
        }

        private static void EStack()
        {
            var aes = Config["Misc"].GetValue<MenuBool>("aes");
            if (aes.Enabled && ObjectManager.Player.GetBuffCount("anniepassivestack") == 3 && E.IsReady())
            {
                E.Cast();
            }
        }

        private static void basestack()
        {
            var basestack = Config["Misc"].GetValue<MenuBool>("asf");
            if (basestack.Enabled && ObjectManager.Player.InFountain() &&
                ObjectManager.Player.GetBuffCount("anniepassivestack") < 4 && !ObjectManager.Player.HasBuff("anniepassiveprimed"))
            {
                W.Cast(Game.CursorPos);
                E.Cast();
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
        
        private static void LastHit()
        {
            var LHQ = Config["Clear"].GetValue<MenuBool>("LHQ").Enabled;
            var allMinions = GameObjects.EnemyMinions.Where(x => x.IsMinion() && !x.IsDead && LHQ)
                .OrderBy(x => x.Distance(ObjectManager.Player.Position));

            foreach (var min in allMinions.Where(x => x.IsValidTarget(Q.Range) && x.Health < Q.GetDamage(x)))
            {
                Orbwalker.ForceTarget = min;
                Q.Cast(min);
            }
        }
    }
}