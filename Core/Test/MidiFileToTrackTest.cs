using System;
using System.Collections;
using System.Diagnostics;
using Godot;
using Melanchall.DryWetMidi.Smf;
using Melanchall.DryWetMidi.Smf.Interaction;
using MusicMachine.Music;
using NUnit.Framework;

namespace MusicMachine.Test
{
[TestFixture]
public class MidiFileToTrackTest
{
    [SetUp]
    public static void MakeMidiFile()
    {
        MidiFileTest.MakeMidiFile();
    }

    [Test]
    public static void MidiFileToTrack()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var midiFile = MidiFileTest.GetMidiFile();
        var tempoMap = midiFile.GetTempoMap();
        foreach (var trackChunk in midiFile.GetTrackChunks())
        {
            //to be replaced with manageTimedEvents
            Console.WriteLine($"Track chunk {trackChunk}");
            var noteTrack = trackChunk.GetNotes().MakeNoteTrack(tempoMap);
            foreach (var pair in noteTrack.Events())
            {
                Console.WriteLine(pair);
            }
        }
        stopwatch.Stop();
        Console.WriteLine($"Took {stopwatch.ElapsedMilliseconds} milliseconds");
    }
}
}