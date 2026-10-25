using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace KineticRank.Tests.EditMode
{
    public class MoveAdmissionTests
    {
        private GameObject host;
        private Component manager;
        private Type managerType;

        [SetUp]
        public void SetUp()
        {
            // Runtime scripts live in Assembly-CSharp, which an asmdef cannot reference.
            managerType = Type.GetType("GameManager, Assembly-CSharp", true);
            host = new GameObject("Move admission test");
            manager = host.AddComponent(managerType);
        }

        [TearDown]
        public void TearDown()
        {
            if (host != null) UnityEngine.Object.DestroyImmediate(host);
        }

        private bool Flag(string name)
        {
            return (bool)managerType.GetProperty(name).GetValue(manager);
        }

        private object Call(string name, params object[] arguments)
        {
            return managerType.GetMethod(name).Invoke(manager, arguments);
        }

        [Test]
        public void MoveAdmissionRejectsOverlapAndReopensAfterCompletion()
        {
            Assert.That(Flag("IsInputLocked"), Is.False);
            Assert.That(Call("TryBeginMove"), Is.EqualTo(true));
            Assert.That(Flag("IsResolvingMove"), Is.True);
            Assert.That(Flag("IsInputLocked"), Is.True);
            Assert.That(Call("TryBeginMove"), Is.EqualTo(false));

            Call("CompleteMove");
            Assert.That(Flag("IsResolvingMove"), Is.False);
            Assert.That(Flag("IsInputLocked"), Is.False);
            Call("CompleteMove");
            Assert.That(Call("TryBeginMove"), Is.EqualTo(true));
        }

        [Test]
        public void LegacyPlacementCallbackCannotUnlockAnActiveMove()
        {
            Assert.That(Call("TryBeginMove"), Is.EqualTo(true));

            Call("OnTokenPlaced", 0, 0, true);

            Assert.That(Flag("IsResolvingMove"), Is.True);
            Assert.That(Flag("IsInputLocked"), Is.True);
            Assert.That(Call("TryBeginMove"), Is.EqualTo(false));

            Call("CompleteMove");
            Assert.That(Flag("IsResolvingMove"), Is.False);
            Assert.That(Flag("IsInputLocked"), Is.False);
            Assert.That(Call("TryBeginMove"), Is.EqualTo(true));
        }

        [Test]
        public void TerminalMatchRejectsMovesAndCompletionCannotReopenInput()
        {
            // Set terminal state directly to avoid rank persistence and presentation effects.
            managerType.GetProperty("IsMatchOver")
                .GetSetMethod(true).Invoke(manager, new object[] { true });

            Assert.That(Flag("IsInputLocked"), Is.True);
            Assert.That(Call("TryBeginMove"), Is.EqualTo(false));
            Call("CompleteMove");
            Assert.That(Flag("IsMatchOver"), Is.True);
            Assert.That(Flag("IsInputLocked"), Is.True);
            Assert.That(Flag("IsResolvingMove"), Is.False);
        }
    }
}
