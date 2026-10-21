using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class EloHudBindingTests
{
    static Type FindType(string name)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(name);
            if (type != null) return type;
        }
        Assert.Fail("Missing runtime type: " + name);
        return null;
    }

    [Test]
    public void BindingRefreshesAfterSettlementAndNullRestoresCasualText()
    {
        var storeType = FindType("PlayerRankStore");
        var sessionType = FindType("RankedMatchSession");
        var hudType = FindType("EloHud");
        var formatter = FindType("RankedMatchPresentation").GetMethod("Format");
        var getProfile = storeType.GetMethod("GetProfile");
        var beginIsolation = storeType.GetMethod("BeginIsolatedTestScope");
        using ((IDisposable)beginIsolation.Invoke(null, null))
        {
            GameObject root = null;
            try
            {
                root = new GameObject("EloHud binding test", typeof(RectTransform));
                root.SetActive(false);
                var label = root.AddComponent(FindType("TMPro.TextMeshProUGUI"));
                var hud = root.AddComponent(hudType);
                hudType.GetField("label").SetValue(hud, label);
                root.SetActive(true);
                var textProperty = label.GetType().GetProperty("text");
                string casual = (string)textProperty.GetValue(label);
                StringAssert.Contains("RED ", casual);
                StringAssert.Contains("BLUE ", casual);

                var session = Activator.CreateInstance(sessionType, new object[] { 1375, true });
                var bind = hudType.GetMethod("BindRankedSession");
                bind.Invoke(hud, new object[] { session });
                string active = (string)textProperty.GetValue(label);
                Assert.That(active, Is.EqualTo(formatter.Invoke(null, new object[] { session, getProfile.Invoke(null, null) })));
                StringAssert.Contains("Opponent rating 1375", active);
                StringAssert.DoesNotContain("Rating change", active);

                Assert.That(sessionType.GetMethod("TryComplete").Invoke(session, new object[] { true }), Is.True);
                hudType.GetMethod("Refresh").Invoke(hud, null);
                string completed = (string)textProperty.GetValue(label);
                Assert.That(completed, Is.EqualTo(formatter.Invoke(null, new object[] { session, getProfile.Invoke(null, null) })));
                StringAssert.Contains("Rating change +", completed);

                bind.Invoke(hud, new object[] { null });
                Assert.That(textProperty.GetValue(label), Is.EqualTo(casual));
            }
            finally
            {
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
