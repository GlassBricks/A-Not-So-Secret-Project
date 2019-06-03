using System.Collections.Generic;
using Godot;
using MusicMachine.Music;
using MusicMachine.Scenes.Objects;
using MusicMachine.Tracks;
using MusicMachine.Util;

namespace MusicMachine.Test
{
public class PlayerDisplayer : Spatial
{
    private static readonly PackedScene Pointer = GD.Load<PackedScene>("res://Scenes/Objects/Pointer.tscn");
    private readonly bool[] _cacheArr = new bool[128];
    private readonly Color[] _colors;
    private readonly Spatial _displayPoint;
    private readonly Pointer[][] _pointers;
    private readonly Queue<Pointer> _queuedPointers = new Queue<Pointer>();
    private readonly SortofVirtualSynth _sortofVirtualSynth;

    private PlayerDisplayer()
    {
        //donut use
    }

    public PlayerDisplayer(Spatial displayPoint, Program program, string soundFontFile)
    {
        _displayPoint = displayPoint;
        _colors       = new Color[program.InstrumentTracks.Count];
        _colors.FillWith(
            () => Color.FromHsv(
                (float) GD.Randf(),
                (float) (0.8 + GD.Randf() * 0.6),
                (float) (0.4 + GD.Randf() * 0.6),
                0.5f));
        _pointers = new Pointer[program.InstrumentTracks.Count][];
        for (var index = 0; index < _pointers.Length; index++)
            _pointers[index] = new Pointer[128];
        _sortofVirtualSynth = new SortofVirtualSynth(program) {SoundFontFile = soundFontFile};
        AddChild(_sortofVirtualSynth);
    }

    public override void _Ready()
    {
        SetProcess(false);
    }

    public void Play(long startMidiTicks = 0)
    {
        Stop();
        _sortofVirtualSynth.Play(startMidiTicks);
        SetProcess(true);
    }

    public void Stop()
    {
        foreach (var child in GetChildren())
            if (child is Spatial spatial)
            {
                spatial.SetVisible(false);
                spatial.QueueFree();
            }
        _queuedPointers.Clear();
        _sortofVirtualSynth.Stop();
        foreach (var spatials in _pointers)
            spatials.Fill(null);
    }

    public override void _Process(float _)
    {
        var channelHasPt = _cacheArr;
        for (var iIndex = 0; iIndex < SortofVirtualSynth.PlayingState.Count; iIndex++)
        {
            channelHasPt.Fill(false);
            var notesOn = SortofVirtualSynth.PlayingState[iIndex].NotesOn;
            foreach (var playerPair in notesOn)
            {
                var pointer = _pointers[iIndex][playerPair.Key];
                if (pointer == null)
                    _pointers[iIndex][playerPair.Key] = pointer = GetPointer();
                pointer.SpatialMaterial.SetAlbedo(_colors[iIndex]);
                pointer.SpatialMaterial.SetEmission(_colors[iIndex]);
                pointer.SetTranslation(
                    _displayPoint.Translation
                  + Vector3.Forward * iIndex / 1.1f
                  + Vector3.Right * (playerPair.Key + playerPair.Value.PitchBend * Program.MaxSemitonesPitchBend) / 10);
                pointer.SetScale(new Vector3(1, 1, 1) * Mathf.Pow(2, playerPair.Value.VolumeDb / 10 + 2));
                channelHasPt[playerPair.Key] = true;
            }
            for (var pIndex = 0; pIndex < _cacheArr.Length; pIndex++)
            {
                if (channelHasPt[pIndex])
                    continue;
                var pointer = _pointers[iIndex][pIndex];
                if (pointer == null)
                    continue;
                pointer.SetVisible(false);
                _queuedPointers.Enqueue(pointer);
                _pointers[iIndex][pIndex] = null;
            }
        }
    }

    private Pointer GetPointer()
    {
        if (_queuedPointers.Count != 0)
        {
            var qPointer = _queuedPointers.Dequeue();
            qPointer.SetVisible(true);
            return qPointer;
        }
        var pointer = (Spatial) Pointer.Instance();
        AddChild(pointer);
        return (Pointer) pointer;
    }
}
}