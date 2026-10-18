using System.Collections.Generic;
using KineticRank.Logic;
using NUnit.Framework;

public class MusicPlaylistTests
{
    [Test]
    public void Next_CyclesInOrderAndWraps()
    {
        var playlist = new MusicPlaylist(new[] { "a", "b", "c" });

        Assert.IsNull(playlist.Current);
        Assert.AreEqual("a", playlist.Next());
        Assert.AreEqual("b", playlist.Next());
        Assert.AreEqual("c", playlist.Next());
        Assert.AreEqual("a", playlist.Next());
        Assert.AreEqual("a", playlist.Current);
    }

    [Test]
    public void Next_SkipsUnavailableTracks()
    {
        var missing = new HashSet<string> { "b" };
        var playlist = new MusicPlaylist(new[] { "a", "b", "c" });

        Assert.AreEqual("a", playlist.Next(n => !missing.Contains(n)));
        Assert.AreEqual("c", playlist.Next(n => !missing.Contains(n)));
        Assert.AreEqual("a", playlist.Next(n => !missing.Contains(n)));
    }

    [Test]
    public void Next_ReturnsNullWhenNothingAvailable_AndKeepsPosition()
    {
        var playlist = new MusicPlaylist(new[] { "a", "b" });
        playlist.Next();

        Assert.IsNull(playlist.Next(n => false));
        Assert.AreEqual("a", playlist.Current);
    }

    [Test]
    public void EmptyOrNullList_ReturnsNull()
    {
        Assert.IsNull(new MusicPlaylist(new string[0]).Next());
        Assert.IsNull(new MusicPlaylist(null).Next());
    }

    [Test]
    public void Reset_StartsFromFirstTrack()
    {
        var playlist = new MusicPlaylist(new[] { "a", "b" });
        playlist.Next();
        playlist.Next();

        playlist.Reset();

        Assert.IsNull(playlist.Current);
        Assert.AreEqual("a", playlist.Next());
    }

    [Test]
    public void DefaultTrackLists_MatchResourceNames()
    {
        CollectionAssert.AreEqual(new[] { "menu_1", "menu_2" }, MusicPlaylist.MenuTracks);
        Assert.AreEqual(6, MusicPlaylist.MatchTracks.Length);
        Assert.AreEqual("gameplay_1", MusicPlaylist.MatchTracks[0]);
        Assert.AreEqual("gameplay_6", MusicPlaylist.MatchTracks[5]);
    }

    [Test]
    public void Fade_InterpolatesAndClamps()
    {
        MusicFade.Volumes(0f, 0.75f, 0.45f, out float outgoing, out float incoming);
        Assert.AreEqual(0.45f, outgoing, 1e-5f);
        Assert.AreEqual(0f, incoming, 1e-5f);

        MusicFade.Volumes(0.375f, 0.75f, 0.45f, out outgoing, out incoming);
        Assert.AreEqual(0.225f, outgoing, 1e-5f);
        Assert.AreEqual(0.225f, incoming, 1e-5f);

        MusicFade.Volumes(5f, 0.75f, 0.45f, out outgoing, out incoming);
        Assert.AreEqual(0f, outgoing, 1e-5f);
        Assert.AreEqual(0.45f, incoming, 1e-5f);

        MusicFade.Volumes(-1f, 0.75f, 0.45f, out outgoing, out incoming);
        Assert.AreEqual(0.45f, outgoing, 1e-5f);
        Assert.AreEqual(0f, incoming, 1e-5f);
    }

    [Test]
    public void Fade_ZeroDurationSwitchesImmediately()
    {
        MusicFade.Volumes(0f, 0f, 0.45f, out float outgoing, out float incoming);
        Assert.AreEqual(0f, outgoing, 1e-5f);
        Assert.AreEqual(0.45f, incoming, 1e-5f);
    }
}
