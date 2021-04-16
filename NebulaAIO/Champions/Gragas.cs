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
using EnsoulSharp.SDK.Utility;

namespace NebulaAio.Champions
{

    public class Gragas
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuC;
        private static HitChance hitchance;
        private static bool Exploded { get; set;  }
        private static Vector3 insecpos;
        private static Vector3 eqpos;
        private static Vector3 movingawaypos;
        private static GameObject Barrel;
        private static SpellSlot igniteslot;

        public static void OnGameLoad()
        {
            if (ObjectManager.Player.CharacterName != "Gragas")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 850f);
            W = new Spell(SpellSlot.W, 400f);
            E = new Spell(SpellSlot.E, 600f);
            R = new Spell(SpellSlot.R, 1000f);

            Q.SetSkillshot(0.25f, 250f, 1000f, false, SpellType.Circle);
            E.SetSkillshot(0.25f, 180f, 900f, true, SpellType.Line);
            R.SetSkillshot(0.25f, 400f, float.MaxValue, false, SpellType.Circle);
            
            igniteslot = ObjectManager.Player.GetSpellSlot("SummonerDot");


            Config = new Menu("Gragas", "[Nebula]: Gragas", true);

            menuC = new Menu("Csettings", "Combo");
            menuC.Add(new MenuBool("UseQ", "Use Q in Combo"));
            menuC.Add(new MenuBool("UseW", "Use W in Combo"));
            menuC.Add(new MenuBool("UseE", "Use E in Combo"));
            menuC.Add(new MenuBool("UseR", "Use R in Combo"));
            menuC.Add(new MenuList("Pred", "Prediction Hitchance",
                new string[] {"Low", "Medium", "High", "Very High"}, 2));

            var menuL = new Menu("Clear", "Clear");
            menuL.Add(new MenuBool("LcQ", "Use Q in Lanclear"));
            menuL.Add(new MenuBool("JcQ", "Use Q in Jungleclear"));
            menuL.Add(new MenuBool("JcW", "Use W in Jungleclear"));
            menuL.Add(new MenuBool("JcE", "Use E in Jungleclear"));

            var menuM = new Menu("Misc", "Misc");
            menuM.Add(new MenuKeyBind("InsecMode", "Insec Mode - Leftclick The Target You Want To Insec", Keys.T,
                KeyBindType.Press));
            menuM.Add(new MenuSliderButton("Skin", "SkindID", 0, 0, 30, false));
            
            var menuK = new Menu("Killsteal", "Killsteal");
            menuK.Add(new MenuBool("KsQ", "Use Q to Killsteal"));
            menuK.Add(new MenuBool("KsE", "Use E to Killsteal"));
            menuK.Add(new MenuBool("KsR", "Use R to Killsteal"));

            var menuD = new Menu("dsettings", "Drawings ");
            menuD.Add(new MenuBool("drawQ", "Q Range  (White)", true));
            menuD.Add(new MenuBool("drawE", "E Range (White)", true));
            menuD.Add(new MenuBool("drawR", "R Range  (Red)", true));
            menuD.Add(new MenuBool("DrawInsec", "Draw Insec Position"));
            menuD.Add(new MenuBool("drawD", "Draw Combo Damage", true));



            Config.Add(menuC);
            Config.Add(menuL);
            Config.Add(menuM);
            Config.Add(menuK);
            Config.Add(menuD);

            Config.Attach();

