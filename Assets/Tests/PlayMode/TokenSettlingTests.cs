using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KineticRank.Tests.PlayMode
{
    public class TokenSettlingTests
    {
        private GameObject host;
        private Component token;
        private Type tokenType;

        [SetUp]
        public void SetUp()
        {
            tokenType = Type.GetType("Token, Assembly-CSharp", true);
            host = new GameObject("Token settling test");
            token = host.AddComponent(tokenType);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (host != null) UnityEngine.Object.Destroy(host);
            yield return null;
        }

        private bool HasArrived()
        {
            return (bool)tokenType.GetProperty("HasArrived").GetValue(token);
        }

        private void Move(string method, Vector2 target)
        {
            tokenType.GetMethod(method, BindingFlags.Public | BindingFlags.Instance)
                .Invoke(token, new object[] { target });
        }

        [UnityTest]
        public IEnumerator AnimatedFallArrivesAtTarget()
        {
            host.transform.position = new Vector2(0f, 2f);
            Vector2 target = Vector2.zero;
            Move("AnimateFall", target);
            Assert.That(HasArrived(), Is.False);

            float deadline = Time.realtimeSinceStartup + 5f;
            while (!HasArrived() && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.That(HasArrived(), Is.True, "Fall did not finish within five seconds.");
            Assert.That((Vector2)host.transform.position, Is.EqualTo(target));
        }

        [UnityTest]
        public IEnumerator SettlingCancelsActiveFallAndSurvivesFollowingFrames()
        {
            host.transform.position = new Vector2(0f, 1000f);
            Move("AnimateFall", Vector2.zero);
            Assert.That(HasArrived(), Is.False);
            yield return null;

            Vector2 settled = new Vector2(3f, 4f);
            Move("SettleAt", settled);
            Assert.That(HasArrived(), Is.True);
            yield return null;
            yield return null;
            Assert.That(HasArrived(), Is.True);
            Assert.That((Vector2)host.transform.position, Is.EqualTo(settled));

            Move("AnimateFall", new Vector2(3f, 1000f));
            Assert.That(HasArrived(), Is.False, "A subsequent fall must reset arrival state.");
        }
    }
}
