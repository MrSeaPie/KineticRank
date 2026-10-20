using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace KineticRank.Tests.PlayMode
{
    // Traditional mode: PlayerController.Awake marks every power-up used, so keys, HUD and the Solo bot only see Normal moves.
    // Runtime types are reached by reflection because this asmdef cannot reference Assembly-CSharp; no GameManager, so no rank file is touched.
    public class TraditionalModeTests
    {
        const BindingFlags AnyStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        const BindingFlags AnyInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        GameObject go;

        // Accepts a full name (KineticRank.Logic.PuzzleSettings) or, as a fallback, a simple type name in any namespace
        static Type RuntimeType(string name)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var type = assemblies.Select(a => a.GetType(name, false)).FirstOrDefault(t => t != null);
            if (type == null)
            {
                type = assemblies
                    .SelectMany(a =>
                    {
                        try { return a.GetTypes(); }
                        catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null).ToArray(); }
                    })
                    .FirstOrDefault(t => t.Name == name && (t.Namespace == null || t.Namespace.StartsWith("KineticRank")));
            }
            Assert.IsNotNull(type, $"Runtime type {name} not found");
            return type;
        }

        static bool StaticEnabled(string typeName)
        {
            var type = RuntimeType(typeName);
            var prop = type.GetProperty("Enabled", AnyStatic);
            if (prop != null) return (bool)prop.GetValue(null);
            var field = type.GetField("Enabled", AnyStatic);
            Assert.IsNotNull(field, $"{typeName}.Enabled not found");
            return (bool)field.GetValue(null);
        }

        static void SetTraditional(bool on)
        {
            RuntimeType("TraditionalSettings").GetMethod(on ? "StartTraditional" : "Clear", AnyStatic).Invoke(null, null);
        }

        Component SpawnController()
        {
            go = new GameObject("TraditionalModeTests.PlayerController");
            return go.AddComponent(RuntimeType("PlayerController"));
        }

        static bool Available(Component pc, string method, bool red)
        {
            return (bool)pc.GetType().GetMethod(method, AnyInstance).Invoke(pc, new object[] { red });
        }

        static object Kind(string name)
        {
            return Enum.Parse(RuntimeType("KineticRank.Logic.MoveKind"), name);
        }

        [SetUp]
        public void SetUp()
        {
            Assume.That(StaticEnabled("KineticRank.Logic.PuzzleSettings"), Is.False, "Puzzle mode leaked from another fixture");
            Assume.That(StaticEnabled("SoloSettings"), Is.False, "Solo mode leaked from another fixture");
            SetTraditional(false);
        }

        [TearDown]
        public void TearDown()
        {
            if (go != null) UnityEngine.Object.DestroyImmediate(go);
            go = null;
            SetTraditional(false);
        }

        [Test]
        public void TraditionalOn_NeitherSideHasPowerUps()
        {
            SetTraditional(true);
            var pc = SpawnController();

            Assert.IsFalse(Available(pc, "QueenAvailable", true), "Red queen");
            Assert.IsFalse(Available(pc, "CleanerAvailable", true), "Red cleaner");
            Assert.IsFalse(Available(pc, "QueenAvailable", false), "Blue queen");
            Assert.IsFalse(Available(pc, "CleanerAvailable", false), "Blue cleaner");
        }

        [Test]
        public void TraditionalOn_SelectingPowerUpStaysOnNormal()
        {
            SetTraditional(true);
            var pc = SpawnController();
            var select = pc.GetType().GetMethod("Select", AnyInstance);
            var selected = pc.GetType().GetProperty("SelectedKind", AnyInstance);

            select.Invoke(pc, new[] { Kind("Queen") });
            Assert.AreEqual(Kind("Normal"), selected.GetValue(pc), "Queen selection");

            select.Invoke(pc, new[] { Kind("Cleaner") });
            Assert.AreEqual(Kind("Normal"), selected.GetValue(pc), "Cleaner selection");
        }

        [Test]
        public void TraditionalOff_AllPowerUpsAvailable()
        {
            var pc = SpawnController();

            Assert.IsTrue(Available(pc, "QueenAvailable", true), "Red queen");
            Assert.IsTrue(Available(pc, "CleanerAvailable", true), "Red cleaner");
            Assert.IsTrue(Available(pc, "QueenAvailable", false), "Blue queen");
            Assert.IsTrue(Available(pc, "CleanerAvailable", false), "Blue cleaner");
        }
    }
}