            GameEvent.OnGameTick += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            GameObject.OnCreate += GragasObject;
            GameObject.OnDelete += GragasBarrelNull;
            Drawing.OnDraw += etcdraw;
            AntiGapcloser.OnGapcloser += OnEnemyGapcloser;
        }
        
        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        private static void OnEnemyGapcloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (!sender.IsEnemy | args.EndPosition.DistanceToPlayer() > 300)
            {
                return;
            }

            if (E.IsReady() && E.GetPrediction(sender).Hitchance >= HitChance.High)
            {
                E.Cast(sender);
            }

            if (R.IsReady() && sender.IsValidTarget(R.Range))
            {
                R.Cast(sender);
            }
        }

        private static void etcdraw(EventArgs args)
        {
            var target = TargetSelector.SelectedTarget;
            if (target == null || target.IsValidTarget(R.Range))
            {
                target = TargetSelector.GetTarget(R.Range);
            }

            if (target == null)
            {
                return;
            }

            var epos = Drawing.WorldToScreen(target.Position);
            
            if (Config["dsettings"].GetValue<MenuBool>("DrawInsec").Enabled && R.IsReady() && target.IsValidTarget(R.Range) && R.Level > 0)
                
                Drawing.DrawText(epos.X, epos.Y, Color.DarkGreen, "Insec Target");
            if (Config["dsettings"].GetValue<MenuBool>("DrawInsec").Enabled && R.IsReady() && target.IsValidTarget(R.Range) && R.Level > 0)
                Render.Circle.DrawCircle(target.Position, 150, Color.LightGreen);
            if (Config["dsettings"].GetValue<MenuBool>("DrawInsec").Enabled && R.IsReady() &&
                target.IsValidTarget(R.Range) && R.Level > 0)
                insecpos = ObjectManager.Player.Position.Extend(target.Position,
                    ObjectManager.Player.Distance(target) + 150);
            Render.Circle.DrawCircle(insecpos, 100, Color.GreenYellow);
        }

        private static void GragasBarrelNull(GameObject sender, EventArgs args)
        {
            {
            }

            if (sender.Name.Contains("Gragas") && sender.Name.Contains("Q_Ally"))
            {
                Barrel = null;
            }
        }

        public static void OnGameUpdate(EventArgs args)
        {
            
            if (Config["Misc"].GetValue<MenuKeyBind>("InsecMode").Active)
                InsecCombo();


            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicW();
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
            Killsteal();
            skind();
        }

        private static void InsecCombo()
        {
            var target = TargetSelector.SelectedTarget;
            if (target == null || !target.IsValidTarget(R.Range))
            {
                return;
            }
            
            switch (comb(menuC, "Pred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }

            Orbwalker.Orbwalk(null, Game.CursorPos);

            eqpos = ObjectManager.Player.Position.Extend(target.Position, ObjectManager.Player.Distance(target));
            insecpos = ObjectManager.Player.Position.Extend(target.Position,
                ObjectManager.Player.Distance(target) + 200);
            movingawaypos =
                ObjectManager.Player.Position.Extend(target.Position, ObjectManager.Player.Distance(target) + 300);
            eqpos = ObjectManager.Player.Position.Extend(target.Position, ObjectManager.Player.Distance(target) + 100);

            eqpos = ObjectManager.Player.Position.Extend(target.Position, ObjectManager.Player.Distance(target));
            insecpos = ObjectManager.Player.Position.Extend(target.Position,
                ObjectManager.Player.Distance(target) + 200);
            movingawaypos =
                ObjectManager.Player.Position.Extend(target.Position, ObjectManager.Player.Distance(target) + 300);
            eqpos = ObjectManager.Player.Position.Extend(target.Position, ObjectManager.Player.Distance(target) + 100);

            if (target.IsFacing(ObjectManager.Player) == false &&
                target.IsMoving & (R.IsInRange(insecpos) && target.Distance(insecpos) < 300))
                R.Cast(movingawaypos);

            if (R.IsInRange(insecpos) && target.Distance(insecpos) < 300 && target.IsFacing(ObjectManager.Player) &&
                target.IsMoving)
                R.Cast(eqpos);

            else if (R.IsInRange(insecpos) && target.Distance(insecpos) < 300)
                R.Cast(insecpos);

            if (!Exploded) return;

            var prediction = E.GetPrediction(target);
            if (prediction.Hitchance >= hitchance)
            {
                E.Cast(target.Position);
                Q.Cast(target.Position);
            }
        }

        private static void GragasObject(GameObject sender, EventArgs args)
        {
            if (sender.Name.Contains("Gragas") && sender.Name.Contains("R_End"))
            {
                Exploded = true;
                DelayAction.Add(3000, () => { Exploded = false; });
            }

            if (sender.Name.Contains("Gragas") && sender.Name.Contains("Q_Ally"))
            {
                Barrel = sender;



            }
        }

        private static bool IsWall(Vector3 pos)
        {
            CollisionFlags collisionFlags = NavMesh.GetCollisionFlags(pos);
            return (collisionFlags == CollisionFlags.Wall);
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

        private static void LogicR()
        {
            var target = TargetSelector.GetTarget(1000);
            var useR = Config["Csettings"].GetValue<MenuBool>("UseR");
            if (target == null) return;
            
            if (R.IsReady() && useR.Enabled && Q.GetDamage(target) + W.GetDamage(target) + E.GetDamage(target) + R.GetDamage(target) >= target.Health && target.IsValidTarget(R.Range))
            {
                R.Cast(insecpos);
            }
        }

        private static void LogicW()
        {
            var target = TargetSelector.GetTarget(W.Range);
            var useW = Config["Csettings"].GetValue<MenuBool>("UseW");
            if (target == null) return;

            if (W.IsReady() && useW.Enabled && target.IsValidTarget(E.Range))
            {
                W.Cast();
            }
        }

        private static void LogicE()
        {
            var target = TargetSelector.GetTarget(E.Range);
            var useE = Config["Csettings"].GetValue<MenuBool>("UseE");
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

            if (E.IsReady() && useE.Enabled && ObjectManager.Player.HasBuff("GragasWAttackBuff") && input.Hitchance >= hitchance && target.IsValidTarget(E.Range))
            {
                E.Cast(input.CastPosition);
            }

            if (E.IsReady() && useE.Enabled && input.Hitchance >= hitchance && target.IsValidTarget(E.Range) &&
                Q.GetDamage(target) + W.GetDamage(target) + E.GetDamage(target) + R.GetDamage(target) >= target.Health)
            {
                E.Cast(input.CastPosition);
            }
        }
        

        private static void LogicQ()
        {
            var target = TargetSelector.GetTarget(1000);
            var input = Q.GetPrediction(target);
            var useQ = Config["Csettings"].GetValue<MenuBool>("UseQ");
            if (target == null) return;
            
            switch (comb(menuC, "Pred"))
            {
                case 0: hitchance = HitChance.Low; break;
                case 1: hitchance = HitChance.Medium; break;
                case 2: hitchance = HitChance.High; break;
                case 3: hitchance = HitChance.VeryHigh; break;
                default: hitchance = HitChance.High; break;
            }

            if (Q.IsReady() && useQ.Enabled && input.Hitchance >= hitchance && Barrel == null && target.IsValidTarget(Q.Range))
            {
                Q.Cast(input.CastPosition);
            }
        }

        private static void Jungle()
        {
            var JcEe = Config["Clear"].GetValue<MenuBool>("JcE");
            var JcWe = Config["Clear"].GetValue<MenuBool>("JcW");
            var JcQq = Config["Clear"].GetValue<MenuBool>("JcQ");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(E.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcEe.Enabled && E.IsReady() && ObjectManager.Player.Distance(mob.Position) < E.Range) E.Cast(mob);
                if (JcWe.Enabled && W.IsReady() && ObjectManager.Player.Distance(mob.Position) < W.Range) W.Cast();
                if (JcQq.Enabled && Q.IsReady() &&  ObjectManager.Player.Distance(mob.Position) < Q.Range) Q.Cast(mob.Position);
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
                    var qFarmLocation = Q.GetCircularFarmLocation(minions);
                    if (qFarmLocation.Position.IsValid())
                    {
                        Q.Cast(qFarmLocation.Position);
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
            var target = TargetSelector.GetTarget(1000);

            if (target == null) return;
            if (target.IsInvulnerable) return;

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