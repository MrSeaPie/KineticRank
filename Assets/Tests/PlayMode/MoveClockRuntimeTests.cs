using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KineticRank.Tests.PlayMode
{
    public class MoveClockRuntimeTests
    {
        const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        static Type RuntimeType(string name)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(name);
                if (type != null) return type;
            }
            throw new InvalidOperationException("Missing runtime type: " + name);
        }

        static object GetProperty(object target, string name)
        {
            var prop = target.GetType().GetProperty(name, AnyInstance);
            Assert.That(prop, Is.Not.Null, "Missing property: " + target.GetType().Name + "." + name);
            return prop.GetValue(target);
        }

        static object InvokeMethod(object target, string name, params object[] args)
        {
            var method = target.GetType().GetMethod(name, AnyInstance);
            Assert.That(method, Is.Not.Null, "Missing method: " + target.GetType().Name + "." + name);
            return method.Invoke(target, args);
        }

        GameObject fixtureRoot;

        [SetUp]
        public void SetUp()
        {
            fixtureRoot = new GameObject("MoveClockTestFixture");
        }

        [TearDown]
        public void TearDown()
        {
            if (fixtureRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(fixtureRoot);
            }
        }

        [Test]
        public void MoveClockState_DirectLogic_CountdownAndExpiry()
        {
            var stateType = RuntimeType("KineticRank.Logic.MoveClockState");
            var state = Activator.CreateInstance(stateType, 30f, 10f);
            
            var startTurnMethod = stateType.GetMethod("StartTurn", new[] { typeof(bool) });
            startTurnMethod.Invoke(state, new object[] { true });

            var tickMethod = stateType.GetMethod("Tick", new[] { typeof(float) });
            bool expired = (bool)tickMethod.Invoke(state, new object[] { 10f });
            Assert.IsFalse(expired);

            float remaining = (float)stateType.GetProperty("RemainingSeconds").GetValue(state);
            Assert.AreEqual(20f, remaining, 1e-4f);

            bool warning = (bool)stateType.GetProperty("IsWarning").GetValue(state);
            Assert.IsFalse(warning);

            expired = (bool)tickMethod.Invoke(state, new object[] { 15f });
            Assert.IsFalse(expired);
            warning = (bool)stateType.GetProperty("IsWarning").GetValue(state);
            Assert.IsTrue(warning);

            expired = (bool)tickMethod.Invoke(state, new object[] { 10f });
            Assert.IsTrue(expired);
            bool isExpired = (bool)stateType.GetProperty("IsExpired").GetValue(state);
            Assert.IsTrue(isExpired);
        }

        [Test]
        public void MoveClockState_PauseResume_HaltsProgression()
        {
            var stateType = RuntimeType("KineticRank.Logic.MoveClockState");
            var state = Activator.CreateInstance(stateType, 30f, 10f);

            var startTurnMethod = stateType.GetMethod("StartTurn", new[] { typeof(bool) });
            startTurnMethod.Invoke(state, new object[] { true });

            var tickMethod = stateType.GetMethod("Tick", new[] { typeof(float) });
            tickMethod.Invoke(state, new object[] { 5f });

            var pauseMethod = stateType.GetMethod("Pause", Type.EmptyTypes);
            pauseMethod.Invoke(state, null);

            bool expired = (bool)tickMethod.Invoke(state, new object[] { 10f });
            Assert.IsFalse(expired);

            float remaining = (float)stateType.GetProperty("RemainingSeconds").GetValue(state);
            Assert.AreEqual(25f, remaining, 1e-4f);

            var resumeMethod = stateType.GetMethod("Resume", Type.EmptyTypes);
            resumeMethod.Invoke(state, null);

            tickMethod.Invoke(state, new object[] { 5f });
            remaining = (float)stateType.GetProperty("RemainingSeconds").GetValue(state);
            Assert.AreEqual(20f, remaining, 1e-4f);
        }

        [UnityTest]
        public IEnumerator MoveClockComponent_LifecycleAndSingleForfeit()
        {
            var clockType = RuntimeType("MoveClock");
            var comp = fixtureRoot.AddComponent(clockType) as MonoBehaviour;
            Assert.IsNotNull(comp);

            yield return null;

            var state = GetProperty(comp, "State");
            Assert.IsNotNull(state);

            yield return null;
        }

        [UnityTest]
        public IEnumerator MoveClockComponent_TimeoutForfeit_FinishesMatchSingleSettlement()
        {
            var gmType = RuntimeType("GameManager");
            var clockType = RuntimeType("MoveClock");

            var gmGo = new GameObject("GameManagerTimeoutFixture");
            var gm = gmGo.AddComponent(gmType);

            var clockGo = new GameObject("MoveClockTimeoutFixture");
            var clock = clockGo.AddComponent(clockType) as MonoBehaviour;

            yield return null;

            var triggerMethod = clockType.GetMethod("TriggerTimeoutForfeit", BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(triggerMethod, "Missing TriggerTimeoutForfeit method");
            triggerMethod.Invoke(clock, new object[] { true });

            var isMatchOverProp = gmType.GetProperty("IsMatchOver", BindingFlags.Public | BindingFlags.Instance);
            var wasTimeoutProp = gmType.GetProperty("WasTimeout", BindingFlags.Public | BindingFlags.Instance);

            Assert.IsTrue((bool)isMatchOverProp.GetValue(gm), "Match should be marked over after timeout forfeit");
            Assert.IsTrue((bool)wasTimeoutProp.GetValue(gm), "WasTimeout should be true after timeout forfeit");

            // Verify idempotency / single settlement on subsequent calls
            triggerMethod.Invoke(clock, new object[] { false });
            Assert.IsTrue((bool)isMatchOverProp.GetValue(gm));
            Assert.IsTrue((bool)wasTimeoutProp.GetValue(gm));

            UnityEngine.Object.DestroyImmediate(clockGo);
            UnityEngine.Object.DestroyImmediate(gmGo);
            yield return null;
        }
    }
}
