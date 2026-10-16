using KineticRank.Logic;
using NUnit.Framework;

namespace KineticRank.Tests.EditMode
{
    public class ConfirmWindowTests
    {
        [Test]
        public void IsArmed_BeforeAnyPress_ReturnsFalse()
        {
            var window = new ConfirmWindow(2f);
            Assert.IsFalse(window.IsArmed(0f));
            Assert.IsFalse(window.IsArmed(1f));
        }

        [Test]
        public void Press_First_ArmsWithoutConfirming()
        {
            var window = new ConfirmWindow(2f);
            Assert.IsFalse(window.Press(10f));
            Assert.IsTrue(window.IsArmed(10f));
        }

        [Test]
        public void Press_SecondWithinWindow_Confirms()
        {
            var window = new ConfirmWindow(2f);
            window.Press(10f);
            Assert.IsTrue(window.Press(11f));
        }

        [Test]
        public void Press_SecondExactlyAtWindowEdge_Confirms()
        {
            var window = new ConfirmWindow(2f);
            window.Press(10f);
            Assert.IsTrue(window.Press(12f));
        }

        [Test]
        public void Press_SecondAfterWindowExpires_RearmsInsteadOfConfirming()
        {
            var window = new ConfirmWindow(2f);
            window.Press(10f);
            Assert.IsFalse(window.Press(12.5f));
            Assert.IsTrue(window.IsArmed(12.5f));
            Assert.IsTrue(window.Press(14f));
        }

        [Test]
        public void Press_AfterConfirm_StartsNewWindow()
        {
            var window = new ConfirmWindow(2f);
            window.Press(10f);
            window.Press(11f);
            Assert.IsFalse(window.IsArmed(11f));
            Assert.IsFalse(window.Press(11.5f));
        }

        [Test]
        public void Cancel_DisarmsSoNextPressOnlyArms()
        {
            var window = new ConfirmWindow(2f);
            window.Press(10f);
            window.Cancel();
            Assert.IsFalse(window.IsArmed(10.5f));
            Assert.IsFalse(window.Press(11f));
        }

        [Test]
        public void Constructor_NegativeWindow_ClampsToZero()
        {
            var window = new ConfirmWindow(-1f);
            window.Press(10f);
            Assert.IsTrue(window.IsArmed(10f));
            Assert.IsFalse(window.IsArmed(10.5f));
            Assert.IsFalse(window.Press(10.5f));
            Assert.IsTrue(window.Press(10.5f));
        }
    }
}
