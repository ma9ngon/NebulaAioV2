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

    public class Twitch
    {
        private static Spell Q, W, E, R;
        private static Menu Config;
        private static SpellSlot igniteslot;

        public static void OnGameLoad()
        {
            if (ObjectManager.Player.CharacterName != "Twitch")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 950f);
            E = new Spell(SpellSlot.E, 1200f);
            R = new Spell(SpellSlot.R, 890f);

            W.SetSkillshot(0.25f, 100f, 1400f, false, SpellType.Circle);
            
            igniteslot = ObjectManager.Player.GetSpellSlot("SummonerDot");


            Config = new Menu("Twitch", "[Nebula]: Twitch", true);

            var menuC = new Menu("Csettings", "Combo");
            menuC.Add(new MenuBool("UseQ", "Use Q in Combo"));
            menuC.Add(new MenuSlider("qcount", "Min enemys To use Q", 1, 1, 5));
            menuC.Add(new MenuBool("UseW", "Use W in Combo"));
            menuC.Add(new MenuBool("UseE", "Use E in Combo"));
            menuC.Add(new MenuBool("UseR", "Use R in Combo"));
            menuC.Add(new MenuSlider("rcount", "Min enemys To use R", 2, 1, 5));

            var menuE = new Menu("Esettings", "E Settings");
            menuE.Add(new MenuBool("Ekill", "Use E Only When target is Killable", true));
            menuE.Add(new MenuBool("Estacks", "Use E When Target have Max Stacks", false));
            menuE.Add(new MenuBool("Edrake", "Use E To Drake/Baron Steal", true));

            var menuK = new Menu("Killsteal", "Killsteal");
            menuK.Add(new MenuBool("KsE", "Use E to Killsteal"));

            var menuM = new Menu("Misc", "Misc");
            menuM.Add(new MenuSliderButton("Skin", "SkindID", 0, 0, 30, false));

            var menuD = new Menu("dsettings", "Drawings ");
            menuD.Add(new MenuBool("drawW", "W Range  (White)", true));
            menuD.Add(new MenuBool("drawE", "E Range (White)", true));
            menuD.Add(new MenuBool("drawR", "R Range  (Red)", true));
            menuD.Add(new MenuBool("drawD", "Draw Combo Damage", true));



            Config.Add(menuC);
            Config.Add(menuE);
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


            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicE();
                LogicQ();
                LogicW();
                LogicR();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LastHit)
            {

            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.Harass)
            {

            }
            steal();
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
        
        private static void steal()
        {
            if (Config["Esettings"].GetValue<MenuBool>("Edrake").Enabled && E.IsReady() )
            {
                var mob = ObjectManager.Get<AIMinionClient>()
                    .Where(x => x.IsValidTarget(E.Range) && x.AttackRange >= 500f && x.IsJungle())
                    .OrderBy(x => x.Health).FirstOrDefault();
                if (mob == null)
                    return;

                if (mob.Health <= GetRealEDamage(mob) + 25f && !mob.IsDead)
                {
                    E.Cast();
                }
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
                if(args.EndPosition.DistanceToPlayer() <= 300 && sender.IsValidTarget(W.Range))
                {
                    if (W.Cast(sender) == CastStates.SuccessfullyCasted)
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

            if (R.IsReady() && useR.Enabled && GameObjects.EnemyHeroes.Count(x => x.DistanceToPlayer() <= R.Range) <= 2 && target.Health <= ObjectManager.Player.GetAutoAttackDamage(target) * 4 + GetRealEDamage(target) * 2 && target.IsValidTarget(R.Range))
            {
                R.Cast();
            }

            if (R.IsReady() && useR.Enabled && GameObjects.EnemyHeroes.Count(x => x.DistanceToPlayer() <= R.Range) >= combor)
            {
                R.Cast();
            }
        }

        private static void LogicW()
        {
            var target = TargetSelector.GetTarget(W.Range);
            var useW = Config["Csettings"].GetValue<MenuBool>("UseW");

            if (W.IsReady() && useW.Enabled && target.IsValidTarget(W.Range) && target.Health > GetRealEDamage(target) && GetEStackCount(target) < 6)
            {
                var wPred = W.GetPrediction(target);
                if (wPred.Hitchance >= HitChance.High)
                {
                    W.Cast(wPred.CastPosition);
                }
            }
        }

        private static void LogicE()
        {
            var target = TargetSelector.GetTarget(E.Range);
            var useE = Config["Csettings"].GetValue<MenuBool>("UseE");
            var useEkill = Config["Esettings"].GetValue<MenuBool>("Ekill");
            var estack = Config["Esettings"].GetValue<MenuBool>("Estacks");
            if (target == null) return;

            if (E.IsReady() && useE.Enabled && useEkill.Enabled && target.IsValidTarget(E.Range) && target.Health <= GetRealEDamage(target) && target.Buffs.Any(b => b.Name.ToLower() == "twitchdeadlyvenom"))
            {
                E.Cast();
            }

            if (E.IsReady() && useE.Enabled && estack.Enabled && target.IsValidTarget(E.Range) &&
                GetEStackCount(target) >= 6 && target.Buffs.Any(b => b.Name.ToLower() == "twitchdeadlyvenom"))
            {
                E.Cast();
            }
        }


        private static void LogicQ()
        {
            var target = TargetSelector.GetTarget(1000);
            var useQ = Config["Csettings"].GetValue<MenuBool>("UseQ");
            var comboq = Config["Csettings"].GetValue<MenuSlider>("qcount").Value;
            if (target == null) return;

            if (Q.IsReady() && useQ.Enabled && GameObjects.EnemyHeroes.Count(x => x.DistanceToPlayer() <= 750) >= comboq && target.IsValidTarget(Q.Range))
            {
                Q.Cast();
            }
        }

        private static void Killsteal()
        {
            var ksE = Config["Killsteal"].GetValue<MenuBool>("KsE").Enabled;

            if (ksE && E.IsReady())
            {
                foreach (
                    var target in 
                    GameObjects.EnemyHeroes.Where(
                        x => x.IsValidTarget(E.Range) && !x.IsInvulnerable))
                {
                    if (target.IsValidTarget(E.Range) && target.Health < GetRealEDamage(target) - target.HPRegenRate)
                    {
                        E.Cast();
                    }
                }
            }

        }

        private static double GetRealEDamage(AIBaseClient target)
        {
            if (target != null && !target.IsDead && target.Buffs.Any(b => b.Name.ToLower() == "twitchdeadlyvenom"))
            {
                if (target.HasBuff("KindredRNoDeathBuff"))
                {
                    return 0;
                }

                if (target.HasBuff("UndyingRage") && target.GetBuff("UndyingRage").EndTime - Game.Time > 0.3)
                {
                    return 0;
                }

                if (target.HasBuff("JudicatorIntervention"))
                {
                    return 0;
                }

                if (target.HasBuff("ChronoShift") && target.GetBuff("ChronoShift").EndTime - Game.Time > 0.3)
                {
                    return 0;
                }

                if (target.HasBuff("FioraW"))
                {
                    return 0;
                }

                if (target.HasBuff("ShroudofDarkness"))
                {
                    return 0;
                }

                if (target.HasBuff("SivirShield"))
                {
                    return 0;
                }

                var damage = 0d;

                damage += E.IsReady() ? GetEDMGTwitch(target) : 0d;

                if (target.CharacterName == "Morderkaiser")
                {
                    damage -= target.Mana;
                }

                if (ObjectManager.Player.HasBuff("SummonerExhaust"))
                {
                    damage = damage * 0.6f;
                }

                if (target.HasBuff("BlitzcrankManaBarrierCD") && target.HasBuff("ManaBarrier"))
                {
                    damage -= target.Mana / 2f;
                }

                if (target.HasBuff("GarenW"))
                {
                    damage = damage * 0.7f;
                }

                if (target.HasBuff("ferocioushowl"))
                {
                    damage = damage * 0.7f;
                }

                return damage;
            }

            return 0d;
        }

        public static double GetEDMGTwitch(AIBaseClient target)
        {
            if (target == null || !target.IsValidTarget())
            {
                return 0;
            }

            if (!target.HasBuff("twitchdeadlyvenom"))
            {
                return 0;
            }

            var eLevel = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).Level;
            if (eLevel <= 0)
            {
                return 0;
            }

            var buffCount = GetEStackCount(target);

            var baseDamage = new[] {0, 20, 30, 40, 50, 60}[eLevel];
            var extraDamage = new[] {0, 15, 20, 25, 30, 35}[eLevel] + 0.33f * ObjectManager.Player.TotalMagicalDamage +
                              0.35f * (ObjectManager.Player.TotalAttackDamage - ObjectManager.Player.BaseAttackDamage);
            var resultDamage =
                ObjectManager.Player.CalculateDamage(target, DamageType.Physical, baseDamage + extraDamage * buffCount);
            if (ObjectManager.Player.HasBuff("SummonerExhaust"))
            {
                resultDamage *= 0.6f;
            }

            return resultDamage;
        }

        public static int GetEStackCount(AIBaseClient target)
        {
            if (target == null || target.IsDead ||
                !target.IsValidTarget() ||
                target.Type != GameObjectType.AIMinionClient && target.Type != GameObjectType.AIHeroClient)
            {
                return 0;
            }

            return target.GetBuffCount("twitchdeadlyvenom");
        }
        
        public static bool IsUnKillable(AIBaseClient target)
        {
            if (target == null || target.IsDead || target.Health <= 0)
            {
                return true;
            }

            if (target.HasBuff("KindredRNoDeathBuff"))
            {
                return true;
            }

            if (target.HasBuff("UndyingRage") && target.GetBuff("UndyingRage").EndTime - Game.Time > 0.3f &&
                target.Health <= target.MaxHealth * 0.10f)
            {
                return true;
            }

            if (target.HasBuff("JudicatorIntervention"))
            {
                return true;
            }

            if (target.HasBuff("ChronoShift") && target.GetBuff("ChronoShift").EndTime - Game.Time > 0.3f &&
                target.Health <= target.MaxHealth * 0.10f)
            {
                return true;
            }

            if (target.HasBuff("VladimirSanguinePool"))
            {
                return true;
            }

            if (target.HasBuff("ShroudofDarkness"))
            {
                return true;
            }

            if (target.HasBuff("SivirShield"))
            {
                return true;
            }

            if (target.HasBuff("itemmagekillerveil"))
            {
                return true;
            }

            return target.HasBuff("FioraW");
        }
    }
}