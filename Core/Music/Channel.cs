using System.Collections.Concurrent;
using System.Collections.Generic;
using Melanchall.DryWetMidi.Common;
using MusicMachine.ThirdParty.Midi;

namespace MusicMachine.Music
{
public class Channel
{
    //pan
    private readonly List<short> _cachedList = new List<short>();
    public readonly ConcurrentDictionary<short, AdsrPlayer> NotesOn = new ConcurrentDictionary<short, AdsrPlayer>();
    public ushort Bank;
    public float Expression = 1;
    public float PitchBend;
    public SevenBitNumber Program;
    public float Volume = 1;

    public void ClearNotPlaying()
    {
        var toRemove = _cachedList;
        foreach (var pair in NotesOn)
            if (!pair.Value.Playing)
                toRemove.Add(pair.Key);
        foreach (var i in toRemove)
            NotesOn.TryRemove(i, out _);
        toRemove.Clear();
    }

    public void UpdateVolume(float ampDb, float volumeDb)
    {
//        GD.Print("UPDATE VOLUME:");
        foreach (var player in NotesOn.Values)
            player.UpdateChannelVolume(ampDb, volumeDb, this);
    }

    public void UpdatePitchBend()
    {
        foreach (var player in NotesOn.Values)
            player.PitchBend = PitchBend;
    }
}
}