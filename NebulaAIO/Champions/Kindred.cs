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

    public class Kindred
    {
        private static Spell Q, W, E, R;
        private static Menu Config;
        private static SpellSlot igniteslot;

        public static void OnGameLoad()
        {
            if (ObjectManager.Player.CharacterName != "Kindred")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 340f);
            W = new Spell(SpellSlot.W, 550f);
            E = new Spell(SpellSlot.E, ObjectManager.Player.GetRealAutoAttackRange());
            R = new Spell(SpellSlot.R, 500f);

            E.SetTargetted(0.25f, float.MaxValue);
            
            igniteslot = ObjectManager.Player.GetSpellSlot("SummonerDot");


            Config = new Menu("Kindred", "[Nebula]: Kindred", true);

            var menuC = new Menu("Csettings", "Combo");
            menuC.Add(new MenuBool("UseQ", "Use Q in Combo"));
            menuC.Add(new MenuBool("UseW", "Use W in Combo"));
            menuC.Add(new MenuBool("UseE", "Use E in Combo"));
            menuC.Add(new MenuBool("UseR", "Auto Use R to save"));

            var menuL = new Menu("Clear", "Clear");
            menuL.Add(new MenuBool("JcQ", "Use Q in Jungleclear"));
            menuL.Add(new MenuBool("JcW", "Use W in Jungleclear"));
            menuL.Add(new MenuBool("JcE", "Use E in Jungleclear"));

            var menuM = new Menu("Misc", "Misc");
            menuM.Add(new MenuSliderButton("Skin", "SkindID", 0, 0, 30, false));

            var menuD = new Menu("dsettings", "Drawings ");
            menuD.Add(new MenuBool("drawQ", "Q Range  (White)", true));
            menuD.Add(new MenuBool("drawW", "W Range (White)", true));
            menuD.Add(new MenuBool("drawE", "E Range (White)", true));
            menuD.Add(new MenuBool("drawR", "R Range  (Red)", true));
            menuD.Add(new MenuBool("drawD", "Draw Combo Damage", true));



            Config.Add(menuC);
            Config.Add(menuL);
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
                LogicE();
                LogicQ();
                LogicW();

            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                Jungle();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LastHit)
            {

            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.Harass)
            {

            }
            LogicR();
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

        private static void LogicQ()
        {
            var target = TargetSelector.GetTarget(Q.Range);
            var useQ = Config["Csettings"].GetValue<MenuBool>("UseQ").Enabled;
            if (target == null) return;

            if (Q.IsReady() && useQ && target.IsValidTarget(Q.Range))
            {
                Q.Cast(Game.CursorPos);
            }
        }

        private static void LogicW()
        {
            var target = TargetSelector.GetTarget(W.Range);
            var useW = Config["Csettings"].GetValue<MenuBool>("UseW").Enabled;
            if (target == null) return;

            if (W.IsReady() && useW && target.IsValidTarget(W.Range))
            {
                W.Cast(target);
            }
        }
        
        private static void LogicE()
        {
            var target = TargetSelector.GetTarget(E.Range);
            var useE = Config["Csettings"].GetValue<MenuBool>("UseE").Enabled;
            if (target == null) return;

            if (E.IsReady() && useE && target.IsValidTarget(E.Range))
            {
                E.Cast(target);
            }
        }

        private static void LogicR()
        {
            var useR = Config["Csettings"].GetValue<MenuBool>("UseR").Enabled;

            if (useR && ObjectManager.Player.HealthPercent < 30)
            {
                R.Cast();
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
                    Q.Cast(Game.CursorPos);
                if (JcWw.Enabled && W.IsReady() && ObjectManager.Player.Distance(mob.Position) < W.Range)
                    W.Cast(mob.Position);
            }
        }
    }
}