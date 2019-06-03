using System;
using System.Linq;
using Godot;
using Melanchall.DryWetMidi.Smf;
using MusicMachine.Scenes;
using MusicMachine.Tracks;
using MusicMachine.Util;
using NoteOnEvent = MusicMachine.Tracks.NoteOnEvent;

namespace MusicMachine.Test
{
public class TestScene : Area
{
    private ProgramPlayer _displayer;
    private Player _player;
    private Program _program;

    public override void _Ready()
    {
        _player           = GetNode<Player>("Player");
        _player.Primary   = OnAction;
        _player.Secondary = OnSecondary;
        GetNode("Objects");
        var displayPoint = GetNode<Spatial>("DisplayPoint");

        const string midiLoc      = "res://Resources/Midi/Fireflies.mid";
        const string soundFontLoc = "res://Resources/Midi/Timbres Of Heaven GM_GS_XG_SFX V 3.4 Final.sf2";

        _program   = MidiFile.Read(ProjectSettings.GlobalizePath(midiLoc)).ToProgram();
        var synth = _program.PrepareSynth(soundFontLoc);
        AddChild(synth);
        _displayer = new ProgramPlayer(_program);
        AddChild(_displayer);
        foreach (var track in _program.MusicTracks) Console.WriteLine(track);

        OnSecondary(0);
    }

    private void OnAction(float delta)
    {
    }

    private void OnSecondary(float delta)
    {
        _displayer.Play();
//        GD.Print(_song.Tracks.SelectMany(x=>x.Events).Any(x=>x is PitchBendEvent));
    }

    private void OnBodyExited(Node body)
    {
        if (!body.TryCall("OnWorldExit"))
            body.QueueFree();
    }
}
}