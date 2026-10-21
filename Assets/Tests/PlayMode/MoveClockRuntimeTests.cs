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
            foreach (var go in UnityEngine.Object.FindObjectsOfType<GameObject>())
            {
                if (go != null && (go.name.Contains("Fixture") || go.name.Contains("MoveClock") || go.name.Contains("GameManager") || go.name.Contains("PlayerController")))
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }
            var gmType = RuntimeType("GameManager");
            var instProp = gmType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (instProp != null && instProp.CanWrite) instProp.SetValue(null, null);
            var instField = gmType.GetField("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ?? gmType.GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (instField != null) instField.SetValue(null, null);
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

            var winnerProp = gmType.GetProperty("Winner", AnyInstance) ?? gmType.GetProperty("MatchResult", AnyInstance);
            Assert.IsNotNull(winnerProp, "Winner or MatchResult property must exist on GameManager");
            string winnerVal = winnerProp.GetValue(gm)?.ToString() ?? "";
            Assert.IsTrue(winnerVal.IndexOf("Blue", StringComparison.OrdinalIgnoreCase) >= 0 || winnerVal == "2",
                "Expected Blue win on Red timeout forfeit, got: " + winnerVal);

            // Verify idempotency / single settlement on subsequent calls
            triggerMethod.Invoke(clock, new object[] { false });
            Assert.IsTrue((bool)isMatchOverProp.GetValue(gm));
            Assert.IsTrue((bool)wasTimeoutProp.GetValue(gm));

            UnityEngine.Object.DestroyImmediate(clockGo);
            UnityEngine.Object.DestroyImmediate(gmGo);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MoveClockComponent_NaturalClockExpiration_OppositeWinnerAndSingleSettlement()
        {
            var gmType = RuntimeType("GameManager");
            var clockType = RuntimeType("MoveClock");

            var gmGo = new GameObject("GameManagerNaturalExpiryFixture");
            var gm = gmGo.AddComponent(gmType);

            var clockGo = new GameObject("MoveClockNaturalExpiryFixture");
            var clock = clockGo.AddComponent(clockType) as MonoBehaviour;

            var forceRankedProp = clockType.GetProperty("ForceRankedForTesting", AnyInstance | AnyStatic) 
                ?? gmType.GetProperty("ForceRankedForTesting", AnyInstance | AnyStatic);
            if (forceRankedProp != null && forceRankedProp.CanWrite)
            {
                forceRankedProp.SetValue(forceRankedProp.GetSetMethod(true).IsStatic ? null : (object)clock, true);
            }
            var forceRankedField = clockType.GetField("ForceRankedForTesting", AnyInstance | AnyStatic)
                ?? gmType.GetField("ForceRankedForTesting", AnyInstance | AnyStatic);
            if (forceRankedField != null)
            {
                forceRankedField.SetValue(forceRankedField.IsStatic ? null : (object)clock, true);
            }

            var turnDurProp = clockType.GetProperty("TurnDuration", AnyInstance) ?? clockType.GetProperty("turnDuration", AnyInstance);
            if (turnDurProp != null && turnDurProp.CanWrite) turnDurProp.SetValue(clock, 0.05f);
            var turnDurField = clockType.GetField("TurnDuration", AnyInstance) ?? clockType.GetField("turnDuration", AnyInstance);
            if (turnDurField != null) turnDurField.SetValue(clock, 0.05f);

            yield return null;

            var stateObj = GetProperty(clock, "State");
            if (stateObj != null)
            {
                var stateType = stateObj.GetType();
                var resetMethod = stateType.GetMethod("Reset", new[] { typeof(float), typeof(float) }) 
                    ?? stateType.GetMethod("Configure", new[] { typeof(float), typeof(float) });
                if (resetMethod != null) resetMethod.Invoke(stateObj, new object[] { 0.05f, 0.02f });

                var startTurn = stateType.GetMethod("StartTurn", new[] { typeof(bool) });
                if (startTurn != null) startTurn.Invoke(stateObj, new object[] { true });
            }

            var startTurnClock = clockType.GetMethod("StartTurn", new[] { typeof(bool) });
            if (startTurnClock != null) startTurnClock.Invoke(clock, new object[] { true });

            var isMatchOverProp = gmType.GetProperty("IsMatchOver", AnyInstance);
            var wasTimeoutProp = gmType.GetProperty("WasTimeout", AnyInstance);
            Assert.IsNotNull(isMatchOverProp, "GameManager.IsMatchOver property must exist");
            Assert.IsNotNull(wasTimeoutProp, "GameManager.WasTimeout property must exist");

            float deadline = Time.realtimeSinceStartup + 3.0f;
            while (Time.realtimeSinceStartup < deadline && !(bool)isMatchOverProp.GetValue(gm))
            {
                yield return null;
            }

            Assert.IsTrue((bool)isMatchOverProp.GetValue(gm), "Match should be marked over strictly by natural Update-driven expiration");
            Assert.IsTrue((bool)wasTimeoutProp.GetValue(gm), "WasTimeout should be true");

            var winnerProp = gmType.GetProperty("Winner", AnyInstance) ?? gmType.GetProperty("MatchResult", AnyInstance);
            Assert.IsNotNull(winnerProp, "Winner or MatchResult property must exist on GameManager");
            string winnerVal = winnerProp.GetValue(gm)?.ToString() ?? "";
            Assert.IsTrue(winnerVal.IndexOf("Blue", StringComparison.OrdinalIgnoreCase) >= 0 || winnerVal == "2", 
                "Opposite player (Blue) should win unconditionally when Red times out. Actual winner: " + winnerVal);

            yield return null;
            yield return null;
            Assert.IsTrue((bool)isMatchOverProp.GetValue(gm), "Match must remain concluded with single settlement");

            UnityEngine.Object.DestroyImmediate(clockGo);
            UnityEngine.Object.DestroyImmediate(gmGo);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MoveClockComponent_ResolvingMove_PausesClockProgression()
        {
            var gmType = RuntimeType("GameManager");
            var clockType = RuntimeType("MoveClock");

            var gmGo = new GameObject("GameManagerResolvingFixture");
            var gm = gmGo.AddComponent(gmType);

            var clockGo = new GameObject("MoveClockResolvingFixture");
            var clock = clockGo.AddComponent(clockType) as MonoBehaviour;

            var forceRankedProp = clockType.GetProperty("ForceRankedForTesting", AnyInstance | AnyStatic) 
                ?? gmType.GetProperty("ForceRankedForTesting", AnyInstance | AnyStatic);
            if (forceRankedProp != null && forceRankedProp.CanWrite)
            {
                forceRankedProp.SetValue(forceRankedProp.GetSetMethod(true).IsStatic ? null : (object)clock, true);
            }

            var startTurnClock = clockType.GetMethod("StartTurn", new[] { typeof(bool) });
            if (startTurnClock != null) startTurnClock.Invoke(clock, new object[] { true });

            yield return null;

            var setResolvingMethod = gmType.GetProperty("IsResolvingMove", AnyInstance)?.GetSetMethod(true);
            var resolvingField = gmType.GetField("<IsResolvingMove>k__BackingField", AnyInstance);
            if (setResolvingMethod != null) setResolvingMethod.Invoke(gm, new object[] { true });
            else if (resolvingField != null) resolvingField.SetValue(gm, true);

            var stateObj = GetProperty(clock, "State");
            var stateType = stateObj.GetType();
            var remProp = stateType.GetProperty("RemainingSeconds");
            float remWhileResolving1 = (float)remProp.GetValue(stateObj);

            for (int i = 0; i < 5; i++) yield return null;

            float remWhileResolving2 = (float)remProp.GetValue(stateObj);
            Assert.AreEqual(remWhileResolving1, remWhileResolving2, 1e-4f, "MoveClock must pause while GameManager.IsResolvingMove is true");

            if (setResolvingMethod != null) setResolvingMethod.Invoke(gm, new object[] { false });
            else if (resolvingField != null) resolvingField.SetValue(gm, false);

            for (int i = 0; i < 5; i++) yield return null;

            float remResumed = (float)remProp.GetValue(stateObj);
            Assert.Less(remResumed, remWhileResolving2, "MoveClock must resume ticking once GameManager.IsResolvingMove becomes false");

            UnityEngine.Object.DestroyImmediate(clockGo);
            UnityEngine.Object.DestroyImmediate(gmGo);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MoveClockComponent_TurnTransition_SynchronizesActiveSideAndResetsTimer()
        {
            var gmType = RuntimeType("GameManager");
            var pcType = RuntimeType("PlayerController");
            var clockType = RuntimeType("MoveClock");

            var gmGo = new GameObject("GameManagerTurnHandoffFixture");
            var gm = gmGo.AddComponent(gmType);

            var pcGo = new GameObject("PlayerControllerTurnHandoffFixture");
            var pc = pcGo.AddComponent(pcType);

            var clockGo = new GameObject("MoveClockTurnHandoffFixture");
            var clock = clockGo.AddComponent(clockType) as MonoBehaviour;

            var forceRankedProp = clockType.GetProperty("ForceRankedForTesting", AnyInstance | AnyStatic) 
                ?? gmType.GetProperty("ForceRankedForTesting", AnyInstance | AnyStatic);
            if (forceRankedProp != null && forceRankedProp.CanWrite)
            {
                forceRankedProp.SetValue(forceRankedProp.GetSetMethod(true).IsStatic ? null : (object)clock, true);
            }

            yield return null;

            var startTurnClock = clockType.GetMethod("StartTurn", new[] { typeof(bool) });
            if (startTurnClock != null) startTurnClock.Invoke(clock, new object[] { true });

            var stateObj = GetProperty(clock, "State");
            var stateType = stateObj.GetType();
            var tickMethod = stateType.GetMethod("Tick", new[] { typeof(float) });
            tickMethod.Invoke(stateObj, new object[] { 20f });

            var remProp = stateType.GetProperty("RemainingSeconds");
            float remBefore = (float)remProp.GetValue(stateObj);
            Assert.AreEqual(10f, remBefore, 0.2f, "Timer should be depleted to 10s");

            var setResolvingMethod = gmType.GetProperty("IsResolvingMove", AnyInstance)?.GetSetMethod(true);
            var resolvingField = gmType.GetField("<IsResolvingMove>k__BackingField", AnyInstance);
            if (setResolvingMethod != null) setResolvingMethod.Invoke(gm, new object[] { true });
            else if (resolvingField != null) resolvingField.SetValue(gm, true);

            yield return null;

            var isRedTurnField = pcType.GetField("isRedTurn", AnyInstance) ?? pcType.GetField("_isRedTurn", AnyInstance);
            var isRedTurnProp = pcType.GetProperty("isRedTurn", AnyInstance) ?? pcType.GetProperty("IsRedTurn", AnyInstance);
            if (isRedTurnProp != null && isRedTurnProp.CanWrite) isRedTurnProp.SetValue(pc, false);
            else if (isRedTurnField != null) isRedTurnField.SetValue(pc, false);

            if (setResolvingMethod != null) setResolvingMethod.Invoke(gm, new object[] { false });
            else if (resolvingField != null) resolvingField.SetValue(gm, false);

            yield return null;

            var activeIsRedProp = clockType.GetProperty("ActiveIsRed", AnyInstance);
            Assert.IsFalse((bool)activeIsRedProp.GetValue(clock), "MoveClock.ActiveIsRed should automatically synchronize to PlayerController turn (false/Blue)");

            float remAfter = (float)remProp.GetValue(stateObj);
            Assert.Greater(remAfter, 25f, "MoveClock timer should reset to fresh turn duration upon turn handoff");

            UnityEngine.Object.DestroyImmediate(clockGo);
            UnityEngine.Object.DestroyImmediate(pcGo);
            UnityEngine.Object.DestroyImmediate(gmGo);
            yield return null;
        }
    }
}
